open System.Text.RegularExpressions
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

// dotnet run exports its SDK paths; nested consumers must select their own SDK via global.json.
for name in [ "MSBuildSDKsPath"; "MSBuildExtensionsPath" ] do
    System.Environment.SetEnvironmentVariable(name, null)

System.Environment.GetCommandLineArgs()
|> Array.tail
|> Array.toList
|> Context.FakeExecutionContext.Create false "build.fsx"
|> Context.RuntimeContext.Fake
|> Context.setExecutionContext

let inline (==>!) x y = x ==> y |> ignore

let srcDir = Path.getDirectory __SOURCE_DIRECTORY__
let slnDir = Path.getDirectory srcDir
let rootDir = Path.getDirectory slnDir
let nugetsDir = rootDir </> "nugets"
let testsDir = srcDir </> "Tests"
let docsDir = srcDir </> "Docs"
let docsTestsDir = srcDir </> "Docs.Tests"
let exampleDir = srcDir </> "Example"
let exampleServerDir = exampleDir </> "Example.Application"
let exampleSpecDir = exampleDir </> "Example.Specification"
let exampleDocsDir = exampleDir </> "Example.Documentation"
let examplePresentationDir = exampleDir </> "Example.Presentation"
let otherExampleHosts = [exampleSpecDir; exampleDocsDir; examplePresentationDir]
let exampleHosts = exampleServerDir :: otherExampleHosts
let mockCloudflareDir = srcDir </> "MockCloudflareProxy"
let cliTestsDir = srcDir </> "FSharp.ViewEngine.Cli.Tests"
let buildTestsDir = srcDir </> "Build.Tests"
let benchmarksDir = srcDir </> "Benchmarks"
let releaseRepository = Environment.environVarOrDefault "RELEASE_REPOSITORY" rootDir
let releaseMetadataPath =
    Environment.environVarOrDefault
        "RELEASE_METADATA_PATH"
        (__SOURCE_DIRECTORY__ </> "obj" </> "release-metadata.json")

let exec workDir cmd args =
    CreateProcess.fromRawCommand cmd args
    |> CreateProcess.withWorkingDirectory workDir
    |> CreateProcess.ensureExitCode
    |> Proc.start
    |> Async.AwaitTask
    |> Async.Ignore

let dotnet workdir args = exec workdir "dotnet" args

let restoreFrontendTools directory =
    exec directory "npm" ["ci"] |> Async.RunSynchronously

let frontendTool directory name =
    let extension = if Environment.isWindows then ".cmd" else ""
    directory </> "node_modules" </> ".bin" </> (name + extension)

let bundleJs directory entries options =
    exec directory (frontendTool slnDir "esbuild")
        (entries @ ["--bundle"; "--format=esm"; "--splitting"; "--minify"] @ options)

let docsFrontend = "src/Docs.Components/fve.json"
let exampleFrontend = "src/Example/Example.UI/fve.json"
let frontend command config = dotnet slnDir ["run"; "--project"; "src/FSharp.ViewEngine.Cli"; "--"; command; "--config"; config]
let docsEntries = ["js/app.js"]
let exampleAssets = Path.getDirectory exampleServerDir </> "Example.UI" </> "Components" </> "Assets"
let exampleEntries = ["js/app.js"]
let exampleVendorModules = ["datastar-rocket.*.js"]
let exampleExternals = exampleVendorModules |> List.map (fun filename -> $"--external:./{filename}")
let watchJsOptions watch = if watch then ["--watch=forever"] else []

let buildDocsJs watch =
    bundleJs docsDir docsEntries (["--outdir=wwwroot/js"; "--metafile=obj/components-assets.json"] @ watchJsOptions watch)

let copyExampleVendor directory =
    // Keep pinned third-party modules and their notices at their existing URLs.
    let outputDir = directory </> "wwwroot/js"
    Directory.ensure outputDir
    for pattern in exampleVendorModules @ ["*.txt"; "*.json"] do
        for file in System.IO.Directory.EnumerateFiles(exampleAssets, pattern) do
            System.IO.File.Copy(file, outputDir </> System.IO.Path.GetFileName(file), true)

let buildExampleJs directory watch =
    bundleJs directory exampleEntries (["--outdir=wwwroot/js"; "--entry-names=[name]"] @ exampleExternals @ watchJsOptions watch)

