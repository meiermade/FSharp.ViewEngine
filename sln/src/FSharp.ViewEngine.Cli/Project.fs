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

    [<Literal>]
    let StylesFileName = "FSharp.ViewEngine.Components.css"

    let CoreVersion =
        Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
        |> Seq.tryFind (fun attribute -> attribute.Key = "FSharpViewEngineCoreVersion")
        |> Option.map _.Value
        |> Option.filter (String.IsNullOrWhiteSpace >> not)
        |> Option.defaultWith (fun () -> invalidOp "The CLI package is missing its FSharp.ViewEngine Core version metadata.")

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
    let stylesPath (root:string) = Path.Combine(root, ComponentsDirectory, StylesFileName)
    let relativeStylesPath = $"{ComponentsDirectory}/{StylesFileName}"

    let projectBlock (components:RegistryComponent list) =
        let compileEntries =
            components
            |> List.filter _.Compile
            |> List.map (fun item -> $"      <Compile Include=\"{relativeSourcePath item.FileName}\" />")
        let assetEntries =
            [ $"      <None Include=\"{ConfigFileName}\" />"
              $"      <None Include=\"{relativeStylesPath}\" />"
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
                content.Remove(startIndex, endIndex - startIndex).Insert(startIndex, next)
                |> Text.writeAtomic projectPath
                Ok ()
        | Ok None ->
            if not previousComponents.IsEmpty then
                Error "The fve-managed project block is missing. Re-run with --overwrite to recreate it explicitly."
            else
                let projectEnd = content.LastIndexOf("</Project>", StringComparison.Ordinal)
                if projectEnd < 0 then Error $"'{projectPath}' is not an MSBuild project."
                else
                    let separator = if projectEnd > 0 && content[projectEnd - 1] = '\n' then "" else "\n"
                    content.Insert(projectEnd, $"{separator}{next}\n") |> Text.writeAtomic projectPath
                    Ok ()

    let createProject (projectPath:string) (namespaceName:string) (framework:string) =
        let content =
            $"""<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>{framework}</TargetFramework>
    <RootNamespace>{namespaceName}</RootNamespace>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FSharp.ViewEngine" Version="{CoreVersion}" />
  </ItemGroup>
</Project>
"""
        Text.writeAtomic projectPath content
