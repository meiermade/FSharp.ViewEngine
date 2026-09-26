namespace FSharp.ViewEngine.Cli

open System
open System.IO
open System.Reflection
open System.Text.Json

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
    let relativeSourcePath (fileName:string) = $"{ComponentsDirectory}/{fileName}"
    let projectBlock (components:RegistryComponent list) =
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

    let private findBlock (content:string) =
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

    let updateProject (projectPath:string) (previousComponents:RegistryComponent list) (nextComponents:RegistryComponent list) overwrite =
        let content = File.ReadAllText projectPath |> Text.normalize
        let previous = projectBlock previousComponents
        let next = projectBlock nextComponents
        match findBlock content with
        | Error message -> Error message
        | Ok (Some (startIndex, endIndex)) ->
            let current = content.Substring(startIndex, endIndex - startIndex)
            if current <> previous && not overwrite then
                Error "The fve-managed project block was modified. Re-run with --overwrite to replace it explicitly."
            else
                writeProjectBlock projectPath (withoutBlock content startIndex endIndex) next
        | Ok None ->
            if not previousComponents.IsEmpty then
                Error "The fve-managed project block is missing. Re-run with --overwrite to recreate it explicitly."
            else
                writeProjectBlock projectPath content next

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