let publishBrowserAssets directory entries externals =
    let output = directory </> "out"
    let publicRoot = output </> "wwwroot"
    let metadata = directory </> "obj/browser-assets.json"
    bundleJs directory entries
        (["--outdir=out/wwwroot/js"; "--entry-names=[name]-[hash]"; $"--metafile={metadata}"] @ externals)
    |> Async.RunSynchronously

    use manifest = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText metadata)
    let app =
        manifest.RootElement.GetProperty("outputs").EnumerateObject()
        |> Seq.find (fun file ->
            match file.Value.TryGetProperty("entryPoint") with
            | true, entry -> entry.GetString() = "js/app.js"
            | _ -> false)
        |> fun file -> System.IO.Path.GetFileName file.Name

    let cssBytes = System.IO.File.ReadAllBytes(publicRoot </> "css/output.css")
    let cssHash = System.Security.Cryptography.SHA256.HashData cssBytes |> System.Convert.ToHexStringLower
    let css = $"output-{cssHash.Substring(0, 16)}.css"
    System.IO.File.WriteAllBytes(publicRoot </> "css" </> css, cssBytes)
    let paths = dict ["script", "/js/" + app; "stylesheet", "/css/" + css]
    System.IO.File.WriteAllText(output </> "browser-assets.json", System.Text.Json.JsonSerializer.Serialize paths)

let tailwindcss toolsDirectory workDir args =
    exec workDir (frontendTool toolsDirectory "tailwindcss") args

let packageProject (package:PackagePublishing.Package) =
    srcDir </> package.Id </> $"{package.Id}.fsproj"

let boolEnvironment name =
    match Environment.environVarOrFail name with
    | "true" -> true
    | "false" -> false
    | value -> failwith $"{name} must be true or false, found: {value}"

let releaseInputs () =
    PackagePublishing.validateInputs
        (Environment.environVarOrFail "PACKAGE_ID")
        (Environment.environVarOrFail "PACKAGE_VERSION")
        None
        (boolEnvironment "MARK_LATEST")

let optionalEnvironment name =
    Environment.environVarOrNone name
    |> Option.filter (System.String.IsNullOrWhiteSpace >> not)

let releaseSelection () =
    PackagePublishing.validateSelection
        (Environment.environVarOrFail "PACKAGE_SELECTION")
        (Environment.environVarOrFail "CORE_PACKAGE_VERSION")
        (Environment.environVarOrFail "CLI_PACKAGE_VERSION")

let selectedPackage () =
    match Environment.environVarOrFail "PACKAGE_ID" with
    | "FSharp.ViewEngine" -> PackagePublishing.Package.ViewEngine
    | "FSharp.ViewEngine.Cli" -> PackagePublishing.Package.Cli
    | packageId -> failwith $"Unsupported package: {packageId}"

let getVersion () =
    let value = Environment.environVarOrFail "PACKAGE_VERSION"
    let matched = Regex.Match(value, @"^(\d+\.\d+\.\d+(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?)$")
    if matched.Success then matched.Groups[1].Value else failwith $"invalid package version: {value}"

let corePaths = [ "sln/src/FSharp.ViewEngine" ]
let cliPaths = [ "sln/src/FSharp.ViewEngine.Cli"; "sln/src/FSharp.ViewEngine.Components" ]
let docsPaths = [ "sln/src/Docs"; "sln/src/Docs.Components"; "sln/src/Example"; "pulumi"; "sln/src/FSharp.ViewEngine.Components"; "sln/src/FSharp.ViewEngine"; "sln/src/Build"; "sln/Dockerfile"; "sln/package.json"; "sln/package-lock.json" ]

