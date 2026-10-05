namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>popover</category>
[<RequireQualifiedAccess>]
type PopoverSide =
    | Top
    | Bottom
    | Start
    | End

/// <category>popover</category>
[<RequireQualifiedAccess>]
type PopoverAlignment =
    | Start
    | Center
    | End

/// <category>popover</category>
[<NoEquality; NoComparison>]
type PopoverConfig =
    private
        { id:string
          triggerLabel:string
          trigger:HtmlElement
          contentLabel:string
          content:HtmlElement
          side:PopoverSide
          alignment:PopoverAlignment
          initialFocus:bool
          triggerAttributes:HtmlAttribute list
          contentClass:string option
          triggerClass:string option
          attributes:HtmlAttribute list }

/// <summary>
/// Trigger-anchored, dismissible rich content. Use DropdownMenu for commands and Tooltip for non-interactive descriptions.
/// </summary>
/// <category>popover</category>
[<RequireQualifiedAccess>]
module Popover =
    let create id triggerLabel trigger contentLabel content =
        { id = TextField.stableId id
          triggerLabel = TextField.requiredText (nameof triggerLabel) triggerLabel
          trigger = trigger
          contentLabel = TextField.requiredText (nameof contentLabel) contentLabel
          content = content
          side = PopoverSide.Bottom
          alignment = PopoverAlignment.Start
          initialFocus = false
          triggerAttributes = []
          contentClass = None
          triggerClass = None
          attributes = [] }

    let withSide side (config:PopoverConfig) = { config with side = side }
    let withAlignment alignment (config:PopoverConfig) = { config with alignment = alignment }
    let focusContentOnOpen (config:PopoverConfig) = { config with initialFocus = true }
    let withTriggerAttributes attributes (config:PopoverConfig) = { config with triggerAttributes = attributes }
    let internal withContentClass className (config:PopoverConfig) = { config with contentClass = Some className }
    let internal withTriggerClass className (config:PopoverConfig) = { config with triggerClass = Some className }
    let withAttributes attributes (config:PopoverConfig) = { config with attributes = attributes }

    let render (config:PopoverConfig) =
        let panelId = config.id + "-content"
        let anchor = "--fve-popover-" + ComponentHtml.optionToken config.id
        let position =
            match config.side, config.alignment with
            | PopoverSide.Top, PopoverAlignment.Start -> "block-start span-inline-end"
            | PopoverSide.Top, PopoverAlignment.Center -> "block-start"
            | PopoverSide.Top, PopoverAlignment.End -> "block-start span-inline-start"
            | PopoverSide.Bottom, PopoverAlignment.Start -> "block-end span-inline-end"
            | PopoverSide.Bottom, PopoverAlignment.Center -> "block-end"
            | PopoverSide.Bottom, PopoverAlignment.End -> "block-end span-inline-start"
            | PopoverSide.Start, PopoverAlignment.Start -> "inline-start span-block-end"
            | PopoverSide.Start, PopoverAlignment.Center -> "inline-start"
            | PopoverSide.Start, PopoverAlignment.End -> "inline-start span-block-start"
            | PopoverSide.End, PopoverAlignment.Start -> "inline-end span-block-end"
            | PopoverSide.End, PopoverAlignment.Center -> "inline-end"
            | PopoverSide.End, PopoverAlignment.End -> "inline-end span-block-start"
        span {
            _id config.id
            _class "inline-flex"
            _style $"anchor-name:{anchor}"
            for attribute in ComponentHtml.safeAttributes [ "id"; "class"; "style"; "aria-label" ] config.attributes do attribute
            button {
                _id (config.id + "-trigger")
                _type "button"
                _ariaLabel config.triggerLabel
                _ariaHaspopup "dialog"
                _ariaControls panelId
                _attr ("popovertarget", panelId)
                _class (defaultArg config.triggerClass "inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] shadow-sm hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-50")
                for attribute in ComponentHtml.safeAttributes [ "type"; "id"; "class"; "aria-label"; "aria-controls"; "aria-haspopup"; "popovertarget" ] config.triggerAttributes do attribute
                config.trigger
            }
            div {
                _id panelId
                _role "dialog"
                _ariaLabel config.contentLabel
                _attr ("popover", "auto")
                _style $"inset:auto;margin:0.5rem 0;position-anchor:{anchor};position-area:{position};position-try-fallbacks:flip-block,flip-inline,flip-block flip-inline"
                _dataOn ("keydown", "if (evt.key == 'Escape') el.dataset.fveEscapeDismissed = 'true'")
                if config.initialFocus then
                    _dataOn ("toggle", "if (evt.newState == 'open') setTimeout(() => el.querySelector('input:not(:disabled),select:not(:disabled),textarea:not(:disabled),button:not(:disabled),a[href],[tabindex=\"0\"]')?.focus(), 0); else if (el.dataset.fveEscapeDismissed == 'true') { delete el.dataset.fveEscapeDismissed; el.previousElementSibling?.focus() }")
                else
                    _dataOn ("toggle", "if (evt.newState == 'closed' && el.dataset.fveEscapeDismissed == 'true') { delete el.dataset.fveEscapeDismissed; el.previousElementSibling?.focus() }")
                _class (ComponentHtml.classes [ "fixed max-h-[min(32rem,calc(100dvh-2rem))] w-[min(24rem,calc(100vw-2rem))] overflow-auto rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[var(--fve-surface)] p-4 text-[var(--fve-text)] shadow-xl open:block"; defaultArg config.contentClass "" ])
                config.content
            }
        }
