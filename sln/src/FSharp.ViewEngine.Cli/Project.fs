namespace FSharp.ViewEngine.Cli

open System
open System.IO
open System.Reflection
open System.Text.Json
open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
module ConsumerProject =
    [<Literal>]
    let ConfigFileName = "fve.json"

    [<Literal>]
    let ComponentsDirectory = "Components"

    let private packageVersion metadataKey packageName =
        Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
        |> Seq.tryFind (fun attribute -> attribute.Key = metadataKey)
        |> Option.map _.Value
        |> Option.filter (String.IsNullOrWhiteSpace >> not)
        |> Option.defaultWith (fun () -> invalidOp $"The CLI package is missing its {packageName} version metadata.")

    let CoreVersion = packageVersion "FSharpViewEngineCoreVersion" "FSharp.ViewEngine Core"
    let FSharpCoreVersion = packageVersion "FSharpCorePackageVersion" "FSharp.Core"

    let private startMarker = "  <!-- fve:components:start -->"
    let private endMarker = "  <!-- fve:components:end -->"

    let private jsonOptions =
        let options = JsonSerializerOptions()
        options.WriteIndented <- true
        options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        options

    let configPath (projectPath:string) = Path.Combine(Path.GetDirectoryName projectPath, ConfigFileName)

    let readConfiguration (path:string) =
        if not (File.Exists path) then Error $"Configuration '{path}' was not found. Run 'fve init' first."
        else
            try
                let configuration = JsonSerializer.Deserialize<Configuration>(File.ReadAllText path, jsonOptions)
                if isNull (box configuration) || configuration.SchemaVersion <> 1 then
                    Error $"Configuration '{path}' uses an unsupported schema."
                else Ok configuration
            with exceptionValue -> Error $"Could not read configuration '{path}': {exceptionValue.Message}"

    let writeConfiguration (path:string) (configuration:Configuration) =
        JsonSerializer.Serialize(configuration, jsonOptions) + "\n" |> Text.writeAtomic path

    let sourcePath (root:string) (fileName:string) = Path.Combine(root, ComponentsDirectory, fileName)
    let relativeSourcePath (fileName:string) = $"{ComponentsDirectory}/{fileName.Replace('\\', '/')}"
    let tryFileName (relativePath:string) =
        let normalized = relativePath.Replace('\\', '/')
        let prefix = ComponentsDirectory + "/"
        if normalized.StartsWith(prefix, StringComparison.Ordinal) then Some normalized[prefix.Length..]
        else None
    let projectBlock (components:RegistryFile list) =
        let compileEntries =
            components
            |> List.filter _.Compile
            |> List.map (fun item -> $"      <Compile Include=\"{relativeSourcePath item.FileName}\" />")
        let assetEntries =
            [ $"      <None Include=\"{ConfigFileName}\" />"
              for item in components do
                  if not item.Compile then
                      $"      <None Include=\"{relativeSourcePath item.FileName}\" />" ]
        let body = String.concat "\n" (compileEntries @ assetEntries) + "\n"
        $"{startMarker}\n  <ItemGroup Label=\"fve components\">\n{body}  </ItemGroup>\n{endMarker}"

    let private findBlockBetween (startMarker:string) (endMarker:string) (content:string) =
        let startIndex = content.IndexOf(startMarker, StringComparison.Ordinal)
        let endIndex = content.IndexOf(endMarker, StringComparison.Ordinal)
        if startIndex < 0 && endIndex < 0 then Ok None
        elif startIndex < 0 || endIndex < startIndex then Error "The project contains incomplete fve component markers."
        else Ok (Some (startIndex, endIndex + endMarker.Length))

    let private insertionIndex (content:string) =
        let projectEnd = content.LastIndexOf("</Project>", StringComparison.Ordinal)
        if projectEnd < 0 then Error "The file is not an MSBuild project."
        else
            let firstItemGroup = content.IndexOf("<ItemGroup", StringComparison.Ordinal)
            if firstItemGroup < 0 then Ok projectEnd
            else
                let previousNewline = content.LastIndexOf('\n', firstItemGroup)
                let lineStart = if previousNewline < 0 then 0 else previousNewline + 1
                let beforeItemGroup = content.Substring(lineStart, firstItemGroup - lineStart)
                Ok (if String.IsNullOrWhiteSpace beforeItemGroup then lineStart else firstItemGroup)

    let private withoutBlock (content:string) startIndex endIndex =
        let removalEnd =
            if endIndex < content.Length && content[endIndex] = '\n' then endIndex + 1
            else endIndex
        content.Remove(startIndex, removalEnd - startIndex)

    let private writeProjectBlock projectPath content block =
        match insertionIndex content with
        | Error _ -> Error $"'{projectPath}' is not an MSBuild project."
        | Ok index ->
            let separator = if index > 0 && content[index - 1] <> '\n' then "\n" else ""
            content.Insert(index, $"{separator}{block}\n") |> Text.writeAtomic projectPath
            Ok ()

    let private scriptBlock namespaceName (components:RegistryFile list) =
        let sources = components |> List.filter _.Compile |> List.map (fun item -> "\"" + relativeSourcePath item.FileName + "\"")
        String.concat "\n" [
            yield "// fve:components:start"
            yield $"#r \"nuget: FSharp.ViewEngine, {CoreVersion}\""
            if not sources.IsEmpty then
                // One #load interaction keeps internal definitions accessible across installed files.
                yield "#load " + String.concat " " sources
                yield "open " + namespaceName
            yield "// fve:components:end" ]

    let private updateScript projectPath namespaceName previousComponents nextComponents overwrite =
        let content = File.ReadAllText projectPath |> Text.normalize
        let previous = scriptBlock namespaceName (previousComponents |> Option.defaultValue [])
        let next = scriptBlock namespaceName nextComponents
        match findBlockBetween "// fve:components:start" "// fve:components:end" content with
        | Error message -> Error message
        | Ok (Some (first, last)) ->
            if content.Substring(first, last - first) <> previous && not overwrite then
                Error "The fve-managed script block was modified. Re-run with --overwrite to replace it explicitly."
            else
                content.Substring(0, first) + next + content.Substring(last) |> Text.writeAtomic projectPath
                Ok ()
        | Ok None when previousComponents.IsSome && not overwrite -> Error "The fve-managed script block is missing. Re-run with --overwrite to recreate it explicitly."
        | Ok None when Regex.IsMatch(content, "(?mi)^[ \\t]*#r[ \\t]*@?\"{1,3}nuget:[ \\t]*FSharp\\.ViewEngine(?:[ \\t]*,|[ \\t]*\")") ->
            Error "The script already references FSharp.ViewEngine outside a managed block. Remove that #r directive before initializing or recreating the block."
        | Ok None ->
            let firstLine = if content.StartsWith("#!", StringComparison.Ordinal) then (match content.IndexOf('\n') with -1 -> content.Length | index -> index + 1) else 0
            let prefix = content.Substring(0, firstLine)
            let separator = if prefix <> "" && not (prefix.EndsWith("\n", StringComparison.Ordinal)) then "\n" else ""
            prefix + separator + next + "\n" + content.Substring(firstLine) |> Text.writeAtomic projectPath
            Ok ()

    let updateProject (projectPath:string) (previousComponents:RegistryFile list) (nextComponents:RegistryFile list) overwrite =
        let content = File.ReadAllText projectPath |> Text.normalize
        match findBlockBetween startMarker endMarker content with
        | Error message -> Error message
        | Ok (Some (startIndex, endIndex)) ->
            let current = content.Substring(startIndex, endIndex - startIndex)
            if current <> projectBlock previousComponents && not overwrite then
                Error "The fve-managed project block was modified. Re-run with --overwrite to replace it explicitly."
            else
                writeProjectBlock projectPath (withoutBlock content startIndex endIndex) (projectBlock nextComponents)
        | Ok None ->
            if not previousComponents.IsEmpty then
                Error "The fve-managed project block is missing. Re-run with --overwrite to recreate it explicitly."
            else
                writeProjectBlock projectPath content (projectBlock nextComponents)

    let updateConsumer (projectPath:string) namespaceName previousComponents nextComponents overwrite =
        if String.Equals(Path.GetExtension projectPath, ".fsx", StringComparison.OrdinalIgnoreCase) then
            updateScript projectPath namespaceName (Some previousComponents) nextComponents overwrite
        else updateProject projectPath previousComponents nextComponents overwrite

    let initializeConsumer (projectPath:string) namespaceName =
        if String.Equals(Path.GetExtension projectPath, ".fsx", StringComparison.OrdinalIgnoreCase) then
            updateScript projectPath namespaceName None [] false
        else updateProject projectPath [] [] false

    let createProject (projectPath:string) (namespaceName:string) (framework:string) =
        let content =
            $"""<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>{framework}</TargetFramework>
    <RootNamespace>{namespaceName}</RootNamespace>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <FSharpCoreImplicitPackageVersion>{FSharpCoreVersion}</FSharpCoreImplicitPackageVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FSharp.ViewEngine" Version="{CoreVersion}" />
  </ItemGroup>
</Project>
"""
        Text.writeAtomic projectPath content

    let createScriptStylesheet (projectPath:string) =
        let path = Path.Combine(Path.GetDirectoryName projectPath, "input.css")
        if not (File.Exists path) then
            Text.writeAtomic path "@import \"tailwindcss\" source(none);\n@source \"./**/*.fs\";\n@source \"./**/*.fsx\";\n@source \"./Components/Assets/*.js\";\n"

    let createConsumer (projectPath:string) namespaceName framework =
        if String.Equals(Path.GetExtension projectPath, ".fsx", StringComparison.OrdinalIgnoreCase) then
            Text.writeAtomic projectPath """open System.IO
open FSharp.ViewEngine
open type Html

let asset extension = System.Uri.EscapeDataString(Path.GetFileNameWithoutExtension(__SOURCE_FILE__) + extension)

let document =
    html {
        _lang "en"
        head {
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            title { "Presentation" }
            link { _rel "stylesheet"; _href (asset ".css") }
        }
        body {
            _class "m-0 p-4 font-sans"
            h1 { _class "text-3xl font-semibold"; "Presentation" }
            script { _src (asset ".js") }
        }
    }

let output = Path.Combine(__SOURCE_DIRECTORY__, Path.ChangeExtension(__SOURCE_FILE__, ".html"))
File.WriteAllText(output, Render.toHtmlDocString document)
printfn "%s" output
"""
        else createProject projectPath namespaceName framework