Target.create "ResolveReleaseTransaction" <| fun _ ->
    let requested = Environment.environVarOrDefault "PACKAGE_SELECTION" "auto"
    let githubRepository = Environment.environVarOrFail "GITHUB_REPOSITORY"
    let runId = Environment.environVarOrFail "GITHUB_RUN_ID"
    if Environment.environVarOrFail "GITHUB_REF" <> "refs/heads/main" then failwith "Releases must run from main."
    let planPath = Environment.environVarOrFail "RELEASE_PLAN_PATH" |> Path.getFullName
    let artifactNames = PackagePublishing.runArtifactNames githubRepository runId
    let planName = $"release-plan-{runId}"
    let bundleName = $"package-release-{runId}"
    let reuse = artifactNames.Contains planName
    let bundle = artifactNames.Contains bundleName
    if not reuse && (bundle || Environment.environVarOrDefault "GITHUB_RUN_ATTEMPT" "1" <> "1") then
        failwith "Pinned release transaction is unavailable; refusing to allocate new versions on a retry."
    let plan =
        if reuse then
            PackagePublishing.runProcess false "gh" ["run"; "download"; runId; "--repo"; githubRepository; "--name"; planName; "--dir"; System.IO.Path.GetDirectoryName planPath] |> ignore
            PackagePublishing.readPlan planPath
        else
            let commit,image,exampleImage = PackagePublishing.acceptedCandidate releaseRepository githubRepository
            PackagePublishing.runProcess false "git" ["-C"; releaseRepository; "fetch"; "origin"; "main"; "--no-tags"] |> ignore
            PackagePublishing.runProcess false "git" ["-C"; releaseRepository; "merge-base"; "--is-ancestor"; commit; "origin/main"] |> ignore
            // The selected source must contain this release workflow/automation revision.
            PackagePublishing.runProcess false "git" ["-C"; releaseRepository; "merge-base"; "--is-ancestor"; Environment.environVarOrFail "GITHUB_SHA"; commit] |> ignore
            PackagePublishing.runProcess false "git" ["-C"; releaseRepository; "checkout"; "--detach"; commit] |> ignore
            let coreState = PackagePublishing.packageStateSinceLatestTag releaseRepository "v[0-9]*" "v" corePaths
            let cliState = PackagePublishing.packageStateSinceLatestTag releaseRepository "cli/v[0-9]*" "cli/v" cliPaths
            let previousDocsCommit =
                PackagePublishing.runProcess true "pulumi" ["-C"; rootDir </> "pulumi"; "stack"; "output"; "releaseCommit"; "--stack"; "meiermade/fsharp-view-engine/prod"]
            let docsChanged =
                if not (Regex.IsMatch(previousDocsCommit, "^[a-f0-9]{40}$")) then true
                else
                    PackagePublishing.runProcess true "git" (["-C"; releaseRepository; "diff"; "--name-only"; previousDocsCommit; "HEAD"; "--"] @ docsPaths)
                    |> System.String.IsNullOrWhiteSpace |> not
            let coreVersions = if requested = "cli" || requested = "docs" then [] else PackagePublishing.publishedVersions PackagePublishing.Package.ViewEngine
            let cliVersions = if requested = "core" || requested = "docs" then [] else PackagePublishing.publishedVersions PackagePublishing.Package.Cli
            let plan =
                { PackagePublishing.planRelease requested commit image System.DateTimeOffset.UtcNow coreState cliState docsChanged coreVersions cliVersions with
                    exampleImage = exampleImage }
            PackagePublishing.writePlan planPath plan
            plan
    PackagePublishing.validatePinnedPlan requested plan.commit plan
    if System.String.IsNullOrEmpty plan.exampleImage || not (Regex.IsMatch(plan.exampleImage, "@sha256:[a-f0-9]{64}$")) then
        failwith "The pinned release requires the accepted immutable Example image."
    PackagePublishing.runProcess false "git" ["-C"; releaseRepository; "checkout"; "--detach"; plan.commit] |> ignore
    let outputPath = Environment.environVarOrFail "GITHUB_OUTPUT"
    System.IO.File.AppendAllLines(outputPath, [
        $"commit={plan.commit}"; $"image={plan.image}"; $"exampleImage={plan.exampleImage}"; $"packages={plan.packages}"
        $"coreVersion={plan.coreVersion}"; $"cliVersion={plan.cliVersion}"
        $"docsCoreVersion={plan.docsCoreVersion}"; $"docsCliVersion={plan.docsCliVersion}"
        $"promoteDocs={string plan.promoteDocs |> _.ToLowerInvariant()}"
        $"bundle={string bundle |> _.ToLowerInvariant()}"
        $"newPlan={string (not reuse) |> _.ToLowerInvariant()}" ])
    Trace.trace $"Pinned {plan.commit} / {plan.image}: {plan.packages}; Core {plan.coreVersion}; CLI {plan.cliVersion}; promote Docs {plan.promoteDocs}"

