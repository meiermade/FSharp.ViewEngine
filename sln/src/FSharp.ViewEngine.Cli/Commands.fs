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

[<NoEquality; NoComparison>]
type BuildRequest = { Config:string; Open:bool }

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

    let private installedComponentNames (registry:ComponentRegistry) (configuration:Configuration) =
        let byName = registry.Components |> Seq.map (fun item -> item.Name, item) |> Map.ofSeq
        let byFile = Registry.files registry.Components |> Seq.map (fun file -> Path.GetFileName file.FileName, file.Component) |> Map.ofSeq
        // Existing manifests may track each file as a component. Recover its current owner from the installed file identity, not selector aliases.
        configuration.Components
        |> Array.toList
        |> List.collect (fun name ->
            match Map.tryFind name byName with
            | Some item -> if item.Internal then [] else [name]
            | None ->
                let owned = configuration.Files |> Array.filter (fun file -> file.Component = name)
                let owners = owned |> Array.choose (fun file -> Map.tryFind (Path.GetFileName file.Path) byFile)
                if owned.Length = 0 || owners.Length <> owned.Length then [name]
                else owners |> Array.filter (fun owner -> not byName[owner].Internal) |> Array.toList)
        |> List.distinct

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
                    let projectExisted = File.Exists projectPath
                    if not projectExisted then ConsumerProject.createConsumer projectPath validNamespace targetFramework
                    match ConsumerProject.initializeConsumer projectPath validNamespace with
                    | Error message ->
                        if not projectExisted && File.Exists projectPath then File.Delete projectPath
                        fail output message
                    | Ok () ->
                        if Path.GetExtension(projectPath).Equals(".fsx", StringComparison.OrdinalIgnoreCase) then
                            ConsumerProject.createScriptStylesheet projectPath
                        { SchemaVersion = 1
                          RegistryVersion = registry.Version
                          Project = Path.GetFileName projectPath
                          Namespace = validNamespace
                          Components = [||]
                          Files = [||]
                          Modules = [||] }
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
            let requested = installedComponentNames registry configuration @ request.Components |> List.distinct
            match Registry.resolve registry requested with
            | Error message -> fail output message
            | Ok selected ->
                let root = Path.GetDirectoryName configPath
                let projectPath = Path.GetFullPath(Path.Combine(root, configuration.Project))
                if not (File.Exists projectPath) then fail output $"Project '{projectPath}' was not found."
                else
                    let installed = configuration.Files |> Seq.map (fun file -> Path.GetFileName file.Path, file) |> Map.ofSeq
                    let previousFiles =
                        configuration.Files
                        |> Array.filter (fun file -> not (file.Component.StartsWith("$", StringComparison.Ordinal)))
                        |> Array.choose (fun file ->
                            ConsumerProject.tryFileName file.Path
                            |> Option.map (fun fileName ->
                                { Component = file.Component
                                  FileName = fileName
                                  Compile = Path.GetExtension(fileName) = ".fs"
                                  Module = false
                                  Source = "" }))
                        |> Array.toList
                    let mutable conflicts = []
                    let planned =
                        Registry.files selected
                        |> List.map (fun item ->
                            let path = ConsumerProject.sourcePath root item.FileName
                            let source = Registry.sourceFor configuration.Namespace item
                            let checksum = Text.checksum source
                            let existing = Map.tryFind (Path.GetFileName item.FileName) installed
                            let previousPath = existing |> Option.map (fun file -> Path.GetFullPath(Path.Combine(root, file.Path)))
                            let relocated = previousPath |> Option.exists (fun previous -> not (String.Equals(previous, path, StringComparison.Ordinal)))
                            let previousExists = previousPath |> Option.exists File.Exists
                            let previousUnchanged =
                                match existing, previousPath with
                                | Some file, Some previous when File.Exists previous -> Text.checksum (File.ReadAllText previous) = file.RegistryChecksum
                                | _ -> false
                            let targetExists = File.Exists path
                            let shouldWrite, removePrevious =
                                match existing, relocated, previousExists, previousUnchanged, targetExists with
                                | Some _, false, _, _, true -> request.Overwrite, false
                                | Some _, false, _, _, false ->
                                    if not request.Overwrite then conflicts <- $"'{path}' is missing." :: conflicts
                                    request.Overwrite, false
                                | Some _, true, true, false, _ when not request.Overwrite ->
                                    conflicts <- $"'{previousPath.Value}' contains consumer changes and cannot be relocated automatically." :: conflicts
                                    false, false
                                | Some _, true, true, _, true when not request.Overwrite ->
                                    conflicts <- $"'{path}' already exists while relocating '{previousPath.Value}'." :: conflicts
                                    false, false
                                | Some _, true, true, _, _ -> true, true
                                | Some _, true, false, _, _ ->
                                    if not request.Overwrite then conflicts <- $"'{previousPath.Value}' is missing." :: conflicts
                                    request.Overwrite, false
                                | None, _, _, _, true ->
                                    if Text.checksum (File.ReadAllText path) = checksum then false, false
                                    elif request.Overwrite then true, false
                                    else
                                        conflicts <- $"'{path}' already exists with different content." :: conflicts
                                        false, false
                                | None, _, _, _, false -> true, false
                            item, path, source, checksum, existing, shouldWrite, relocated, previousPath, removePrevious)
                    if not conflicts.IsEmpty then
                        conflicts |> List.rev |> List.iter (fun conflict -> output.Error.WriteLine($"conflict: {conflict}"))
                        output.Error.WriteLine("No files were changed. Re-run with --overwrite only if replacing those files is intended.")
                        2
                    else
                        match ConsumerProject.updateConsumer projectPath configuration.Namespace previousFiles (Registry.files selected) request.Overwrite with
                        | Error message -> fail output message
                        | Ok () ->
                            for (_, path, source, _, _, shouldWrite, _, previousPath, removePrevious) in planned do
                                if shouldWrite then Text.writeAtomic path source
                                if removePrevious then previousPath |> Option.iter File.Delete
                            let componentFiles =
                                planned
                                |> List.map (fun (item, _, _, checksum, existing, shouldWrite, relocated, _, _) ->
                                    match existing with
                                    | Some file when not shouldWrite && not relocated -> { file with Component = item.Component }
                                    | _ ->
                                        { Component = item.Component
                                          Path = ConsumerProject.relativeSourcePath item.FileName
                                          RegistryVersion = registry.Version
                                          RegistryChecksum = checksum })
                            let nonComponentFiles = configuration.Files |> Array.filter (fun file -> file.Component.StartsWith("$", StringComparison.Ordinal))
                            { configuration with
                                RegistryVersion = registry.Version
                                Components = List.toArray requested
                                Files = Array.append nonComponentFiles (List.toArray componentFiles)
                                Modules = Registry.files selected |> List.filter _.Module |> List.map (fun item -> ConsumerProject.relativeSourcePath item.FileName) |> List.toArray }
                            |> ConsumerProject.writeConfiguration configPath
                            for name in requested do
                                if not (Array.contains name configuration.Components) then
                                    output.Out.WriteLine($"Added {name}")
                            if requested |> List.forall (fun name -> Array.contains name configuration.Components) then
                                output.Out.WriteLine("All selected components are already installed.")
                            0

    let build (registry:ComponentRegistry) (output:CommandOutput) (request:BuildRequest) =
        try
            let configPath = Path.GetFullPath request.Config
            match ConsumerProject.readConfiguration configPath with
            | Error message -> fail output message
            | Ok configuration ->
                let root = Path.GetDirectoryName configPath
                let modules =
                    if not (isNull configuration.Modules) then configuration.Modules
                    else
                        // Pre-build manifests did not record entry modules. Use only files actually installed.
                        let known = Registry.files registry.Components |> List.filter _.Module |> List.map (fun file -> Path.GetFileName file.FileName) |> Set.ofList
                        configuration.Files |> Array.filter (fun file -> Set.contains (Path.GetFileName file.Path) known) |> Array.map _.Path
                let options =
                    { ProjectPath = Path.GetFullPath(Path.Combine(root, configuration.Project))
                      Modules = modules |> Array.map (fun path -> Path.GetFullPath(Path.Combine(root, path)))
                      Open = request.Open }
                ScriptBuild.build output.Out options
                0
        with exceptionValue -> fail output exceptionValue.Message

    let diff (registry:ComponentRegistry) (output:CommandOutput) (request:DiffRequest) =
        let configPath = Path.GetFullPath request.Config
        match ConsumerProject.readConfiguration configPath with
        | Error message -> fail output message
        | Ok configuration ->
            let root = Path.GetDirectoryName configPath
            let current = Registry.files registry.Components |> Seq.map (fun item -> Path.GetFileName item.FileName, item) |> Map.ofSeq
            let mutable different = configuration.RegistryVersion <> registry.Version
            if different then output.Out.WriteLine($"registry {configuration.RegistryVersion} -> {registry.Version}")
            match Registry.resolve registry (installedComponentNames registry configuration) with
            | Error message ->
                different <- true
                output.Out.WriteLine(message)
            | Ok selected ->
                let tracked = configuration.Files |> Seq.map (fun file -> Path.GetFileName file.Path) |> Set.ofSeq
                for file in Registry.files selected do
                    if not (Set.contains (Path.GetFileName file.FileName) tracked) then
                        different <- true
                        let path = ConsumerProject.relativeSourcePath file.FileName
                        output.Out.WriteLine($"untracked {file.Component} {path}")
            for file in configuration.Files do
                let path = Path.Combine(root, file.Path)
                let item = Map.tryFind (Path.GetFileName file.Path) current
                match item with
                | Some item when file.Path.Replace('\\', '/') <> ConsumerProject.relativeSourcePath item.FileName ->
                    different <- true
                    output.Out.WriteLine($"relocated {file.Component} {file.Path} -> {ConsumerProject.relativeSourcePath item.FileName}")
                | _ -> ()
                let expected = item |> Option.map (Registry.sourceFor configuration.Namespace)
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
