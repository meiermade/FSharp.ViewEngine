namespace FSharp.ViewEngine.Cli

open System
open System.CommandLine

[<RequireQualifiedAccess>]
module Cli =
    let private optionValue fallback (value:string) =
        if String.IsNullOrWhiteSpace value then fallback else value

    let create (registry:ComponentRegistry) (output:CommandOutput) =
        let root = RootCommand("Install and compare consumer-owned FSharp.ViewEngine component source.")

        let initCommand = Command("init", "Initialize or select a consumer-owned F# Components project.")
        let projectArgument = Argument<string>("project")
        projectArgument.Description <- "Path to the Components .fsproj file."
        let namespaceOption = Option<string>("--namespace")
        namespaceOption.Description <- "Namespace root for copied component source. Defaults to the project name."
        let frameworkOption = Option<string>("--framework")
        frameworkOption.Description <- "Target framework for a newly created project: net8.0, net9.0, or net10.0."
        initCommand.Add(projectArgument)
        initCommand.Add(namespaceOption)
        initCommand.Add(frameworkOption)
        initCommand.SetAction(fun result ->
            Commands.init registry output
                { Project = result.GetRequiredValue projectArgument
                  Namespace = result.GetValue namespaceOption |> Option.ofObj
                  Framework = result.GetValue frameworkOption |> optionValue "net8.0" })
        root.Add(initCommand)

        let addCommand = Command("add", "Add selected components and their transitive source dependencies.")
        let componentsArgument = Argument<string array>("components")
        componentsArgument.Description <- "One or more component names."
        componentsArgument.Arity <- ArgumentArity.OneOrMore
        let addConfigOption = Option<string>("--config")
        addConfigOption.Description <- "Path to fve.json. Defaults to ./fve.json."
        let overwriteOption = Option<bool>("--overwrite")
        overwriteOption.Description <- "Explicitly replace conflicting managed source and project entries."
        addCommand.Add(componentsArgument)
        addCommand.Add(addConfigOption)
        addCommand.Add(overwriteOption)
        addCommand.SetAction(fun result ->
            Commands.add registry output
                { Config = result.GetValue addConfigOption |> optionValue ConsumerProject.ConfigFileName
                  Components = result.GetRequiredValue componentsArgument |> Array.toList
                  Overwrite = result.GetValue overwriteOption })
        root.Add(addCommand)

        let diffCommand = Command("diff", "Compare installed source with this tool's embedded registry version.")
        let diffConfigOption = Option<string>("--config")
        diffConfigOption.Description <- "Path to fve.json. Defaults to ./fve.json."
        diffCommand.Add(diffConfigOption)
        diffCommand.SetAction(fun result ->
            Commands.diff registry output
                { Config = result.GetValue diffConfigOption |> optionValue ConsumerProject.ConfigFileName })
        root.Add(diffCommand)
        root

module Program =
    [<EntryPoint>]
    let main arguments =
        let registry = Registry.load ()
        let output = { Out = Console.Out; Error = Console.Error }
        Cli.create registry output |> fun command -> command.Parse(arguments).Invoke()
