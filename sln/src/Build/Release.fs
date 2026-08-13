module Release

open System
open System.Globalization
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Fake.Tools.Git

[<Struct>]
type private CalendarVersion =
    { year:int
      month:int
      minor:int }

    override this.ToString() = $"{this.year}.{this.month}.{this.minor}"

type Metadata =
    { tag:string
      version:string
      commit:string
      previousTag:string option }

let private versionPattern = Regex("^v?(?<year>[0-9]{4})\\.(?<month>[0-9]{1,2})\\.(?<minor>[0-9]+)$")

let private tagPattern tagPrefix =
    Regex($"^{Regex.Escape tagPrefix}(?<version>[0-9]{{4}}\\.[0-9]{{1,2}}\\.[0-9]+)$")

let private parseVersion (value:string) =
    let matched = versionPattern.Match value
    if not matched.Success then
        invalidArg (nameof value) $"Invalid calendar version: {value}. Expected YYYY.M.MINOR."

    let number (group:string) = Int32.Parse(matched.Groups[group].Value, CultureInfo.InvariantCulture)
    let version =
        { year = number "year"
          month = number "month"
          minor = number "minor" }

    if version.month < 1 || version.month > 12 then
        invalidArg (nameof value) $"Invalid calendar month in version: {value}"

    version

let private gitValue repository command =
    CommandHelper.runSimpleGitCommand repository command

let private knownTags (repository:string) (tagPrefix:string) =
    let pattern = tagPattern tagPrefix

    CommandHelper.getGitResult repository "tag --list"
    |> List.choose (fun tag ->
        let matched = pattern.Match tag
        if not matched.Success then None
        else
            let version = parseVersion matched.Groups["version"].Value
            let commit = gitValue repository $"rev-list -n 1 {tag}"
            Some(tag, version, commit))

let private versionKey (version:CalendarVersion) = version.year, version.month, version.minor

let private resolveMetadata (repository:string) (tagPrefix:string) (requestedVersion:string) =
    if String.IsNullOrWhiteSpace tagPrefix then
        invalidArg (nameof tagPrefix) "Release tag prefix cannot be empty."

    let commit = Information.getCurrentSHA1 repository
    let tags = knownTags repository tagPrefix
    let version = parseVersion requestedVersion

    let tag =
        match tags |> List.tryFind (fun (_, existingVersion, _) -> versionKey existingVersion = versionKey version) with
        | Some(tag, _, existingCommit) when existingCommit <> commit ->
            raise (InvalidOperationException $"Release tag {tag} already points to {existingCommit}, not {commit}")
        | Some(tag, _, _) -> tag
        | None -> $"{tagPrefix}{version}"

    let previousTag =
        tags
        |> List.filter (fun (_, existingVersion, _) -> versionKey existingVersion < versionKey version)
        |> List.sortBy (fun (_, existingVersion, _) -> versionKey existingVersion)
        |> List.tryLast
        |> Option.map (fun (existingTag, _, _) -> existingTag)

    { tag = tag
      version = version.ToString()
      commit = commit
      previousTag = previousTag }

let writeMetadata (path:string) (metadata:Metadata) =
    let directory = Path.GetDirectoryName(Path.GetFullPath path)
    Directory.CreateDirectory directory |> ignore

    let options = JsonSerializerOptions(WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    File.WriteAllText(path, JsonSerializer.Serialize(metadata, options) + Environment.NewLine)

let readMetadata (path:string) =
    use document = JsonDocument.Parse(File.ReadAllText path)
    let root = document.RootElement
    let previousTag =
        match root.GetProperty("previousTag") with
        | value when value.ValueKind = JsonValueKind.String -> value.GetString() |> Option.ofObj
        | _ -> None

    { tag = root.GetProperty("tag").GetString()
      version = root.GetProperty("version").GetString()
      commit = root.GetProperty("commit").GetString()
      previousTag = previousTag }

let prepare (repository:string) (outputPath:string) (tagPrefix:string) (version:string) =
    let metadata = resolveMetadata repository tagPrefix version
    writeMetadata outputPath metadata
    metadata
