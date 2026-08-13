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

    let readEmbedded (assembly:Assembly) resourceName =
        use stream = assembly.GetManifestResourceStream resourceName
        if isNull stream then
            invalidArg (nameof resourceName) $"Embedded source resource '{resourceName}' was not found."
        use reader = new StreamReader(stream)
        reader.ReadToEnd()
