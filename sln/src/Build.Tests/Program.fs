module Build.Tests.Program

open System
open System.IO
open System.IO.Compression
open System.Diagnostics
open System.Text.RegularExpressions
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

let private repositoryPath path =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "..", path))

let private repositoryFile path = repositoryPath path |> File.ReadAllText

let private runProcessWithEnvironment workingDirectory command arguments environment =
    let startInfo = ProcessStartInfo(command)
    startInfo.WorkingDirectory <- workingDirectory
    startInfo.UseShellExecute <- false
    for argument in arguments do startInfo.ArgumentList.Add argument
    for key, value in environment do startInfo.Environment[key] <- value
    use child = Process.Start startInfo
    child.WaitForExit()
    child.ExitCode

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

        test "Release selection carries one coherent public package snapshot" {
            let core = PackagePublishing.validateSelection "core" "2026.8.3" "2026.8.1"
            Expect.isSome core.core "Core selected"
            Expect.isNone core.components "Components package not selected"
            Expect.equal core.componentsVersion "2026.8.1" "existing Components remains traceable"

            let components = PackagePublishing.validateSelection "components" "2026.8.3" "2026.9.0"
            Expect.isNone components.core "Core not selected"
            Expect.isSome components.components "Components selected"

            let both = PackagePublishing.validateSelection "both" "2026.8.3" "2026.9.0"
            Expect.isSome both.core "Core package selected together"
            Expect.isSome both.components "Components package selected together"
            Expect.equal
                both.components.Value.minimumDependency
                (Some { package = PackagePublishing.Package.ViewEngine; minimumVersion = "2026.8.3" })
                "Components uses the selected Core version"

            let docs = PackagePublishing.validateSelection "docs" "2026.8.3" "2026.9.0"
            Expect.isNone docs.core "Docs-only does not republish Core"
            Expect.isNone docs.components "Docs-only does not republish Components"
        }

        testCase "Invalid package selections or versions fail" <| fun _ ->
            let invalidCases = [
                fun () -> PackagePublishing.validateSelection "core" "preview" "2026.8.1" |> ignore
                fun () -> PackagePublishing.validateSelection "components" "2026.8.3" "preview" |> ignore
                fun () -> PackagePublishing.validateSelection "other" "2026.8.3" "2026.9.0" |> ignore
            ]
            for invalid in invalidCases do Expect.throws invalid "invalid package selection"

        test "Release coherence publishes exactly changed contracts and pins unselected public versions" {
            let state latestVersion changedSinceLatest : PackagePublishing.PackageState =
                { latestVersion = latestVersion
                  changedSinceLatest = changedSinceLatest }
            let coreUnchanged = state (Some "2026.8.2") false
            let componentsUnpublished = state None true
            let components = PackagePublishing.validateSelection "components" "2026.8.2" "2026.9.0"
            PackagePublishing.validateCoherence components coreUnchanged componentsUnpublished
            Expect.throws
                (fun () -> PackagePublishing.validateCoherence components (state (Some "2026.8.2") true) componentsUnpublished)
                "changed Core cannot remain unpublished"
            Expect.throws
                (fun () -> PackagePublishing.validateCoherence components coreUnchanged (state (Some "2026.8.4") false))
                "unchanged Components cannot be republished"
            Expect.throws
                (fun () ->
                    PackagePublishing.validateSelection "components" "2026.8.1" "2026.9.0"
                    |> fun selection -> PackagePublishing.validateCoherence selection coreUnchanged componentsUnpublished)
                "Components cannot advertise an older Core snapshot"

            let docs = PackagePublishing.validateSelection "docs" "2026.8.2" "2026.8.4"
            let componentsUnchanged = state (Some "2026.8.4") false
            PackagePublishing.validateCoherence docs coreUnchanged componentsUnchanged
            Expect.throws
                (fun () ->
                    PackagePublishing.validateSelection "docs" "2099.1.0" "2026.8.4"
                    |> fun selection -> PackagePublishing.validateCoherence selection coreUnchanged componentsUnchanged)
                "Docs-only cannot advertise an arbitrary Core version"
            Expect.throws
                (fun () -> PackagePublishing.validateCoherence docs coreUnchanged (state (Some "2026.8.4") true))
                "Docs-only cannot carry unpublished Components changes"
        }

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

        test "One public workflow selects a coherent package and Docs release" {
            let publish = workflow "publish.yml"
            Expect.stringContains publish "type: choice" "package selection is a choice"
            for selection in [ "core"; "components"; "both"; "docs" ] do
                Expect.stringContains publish $"- {selection}" $"{selection} selection"
            Expect.stringContains publish "candidateCommit:" "release authorization selects an exact commit"
            Expect.stringContains publish "candidateImage:" "release authorization selects an exact digest"
            Expect.stringContains publish "coreVersion:" "the public Core version is explicit"
            Expect.stringContains publish "componentsVersion:" "the public Components version is explicit"
            Expect.isFalse (publish.Contains("componentsMinimumCoreVersion:")) "the selected public Core version is the Components bound"
            Expect.isFalse (publish.Contains("docsVersion:")) "no new Docs package versions"
            Expect.isFalse (publish.Contains("Publish FSharp.ViewEngine.Docs")) "no Docs publication path"
            Expect.stringContains publish "inputs.packages == 'components' || inputs.packages == 'both'" "both selects Components"
            Expect.stringContains publish "inputs.packages == 'core' || inputs.packages == 'both'" "both selects Core"
            Expect.stringContains publish "LOCAL_DEPENDENCY_PACKAGE_PATH:" "a selected Core package can satisfy Components preflight"
            Expect.stringContains publish "COMPONENTS_MINIMUM_CORE_VERSION: ${{ inputs.coreVersion }}" "Components uses the selected public Core version"
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
            let productionStart = publish.IndexOf("\n  production:", StringComparison.Ordinal)
            Expect.isTrue (packageStart > 0 && publishStart > packageStart && productionStart > publishStart) "package, publication, and promotion are ordered"
            let packageBlock = publish.Substring(packageStart, publishStart - packageStart)
            let publishBlock = publish.Substring(publishStart, productionStart - publishStart)
            Expect.equal
                (packageBlock.Split("actions/upload-artifact@", StringSplitOptions.None).Length - 1)
                1
                "selected packages share one verified release bundle"
            Expect.equal
                (publishBlock.Split("actions/download-artifact@", StringSplitOptions.None).Length - 1)
                1
                "publication downloads the release bundle once"
            Expect.isFalse (publish.Contains("uses: ./.github/workflows/deploy.yml")) "release promotion remains explicit"
            Expect.isFalse (publish.Contains("secrets: inherit")) "publication does not inherit unrelated secrets"

            Expect.stringContains packageBlock "./fake.sh Test --single-target" "release source is tested once"
            Expect.stringContains packageBlock "ValidateReleaseSelection" "changed package contracts are checked"
            Expect.stringContains packageBlock "Verify Core package" "Core package is verified"
            Expect.stringContains packageBlock "Verify Components package" "Components package is verified"
            Expect.isFalse (packageBlock.Contains("Verify Docs package")) "Documentation is verified within Components"

            let publishCore = publishBlock.IndexOf("Publish FSharp.ViewEngine", StringComparison.Ordinal)
            let publishComponents = publishBlock.IndexOf("Publish FSharp.ViewEngine.Components", StringComparison.Ordinal)
            Expect.isTrue (publishCore > 0 && publishComponents > publishCore) "Core publication precedes Components"
            Expect.stringContains publishBlock "VerifyPublishedPackageRelease" "NuGet.org artifacts receive post-publication verification"
            Expect.stringContains packageBlock "VerifyPublicPackageSnapshot" "unselected advertised packages are verified from NuGet.org"
            Expect.isFalse (publishBlock.Contains("Publish FSharp.ViewEngine.Docs")) "retired Docs publication is absent"
            Expect.stringContains publishBlock "environment: release" "publication uses the protected environment"
            Expect.stringContains publishBlock "NuGet/login@8d196754b4036150537f80ac539e15c2f1028841" "publication uses trusted publishing"
        }

        test "Explicit releases gate publication and promote the accepted digest safely" {
            let publish = workflow "publish.yml"
            let config = repositoryFile "pulumi/src/config.ts"
            let redirect = repositoryFile "pulumi/src/cloudflare/redirect.ts"
            let releaseRunner = repositoryFile "e2e/scripts/test-release-candidate.sh"
            let retirementCheck = repositoryFile "e2e/scripts/verify-docs-retirement.mjs"
            Expect.stringContains publish "queue: max" "explicit releases retain their serialized queue position"
            Expect.stringContains publish "cancel-in-progress: false" "an active release is never preempted"
            Expect.stringContains publish "Prove protected staging runs the selected candidate" "staging identity is proven first"
            Expect.stringContains releaseRunner "E2E_CROSS_BROWSER_MODE=focused" "release acceptance runs the right-sized browser suite"
            Expect.stringContains releaseRunner "production-smoke.spec.ts" "release acceptance identifies the production-only suite"
            Expect.stringContains releaseRunner "!=" "staging release acceptance excludes the production-only suite"
            Expect.stringContains publish "Retry-free staging release acceptance (${{ matrix.label }})" "the release gate names its retry policy and isolated browser job"
            Expect.stringContains publish "E2E_BROWSER: ${{ matrix.browser }}" "each release job selects exactly one browser"
            Expect.stringContains publish "fail-fast: false" "every release browser reports its result"
            Expect.equal (Regex.Matches(publish, "browser: chromium").Count) 2 "Chromium is split into two isolated workflow shards"
            Expect.stringContains publish "E2E_SHARD: ${{ matrix.shard }}" "release jobs select one non-overlapping shard"
            Expect.stringContains publish "- acceptance" "package preparation waits for every release browser job"
            Expect.stringContains publish "DEPLOY_IMAGE: ${{ needs.candidate.outputs.image }}" "production receives the accepted digest"
            Expect.stringContains publish "environment: release" "publication uses its protected environment"
            Expect.stringContains publish "name: production" "promotion uses its protected environment"
            Expect.isFalse (publish.Contains("steps.smoke.outcome != 'success'")) "recovery remains active after canonical smoke"
            Expect.stringContains publish "(failure() || cancelled()) && steps.deploy.outcome != 'skipped'" "any unsuccessful post-mutation path restores production"
            Expect.stringContains publish "steps.previous.outputs.image" "recovery preserves the previous immutable image"
            Expect.stringContains publish "LEGACY_PRODUCTION_COMPONENTS_VERSION: unreleased" "initial recovery records the actual pre-Components snapshot"
            Expect.isFalse (publish.Contains("FALLBACK_CORE_VERSION: ${{ inputs.coreVersion }}")) "recovery never substitutes candidate package metadata"
            Expect.stringContains publish "Enable permanent legacy redirect after canonical acceptance" "redirect waits for canonical smoke"
            Expect.stringContains config "https://fve.meiermade.com" "production has one canonical origin"
            Expect.stringContains config "ALLOW_UNRELEASED_PACKAGE_SNAPSHOT" "rollback can represent the actual pre-Components snapshot explicitly"
            Expect.stringContains redirect "Response.redirect(target.toString(), 301)" "the legacy redirect is permanent"
            Expect.stringContains redirect "new URL(event.request.url)" "paths and queries start from the original URL"
            Expect.stringContains redirect "target.protocol = 'https:'" "the redirect forces HTTPS"
            Expect.stringContains redirect "target.hostname = '${config.appConfig.hostname}'" "only the canonical host changes"
            Expect.stringContains redirect "WorkersRoute" "the redirect owns an isolated edge route"
            Expect.isFalse (redirect.Contains("http_request_dynamic_redirect")) "the app does not contend for a shared zone entry-point ruleset"
            Expect.stringContains retirementCheck "availableVersions" "retirement evidence covers every published Docs version"
            Expect.stringContains retirementCheck "includes('Legacy')" "retirement evidence requires the Legacy reason"
            Expect.stringContains retirementCheck "replacementPackage" "retirement evidence requires the Components replacement"
            Expect.stringContains retirementCheck "entry.packageContent" "retirement evidence preserves pinned downloads"
        }

        test "Main deploys only a protected immutable staging candidate" {
            let deploy = workflow "deploy.yml"
            Expect.stringContains deploy "push:" "main changes deploy automatically"
            Expect.stringContains deploy "- main" "only main deploys automatically"
            Expect.stringContains deploy "workflow_dispatch:" "staging remains manually redeployable"
            Expect.stringContains deploy "name: staging" "deployment uses the staging GitHub environment"
            Expect.stringContains deploy "url: https://fve.meiermade.net" "deployment names the protected staging origin"
            Expect.stringContains deploy "stack-name: meiermade/fsharp-view-engine/dev" "main updates only the isolated dev stack"
            Expect.isFalse (deploy.Contains("stack-name: meiermade/fsharp-view-engine/prod")) "main never updates production"
            Expect.stringContains deploy "pulumi env run meiermade/fsharpviewengine/e2e-dev" "acceptance loads scoped credentials in the consuming command"
            Expect.stringContains deploy "bash scripts/test-published-ci.sh tests/staging-smoke.spec.ts" "main runs the bounded staging smoke suite"
            Expect.stringContains deploy "DEPLOY_IMAGE: ${{ steps.previous.outputs.image }}" "unsuccessful acceptance restores the previous immutable image"
            Expect.stringContains deploy "queue: max" "every successful merge retains its place in the staging queue"
            Expect.stringContains deploy "cancel-in-progress: false" "a newer merge does not interrupt an active rollout"
            Expect.stringContains deploy "if: ${{ always() }}" "workflow cancellation leaves the deployment job available for recovery"
            Expect.stringContains deploy "failure() || cancelled()" "failure and cancellation both enter recovery"
            Expect.stringContains deploy "steps.deploy.outcome != 'skipped'" "recovery runs whenever deployment may have mutated staging"
            Expect.stringContains deploy "steps.restore.outcome == 'success'" "a restored release must pass readiness"
            Expect.stringContains deploy "Confirm restored staging release is healthy" "recovery proves the previous release is serving"
            Expect.isFalse (deploy.Contains("\n    timeout-minutes:")) "a job timeout cannot preempt bounded recovery steps"
            Expect.isFalse (deploy.Contains("workflow_call:")) "package releases do not call staging deployment"
            Expect.isFalse (deploy.Contains("secrets: inherit")) "staging does not inherit unrelated secrets"
            Expect.isFalse (deploy.Contains("playwright install --with-deps")) "staging acceptance uses the pinned browser image"
        }

        test "Published E2E runner removes Access state and preserves the browser result" {
            let repository = repositoryPath "."
            let e2eDirectory = repositoryPath "e2e"
            let authState = Path.Combine(e2eDirectory, ".auth", "access.json")
            let fakeDirectory = Path.Combine(Path.GetTempPath(), $"fve-fake-docker.{Guid.NewGuid():N}")
            Directory.CreateDirectory fakeDirectory |> ignore
            let fakeDocker = Path.Combine(fakeDirectory, "docker")
            File.WriteAllText(fakeDocker, "#!/usr/bin/env bash\nset -eu\nprintf cookie > \"$FAKE_AUTH_STATE\"\nexit \"${FAKE_DOCKER_EXIT:-0}\"\n")
            File.SetUnixFileMode(fakeDocker, UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)
            let currentPath = Environment.GetEnvironmentVariable "PATH"
            let baseEnvironment exitCode =
                [ "PATH", $"{fakeDirectory}{Path.PathSeparator}{currentPath}"
                  "FAKE_AUTH_STATE", authState
                  "FAKE_DOCKER_EXIT", string exitCode
                  "DOCS_E2E_BASE_URL", "https://fve.meiermade.net"
                  "DOCS_EXPECTED_COMMIT", String.replicate 40 "a" ]
            try
                let success = runProcessWithEnvironment repository "bash" [ "e2e/scripts/test-published-ci.sh" ] (baseEnvironment 0)
                Expect.equal success 0 "successful browser result is preserved"
                Expect.isFalse (File.Exists authState) "Access state is removed after success"

                let failure = runProcessWithEnvironment repository "bash" [ "e2e/scripts/test-published-ci.sh" ] (baseEnvironment 23)
                Expect.equal failure 23 "failed browser result is preserved"
                Expect.isFalse (File.Exists authState) "Access state is removed after failure"
            finally
                if File.Exists authState then File.Delete authState
                Directory.Delete(fakeDirectory, true)
        }

        test "E2E workflows share the pinned Playwright image and stage browser coverage" {
            let preview = workflow "preview.yml"
            let package = repositoryFile "e2e/package.json"
            let pullRequestRunner = repositoryFile "e2e/scripts/test-ci.sh"
            let productionRunner = repositoryFile "e2e/scripts/test-published-ci.sh"
            let image = repositoryFile "e2e/playwright-image.txt"
            let config = repositoryFile "e2e/playwright.config.ts"
            let selection = repositoryFile "e2e/scripts/verify-test-selection.mjs"
            let docsBrowser = repositoryFile "e2e/tests/docs.spec.ts"
            Expect.stringContains preview "bash scripts/test-ci.sh" "workflow delegates container orchestration"
            Expect.stringContains preview "name: E2E (${{ matrix.label }})" "browser engines and Chromium shards use isolated matrix jobs"
            Expect.stringContains preview "fail-fast: false" "every browser reports its result"
            Expect.stringContains preview "E2E_CROSS_BROWSER_MODE: focused" "pull requests use focused cross-browser coverage"
            Expect.isFalse (preview.Contains("schedule:")) "the browser suite is not scheduled nightly"
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
                Expect.stringContains productionRunner $"--project={browser}" $"release acceptance retains intentional {browser} coverage"
            Expect.stringContains config "fullyParallel: true" "independent tests may use bounded parallel workers"
            Expect.stringContains config "workers: process.env.CI ? 1 : undefined" "each hosted shard isolates browser state in one bounded worker"
            Expect.stringContains productionRunner "--shard=$E2E_SHARD" "browser scripts support complete non-overlapping workflow shards"
            Expect.stringContains config "retries: 0" "browser acceptance never conceals failures with retries"
            Expect.stringContains preview "npm run test:selection" "pull requests verify intentional suite ownership"
            Expect.stringContains selection "Chromium must retain 200–250 primary checks" "primary coverage has a discoverable budget"
            Expect.stringContains selection "Firefox and WebKit focused selections differ" "secondary engines retain one compatibility contract"
            Expect.stringContains selection "Chromium workflow shards do not cover the complete primary selection" "workflow shards are complete and unique"
            Expect.stringContains docsBrowser "request.get('/sitemap.xml')" "browser-independent route checks consume the server-owned route inventory"
            Expect.isFalse (docsBrowser.Contains("const routes = [")) "browser tests do not maintain a second public-route inventory"
            Expect.stringContains package "test:pr" "the focused pull-request mode is directly runnable"
            Expect.stringContains package "test:release" "the right-sized release mode is directly runnable"
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

        test "Docs Cloudflare configuration retains production-only RUM protection" {
            let cloudflareIndex = repositoryFile "pulumi/src/cloudflare/index.ts"
            let rum = repositoryFile "pulumi/src/cloudflare/rum.ts"
            Expect.stringContains cloudflareIndex "import './rum'" "Cloudflare composition owns the RUM rule"
            Expect.stringContains rum "config.isStaging" "staging does not contend for the shared zone configuration phase"
            Expect.stringContains rum "phase: 'http_config_settings'" "production configuration rule uses the Cloudflare settings phase"
            Expect.stringContains rum "action: 'set_config'" "configuration rule changes only matched request settings"
            Expect.stringContains rum "disableRum: true" "automatic browser RUM is deliberately disabled"
            Expect.stringContains rum "http.host eq" "rule is hostname scoped"
            Expect.stringContains rum "config.appConfig.hostname" "the production rule uses the validated application hostname"
        }

        test "Pulumi workflows install the GKE credential plugin" {
            let expectedAction = "google-github-actions/setup-gcloud@aa5489c8933f4cc7a4f7d45035b3b1440c9c10db # v3.0.1"
            for name in [ "deploy.yml"; "preview.yml"; "publish.yml" ] do
                let workflow = workflow name
                Expect.stringContains workflow expectedAction $"{name} pins setup-gcloud"
                Expect.stringContains workflow "install_components: gke-gcloud-auth-plugin" $"{name} installs GKE authentication"
        }

        test "Staging delivery uses current provider state and safe rollout boundaries" {
            let deploy = workflow "deploy.yml"
            let preview = workflow "preview.yml"
            let appConfig = repositoryFile "pulumi/src/config.ts"
            let pulumiDev = repositoryFile "pulumi/Pulumi.dev.yaml"
            let access = repositoryFile "pulumi/src/cloudflare/access.ts"
            let zone = repositoryFile "pulumi/src/cloudflare/zone.ts"
            let deployment = repositoryFile "pulumi/src/k8s/deployment.ts"
            let stack = repositoryFile "pulumi/index.ts"
            let playwright = repositoryFile "e2e/playwright.config.ts"
            let accessSetup = repositoryFile "e2e/access-setup.ts"
            Expect.stringContains deploy "pulumi refresh --run-program" "refresh uses the current provider configuration"
            Expect.isFalse (deploy.Contains("refresh: true")) "the update does not refresh against stale provider state"
            Expect.stringContains preview "stack-name: meiermade/fsharp-view-engine/dev" "pull requests preview the isolated staging stack"
            Expect.stringContains preview "stack-name: meiermade/fsharp-view-engine/prod" "pull requests expose any production-boundary drift without updating it"
            Expect.stringContains pulumiDev "fsharpviewengine/dev" "the dev stack composes only the development ESC environment"
            Expect.stringContains appConfig "https://fve.meiermade.net" "the staging origin is exact"
            Expect.stringContains appConfig "zoneId: rawCloudflareConfig.require('zoneId')" "both stacks require their ESC-projected zone ID"
            Expect.isFalse (zone.Contains("getZoneOutput")) "application stacks do not rediscover configured Cloudflare zones"
            Expect.stringContains appConfig "DEPLOY_IMAGE must be an immutable sha256 image reference" "rollback and promotion reject mutable images"
            Expect.stringContains access "allowAdminsAccessPolicyId" "administrators retain normal Access authentication"
            Expect.stringContains access "ZeroTrustAccessServiceToken" "CI receives a staging-only service token"
            Expect.stringContains access "additionalSecretOutputs: ['clientSecret']" "the Access client secret remains secret"
            Expect.stringContains deployment "maxUnavailable: 0" "rollout retains the previous healthy replica"
            Expect.stringContains deployment "maxSurge: 1" "rollout creates the candidate before retirement"
            Expect.stringContains deployment "protect: config.isStaging" "staging workload resources resist accidental deletion"
            Expect.stringContains deployment "name: 'RELEASE_IMAGE'" "the workload exposes its exact image identity"
            Expect.stringContains stack "export const imageDigest" "the stack publishes the immutable image"
            Expect.stringContains stack "export const e2eReady" "the downstream E2E environment has an explicit bootstrap guard"
            Expect.stringContains playwright "hasAccessClientSecret ? 'off'" "credential-bearing runs do not retain network traces"
            Expect.stringContains accessSetup "new URL(baseURL).origin !== stagingOrigin" "Access credentials are limited to the staging origin"
            Expect.stringContains accessSetup "storageState" "a domain-scoped Access cookie replaces credential-bearing browser requests"
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
