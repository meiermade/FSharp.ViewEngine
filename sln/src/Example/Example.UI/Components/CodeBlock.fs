namespace Example.Components

open System
open FSharp.ViewEngine
open type Html

module private CodeStyles =
    let code = "m-0! overflow-x-auto rounded-xl border border-[var(--fve-border)] bg-[var(--fve-docs-code-surface,color-mix(in_oklch,var(--fve-background)_72%,var(--fve-surface-subtle)))]! p-4 font-mono! text-sm! leading-[1.55]! text-[var(--fve-text)]! [text-shadow:none]! shadow-sm"

module internal CopyableCode =
    // Prism creates these spans after rendering, so their presentation travels with the source component.
    // light-dark follows the owning ComponentsTheme color scheme, including locally scoped themes.
    let syntaxClasses =
        """[&_:where(.token.comment,.token.prolog,.token.doctype,.token.cdata)]:text-[var(--fve-muted-text)]
        [&_:where(.token.punctuation)]:text-[var(--fve-text)]
        [&_:where(.token.deleted,.token.property,.token.tag,.token.keyword)]:text-[light-dark(var(--color-red-700),var(--color-red-300))]
        [&_:where(.token.boolean,.token.constant,.token.number,.token.symbol)]:text-[light-dark(var(--color-blue-700),var(--color-blue-300))]
        [&_:where(.token.attr-name,.token.builtin,.token.string)]:text-[light-dark(var(--color-green-800),var(--color-green-300))]
        [&_:where(.token.operator)]:text-[light-dark(var(--color-cyan-800),var(--color-cyan-200))]
        [&_:where(.token.class-name,.token.function)]:text-[light-dark(var(--color-purple-700),var(--color-purple-300))]
        [&_:where(.token.variable)]:text-[light-dark(var(--color-orange-800),var(--color-orange-300))]
        [&_:where(.token.important,.token.bold)]:font-bold [&_:where(.token.italic)]:italic"""

    let renderSource (source:string) =
        Render.toString (Html.text source)
        |> _.Replace("\r", "&#13;").Replace("\n", "&#10;")
        |> Html.raw

    let private actionStyle =
        "group grid size-8 min-w-8 cursor-pointer place-items-center rounded-md border border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] p-0 text-xs leading-4 whitespace-nowrap text-[var(--fve-muted-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

    let private copyButton (label:string) position =
        button {
            _type "button"
            _ariaLabel label
            _title label
            _class (actionStyle + position + " data-[copy-error=true]:border-red-600 data-[copy-error=true]:text-red-700 dark:data-[copy-error=true]:text-red-300")
            _attr("data-docs-copy-button", "")
            raw """<svg class="size-4 group-data-[copied=true]:hidden group-data-[copy-error=true]:hidden" data-docs-copy-icon="copy" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path d="M5.5 2.75A2.75 2.75 0 0 0 2.75 5.5v7A2.75 2.75 0 0 0 5.5 15.25h1.75a.75.75 0 0 0 0-1.5H5.5c-.69 0-1.25-.56-1.25-1.25v-7c0-.69.56-1.25 1.25-1.25h5c.69 0 1.25.56 1.25 1.25v1.75a.75.75 0 0 0 1.5 0V5.5a2.75 2.75 0 0 0-2.75-2.75h-5Z"/><path d="M9.5 8.25A2.75 2.75 0 0 0 6.75 11v4.5a2.75 2.75 0 0 0 2.75 2.75h5A2.75 2.75 0 0 0 17.25 15.5V11a2.75 2.75 0 0 0-2.75-2.75h-5Zm-1.25 2.75c0-.69.56-1.25 1.25-1.25h5c.69 0 1.25.56 1.25 1.25v4.5c0 .69-.56 1.25-1.25 1.25h-5c-.69 0-1.25-.56-1.25-1.25V11Z"/></svg>"""
            raw """<svg class="hidden size-4 group-data-[copied=true]:block" data-docs-copy-icon="success" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 0 1 .143 1.051l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.479-9.816a.75.75 0 0 1 1.051-.143Z" clip-rule="evenodd"/></svg>"""
            raw """<svg class="hidden size-4 group-data-[copy-error=true]:block" data-docs-copy-icon="error" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm0-12.75a.75.75 0 0 1 .75.75v4a.75.75 0 0 1-1.5 0V6a.75.75 0 0 1 .75-.75ZM10 14a1 1 0 1 0 0-2 1 1 0 0 0 0 2Z" clip-rule="evenodd"/></svg>"""
            span {
                _data("docs-copy-label", "true")
                _class "sr-only"
                _role "status"
            }
        }

    let renderButton label = copyButton label " absolute top-3 right-3 z-2"

    let private downloadButton fileName =
        button {
            _type "button"
            _hidden true
            _ariaLabel ("Download " + fileName)
            _title ("Download " + fileName)
            _class actionStyle
            _data ("fve-code-download", fileName)
            raw """<svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 3v12m-4-4 4 4 4-4M5 15v4a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-4"/></svg>"""
        }

    let renderWithOptions (className:string) (codeClass:string) (label:string) (source:string) fileName download numbered highlighted attributes =
        let header = Option.isSome fileName || download
        let annotated = numbered || not (Set.isEmpty highlighted)
        let sourceElement (classes:string) = code { _class classes; _data("docs-copy-source", "true"); renderSource source }
        let renderLines () =
            let lines = Text.RegularExpressions.Regex.Split(source, "\\r\\n|\\n|\\r")
            let count = if lines[lines.Length - 1] = "" then lines.Length - 1 else lines.Length
            let gutter = (string count).Length + 2
            span {
                _class "relative block w-max min-w-full whitespace-pre"
                if numbered then _style $"--fve-code-gutter:{gutter}ch"
                span {
                    _class "pointer-events-none absolute inset-0 select-none"
                    _ariaHidden true
                    for index in 1 .. count do
                        span {
                            let emphasized = Set.contains index highlighted
                            _class (if emphasized then "block h-[1lh] bg-[var(--fve-brand-subtle)]" else "block h-[1lh]")
                            if emphasized then _data ("fve-code-highlight", string index)
                            if numbered then
                                span {
                                    _class "inline-block w-[var(--fve-code-gutter)] pe-3 text-end tabular-nums text-[var(--fve-muted-text)]"
                                    _data ("fve-code-line-number", string index)
                                    string index
                                }
                        }
                }
                sourceElement (codeClass + " relative block" + if numbered then " ps-[var(--fve-code-gutter)]!" else "")
            }
        ComponentElement.render "fve-code-block"
            [_class ("block relative min-w-0 " + syntaxClasses); _data("docs-copyable-code", "true")]
            attributes [
                if header then
                    div {
                        _class "flex min-w-0 items-center justify-between gap-3 rounded-t-xl border border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] px-3 py-2"
                        for name in Option.toList fileName do
                            span { _class "min-w-0 truncate font-mono text-sm text-[var(--fve-text)]"; _title name; name }
                        div {
                            _class "ms-auto flex shrink-0 gap-1"
                            if download then
                                let leaf = fileName |> Option.map (fun (name:string) -> IO.Path.GetFileName(name.Replace('\\', '/')))
                                let safeName = leaf |> Option.filter (fun name -> not (String.IsNullOrWhiteSpace name) && name <> "." && name <> "..")
                                downloadButton (safeName |> Option.defaultValue "snippet.txt")
                            copyButton $"Copy {label}" ""
                        }
                    }
                else renderButton $"Copy {label}"
                pre {
                    _class (className + if header then " rounded-t-none! border-t-0!" else "")
                    _tabindex 0
                    if annotated then renderLines () else sourceElement codeClass
                } ]

    let render className codeClass label source attributes =
        renderWithOptions className codeClass label source None false false Set.empty attributes