Target.create "WriteReleasePreviewSummary" <| fun _ ->
    let plan = PackagePublishing.readPlan (Environment.environVarOrFail "RELEASE_PLAN_PATH")
    for name, expected in [
        "DEPLOY_IMAGE", plan.image; "DEPLOY_EXAMPLE_IMAGE", plan.exampleImage; "RELEASE_COMMIT", plan.commit
        "CORE_PACKAGE_VERSION", plan.docsCoreVersion; "CLI_PACKAGE_VERSION", plan.docsCliVersion
        "PROMOTE_DOCS", plan.promoteDocs.ToString().ToLowerInvariant() ] do
        if Environment.environVarOrFail name <> expected then
            failwith $"Release preview {name} differs from the pinned transaction."
    use production = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(Environment.environVarOrFail "PRODUCTION_STATE_PATH"))
    let summary = PackagePublishing.releasePreviewSummary (Environment.environVarOrFail "GITHUB_SHA") plan production.RootElement
    System.IO.File.AppendAllText(Environment.environVarOrFail "GITHUB_STEP_SUMMARY", summary)

Target.create "ValidateReleaseSelection" <| fun _ ->
    let selection = releaseSelection ()
    let coreState =
        PackagePublishing.packageStateSinceLatestTag
            releaseRepository
            "v[0-9]*"
            "v"
            corePaths
    let cliState =
        PackagePublishing.packageStateSinceLatestTag
            releaseRepository
            "cli/v[0-9]*"
            "cli/v"
            cliPaths
    PackagePublishing.validateCoherence selection coreState cliState

    let selected =
        [ selection.core; selection.cli ]
        |> List.choose id
        |> List.map (fun inputs -> $"{inputs.package.Id} {inputs.version}")
        |> function | [] -> "Docs only" | values -> String.concat ", " values
    Trace.trace $"Validated release coherence: {selected}; Core {selection.coreVersion}; CLI {selection.cliVersion}"

Target.create "PrepareRelease" <| fun _ ->
    let inputs = releaseInputs ()
    let expectedRef = Environment.environVarOrDefault "GITHUB_REF" "refs/heads/main"
    if expectedRef <> "refs/heads/main" then failwith $"Releases must run from main, not {expectedRef}."

    match inputs.minimumDependency with
    | Some dependency when not (PackagePublishing.confirmPublished dependency.package.Id dependency.minimumVersion) ->
        match Environment.environVarOrNone "LOCAL_DEPENDENCY_PACKAGE_PATH" with
        | Some packagePath -> PackagePublishing.validateLocalPackage dependency.package dependency.minimumVersion packagePath
        | None -> failwith $"{dependency.package.Id} {dependency.minimumVersion} must be available from NuGet before publishing {inputs.package.Id}."
    | _ -> ()

    let metadata = Release.prepare releaseRepository releaseMetadataPath inputs.package.TagPrefix inputs.version
    Trace.trace $"Prepared {metadata.tag} for {metadata.commit}"
    Trace.trace $"Release metadata: {releaseMetadataPath}"

Target.create "ReadReleaseMetadata" <| fun _ ->
    let metadata = Release.readMetadata releaseMetadataPath
    let outputPath = Environment.environVarOrFail "GITHUB_OUTPUT"
    let previousTag = metadata.previousTag |> Option.defaultValue ""
    System.IO.File.AppendAllLines(outputPath, [
        $"tag={metadata.tag}"
        $"version={metadata.version}"
        $"commit={metadata.commit}"
        $"previousTag={previousTag}" ])

Target.create "RecordPackageChecksums" <| fun _ ->
    let package = selectedPackage ()
    let packageDirectory = Environment.environVarOrDefault "PACKAGE_DIRECTORY" nugetsDir |> Path.getFullName
    let packagePaths =
        !! $"{packageDirectory}/{package.Id}.*.nupkg"
        ++ $"{packageDirectory}/{package.Id}.*.snupkg"
        |> Seq.sort
        |> Seq.toList
    PackagePublishing.writeChecksums (packageDirectory </> "SHA256SUMS") packagePaths

