namespace Docs.Pages

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
          dependencies: string list }

    let private registry =
        use stream =
            typeof<DocPage>.Assembly
                .GetManifestResourceStream("Docs.ComponentRegistry.props")

        XDocument.Load(stream).Descendants()
        |> Seq.choose (fun element ->
            let name = element.Attribute(XName.Get "FveName")

            if isNull name then
                None
            else
                Some(
                    name.Value,
                    { file = element.Attribute(XName.Get "FveFile").Value
                      dependencies =
                        element
                            .Attribute(XName.Get "FveDependencies")
                            .Value.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        |> Array.toList }
                ))
        |> Map.ofSeq

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
        |> Seq.filter (fun name -> name.EndsWith("-script", StringComparison.Ordinal))
        |> Seq.map (fun name -> registry[name].file)
        |> Set.ofSeq

    let private code language (source: string) =
        fveCodeBlock {
            _language language
            source
        }

    let private inlineCode (value: string) =
        Html.code {
            _class "[overflow-wrap:anywhere]"
            value
        }

    let private link href (label: string) =
        a {
            _href href
            _class "font-semibold text-[var(--fve-brand-text)] underline underline-offset-2"
            label
        }

    let private imports files =
        [ p {
              "Add to "
              inlineCode "src/Acme.Web/js/app.js"
              " once, alongside your other component imports:"
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
              if dependencies.Contains "code-assets" then
                  p { "Include this once in your document head to enable copying:" }
                  code "fsharp" "CodeBlock.assets None []"

                  p {
                      "For optional syntax highlighting and nonce CSP, see "
                      link "/components/code-block#syntax-highlighting" "Code block setup"
                      "."
                  }
              if dependencies.Contains "mermaid-assets" then
                  p {
                      "Serve the "
                      link "https://mermaid.js.org/intro/getting-started.html" "Mermaid browser bundle"
                      " as "
                      inlineCode "wwwroot/scripts/mermaid.js"
                      ", then include this once in your document head:"
                  }

                  code "fsharp" "Mermaid.assets \"/scripts/mermaid.js\""

                  p {
                      "For nonce CSP, use "
                      inlineCode "Mermaid.assetsWithNonce"
                      " with your server-issued document nonce; see the "
                      link "/extensions/datastar#content-security-policy" "CSP guide"
                      "."
                  }
              if selector = "chart" then
                  p { "ECharts loads automatically when a nonempty chart connects." }
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
            (exampleCandidates selector).Split(' ', StringSplitOptions.RemoveEmptyEntries)
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
