namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>toggle-group</category>
[<RequireQualifiedAccess>]
type ToggleGroupVariant =
    | Connected
    | Spaced

/// <category>toggle-group</category>
[<RequireQualifiedAccess>]
type ToggleGroupOrientation =
    | Horizontal
    | Vertical

/// <category>toggle-group</category>
[<NoEquality; NoComparison>]
type ToggleGroupOption<'value> =
    private
        { value:'value
          label:string
          leading:HtmlElement option
          disabled:bool }

/// <category>toggle-group</category>
[<RequireQualifiedAccess>]
module ToggleGroupOption =
    let create value label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A toggle label is required."
        { value = value; label = label; leading = None; disabled = false }

    let withLeading leading (option:ToggleGroupOption<'value>) = { option with leading = Some leading }
    let disabled (option:ToggleGroupOption<'value>) = { option with disabled = true }

/// <category>toggle-group</category>
[<NoEquality; NoComparison>]
type ToggleGroupConfig<'value, 'mode when 'value:equality> =
    private
        { mode:'mode
          name:string
          id:string
          label:string
          encode:'value -> string
          options:ToggleGroupOption<'value> list
          selected:'value list
          isMultiple:bool
          allowsEmpty:bool
          variant:ToggleGroupVariant
          orientation:ToggleGroupOrientation
          size:ControlSize
          isDisabled:bool }

/// <category>toggle-group</category>
type ToggleGroupConfig<'value when 'value:equality> = ToggleGroupConfig<'value, SingleSelection>

/// <category>toggle-group</category>
[<RequireQualifiedAccess>]
module ToggleGroup =
    let private createConfig mode isMultiple name id label encode options =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable toggle-group ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible toggle-group label is required."
        if List.isEmpty options then invalidArg (nameof options) "At least one toggle is required."
        ChoiceSelection.validateKeys (fun option -> encode option.value) options
        { mode = mode
          name = name
          id = id
          label = label
          encode = encode
          options = options
          selected = []
          isMultiple = isMultiple
          allowsEmpty = isMultiple
          variant = ToggleGroupVariant.Connected
          orientation = ToggleGroupOrientation.Horizontal
          size = ControlSize.Medium
          isDisabled = false }

    /// Creates a single-value group. Selection cannot be cleared unless allowEmpty is applied.
    let single name id label encode options : ToggleGroupConfig<'value, SingleSelection> =
        createConfig SingleSelection false name id label encode options

    /// Creates a multiple-value group in which every toggle can be changed independently.
    let multiple name id label encode options : ToggleGroupConfig<'value, MultipleSelection> =
        createConfig MultipleSelection true name id label encode options

    let private ensureKnown values (config:ToggleGroupConfig<'value, 'mode>) =
        let distinct = List.distinct values
        if distinct |> List.exists (fun value -> config.options |> List.exists (fun option -> option.value = value) |> not) then
            invalidArg (nameof values) "Every selected value must match an option."
        distinct

    let withSelected selected (config:ToggleGroupConfig<'value, SingleSelection>) = { config with selected = ensureKnown [ selected ] config }
    let withSelectedMany selected (config:ToggleGroupConfig<'value, MultipleSelection>) = { config with selected = ensureKnown selected config }
    let allowEmpty (config:ToggleGroupConfig<'value, SingleSelection>) = { config with allowsEmpty = true }
    let withVariant variant (config:ToggleGroupConfig<'value, 'mode>) = { config with variant = variant }
    let withOrientation orientation (config:ToggleGroupConfig<'value, 'mode>) = { config with orientation = orientation }
    let withSize size (config:ToggleGroupConfig<'value, 'mode>) = { config with size = size }
    let disabled (config:ToggleGroupConfig<'value, 'mode>) = { config with isDisabled = true }

    let render (config:ToggleGroupConfig<'value, 'mode>) =
        let signal = $"_{ComponentHtml.signalToken config.id}_selected"
        let selectedChoices =
            config.selected
            |> List.map (fun value ->
                let option = config.options |> List.find (fun option -> option.value = value)
                { SelectedChoice.value = config.encode value; label = option.label })
        let initial =
            if config.isMultiple then
                selectedChoices |> ChoiceSelection.json
            else
                config.selected |> List.tryHead |> Option.map config.encode |> Option.defaultValue "" |> ComponentHtml.javascriptString
        let available = config.options |> List.filter (fun option -> not option.disabled && not config.isDisabled)
        let buttonId option = $"{config.id}-toggle-{option.value |> config.encode |> ComponentHtml.optionToken}"
        let availableIds = available |> List.map buttonId
        let isPressed option =
            if config.isMultiple then ChoiceSelection.selected signal (config.encode option.value)
            else $"${signal} == {ComponentHtml.javascriptString (config.encode option.value)}"
        let toggle option =
            if config.isMultiple then ChoiceSelection.toggle signal (config.encode option.value) option.label
            elif config.allowsEmpty then $"${signal} = ${signal} == {ComponentHtml.javascriptString (config.encode option.value)} ? '' : {ComponentHtml.javascriptString (config.encode option.value)}"
            else $"${signal} = {ComponentHtml.javascriptString (config.encode option.value)}"
        let groupClasses =
            match config.orientation, config.variant with
            | ToggleGroupOrientation.Horizontal, ToggleGroupVariant.Connected -> ComponentHtml.classes [ "isolate inline-flex items-stretch"; ComponentHtml.horizontalScrollClasses ]
            | ToggleGroupOrientation.Horizontal, ToggleGroupVariant.Spaced -> "inline-flex flex-wrap items-center gap-2"
            | ToggleGroupOrientation.Vertical, ToggleGroupVariant.Connected -> "isolate inline-flex flex-col items-stretch"
            | ToggleGroupOrientation.Vertical, ToggleGroupVariant.Spaced -> "inline-flex flex-col items-stretch gap-2"
        div {
            _id config.id
            _role "group"
            _ariaLabel config.label
            if config.orientation = ToggleGroupOrientation.Horizontal && config.variant = ToggleGroupVariant.Connected then
                for attribute in ComponentHtml.horizontalScrollAttributes do attribute
            _dataSignals $"{{{signal}: {initial}}}"
            _class groupClasses
            if config.isMultiple then
                ChoiceSelection.render config.name signal selectedChoices config.isDisabled
            else
                input { _type "hidden"; _name config.name; _value (config.selected |> List.tryHead |> Option.map config.encode |> Option.defaultValue ""); _disabled config.isDisabled; _dataBind signal }
            for index, option in config.options |> List.indexed do
                let unavailable = config.isDisabled || option.disabled
                let availableIndex = available |> List.tryFindIndex (fun candidate -> candidate.value = option.value) |> Option.defaultValue 0
                let previousId = if List.isEmpty availableIds then "" else availableIds[(availableIndex - 1 + availableIds.Length) % availableIds.Length]
                let nextId = if List.isEmpty availableIds then "" else availableIds[(availableIndex + 1) % availableIds.Length]
                let firstId = availableIds |> List.tryHead |> Option.defaultValue ""
                let lastId = availableIds |> List.tryLast |> Option.defaultValue ""
                let focus target = $"document.getElementById({ComponentHtml.javascriptString target})?.focus()"
                let directionKeys =
                    match config.orientation with
                    | ToggleGroupOrientation.Horizontal -> $"evt.key == 'ArrowLeft' && (evt.preventDefault(), {focus previousId}); evt.key == 'ArrowRight' && (evt.preventDefault(), {focus nextId})"
                    | ToggleGroupOrientation.Vertical -> $"evt.key == 'ArrowUp' && (evt.preventDefault(), {focus previousId}); evt.key == 'ArrowDown' && (evt.preventDefault(), {focus nextId})"
                let connectedClasses =
                    if config.variant = ToggleGroupVariant.Spaced || config.options.Length = 1 then "rounded-[var(--fve-radius-control)]"
                    else
                        match config.orientation, index with
                        | ToggleGroupOrientation.Horizontal, 0 -> "rounded-s-[var(--fve-radius-control)]"
                        | ToggleGroupOrientation.Horizontal, index when index = config.options.Length - 1 -> "-ml-px rounded-e-[var(--fve-radius-control)]"
                        | ToggleGroupOrientation.Horizontal, _ -> "-ml-px"
                        | ToggleGroupOrientation.Vertical, 0 -> "rounded-t-[var(--fve-radius-control)]"
                        | ToggleGroupOrientation.Vertical, index when index = config.options.Length - 1 -> "-mt-px rounded-b-[var(--fve-radius-control)]"
                        | ToggleGroupOrientation.Vertical, _ -> "-mt-px"
                button {
                    _id (buttonId option)
                    _type "button"
                    _disabled unavailable
                    _ariaDisabled unavailable
                    _ariaPressed (config.selected |> List.contains option.value)
                    _dataAttr ("aria-pressed", $"{isPressed option} ? 'true' : 'false'")
                    if not unavailable then
                        _dataOn ("click", toggle option)
                        _dataOn ("keydown", $"{directionKeys}; evt.key == 'Home' && (evt.preventDefault(), {focus firstId}); evt.key == 'End' && (evt.preventDefault(), {focus lastId})")
                    _class (ComponentHtml.classes [ "inline-flex items-center justify-center gap-2 bg-[var(--fve-surface)] px-3 font-medium text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] outline-none transition-colors hover:bg-[var(--fve-surface-hover)] focus-visible:z-10 focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)] aria-pressed:bg-[var(--fve-brand-subtle)] aria-pressed:text-[var(--fve-brand-text)] disabled:pointer-events-none disabled:opacity-50"; ComponentHtml.sizeClasses (Some config.size); connectedClasses ])
                    match option.leading with
                    | Some leading -> span { _ariaHidden true; _class "flex size-4 shrink-0 items-center justify-center"; leading }
                    | None -> ()
                    option.label
                }
        }
