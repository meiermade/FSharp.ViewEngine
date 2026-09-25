namespace FSharp.ViewEngine.Cli

open System
open System.IO

[<NoEquality; NoComparison>]
type CommandOutput =
    { Out:TextWriter
      Error:TextWriter }

[<NoEquality; NoComparison>]
type InitRequest =
    { Project:string
      Namespace:string option
      Framework:string }

[<NoEquality; NoComparison>]
type AddRequest =
    { Config:string
      Components:string list
      Overwrite:bool }

[<NoEquality; NoComparison>]
type DiffRequest = { Config:string }

[<RequireQualifiedAccess>]
module Commands =
    let private fail (output:CommandOutput) (message:string) =
        output.Error.WriteLine($"error: {message}")
        2

    let private defaultNamespace (projectPath:string) =
        Path.GetFileNameWithoutExtension(projectPath).Replace('-', '_').Replace(' ', '_')

    let private framework (value:string) =
        match value with
        | "net8.0" | "net9.0" | "net10.0" -> Ok value
        | _ -> Error "--framework must be net8.0, net9.0, or net10.0."

    let private componentsForNames (registry:ComponentRegistry) (names:seq<string>) =
        let selected = names |> Set.ofSeq
        registry.Components |> List.filter (fun item -> Set.contains item.Name selected)

    let init (registry:ComponentRegistry) (output:CommandOutput) (request:InitRequest) =
        match Validation.projectPath request.Project, framework request.Framework with
        | Error message, _ | _, Error message -> fail output message
        | Ok projectPath, Ok targetFramework ->
            let namespaceName = request.Namespace |> Option.defaultWith (fun () -> defaultNamespace projectPath)
            match Validation.namespaceName namespaceName with
            | Error message -> fail output message
            | Ok validNamespace ->
                let root = Path.GetDirectoryName projectPath
                let configPath = ConsumerProject.configPath projectPath
                if File.Exists configPath then
                    match ConsumerProject.readConfiguration configPath with
                    | Error message -> fail output message
                    | Ok configuration when
                        String.Equals(Path.GetFullPath(Path.Combine(root, configuration.Project)), projectPath, StringComparison.Ordinal)
                        && String.Equals(configuration.Namespace, validNamespace, StringComparison.Ordinal) ->
                        output.Out.WriteLine($"Already initialized {configPath}")
                        0
                    | Ok _ -> fail output $"'{configPath}' already selects a different project or namespace."
                else
                    let stylesPath = ConsumerProject.stylesPath root
                    if File.Exists stylesPath && Text.checksum (File.ReadAllText stylesPath) <> Text.checksum registry.Styles then
                        fail output $"'{stylesPath}' already exists with different content."
                    else
                        let projectExisted = File.Exists projectPath
                        if not projectExisted then ConsumerProject.createProject projectPath validNamespace targetFramework
                        match ConsumerProject.updateProject projectPath [] [] false with
                        | Error message ->
                            if not projectExisted && File.Exists projectPath then File.Delete projectPath
                            fail output message
                        | Ok () ->
                            if not (File.Exists stylesPath) then Text.writeAtomic stylesPath registry.Styles
                            let styles =
                                { Component = "$styles"
                                  Path = ConsumerProject.relativeStylesPath
                                  RegistryVersion = registry.Version
                                  RegistryChecksum = Text.checksum registry.Styles }
                            { SchemaVersion = 1
                              RegistryVersion = registry.Version
                              Project = Path.GetFileName projectPath
                              Namespace = validNamespace
                              Components = [||]
                              Files = [| styles |] }
                            |> ConsumerProject.writeConfiguration configPath
                            output.Out.WriteLine($"Initialized {projectPath}")
                            output.Out.WriteLine($"Tailwind source: {ConsumerProject.ComponentsDirectory}/**/*.fs")
                            0

    let add (registry:ComponentRegistry) (output:CommandOutput) (request:AddRequest) =
        let configPath = Path.GetFullPath request.Config
        match ConsumerProject.readConfiguration configPath with
        | Error message -> fail output message
        | Ok configuration when request.Components.IsEmpty -> fail output "At least one component is required."
        | Ok configuration ->
            let requested = configuration.Components |> Array.toList |> List.append request.Components
            match Registry.resolve registry requested with
            | Error message -> fail output message
            | Ok selected ->
                let root = Path.GetDirectoryName configPath
                let projectPath = Path.GetFullPath(Path.Combine(root, configuration.Project))
                if not (File.Exists projectPath) then fail output $"Project '{projectPath}' was not found."
                else
                    let previousComponents = componentsForNames registry configuration.Components
                    let installed = configuration.Files |> Seq.map (fun file -> file.Component, file) |> Map.ofSeq
                    let mutable conflicts = []
                    let planned =
                        selected
                        |> List.map (fun item ->
                            let path = ConsumerProject.sourcePath root item.FileName
                            let source = Registry.sourceFor configuration.Namespace item
                            let checksum = Text.checksum source
                            let existing = Map.tryFind item.Name installed
                            let shouldWrite =
                                match existing, File.Exists path with
                                | Some _, true -> request.Overwrite
                                | Some _, false ->
                                    if not request.Overwrite then conflicts <- $"'{path}' is missing." :: conflicts
                                    request.Overwrite
                                | None, true ->
                                    if Text.checksum (File.ReadAllText path) = checksum then false
                                    elif request.Overwrite then true
                                    else
                                        conflicts <- $"'{path}' already exists with different content." :: conflicts
                                        false
                                | None, false -> true
                            item, path, source, checksum, existing, shouldWrite)
                    if not conflicts.IsEmpty then
                        conflicts |> List.rev |> List.iter (fun conflict -> output.Error.WriteLine($"conflict: {conflict}"))
                        output.Error.WriteLine("No files were changed. Re-run with --overwrite only if replacing those files is intended.")
                        2
                    else
                        match ConsumerProject.updateProject projectPath previousComponents selected request.Overwrite with
                        | Error message -> fail output message
                        | Ok () ->
                            for (_, path, source, _, _, shouldWrite) in planned do
                                if shouldWrite then Text.writeAtomic path source
                            let componentFiles =
                                planned
                                |> List.map (fun (item, _, _, checksum, existing, shouldWrite) ->
                                    match existing with
                                    | Some file when not shouldWrite -> file
                                    | _ ->
                                        { Component = item.Name
                                          Path = ConsumerProject.relativeSourcePath item.FileName
                                          RegistryVersion = registry.Version
                                          RegistryChecksum = checksum })
                            let nonComponentFiles = configuration.Files |> Array.filter (fun file -> file.Component.StartsWith("$", StringComparison.Ordinal))
                            { configuration with
                                RegistryVersion = registry.Version
                                Components = selected |> List.map _.Name |> List.toArray
                                Files = Array.append nonComponentFiles (List.toArray componentFiles) }
                            |> ConsumerProject.writeConfiguration configPath
                            for item in selected do
                                if not (Array.contains item.Name configuration.Components) then
                                    output.Out.WriteLine($"Added {item.Name}")
                            if selected |> List.forall (fun item -> Array.contains item.Name configuration.Components) then
                                output.Out.WriteLine("All selected components are already installed.")
                            0

    let diff (registry:ComponentRegistry) (output:CommandOutput) (request:DiffRequest) =
        let configPath = Path.GetFullPath request.Config
        match ConsumerProject.readConfiguration configPath with
        | Error message -> fail output message
        | Ok configuration ->
            let root = Path.GetDirectoryName configPath
            let current = registry.Components |> Seq.map (fun item -> item.Name, item) |> Map.ofSeq
            let mutable different = configuration.RegistryVersion <> registry.Version
            if different then output.Out.WriteLine($"registry {configuration.RegistryVersion} -> {registry.Version}")
            for file in configuration.Files do
                let path = Path.Combine(root, file.Path)
                let expected =
                    if file.Component = "$styles" then Some registry.Styles
                    else Map.tryFind file.Component current |> Option.map (Registry.sourceFor configuration.Namespace)
                match expected, File.Exists path with
                | None, _ ->
                    different <- true
                    output.Out.WriteLine($"unavailable {file.Component} {file.Path}")
                | Some _, false ->
                    different <- true
                    output.Out.WriteLine($"missing {file.Component} {file.Path}")
                | Some source, true when Text.checksum (File.ReadAllText path) <> Text.checksum source ->
                    different <- true
                    output.Out.WriteLine($"modified {file.Component} {file.Path}")
                | Some _, true -> output.Out.WriteLine($"unchanged {file.Component} {file.Path}")
            if different then 1 else 0
