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
    let command = Cli.create registry { Out=stdout; Error=stderr }
    let invocation = InvocationConfiguration(Output=stdout, Error=stderr)
    let exitCode = command.Parse(arguments |> Array.ofList).Invoke(invocation)
    exitCode,stdout.ToString(),stderr.ToString()
let withTemp action =
    let path = Path.Combine(Path.GetTempPath(), $"fve-cli-tests-{Guid.NewGuid():N}")
    Directory.CreateDirectory path |> ignore
    try action path finally Directory.Delete(path,true)
let projectPath root = Path.Combine(root,"Acme.Components.fsproj")
let configPath root = Path.Combine(root,ConsumerProject.ConfigFileName)
let componentPath root name = Path.Combine(root,ConsumerProject.ComponentsDirectory,name)
let initialize root =
    let code,_,error = invoke ["init"; "--framework"; "net8.0"; projectPath root; "--namespace"; "Acme.Components"]
    Expect.equal code 0 error
let add root names =
    let code,_,error = invoke (["add"] @ names @ ["--config"; configPath root])
    Expect.equal code 0 error

let tests = testList "fve" [
    testCase "help and parser expose the typed command contract" <| fun _ ->
        let code,output,error = invoke ["--help"]
        Expect.equal code 0 error
        for value in ["init <project>"; "add <components>"; "diff"] do Expect.stringContains output value "public command"
        Expect.equal (output.Split("--version").Length-1) 1 "one version option"
        let code,output,error = invoke ["add"; "--help"]
        Expect.equal code 0 error
        for value in ["--overwrite"; "--config"] do Expect.stringContains output value "source ownership options"
        for arguments in [["add"]; ["wat"]] do
            let code,_,_ = invoke arguments
            Expect.notEqual code 0 "missing and unknown inputs fail"
        let code,output,error = invoke ["--version"]
        Expect.equal code 0 error
        Expect.isGreaterThan (output.Trim().Length) 0 "generated version"

    testCase "components and required helpers have one namespace and dependency-valid compile order" <| fun _ ->
        let order = registry.Components |> List.mapi (fun index item -> item.Name,index) |> Map.ofList
        for item in registry.Components do
            Expect.isFalse (item.FileName.Contains('/')) "framework directory categories are not distribution boundaries"
            for dependency in item.Dependencies do Expect.isLessThan order[dependency] order[item.Name] $"{dependency} precedes {item.Name}"
            match Registry.resolve registry [item.Name] with
            | Error message -> failtest message
            | Ok items -> Expect.equal items[items.Length-1].Name item.Name "requested component ends its closure"
        for retired in ["documentation"; "documentation-registry"; "page"; "section"; "compositions"; "app-shell"; "media-library"; "fixture"; "text-field"; "identity"] do
            match Registry.resolve registry [retired] with
            | Error _ -> ()
            | Ok _ -> failtest $"{retired} must not install an application/spec framework or unrelated bundle."

    testCase "independently installable controls copy only their genuine dependencies" <| fun _ ->
        for requested,expected in [
            "card",["foundation"; "card"]
            "input",["foundation"; "field-foundation"; "input"]
            "textarea",["foundation"; "field-foundation"; "textarea"]
            "avatar",["foundation"; "avatar"]
            "copy-reveal",["foundation"; "button"; "copy-reveal"]
            "page-header",["foundation"; "page-header"]
            "section-header",["foundation"; "section-header"]
            "page-top-bar",["foundation"; "page-top-bar"]
            "field-group",["foundation"; "field-foundation"; "field"; "field-group"]
            "dialog",["foundation"; "button"; "native-overlay"; "dialog"]
            "drawer",["foundation"; "button"; "native-overlay"; "drawer"]
            "code-block",["code-assets"; "code-block"]
            "mermaid",["mermaid-assets"; "mermaid"]
            "fsharp-api-reference",["code-assets"; "code-block"; "fsharp-api-reference"] ] do
            withTemp <| fun root ->
                initialize root
                add root [requested]
                let actual = Directory.GetFiles(Path.Combine(root,"Components"),"*.fs") |> Array.map Path.GetFileName |> Array.sort
                let files = registry.Components |> List.filter (fun item -> List.contains item.Name expected) |> List.map _.FileName |> List.sort |> List.toArray
                Expect.sequenceEqual actual files $"{requested} source boundary"

    testCase "multiple components create ordered owned source without framework assets" <| fun _ ->
        withTemp <| fun root ->
            initialize root
            add root ["card"; "input"; "code-block"; "mermaid"; "phone"]
            let project = File.ReadAllText(projectPath root)
            Expect.stringContains project $"PackageReference Include=\"FSharp.ViewEngine\" Version=\"{ConsumerProject.CoreVersion}\"" "normal Core dependency"
            Expect.stringContains project $"<FSharpCoreImplicitPackageVersion>{ConsumerProject.FSharpCoreVersion}</FSharpCoreImplicitPackageVersion>" "compatible FSharp.Core on supported SDKs"
            Expect.stringContains project "<None Include=\"fve.json\" />" "owned configuration is visible in IDEs"
            Expect.isEmpty (Directory.GetFiles(root,"*.css",SearchOption.AllDirectories)) "no copied stylesheet"
            let source = File.ReadAllText(componentPath root "Card.fs")
            Expect.stringContains source "namespace Acme.Components" "configured root namespace"
            Expect.isFalse (source.Contains "FSharp.ViewEngine.Components") "original namespace is fully replaced"
            Expect.isLessThan (project.IndexOf("CodeAssets.fs")) (project.IndexOf("CodeBlock.fs")) "asset helper precedes renderer"
            Expect.isLessThan (project.IndexOf("FieldFoundation.fs")) (project.IndexOf("Input.fs")) "field helper precedes input"

    testCase "existing projects and misplaced managed blocks keep consumer source last" <| fun _ ->
        withTemp <| fun root ->
            let project = projectPath root
            File.WriteAllText(project,"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Compile Include=\"FinanceComponents.fs\" /></ItemGroup></Project>")
            File.WriteAllText(Path.Combine(root,"FinanceComponents.fs"),"namespace FinanceComponents")
            initialize root
            add root ["button"]
            let initialized = File.ReadAllText project
            Expect.isLessThan (initialized.IndexOf("Components/Button.fs")) (initialized.IndexOf("FinanceComponents.fs")) "generated definitions precede consumer"
            let startIndex = initialized.IndexOf("  <!-- fve:components:start -->")
            let endMarker = "  <!-- fve:components:end -->"
            let markerEnd = initialized.IndexOf(endMarker,startIndex)+endMarker.Length
            let removalEnd = if markerEnd<initialized.Length && initialized[markerEnd]='\n' then markerEnd+1 else markerEnd
            let block = initialized.Substring(startIndex,markerEnd-startIndex)
            let rest = initialized.Remove(startIndex,removalEnd-startIndex)
            File.WriteAllText(project,rest.Insert(rest.LastIndexOf("</Project>"),block+"\n"))
            add root ["button"]
            let relocated = File.ReadAllText project
            Expect.isLessThan (relocated.IndexOf("Components/Button.fs")) (relocated.IndexOf("FinanceComponents.fs")) "unmodified trailing block is relocated"

    testCase "modified managed blocks fail closed until explicit overwrite" <| fun _ ->
        withTemp <| fun root ->
            initialize root
            add root ["button"]
            let project = projectPath root
            let modified = File.ReadAllText(project).Replace("      <Compile Include=\"Components/Button.fs\" />","      <Compile Include=\"Components/Button.fs\" />\n      <Compile Include=\"ConsumerOwned.fs\" />")
            File.WriteAllText(project,modified)
            let code,_,error = invoke ["add"; "badge"; "--config"; configPath root]
            Expect.equal code 2 "conflict fails closed"
            Expect.stringContains error "managed project block was modified" "explicit ownership boundary"
            Expect.equal (File.ReadAllText project) modified "no partial change"
            let code,_,error = invoke ["add"; "badge"; "--overwrite"; "--config"; configPath root]
            Expect.equal code 0 error
            Expect.isFalse ((File.ReadAllText project).Contains "ConsumerOwned.fs") "explicit overwrite restores managed source"

    testCase "repeat add is idempotent and preserves edited source until overwrite" <| fun _ ->
        withTemp <| fun root ->
            initialize root
            add root ["button"]
            let project = File.ReadAllText(projectPath root)
            let button = componentPath root "Button.fs"
            let source = File.ReadAllText button
            add root ["button"]
            Expect.equal (File.ReadAllText(projectPath root)) project "project unchanged"
            Expect.equal (File.ReadAllText button) source "source unchanged"
            File.AppendAllText(button,"\n// local customization\n")
            add root ["button"]
            Expect.stringContains (File.ReadAllText button) "local customization" "ordinary add retains edits"
            let code,output,error = invoke ["diff"; "--config"; configPath root]
            Expect.equal code 1 error
            Expect.stringContains output "modified button" "diff reports changed source"
            let code,_,error = invoke ["add"; "button"; "--overwrite"; "--config"; configPath root]
            Expect.equal code 0 error
            Expect.equal (File.ReadAllText button) source "explicit replacement restores source"

    testCase "unmanaged conflicts and unknown selectors preserve initialized state" <| fun _ ->
        withTemp <| fun root ->
            initialize root
            let beforeProject = File.ReadAllText(projectPath root)
            let beforeConfig = File.ReadAllText(configPath root)
            let path = componentPath root "Button.fs"
            Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore
            File.WriteAllText(path,"namespace Local")
            let code,_,error = invoke ["add"; "button"; "--config"; configPath root]
            Expect.equal code 2 "unmanaged conflict"
            Expect.stringContains error "No files were changed" "failure is explicit"
            Expect.equal (File.ReadAllText path) "namespace Local" "consumer source preserved"
            let code,_,error = invoke ["add"; "unknown"; "--config"; configPath root]
            Expect.equal code 2 "unknown selector"
            Expect.stringContains error "Unknown component" "selector failure is clear"
            Expect.equal (File.ReadAllText(projectPath root)) beforeProject "project unchanged"
            Expect.equal (File.ReadAllText(configPath root)) beforeConfig "configuration unchanged"
]

[<EntryPoint>]
let main arguments = runTestsWithCLIArgs [] arguments tests