Target.create "PublishPackageRelease" <| fun _ ->
    let inputs = releaseInputs ()
    let packageDirectory = Environment.environVarOrFail "PACKAGE_DIRECTORY" |> Path.getFullName
    let metadata = Release.readMetadata releaseMetadataPath
    let assets =
        PackagePublishing.expectedAssetNames inputs.package.Id inputs.version
        |> List.map (fun name -> packageDirectory </> name)
    let packagePath = packageDirectory </> $"{inputs.package.Id}.{inputs.version}.nupkg"

    PackagePublishing.verifyChecksums (packageDirectory </> "SHA256SUMS") packageDirectory
    PackagePublishing.publishOrVerify
        packagePath
        inputs.package.Id
        inputs.version
        (Environment.environVarOrFail "NUGET_API_KEY")
        (Environment.environVarOrDefault "RUNNER_TEMP" (System.IO.Path.GetTempPath()) </> "fsharp-viewengine-publish")
    PackagePublishing.waitForPublished inputs.package.Id inputs.version 60 (System.TimeSpan.FromSeconds 10.)
    PackagePublishing.ensureTag releaseRepository metadata.tag metadata.commit
    PackagePublishing.reconcileGitHubRelease
        (Environment.environVarOrFail "GITHUB_REPOSITORY")
        inputs.package.Id
        inputs.version
        metadata.tag
        metadata.previousTag
        inputs.markLatest
        assets

Target.create "VerifyPublicPackageSnapshot" <| fun _ ->
    let package = selectedPackage ()
    let version = getVersion ()
    let publishedDirectory =
        Environment.environVarOrDefault
            "PUBLISHED_PACKAGE_DIRECTORY"
            (Environment.environVarOrDefault "RUNNER_TEMP" (System.IO.Path.GetTempPath()) </> $"public-{package.Id}-{version}")
        |> Path.getFullName
    let publishedPackage =
        PackagePublishing.downloadPublishedArtifacts
            package.Id
            version
            publishedDirectory
            60
            (System.TimeSpan.FromSeconds 10.)
    PackageVerification.verify
        (fun workDir args -> dotnet workDir args |> Async.RunSynchronously)
        publishedPackage

Target.create "VerifyPublishedPackageRelease" <| fun _ ->
    let inputs = releaseInputs ()
    let packageDirectory = Environment.environVarOrFail "PACKAGE_DIRECTORY" |> Path.getFullName
    let expectedPackage = packageDirectory </> $"{inputs.package.Id}.{inputs.version}.nupkg"
    let publishedDirectory =
        Environment.environVarOrDefault
            "PUBLISHED_PACKAGE_DIRECTORY"
            (Environment.environVarOrDefault "RUNNER_TEMP" (System.IO.Path.GetTempPath()) </> $"published-{inputs.package.Id}-{inputs.version}")
        |> Path.getFullName
    let publishedPackage =
        PackagePublishing.downloadPublishedArtifacts
            inputs.package.Id
            inputs.version
            publishedDirectory
            60
            (System.TimeSpan.FromSeconds 10.)
    PackagePublishing.verifyPublishedPackage expectedPackage publishedPackage
    PackageVerification.verify
        (fun workDir args -> dotnet workDir args |> Async.RunSynchronously)
        publishedPackage

Target.create "CleanNugets" <| fun _ -> Shell.cleanDir nugetsDir

Target.create "Test" <| fun _ ->
    ["net10.0"; "net11.0"]
    |> List.iter (fun tfm ->
        dotnet testsDir ["run"; "--framework"; tfm]
        |> Async.RunSynchronously
    )

    // Release callers use --single-target, so Test owns its asset prerequisites too.
    restoreFrontendTools slnDir
    restoreFrontendTools docsDir
    buildDocsJs false |> Async.RunSynchronously
    for host in exampleHosts do
        copyExampleVendor host
        buildExampleJs host false |> Async.RunSynchronously
    dotnet docsTestsDir ["run"]
    |> Async.RunSynchronously

    dotnet cliTestsDir ["run"]
    |> Async.RunSynchronously

    dotnet buildTestsDir ["run"]
    |> Async.RunSynchronously

