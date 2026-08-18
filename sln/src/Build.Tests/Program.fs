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
            let workflowPath =
                Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "..", ".github", "workflows", "_publish-package.yml"))
            let workflow = File.ReadAllText workflowPath
            Expect.stringContains workflow "if: inputs.packageId == ''" "direct dispatch runs only the OIDC smoke test"
            Expect.equal
                (workflow.Split("if: inputs.packageId != ''", StringSplitOptions.None).Length - 1)
                2
                "workflow calls run package and publish jobs"
            Expect.isFalse (workflow.Contains("if: github.event_name ==")) "caller event does not select reusable jobs"
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
