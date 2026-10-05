namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <summary>Controls whether joined controls are arranged horizontally or vertically.</summary>
/// <category>button-group</category>
[<RequireQualifiedAccess>]
type ButtonGroupOrientation =
    | Horizontal
    | Vertical

/// <summary>A typed Button or DropdownMenu segment in a joined group.</summary>
/// <category>button-group</category>
[<NoEquality; NoComparison>]
type ButtonGroupItem<'destination> =
    private
        | Button of ButtonConfig
        | Menu of DropdownMenuConfig<'destination>

/// <summary>Converts existing controls into ButtonGroup segments without changing their behavior.</summary>
/// <category>button-group</category>
[<RequireQualifiedAccess>]
module ButtonGroupItem =
    /// Adds an existing Button configuration, including icon-only content.
    let button config : ButtonGroupItem<'destination> = Button config
    /// Adds an existing DropdownMenu configuration.
    let menu config : ButtonGroupItem<'destination> = Menu config

/// <summary>Opaque configuration produced and modified by the ButtonGroup module.</summary>
/// <category>button-group</category>
[<NoEquality; NoComparison>]
type ButtonGroupConfig<'destination> =
    private
        { label:string
          items:ButtonGroupItem<'destination> list
          orientation:ButtonGroupOrientation
          attributes:HtmlAttribute list }

/// <summary>Joins related controls visually while preserving each control’s native behavior.</summary>
/// <category>button-group</category>
[<RequireQualifiedAccess>]
module ButtonGroup =
    /// <summary>Creates a horizontal group. Every control retains its own focus stop and behavior.</summary>
    let create label items =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible button-group label is required."
        if List.isEmpty items then invalidArg (nameof items) "A button group requires at least one control."
        { label = label
          items = items
          orientation = ButtonGroupOrientation.Horizontal
          attributes = [] }

    /// Arranges the controls horizontally or vertically.
    let withOrientation orientation (config:ButtonGroupConfig<'destination>) = { config with orientation = orientation }
    /// Adds safe consumer-authored attributes to the group container.
    let withAttributes attributes (config:ButtonGroupConfig<'destination>) = { config with attributes = attributes }

    /// Renders the labelled group and resolves DropdownMenu destinations.
    let render (resolve:'destination -> string) (config:ButtonGroupConfig<'destination>) =
        let count = config.items.Length
        let overlapClass index =
            if index = 0 then ""
            else match config.orientation with ButtonGroupOrientation.Horizontal -> "-ml-px" | ButtonGroupOrientation.Vertical -> "-mt-px"
        let shapeClass index =
            if count = 1 then ""
            else
                match config.orientation, index with
                | ButtonGroupOrientation.Horizontal, 0 -> "!rounded-e-none"
                | ButtonGroupOrientation.Horizontal, index when index = count - 1 -> "!rounded-s-none"
                | ButtonGroupOrientation.Horizontal, _ -> "!rounded-none"
                | ButtonGroupOrientation.Vertical, 0 -> "!rounded-b-none"
                | ButtonGroupOrientation.Vertical, index when index = count - 1 -> "!rounded-t-none"
                | ButtonGroupOrientation.Vertical, _ -> "!rounded-none"
        let controlClass index = ComponentHtml.classes [ overlapClass index; shapeClass index; "focus-visible:relative focus-visible:z-10" ]
        div {
            _role "group"
            _ariaLabel config.label
            _class (match config.orientation with ButtonGroupOrientation.Horizontal -> ComponentHtml.classes [ "isolate inline-flex items-stretch"; ComponentHtml.horizontalScrollClasses ] | ButtonGroupOrientation.Vertical -> "isolate inline-flex flex-col items-stretch")
            if config.orientation = ButtonGroupOrientation.Horizontal then
                for attribute in ComponentHtml.horizontalScrollAttributes do attribute
            for attribute in ComponentHtml.safeAttributes [ "class"; "role"; "aria-label"; "tabindex"; "data-on:keydown"; "data-on:focusin" ] config.attributes do attribute
            for index, item in config.items |> List.indexed do
                match item with
                | Button button -> button |> Button.withGroupClass (controlClass index) |> Button.render
                | Menu menu -> menu |> DropdownMenu.withinGroup (overlapClass index) (ComponentHtml.classes [ shapeClass index; "bg-transparent text-[var(--fve-neutral-text)] ring-1 ring-inset ring-[color-mix(in_srgb,var(--fve-neutral-text)_20%,transparent)] focus-visible:relative focus-visible:z-10" ]) |> DropdownMenu.render resolve
        }
