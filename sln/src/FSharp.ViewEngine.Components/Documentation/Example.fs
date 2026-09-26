namespace FSharp.ViewEngine.Components.Documentation

open FSharp.ViewEngine
open System
open type Html

module Example =
    let private render gallery codeFirst (id:string) (label:string) (language:string) (source:string) (preview:HtmlElement) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "An example ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An example label is required."

        let token =
            id
            |> Seq.map (fun character -> if Char.IsLetterOrDigit character then character else '_')
            |> Seq.toArray
            |> String

        let signal = $"{token}Example"
        let initial = if codeFirst then "code" else "preview"
        let normalized = if String.IsNullOrWhiteSpace language then "text" else language.Trim().ToLowerInvariant()
        let prismLanguage = if normalized = "fs" then "fsharp" else normalized
        let tabId name = $"{id}-tab-{name}"
        let panelId name = $"{id}-panel-{name}"
        let select name = $"${signal} = '{name}'"
        let activate name =
            if name = "code" then $", queueMicrotask(() => window.renderCode?.(document.getElementById('{panelId name}')))"
            else $", queueMicrotask(() => window.renderDocsPreview?.(document.getElementById('{panelId name}')))"
        let focus name = $"{select name}, document.getElementById('{tabId name}').focus(){activate name}"
        let focusPreview = focus "preview"
        let focusCode = focus "code"

        let frameClasses =
            if gallery then
                "relative min-w-0 overflow-visible bg-transparent"
            else
                "relative min-w-0 overflow-hidden rounded-xl border border-[var(--fve-border)] bg-[var(--fve-surface)] has-[.fve-components]:overflow-visible"
        let toolbarClasses =
            if gallery then
                "flex min-h-12 flex-wrap items-center justify-between gap-2 pb-3"
            else
                "flex min-h-12 items-center justify-between gap-4 border-b border-[var(--fve-border)] py-1.5 pr-2 pl-4"
        let titleClasses =
            if gallery then
                "m-0 min-w-0 flex-[1_1_8rem] text-base font-semibold text-[var(--fve-text)]"
            else
                "min-w-0 overflow-hidden text-ellipsis whitespace-nowrap text-sm font-semibold text-[var(--fve-text)]"
        let panelClasses =
            if gallery then
                "min-h-40 overflow-hidden rounded-xl border border-[var(--fve-border)] bg-[var(--fve-page)] p-6 has-[[data-fve-full-bleed-example=true]]:p-0 has-[[data-page-example-preview=true]]:overflow-visible has-[[data-page-example-preview=true]]:rounded-none has-[[data-page-example-preview=true]]:border-0"
            else
                "min-h-40 bg-[var(--fve-page)] p-6 has-[[data-fve-full-bleed-example=true]]:p-0 has-[[data-page-example-preview=true]]:overflow-visible has-[[data-page-example-preview=true]]:rounded-none has-[[data-page-example-preview=true]]:border-0"
        let codeClasses =
            let frame = if gallery then "rounded-xl border border-[var(--fve-border)]" else "border-0 rounded-none"
            $"relative min-h-40 overflow-auto bg-[var(--fve-neutral-subtle)] p-4 font-mono text-sm leading-[1.55] text-[var(--fve-text)] {frame} language-{prismLanguage}"

        section {
            _class frameClasses
            _data("docs-copyable-code", "true")
            _data("docs-example", "true")
            _data("signals", $"{{ {signal}: '{initial}' }}")
            div {
                _class toolbarClasses
                _data("docs-example-toolbar", "true")
                if gallery then h2 { _class titleClasses; _data("docs-example-title", "true"); label }
                else div { _class titleClasses; _data("docs-example-title", "true"); label }
                div {
                    _role "tablist"
                    _ariaLabel label
                    _class "flex shrink-0 items-center rounded-[0.625rem] bg-[var(--fve-surface-hover)] p-[0.1875rem]"
                    let tabs = [ "code", "Code", "preview"; "preview", "Preview", "code" ]
                    for name, tabLabel, next in (if gallery then List.rev tabs else tabs) do
                        let isSelected = name = initial
                        let dynamicAction = activate name
                        button {
                            _id (tabId name)
                            _type "button"
                            _role "tab"
                            _ariaControls (panelId name)
                            _ariaSelected isSelected
                            _data("attr:aria-selected", $"${signal} == '{name}' ? 'true' : 'false'")
                            _data("attr:data-selected", $"${signal} == '{name}' ? 'true' : null")
                            _data("attr:tabindex", $"${signal} == '{name}' ? 0 : -1")
                            _data("on:click", $"{select name}{dynamicAction}")
                            _data("on:keydown", $"(evt.key == 'ArrowLeft' || evt.key == 'ArrowRight') && (evt.preventDefault(), {focus next}); evt.key == 'Home' && (evt.preventDefault(), {if gallery then focusPreview else focusCode}); evt.key == 'End' && (evt.preventDefault(), {if gallery then focusCode else focusPreview})")
                            _class "cursor-pointer rounded-[0.4375rem] border-0 bg-transparent px-3 py-1.5 text-sm font-semibold text-[var(--fve-muted-text)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-[var(--fve-brand-ring)] data-[selected=true]:bg-[var(--fve-surface)] data-[selected=true]:text-[var(--fve-text)] data-[selected=true]:shadow-sm"
                            tabLabel
                        }
                }
            }
            div {
                _id (panelId "preview")
                _role "tabpanel"
                _attr("aria-labelledby", tabId "preview")
                _class panelClasses
                _data("docs-example-preview", "true")
                _data("show", $"${signal} == 'preview'")
                if codeFirst then _style "display:none"
                else _data("docs-preview-initial", "true")
                preview
            }
            pre {
                _id (panelId "code")
                _role "tabpanel"
                _attr("aria-labelledby", tabId "code")
                _tabindex 0
                _class codeClasses
                _data("docs-example-code", "true")
                _data("show", $"${signal} == 'code'")
                if not codeFirst then _style "display:none"
                CopyableCode.renderButton $"Copy {label} code"
                code {
                    _class $"language-{prismLanguage}"
                    _data("docs-copy-source", "true")
                    source
                }
            }
        }

    let previewFirst id label language source preview = render false false id label language source preview
    let codeFirst id label language source preview = render false true id label language source preview
    /// A named, preview-first gallery item with copyable code and an h2 toolbar title.
    let gallery id label language source preview = render true false id label language source preview
