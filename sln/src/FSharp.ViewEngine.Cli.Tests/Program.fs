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
let configuration root =
    match ConsumerProject.readConfiguration (configPath root) with
    | Ok value -> value
    | Error error -> failtest error
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

    testCase "components own ordered files and resolve only public selectors" <| fun _ ->
        for item in registry.Components do
            Expect.isNonEmpty item.Files "every owner has source"
            for file in item.Files do
                Expect.equal file.Component item.Name "files belong to their logical component"
                if file.Compile then Expect.isFalse (file.FileName.Contains('/')) "flat F# source"
                else Expect.isTrue (file.FileName.StartsWith("Assets/")) "assets have owned paths"
            if not item.Internal then
                match Registry.resolve registry [item.Name] with
                | Error message -> failtest message
                | Ok items ->
                    Expect.equal items[items.Length-1].Name item.Name "requested component ends its closure"
                    let order = items |> List.mapi (fun index item -> item.Name,index) |> Map.ofList
                    for resolved in items do
                        for dependency in resolved.Dependencies do
                            Expect.isLessThan order[dependency] order[resolved.Name] "dependencies compile first"
                    withTemp <| fun root ->
                        initialize root
                        add root [item.Name]
                        Expect.sequenceEqual (configuration root).Components [item.Name] "only selected components are exposed"
                        let installed = (configuration root).Files
                        Expect.equal installed.Length (Registry.files items).Length "every required file is tracked separately"
                        for file in installed do
                            Expect.equal (Text.checksum (File.ReadAllText(Path.Combine(root,file.Path)))) file.RegistryChecksum "each copied file has its own checksum"
        for retired in ["documentation"; "documentation-registry"; "page"; "compositions"; "app-shell"; "media-library"; "fixture"; "text-field"; "identity"; "theme-switcher"; "item"; "steps"; "button-script"; "code-block-script"; "rocket-runtime"; "native-control"] do
            match Registry.resolve registry [retired] with
            | Error _ -> ()
            | Ok _ -> failtest $"{retired} must not be an installable selector or alias."

    testCase "independent controls install complete narrow file boundaries" <| fun _ ->
        let common = "ComponentBuilder.fs Foundation.fs"
        let runtime = "Assets/datastar-LICENSE.txt Assets/datastar-rocket.1.0.4.js"
        for requested,source in [
            "card", $"{common} Card.fs"
            "input", $"{common} FieldFoundation.fs Input.fs"
            "icon", $"{common} IconNames.fs IconData.fs Icon.fs Assets/heroicons-LICENSE.txt Assets/heroicons.2.2.0.provenance.json Assets/bootstrap-icons-LICENSE.txt Assets/bootstrap-icons.1.13.1.provenance.json"
            "button", $"{common} {runtime} Button.fs Assets/button.js"
            "select", $"{common} {runtime} ChoiceSelection.fs Select.fs Assets/select.js"
            "button-group", $"{common} {runtime} ButtonGroup.fs Assets/scroll-region.js"
            "checkbox", $"{common} {runtime} Checkbox.fs Assets/checkbox.js Assets/native-control.js"
            "radio-group", $"{common} {runtime} Field.fs FieldGroup.fs RadioGroup.fs Assets/radio.js Assets/native-control.js"
            "file-selection", $"{common} {runtime} FileSelection.fs Assets/file-selection.js Assets/native-control.js"
            "code-block", $"{common} {runtime} CodeAssets.fs CodeBlock.fs Assets/code-block.js"
            "mermaid", $"{common} {runtime} MermaidAssets.fs Mermaid.fs Assets/mermaid.js"
            "chart", $"{common} {runtime} Chart.fs Assets/chart.js Assets/echarts.6.1.0.esm.min.js Assets/echarts-LICENSE.txt Assets/echarts-NOTICE.txt Assets/echarts.6.1.0.provenance.json"
            "command", $"{common} {runtime} Button.fs Assets/button.js NativeOverlay.fs Assets/native-overlay.js Dialog.fs Assets/dialog.js Command.fs Assets/command.js"
            "section", $"{common} SectionHeader.fs Section.fs"
            "disclosure", $"{common} Disclosure.fs"
            "time-picker", $"{common} {runtime} FieldFoundation.fs Input.fs InputGroup.fs Popover.fs Assets/popover.js TimePicker.fs Assets/time-picker.js"
            "day-calendar", $"{common} {runtime} FieldFoundation.fs Input.fs InputGroup.fs TimePicker.fs Assets/time-picker.js Popover.fs Assets/popover.js ChoiceSelection.fs Select.fs Assets/select.js CalendarRendering.fs Assets/scroll-region.js Assets/calendar-range.js DayCalendar.fs"
            "date-picker", $"{common} {runtime} CalendarRendering.fs Assets/scroll-region.js Assets/calendar-range.js FieldFoundation.fs Input.fs InputGroup.fs TimePicker.fs Assets/time-picker.js Popover.fs Assets/popover.js ChoiceSelection.fs Select.fs Assets/select.js MonthCalendar.fs Assets/calendar-selection.js DatePicker.fs Assets/date-picker.js"
            "color-mode", "ColorMode.fs" ] do
            withTemp <| fun root ->
                initialize root
                add root [requested]
                let componentsRoot = Path.Combine(root,"Components")
                let actual = Directory.GetFiles(componentsRoot,"*",SearchOption.AllDirectories) |> Array.map (fun path -> Path.GetRelativePath(componentsRoot,path).Replace('\\','/')) |> Array.sort
                let expected = source.Split(' ',StringSplitOptions.RemoveEmptyEntries) |> Array.sort
                Expect.sequenceEqual actual expected $"{requested} file boundary"

    testCase "multiple components create ordered owned source without framework assets" <| fun _ ->
        withTemp <| fun root ->
            initialize root
            add root ["card"; "input"; "code-block"; "mermaid"; "phone"]
            let project = File.ReadAllText(projectPath root)
            Expect.stringContains project $"PackageReference Include=\"FSharp.ViewEngine\" Version=\"{ConsumerProject.CoreVersion}\"" "normal Core dependency"
            Expect.stringContains project $"<FSharpCoreImplicitPackageVersion>{ConsumerProject.FSharpCoreVersion}</FSharpCoreImplicitPackageVersion>" "compatible FSharp.Core"
            Expect.stringContains project "<None Include=\"fve.json\" />" "configuration visible in IDEs"
            Expect.isEmpty (Directory.GetFiles(root,"*.css",SearchOption.AllDirectories)) "no copied stylesheet"
            let source = File.ReadAllText(componentPath root "Card.fs")
            Expect.stringContains source "namespace Acme.Components" "configured namespace"
            Expect.isFalse (source.Contains "FSharp.ViewEngine.Components") "original namespace replaced"
            Expect.isLessThan (project.IndexOf("CodeAssets.fs")) (project.IndexOf("CodeBlock.fs")) "companion F# helper precedes renderer"
            Expect.isLessThan (project.IndexOf("FieldFoundation.fs")) (project.IndexOf("Input.fs")) "dependency precedes input"

    testCase "existing projects and misplaced managed blocks keep consumer source last" <| fun _ ->
        withTemp <| fun root ->
            let project = projectPath root
            File.WriteAllText(project,"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Compile Include=\"FinanceComponents.fs\" /></ItemGroup></Project>")
            File.WriteAllText(Path.Combine(root,"FinanceComponents.fs"),"namespace FinanceComponents")
            initialize root
            add root ["button"]
            let initialized = File.ReadAllText project
            Expect.stringContains initialized "<None Include=\"Components/Assets/button.js\" />" "editable JavaScript"
            Expect.isFalse (initialized.Contains "<Compile Include=\"Components/Assets/") "assets are not compiled"
            let script = File.ReadAllText(componentPath root "Assets/button.js")
            Expect.stringContains script "import { rocket } from './datastar-rocket.1.0.4.js'" "shared pinned runtime"
            Expect.stringContains script "rocket('fve-button'" "owned custom element"
            Expect.isTrue (File.Exists(componentPath root "Assets/datastar-LICENSE.txt")) "license accompanies runtime"
            Expect.isLessThan (initialized.IndexOf("Components/Button.fs")) (initialized.IndexOf("FinanceComponents.fs")) "definitions precede consumer"
            let startIndex = initialized.IndexOf("  <!-- fve:components:start -->")
            let endMarker = "  <!-- fve:components:end -->"
            let markerEnd = initialized.IndexOf(endMarker,startIndex)+endMarker.Length
            let removalEnd = if markerEnd<initialized.Length && initialized[markerEnd]='\n' then markerEnd+1 else markerEnd
            let block = initialized.Substring(startIndex,markerEnd-startIndex)
            let rest = initialized.Remove(startIndex,removalEnd-startIndex)
            File.WriteAllText(project,rest.Insert(rest.LastIndexOf("</Project>"),block+"\n"))
            add root ["button"]
            Expect.isLessThan ((File.ReadAllText project).IndexOf("Components/Button.fs")) ((File.ReadAllText project).IndexOf("FinanceComponents.fs")) "unmodified block relocated"

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

    testCase "repeat add preserves independently edited files and upgrades file-based manifests" <| fun _ ->
        withTemp <| fun root ->
            initialize root
            add root ["button"]
            let project = File.ReadAllText(projectPath root)
            let before = configuration root
            let owned = before.Files |> Array.filter (fun file -> file.Component = "button")
            Expect.equal owned.Length 2 "F# and JavaScript share one owner"
            let originals = owned |> Array.map (fun file -> file.Path,File.ReadAllText(Path.Combine(root,file.Path)))
            for path,_ in originals do File.AppendAllText(Path.Combine(root,path),"\n// local customization\n")
            let legacyFiles = before.Files |> Array.map (fun file ->
                let owner = match Path.GetFileName file.Path with "button.js" -> "button-script" | "datastar-LICENSE.txt" -> "datastar-license" | _ -> file.Component
                { file with Component = owner })
            { before with Components = legacyFiles |> Array.map _.Component |> Array.distinct; Files = legacyFiles }
            |> ConsumerProject.writeConfiguration (configPath root)
            add root ["button"]
            Expect.equal (File.ReadAllText(projectPath root)) project "project remains unchanged"
            let migrated = configuration root
            Expect.sequenceEqual migrated.Components ["button"] "legacy file selectors become one public component"
            for file in owned do
                let current = migrated.Files |> Array.find (fun current -> current.Path = file.Path)
                Expect.equal current.RegistryChecksum file.RegistryChecksum "independent baseline retained"
                Expect.stringContains (File.ReadAllText(Path.Combine(root,file.Path))) "local customization" "neither file is overwritten"
            let code,output,error = invoke ["diff"; "--config"; configPath root]
            Expect.equal code 1 error
            for path,_ in originals do Expect.stringContains output $"modified button {path}" "each edited file reported"
            let code,_,error = invoke ["add"; "button"; "--overwrite"; "--config"; configPath root]
            Expect.equal code 0 error
            for path,source in originals do Expect.equal (File.ReadAllText(Path.Combine(root,path))) source "explicit replacement restores each file"
            let config = File.ReadAllText(configPath root)
            add root ["button"]
            Expect.equal (File.ReadAllText(configPath root)) config "repeat add is idempotent"

    testCase "new companion files are reported and installed without replacing customized files" <| fun _ ->
        withTemp <| fun root ->
            initialize root
            add root ["button"]
            let helper = { Component="button"; FileName="Assets/button-helper.js"; Compile=false; Source="export const enhanced = true;" }
            let next = { registry with Components = registry.Components |> List.map (fun item -> if item.Name = "button" then { item with Files = item.Files @ [helper] } else item) }
            File.AppendAllText(componentPath root "Assets/button.js","\n// keep this\n")
            use stdout = new StringWriter()
            use stderr = new StringWriter()
            let output = { Out=stdout; Error=stderr }
            Expect.equal (Commands.diff next output { Config=configPath root }) 1 "new owned file creates drift"
            Expect.stringContains (stdout.ToString()) "untracked button Components/Assets/button-helper.js" "missing companion is visible"
            Expect.equal (Commands.add next output { Config=configPath root; Components=["button"]; Overwrite=false }) 0 (stderr.ToString())
            Expect.equal (File.ReadAllText(componentPath root "Assets/button-helper.js")) helper.Source "new file installed"
            Expect.stringContains (File.ReadAllText(componentPath root "Assets/button.js")) "keep this" "customization preserved"

    testCase "unmanaged conflicts and unavailable selectors preserve initialized state" <| fun _ ->
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
            for name in ["unknown"; "button-script"; "rocket-runtime"] do
                let code,_,error = invoke ["add"; name; "--config"; configPath root]
                Expect.equal code 2 "unavailable selector"
                Expect.stringContains error "Unknown component" "selector failure is clear"
            Expect.equal (File.ReadAllText(projectPath root)) beforeProject "project unchanged"
            Expect.equal (File.ReadAllText(configPath root)) beforeConfig "configuration unchanged"
]

[<EntryPoint>]
let main arguments = runTestsWithCLIArgs [] arguments tests
