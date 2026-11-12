namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open System
open type Html

/// <category>example</category>
[<RequireQualifiedAccess>]
type ExampleVariant = PreviewFirst | CodeFirst | Lead | Gallery

/// <category>example</category>
[<AutoOpen>]
module ExampleAttributes =
    type Html with
        static member _variant(value:ExampleVariant) = Html._attr("variant", match value with ExampleVariant.PreviewFirst -> "preview-first" | ExampleVariant.CodeFirst -> "code-first" | ExampleVariant.Lead -> "lead" | ExampleVariant.Gallery -> "gallery")

type internal ExampleCodeElement(attributes:HtmlAttribute list, source:string) =
    inherit HtmlElement()
    member _.Attributes = attributes
    member _.Source = source
    override _.Render _ = invalidOp "fveExampleCode belongs inside fveExample."

module internal Example =
    let private render gallery showTitle codeFirst (id:string) (label:string) (language:string) (source:string) (preview:HtmlElement) attributes codeAttributes =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "An example ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An example label is required."

        let initial = if codeFirst then "code" else "preview"
        let normalized = if String.IsNullOrWhiteSpace language then "text" else language.Trim().ToLowerInvariant()
        let prismLanguage = if normalized = "fs" then "fsharp" else normalized
        let tabId name = $"{id}-tab-{name}"
        let panelId name = $"{id}-panel-{name}"
        let tabs =
            if gallery then [ "preview", "Preview"; "code", "Code" ]
            else [ "code", "Code"; "preview", "Preview" ]

        let frameClasses =
            if gallery then
                "relative min-w-0 overflow-visible bg-transparent"
            else
                "relative min-w-0 overflow-hidden rounded-xl border border-[var(--fve-border)] bg-[var(--fve-surface)] has-[.fve-components]:overflow-visible"
        let toolbarClasses =
            if gallery then
                "flex min-h-10 flex-wrap items-center justify-between gap-2 pb-2"
            else
                "flex min-h-10 items-center justify-between gap-2 border-b border-[var(--fve-border)] py-1 pr-2 pl-3"
        let titleClasses =
            if gallery then
                "m-0 min-w-0 flex-[1_1_8rem] text-base font-semibold text-[var(--fve-text)]"
            else
                "min-w-0 overflow-hidden text-ellipsis whitespace-nowrap text-sm font-semibold text-[var(--fve-text)]"
        let panelClasses = "min-h-40 overflow-visible"
        let previewFrameClasses =
            let frame = if gallery then "rounded-xl border border-[var(--fve-border)]" else "border-0 rounded-none"
            "relative box-border min-h-40 min-w-[min(15rem,100%)] max-w-full overflow-hidden bg-[var(--fve-background)] p-[12px] sm:p-6 " + frame + " has-[[data-fve-full-bleed-example=true]]:p-0 has-[[data-page-example-preview=true]]:overflow-visible has-[[data-page-example-preview=true]]:rounded-none has-[[data-page-example-preview=true]]:border-0"
        let codeClasses =
            let frame = if gallery then "rounded-xl border border-[var(--fve-border)]" else "border-0 rounded-none"
            $"relative m-0! min-h-40 overflow-auto bg-[var(--fve-docs-code-surface,color-mix(in_oklch,var(--fve-background)_72%%,var(--fve-surface-subtle)))]! p-4 font-mono! text-sm! leading-[1.55]! text-[var(--fve-text)]! [text-shadow:none]! {frame} language-{prismLanguage}"

        (Html.el "fve-example") {
            let defaults = [_class ("block " + frameClasses); _data("docs-example", "true"); _attr("initial-tab", initial)]
            for attribute in ComponentElement.attributes (defaults @ attributes) do attribute
            div {
                _class toolbarClasses
                _data("docs-example-toolbar", "true")
                if gallery then h2 { _class (if showTitle then titleClasses else "sr-only"); _data("docs-example-title", "true"); label }
                else div { _class titleClasses; _data("docs-example-title", "true"); label }
                div {
                    _class "ml-auto flex w-full min-w-0 max-w-full flex-wrap items-center justify-between gap-2 sm:w-auto sm:justify-end"
                    div {
                        _role "group"
                        _ariaLabel (label + " preview width")
                        _attr("data-fve-example-widths", "")
                        _hidden codeFirst
                        _class "flex items-center gap-0.5"
                        button {
                            _type "button"
                            _ariaLabel "Desktop preview"
                            _title "Desktop preview"
                            _ariaPressed true
                            _attr("data-fve-example-width", "100%")
                            _class "grid size-8 place-items-center rounded-md text-[var(--fve-muted-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] aria-pressed:bg-[var(--fve-surface-hover)] aria-pressed:text-[var(--fve-text)]"
                            raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M2.5 4A1.5 1.5 0 0 1 4 2.5h12A1.5 1.5 0 0 1 17.5 4v8A1.5 1.5 0 0 1 16 13.5h-5.25V16h2.5a.75.75 0 0 1 0 1.5h-6.5a.75.75 0 0 1 0-1.5h2.5v-2.5H4A1.5 1.5 0 0 1 2.5 12V4Zm1.5 0v8h12V4H4Z" clip-rule="evenodd"/></svg>"""
                        }
                        button {
                            _type "button"
                            _ariaLabel "Mobile preview"
                            _title "Mobile preview"
                            _ariaPressed false
                            _attr("data-fve-example-width", "390px")
                            _class "grid size-8 place-items-center rounded-md text-[var(--fve-muted-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] aria-pressed:bg-[var(--fve-surface-hover)] aria-pressed:text-[var(--fve-text)]"
                            raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M6.25 1.75A2.25 2.25 0 0 0 4 4v12a2.25 2.25 0 0 0 2.25 2.25h7.5A2.25 2.25 0 0 0 16 16V4a2.25 2.25 0 0 0-2.25-2.25h-7.5ZM5.5 4c0-.414.336-.75.75-.75h7.5c.414 0 .75.336.75.75v10.5h-9V4Zm0 12v-.25h9V16a.75.75 0 0 1-.75.75h-7.5A.75.75 0 0 1 5.5 16Z" clip-rule="evenodd"/></svg>"""
                        }
                    }
                    div {
                    _role "tablist"
                    _ariaLabel label
                    _class "flex min-w-0 max-w-full flex-wrap items-center rounded-[0.625rem] bg-[var(--fve-surface-hover)] p-0.5"
                    for name, tabLabel in tabs do
                        let isSelected = name = initial
                        button {
                            _id (tabId name)
                            _type "button"
                            _role "tab"
                            _ariaControls (panelId name)
                            _ariaSelected isSelected
                            _tabindex (if isSelected then 0 else -1)
                            _attr("data-fve-example-tab", name)
                            if isSelected then _attr("data-selected", "true")
                            _class "cursor-pointer rounded-[0.4375rem] border-0 bg-transparent px-2.5 py-1 text-sm font-semibold text-[var(--fve-muted-text)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-[var(--fve-brand-ring)] data-[selected=true]:bg-[var(--fve-surface)] data-[selected=true]:text-[var(--fve-text)] data-[selected=true]:shadow-sm"
                            tabLabel
                        }
                    }
                }
            }
            div {
                _id (panelId "preview")
                _role "tabpanel"
                _attr("aria-labelledby", tabId "preview")
                _class panelClasses
                _data("docs-example-preview", "true")
                _hidden codeFirst
                if not codeFirst then _data("docs-preview-initial", "true")
                div {
                    _class previewFrameClasses
                    _style "width:100%;max-width:100%;min-width:min(15rem,100%)"
                    _attr ("data-docs-preview-frame", "true")
                    preview
                    div {
                        _role "separator"
                        _tabindex 0
                        _ariaLabel ("Resize " + label + " preview")
                        _ariaOrientation "vertical"
                        _ariaValuemin "240"
                        _ariaValuemax "1920"
                        _ariaValuenow "0"
                        _attr("data-fve-example-resize", "")
                        _class "group absolute inset-y-0 right-0 z-20 flex w-4 touch-none cursor-col-resize items-center justify-center outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                        span { _ariaHidden true; _class "h-12 w-1 rounded-full bg-[var(--fve-border)] shadow-sm transition-colors group-hover:bg-[var(--fve-muted-text)]" }
                    }
                }
            }
            (Html.el "fve-code-block") {
                _id (panelId "code")
                _role "tabpanel"
                _attr("aria-labelledby", tabId "code")
                _class ("block relative min-w-0 " + CopyableCode.syntaxClasses)
                _data("docs-example-code", "true")
                _data("docs-copyable-code", "true")
                _hidden (not codeFirst)
                pre {
                    _class codeClasses
                    _tabindex 0
                    code {
                        for attribute in ComponentElement.attributes ([_class $"bg-transparent! font-mono! text-sm! leading-[1.55]! text-[var(--fve-text)]! [text-shadow:none]! language-{prismLanguage}"; _data("docs-copy-source", "true")] @ codeAttributes) do attribute
                        CopyableCode.renderSource source
                    }
                }
                CopyableCode.renderButton $"Copy {label} code"
            }
        }

    let code attributes children = ExampleCodeElement(attributes, ComponentContent.plainText "fveExampleCode" children) :> HtmlElement

    let element attributes (children:HtmlElement list) =
        // The name scopes inner tab/panel IDs. A separate optional id belongs to the native section.
        let name = ComponentElement.required "name" attributes
        if name |> Seq.exists Char.IsWhiteSpace then invalidArg "name" "An example name cannot contain whitespace."
        let label = ComponentElement.required "label" attributes
        let sources, preview =
            children |> List.fold (fun (sources, preview) child -> match child with :? ExampleCodeElement as source -> source :: sources, preview | _ -> sources, child :: preview) ([], [])
        let source = match sources with [source] -> source | _ -> invalidArg "children" "Exactly one fveExampleCode is required."
        let language = ComponentElement.value "language" source.Attributes |> Option.orElse (ComponentElement.value "language" attributes) |> Option.defaultValue "text"
        let variant = ComponentElement.choice "variant" "preview-first" ["preview-first"; "code-first"; "lead"; "gallery"] attributes
        render (variant = "lead" || variant = "gallery") (variant <> "lead") (variant = "code-first") name label language source.Source (ComponentContent.fragment (List.rev preview))
            (ComponentHtml.safeAttributes ["name"; "label"; "language"; "variant"] attributes) (ComponentHtml.safeAttributes ["language"] source.Attributes)

/// <category>example</category>
[<AutoOpen>]
module ExampleDsl =
    let fveExample = ComponentElementBuilder(Example.element)
    let fveExampleCode = ComponentElementBuilder(Example.code)
