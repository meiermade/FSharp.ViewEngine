namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html

/// <category>message</category>
[<RequireQualifiedAccess>]
type MessageSide =
    | Sender
    | Receiver

/// <category>message</category>
[<NoEquality; NoComparison>]
type MessageConfig =
    private
        { sender:string
          content:HtmlElement
          side:MessageSide
          avatar:HtmlElement option
          metadata:string option
          status:string option
          actions:HtmlElement option
          attributes:HtmlAttribute list }

/// <summary>
/// A presentational message row. The host owns transport, persistence, streaming, and retry policy.
/// </summary>
/// <category>message</category>
[<RequireQualifiedAccess>]
module Message =
    let create sender content =
        { sender = TextField.requiredText (nameof sender) sender
          content = content
          side = MessageSide.Receiver
          avatar = None
          metadata = None
          status = None
          actions = None
          attributes = [] }

    let withSide side (config:MessageConfig) = { config with side = side }
    let withAvatar avatar (config:MessageConfig) = { config with avatar = Some avatar }
    let withMetadata metadata (config:MessageConfig) = { config with metadata = Some (TextField.requiredText (nameof metadata) metadata) }
    let withStatus status (config:MessageConfig) = { config with status = Some (TextField.requiredText (nameof status) status) }
    let withActions actions (config:MessageConfig) = { config with actions = Some actions }
    let withAttributes attributes (config:MessageConfig) = { config with attributes = attributes }

    let render (config:MessageConfig) =
        let sent = config.side = MessageSide.Sender
        article {
            _ariaLabel ("Message from " + config.sender)
            _class (ComponentHtml.classes [
                "flex min-w-0 items-end gap-3"
                if sent then "ml-auto flex-row-reverse" else "mr-auto" ])
            for attribute in ComponentHtml.safeAttributes [ "id"; "class"; "aria-label" ] config.attributes do attribute
            match config.avatar with
            | Some avatar -> div { _class "shrink-0 self-start"; avatar }
            | None -> ()
            div {
                _class (ComponentHtml.classes [ "grid min-w-0 max-w-[min(42rem,85%)] gap-1"; if sent then "justify-items-end" ])
                div {
                    _class (ComponentHtml.classes [
                        "min-w-0 overflow-wrap-anywhere rounded-2xl px-4 py-2.5 text-sm leading-6 shadow-sm"
                        if sent then "rounded-br-sm bg-[var(--fve-brand-solid)] text-white" else "rounded-bl-sm bg-[var(--fve-surface-subtle)] text-[var(--fve-text)] ring-1 ring-[var(--fve-border)]" ])
                    config.content
                }
                if config.metadata.IsSome || config.status.IsSome || config.actions.IsSome then
                    footer {
                        _class (ComponentHtml.classes [ "flex min-w-0 flex-wrap items-center gap-x-2 gap-y-1 px-1 text-xs text-[var(--fve-muted-text)]"; if sent then "justify-end" ])
                        match config.metadata with Some metadata -> span { metadata } | None -> ()
                        match config.status with Some status -> span { _ariaLabel ("Status: " + status); status } | None -> ()
                        match config.actions with Some actions -> div { _class "flex items-center gap-1"; actions } | None -> ()
                    }
            }
        }
