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
            Expect.equal WatchDocs.defaultUrl "http://127.0.0.1:5054" "Docs has one stable product-specific URL"
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

        test "UTC calendar versions advance independent streams and reset each month" {
            let now = DateTimeOffset(2026,10,1,0,30,0,TimeSpan.Zero)
            Expect.equal (Release.nextVersion now ["2026.8.2"]) "2026.10.0" "first release of the month starts at zero"
            Expect.equal (Release.nextVersion now ["2026.10.0";"2026.10.4";"2026.9.20"]) "2026.10.5" "largest published month counter wins"
            Expect.equal (Release.nextVersion (now.ToOffset(TimeSpan.FromHours -7.)) ["2026.9.9"]) "2026.10.0" "local September clock still allocates October UTC"
            Expect.equal (Release.nextVersion (DateTimeOffset(2027,1,1,0,0,0,TimeSpan.Zero)) ["2026.12.8"]) "2027.1.0" "new year starts a new counter"
            Expect.throws (fun () -> Release.nextVersion now ["2026.11.0"] |> ignore) "a clock behind published versions cannot allocate"
        }

        test "Automatic and explicit releases keep package publication separate from Docs promotion" {
            let state version changed : PackagePublishing.PackageState = { latestVersion=Some version; changedSinceLatest=changed }
            let core = state "2026.8.2" true
            let cli = state "2026.10.4" true
            let now = DateTimeOffset(2026,10,13,0,0,0,TimeSpan.Zero)
            let plan requested docsChanged core cli =
                PackagePublishing.planRelease requested (String.replicate 40 "a") ("registry/image@sha256:"+String.replicate 64 "b") now core cli docsChanged ["2026.10.1"] ["2026.10.5"]
            let automatic = plan "auto" true core cli
            Expect.equal (automatic.packages,automatic.coreVersion,automatic.cliVersion,automatic.promoteDocs) ("both","2026.10.2","2026.10.6",true) "changed projects use independent tag/NuGet counters"
            let coreOnly = plan "core" true core cli
            Expect.equal (coreOnly.packages,coreOnly.cliVersion,coreOnly.promoteDocs) ("core","2026.10.4",false) "pending CLI and Docs changes do not force publication/promotion"
            let cliOnly = plan "cli" true core cli
            Expect.equal (cliOnly.packages,cliOnly.coreVersion,cliOnly.promoteDocs) ("cli","2026.8.2",false) "Core is independent"
            let docs = plan "docs" true core cli
            Expect.equal (docs.packages,docs.promoteDocs,docs.docsCoreVersion,docs.docsCliVersion) ("docs",true,"unreleased","unreleased") "Docs promotion cannot claim unpublished source is released"
            let unchangedCore = { core with changedSinceLatest=false }
            let unchangedCli = { cli with changedSinceLatest=false }
            let both = plan "both" true unchangedCore cli
            Expect.equal (both.packages,both.coreVersion,both.promoteDocs) ("cli","2026.8.2",false) "Both does not republish unchanged Core"
            let automaticCli = plan "auto" false unchangedCore cli
            Expect.equal (automaticCli.packages,automaticCli.promoteDocs) ("cli",false) "CLI-only changes do not force Docs promotion"
            let docsOnly = plan "auto" true unchangedCore unchangedCli
            Expect.equal (docsOnly.packages,docsOnly.docsCoreVersion,docsOnly.docsCliVersion) ("docs","2026.8.2","2026.10.4") "unchanged public package identities remain traceable"
            Expect.throws (fun () -> plan "auto" false unchangedCore unchangedCli |> ignore) "nothing changed"
            Expect.throws (fun () -> plan "core" true unchangedCore cli |> ignore) "unchanged requested Core is not republished"
        }

        test "Pinned transaction round-trips without reallocating versions or source" {
            let directory = Path.Combine(Path.GetTempPath(), $"fve-plan.{Guid.NewGuid():N}")
            let path = Path.Combine(directory,"transaction.json")
            try
                let state : PackagePublishing.PackageState = { latestVersion=Some "2026.10.4"; changedSinceLatest=true }
                let commit = String.replicate 40 "a"
                let plan = PackagePublishing.planRelease "cli" commit ("registry/image@sha256:"+String.replicate 64 "b") (DateTimeOffset(2026,10,13,0,0,0,TimeSpan.Zero)) state state true [] []
                PackagePublishing.writePlan path plan
                let pinned = PackagePublishing.readPlan path
                PackagePublishing.validatePinnedPlan "cli" commit pinned
                Expect.equal pinned plan "retry consumes the complete original plan, including image/versions/promotion choice"
                Expect.throws (fun () -> PackagePublishing.validatePinnedPlan "docs" commit pinned) "selection cannot change on retry"
                Expect.throws (fun () -> PackagePublishing.validatePinnedPlan "cli" (String.replicate 40 "c") pinned) "source cannot change on retry"
                Expect.throws (fun () -> PackagePublishing.validatePinnedPlan "cli" commit {pinned with image="registry/image:latest"}) "mutable image cannot enter the transaction"
                Expect.throws (fun () -> PackagePublishing.validatePinnedPlan "cli" commit {pinned with promoteDocs=true}) "package-only retry cannot silently promote Docs"
            finally
                if Directory.Exists directory then Directory.Delete(directory,true)
        }

        test "Production preview summary distinguishes Docs promotion from package-only publication" {
            let state : PackagePublishing.PackageState = { latestVersion=Some "2026.10.4"; changedSinceLatest=true }
            let commit = String.replicate 40 "a"
            let image = "registry/image@sha256:"+String.replicate 64 "b"
            let infrastructure = String.replicate 40 "c"
            let currentImage = "registry/image@sha256:"+String.replicate 64 "d"
            use production = System.Text.Json.JsonDocument.Parse($"""{{"releaseCommit":"{String.replicate 40 "e"}","imageDigest":"{currentImage}","coreVersion":"2026.8.2","cliVersion":"unreleased","ciAccessClientSecret":"must-not-appear"}}""")
            let plan selection = PackagePublishing.planRelease selection commit image (DateTimeOffset(2026,11,7,0,0,0,TimeSpan.Zero)) state state true [] []
            let docs = PackagePublishing.releasePreviewSummary infrastructure (plan "auto") production.RootElement
            for value in [currentImage;image;commit;infrastructure;"FSharp.ViewEngine <code>2026.11.0</code>";"FSharp.ViewEngine.Cli <code>2026.11.0</code>"] do
                Expect.stringContains docs value "the actual candidate, production identity and selected publications are reviewable"
            Expect.isFalse (docs.Contains("must-not-appear")) "only public identity fields reach the summary"
            Expect.isFalse (docs.Contains("saved plan")) "the preview does not claim to constrain the later update"
            let packageOnly = PackagePublishing.releasePreviewSummary infrastructure (plan "cli") production.RootElement
            Expect.stringContains packageOnly "No production Docs deployment is selected" "package-only releases do not invent a production deployment"
            Expect.stringContains packageOnly $"| Immutable image | <code>{currentImage}</code> | <code>{currentImage}</code> |" "Docs keeps its actual image"
            Expect.stringContains packageOnly "| Advertised Core version | <code>2026.8.2</code> | <code>2026.8.2</code> |" "Docs metadata does not switch to the candidate's unreleased snapshot"
            Expect.isFalse (packageOnly.Contains(image)) "an unused candidate image is not proposed for production"
            Expect.throws (fun () -> PackagePublishing.releasePreviewSummary "main" (plan "docs") production.RootElement |> ignore) "the infrastructure revision is immutable"
        }

        test "FAKE writes the production summary from the pinned transaction without changing it" {
            let directory = Path.Combine(Path.GetTempPath(), $"fve-preview-summary.{Guid.NewGuid():N}")
            Directory.CreateDirectory directory |> ignore
            try
                let planPath = Path.Combine(directory,"transaction.json")
                let productionPath = Path.Combine(directory,"production.json")
                let summaryPath = Path.Combine(directory,"summary.md")
                let state : PackagePublishing.PackageState = { latestVersion=Some "2026.10.4"; changedSinceLatest=false }
                let plan = PackagePublishing.planRelease "docs" (String.replicate 40 "a") ("registry/image@sha256:"+String.replicate 64 "b") DateTimeOffset.UtcNow state state true [] []
                PackagePublishing.writePlan planPath plan
                let pinnedBytes = File.ReadAllBytes planPath
                File.WriteAllText(productionPath,"{}")
                File.WriteAllText(summaryPath,"Existing summary\n")
                let arguments = [typeof<Release.Metadata>.Assembly.Location;"--target";"WriteReleasePreviewSummary";"--single-target"]
                let environment = [
                    "RELEASE_PLAN_PATH",planPath; "PRODUCTION_STATE_PATH",productionPath
                    "GITHUB_SHA",String.replicate 40 "c"; "GITHUB_STEP_SUMMARY",summaryPath
                    "DEPLOY_IMAGE",plan.image; "RELEASE_COMMIT",plan.commit
                    "CORE_PACKAGE_VERSION",plan.docsCoreVersion; "CLI_PACKAGE_VERSION",plan.docsCliVersion
                    "PROMOTE_DOCS","true" ]
                let result = runProcessWithEnvironment directory "dotnet" arguments environment
                Expect.equal result 0 "the real FAKE entry reads the pinned files and writes the job summary"
                Expect.equal (File.ReadAllBytes planPath) pinnedBytes "summary generation cannot reallocate release identity"
                let summary = File.ReadAllText summaryPath
                Expect.stringContains summary "Existing summary\n## Production release preview" "other job summary content is preserved"
                Expect.stringContains summary "None (Docs only)" "Docs has no package version of its own"
                Expect.stringContains summary "not recorded" "missing legacy metadata is shown truthfully rather than fabricated"
                let drifted = environment |> List.map (fun (key,value) -> key, if key = "DEPLOY_IMAGE" then "" else value)
                Expect.notEqual (runProcessWithEnvironment directory "dotnet" arguments drifted) 0 "a dropped or changed job output cannot silently preview a different candidate or rebuild an image"
                Expect.equal (File.ReadAllText summaryPath) summary "mismatched preview inputs fail before writing approval evidence"
            finally
                Directory.Delete(directory,true)
        }

        test "Accepted candidate identity is exact, protected, ready and successfully deployed" {
            let commit = String.replicate 40 "a"
            let image = "registry/image@sha256:"+String.replicate 64 "b"
            PackagePublishing.validateCandidate commit commit image "https://fve.meiermade.net" true
            for accepted,source,digest,origin,ready in [
                String.replicate 40 "c",commit,image,"https://fve.meiermade.net",true
                commit,"main",image,"https://fve.meiermade.net",true
                commit,commit,"registry/image:latest","https://fve.meiermade.net",true
                commit,commit,image,"https://fve.meiermade.com",true
                commit,commit,image,"https://fve.meiermade.net",false ] do
                Expect.throws (fun () -> PackagePublishing.validateCandidate accepted source digest origin ready) "unaccepted or drifted identity fails closed"
        }

        test "Independent selection pins unselected public package versions" {
            let state latestVersion changedSinceLatest : PackagePublishing.PackageState =
                { latestVersion = latestVersion
                  changedSinceLatest = changedSinceLatest }
            let coreUnchanged = state (Some "2026.8.2") false
            let cliUnpublished = state None true
            let cli = PackagePublishing.validateSelection "cli" "2026.8.2" "2026.9.0"
            PackagePublishing.validateCoherence cli coreUnchanged cliUnpublished
            PackagePublishing.validateCoherence cli (state (Some "2026.8.2") true) cliUnpublished
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
            PackagePublishing.validateCoherence docs coreUnchanged (state (Some "2026.8.4") true)
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
            let publish = workflow "release.yml"
            Expect.stringContains publish "type: choice" "package selection is a choice"
            for selection in [ "core"; "cli"; "both"; "docs" ] do
                Expect.stringContains publish $"- {selection}" $"{selection} selection"
            Expect.stringContains publish "default: auto" "changed projects are the default"
            Expect.stringContains publish "ResolveReleaseTransaction" "FAKE owns source/selection/version policy"
            Expect.isFalse (publish.Contains("inputs.candidateCommit") || publish.Contains("inputs.candidateImage") || publish.Contains("inputs.coreVersion") || publish.Contains("inputs.cliVersion")) "operators do not supply release identity/version inputs"
            Expect.isFalse (publish.Contains("componentsVersion:")) "the retired Components version is not selected"
            Expect.isFalse (publish.Contains("docsVersion:")) "no new Docs package versions"
            Expect.isFalse (publish.Contains("Publish FSharp.ViewEngine.Docs")) "no Docs publication path"
            Expect.isFalse (publish.Contains("Publish FSharp.ViewEngine.Components")) "no Components publication path"
            Expect.stringContains publish "needs.candidate.outputs.packages == 'cli' || needs.candidate.outputs.packages == 'both'" "resolved selection controls CLI"
            Expect.stringContains publish "needs.candidate.outputs.packages == 'core' || needs.candidate.outputs.packages == 'both'" "resolved selection controls Core"
            Expect.isFalse (File.Exists(workflowPath "publish-docs.yml")) "there is no second package-publishing entry point"
            Expect.isFalse (File.Exists(workflowPath "_publish-package.yml")) "single-use reusable workflow is removed"
            Expect.isFalse (File.Exists(workflowPath "verify-nuget-auth.yml")) "publication owns its OIDC authentication"
            Expect.isFalse (publish.Contains("secrets: inherit")) "publication does not inherit unrelated secrets"
        }

        test "Package workflow verifies one release bundle before ordered publication" {
            let publish = workflow "release.yml"
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
            Expect.stringContains publishBlock "cmp \"$RUNNER_TEMP/transaction/transaction.json\" \"$RUNNER_TEMP/release/transaction.json\"" "publication verifies the complete pinned transaction"
            Expect.isFalse (publishBlock.Contains("./fake.sh Pack")) "publication reuses verified bytes rather than repacking"
            Expect.isFalse (publish.Contains("uses: ./.github/workflows/deploy-staging-docs.yml")) "release promotion remains explicit"
            Expect.isFalse (publish.Contains("secrets: inherit")) "publication does not inherit unrelated secrets"

            Expect.stringContains packageBlock "./fake.sh Test --single-target" "release source is tested once"
            Expect.stringContains packageBlock "release-plan-${{ github.run_id }}" "bundle consumes the previously pinned transaction"
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
            let publish = workflow "release.yml"
            let config = repositoryFile "pulumi/src/config.ts"
            let retirementCheck = repositoryFile "e2e/scripts/verify-package-retirement.mjs"
            Expect.stringContains publish "queue: max" "explicit releases retain their serialized queue position"
            Expect.stringContains publish "cancel-in-progress: false" "an active release is never preempted"
            Expect.stringContains publish "needs: [candidate, acceptance]" "new package preparation waits for deployed acceptance"
            Expect.stringContains publish "needs.candidate.outputs.promoteDocs == 'true'" "package-only releases do not implicitly promote Docs"
            Expect.stringContains publish "needs.candidate.outputs.bundle == 'true'" "retries reuse an already verified bundle"
            Expect.stringContains publish "image: ${{ needs.candidate.outputs.image }}" "production receives the accepted digest"
            Expect.stringContains publish "environment: release" "publication uses its protected environment"
            Expect.stringContains config "https://fve.meiermade.com" "production has one canonical origin"
            Expect.stringContains config "ALLOW_UNRELEASED_PACKAGE_SNAPSHOT" "Docs-only snapshots can truthfully advertise unpublished package changes"
            Expect.stringContains retirementCheck "retiredPackages" "retirement evidence covers Components and Docs"
            Expect.stringContains retirementCheck "availableVersions" "retirement evidence covers every published version"
            Expect.stringContains retirementCheck "includes('Legacy')" "retirement evidence requires the Legacy reason"
            Expect.stringContains retirementCheck "FSharp.ViewEngine.Cli" "retirement evidence requires the CLI replacement"
            Expect.stringContains retirementCheck "entry.packageContent" "retirement evidence preserves pinned downloads"
        }

        test "Main deploys only a protected immutable staging candidate" {
            let deploy = workflow "deploy-staging-docs.yml"
            Expect.stringContains deploy "push:" "main changes deploy automatically"
            Expect.stringContains deploy "- main" "only main deploys automatically"
            Expect.stringContains deploy "workflow_dispatch:" "staging remains manually redeployable"
            Expect.stringContains deploy "name: staging" "deployment uses the staging GitHub environment"
            Expect.stringContains deploy "url: https://fve.meiermade.net" "deployment names the protected staging origin"
            Expect.stringContains deploy "stack-name: meiermade/fsharp-view-engine/dev" "main updates only the isolated dev stack"
            Expect.isFalse (deploy.Contains("stack-name: meiermade/fsharp-view-engine/prod")) "main never updates production"
            Expect.stringContains deploy "pulumi env run meiermade/fsharpviewengine/e2e-dev" "acceptance loads scoped credentials in the consuming command"
            Expect.stringContains deploy "bash scripts/test-release-smoke.sh" "main runs the same bounded smoke as production"
            Expect.stringContains deploy "queue: max" "every successful merge retains its place in the staging queue"
            Expect.stringContains deploy "cancel-in-progress: false" "a newer merge does not interrupt an active rollout"
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

        test "FAKE pins accepted staging once and refuses to reallocate a retry" {
            let root = Path.Combine(Path.GetTempPath(), $"fve-transaction.{Guid.NewGuid():N}")
            let repository = Path.Combine(root,"repository")
            let bin = Path.Combine(root,"bin")
            Directory.CreateDirectory repository |> ignore
            Directory.CreateDirectory bin |> ignore
            let git arguments = PackagePublishing.runProcess true "git" (["-C";repository] @ arguments)
            let calls = Path.Combine(root,"calls")
            let savedPlan = Path.Combine(root,"saved.json")
            let planPath = Path.Combine(root,"transaction","transaction.json")
            let output = Path.Combine(root,"output")
            let script name (content:string) =
                let path = Path.Combine(bin,name)
                File.WriteAllText(path,content)
                File.SetUnixFileMode(path,UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)
            try
                git ["init";"--initial-branch=main"] |> ignore
                git ["config";"user.email";"ci@meiermade.com"] |> ignore
                git ["config";"user.name";"Release test"] |> ignore
                for directory in ["sln/src/FSharp.ViewEngine";"sln/src/FSharp.ViewEngine.Cli"] do
                    Directory.CreateDirectory(Path.Combine(repository,directory)) |> ignore
                    File.WriteAllText(Path.Combine(repository,directory,"source.fs"),"published")
                git ["add";"."] |> ignore
                git ["-c";"commit.gpgSign=false";"commit";"-m";"published"] |> ignore
                git ["tag";"v2026.8.2"] |> ignore
                git ["tag";"cli/v2026.10.4"] |> ignore
                let publishedCommit = git ["rev-parse";"HEAD"]
                File.WriteAllText(Path.Combine(repository,"sln/src/FSharp.ViewEngine.Cli/source.fs"),"pending")
                git ["add";"."] |> ignore
                git ["-c";"commit.gpgSign=false";"commit";"-m";"accepted"] |> ignore
                let commit = git ["rev-parse";"HEAD"]
                git ["remote";"add";"origin";repository] |> ignore
                script "gh" """#!/usr/bin/env bash
set -euo pipefail
printf 'gh %s %s\n' "$1" "${2:-}" >> "$FAKE_CALLS"
if [[ "$1" == api && "$2" == *'/actions/workflows/'* ]]; then
  if [[ " $* " == *' --paginate '* && " $* " == *' --slurp '* ]]; then
    printf '%s' "$FAKE_ACCEPTED"
  elif [[ " $* " == *' --paginate '* ]]; then jq -c '.[]' <<< "$FAKE_ACCEPTED"
  else jq '.[0]' <<< "$FAKE_ACCEPTED"; fi
elif [[ "$1" == api ]]; then printf '%s' "$FAKE_ARTIFACTS"
elif [[ "$1 $2" == 'run list' ]]; then
  jq '[.[].workflow_runs[] | select(.head_branch == "main")] | sort_by(.created_at) | reverse | .[:1] | map({headSha: .head_sha, status, conclusion})' <<< "$FAKE_ACCEPTED" | jq -er "${@: -1}"
elif [[ "$1 $2" == 'run download' ]]; then mkdir -p "${@: -1}"; cp "$FAKE_SAVED_PLAN" "${@: -1}/transaction.json"
else exit 91; fi
"""
                script "pulumi" """#!/usr/bin/env bash
set -euo pipefail
printf 'pulumi\n' >> "$FAKE_CALLS"
if [[ " $* " == *' --json '* ]]; then
  printf '{"releaseCommit":"%s","imageDigest":"%s","origin":"https://fve.meiermade.net","e2eReady":true}' "$FAKE_STAGED" "$FAKE_IMAGE"
else printf '%s' "$FAKE_PUBLISHED"; fi
"""
                let image = "registry/image@sha256:"+String.replicate 64 "b"
                let mutable latestStatus = "completed"
                let mutable latestConclusion = "success"
                let mutable newerCreatedConclusion = "success"
                let mutable newerStarted = "2026-11-07T11:00:00Z"
                let run attempt artifacts staged =
                    let rerun = {| head_sha=commit; head_branch="main"; created_at="2026-11-07T10:00:00Z"; run_started_at="2026-11-07T12:00:00Z"; status=latestStatus; conclusion=latestConclusion |}
                    let newerCreated = {| rerun with created_at="2026-11-07T11:00:00Z"; run_started_at=newerStarted; status="completed"; conclusion=newerCreatedConclusion |}
                    let otherBranch = {| rerun with head_branch="feature"; run_started_at="2026-11-07T13:00:00Z"; status="completed"; conclusion="failure" |}
                    let history = System.Text.Json.JsonSerializer.Serialize [| {| workflow_runs=[|newerCreated|] |}; {| workflow_runs=[|rerun|] |}; {| workflow_runs=[|otherBranch|] |} |]
                    File.WriteAllText(calls,"")
                    if File.Exists planPath then File.Delete planPath
                    let info = ProcessStartInfo("dotnet")
                    info.WorkingDirectory <- root
                    info.UseShellExecute <- false
                    info.RedirectStandardOutput <- true
                    info.RedirectStandardError <- true
                    for argument in [typeof<Release.Metadata>.Assembly.Location;"--target";"ResolveReleaseTransaction";"--single-target"] do info.ArgumentList.Add argument
                    for key,value in [
                        "PATH",bin+string Path.PathSeparator+Environment.GetEnvironmentVariable("PATH")
                        "RELEASE_REPOSITORY",repository; "RELEASE_PLAN_PATH",planPath
                        "GITHUB_REPOSITORY","meiermade/FSharp.ViewEngine"; "GITHUB_RUN_ID","4242"
                        "GITHUB_RUN_ATTEMPT",string attempt; "GITHUB_REF","refs/heads/main"; "GITHUB_SHA",commit
                        "GITHUB_OUTPUT",output; "PACKAGE_SELECTION","docs"; "FAKE_CALLS",calls
                        "FAKE_ARTIFACTS",artifacts; "FAKE_ACCEPTED",history; "FAKE_STAGED",staged
                        "FAKE_IMAGE",image; "FAKE_PUBLISHED",publishedCommit; "FAKE_SAVED_PLAN",savedPlan ] do info.Environment[key] <- value
                    use child = Process.Start info
                    let stdout = child.StandardOutput.ReadToEndAsync()
                    let stderr = child.StandardError.ReadToEndAsync()
                    if not (child.WaitForExit 30000) then child.Kill(true); failwith "Release target did not terminate."
                    child.ExitCode, stdout.Result+stderr.Result
                let rejected,_ = run 1 "" (String.replicate 40 "c")
                Expect.notEqual rejected 0 "a drifted staging candidate cannot create a plan"
                Expect.isFalse (File.Exists planPath) "no allocation before candidate acceptance"
                let initial,diagnostics = run 1 "" commit
                Expect.equal initial 0 diagnostics
                let plan = PackagePublishing.readPlan planPath
                Expect.equal (plan.commit,plan.image,plan.packages,plan.docsCliVersion) (commit,image,"docs","unreleased") "Docs is independent and truthful"
                File.Copy(planPath,savedPlan)
                for status,conclusion in ["completed","failure";"completed","cancelled";"in_progress",""] do
                    latestStatus <- status
                    latestConclusion <- conclusion
                    let failed,_ = run 1 "" commit
                    Expect.notEqual failed 0 "an older-created run retried last must not fall back to the newer-created success for the same commit, even across API pages"
                    Expect.isFalse (File.Exists planPath) "unaccepted staging never creates a release transaction"
                latestStatus <- "completed"
                latestConclusion <- "success"
                newerCreatedConclusion <- "failure"
                let fixedAttempt,diagnostics = run 1 "" commit
                Expect.equal fixedAttempt 0 $"The latest actual attempt succeeds even when a newer-created run previously failed: {diagnostics}"
                newerStarted <- "2026-11-07T12:00:00Z"
                let ambiguous,_ = run 1 "" commit
                Expect.notEqual ambiguous 0 "a failed attempt tied for latest start time must fail closed"
                let resumed,diagnostics = run 2 "release-plan-4242\npackage-release-4242\n" (String.replicate 40 "d")
                Expect.equal resumed 0 diagnostics
                Expect.equal (PackagePublishing.readPlan planPath) plan "newer staging cannot change the pinned transaction"
                Expect.isFalse ((File.ReadAllText calls).Contains("pulumi")) "verified bundle retries do not query newer staging"
                Expect.stringContains (File.ReadAllText output) "bundle=true" "verified package bytes are reused"
                let missing,_ = run 2 "" commit
                Expect.notEqual missing 0 "missing retry artifacts cannot mint another release"
                Expect.isFalse (File.Exists planPath) "retry fails before creating another transaction"
                Expect.isFalse ((File.ReadAllText calls).Contains("pulumi")) "retry failure happens before candidate resolution"
            finally
                Directory.Delete(root,true)
        }

        test "PR browser matrix retains all regression shards" {
            let previewShards = workflow "validate-pr.yml" |> browserShards
            Expect.isNonEmpty previewShards "PR acceptance selects browser shards"

            for browser, total in [ "chromium", 6; "firefox", 3; "webkit", 3 ] do
                let shards = previewShards |> List.filter (fun shard -> shard.browser = browser)
                let totals = shards |> List.map _.total |> List.distinct
                Expect.equal totals [ total ] $"{browser} retains its full shard total"
                let indexes = shards |> List.map _.index |> List.sort
                Expect.equal indexes [ 1 .. total ] $"{browser} shards are complete and unique"
        }

        test "Privileged Pulumi preview excludes fork pull requests" {
            let preview = workflow "validate-pr.yml"
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
            for name in [ "deploy-staging-docs.yml"; "validate-pr.yml"; "deploy-production-docs.yml" ] do
                let workflow = workflow name
                Expect.stringContains workflow expectedAction $"{name} pins setup-gcloud"
                Expect.stringContains workflow "install_components: gke-gcloud-auth-plugin" $"{name} installs GKE authentication"
        }

        test "Staging delivery uses current provider state and safe rollout boundaries" {
            let deploy = workflow "deploy-staging-docs.yml"
            let preview = workflow "validate-pr.yml"
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
            Expect.isFalse (preview.Contains("stack-name: meiermade/fsharp-view-engine/prod")) "production previews belong to the pinned release, not pull requests"
            Expect.stringContains pulumiDev "fsharpviewengine/dev" "the dev stack composes only the development ESC environment"
            Expect.stringContains appConfig "https://fve.meiermade.net" "the staging origin is exact"
            Expect.stringContains appConfig "zoneId: rawCloudflareConfig.require('zoneId')" "both stacks require their ESC-projected zone ID"
            Expect.isFalse (zone.Contains("getZoneOutput")) "application stacks do not rediscover configured Cloudflare zones"
            Expect.stringContains appConfig "DEPLOY_IMAGE must be an immutable sha256 image reference" "promotion rejects mutable images"
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
