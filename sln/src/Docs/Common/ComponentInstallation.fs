namespace Docs.Common

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml.Linq
open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

/// Shared component setup, using the same dependency closure that the CLI installs.
module ComponentInstallation =
    type private Entry =
        { file: string
          dependencies: string list
          modules: string list }

    let private registry =
        use stream =
            typeof<DocPage>.Assembly
                .GetManifestResourceStream("Docs.ComponentRegistry.props")

        let entries = XDocument.Load(stream).Descendants() |> Seq.toList
        let attribute name (element: XElement) =
            let value = element.Attribute(XName.Get name)
            if isNull value then "" else value.Value

        entries
        |> List.choose (fun element ->
            let name = attribute "FveName" element
            if name = "" then None
            else
                Some(
                    name,
                    { file = attribute "FveFile" element
                      dependencies = (attribute "FveDependencies" element).Split(';', StringSplitOptions.RemoveEmptyEntries) |> Array.toList
                      modules =
                        entries
                        |> List.filter (fun file ->
                            (attribute "FveName" file = name || attribute "FveOwner" file = name)
                            && attribute "FveModule" file = "true")
                        |> List.map (attribute "FveFile") }
                ))
        |> Map.ofList

    let private closure selector =
        let rec visit found name =
            if Set.contains name found then
                found
            else
                let found = Set.add name found

                match Map.tryFind name registry with
                | Some entry -> List.fold visit found entry.dependencies
                | None -> found

        visit Set.empty selector

    let private modules (selectors: seq<string>) =
        selectors
        |> Seq.choose (fun name -> Map.tryFind name registry)
        |> Seq.collect _.modules
        |> Set.ofSeq

    let private code language (source: string) =
        fveCodeBlock {
            _language language
            source
        }

    let private link href (label: string) =
        fveLink {
            _href href
            _underline LinkUnderline.Always
            label
        }

    let private imports files =
        [ p {
              "The CLI adds these browser modules to its generated imports file. Load your app entry point once; do not add these imports by hand."
          }
          code
              "javascript"
              (files
               |> Seq.map (fun file -> $"import '../../Acme.Components/Components/{file}';")
               |> String.concat "\n") ]

    let section selector =
        let dependencies = closure selector
        let files = modules dependencies

        DocumentationSection.create
            "installation"
            "Installation"
            [ h2 {
                  _class "text-xl font-semibold text-[var(--fve-text)]"
                  "Installation"
              }
              p { "Copy this component and its required dependencies into the consumer-owned Components project." }
              code "shell" $"dotnet fve add {selector} --config src/Acme.Components/fve.json"
              if Set.isEmpty files then
                  p { "No additional JavaScript import is required." }
              else
                  yield! imports files
              if dependencies.Contains "code-block" then
                  p { "The CLI adds Prism to package.json for syntax highlighting. Run npm install after adding the component; no separate Prism files or head setup are needed." }
              if dependencies.Contains "mermaid" then
                  p { "The CLI adds Mermaid to package.json. Run npm install after adding the component. The renderer loads when a diagram becomes visible." }
              if dependencies.Contains "chart" then
                  p { "The CLI adds ECharts to package.json. Run npm install after adding the component. The renderer loads when a nonempty chart connects." }
              p {
                  _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)]"
                  "The CLI resolves dependencies and preserves consumer-authored files. See "
                  link "/components/installation" "Installation"
                  " for project initialization, Tailwind, the shared runtime, and building/loading your app entry."
              } ]

    // These are optional compositions used by this page's recipes, not component dependencies.
    let private exampleCandidates =
        function
        | "button" -> "dropdown-menu"
        | "table" -> "button dropdown-menu"
        | "input" -> "field select button"
        | "textarea" -> "field"
        | "select" -> "field button"
        | "field" -> "input field-group checkbox"
        | "field-group" -> "checkbox"
        | "checkbox"
        | "switch" -> "field button"
        | "radio-group" -> "button"
        | "toggle-group"
        | "tag-input" -> "field button"
        | "choice-cards" -> "radio-group checkbox"
        | "copy-reveal" -> "field"
        | "dropdown-menu" -> "button field textarea"
        | "popover" -> "button field input"
        | "card"
        | "section-header"
        | "chart" -> "button"
        | "button-group" -> "button link select dropdown-menu popover"
        | "page-top-bar" -> "breadcrumbs side-nav"
        | "page-header" -> "button"
        | "description-list" -> "badge"
        | "file-selection" -> "field"
        | "time-picker" -> "field"
        | "date-picker" -> "field button"
        | "month-calendar" -> "button"
        | "notice"
        | "notification" -> "button"
        | "message" -> "avatar button"
        | "input-group" -> "field input textarea kbd button popover"
        | "error-summary" -> "field input"
        | _ -> ""

    let exampleSetup selector (source: string) =
        let installed = closure selector

        let additional =
            (exampleCandidates selector + " icon").Split(' ', StringSplitOptions.RemoveEmptyEntries)
            |> Array.filter (fun name ->
                let builder = "fve" + Path.GetFileNameWithoutExtension(registry[name].file)

                let pattern =
                    match name with
                    | "field" -> @"\bfve(?:Field|Label|FieldDescription|FieldError)\b"
                    | "select" -> @"\bfve(?:Select|MultiSelect)\b"
                    | _ -> $@"\b{builder}\b"

                not (installed.Contains name) && Regex.IsMatch(source, pattern))
            |> Array.toList

        match additional with
        | [] -> []
        | _ ->
            [ p {
                  "This example also uses "

                  for index, name in List.indexed additional do
                      if index > 0 then
                          ", "

                      link ($"/components/{name}#installation") (Path.GetFileNameWithoutExtension(registry[name].file))

                  ". See their Installation sections for any additional setup."
              } ]
