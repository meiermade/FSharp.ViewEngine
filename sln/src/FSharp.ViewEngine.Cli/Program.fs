namespace FSharp.ViewEngine.Cli

open System
open System.CommandLine

[<RequireQualifiedAccess>]
module Cli =
    let private optionValue fallback (value:string) =
        if String.IsNullOrWhiteSpace value then fallback else value

    let create (registry:ComponentRegistry) (output:CommandOutput) =
        let root = RootCommand("Install editable FSharp.ViewEngine components and build standalone HTML scripts.")

        let initCommand = Command("init", "Initialize or select a consumer-owned F# Components project or script.")
        let projectArgument = Argument<string>("project")
        projectArgument.Description <- "Path to the Components .fsproj or standalone .fsx file."
        let namespaceOption = Option<string>("--namespace")
        namespaceOption.Description <- "Namespace root for copied component source. Defaults to the project name."
        let frameworkOption = Option<string>("--framework")
        frameworkOption.Description <- "Target framework for a newly created project: net8.0, net9.0, net10.0, or net11.0 (default). Existing projects keep their target framework."
        let frontendRoot = Option<string>("--frontend-root")
        frontendRoot.Description <- "Web asset root relative to fve.json. Defaults to the project directory."
        initCommand.Add(frontendRoot)
        initCommand.Add(projectArgument)
        initCommand.Add(namespaceOption)
        initCommand.Add(frameworkOption)
        initCommand.SetAction(fun result ->
            Commands.init registry output
                { Project = result.GetRequiredValue projectArgument
                  Namespace = result.GetValue namespaceOption |> Option.ofObj
                  Framework = result.GetValue frameworkOption |> optionValue "net11.0"
                  FrontendRoot = result.GetValue frontendRoot |> Option.ofObj })
        root.Add(initCommand)

        let configOption () =
            let option = Option<string>("--config")
            option.Description <- "Path to fve.json. Defaults to ./fve.json."
            option

        let addCommand = Command("add", "Install editable components and their dependencies.")
        let names = Argument<string array>("names")
        names.Arity <- ArgumentArity.OneOrMore
        let config = configOption ()
        let overwrite = Option<bool>("--overwrite")
        overwrite.Description <- "Explicitly replace conflicting managed source and project entries."
        addCommand.Add(names)
        addCommand.Add(config)
        addCommand.Add(overwrite)
        addCommand.SetAction(fun result ->
            Commands.add registry output
                { Config = result.GetValue config |> optionValue ConsumerProject.ConfigFileName
                  Components = result.GetRequiredValue names |> Array.toList
                  Overwrite = result.GetValue overwrite })
        root.Add(addCommand)

        let diffCommand = Command("diff", "Compare installed source with this tool's embedded registry version.")
        let diffConfigOption = Option<string>("--config")
        diffConfigOption.Description <- "Path to fve.json. Defaults to ./fve.json."
        diffCommand.Add(diffConfigOption)
        diffCommand.SetAction(fun result ->
            Commands.diff registry output
                { Config = result.GetValue diffConfigOption |> optionValue ConsumerProject.ConfigFileName })
        root.Add(diffCommand)

        let run = Command("run", "Render a standalone F# script with its CSS and JavaScript.")
        let script = Argument<string>("script")
        let runConfig = configOption ()
        run.Add(script)
        run.Add(runConfig)
        run.SetAction(fun result ->
            Commands.run output
                { Script = result.GetRequiredValue script
                  Config = result.GetValue runConfig |> optionValue (ConsumerProject.configPath (IO.Path.GetFullPath(result.GetRequiredValue script))) })
        root.Add(run)
        for name, watch in ["compile", false; "watch", true] do
            let command = Command(name, if watch then "Rebuild CSS and JavaScript as sources change." else "Compile CSS and JavaScript for a web host.")
            let config = configOption ()
            command.Add(config)
            command.SetAction(fun result -> Commands.compile output (result.GetValue config |> optionValue ConsumerProject.ConfigFileName) watch)
            root.Add(command)
        root

module Program =
    [<EntryPoint>]
    let main arguments =
        let registry = Registry.load ()
        let output = { Out = Console.Out; Error = Console.Error }
        Cli.create registry output |> fun command -> command.Parse(arguments).Invoke()