Target.create "Pack"  (fun _ ->
    let package = selectedPackage ()
    let version = getVersion ()
    let arguments =
        [ "pack"
          packageProject package
          "--configuration"
          "Release"
          "--output"
          nugetsDir ]

    let arguments =
        match package with
        | PackagePublishing.Package.ViewEngine -> arguments @ [ $"/p:FSharpViewEnginePackageVersion={version}" ]
        | PackagePublishing.Package.Cli ->
            let coreVersion = Environment.environVarOrDefault "CORE_PACKAGE_VERSION" "2026.8.2"
            arguments @
                [ $"/p:FSharpViewEngineCliPackageVersion={version}"
                  $"/p:FSharpViewEnginePackageVersion={coreVersion}" ]

    Trace.trace $"Packing {package.Id} {version}"
    dotnet rootDir arguments |> Async.RunSynchronously
)

Target.create "VerifyPackage" (fun _ ->
    let packages =
        match Environment.environVarOrNone "PACKAGE_PATH" with
        | Some package -> [ Path.getFullName package ]
        | None ->
            let package = selectedPackage ()
            let packageDirectory = Environment.environVarOrDefault "PACKAGE_DIRECTORY" nugetsDir
            let packages =
                !! $"{packageDirectory}/{package.Id}.*.nupkg"
                |> Seq.filter (PackagePublishing.belongsToPackage package.Id)
                |> Seq.filter (fun path -> not (path.EndsWith(".snupkg", System.StringComparison.OrdinalIgnoreCase)))
                |> Seq.sort
                |> Seq.toList

            match packages with
            | [ packagePath ] -> [ packagePath ]
            | _ ->
                let found = packages |> List.map System.IO.Path.GetFileName |> String.concat ", "
                failwith $"Expected exactly one {package.Id} package, found: {found}"

    for package in packages do
        if not (File.exists package) then failwith $"Package does not exist: {package}"

        PackageVerification.verify
            (fun workDir args -> dotnet workDir args |> Async.RunSynchronously)
            package
)

