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
      commit:string }

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

let private resolveMetadata (repository:string) (tagPrefix:string) (now:DateTimeOffset) (versionOverride:string option) =
    if String.IsNullOrWhiteSpace tagPrefix then
        invalidArg (nameof tagPrefix) "Release tag prefix cannot be empty."

    let commit = Information.getCurrentSHA1 repository
    let tags = knownTags repository tagPrefix

    let selectedTag, selectedVersion =
        match versionOverride |> Option.filter (String.IsNullOrWhiteSpace >> not) with
        | Some requested ->
            let version = parseVersion requested

            match tags |> List.tryFind (fun (_, existingVersion, _) -> versionKey existingVersion = versionKey version) with
            | Some(tag, _, existingCommit) when existingCommit <> commit ->
                raise (InvalidOperationException $"Release tag {tag} already points to {existingCommit}, not {commit}")
            | Some(tag, _, _) -> tag, version.ToString()
            | None -> $"{tagPrefix}{version}", version.ToString()
        | None ->
            match tags |> List.filter (fun (_, _, tagCommit) -> tagCommit = commit) |> List.sortBy (fun (_, version, _) -> versionKey version) |> List.tryLast with
            | Some(tag, version, _) -> tag, version.ToString()
            | None ->
                let year = now.Year
                let month = now.Month
                let nextMinor =
                    tags
                    |> List.choose (fun (_, version, _) ->
                        if version.year = year && version.month = month then Some version.minor else None)
                    |> function
                        | [] -> 0
                        | minors -> List.max minors + 1

                let version =
                    { year = year
                      month = month
                      minor = nextMinor }

                $"{tagPrefix}{version}", version.ToString()

    { tag = selectedTag
      version = selectedVersion
      commit = commit }

let private writeMetadata (path:string) (metadata:Metadata) =
    let directory = Path.GetDirectoryName(Path.GetFullPath path)
    Directory.CreateDirectory directory |> ignore

    let options = JsonSerializerOptions(WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    File.WriteAllText(path, JsonSerializer.Serialize(metadata, options) + Environment.NewLine)

let readMetadata (path:string) =
    use document = JsonDocument.Parse(File.ReadAllText path)
    let root = document.RootElement

    { tag = root.GetProperty("tag").GetString()
      version = root.GetProperty("version").GetString()
      commit = root.GetProperty("commit").GetString() }

let prepare (repository:string) (outputPath:string) (tagPrefix:string) (versionOverride:string option) =
    let metadata = resolveMetadata repository tagPrefix DateTimeOffset.UtcNow versionOverride
    writeMetadata outputPath metadata
    metadata

let tag (repository:string) (metadata:Metadata) =
    let actualCommit = Information.getCurrentSHA1 repository
    if actualCommit <> metadata.commit then
        raise (InvalidOperationException $"Release metadata identifies {metadata.commit}, but HEAD is {actualCommit}")

    let existingCommit =
        CommandHelper.getGitResult repository "tag --list"
        |> List.tryFind (fun existingTag -> existingTag = metadata.tag)
        |> Option.map (fun existingTag -> gitValue repository $"rev-list -n 1 {existingTag}")

    match existingCommit with
    | Some commit when commit <> metadata.commit ->
        raise (InvalidOperationException $"Release tag {metadata.tag} already points to {commit}, not {metadata.commit}")
    | Some _ -> ()
    | None -> Branches.tag repository metadata.tag

    Branches.pushTag repository "origin" metadata.tag
