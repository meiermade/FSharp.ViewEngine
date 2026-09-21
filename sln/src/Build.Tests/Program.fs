module Build.Tests.Program

open System
open System.IO
open System.IO.Compression
open Expecto

let private writePackage path (entries:(string * string) list) =
    use archive = ZipFile.Open(path, ZipArchiveMode.Create)
    for name, content in entries do
        let entry = archive.CreateEntry name
        use writer = new StreamWriter(entry.Open())
        writer.Write content

let private workflowPath name =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "..", ".github", "workflows", name))

let private workflow name = workflowPath name |> File.ReadAllText

let private repositoryFile path =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "..", path))
    |> File.ReadAllText

let tests =
    testList "Package publishing" [
        test "WatchDocs owns a stable loopback review URL" {
            let build = repositoryFile "sln/src/Build/Program.fs"
            let docsConfig = repositoryFile "sln/src/Docs/src/Common/Config.fs"
            let compose = repositoryFile "compose.yml"
            let deployment = repositoryFile "pulumi/src/k8s/deployment.ts"
            Expect.equal WatchDocs.defaultUrl "http://127.0.0.1:5054" "Docs has one stable product-specific URL"
            Expect.stringContains docsConfig "DOCS_SERVER_URL\" \"http://127.0.0.1:5054" "the direct Docs host uses the same default"
            Expect.stringContains compose "DOCS_SERVER_URL: http://0.0.0.0:5000" "the Docker host names its listener explicitly"
            Expect.stringContains deployment "name: 'DOCS_SERVER_URL', value: 'http://0.0.0.0:5000'" "the deployed host names its listener explicitly"
            Expect.stringContains build "\"--watch=always\"" "background WatchDocs must not silently stop its CSS watcher"
            Expect.stringContains build "WatchDocs.runExclusiveWatcher" "one owned watcher replaces only its predecessor"
            Expect.stringContains build "DOCS_SERVER_URL" "local review has a Docs-specific override"
            for invalid in [ "https://localhost:5054"; "http://localhost:5054"; "http://127.0.0.1:5054/path"; "http://127.0.0.1:5054?state=ready" ] do
                Expect.throws (fun () -> WatchDocs.ensureLoopbackUrlAvailable "WatchDocs test" invalid) $"rejects {invalid}"
        }

        test "Core release inputs select Core and Latest" {
            let inputs = PackagePublishing.validateInputs "FSharp.ViewEngine" "2026.8.2" None true
            Expect.equal inputs.package PackagePublishing.Package.ViewEngine "Core package"
            Expect.equal inputs.version "2026.8.2" "Core version"
            Expect.isNone inputs.minimumDependency "Core has no package dependency"
            Expect.isTrue inputs.markLatest "Core is Latest"
        }

        test "Dependent package release inputs preserve the package graph and remain non-Latest" {
            let components = PackagePublishing.validateInputs "FSharp.ViewEngine.Components" "2026.8.0" (Some "2026.8.2") false
            Expect.equal components.package PackagePublishing.Package.Components "Components package"
            Expect.equal
                components.minimumDependency
                (Some { package = PackagePublishing.Package.ViewEngine; minimumVersion = "2026.8.2" })
                "Components depends on Core"
            Expect.isFalse components.markLatest "Components is not Latest"

            Expect.throws
                (fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Docs" "2026.8.1" (Some "2026.8.0") false |> ignore)
                "the retired Docs package cannot be published"
        }

        testCase "Invalid package release inputs fail" <| fun _ ->
            let invalidCases = [
                fun () -> PackagePublishing.validateInputs "Other" "2026.8.0" None false |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine" "2026.8.0" (Some "2026.8.0") true |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Components" "2026.8.0" None false |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Components" "2026.8.0" (Some "2026.8.0") true |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Docs" "2026.8.0" None false |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Docs" "2026.8.0" (Some "2026.8.0") true |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine" "preview" None true |> ignore
            ]
            for invalid in invalidCases do Expect.throws invalid "invalid release input"

        test "Release selection validates conditional package versions" {
            let core = PackagePublishing.validateSelection "core" (Some "2026.8.3") None None
            Expect.isSome core.core "Core selected"
            Expect.isNone core.components "Components package not selected"

            let components = PackagePublishing.validateSelection "components" None (Some "2026.8.1") (Some "2026.8.3")
            Expect.isNone components.core "Core not selected"
            Expect.isSome components.components "Components selected"

            let both = PackagePublishing.validateSelection "both" (Some "2026.8.3") (Some "2026.8.1") None
            Expect.isSome both.core "Core package selected together"
            Expect.isSome both.components "Components package selected together"
            Expect.equal
                both.components.Value.minimumDependency
                (Some { package = PackagePublishing.Package.ViewEngine; minimumVersion = "2026.8.3" })
                "Components uses the selected Core version"
        }

        testCase "Invalid conditional package versions fail" <| fun _ ->
            let invalidCases = [
                fun () -> PackagePublishing.validateSelection "core" None None None |> ignore
                fun () -> PackagePublishing.validateSelection "core" (Some "2026.8.3") (Some "2026.8.2") None |> ignore
                fun () -> PackagePublishing.validateSelection "components" None (Some "2026.8.1") None |> ignore
                fun () -> PackagePublishing.validateSelection "components" (Some "2026.8.3") (Some "2026.8.1") (Some "2026.8.3") |> ignore
                fun () -> PackagePublishing.validateSelection "docs" None (Some "2026.8.2") (Some "2026.8.1") |> ignore
                fun () -> PackagePublishing.validateSelection "both" (Some "2026.8.3") None None |> ignore
                fun () -> PackagePublishing.validateSelection "both" None (Some "2026.8.1") None |> ignore
                fun () -> PackagePublishing.validateSelection "both" (Some "2026.8.3") (Some "2026.8.1") (Some "2026.8.2") |> ignore
                fun () -> PackagePublishing.validateSelection "other" None None None |> ignore
            ]
            for invalid in invalidCases do Expect.throws invalid "invalid package selection"

        test "An unpublished selected dependency package can satisfy release preflight" {
            let directory = Path.Combine(Path.GetTempPath(), $"fve-local-dependency.{Guid.NewGuid():N}")
            Directory.CreateDirectory directory |> ignore
            try
                let packagePath = Path.Combine(directory, "FSharp.ViewEngine.Components.2026.8.3.nupkg")
                File.WriteAllText(packagePath, "package")
                PackagePublishing.validateLocalPackage PackagePublishing.Package.Components "2026.8.3" packagePath
                Expect.throws
                    (fun () -> PackagePublishing.validateLocalPackage PackagePublishing.Package.Components "2026.8.2" packagePath)
                    "wrong selected Components version"
            finally
                Directory.Delete(directory, true)
        }

        test "One public workflow selects independent package releases" {
            let publish = workflow "publish.yml"
            Expect.stringContains publish "type: choice" "package selection is a choice"
            for selection in [ "core"; "components"; "both" ] do
                Expect.stringContains publish $"- {selection}" $"{selection} selection"
            Expect.stringContains publish "coreVersion:" "independent Core version"
            Expect.stringContains publish "componentsVersion:" "independent Components package version"
            Expect.stringContains publish "componentsMinimumCoreVersion:" "Components minimum Core version"
            Expect.isFalse (publish.Contains("docsVersion:")) "no new Docs versions"
            Expect.isFalse (publish.Contains("FSharp.ViewEngine.Docs")) "no Docs publication path"
            Expect.stringContains publish "inputs.packages == 'components' || inputs.packages == 'both'" "both selects Components"
            Expect.stringContains publish "inputs.packages == 'core' || inputs.packages == 'both'" "both selects Core"
            Expect.stringContains publish "LOCAL_DEPENDENCY_PACKAGE_PATH:" "a selected Core package can satisfy Components preflight"
            Expect.stringContains publish "inputs.packages == 'both' && inputs.coreVersion || inputs.componentsMinimumCoreVersion" "bundled dependency matches the selected Core"
            Expect.isFalse (File.Exists(workflowPath "publish-docs.yml")) "there is no second package-publishing entry point"
            Expect.isFalse (File.Exists(workflowPath "_publish-package.yml")) "single-use reusable workflow is removed"
            Expect.isFalse (File.Exists(workflowPath "verify-nuget-auth.yml")) "publication owns its OIDC authentication"
            Expect.isFalse (publish.Contains("secrets: inherit")) "publication does not inherit unrelated secrets"
        }

        test "Components follows the independent public package spine" {
            let project = repositoryFile "sln/src/FSharp.ViewEngine.Components/FSharp.ViewEngine.Components.fsproj"
            let readme = repositoryFile "sln/src/FSharp.ViewEngine.Components/README.md"
            let publishing = repositoryFile "sln/src/Build/PackagePublishing.fs"
            let preview = workflow "preview.yml"
            let publish = workflow "publish.yml"

            Expect.stringContains project "<TargetFramework>net8.0</TargetFramework>" "Components ships one compatibility asset"
            Expect.stringContains project "<PackageId>FSharp.ViewEngine.Components</PackageId>" "Components has its own package identity"
            Expect.stringContains project "ValidateComponentsPackageVersions" "direct packing validates package versions"
            Expect.stringContains project "FSharpViewEngineComponentsPackageVersion" "Components version is explicit"
            Expect.stringContains project "FSharpViewEnginePackageVersion" "minimum Core version is explicit"
            Expect.stringContains project "<IncludeSymbols>true</IncludeSymbols>" "portable symbols are enabled"
            Expect.stringContains project "<PublishRepositoryUrl>true</PublishRepositoryUrl>" "Source Link repository metadata is enabled"
            Expect.stringContains project "contentFiles/any/any" "Tailwind manifest is packaged"
            Expect.stringContains project "..\\FSharp.ViewEngine\\FSharp.ViewEngine.fsproj" "Components depends on Core"
            Expect.isFalse (project.Contains("FSharp.ViewEngine.Docs")) "Components does not depend on Docs"
            Expect.stringContains readme "dotnet add package FSharp.ViewEngine.Components" "package README documents installation"
            Expect.stringContains readme "FSharp.ViewEngine.Components.tailwind.css" "package README documents Tailwind setup"
            Expect.stringContains preview "Verify Components package compatibility" "pull requests prove clean consumers"
            Expect.stringContains publishing "components/v" "Components has a distinct release tag namespace"
            Expect.stringContains publish "Publish FSharp.ViewEngine.Components" "Components is independently publishable"
        }

        test "Documentation is compiled and packaged inside Components with selective CSS" {
            let project = repositoryFile "sln/src/FSharp.ViewEngine.Components/FSharp.ViewEngine.Components.fsproj"
            let view = repositoryFile "sln/src/FSharp.ViewEngine.Components/Documentation/View.fs"
            let document = repositoryFile "sln/src/FSharp.ViewEngine.Components/Documentation/Document.fs"
            let manifest = repositoryFile "sln/src/FSharp.ViewEngine.Components/Documentation/Documentation.tailwind.css"
            let baseManifest = repositoryFile "sln/src/FSharp.ViewEngine.Components/FSharp.ViewEngine.Components.tailwind.css"
            let appModeManifest = repositoryFile "sln/src/FSharp.ViewEngine.Components/AppMode.tailwind.css"
            let appModeRuntime = repositoryFile "sln/src/FSharp.ViewEngine.Components/app-mode.js"
            let hostedAppModeRuntime = repositoryFile "sln/src/Docs/wwwroot/scripts/fve-app-mode.js"
            let docsStyles = repositoryFile "sln/src/Docs/input.css"
            let dockerfile = repositoryFile "sln/Dockerfile"
            let solution = repositoryFile "sln/FSharp.ViewEngine.slnx"
            Expect.stringContains project "Documentation/View.fs" "Components compiles the maintained Documentation implementation"
            Expect.isFalse (project.Contains("Documentation/Builders.fs")) "legacy Documentation builders are not compiled"
            Expect.isFalse (document.Contains("DocsBlock")) "documentation sections store typed HTML rather than legacy blocks"
            Expect.isFalse (document.Contains("DocsInline")) "documentation sections have no legacy inline model"
            Expect.stringContains project "contentFiles/any/any/Documentation" "optional Documentation stylesheet ships in Components"
            Expect.isFalse (solution.Contains("FSharp.ViewEngine.Docs")) "there is no separate Docs project"
            Expect.isFalse (project.Contains("DefaultStyles.fs")) "embedded stylesheet source is removed"
            Expect.stringContains manifest ".spec-shell" "Documentation layout remains in the readable manifest"
            Expect.equal hostedAppModeRuntime appModeRuntime "Docs serves the package-owned App mode runtime verbatim"
            Expect.stringContains manifest "--docs-text-reading: 1rem" "semantic typography remains in the manifest"
            Expect.isFalse (view.Contains("DefaultStyles.css")) "documents do not inject package CSS"
            Expect.stringContains docsStyles "FSharp.ViewEngine.Components.tailwind.css" "repository host imports shared styles"
            Expect.stringContains docsStyles "FSharp.ViewEngine.Components/AppMode.tailwind.css" "repository host explicitly opts into App mode"
            Expect.stringContains docsStyles "Documentation/Documentation.tailwind.css" "repository host explicitly opts into Documentation"
            Expect.isFalse (baseManifest.Contains("AppMode.tailwind.css")) "the base manifest does not import optional App mode"
            Expect.isFalse (baseManifest.Contains("data-fve-app-mode-root")) "the base manifest excludes App-mode viewer selectors"
            Expect.stringContains baseManifest ".spec-browser-frame" "static Browser styling remains in the base manifest"
            Expect.stringContains baseManifest ".fve-phone" "static Phone styling remains in the base manifest"
            Expect.stringContains appModeManifest "data-fve-app-mode-root" "the optional manifest owns App-mode viewer selectors"
            Expect.stringContains project "Browser.fs" "Browser is a shared Primitive"
            Expect.stringContains project "Phone.fs" "Phone is a shared Primitive"
            Expect.isFalse (project.Contains("Documentation/AppMode.fs")) "App mode is not a Documentation public component"
            Expect.stringContains dockerfile "FSharp.ViewEngine.Components/Documentation/verify-tailwind.sh" "container verifies optional Documentation CSS"
            Expect.isFalse ((workflow "preview.yml").Contains("PACKAGE_ID: FSharp.ViewEngine.Docs")) "CI verifies one UI package"
        }

        test "Documentation documents consumer-owned Noto and semantic typography" {
            let readme = repositoryFile "sln/src/FSharp.ViewEngine.Components/Documentation/README.md"
            Expect.stringContains readme "does not ship font binaries or request Google-hosted assets" "font delivery remains consumer-owned"
            Expect.stringContains readme "font-family: \"Noto Sans\"" "optional Noto Sans self-hosting is explicit"
            Expect.stringContains readme "font-family: \"Noto Sans Mono\"" "optional Noto Sans Mono self-hosting is explicit"
            Expect.stringContains readme "font-display: swap" "self-hosting recipe keeps fallback text readable"
            for role in [ "--docs-text-ancillary"; "--docs-text-ui"; "--docs-text-reading"; "--docs-text-code" ] do
                Expect.stringContains readme role $"README documents {role}"
            Expect.isFalse (readme.Contains("fonts.googleapis.com")) "the package does not recommend runtime Google requests"
            Expect.isFalse (readme.Contains("fonts.gstatic.com")) "the package does not recommend runtime Google assets"
        }

        test "Package workflow verifies one release bundle before ordered publication" {
            let publish = workflow "publish.yml"
            let packageStart = publish.IndexOf("\n  package:", StringComparison.Ordinal)
            let publishStart = publish.IndexOf("\n  publish:", StringComparison.Ordinal)
            Expect.isTrue (packageStart > 0 && publishStart > packageStart) "package job precedes publish job"
            Expect.equal
                (publish.Split("actions/upload-artifact@", StringSplitOptions.None).Length - 1)
                1
                "selected packages share one verified release bundle"
            Expect.equal
                (publish.Split("actions/download-artifact@", StringSplitOptions.None).Length - 1)
                1
                "publication downloads the release bundle once"
            Expect.isFalse (publish.Contains("uses: ./.github/workflows/deploy.yml")) "package publication does not deploy the site"
            Expect.isFalse (publish.Contains("secrets: inherit")) "publication does not inherit unrelated secrets"

            let packageBlock = publish.Substring(packageStart, publishStart - packageStart)
            Expect.stringContains packageBlock "./fake.sh Test --single-target" "release source is tested once"
            Expect.stringContains packageBlock "Verify Core package" "Core package is verified"
            Expect.stringContains packageBlock "Verify Components package" "Components package is verified"
            Expect.isFalse (packageBlock.Contains("Verify Docs package")) "Documentation is verified within Components"

            let publishBlock = publish.Substring(publishStart)
            let publishCore = publishBlock.IndexOf("Publish FSharp.ViewEngine", StringComparison.Ordinal)
            let publishComponents = publishBlock.IndexOf("Publish FSharp.ViewEngine.Components", StringComparison.Ordinal)
            Expect.isTrue (publishCore > 0 && publishComponents > publishCore) "Core publication precedes Components"
            Expect.isFalse (publishBlock.Contains("Publish FSharp.ViewEngine.Docs")) "retired Docs publication is absent"
            Expect.stringContains publishBlock "environment: release" "publication uses the protected environment"
            Expect.stringContains publishBlock "NuGet/login@8d196754b4036150537f80ac539e15c2f1028841" "publication uses trusted publishing"
        }

        test "Documentation deploy tracks main independently from package releases" {
            let deploy = workflow "deploy.yml"
            Expect.stringContains deploy "push:" "main changes deploy automatically"
            Expect.stringContains deploy "- main" "only main deploys automatically"
            Expect.stringContains deploy "workflow_dispatch:" "site remains manually redeployable"
            Expect.isFalse (deploy.Contains("workflow_call:")) "package releases do not call site deployment"
            Expect.isFalse (deploy.Contains("expectedCoreVersion")) "site health does not predict Core publication"
            Expect.isFalse (deploy.Contains("expectedDocsVersion")) "site health does not predict Docs publication"
            Expect.stringContains deploy "bash scripts/test-published-ci.sh" "production acceptance uses the browser image"
            Expect.isFalse (deploy.Contains("playwright install --with-deps")) "production acceptance skips host browser installation"
        }

        test "E2E workflows share the pinned Playwright image and stage browser coverage" {
            let preview = workflow "preview.yml"
            let package = repositoryFile "e2e/package.json"
            let pullRequestRunner = repositoryFile "e2e/scripts/test-ci.sh"
            let productionRunner = repositoryFile "e2e/scripts/test-published-ci.sh"
            let image = repositoryFile "e2e/playwright-image.txt"
            Expect.stringContains preview "bash scripts/test-ci.sh" "workflow delegates container orchestration"
            Expect.stringContains preview "name: E2E (${{ matrix.browser }})" "browser engines use isolated matrix jobs"
            Expect.stringContains preview "fail-fast: false" "every browser reports its result"
            Expect.stringContains preview "E2E_CROSS_BROWSER_MODE: focused" "pull requests use focused cross-browser coverage"
            Expect.isFalse (preview.Contains("schedule:")) "the complete suite is not scheduled nightly"
            Expect.isFalse (preview.Contains("playwright install --with-deps")) "host browser installation is skipped"
            Expect.stringContains
                image
                "mcr.microsoft.com/playwright:v1.62.1-noble@sha256:"
                "browser image matches and pins the project dependency"
            for runner in [ pullRequestRunner; productionRunner ] do
                Expect.stringContains runner "playwright-image.txt" "runner uses the shared image reference"
                Expect.stringContains runner "E2E_CROSS_BROWSER_MODE" "runner selects an explicit delivery-stage mode"
            Expect.stringContains pullRequestRunner "--network host" "browser container reaches the local Docs image"
            for browser in [ "chromium"; "firefox"; "webkit" ] do
                Expect.stringContains pullRequestRunner $"--project={browser}" $"pull requests retain {browser} coverage"
                Expect.stringContains productionRunner $"--project={browser}" $"release acceptance retains complete {browser} coverage"
            Expect.stringContains package "test:pr" "the focused pull-request mode is directly runnable"
            Expect.stringContains package "test:release" "the complete release mode is directly runnable"
        }

        test "Privileged Pulumi preview excludes fork pull requests" {
            let preview = workflow "preview.yml"
            let privilegedStart = preview.IndexOf("\n  preview:", StringComparison.Ordinal)
            let terminalStart = preview.IndexOf("\n  test:", StringComparison.Ordinal)
            Expect.isTrue (privilegedStart > 0 && terminalStart > privilegedStart) "terminal test follows privileged preview"
            let privileged = preview.Substring(privilegedStart, terminalStart - privilegedStart)
            Expect.stringContains
                privileged
                "github.event.pull_request.head.repo.full_name == github.repository"
                "OIDC preview only runs for repository branches"
            Expect.stringContains privileged "id-token: write" "trusted previews authenticate with OIDC"
            let terminal = preview.Substring(terminalStart)
            Expect.stringContains terminal "name: Test" "protected branch check keeps its name"
            Expect.stringContains terminal "needs.preview.result == 'skipped'" "forks may skip privileged preview"
        }

        test "Docs Cloudflare configuration disables unreliable RUM only for its hostname" {
            let cloudflareIndex = repositoryFile "pulumi/src/cloudflare/index.ts"
            let rum = repositoryFile "pulumi/src/cloudflare/rum.ts"
            Expect.stringContains cloudflareIndex "import './rum'" "Cloudflare composition owns the RUM rule"
            Expect.stringContains rum "phase: 'http_config_settings'" "configuration rule uses the Cloudflare settings phase"
            Expect.stringContains rum "action: 'set_config'" "configuration rule changes only matched request settings"
            Expect.stringContains rum "disableRum: true" "automatic browser RUM is deliberately disabled"
            Expect.stringContains rum "http.host eq" "rule is hostname scoped"
            Expect.stringContains rum "config.identifier" "hostname uses the product identifier"
            Expect.stringContains rum "config.cloudflareConfig.zoneName" "hostname uses the configured zone"
        }

        test "Pulumi workflows install the GKE credential plugin" {
            let expectedAction = "google-github-actions/setup-gcloud@aa5489c8933f4cc7a4f7d45035b3b1440c9c10db # v3.0.1"
            for name in [ "deploy.yml"; "preview.yml" ] do
                let workflow = workflow name
                Expect.stringContains workflow expectedAction $"{name} pins setup-gcloud"
                Expect.stringContains workflow "install_components: gke-gcloud-auth-plugin" $"{name} installs GKE authentication"
        }

        test "Production refresh runs the current Kubernetes provider program" {
            let deploy = workflow "deploy.yml"
            Expect.stringContains deploy "pulumi refresh --run-program" "refresh uses the current provider configuration"
            Expect.isFalse (deploy.Contains("refresh: true")) "the update does not refresh against stale provider state"
        }

        test "Versioned changelog entries follow verified package releases" {
            let build = repositoryFile "sln/src/Build/Program.fs"
            let readme = repositoryFile "README.md"
            Expect.isFalse (build.Contains("validateChangelog")) "release preparation does not require a future changelog entry"
            Expect.stringContains readme "after the package is published and verified" "release documentation records the post-release changelog step"
        }

        test "Package discovery matches exact package identities" {
            Expect.isTrue
                (PackagePublishing.belongsToPackage "FSharp.ViewEngine" "/packages/FSharp.ViewEngine.2026.8.3.nupkg")
                "Core package matches Core"
            Expect.isFalse
                (PackagePublishing.belongsToPackage "FSharp.ViewEngine" "/packages/FSharp.ViewEngine.Components.2026.8.0.nupkg")
                "Components package does not match Core"
            Expect.isFalse
                (PackagePublishing.belongsToPackage "FSharp.ViewEngine" "/packages/FSharp.ViewEngine.Docs.2026.8.1.nupkg")
                "Docs package does not match Core"
            Expect.isTrue
                (PackagePublishing.belongsToPackage "FSharp.ViewEngine.Components" "/packages/FSharp.ViewEngine.Components.2026.8.0.nupkg")
                "Components package matches Components"
        }

        test "Expected release assets are exact" {
            Expect.sequenceEqual
                (PackagePublishing.expectedAssetNames "FSharp.ViewEngine.Components" "2026.8.0")
                [ "FSharp.ViewEngine.Components.2026.8.0.nupkg"
                  "FSharp.ViewEngine.Components.2026.8.0.snupkg"
                  "SHA256SUMS" ]
                "release assets"
        }

        test "Release metadata preserves same-package previous tag" {
            let directory = Path.Combine(Path.GetTempPath(), $"fve-release-tests.{Guid.NewGuid():N}")
            Directory.CreateDirectory directory |> ignore
            try
                let run arguments = PackagePublishing.runProcess true "git" ([ "-C"; directory ] @ arguments) |> ignore
                run [ "init"; "--initial-branch=main" ]
                run [ "config"; "user.email"; "test@example.com" ]
                run [ "config"; "user.name"; "Test" ]
                File.WriteAllText(Path.Combine(directory, "file"), "content")
                run [ "add"; "file" ]
                run [ "commit"; "-m"; "initial" ]
                run [ "tag"; "v2026.8.0" ]
                run [ "tag"; "components/v2026.8.0" ]
                run [ "tag"; "docs/v2026.8.0" ]

                let metadata = Release.prepare directory (Path.Combine(directory, "release.json")) "components/v" "2026.8.1"
                Expect.equal metadata.previousTag (Some "components/v2026.8.0") "previous Components tag excludes Core and Docs"
            finally
                Directory.Delete(directory, true)
        }

        test "GitHub Release assets must match exactly" {
            let expected = PackagePublishing.expectedAssetNames "FSharp.ViewEngine" "2026.8.2"
            PackagePublishing.validateReleaseAssets expected (List.rev expected)
            Expect.throws
                (fun () -> PackagePublishing.validateReleaseAssets expected ("extra" :: expected))
                "extra release asset"
            Expect.throws
                (fun () -> PackagePublishing.validateReleaseAssets expected (List.tail expected))
                "missing release asset"
        }

        test "Repository-signed NuGet package matches verified package" {
            let directory = Path.Combine(Path.GetTempPath(), $"fve-build-tests.{Guid.NewGuid():N}")
            Directory.CreateDirectory directory |> ignore
            try
                let expected = Path.Combine(directory, "expected.nupkg")
                let signed = Path.Combine(directory, "signed.nupkg")
                writePackage expected [ "lib/net8.0/a.dll", "same"; "README.md", "same" ]
                writePackage signed [ "lib/net8.0/a.dll", "same"; "README.md", "same"; ".signature.p7s", "signature" ]
                PackagePublishing.verifyPublishedPackage expected signed
            finally
                Directory.Delete(directory, true)
        }

        test "Changed NuGet package is rejected" {
            let directory = Path.Combine(Path.GetTempPath(), $"fve-build-tests.{Guid.NewGuid():N}")
            Directory.CreateDirectory directory |> ignore
            try
                let expected = Path.Combine(directory, "expected.nupkg")
                let changed = Path.Combine(directory, "changed.nupkg")
                writePackage expected [ "README.md", "expected" ]
                writePackage changed [ "README.md", "changed"; ".signature.p7s", "signature" ]
                Expect.throws
                    (fun () -> PackagePublishing.verifyPublishedPackage expected changed)
                    "changed package"
            finally
                Directory.Delete(directory, true)
        }
    ]

[<EntryPoint>]
let main args = runTestsWithCLIArgs [] args tests
