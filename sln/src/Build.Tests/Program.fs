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

let private workflow name =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "..", ".github", "workflows", name))
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

        test "Reusable publish workflow selects jobs from package input" {
            let workflow = workflow "_publish-package.yml"
            Expect.stringContains workflow "if: inputs.packageId == ''" "direct dispatch runs only the OIDC smoke test"
            Expect.equal
                (workflow.Split("if: inputs.packageId != ''", StringSplitOptions.None).Length - 1)
                2
                "workflow calls run package and publish jobs"
            Expect.isFalse (workflow.Contains("if: github.event_name ==")) "caller event does not select reusable jobs"

            let uploadStart = workflow.IndexOf("      - name: Upload verified package", StringComparison.Ordinal)
            let publishStart = workflow.IndexOf("\n  publish:", uploadStart, StringComparison.Ordinal)
            let uploadBlock = workflow.Substring(uploadStart, publishStart - uploadStart)
            Expect.stringContains workflow "cp \"$RELEASE_METADATA_PATH\" nugets/release-metadata.json" "metadata is staged with package assets"
            Expect.stringContains uploadBlock "nugets/release-metadata.json" "staged metadata is uploaded"
            Expect.isFalse (uploadBlock.Contains("${{ runner.temp }}")) "artifact paths have one common package root"
        }

        test "Docs publication deploys and verifies before package publication" {
            let reusable = workflow "_publish-package.yml"
            let core = workflow "publish.yml"
            let docs = workflow "publish-docs.yml"
            let deploy = workflow "deploy.yml"

            Expect.stringContains reusable "deployDocs:" "reusable deployment input"
            Expect.stringContains core "deployDocs: false" "Core publication remains package-only"
            Expect.stringContains docs "deployDocs: true" "Docs publication deploys the site"

            let deployStart = reusable.IndexOf("\n  deploy-docs:", StringComparison.Ordinal)
            let publishStart = reusable.IndexOf("\n  publish:", StringComparison.Ordinal)
            Expect.isTrue (deployStart > 0 && publishStart > deployStart) "deployment job precedes publication"

            let deployBlock = reusable.Substring(deployStart, publishStart - deployStart)
            Expect.stringContains deployBlock "uses: ./.github/workflows/deploy.yml" "deployment uses the reusable workflow"
            Expect.stringContains deployBlock "ref: ${{ needs.package.outputs.commit }}" "deployment checks out the verified commit"
            Expect.stringContains deployBlock "expectedVersion: ${{ needs.package.outputs.version }}" "health expects the Docs version"
            Expect.stringContains deployBlock "expectedCommit: ${{ needs.package.outputs.commit }}" "health expects the verified commit"

            let publishBlock = reusable.Substring(publishStart)
            Expect.stringContains publishBlock "- deploy-docs" "publication waits for deployment"
            Expect.stringContains publishBlock "needs.deploy-docs.result == 'success'" "failed deployment blocks publication"
            Expect.stringContains deploy "workflow_call:" "deployment remains reusable"
            Expect.stringContains deploy "workflow_dispatch:" "deployment remains manually dispatchable"
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
