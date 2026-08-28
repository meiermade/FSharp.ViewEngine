namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type EmptyStateConfig =
    private
        { title:string
          description:string
          icon:HtmlElement option
          actions:HtmlElement option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module EmptyState =
    let create title description =
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "An empty-state title is required."
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "An empty-state description is required."
        { title = title; description = description; icon = None; actions = None; attributes = [] }

    let withIcon icon (config:EmptyStateConfig) = { config with icon = Some icon }
    let withActions actions (config:EmptyStateConfig) = { config with actions = Some actions }
    let withAttributes attributes (config:EmptyStateConfig) = { config with attributes = attributes }

    let render config =
        div {
            _class "grid justify-items-center gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface-subtle)] p-6 text-center ring-1 ring-inset ring-[var(--fve-border)]"
            for attribute in ComponentHtml.safeAttributes [ "class" ] config.attributes do attribute
            match config.icon with
            | Some icon ->
                span {
                    _ariaHidden "true"
                    _class "grid size-11 place-items-center rounded-full bg-[var(--fve-neutral-subtle)] text-[var(--fve-neutral-text)] ring-1 ring-inset ring-[var(--fve-neutral-ring)]"
                    icon
                }
            | None -> ()
            div {
                _class "max-w-xl"
                p { _class "font-semibold text-[var(--fve-text)]"; config.title }
                p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; config.description }
            }
            match config.actions with
            | Some actions -> div { _class "mt-1 flex flex-wrap justify-center gap-2"; actions }
            | None -> ()
        }
