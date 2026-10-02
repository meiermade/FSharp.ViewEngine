namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

open System.Security.Cryptography
open System.Text

module private CodeStyles =
    let code = "m-0! overflow-x-auto rounded-xl border border-[var(--fve-border)] bg-[var(--fve-docs-code-surface,var(--fve-background))]! p-4 font-mono! text-sm! leading-[1.55]! text-[var(--fve-text)]! [text-shadow:none]! shadow-sm"

module internal CopyableCode =
    let refreshAfterMorph = "if (evt.detail.type === 'datastar-patch-elements') queueMicrotask(() => { if (el.isConnected) window.renderCode?.(el) })"

    let private sourceFingerprint (source:string) =
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes source))

    let renderSource (source:string) =
        Render.toString (Html.text source)
        |> _.Replace("\r", "&#13;").Replace("\n", "&#10;")
        |> Html.raw

    let renderButton (label:string) =
        button {
            _type "button"
            _ariaLabel label
            _title label
            _class "group absolute top-3 right-3 z-2 grid size-8 min-w-8 cursor-pointer place-items-center rounded-md border border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] p-0 text-xs leading-4 whitespace-nowrap text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] data-[copy-error=true]:border-red-600 data-[copy-error=true]:text-red-700 dark:data-[copy-error=true]:text-red-300"
            _data("on:click", "window.fsharpDocsCopy(evt.currentTarget)")
            raw """<svg class="size-4 group-data-[copied=true]:hidden group-data-[copy-error=true]:hidden" data-docs-copy-icon="copy" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path d="M5.5 2.75A2.75 2.75 0 0 0 2.75 5.5v7A2.75 2.75 0 0 0 5.5 15.25h1.75a.75.75 0 0 0 0-1.5H5.5c-.69 0-1.25-.56-1.25-1.25v-7c0-.69.56-1.25 1.25-1.25h5c.69 0 1.25.56 1.25 1.25v1.75a.75.75 0 0 0 1.5 0V5.5a2.75 2.75 0 0 0-2.75-2.75h-5Z"/><path d="M9.5 8.25A2.75 2.75 0 0 0 6.75 11v4.5a2.75 2.75 0 0 0 2.75 2.75h5A2.75 2.75 0 0 0 17.25 15.5V11a2.75 2.75 0 0 0-2.75-2.75h-5Zm-1.25 2.75c0-.69.56-1.25 1.25-1.25h5c.69 0 1.25.56 1.25 1.25v4.5c0 .69-.56 1.25-1.25 1.25h-5c-.69 0-1.25-.56-1.25-1.25V11Z"/></svg>"""
            raw """<svg class="hidden size-4 group-data-[copied=true]:block" data-docs-copy-icon="success" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 0 1 .143 1.051l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.479-9.816a.75.75 0 0 1 1.051-.143Z" clip-rule="evenodd"/></svg>"""
            raw """<svg class="hidden size-4 group-data-[copy-error=true]:block" data-docs-copy-icon="error" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm0-12.75a.75.75 0 0 1 .75.75v4a.75.75 0 0 1-1.5 0V6a.75.75 0 0 1 .75-.75ZM10 14a1 1 0 1 0 0-2 1 1 0 0 0 0 2Z" clip-rule="evenodd"/></svg>"""
            span {
                _data("docs-copy-label", "true")
                _class "sr-only"
                _role "status"
            }
        }

    let render (className:string) (codeClass:string) (label:string) (source:string) =
        div {
            _class "relative min-w-0"
            _data("docs-copyable-code", "true")
            _data("init", $"window.renderCode?.(el, '{sourceFingerprint source}')")
            _data("on:datastar-fetch", refreshAfterMorph)
            renderButton $"Copy {label}"
            pre { _class className; _tabindex 0; code { _class codeClass; _data("docs-copy-source", "true"); renderSource source } }
        }

/// <category>code-block</category>
[<NoEquality; NoComparison>]
type CodeBlockConfig = private { language:string; source:string }

/// <category>code-block</category>
[<RequireQualifiedAccess>]
module CodeBlock =
    /// Include once in the document head. URLs point to consumer-owned Prism assets.
    let assets stylesheet scripts = CodeAssets.render stylesheet scripts

    let create language source : CodeBlockConfig =
        if String.IsNullOrWhiteSpace source then invalidArg (nameof source) "Code source cannot be empty."
        { language = if String.IsNullOrWhiteSpace language then "text" else language.Trim().ToLowerInvariant()
          source = source }

    let render (block:CodeBlockConfig) =
        let prismLanguage = if block.language = "fs" then "fsharp" else block.language
        CopyableCode.render ($"{CodeStyles.code} language-{prismLanguage}") ($"bg-transparent! font-mono! text-sm! leading-[1.55]! text-[var(--fve-text)]! [text-shadow:none]! language-{prismLanguage}") "code" block.source
