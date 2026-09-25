module FSharp.ViewEngine.Cli.Tests

open System
open System.CommandLine
open System.IO
open Expecto
open FSharp.ViewEngine.Cli

let registry = Registry.load ()

let invoke arguments =
    use stdout = new StringWriter()
    use stderr = new StringWriter()
    let output = { Out = stdout; Error = stderr }
    let command = Cli.create registry output
    let invocation = InvocationConfiguration()
    invocation.Output <- stdout
    invocation.Error <- stderr
    let exitCode = command.Parse(arguments |> Array.ofList).Invoke(invocation)
    exitCode, stdout.ToString(), stderr.ToString()

let withTemp action =
    let path = Path.Combine(Path.GetTempPath(), $"fve-cli-tests-{Guid.NewGuid():N}")
    Directory.CreateDirectory path |> ignore
    try action path
    finally Directory.Delete(path, true)

let projectPath root = Path.Combine(root, "Acme.Components.fsproj")
let configPath root = Path.Combine(root, ConsumerProject.ConfigFileName)
let componentPath root name = Path.Combine(root, ConsumerProject.ComponentsDirectory, name)

let initialize root =
    let exitCode, _, error =
        invoke [ "init"; "--framework"; "net8.0"; projectPath root; "--namespace"; "Acme.Components" ]
    Expect.equal exitCode 0 error

