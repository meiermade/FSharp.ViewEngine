open System.Text.RegularExpressions
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

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
let tailwindcss args = exec docsDir "tailwindcss" args

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
let docsPaths = [ "sln/src/Docs"; "sln/src/FSharp.ViewEngine.Components"; "sln/src/FSharp.ViewEngine"; "sln/Dockerfile" ]

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
            let commit,image = PackagePublishing.acceptedCandidate releaseRepository githubRepository
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
            let plan = PackagePublishing.planRelease requested commit image System.DateTimeOffset.UtcNow coreState cliState docsChanged coreVersions cliVersions
            PackagePublishing.writePlan planPath plan
            plan
    PackagePublishing.validatePinnedPlan requested plan.commit plan
    PackagePublishing.runProcess false "git" ["-C"; releaseRepository; "checkout"; "--detach"; plan.commit] |> ignore
    let outputPath = Environment.environVarOrFail "GITHUB_OUTPUT"
    System.IO.File.AppendAllLines(outputPath, [
        $"commit={plan.commit}"; $"image={plan.image}"; $"packages={plan.packages}"
        $"coreVersion={plan.coreVersion}"; $"cliVersion={plan.cliVersion}"
        $"docsCoreVersion={plan.docsCoreVersion}"; $"docsCliVersion={plan.docsCliVersion}"
        $"promoteDocs={string plan.promoteDocs |> _.ToLowerInvariant()}"
        $"bundle={string bundle |> _.ToLowerInvariant()}"
        $"newPlan={string (not reuse) |> _.ToLowerInvariant()}" ])
    Trace.trace $"Pinned {plan.commit} / {plan.image}: {plan.packages}; Core {plan.coreVersion}; CLI {plan.cliVersion}; promote Docs {plan.promoteDocs}"

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
    ["net8.0"; "net9.0"; "net10.0"]
    |> List.iter (fun tfm ->
        dotnet testsDir ["run"; "--framework"; tfm]
        |> Async.RunSynchronously
    )

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

Target.create "WatchDocs" (fun _ ->
    let docsUrl = WatchDocs.defaultUrl

    WatchDocs.runExclusiveWatcher "WatchDocs" docsUrl <| fun () ->
        Trace.trace $"Starting the FSharp.ViewEngine Docs at {docsUrl}"

        let watchApp =
            CreateProcess.fromRawCommand "dotnet"
                ["watch"; "run"; "--no-restore"; "--no-launch-profile"; "--non-interactive"]
            |> CreateProcess.setEnvironmentVariable "DOCS_SERVER_URL" docsUrl
            |> CreateProcess.setEnvironmentVariable "DOCS_PUBLIC_ORIGIN" docsUrl
            // Avoid recursive native FileSystemWatcher failures on macOS.
            |> CreateProcess.setEnvironmentVariable "DOTNET_USE_POLLING_FILE_WATCHER" "1"
            |> CreateProcess.withWorkingDirectory docsDir
            |> CreateProcess.ensureExitCode
            |> Proc.start
            |> Async.AwaitTask
            |> Async.Ignore

        let watchCss =
            // Imported package stylesheets are siblings of Docs, so watch their common root.
            exec (System.IO.Path.GetDirectoryName docsDir) "tailwindcss"
                [ "--input"; "Docs/input.css"; "--output"; "Docs/wwwroot/css/output.css"
                  "--watch=always"; "--minify" ]

        Async.Parallel [| watchApp; watchCss |]
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
    tailwindcss [ "--input"; "input.css"; "--output"; "wwwroot/css/output.css"; "--minify" ]
    |> Async.RunSynchronously

Target.create "PublishDocs" <| fun _ ->
    dotnet docsDir [
        "publish"
        "--output"; "./out"
        "--self-contained"; "false"
    ]
    |> Async.RunSynchronously

Target.create "Default" (fun _ -> Target.listAvailable())

"Test" ==>! "Pack"
"CleanNugets" ==>! "Pack"
"Pack" ==>! "VerifyPackage"
"BuildDocsCss" ==>! "PublishDocs"

Target.runOrDefaultWithArguments "Default"
