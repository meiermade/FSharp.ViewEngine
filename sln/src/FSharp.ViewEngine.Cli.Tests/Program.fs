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
            if item.Compile then
                Expect.isFalse (item.FileName.Contains('/')) "F# components have no framework directory categories"
            else
                Expect.isTrue (item.FileName.StartsWith("Assets/")) "non-compiled dependencies are explicit owned assets"
            for dependency in item.Dependencies do Expect.isLessThan order[dependency] order[item.Name] $"{dependency} precedes {item.Name}"
            match Registry.resolve registry [item.Name] with
            | Error message -> failtest message
            | Ok items -> Expect.equal items[items.Length-1].Name item.Name "requested component ends its closure"
        for retired in ["documentation"; "documentation-registry"; "page"; "section"; "compositions"; "app-shell"; "media-library"; "fixture"; "text-field"; "identity"; "theme-switcher"; "item"; "steps"] do
            match Registry.resolve registry [retired] with
            | Error _ -> ()
            | Ok _ -> failtest $"{retired} is retired and must not install a component or compatibility alias."

    testCase "independently installable controls copy only their genuine dependencies" <| fun _ ->
        for requested,expected in [
            "card",["component-builder"; "foundation"; "card"]
            "button-group",["component-builder"; "foundation"; "button-group"]
            "dropdown-menu",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "dropdown-menu"]
            "link",["component-builder"; "foundation"; "link"]
            "input",["component-builder"; "foundation"; "field-foundation"; "input"]
            "field",["component-builder"; "foundation"; "field"]
            "input-group",["component-builder"; "foundation"; "input-group"]
            "select",["component-builder"; "foundation"; "choice-selection"; "datastar-license"; "rocket-runtime"; "select"]
            "textarea",["component-builder"; "foundation"; "field-foundation"; "textarea"]
            "avatar",["component-builder"; "foundation"; "avatar"]
            "badge",["component-builder"; "foundation"; "badge"]
            "kbd",["component-builder"; "foundation"; "kbd"]
            "separator",["component-builder"; "foundation"; "separator"]
            "skeleton",["component-builder"; "foundation"; "skeleton"]
            "progress",["component-builder"; "foundation"; "progress"]
            "loading-indicator",["component-builder"; "foundation"; "loading-indicator"]
            "empty-state",["component-builder"; "foundation"; "empty-state"]
            "metric",["component-builder"; "foundation"; "metric"]
            "callout",["component-builder"; "foundation"; "callout"]
            "button",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "button-script"; "button"]
            "table",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "table-script"; "table"]
            "copy-reveal",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "button-script"; "button"; "copy-reveal"]
            "page-header",["component-builder"; "foundation"; "page-header"]
            "section-header",["component-builder"; "foundation"; "section-header"]
            "description-list",["component-builder"; "foundation"; "description-list"]
            "pagination",["component-builder"; "foundation"; "pagination"]
            "file-selection",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "file-selection"]
            "error-summary",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "error-summary"]
            "notice",["component-builder"; "foundation"; "notice"]
            "notification",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "notification"]
            "message",["component-builder"; "foundation"; "message"]
            "browser",["component-builder"; "foundation"; "browser"]
            "phone",["component-builder"; "foundation"; "phone"]
            "page-top-bar",["component-builder"; "foundation"; "page-top-bar"]
            "field-group",["component-builder"; "foundation"; "field"; "field-group"]
            "checkbox",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "checkbox"]
            "switch",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "switch"]
            "toggle-button",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "toggle-button"]
            "radio-group",["component-builder"; "foundation"; "field"; "field-group"; "datastar-license"; "rocket-runtime"; "radio-group"]
            "toggle-group",["component-builder"; "foundation"; "choice-selection"; "datastar-license"; "rocket-runtime"; "toggle-group"]
            "choice-cards",["component-builder"; "foundation"; "field"; "field-group"; "choice-cards"]
            "tag-input",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "tag-input-script"; "tag-input"]
            "dialog",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "button-script"; "button"; "native-overlay"; "dialog"]
            "drawer",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "button-script"; "button"; "native-overlay"; "drawer"]
            "code-block",["code-assets"; "component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "code-block"]
            "mermaid",["mermaid-assets"; "component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "mermaid"]
            "fsharp-api-reference",["code-assets"; "component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "code-block"; "fsharp-api-reference"]
            "side-nav",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "dropdown-menu"; "side-nav"]
            "bottom-navigation",["component-builder"; "foundation"; "bottom-navigation"]
            "chart",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "echarts-license"; "echarts-notice"; "echarts-provenance"; "echarts-runtime"; "chart-script"; "chart"]
            "day-calendar",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "calendar-rendering"; "day-calendar"]
            "week-calendar",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "calendar-rendering"; "week-calendar"]
            "month-calendar",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "calendar-rendering"; "field-foundation"; "popover"; "month-calendar"]
            "year-calendar",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "calendar-rendering"; "field-foundation"; "popover"; "month-calendar"; "year-calendar"]
            "date-picker",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "calendar-rendering"; "field-foundation"; "popover"; "month-calendar"; "date-picker"]
            "example",["code-assets"; "component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "code-block"; "example"]
            "command",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "button-script"; "button"; "native-overlay"; "command"]
            "popover",["component-builder"; "foundation"; "field-foundation"; "datastar-license"; "rocket-runtime"; "popover"]
            "floating-panel",["component-builder"; "foundation"; "field-foundation"; "datastar-license"; "rocket-runtime"; "floating-panel"]
            "tabs",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "tabs"]
            "resizable",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "resizable"]
            "tooltip",["component-builder"; "foundation"; "field-foundation"; "datastar-license"; "rocket-runtime"; "tooltip"]
            "breadcrumbs",["component-builder"; "foundation"; "datastar-license"; "rocket-runtime"; "dropdown-menu"; "button-script"; "button"; "breadcrumbs-script"; "breadcrumbs"]
            "color-mode",["color-mode"] ] do
            withTemp <| fun root ->
                initialize root
                add root [requested]
                let componentsRoot = Path.Combine(root,"Components")
                let actual = Directory.GetFiles(componentsRoot,"*",SearchOption.AllDirectories) |> Array.map (fun path -> Path.GetRelativePath(componentsRoot,path).Replace('\\','/')) |> Array.sort
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
            Expect.stringContains initialized "<None Include=\"Components/Assets/button.js\" />" "JavaScript is editable source, not an F# compile item"
            Expect.isFalse (initialized.Contains "<Compile Include=\"Components/Assets/") "assets are never compiled as F#"
            let script = File.ReadAllText(componentPath root "Assets/button.js")
            Expect.stringContains script "import { rocket } from './datastar-rocket.1.0.4.js'" "the component imports the shared pinned runtime"
            Expect.stringContains script "rocket('fve-button'" "the installed source defines the documented custom element"
            Expect.isTrue (File.Exists(componentPath root "Assets/datastar-LICENSE.txt")) "runtime provenance travels with source"
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