/// <category>code-block</category>
[<AutoOpen>]
module CodeBlockAttributes =
    type Html with
        static member _language(value:string) = Html._attr("language", value)
        static member _fileName(value:string) = Html._attr("file-name", value)
        static member _download(value:bool) = Html._attr("download", if value then "true" else "false")
        static member _lineNumbers(value:bool) = Html._attr("line-numbers", if value then "true" else "false")
        static member _highlightLines(lines:int list) =
            if lines |> List.exists (fun line -> line < 1) then invalidArg "lines" "Highlighted lines must be 1-based."
            Html._attr("highlight-lines", lines |> List.map string |> String.concat ",")

/// <category>code-block</category>
[<RequireQualifiedAccess>]
module CodeBlock =
    let internal render attributes children =
        let source = ComponentContent.plainText "fveCodeBlock" children
        if String.IsNullOrWhiteSpace source then invalidArg "children" "Code source cannot be empty."
        let language = ComponentElement.value "language" attributes |> Option.filter (String.IsNullOrWhiteSpace >> not) |> Option.map (fun value -> value.Trim().ToLowerInvariant()) |> Option.defaultValue "text"
        let prismLanguage = if language = "fs" then "fsharp" else language
        let fileName = ComponentElement.value "file-name" attributes |> Option.filter (String.IsNullOrWhiteSpace >> not)
        let enabled name = ComponentElement.value name attributes = Some "true"
        let highlighted =
            ComponentElement.value "highlight-lines" attributes
            |> Option.map (fun value -> value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            |> Option.defaultValue [||]
            |> Array.choose (fun value -> match Int32.TryParse value with true, line when line > 0 -> Some line | _ -> None)
            |> Set.ofArray
        let label = fileName |> Option.defaultValue "code"
        CopyableCode.renderWithOptions
            ($"{CodeStyles.code} language-{prismLanguage}")
            ($"bg-transparent! font-mono! text-sm! leading-[1.55]! text-[var(--fve-text)]! [text-shadow:none]! language-{prismLanguage}")
            label source fileName (enabled "download") (enabled "line-numbers") highlighted
            (ComponentHtml.safeAttributes ["language"; "file-name"; "download"; "line-numbers"; "highlight-lines"] attributes)

/// <category>code-block</category>
[<AutoOpen>]
module CodeBlockDsl =
    let fveCodeBlock = ComponentElementBuilder(CodeBlock.render)
