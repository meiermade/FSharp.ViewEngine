namespace FSharp.ViewEngine.Docs

open System

[<NoEquality; NoComparison>]
type Target<'destination> =
    private
        { destination:'destination
          query:(string * string) list
          fragment:string option }

module Target =
    let create destination =
        { destination = destination
          query = []
          fragment = None }

    let destination target = target.destination

    let query target = target.query

    let fragment target = target.fragment

    let withQuery (name:string) (value:string) target =
        if String.IsNullOrWhiteSpace name then
            invalidArg (nameof name) "Query parameter names cannot be empty."

        let value = if isNull value then "" else value
        let remaining = target.query |> List.filter (fst >> ((<>) name))
        { target with query = remaining @ [ name, value ] }

    let withFragment (fragment:string) target =
        if String.IsNullOrWhiteSpace fragment then
            invalidArg (nameof fragment) "Fragments cannot be empty."

        { target with fragment = Some fragment }

    let href route target =
        let path = route target.destination

        if String.IsNullOrWhiteSpace path then
            invalidArg (nameof route) "Destination routes cannot be empty."

        let query =
            target.query
            |> List.map (fun (name, value) -> $"{Uri.EscapeDataString name}={Uri.EscapeDataString value}")
            |> function
                | [] -> ""
                | values -> "?" + String.concat "&" values

        let fragment =
            target.fragment
            |> Option.map (Uri.EscapeDataString >> (+) "#")
            |> Option.defaultValue ""

        path + query + fragment
