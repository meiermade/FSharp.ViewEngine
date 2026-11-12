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

let runDotnet root arguments =
    let start = System.Diagnostics.ProcessStartInfo("dotnet")
    start.WorkingDirectory <- root
    for argument in arguments do start.ArgumentList.Add argument
    start.RedirectStandardOutput <- true
    start.RedirectStandardError <- true
    use child = System.Diagnostics.Process.Start start
    let stdout,stderr = child.StandardOutput.ReadToEndAsync(),child.StandardError.ReadToEndAsync()
    if not (child.WaitForExit 90000) then child.Kill true; failtest "dotnet did not finish"
    child.ExitCode,stdout.Result + stderr.Result

let tests = testList "fve" [
    testCase "help and parser expose the typed command contract" <| fun _ ->
        let code,output,error = invoke ["--help"]
        Expect.equal code 0 error
        for value in ["init <project>"; "add <components>"; "diff"; "build"] do Expect.stringContains output value "public command"
        Expect.equal (output.Split("--version").Length-1) 1 "one version option"
        let code,output,error = invoke ["add"; "--help"]
        Expect.equal code 0 error
        for value in ["--overwrite"; "--config"] do Expect.stringContains output value "source ownership options"
        let code,output,error = invoke ["build"; "--help"]
        Expect.equal code 0 error
        for value in ["--open"; "--config"] do Expect.stringContains output value "script build options"
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
        let icons = "IconNames.fs IconData.fs Icon.fs Assets/heroicons-LICENSE.txt Assets/heroicons.2.2.0.provenance.json Assets/bootstrap-icons-LICENSE.txt Assets/bootstrap-icons.1.13.1.provenance.json"
        for requested,source in [
            "card", $"{common} Card.fs"
            "input", $"{common} FieldFoundation.fs Input.fs"
            "icon", $"{common} {icons}"
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
            "slide", $"{common} Slide.fs"
            "presentation", $"{common} {runtime} {icons} Button.fs Assets/button.js Slide.fs Presentation.fs Assets/presentation.js"
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
            let helper = { Component="button"; FileName="Assets/button-helper.js"; Compile=false; Module=false; Source="export const enhanced = true;" }
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

    testCase "scripts install ordered source, execute in FSI and protect authored content" <| fun _ ->
        withTemp <| fun root ->
            let script = Path.Combine(root, "deck.fsx")
            let original = "#!/usr/bin/env -S dotnet fsi\n// My authored content\n"
            File.WriteAllText(script, original)
            let code,_,error = invoke ["init"; script; "--namespace"; "Deck"]
            Expect.equal code 0 error
            Expect.isTrue ((File.ReadAllText script).StartsWith("#!/usr/bin/env -S dotnet fsi\n")) "a shebang stays first"
            let initialized = File.ReadAllText script
            File.WriteAllText(script, original)
            let code,_,error = invoke ["add"; "presentation"; "--config"; configPath root]
            Expect.equal code 2 "even the first installation protects a removed managed block"
            Expect.stringContains error "managed script block is missing" "the repair is explicit"
            Expect.equal (File.ReadAllText script) original "a missing block does not silently reappear"
            Expect.isFalse (Directory.Exists(Path.Combine(root, "Components"))) "no partial component writes"
            File.WriteAllText(script, initialized)
            add root ["presentation"]
            let consumer = """
open FSharp.ViewEngine
open type Html
let deck = fvePresentation {
    _label "Script deck"
    fveSlide { fveSlideHeader { fveSlideTitle { "<Budget>" } } }
    fveSlide { fveSlideHeader { fveSlideTitle { "Next" } } }
}
System.IO.File.WriteAllText(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "result.html"), Render.toString deck)
"""
            File.AppendAllText(script, consumer)
            add root ["presentation"]
            Expect.stringContains (File.ReadAllText script) consumer "consumer body remains byte-for-byte intact"
            Expect.isEmpty (Directory.GetFiles(root, "*.fsproj")) "a script needs no project"
            let code,sdks = runDotnet root ["--list-sdks"]
            Expect.equal code 0 sdks
            let versions = sdks.Split('\n', StringSplitOptions.RemoveEmptyEntries) |> Array.map (fun line -> line.Split(' ')[0])
            for major in ["9."; "10."] do
                let version =
                    versions |> Array.filter (fun version -> version.StartsWith(major, StringComparison.Ordinal))
                    |> Array.tryLast |> Option.defaultWith (fun () -> failtest $"Install a .NET {major} SDK to test supported script authoring.")
                File.WriteAllText(Path.Combine(root, "global.json"), System.Text.Json.JsonSerializer.Serialize {|sdk={|version=version; rollForward="disable"|}|})
                let code,output = runDotnet root ["fsi"; "--exec"; script]
                Expect.equal code 0 $"SDK {version}: {output}"
            let html = File.ReadAllText(Path.Combine(root, "result.html"))
            Expect.stringContains html "&lt;Budget&gt;" "normal script values remain encoded"
            Expect.stringContains html "Slide 2 of 2" "actual installed components run and number correctly"
            let modified = (File.ReadAllText script).Replace("open Deck", "open Deck // customized")
            File.WriteAllText(script, modified)
            let code,_,error = invoke ["add"; "card"; "--config"; configPath root]
            Expect.equal code 2 "managed script edits fail closed"
            Expect.stringContains error "managed script block was modified" "ownership boundary is explicit"
            Expect.equal (File.ReadAllText script) modified "failed installation preserves the script"
            Expect.isFalse (File.Exists(componentPath root "Card.fs")) "no partial source write"
            let code,_,error = invoke ["add"; "card"; "--config"; configPath root; "--overwrite"]
            Expect.equal code 0 error
            Expect.stringContains (File.ReadAllText script) consumer "explicit replacement still preserves consumer code"

    testCase "script build uses installed source, preserves inputs and restores outputs on failure" <| fun _ ->
        withTemp <| fun root ->
            File.WriteAllText(Path.Combine(root,"global.json"), """{"sdk":{"version":"10.0.100","rollForward":"latestFeature"}}""")
            let script = Path.Combine(root,"cash review.fsx")
            let code,_,error = invoke ["init"; script; "--namespace"; "Deck"]
            Expect.equal code 0 error
            add root ["presentation"]
            let modulePath = componentPath root "Assets/presentation.js"
            File.AppendAllText(modulePath,"\nwindow.__consumerBuild = true;\n")
            let inputs = [script; configPath root; Path.Combine(root,"input.css"); modulePath] |> List.map (fun path -> path,File.ReadAllText path)
            let code,output,error = invoke ["build"; "--config"; configPath root]
            Expect.equal code 0 error
            Expect.stringContains output "cash review.html" "output path is reported"
            let html,css,js = Path.ChangeExtension(script,".html"),Path.ChangeExtension(script,".css"),Path.ChangeExtension(script,".js")
            Expect.stringContains (File.ReadAllText html) "cash%20review.css" "starter references the matching URL-encoded stylesheet"
            Expect.stringContains (File.ReadAllText html) "cash%20review.js" "starter references the matching classic bundle"
            Expect.stringContains (File.ReadAllText css) "font-semibold" "authored script classes are scanned"
            Expect.stringContains (File.ReadAllText js) "__consumerBuild" "the bundle reads edited installed JavaScript, not registry source"
            for path,source in inputs do Expect.equal (File.ReadAllText path) source "build leaves all authored inputs and manifests untouched"
            ConsumerProject.writeConfiguration (configPath root) {configuration root with Modules=null}
            let code,output,error = invoke ["build"; "--config"; configPath root]
            Expect.equal code 0 error
            Expect.isFalse (output.Contains "Preparing Tailwind") "a repeated build uses the pinned tool cache"
            Expect.stringContains (File.ReadAllText js) "__consumerBuild" "older manifests resolve only installed entry modules"
            let outputs = [html;css;js] |> List.map (fun path -> path,File.ReadAllText path)
            File.AppendAllText(script,"\n(failwith \"Authored failure\" : unit)\n")
            let code,_,error = invoke ["build"; "--config"; configPath root]
            Expect.equal code 2 "script failure is not success"
            Expect.stringContains error "Authored failure" "the actual script error is retained"
            for path,source in outputs do Expect.equal (File.ReadAllText path) source "script failure retains the last successful outputs"
            File.WriteAllText(script,snd inputs[0])
            File.WriteAllText(Path.Combine(root,"input.css"), "@import \"missing-fve-build-package\";")
            let code,_,error = invoke ["build"; "--config"; configPath root]
            Expect.equal code 2 "CSS failure is not success"
            Expect.stringContains error "missing-fve-build-package" "tool diagnostics identify the failure"
            for path,source in outputs do Expect.equal (File.ReadAllText path) source "asset failure retains the last successful outputs"
            File.WriteAllText(Path.Combine(root,"input.css"),snd inputs[2])
            File.AppendAllText(modulePath,"\nexport const = ;\n")
            let code,_,error = invoke ["build"; "--config"; configPath root]
            Expect.equal code 2 "bundle failure is not success"
            Expect.stringContains error "presentation.js" "bundler diagnostics name the edited module"
            for path,source in outputs do Expect.equal (File.ReadAllText path) source "bundle failure retains successful outputs"
            File.WriteAllText(modulePath,snd inputs[3])
            File.AppendAllText(script,"\nFile.Delete output\n")
            let code,_,error = invoke ["build"; "--config"; configPath root]
            Expect.equal code 2 "a successful script exit without output cannot reuse stale HTML"
            Expect.stringContains error "No HTML output" "missing output is explicit"
            for path,source in outputs do Expect.equal (File.ReadAllText path) source "missing output retains the last successful build"
            Expect.isEmpty (Directory.GetFiles(root,".fve-*")) "temporary build artifacts are cleaned up"

    testCase "build rejects project consumers and missing inputs without changing source" <| fun _ ->
        withTemp <| fun root ->
            initialize root
            let code,_,error = invoke ["build"; "--config"; configPath root]
            Expect.equal code 2 "project builds keep their normal dotnet boundary"
            Expect.stringContains error "dotnet build" "the supported project command is explicit"
        withTemp <| fun root ->
            let script=Path.Combine(root,"deck.fsx")
            let code,_,error=invoke ["init";script]
            Expect.equal code 0 error
            File.Delete(Path.Combine(root,"input.css"))
            let source=File.ReadAllText script
            let code,_,error=invoke ["build";"--config";configPath root]
            Expect.equal code 2 "a missing authored stylesheet fails preflight"
            Expect.stringContains error "input.css" "missing input is named"
            Expect.equal (File.ReadAllText script) source "build cannot repair or replace authored inputs"

    testCase "initializing an existing Core script does not introduce a second package reference" <| fun _ ->
        withTemp <| fun root ->
            let script = Path.Combine(root, "existing.fsx")
            let original = "#r \"nuget: FSharp.ViewEngine, 2026.2.5\"\n// Existing authored deck\n"
            File.WriteAllText(script, original)
            let code,_,error = invoke ["init"; script; "--namespace"; "Deck"]
            Expect.equal code 2 "Core reference ownership must be explicit"
            Expect.stringContains error "Remove that #r directive" "the migration action is clear"
            Expect.equal (File.ReadAllText script) original "existing source is untouched"
            Expect.isFalse (File.Exists(configPath root)) "initialization creates no partial configuration"

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
