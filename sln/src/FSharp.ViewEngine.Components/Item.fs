namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>item</category>
[<RequireQualifiedAccess>]
type ItemVariant =
    | Plain
    | Outlined
    | Muted

/// <category>item</category>
[<NoEquality; NoComparison>]
type ItemConfig =
    private
        { title:string
          description:HtmlElement option
          media:HtmlElement option
          metadata:HtmlElement option
          actions:HtmlElement option
          href:string option
          variant:ItemVariant
          attributes:HtmlAttribute list }

/// <remarks>
/// withLink stretches the title link without wrapping media, descriptions or metadata in an anchor.
/// Supplied slot interactions remain independent. An explicit action cluster keeps the Item plain.
/// </remarks>
/// <category>item</category>
[<RequireQualifiedAccess>]
module Item =
    /// Creates a plain content row with a required visible title.
    let create title =
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "An item title is required."
        { title = title
          description = None
          media = None
          metadata = None
          actions = None
          href = None
          variant = ItemVariant.Plain
          attributes = [] }

    let withDescription description (config:ItemConfig) = { config with description = Some description }
    let withMedia media (config:ItemConfig) = { config with media = Some media }
    let withMetadata metadata (config:ItemConfig) = { config with metadata = Some metadata }
    let withActions actions (config:ItemConfig) = { config with actions = Some actions }

    /// Makes the title a stretched row link. Supplied media, description and metadata
    /// remain separate interaction regions outside the anchor.
    let withLink href (config:ItemConfig) =
        if String.IsNullOrWhiteSpace href then invalidArg (nameof href) "A link destination is required."
        { config with href = Some href }

    let withVariant variant (config:ItemConfig) = { config with variant = variant }
    let withAttributes attributes (config:ItemConfig) = { config with attributes = attributes }

    let private body config =
        fragment {
            match config.media with
            | Some media -> div { _class "relative z-10 flex shrink-0 items-start"; media }
            | None -> ()
            div {
                _class "min-w-0 flex-1"
                p {
                    _class "font-semibold text-[var(--fve-text)] [overflow-wrap:anywhere]"
                    match config.href with
                    | Some href ->
                        a {
                            _href href
                            _class "no-underline outline-none before:absolute before:inset-0 before:rounded-[inherit] focus-visible:before:ring-2 focus-visible:before:ring-[var(--fve-brand-ring)]"
                            config.title
                        }
                    | None -> config.title
                }
                match config.description with
                | Some description -> div { _class "relative z-10 mt-1 text-sm leading-6 text-[var(--fve-muted-text)] [overflow-wrap:anywhere]"; description }
                | None -> ()
            }
            match config.metadata with
            | Some metadata -> div { _class "relative z-10 min-w-0 shrink-0 text-sm text-[var(--fve-muted-text)] max-sm:w-full max-sm:pl-12"; metadata }
            | None -> ()
        }

    let render (config:ItemConfig) =
        if config.href.IsSome && config.actions.IsSome then
            invalidOp "A whole-row Item link cannot also contain actions. Use a plain Item with explicit link and action content instead."
        let variantClasses =
            match config.variant with
            | ItemVariant.Plain -> ""
            | ItemVariant.Outlined -> "rounded-[var(--fve-radius-panel)] ring-1 ring-inset ring-[var(--fve-border)]"
            | ItemVariant.Muted -> "rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface-subtle)]"
        li {
            _class ("relative min-w-0 list-none " + variantClasses)
            for attribute in ComponentHtml.safeAttributes [ "class" ] config.attributes do attribute
            match config.href with
            | Some _ ->
                div {
                    _class "flex min-w-0 flex-wrap items-start gap-3 rounded-[inherit] p-4 transition-colors hover:bg-[var(--fve-surface-hover)]"
                    body config
                }
            | None ->
                div {
                    _class "flex min-w-0 flex-wrap items-start gap-3 p-4"
                    body config
                    match config.actions with
                    | Some actions -> div { _class "ml-auto flex shrink-0 items-center gap-2"; actions }
                    | None -> ()
                }
        }