Target.create "UpdateInstalledComponents" (fun _ ->
    let selectors =
        System.Xml.Linq.XDocument.Load(srcDir </> "FSharp.ViewEngine.Components" </> "FSharp.ViewEngine.Components.Registry.props").Descendants()
        |> Seq.choose (fun element ->
            let name = element.Attribute(System.Xml.Linq.XName.Get "FveName")
            let internal' = element.Attribute(System.Xml.Linq.XName.Get "FveInternal")
            if isNull name || (not (isNull internal') && internal'.Value = "true") then None else Some name.Value)
        |> Seq.toList
    dotnet slnDir (["run"; "--project"; "src/FSharp.ViewEngine.Cli"; "--"; "add"] @ selectors @ ["--config"; "src/Docs.Components/fve.json"; "--overwrite"])
    |> Async.RunSynchronously
    dotnet slnDir ["run"; "--project"; "src/FSharp.ViewEngine.Cli"; "--"; "add"; "icon"; "button"; "field"; "chart"; "checkbox"; "input-group"; "notification"; "command"; "time-picker"; "section"; "disclosure"; "pagination"; "presentation"; "--config"; "src/Example/Example.UI/fve.json"; "--overwrite"]
    |> Async.RunSynchronously
    dotnet slnDir ["run"; "--project"; "src/FSharp.ViewEngine.Cli"; "--"; "add"; "presentation"; "--config"; "src/Example/Example.Presentation/fve.json"; "--overwrite"]
    |> Async.RunSynchronously
)

Target.create "CheckInstalledComponents" (fun _ ->
    for config in ["src/Docs.Components/fve.json"; "src/Example/Example.UI/fve.json"; "src/Example/Example.Presentation/fve.json"] do
        dotnet slnDir ["run"; "--project"; "src/FSharp.ViewEngine.Cli"; "--"; "diff"; "--config"; config]
        |> Async.RunSynchronously
)

Target.create "RestoreFrontendTools" (fun _ -> restoreFrontendTools slnDir)
Target.create "RestoreDocsFrontend" (fun _ -> restoreFrontendTools docsDir)
Target.create "RestoreExampleFrontend" (fun _ -> restoreFrontendTools exampleServerDir)

Target.create "BuildDocsJs" (fun _ -> buildDocsJs false |> Async.RunSynchronously)
let exampleCss host watch =
    let tools = if host = exampleServerDir then host else slnDir
    tailwindcss tools host ([ "--input"; "input.css"; "--output"; "wwwroot/css/output.css"; "--minify" ] @ if watch then ["--watch=always"] else [])

Target.create "BuildExampleJs" (fun _ ->
    for host in exampleHosts do
        copyExampleVendor host
        buildExampleJs host false |> Async.RunSynchronously)

Target.create "VerifyTailwind" (fun _ ->
    TailwindContracts.verify
        (fun input output ->
            tailwindcss slnDir slnDir ["--input"; input; "--output"; output; "--minify"]
            |> Async.RunSynchronously)
        (srcDir </> "FSharp.ViewEngine.Components"))

Target.create "WatchDocs" (fun _ ->
    let reviewUrl = WatchDocs.defaultUrl
    let exampleUrl = "http://127.0.0.1:5055"
    let docsUrl = "http://127.0.0.1:5056"
    let exampleSpecUrl = "http://127.0.0.1:5057"
    let exampleDocsUrl = "http://127.0.0.1:5058"
    let examplePresentationUrl = "http://127.0.0.1:5059"
    let exampleHosts =
        [ exampleServerDir, exampleUrl, false
          exampleSpecDir, exampleSpecUrl, true
          exampleDocsDir, exampleDocsUrl, false
          examplePresentationDir, examplePresentationUrl, false ]

    WatchDocs.runExclusiveWatcher "WatchDocs" reviewUrl <| fun () ->
        for directory, url, _ in exampleHosts do
            WatchDocs.waitForLoopbackUrlAvailable (System.IO.Path.GetFileName directory) url
        WatchDocs.waitForLoopbackUrlAvailable "Docs" docsUrl
        Trace.trace $"Starting the FSharp.ViewEngine Docs at {reviewUrl}"
        // Restore only after replacing the previous watcher, which may still hold these tools open.
        restoreFrontendTools docsDir
        restoreFrontendTools exampleServerDir
        restoreFrontendTools slnDir
        for directory, _, _ in exampleHosts do copyExampleVendor directory
        for host in otherExampleHosts do
            exampleCss host false |> Async.RunSynchronously
            buildExampleJs host false |> Async.RunSynchronously
        frontend "compile" docsFrontend |> Async.RunSynchronously
        frontend "compile" exampleFrontend |> Async.RunSynchronously

        let watchApp =
            CreateProcess.fromRawCommand "dotnet"
                ["watch"; "run"; "--no-launch-profile"; "--non-interactive"]
            |> CreateProcess.setEnvironmentVariable "DOCS_SERVER_URL" docsUrl
            |> CreateProcess.setEnvironmentVariable "DOCS_PUBLIC_ORIGIN" reviewUrl
            |> CreateProcess.setEnvironmentVariable "ASPNETCORE_ENVIRONMENT" "Development"
            // Avoid recursive native FileSystemWatcher failures on macOS.
            |> CreateProcess.setEnvironmentVariable "DOTNET_USE_POLLING_FILE_WATCHER" "1"
            |> CreateProcess.withWorkingDirectory docsDir
            |> CreateProcess.ensureExitCode
            |> Proc.start
            |> Async.AwaitTask
            |> Async.Ignore

        let watchExample (directory, url, catalogSpec) =
            CreateProcess.fromRawCommand "dotnet" ["watch"; "run"; "--no-launch-profile"; "--non-interactive"]
            |> CreateProcess.setEnvironmentVariable "ASPNETCORE_URLS" url
            |> CreateProcess.setEnvironmentVariable "EXAMPLE_PUBLIC_ORIGIN" reviewUrl
            |> CreateProcess.setEnvironmentVariable "EXAMPLE_DOCS_URL" "/examples/documentation"
            |> CreateProcess.setEnvironmentVariable "EXAMPLE_APP_URL" "/examples/application"
            |> CreateProcess.setEnvironmentVariable "EXAMPLE_SPEC_CATALOG_DEMO" (if catalogSpec then "true" else "false")
            |> CreateProcess.setEnvironmentVariable "ASPNETCORE_ENVIRONMENT" "Development"
            |> CreateProcess.setEnvironmentVariable "DOTNET_USE_POLLING_FILE_WATCHER" "1"
            |> CreateProcess.withWorkingDirectory directory
            |> CreateProcess.ensureExitCode
            |> Proc.start
            |> Async.AwaitTask
            |> Async.Ignore

        let watchProxy =
            CreateProcess.fromRawCommand "dotnet" ["watch"; "run"; "--no-launch-profile"; "--non-interactive"]
            |> CreateProcess.setEnvironmentVariable "ASPNETCORE_URLS" reviewUrl
            |> CreateProcess.setEnvironmentVariable "MOCK_CLOUDFLARE_DOCS_ORIGIN" docsUrl
            |> CreateProcess.setEnvironmentVariable "MOCK_CLOUDFLARE_EXAMPLE_ORIGIN" exampleUrl
            |> CreateProcess.setEnvironmentVariable "MOCK_CLOUDFLARE_EXAMPLE_SPEC_ORIGIN" exampleSpecUrl
            |> CreateProcess.setEnvironmentVariable "MOCK_CLOUDFLARE_EXAMPLE_DOCS_ORIGIN" exampleDocsUrl
            |> CreateProcess.setEnvironmentVariable "MOCK_CLOUDFLARE_EXAMPLE_PRESENTATION_ORIGIN" examplePresentationUrl
            |> CreateProcess.setEnvironmentVariable "DOTNET_USE_POLLING_FILE_WATCHER" "1"
            |> CreateProcess.withWorkingDirectory mockCloudflareDir
            |> CreateProcess.ensureExitCode
            |> Proc.start
            |> Async.AwaitTask
            |> Async.Ignore

        let watchAssets = otherExampleHosts |> List.collect (fun host -> [exampleCss host true; buildExampleJs host true])
        Async.Parallel ([watchProxy; watchApp; frontend "watch" docsFrontend; frontend "watch" exampleFrontend] @ List.map watchExample exampleHosts @ watchAssets)
        |> Async.RunSynchronously
        |> ignore
)

Target.create "Benchmark" <| fun parameters ->
    dotnet benchmarksDir ([ "run"; "--configuration"; "Release"; "--" ] @ parameters.Context.Arguments)
    |> Async.RunSynchronously

Target.create "BenchmarkSmoke" <| fun parameters ->
    dotnet benchmarksDir ([ "run"; "--configuration"; "Release"; "--"; "--smoke" ] @ parameters.Context.Arguments)
    |> Async.RunSynchronously

Target.create "BuildDocsCss" <| fun _ ->
    tailwindcss slnDir docsDir [ "--input"; "input.css"; "--output"; "wwwroot/css/output.css"; "--minify" ]
    |> Async.RunSynchronously

Target.create "BuildExampleCss" <| fun _ ->
    for host in exampleHosts do exampleCss host false |> Async.RunSynchronously

Target.create "PublishExample" <| fun _ ->
    for directory in exampleHosts do
        dotnet directory [ "publish"; "--output"; "./out"; "--self-contained"; "false" ]
        |> Async.RunSynchronously
        publishBrowserAssets directory exampleEntries exampleExternals

Target.create "PublishDocs" <| fun _ ->
    dotnet docsDir [
        "publish"
        "--output"; "./out"
        "--self-contained"; "false"
    ]
    |> Async.RunSynchronously
    publishBrowserAssets docsDir docsEntries []

Target.create "Default" (fun _ -> Target.listAvailable())

"Test" ==>! "Pack"
"CleanNugets" ==>! "Pack"
"Pack" ==>! "VerifyPackage"
"RestoreFrontendTools" ==>! "BuildDocsCss"
"RestoreFrontendTools" ==>! "BuildDocsJs"
"RestoreDocsFrontend" ==>! "BuildDocsJs"
"RestoreFrontendTools" ==>! "BuildExampleJs"
"RestoreFrontendTools" ==>! "VerifyTailwind"
"RestoreFrontendTools" ==>! "BuildExampleCss"
"RestoreExampleFrontend" ==>! "BuildExampleCss"
"BuildDocsCss" ==>! "PublishDocs"
"BuildDocsJs" ==>! "PublishDocs"
"BuildExampleJs" ==>! "PublishExample"
"BuildExampleCss" ==>! "PublishExample"
"VerifyTailwind" ==>! "PublishDocs"
"CheckInstalledComponents" ==>! "Test"

Target.runOrDefaultWithArguments "Default"
