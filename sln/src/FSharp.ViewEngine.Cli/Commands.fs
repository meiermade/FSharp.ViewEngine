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
        let byName = registry.Components |> Seq.map (fun item -> item.Name, item) |> Map.ofSeq
        names |> Seq.choose (fun name -> Map.tryFind name byName) |> Seq.toList

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
                    if not projectExisted then ConsumerProject.createProject projectPath validNamespace targetFramework
                    match ConsumerProject.updateProject projectPath [] [] false with
                    | Error message ->
                        if not projectExisted && File.Exists projectPath then File.Delete projectPath
                        fail output message
                    | Ok () ->
                        { SchemaVersion = 1
                          RegistryVersion = registry.Version
                          Project = Path.GetFileName projectPath
                          Namespace = validNamespace
                          Components = [||]
                          Files = [||] }
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
                    let installed = configuration.Files |> Seq.map (fun file -> file.Component, file) |> Map.ofSeq
                    let previousComponents =
                        componentsForNames registry configuration.Components
                        |> List.map (fun item ->
                            match Map.tryFind item.Name installed |> Option.bind (fun file -> ConsumerProject.tryFileName file.Path) with
                            | Some fileName -> { item with FileName = fileName }
                            | None -> item)
                    let mutable conflicts = []
                    let planned =
                        selected
                        |> List.map (fun item ->
                            let path = ConsumerProject.sourcePath root item.FileName
                            let source = Registry.sourceFor configuration.Namespace item
                            let checksum = Text.checksum source
                            let existing = Map.tryFind item.Name installed
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
                        match ConsumerProject.updateProject projectPath previousComponents selected request.Overwrite with
                        | Error message -> fail output message
                        | Ok () ->
                            for (_, path, source, _, _, shouldWrite, _, previousPath, removePrevious) in planned do
                                if shouldWrite then Text.writeAtomic path source
                                if removePrevious then previousPath |> Option.iter File.Delete
                            let componentFiles =
                                planned
                                |> List.map (fun (item, _, _, checksum, existing, shouldWrite, relocated, _, _) ->
                                    match existing with
                                    | Some file when not shouldWrite && not relocated -> file
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
                let item = Map.tryFind file.Component current
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
