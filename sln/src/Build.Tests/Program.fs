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
        test "Core release inputs select Core and Latest" {
            let inputs = PackagePublishing.validateInputs "FSharp.ViewEngine" "2026.8.2" None true
            Expect.equal inputs.package PackagePublishing.Package.ViewEngine "Core package"
            Expect.equal inputs.version "2026.8.2" "Core version"
            Expect.isNone inputs.minimumCoreVersion "Core has no Core dependency"
            Expect.isTrue inputs.markLatest "Core is Latest"
        }

        test "Dependent package release inputs require Core and remain non-Latest" {
            let components = PackagePublishing.validateInputs "FSharp.ViewEngine.Components" "2026.8.0" (Some "2026.8.2") false
            Expect.equal components.package PackagePublishing.Package.Components "Components package"
            Expect.equal components.minimumCoreVersion (Some "2026.8.2") "Components minimum Core"
            Expect.isFalse components.markLatest "Components is not Latest"

            let docs = PackagePublishing.validateInputs "FSharp.ViewEngine.Docs" "2026.8.1" (Some "2026.8.2") false
            Expect.equal docs.package PackagePublishing.Package.Docs "Docs package"
            Expect.equal docs.minimumCoreVersion (Some "2026.8.2") "Docs minimum Core"
            Expect.isFalse docs.markLatest "Docs is not Latest"
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
            let core = PackagePublishing.validateSelection "core" (Some "2026.8.3") None None None None
            Expect.isSome core.core "Core selected"
            Expect.isNone core.components "Components package not selected"
            Expect.isNone core.docs "Docs package not selected"

            let components = PackagePublishing.validateSelection "components" None (Some "2026.8.1") None (Some "2026.8.3") None
            Expect.isNone components.core "Core not selected"
            Expect.isSome components.components "Components selected"
            Expect.isNone components.docs "Docs package not selected"

            let docs = PackagePublishing.validateSelection "docs" None None (Some "2026.8.2") None (Some "2026.8.3")
            Expect.isNone docs.core "Core not selected"
            Expect.isNone docs.components "Components package not selected"
            Expect.isSome docs.docs "Docs package selected"

            let both =
                PackagePublishing.validateSelection
                    "both"
                    (Some "2026.8.3")
                    None
                    (Some "2026.8.2")
                    None
                    (Some "2026.8.3")
            Expect.isSome both.core "Core selected together"
            Expect.isNone both.components "Components package not selected"
            Expect.isSome both.docs "Docs package selected together"
        }

        testCase "Invalid conditional package versions fail" <| fun _ ->
            let invalidCases = [
                fun () -> PackagePublishing.validateSelection "core" None None None None None |> ignore
                fun () -> PackagePublishing.validateSelection "core" (Some "2026.8.3") (Some "2026.8.2") None None None |> ignore
                fun () -> PackagePublishing.validateSelection "components" None (Some "2026.8.1") None None None |> ignore
                fun () -> PackagePublishing.validateSelection "components" (Some "2026.8.3") (Some "2026.8.1") None (Some "2026.8.3") None |> ignore
                fun () -> PackagePublishing.validateSelection "docs" None None (Some "2026.8.2") None None |> ignore
                fun () -> PackagePublishing.validateSelection "docs" (Some "2026.8.3") None (Some "2026.8.2") None (Some "2026.8.3") |> ignore
                fun () -> PackagePublishing.validateSelection "both" (Some "2026.8.3") None None None (Some "2026.8.3") |> ignore
                fun () -> PackagePublishing.validateSelection "other" None None None None None |> ignore
            ]
            for invalid in invalidCases do Expect.throws invalid "invalid package selection"

        test "An unpublished selected Core package can satisfy Docs preflight" {
            let directory = Path.Combine(Path.GetTempPath(), $"fve-local-core.{Guid.NewGuid():N}")
            Directory.CreateDirectory directory |> ignore
            try
                let packagePath = Path.Combine(directory, "FSharp.ViewEngine.2026.8.3.nupkg")
                File.WriteAllText(packagePath, "package")
                PackagePublishing.validateLocalCorePackage "2026.8.3" packagePath
                Expect.throws
                    (fun () -> PackagePublishing.validateLocalCorePackage "2026.8.2" packagePath)
                    "wrong selected Core version"
            finally
                Directory.Delete(directory, true)
        }

        test "One public workflow selects independent package releases" {
            let publish = workflow "publish.yml"
            Expect.stringContains publish "type: choice" "package selection is a choice"
            for selection in [ "core"; "components"; "docs"; "both" ] do
                Expect.stringContains publish $"- {selection}" $"{selection} selection"
            Expect.stringContains publish "coreVersion:" "independent Core version"
            Expect.stringContains publish "componentsVersion:" "independent Components package version"
            Expect.stringContains publish "componentsMinimumCoreVersion:" "Components minimum Core version"
            Expect.stringContains publish "docsVersion:" "independent Docs package version"
            Expect.stringContains publish "minimumCoreVersion:" "Docs minimum Core version"
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

        test "Docs package documents consumer-owned Noto and semantic typography" {
            let readme = repositoryFile "sln/src/FSharp.ViewEngine.Docs/README.md"
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
            Expect.stringContains packageBlock "Verify Docs package" "Docs package is verified"

            let publishBlock = publish.Substring(publishStart)
            let publishCore = publishBlock.IndexOf("Publish FSharp.ViewEngine", StringComparison.Ordinal)
            let publishComponents = publishBlock.IndexOf("Publish FSharp.ViewEngine.Components", StringComparison.Ordinal)
            let publishDocs = publishBlock.IndexOf("Publish FSharp.ViewEngine.Docs", StringComparison.Ordinal)
            Expect.isTrue (publishCore > 0 && publishComponents > publishCore && publishDocs > publishComponents) "dependent package publication follows Core"
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

        test "Pulumi workflows install the GKE credential plugin" {
            let expectedAction = "google-github-actions/setup-gcloud@aa5489c8933f4cc7a4f7d45035b3b1440c9c10db # v3.0.1"
            for name in [ "deploy.yml"; "preview.yml" ] do
                let workflow = workflow name
                Expect.stringContains workflow expectedAction $"{name} pins setup-gcloud"
                Expect.stringContains workflow "install_components: gke-gcloud-auth-plugin" $"{name} installs GKE authentication"
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
