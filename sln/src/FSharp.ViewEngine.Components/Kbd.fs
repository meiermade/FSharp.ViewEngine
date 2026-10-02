namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>kbd</category>
[<NoEquality; NoComparison>]
type KbdConfig =
    private
        { keys:string list
          label:string option
          attributes:HtmlAttribute list }

/// <category>kbd</category>
[<RequireQualifiedAccess>]
module Kbd =
    let private validate keys =
        if List.isEmpty keys || keys |> List.exists String.IsNullOrWhiteSpace then
            invalidArg (nameof keys) "At least one non-empty key is required."
        keys

    /// Creates one semantic keyboard key.
    let create key =
        { keys = validate [ key ]
          label = None
          attributes = [] }

    /// Creates a grouped keyboard shortcut without registering keyboard behavior.
    let shortcut keys =
        { keys = validate keys
          label = None
          attributes = [] }

    let withLabel label (config:KbdConfig) =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible shortcut label is required."
        { config with label = Some label }

    let withAttributes attributes (config:KbdConfig) = { config with attributes = attributes }

    let render (config:KbdConfig) =
        span {
            match config.label with
            | Some label -> _ariaLabel label
            | None -> ()
            _class "inline-flex items-center gap-1 align-middle"
            for attribute in ComponentHtml.safeAttributes [ "class"; "aria-label" ] config.attributes do attribute
            for index, key in config.keys |> List.indexed do
                if index > 0 then
                    span { _ariaHidden true; _class "text-xs text-[var(--fve-muted-text)]"; "+" }
                kbd {
                    _class "inline-flex min-h-5 min-w-5 items-center justify-center rounded-[calc(var(--fve-radius-control)*0.75)] bg-[var(--fve-surface-subtle)] px-1.5 font-mono text-[0.6875rem] font-semibold leading-none text-[var(--fve-muted-text)] ring-1 ring-inset ring-[var(--fve-border)] shadow-[0_1px_0_var(--fve-border)]"
                    key
                }
        }
