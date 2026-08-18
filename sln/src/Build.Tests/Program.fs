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

let tests =
    testList "Package publishing" [
        test "Core release inputs select Core and Latest" {
            let inputs = PackagePublishing.validateInputs "FSharp.ViewEngine" "2026.8.2" None true
            Expect.equal inputs.package PackagePublishing.Package.ViewEngine "Core package"
            Expect.equal inputs.version "2026.8.2" "Core version"
            Expect.isNone inputs.minimumCoreVersion "Core has no Core dependency"
            Expect.isTrue inputs.markLatest "Core is Latest"
        }

        test "Docs release inputs require Core and remain non-Latest" {
            let inputs = PackagePublishing.validateInputs "FSharp.ViewEngine.Docs" "2026.8.1" (Some "2026.8.2") false
            Expect.equal inputs.package PackagePublishing.Package.Docs "Docs package"
            Expect.equal inputs.minimumCoreVersion (Some "2026.8.2") "minimum Core"
            Expect.isFalse inputs.markLatest "Docs is not Latest"
        }

        testCase "Invalid package release inputs fail" <| fun _ ->
            let invalidCases = [
                fun () -> PackagePublishing.validateInputs "Other" "2026.8.0" None false |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine" "2026.8.0" (Some "2026.8.0") true |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Docs" "2026.8.0" None false |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine.Docs" "2026.8.0" (Some "2026.8.0") true |> ignore
                fun () -> PackagePublishing.validateInputs "FSharp.ViewEngine" "preview" None true |> ignore
            ]
            for invalid in invalidCases do Expect.throws invalid "invalid release input"

        test "Release selection validates conditional package versions" {
            let core = PackagePublishing.validateSelection "core" (Some "2026.8.3") None None
            Expect.isSome core.core "Core selected"
            Expect.isNone core.docs "Docs package not selected"

            let docs = PackagePublishing.validateSelection "docs" None (Some "2026.8.2") (Some "2026.8.3")
            Expect.isNone docs.core "Core not selected"
            Expect.isSome docs.docs "Docs package selected"

            let both =
                PackagePublishing.validateSelection
                    "both"
                    (Some "2026.8.3")
                    (Some "2026.8.2")
                    (Some "2026.8.3")
            Expect.isSome both.core "Core selected together"
            Expect.isSome both.docs "Docs package selected together"
        }

        testCase "Invalid conditional package versions fail" <| fun _ ->
            let invalidCases = [
                fun () -> PackagePublishing.validateSelection "core" None None None |> ignore
                fun () -> PackagePublishing.validateSelection "core" (Some "2026.8.3") (Some "2026.8.2") None |> ignore
                fun () -> PackagePublishing.validateSelection "docs" None (Some "2026.8.2") None |> ignore
                fun () -> PackagePublishing.validateSelection "docs" (Some "2026.8.3") (Some "2026.8.2") (Some "2026.8.3") |> ignore
                fun () -> PackagePublishing.validateSelection "both" (Some "2026.8.3") None (Some "2026.8.3") |> ignore
                fun () -> PackagePublishing.validateSelection "other" None None None |> ignore
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
            for selection in [ "core"; "docs"; "both" ] do
                Expect.stringContains publish $"- {selection}" $"{selection} selection"
            Expect.stringContains publish "coreVersion:" "independent Core version"
            Expect.stringContains publish "docsVersion:" "independent Docs package version"
            Expect.stringContains publish "minimumCoreVersion:" "Docs minimum Core version"
            Expect.isFalse (File.Exists(workflowPath "publish-docs.yml")) "there is no second package-publishing entry point"
        }

        test "NuGet authentication diagnostics remain separate from publication" {
            let reusable = workflow "_publish-package.yml"
            let verification = workflow "verify-nuget-auth.yml"
            Expect.isFalse (reusable.Contains("workflow_dispatch:")) "internal package operations cannot be dispatched directly"
            Expect.stringContains verification "workflow_dispatch:" "trusted publishing can be checked manually"
            Expect.stringContains verification "NuGet/login@8d196754b4036150537f80ac539e15c2f1028841" "verification uses trusted publishing"
            Expect.stringContains verification "test -n \"$NUGET_API_KEY\"" "verification requires a temporary API key"
        }

        test "Selected packages verify before one site deployment and ordered publication" {
            let reusable = workflow "_publish-package.yml"
            let deploy = workflow "deploy.yml"

            let coreStart = reusable.IndexOf("\n  package-core:", StringComparison.Ordinal)
            let docsStart = reusable.IndexOf("\n  package-docs:", StringComparison.Ordinal)
            let deployStart = reusable.IndexOf("\n  deploy-site:", StringComparison.Ordinal)
            let publishStart = reusable.IndexOf("\n  publish:", StringComparison.Ordinal)
            Expect.isTrue
                (coreStart > 0 && docsStart > coreStart && deployStart > docsStart && publishStart > deployStart)
                "both package jobs precede the single site deployment and publication"

            Expect.equal
                (reusable.Split("uses: ./.github/workflows/deploy.yml", StringSplitOptions.None).Length - 1)
                1
                "the site deploys once"

            let deployBlock = reusable.Substring(deployStart, publishStart - deployStart)
            Expect.stringContains deployBlock "expectedCoreVersion:" "health identifies a selected Core release"
            Expect.stringContains deployBlock "expectedDocsVersion:" "health identifies a selected Docs package release"
            Expect.stringContains deployBlock "expectedCommit:" "health identifies the verified commit"

            let publishBlock = reusable.Substring(publishStart)
            let publishCore = publishBlock.IndexOf("Publish FSharp.ViewEngine", StringComparison.Ordinal)
            let publishDocs = publishBlock.IndexOf("Publish FSharp.ViewEngine.Docs", StringComparison.Ordinal)
            Expect.isTrue (publishCore > 0 && publishDocs > publishCore) "Core publishes before the dependent Docs package"
            Expect.stringContains publishBlock "needs.deploy-site.result == 'success'" "failed site acceptance blocks publication"
            Expect.stringContains deploy "workflow_call:" "site deployment remains reusable"
            Expect.stringContains deploy "workflow_dispatch:" "site deployment remains manually dispatchable"
        }

        test "Verified package artifacts preserve one download root" {
            let reusable = workflow "_publish-package.yml"
            let uploads =
                [ "      - name: Upload verified Core package", "\n  package-docs:"
                  "      - name: Upload verified Docs package", "\n  deploy-site:" ]

            for uploadName, nextJob in uploads do
                let uploadStart = reusable.IndexOf(uploadName, StringComparison.Ordinal)
                let uploadEnd = reusable.IndexOf(nextJob, uploadStart, StringComparison.Ordinal)
                let uploadBlock = reusable.Substring(uploadStart, uploadEnd - uploadStart)
                Expect.stringContains uploadBlock "nugets/release-metadata.json" $"{uploadName} includes release metadata"
                Expect.isFalse (uploadBlock.Contains("${{ runner.temp }}")) $"{uploadName} has one artifact root"
        }

        test "Pulumi workflows install the GKE credential plugin" {
            let expectedAction = "google-github-actions/setup-gcloud@aa5489c8933f4cc7a4f7d45035b3b1440c9c10db # v3.0.1"
            for name in [ "deploy.yml"; "preview.yml" ] do
                let workflow = workflow name
                Expect.stringContains workflow expectedAction $"{name} pins setup-gcloud"
                Expect.stringContains workflow "install_components: gke-gcloud-auth-plugin" $"{name} installs GKE authentication"
        }

        test "Changelog requires exactly one selected release" {
            let text = "title = \"FSharp.ViewEngine 2026.8.2 · August 14, 2026\""
            PackagePublishing.validateChangelog "FSharp.ViewEngine" "2026.8.2" text
            Expect.throws
                (fun () -> PackagePublishing.validateChangelog "FSharp.ViewEngine.Docs" "2026.8.2" text)
                "missing Docs release"
            Expect.throws
                (fun () -> PackagePublishing.validateChangelog "FSharp.ViewEngine" "2026.8.2" $"{text}\n{text}")
                "duplicate Core release"
        }

        test "Expected release assets are exact" {
            Expect.sequenceEqual
                (PackagePublishing.expectedAssetNames "FSharp.ViewEngine.Docs" "2026.8.0")
                [ "FSharp.ViewEngine.Docs.2026.8.0.nupkg"
                  "FSharp.ViewEngine.Docs.2026.8.0.snupkg"
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
                run [ "tag"; "docs/v2026.8.0" ]

                let metadata = Release.prepare directory (Path.Combine(directory, "release.json")) "v" "2026.8.1"
                Expect.equal metadata.previousTag (Some "v2026.8.0") "previous Core tag excludes Docs"
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
