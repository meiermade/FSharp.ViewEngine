namespace Docs.Common

open System
open System.IO
open System.Reflection

module SourceRegion =
    let private lines (source:string) =
        source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')

    let extract id source =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A source region ID is required."

        let startMarker = $"// docs-example:start {id}"
        let endMarker = $"// docs-example:end {id}"
        let sourceLines = lines source

        let indexes marker =
            sourceLines
            |> Array.indexed
            |> Array.choose (fun (index, line) -> if line.Trim() = marker then Some index else None)

        let starts = indexes startMarker
        let ends = indexes endMarker

        if starts.Length <> 1 || ends.Length <> 1 || ends[0] <= starts[0] then
            invalidArg (nameof id) $"Source region '{id}' must have exactly one ordered start and end marker."

        let region = sourceLines[(starts[0] + 1)..(ends[0] - 1)]
        let contentLines = region |> Array.filter (String.IsNullOrWhiteSpace >> not)
        let indentation =
            if Array.isEmpty contentLines then 0
            else
                contentLines
                |> Array.map (fun line -> line.Length - line.TrimStart().Length)
                |> Array.min

        region
        |> Array.map (fun line ->
            if String.IsNullOrWhiteSpace line then ""
            elif line.Length >= indentation then line.Substring(indentation)
            else line)
        |> String.concat "\n"
        |> _.Trim('\n')

    /// Supporting definitions from the four-space-indented Docs source modules.
    /// Preserve source order so the setup and example can compile together.
    let declarations names source =
        let sourceLines = lines source
        let declaration = System.Text.RegularExpressions.Regex("^    (?:let (?:private )?|type )([A-Za-z][A-Za-z0-9_']*)\\b")
        let definitions =
            sourceLines
            |> Array.indexed
            |> Array.choose (fun (index, line) ->
                let matched = declaration.Match line
                if matched.Success then Some(index, matched.Groups[1].Value) else None)
        for name in names do
            if definitions |> Array.filter (fun (_, candidate) -> candidate = name) |> Array.length <> 1 then
                invalidArg (nameof names) $"Supporting definition '{name}' must exist exactly once."
        definitions
        |> Array.indexed
        |> Array.choose (fun (position, (start, name)) ->
            if not (List.contains name names) then None
            else
                let finish = if position + 1 < definitions.Length then fst definitions[position + 1] else sourceLines.Length
                sourceLines[start..finish - 1]
                |> Array.filter (fun line -> not (line.TrimStart().StartsWith("// docs-example:", StringComparison.Ordinal)))
                |> Array.map (fun line -> if line.StartsWith("    ", StringComparison.Ordinal) then line[4..] else line)
                |> String.concat "\n"
                |> _.Trim()
                |> Some)
        |> String.concat "\n\n"

    let readEmbedded (assembly:Assembly) resourceName =
        use stream = assembly.GetManifestResourceStream resourceName
        if isNull stream then
            invalidArg (nameof resourceName) $"Embedded source resource '{resourceName}' was not found."
        use reader = new StreamReader(stream)
        reader.ReadToEnd()
