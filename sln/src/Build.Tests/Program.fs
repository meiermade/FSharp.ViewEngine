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

type private BrowserShard =
    { browser:string
      index:int
      total:int }

let private browserShards workflow =
    Regex.Matches(workflow, @"(?m)^\s+- label: (chromium|firefox|webkit) (\d+)/(\d+)\s*$")
    |> Seq.cast<Match>
    |> Seq.map (fun matched ->
        { browser = matched.Groups[1].Value
          index = int matched.Groups[2].Value
          total = int matched.Groups[3].Value })
    |> Seq.toList

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

        test "CLI release inputs remain independent and non-Latest" {
            let cli = PackagePublishing.validateInputs "FSharp.ViewEngine.Cli" "2026.9.0" None false
            Expect.equal cli.package PackagePublishing.Package.Cli "CLI package"
            Expect.isNone cli.minimumDependency "CLI has no package dependency"
            Expect.isFalse cli.markLatest "CLI is not Latest"

            Expect.throws
                (fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Components" "2026.9.0" None false |> ignore)
                "the retired Components package cannot be published"
            Expect.throws
                (fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Docs" "2026.8.1" None false |> ignore)
                "the retired Docs package cannot be published"
        }

        testCase "Invalid package release inputs fail" <| fun _ ->
            let invalidCases = [
                fun () -> PackagePublishing.validateInputs "Other" "2026.8.0" None false |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine" "2026.8.0" (Some "2026.8.0") true |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Cli" "2026.8.0" (Some "2026.8.0") false |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Cli" "2026.8.0" None true |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Docs" "2026.8.0" None false |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Docs" "2026.8.0" (Some "2026.8.0") true |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine" "preview" None true |> ignore
            ]
            for invalid in invalidCases do Expect.throws invalid "invalid release input"

        test "Release selection carries one coherent public package snapshot" {
            let core = PackagePublishing.validateSelection "core" "2026.8.3" "2026.8.1"
            Expect.isSome core.core "Core selected"
            Expect.isNone core.cli "CLI package not selected"
            Expect.equal core.cliVersion "2026.8.1" "existing CLI remains traceable"

            let cli = PackagePublishing.validateSelection "cli" "2026.8.3" "2026.9.0"
            Expect.isNone cli.core "Core not selected"
            Expect.isSome cli.cli "CLI selected"

            let both = PackagePublishing.validateSelection "both" "2026.8.3" "2026.9.0"
            Expect.isSome both.core "Core package selected together"
            Expect.isSome both.cli "CLI package selected together"
            Expect.isNone both.cli.Value.minimumDependency "CLI remains independently installable"

            let docs = PackagePublishing.validateSelection "docs" "2026.8.3" "2026.9.0"
            Expect.isNone docs.core "Docs-only does not republish Core"
            Expect.isNone docs.cli "Docs-only does not republish CLI"
        }

        testCase "Invalid package selections or versions fail" <| fun _ ->
            let invalidCases = [
                fun () -> PackagePublishing.validateSelection "core" "preview" "2026.8.1" |> ignore
                fun () -> PackagePublishing.validateSelection "cli" "2026.8.3" "preview" |> ignore
                fun () -> PackagePublishing.validateSelection "other" "2026.8.3" "2026.9.0" |> ignore
            ]
            for invalid in invalidCases do Expect.throws invalid "invalid package selection"

        test "Release coherence publishes exactly changed contracts and pins unselected public versions" {
            let state latestVersion changedSinceLatest : PackagePublishing.PackageState =
                { latestVersion = latestVersion
                  changedSinceLatest = changedSinceLatest }
            let coreUnchanged = state (Some "2026.8.2") false
            let cliUnpublished = state None true
            let cli = PackagePublishing.validateSelection "cli" "2026.8.2" "2026.9.0"
            PackagePublishing.validateCoherence cli coreUnchanged cliUnpublished
            Expect.throws
                (fun () -> PackagePublishing.validateCoherence cli (state (Some "2026.8.2") true) cliUnpublished)
                "changed Core cannot remain unpublished"
            Expect.throws
                (fun () -> PackagePublishing.validateCoherence cli coreUnchanged (state (Some "2026.8.4") false))
                "unchanged CLI cannot be republished"

            let docs = PackagePublishing.validateSelection "docs" "2026.8.2" "2026.8.4"
            let cliUnchanged = state (Some "2026.8.4") false
            PackagePublishing.validateCoherence docs coreUnchanged cliUnchanged
            Expect.throws
                (fun () ->
                    PackagePublishing.validateSelection "docs" "2099.1.0" "2026.8.4"
                    |> fun selection -> PackagePublishing.validateCoherence selection coreUnchanged cliUnchanged)
                "Docs-only cannot advertise an arbitrary Core version"
            Expect.throws
                (fun () -> PackagePublishing.validateCoherence docs coreUnchanged (state (Some "2026.8.4") true))
                "Docs-only cannot carry unpublished CLI changes"
        }

        test "Local selected package identity is exact" {
            let directory = Path.Combine(Path.GetTempPath(), $"fve-local-package.{Guid.NewGuid():N}")
            Directory.CreateDirectory directory |> ignore
            try
                let packagePath = Path.Combine(directory, "FSharp.ViewEngine.Cli.2026.9.0.nupkg")
                File.WriteAllText(packagePath, "package")
                PackagePublishing.validateLocalPackage PackagePublishing.Package.Cli "2026.9.0" packagePath
                Expect.throws
                    (fun () -> PackagePublishing.validateLocalPackage PackagePublishing.Package.Cli "2026.8.2" packagePath)
                    "wrong selected CLI version"
            finally
                Directory.Delete(directory, true)
        }

        test "One public workflow selects a coherent package and Docs release" {
            let publish = workflow "publish.yml"
            Expect.stringContains publish "type: choice" "package selection is a choice"
            for selection in [ "core"; "cli"; "both"; "docs" ] do
                Expect.stringContains publish $"- {selection}" $"{selection} selection"
            Expect.stringContains publish "candidateCommit:" "release authorization selects an exact commit"
            Expect.stringContains publish "candidateImage:" "release authorization selects an exact digest"
            Expect.stringContains publish "coreVersion:" "the public Core version is explicit"
            Expect.stringContains publish "cliVersion:" "the public CLI version is explicit"
            Expect.isFalse (publish.Contains("componentsVersion:")) "the retired Components version is not selected"
            Expect.isFalse (publish.Contains("docsVersion:")) "no new Docs package versions"
            Expect.isFalse (publish.Contains("Publish FSharp.ViewEngine.Docs")) "no Docs publication path"
            Expect.isFalse (publish.Contains("Publish FSharp.ViewEngine.Components")) "no Components publication path"
            Expect.stringContains publish "inputs.packages == 'cli' || inputs.packages == 'both'" "both selects CLI"
            Expect.stringContains publish "inputs.packages == 'core' || inputs.packages == 'both'" "both selects Core"
            Expect.isFalse (File.Exists(workflowPath "publish-docs.yml")) "there is no second package-publishing entry point"
            Expect.isFalse (File.Exists(workflowPath "_publish-package.yml")) "single-use reusable workflow is removed"
            Expect.isFalse (File.Exists(workflowPath "verify-nuget-auth.yml")) "publication owns its OIDC authentication"
            Expect.isFalse (publish.Contains("secrets: inherit")) "publication does not inherit unrelated secrets"
        }

        test "CLI follows the independent public package spine" {
            let project = repositoryFile "sln/src/FSharp.ViewEngine.Cli/FSharp.ViewEngine.Cli.fsproj"
            let readme = repositoryFile "sln/src/FSharp.ViewEngine.Cli/README.md"
            let publishing = repositoryFile "sln/src/Build/PackagePublishing.fs"
            let preview = workflow "preview.yml"
            let publish = workflow "publish.yml"

            Expect.stringContains project "<PackAsTool>true</PackAsTool>" "CLI ships as a dotnet tool"
            Expect.stringContains project "<ToolCommandName>fve</ToolCommandName>" "CLI owns the fve command"
            Expect.stringContains project "<PackageId>FSharp.ViewEngine.Cli</PackageId>" "CLI has its own package identity"
            Expect.stringContains project "FSharpViewEngineCliPackageVersion" "CLI version is explicit"
            Expect.stringContains project "<PublishRepositoryUrl>true</PublishRepositoryUrl>" "repository metadata is enabled"
            Expect.stringContains project "FSharp.ViewEngine.Components.Registry.props" "canonical source registry is embedded"
            Expect.stringContains readme "dotnet tool install FSharp.ViewEngine.Cli" "CLI README documents installation"
            Expect.stringContains preview "Verify CLI package compatibility" "pull requests prove clean consumers"
            Expect.stringContains publishing "cli/v" "CLI has a distinct release tag namespace"
            Expect.stringContains publish "Publish FSharp.ViewEngine.Cli" "CLI is independently publishable"
        }

        test "Documentation is compiled from and distributed with the canonical source registry" {
            let project = repositoryFile "sln/src/FSharp.ViewEngine.Components/FSharp.ViewEngine.Components.fsproj"
            let registry = repositoryFile "sln/src/FSharp.ViewEngine.Components/FSharp.ViewEngine.Components.Registry.props"
            let cliProject = repositoryFile "sln/src/FSharp.ViewEngine.Cli/FSharp.ViewEngine.Cli.fsproj"
            let view = repositoryFile "sln/src/FSharp.ViewEngine.Components/Documentation/View.fs"
            let document = repositoryFile "sln/src/FSharp.ViewEngine.Components/Documentation/Document.fs"
            let baseManifestPath = repositoryPath "sln/src/FSharp.ViewEngine.Components/FSharp.ViewEngine.Components.tailwind.css"
            let docsStyles = repositoryFile "sln/src/Docs/input.css"
            let dockerfile = repositoryFile "sln/Dockerfile"
            let solution = repositoryFile "sln/FSharp.ViewEngine.slnx"
            Expect.stringContains registry "Documentation/View.fs" "the registry owns the maintained Documentation implementation"
            Expect.isFalse (registry.Contains("Documentation/Builders.fs")) "legacy Documentation builders are not compiled"
            Expect.isFalse (document.Contains("DocsBlock")) "documentation sections store typed HTML rather than legacy blocks"
            Expect.isFalse (document.Contains("DocsInline")) "documentation sections have no legacy inline model"
            Expect.stringContains cliProject "EmbeddedResource Include=\"@(FveComponent);@(FveAsset)\"" "the CLI embeds registered source and base assets"
            Expect.stringContains project "<IsPackable>false</IsPackable>" "the old Components package surface is retired"
            Expect.isFalse (solution.Contains("FSharp.ViewEngine.Docs")) "there is no separate Docs project"
            Expect.isFalse (registry.Contains("DefaultStyles.fs")) "embedded stylesheet source is removed"
            Expect.isFalse (registry.Contains("documentation-styles")) "Documentation has no separate registry stylesheet"
            Expect.isFalse (registry.Contains("Documentation.tailwind.css")) "Documentation has no separate CSS asset"
            Expect.stringContains view "bg-[var(--fve-page)]" "Documentation layout utilities remain beside its markup"
            Expect.stringContains view "data-fve-app-mode-root" "Documentation owns the server-rendered App-mode presentation"
            Expect.isFalse (view.Contains("--spec-")) "Documentation themes through the shared fve token contract"
            Expect.isFalse (view.Contains("DefaultStyles.css")) "documents do not inject package CSS"
            Expect.isFalse (File.Exists baseManifestPath) "Components has no separate stylesheet contract"
            Expect.isFalse (docsStyles.Contains("FSharp.ViewEngine.Components.tailwind.css")) "repository host scans component F# directly"
            Expect.isFalse (docsStyles.Contains("AppMode.tailwind.css")) "App mode does not require a separate stylesheet"
            Expect.isFalse (docsStyles.Contains("Documentation.tailwind.css")) "repository host scans Documentation F# directly"
            Expect.stringContains registry "Browser.fs" "Browser is a shared Primitive"
            Expect.stringContains registry "Phone.fs" "Phone is a shared Primitive"
            Expect.stringContains registry "Documentation/Fixture.fs" "Documentation Fixture owns App mode"
            Expect.stringContains dockerfile "FSharp.ViewEngine.Components/Documentation/verify-tailwind.sh" "container verifies Documentation source scanning"
            Expect.isFalse ((workflow "preview.yml").Contains("PACKAGE_ID: FSharp.ViewEngine.Docs")) "CI verifies one CLI package"
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
            Expect.stringContains packageBlock "Verify CLI package" "CLI package is verified"
            Expect.isFalse (packageBlock.Contains("Verify Components package")) "Components package publication is retired"
            Expect.isFalse (packageBlock.Contains("Verify Docs package")) "Documentation source is verified through the CLI"

            let publishCore = publishBlock.IndexOf("Publish FSharp.ViewEngine", StringComparison.Ordinal)
            let publishCli = publishBlock.IndexOf("Publish FSharp.ViewEngine.Cli", StringComparison.Ordinal)
            Expect.isTrue (publishCore > 0 && publishCli > publishCore) "Core publication precedes CLI"
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
            let retirementCheck = repositoryFile "e2e/scripts/verify-package-retirement.mjs"
            Expect.stringContains publish "queue: max" "explicit releases retain their serialized queue position"
            Expect.stringContains publish "cancel-in-progress: false" "an active release is never preempted"
            Expect.stringContains publish "Prove protected staging runs the selected candidate" "staging identity is proven first"
            Expect.stringContains publish "- acceptance" "package preparation waits for the aggregate acceptance result"
            Expect.stringContains publish "DEPLOY_IMAGE: ${{ needs.candidate.outputs.image }}" "production receives the accepted digest"
            Expect.stringContains publish "environment: release" "publication uses its protected environment"
            Expect.stringContains publish "name: production" "promotion uses its protected environment"
            Expect.isFalse (publish.Contains("steps.smoke.outcome != 'success'")) "recovery remains active after canonical smoke"
            Expect.stringContains publish "(failure() || cancelled()) && steps.deploy.outcome != 'skipped'" "any unsuccessful post-mutation path restores production"
            Expect.stringContains publish "steps.previous.outputs.image" "recovery preserves the previous immutable image"
            Expect.stringContains publish "LEGACY_PRODUCTION_CLI_VERSION: unreleased" "initial recovery records the actual pre-CLI snapshot"
            Expect.isFalse (publish.Contains("FALLBACK_CORE_VERSION: ${{ inputs.coreVersion }}")) "recovery never substitutes candidate package metadata"
            Expect.stringContains publish "Enable permanent legacy redirect after canonical acceptance" "redirect waits for canonical smoke"
            Expect.stringContains config "https://fve.meiermade.com" "production has one canonical origin"
            Expect.stringContains config "ALLOW_UNRELEASED_PACKAGE_SNAPSHOT" "rollback can represent the actual pre-CLI snapshot explicitly"
            Expect.stringContains redirect "Response.redirect(target.toString(), 301)" "the legacy redirect is permanent"
            Expect.stringContains redirect "new URL(event.request.url)" "paths and queries start from the original URL"
            Expect.stringContains redirect "target.protocol = 'https:'" "the redirect forces HTTPS"
            Expect.stringContains redirect "target.hostname = '${config.appConfig.hostname}'" "only the canonical host changes"
            Expect.stringContains redirect "WorkersRoute" "the redirect owns an isolated edge route"
            Expect.isFalse (redirect.Contains("http_request_dynamic_redirect")) "the app does not contend for a shared zone entry-point ruleset"
            Expect.stringContains retirementCheck "retiredPackages" "retirement evidence covers Components and Docs"
            Expect.stringContains retirementCheck "availableVersions" "retirement evidence covers every published version"
            Expect.stringContains retirementCheck "includes('Legacy')" "retirement evidence requires the Legacy reason"
            Expect.stringContains retirementCheck "FSharp.ViewEngine.Cli" "retirement evidence requires the CLI replacement"
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
            let fakeArguments = Path.Combine(fakeDirectory, "arguments.txt")
            Directory.CreateDirectory fakeDirectory |> ignore
            let fakeDocker = Path.Combine(fakeDirectory, "docker")
            File.WriteAllText(fakeDocker, "#!/usr/bin/env bash\nset -eu\nprintf cookie > \"$FAKE_AUTH_STATE\"\nprintf '%s\\n' \"$*\" > \"$FAKE_DOCKER_ARGS\"\nexit \"${FAKE_DOCKER_EXIT:-0}\"\n")
            File.SetUnixFileMode(fakeDocker, UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)
            let currentPath = Environment.GetEnvironmentVariable "PATH"
            let baseEnvironment exitCode =
                [ "PATH", $"{fakeDirectory}{Path.PathSeparator}{currentPath}"
                  "FAKE_AUTH_STATE", authState
                  "FAKE_DOCKER_ARGS", fakeArguments
                  "FAKE_DOCKER_EXIT", string exitCode
                  "E2E_BROWSER", "webkit"
                  "E2E_SHARD", "2/2"
                  "DOCS_E2E_BASE_URL", "https://fve.meiermade.net"
                  "DOCS_EXPECTED_COMMIT", String.replicate 40 "a" ]
            try
                let success = runProcessWithEnvironment repository "bash" [ "e2e/scripts/test-published-ci.sh" ] (baseEnvironment 0)
                Expect.equal success 0 "successful browser result is preserved"
                Expect.isFalse (File.Exists authState) "Access state is removed after success"
                let arguments = File.ReadAllText fakeArguments
                Expect.stringContains arguments "--project=webkit" "selected browser reaches Playwright"
                Expect.stringContains arguments "--shard=2/2" "selected shard reaches Playwright"
                Expect.stringContains arguments "--retries=0" "published acceptance is retry-free"
                Expect.isFalse (arguments.Contains("--project=chromium")) "unselected browsers are excluded"

                let failure = runProcessWithEnvironment repository "bash" [ "e2e/scripts/test-published-ci.sh" ] (baseEnvironment 23)
                Expect.equal failure 23 "failed browser result is preserved"
                Expect.isFalse (File.Exists authState) "Access state is removed after failure"
            finally
                if File.Exists authState then File.Delete authState
                Directory.Delete(fakeDirectory, true)
        }

        test "PR and release browser matrices select the same complete shards" {
            let previewShards = workflow "preview.yml" |> browserShards
            let releaseShards = workflow "publish.yml" |> browserShards
            let canonical shards = shards |> List.sortBy (fun shard -> shard.browser, shard.index)
            Expect.isNonEmpty previewShards "PR acceptance selects browser shards"
            Expect.equal (canonical previewShards) (canonical releaseShards) "PR and release acceptance use one browser contract"

            for browser in [ "chromium"; "firefox"; "webkit" ] do
                let shards = previewShards |> List.filter (fun shard -> shard.browser = browser)
                Expect.isNonEmpty shards $"{browser} retains intentional coverage"
                let totals = shards |> List.map _.total |> List.distinct
                Expect.equal totals.Length 1 $"{browser} uses one shard total"
                let total = totals.Head
                let indexes = shards |> List.map _.index |> List.sort
                Expect.equal indexes [ 1 .. total ] $"{browser} shards are complete and unique"
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
                (PackagePublishing.belongsToPackage "FSharp.ViewEngine" "/packages/FSharp.ViewEngine.Cli.2026.9.0.nupkg")
                "CLI package does not match Core"
            Expect.isFalse
                (PackagePublishing.belongsToPackage "FSharp.ViewEngine" "/packages/FSharp.ViewEngine.Docs.2026.8.1.nupkg")
                "Docs package does not match Core"
            Expect.isTrue
                (PackagePublishing.belongsToPackage "FSharp.ViewEngine.Cli" "/packages/FSharp.ViewEngine.Cli.2026.9.0.nupkg")
                "CLI package matches CLI"
        }

        test "Expected release assets are exact" {
            Expect.sequenceEqual
                (PackagePublishing.expectedAssetNames "FSharp.ViewEngine.Cli" "2026.9.0")
                [ "FSharp.ViewEngine.Cli.2026.9.0.nupkg"
                  "SHA256SUMS" ]
                "release assets"
        }

        test "Release metadata preserves same-package previous tag" {
            let directory = Path.Combine(Path.GetTempPath(), $"fve-release-tests.{Guid.NewGuid():N}")
            Directory.CreateDirectory directory |> ignore
            try
                let run arguments = PackagePublishing.runProcess true "git" ([ "-C"; directory ] @ arguments) |> ignore
                run [ "init"; "--initial-branch=main" ]
                run [ "config"; "user.email"; "ci@meiermade.com" ]
                run [ "config"; "user.name"; "Test" ]
                File.WriteAllText(Path.Combine(directory, "file"), "content")
                run [ "add"; "file" ]
                run [ "commit"; "-m"; "initial" ]
                run [ "tag"; "v2026.8.0" ]
                run [ "tag"; "cli/v2026.8.0" ]
                run [ "tag"; "docs/v2026.8.0" ]

                let metadata = Release.prepare directory (Path.Combine(directory, "release.json")) "cli/v" "2026.8.1"
                Expect.equal metadata.previousTag (Some "cli/v2026.8.0") "previous CLI tag excludes Core and Docs"
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