let tests =
    testList "fve" [
        testCase "root and subcommand help expose the typed command contract" <| fun _ ->
            let rootExit, rootOutput, rootError = invoke [ "--help" ]
            Expect.equal rootExit 0 rootError
            Expect.stringContains rootOutput "init <project>" "init is documented"
            Expect.stringContains rootOutput "add <components>" "add is documented"
            Expect.stringContains rootOutput "diff" "diff is documented"
            Expect.equal (rootOutput.Split("--version").Length - 1) 1 "version option appears once"
            let addExit, addOutput, addError = invoke [ "add"; "--help" ]
            Expect.equal addExit 0 addError
            Expect.stringContains addOutput "--overwrite" "conflict behavior is documented"
            Expect.stringContains addOutput "--config" "configuration selection is documented"

        testCase "version output is generated" <| fun _ ->
            let exitCode, output, error = invoke [ "--version" ]
            Expect.equal exitCode 0 error
            Expect.isGreaterThan (output.Trim().Length) 0 "version is printed"

        testCase "parser rejects missing and unknown input without running a command" <| fun _ ->
            let missingExit, _, missingError = invoke [ "add" ]
            Expect.notEqual missingExit 0 "missing components fail"
            Expect.stringContains missingError "Required argument missing" "parser reports the missing argument"
            let unknownExit, _, unknownError = invoke [ "wat" ]
            Expect.notEqual unknownExit 0 "unknown commands fail"
            Expect.stringContains unknownError "Unrecognized command" "parser reports the unknown command"

        testCase "registry dependencies resolve in canonical compile order" <| fun _ ->
            let order = registry.Components |> List.mapi (fun index item -> item.Name, index) |> Map.ofList
            for item in registry.Components do
                for dependency in item.Dependencies do
                    Expect.isLessThan order[dependency] order[item.Name] $"{dependency} precedes {item.Name}"
                match Registry.resolve registry [ item.Name ] with
                | Error message -> failtest message
                | Ok resolved ->
                    Expect.equal (resolved |> List.last |> _.Name) item.Name $"{item.Name} is last in its dependency closure"

        testCase "init and multiple add arguments create deterministic owned source" <| fun _ ->
            withTemp <| fun root ->
                let exitCode, output, error =
                    invoke [ "init"; "--namespace"; "Acme.Components"; projectPath root; "--framework"; "net9.0" ]
                Expect.equal exitCode 0 error
                Expect.stringContains output "Initialized" "initialization is reported"
                let addExit, addOutput, addError =
                    invoke [ "add"; "media-library"; "phone"; "documentation"; "--config"; configPath root ]
                Expect.equal addExit 0 addError
                Expect.stringContains addOutput "Added media-library" "first selected component is reported"
                Expect.stringContains addOutput "Added phone" "second selected component is reported"
                Expect.stringContains addOutput "Added documentation" "third selected component is reported"
                let project = File.ReadAllText(projectPath root)
                Expect.stringContains project $"PackageReference Include=\"FSharp.ViewEngine\" Version=\"{ConsumerProject.CoreVersion}\"" "generated projects pin the registry-compatible Core package"
                Expect.stringContains project "<None Include=\"fve.json\" />" "the CLI configuration is visible in project-oriented IDEs"
                Expect.stringContains project "<None Include=\"Components/FSharp.ViewEngine.Components.css\" />" "base styles are visible in project-oriented IDEs"
                Expect.stringContains project "<None Include=\"Components/Documentation/Documentation.tailwind.css\" />" "selected registry assets are visible in project-oriented IDEs"
                Expect.isTrue (File.Exists(ConsumerProject.stylesPath root)) "initialization copies the structural stylesheet"
                let foundation = project.IndexOf("Foundation.fs", StringComparison.Ordinal)
                let badge = project.IndexOf("Badge.fs", StringComparison.Ordinal)
                let choice = project.IndexOf("ChoiceSelection.fs", StringComparison.Ordinal)
                let textField = project.IndexOf("TextField.fs", StringComparison.Ordinal)
                let select = project.IndexOf("Select.fs", StringComparison.Ordinal)
                let media = project.IndexOf("MediaLibrary.fs", StringComparison.Ordinal)
                Expect.isTrue (foundation < badge && badge < choice && choice < textField && textField < select && select < media) "compile order follows the registry"
                let source = File.ReadAllText(componentPath root "MediaLibrary.fs")
                Expect.stringContains source "namespace Acme.Components.Application" "namespace integration is bounded and explicit"
                Expect.isFalse (source.Contains("FSharp.ViewEngine.Components", StringComparison.Ordinal)) "the original component namespace is fully replaced"
                let configuration =
                    match ConsumerProject.readConfiguration(configPath root) with
                    | Ok value -> value
                    | Error message -> failtest message
                Expect.contains (Array.toList configuration.Components) "media-library" "first selected component is recorded"
                Expect.contains (Array.toList configuration.Components) "phone" "second selected component is recorded"

        testCase "init and add are idempotent" <| fun _ ->
            withTemp <| fun root ->
                initialize root
                let firstExit, _, firstError = invoke [ "add"; "button"; "--config"; configPath root ]
                Expect.equal firstExit 0 firstError
                let beforeProject = File.ReadAllText(projectPath root)
                let beforeSource = File.ReadAllText(componentPath root "Button.fs")
                let secondExit, secondOutput, secondError = invoke [ "add"; "button"; "--config"; configPath root ]
                Expect.equal secondExit 0 secondError
                Expect.stringContains secondOutput "already installed" "idempotency is explicit"
                Expect.equal (File.ReadAllText(projectPath root)) beforeProject "project is unchanged"
                Expect.equal (File.ReadAllText(componentPath root "Button.fs")) beforeSource "source is unchanged"

        testCase "local modifications are preserved, diffed, and replaced only explicitly" <| fun _ ->
            withTemp <| fun root ->
                initialize root
                let addExit, _, addError = invoke [ "add"; "button"; "--config"; configPath root ]
                Expect.equal addExit 0 addError
                let button = componentPath root "Button.fs"
                File.AppendAllText(button, "\n// local customization\n")
                let repeatExit, _, repeatError = invoke [ "add"; "button"; "--config"; configPath root ]
                Expect.equal repeatExit 0 repeatError
                Expect.stringContains (File.ReadAllText button) "local customization" "ordinary add does not overwrite"
                let diffExit, diffOutput, diffError = invoke [ "diff"; "--config"; configPath root ]
                Expect.equal diffExit 1 diffError
                Expect.stringContains diffOutput "modified button" "diff identifies the modified file"
                let overwriteExit, _, overwriteError = invoke [ "add"; "button"; "--overwrite"; "--config"; configPath root ]
                Expect.equal overwriteExit 0 overwriteError
                Expect.isFalse ((File.ReadAllText button).Contains("local customization", StringComparison.Ordinal)) "explicit overwrite restores registry source"

        testCase "conflicting unmanaged files fail before changing the project" <| fun _ ->
            withTemp <| fun root ->
                initialize root
                let projectBefore = File.ReadAllText(projectPath root)
                let path = componentPath root "Button.fs"
                Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore
                File.WriteAllText(path, "namespace Local")
                let exitCode, _, error = invoke [ "add"; "button"; "--config"; configPath root ]
                Expect.equal exitCode 2 "conflicts use the command failure exit code"
                Expect.stringContains error "No files were changed" "failure is explicit"
                Expect.equal (File.ReadAllText(projectPath root)) projectBefore "project is not partially updated"
                Expect.equal (File.ReadAllText path) "namespace Local" "unmanaged source is preserved"

        testCase "unknown components fail without changing initialized files" <| fun _ ->
            withTemp <| fun root ->
                initialize root
                let projectBefore = File.ReadAllText(projectPath root)
                let configBefore = File.ReadAllText(configPath root)
                let exitCode, _, error = invoke [ "add"; "unknown"; "--config"; configPath root ]
                Expect.equal exitCode 2 "unknown components use the command failure exit code"
                Expect.stringContains error "Unknown component" "available component failure is clear"
                Expect.equal (File.ReadAllText(projectPath root)) projectBefore "project is unchanged"
                Expect.equal (File.ReadAllText(configPath root)) configBefore "configuration is unchanged"
    ]

[<EntryPoint>]
let main arguments = runTestsWithCLIArgs [] arguments tests
