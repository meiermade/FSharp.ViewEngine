namespace Example.Components

open System
open System.Text.Json
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html

module internal Chart =
    let json = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

    let render attributes children =
        let id = ComponentElement.required "id" attributes

        if id |> Seq.exists Char.IsWhiteSpace then
            invalidArg "attributes" "A chart ID cannot contain whitespace."

        ComponentElement.required "aria-label" attributes |> ignore
        use options = JsonDocument.Parse(ComponentElement.required "options" attributes)

        if options.RootElement.ValueKind <> JsonValueKind.Object then
            invalidArg
                "attributes"
                "Chart options must be an object. Supply an F# record, anonymous record or JSON object."

        match ComponentElement.value "events" attributes with
        | Some value ->
            let events = JsonSerializer.Deserialize<string array>(value, json)

            if
                isNull events
                || events
                   |> Array.exists (fun name -> isNull name || not (Regex.IsMatch(name, "^[a-z][a-z0-9_-]*$")))
            then
                invalidArg "attributes" "Chart events must be ECharts event names, such as click, datazoom or brushend."
        | None -> ()

        ComponentElement.render
            "fve-chart"
            [ _role "img"
              _class "block min-w-0"
              _attr ("data-preserve-attr", "data-ready data-rocket-host") ]
            (ComponentHtml.safeAttributes [ "data-preserve-attr" ] attributes)
            children

/// <category>chart</category>
[<AutoOpen>]
module ChartAttributes =
    type Html with
        /// JSON-compatible ECharts options. JavaScript callbacks belong in the application's chart-render handler.
        static member _options(value: 'T) =
            Html._attr ("options", JsonSerializer.Serialize(value, Chart.json))

        /// ECharts events to forward as bubbling chart-<name> DOM events, preserving their original detail.
        static member _events(value: string list) =
            Html._attr ("events", JsonSerializer.Serialize(value, Chart.json))

        /// The application decides whether its data is empty; the host does not interpret series or datasets.
        static member _empty(value: bool) =
            Html._attr ("empty", if value then "true" else "false")

/// <summary>Editable Rocket/ECharts host. Import Assets/chart.js; the heavy renderer loads only when a nonempty chart connects.</summary>
/// <category>chart</category>
[<AutoOpen>]
module ChartDsl =
    let fveChart = ComponentElementBuilder(Chart.render)
