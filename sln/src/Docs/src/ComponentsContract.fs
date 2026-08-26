namespace FSharp.ViewEngine.Components

open System
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar

[<RequireQualifiedAccess>]
type Tone =
    | Neutral
    | Brand
    | Positive
    | Warning
    | Critical
    | Informative

[<RequireQualifiedAccess>]
type ControlSize =
    | Small
    | Medium
    | Large

[<RequireQualifiedAccess>]
type Radius =
    | None
    | Medium
    | Large
    | Full

[<RequireQualifiedAccess>]
type Density =
    | Compact
    | Comfortable

[<NoEquality; NoComparison>]
type ComponentsTheme =
    private
        { paletteClass:string
          radiusClass:string
          densityClass:string }

[<RequireQualifiedAccess>]
module ComponentsTheme =
    let sky =
        { paletteClass = "fve-theme-sky"
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable" }

    let emerald =
        { paletteClass = "fve-theme-emerald"
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable" }

    let withRadius radius theme =
        let radiusClass =
            match radius with
            | Radius.None -> "fve-radius-none"
            | Radius.Medium -> "fve-radius-medium"
            | Radius.Large -> "fve-radius-large"
            | Radius.Full -> "fve-radius-full"
        { theme with radiusClass = radiusClass }

    let withDensity density theme =
        let densityClass =
            match density with
            | Density.Compact -> "fve-density-compact"
            | Density.Comfortable -> "fve-density-comfortable"
        { theme with densityClass = densityClass }

    let className theme =
        [ "fve-components"; theme.paletteClass; theme.radiusClass; theme.densityClass ]
        |> String.concat " "

    let attributes theme =
        [ _class (className theme) ]

module private ContractHtml =
    let classes values = values |> List.filter (String.IsNullOrWhiteSpace >> not) |> String.concat " "

    let signalToken value =
        let token = Regex.Replace(value, "[^A-Za-z0-9]+", "_").Trim('_')
        if String.IsNullOrEmpty token then "component" else token

    let toneClasses = function
        | Tone.Neutral -> "bg-[var(--fve-neutral-subtle)] text-[var(--fve-neutral-text)] ring-[var(--fve-border)]"
        | Tone.Brand -> "bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)] ring-[var(--fve-brand-ring)]"
        | Tone.Positive -> "bg-[var(--fve-positive-subtle)] text-[var(--fve-positive-text)] ring-[var(--fve-positive-ring)]"
        | Tone.Warning -> "bg-[var(--fve-warning-subtle)] text-[var(--fve-warning-text)] ring-[var(--fve-warning-ring)]"
        | Tone.Critical -> "bg-[var(--fve-critical-subtle)] text-[var(--fve-critical-text)] ring-[var(--fve-critical-ring)]"
        | Tone.Informative -> "bg-[var(--fve-info-subtle)] text-[var(--fve-info-text)] ring-[var(--fve-info-ring)]"

    let sizeClasses = function
        | ControlSize.Small -> "min-h-8 px-2.5 py-1.5 text-xs"
        | ControlSize.Medium -> "min-h-9 px-3 py-2 text-sm"
        | ControlSize.Large -> "min-h-11 px-4 py-2.5 text-base"

[<RequireQualifiedAccess>]
type ButtonVariant =
    | Primary
    | Secondary
    | Ghost
    | Destructive

[<RequireQualifiedAccess>]
type ButtonType =
    | Button
    | Submit
    | Reset

[<NoEquality; NoComparison>]
type ButtonConfig =
    private
        { label:string
          variant:ButtonVariant
          size:ControlSize
          buttonType:ButtonType
          leading:HtmlElement option
          trailing:HtmlElement option
          disabled:bool
          className:string option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Button =
    let create label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A button label is required."
        { label = label
          variant = ButtonVariant.Secondary
          size = ControlSize.Medium
          buttonType = ButtonType.Button
          leading = None
          trailing = None
          disabled = false
          className = None
          attributes = [] }

    let withVariant variant config = { config with variant = variant }
    let withSize size config = { config with size = size }
    let asSubmit config = { config with buttonType = ButtonType.Submit }
    let withLeading leading config = { config with leading = Some leading }
    let withTrailing trailing config = { config with trailing = Some trailing }
    let disabled config = { config with disabled = true }
    let withClass className config = { config with className = Some className }
    let withAttributes attributes config = { config with attributes = attributes }

    let render config =
        let variantClasses =
            match config.variant with
            | ButtonVariant.Primary -> "bg-[var(--fve-brand-solid)] text-white hover:bg-[var(--fve-brand-hover)] focus-visible:ring-[var(--fve-brand-ring)]"
            | ButtonVariant.Secondary -> "bg-[var(--fve-surface)] text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:ring-[var(--fve-brand-ring)]"
            | ButtonVariant.Ghost -> "bg-transparent text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:ring-[var(--fve-brand-ring)]"
            | ButtonVariant.Destructive -> "bg-[var(--fve-critical-solid)] text-white hover:bg-[var(--fve-critical-hover)] focus-visible:ring-[var(--fve-critical-ring)]"
        let buttonType =
            match config.buttonType with
            | ButtonType.Button -> "button"
            | ButtonType.Submit -> "submit"
            | ButtonType.Reset -> "reset"
        button {
            _type buttonType
            _disabled config.disabled
            _class (
                ContractHtml.classes [
                    "inline-flex items-center justify-center gap-2 rounded-[var(--fve-radius-control)] font-semibold shadow-sm outline-none transition-colors focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50"
                    ContractHtml.sizeClasses config.size
                    variantClasses
                    config.className |> Option.defaultValue "" ])
            for attribute in config.attributes do attribute
            config.leading |> Option.defaultValue empty
            config.label
            config.trailing |> Option.defaultValue empty
        }

    let primary label = create label |> withVariant ButtonVariant.Primary |> render
    let secondary label = create label |> withVariant ButtonVariant.Secondary |> render

[<NoEquality; NoComparison>]
type StatusConfig =
    private
        { label:string
          tone:Tone
          leading:HtmlElement option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Status =
    let create label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A status label is required."
        { label = label; tone = Tone.Neutral; leading = None; attributes = [] }

    let withTone tone (config:StatusConfig) = { config with tone = tone }
    let withLeading leading (config:StatusConfig) = { config with leading = Some leading }
    let withAttributes attributes (config:StatusConfig) = { config with attributes = attributes }

    let render config =
        span {
            _class (ContractHtml.classes [ "inline-flex items-center gap-1.5 rounded-full px-2 py-1 text-xs font-medium ring-1 ring-inset"; ContractHtml.toneClasses config.tone ])
            for attribute in config.attributes do attribute
            config.leading |> Option.defaultValue empty
            config.label
        }

    let positive label = create label |> withTone Tone.Positive |> render
    let warning label = create label |> withTone Tone.Warning |> render

[<NoEquality; NoComparison>]
type TableColumn<'row> =
    private
        { heading:string
          cell:'row -> HtmlElement
          headerClass:string option
          cellClass:string option }

[<NoEquality; NoComparison>]
type TableConfig<'row> =
    private
        { caption:string
          columns:TableColumn<'row> list
          rows:'row list
          emptyState:HtmlElement
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Table =
    let column heading cell =
        if String.IsNullOrWhiteSpace heading then invalidArg (nameof heading) "A column heading is required."
        { heading = heading; cell = cell; headerClass = None; cellClass = None }

    let alignEnd (column:TableColumn<'row>) =
        { column with headerClass = Some "text-right"; cellClass = Some "text-right" }

    let create caption columns rows =
        if String.IsNullOrWhiteSpace caption then invalidArg (nameof caption) "A table caption is required."
        if List.isEmpty columns then invalidArg (nameof columns) "At least one table column is required."
        { caption = caption
          columns = columns
          rows = rows
          emptyState = div { _class "p-6 text-center text-sm text-[var(--fve-muted-text)]"; "No records" }
          attributes = [] }

    let withEmptyState emptyState (config:TableConfig<'row>) = { config with emptyState = emptyState }
    let withAttributes attributes (config:TableConfig<'row>) = { config with attributes = attributes }

    let render config =
        div {
            _class "overflow-hidden rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] ring-1 ring-[var(--fve-border)]"
            if List.isEmpty config.rows then
                config.emptyState
            else
                table {
                    _class "min-w-full divide-y divide-[var(--fve-border)] text-left text-sm"
                    for attribute in config.attributes do attribute
                    caption { _class "sr-only"; config.caption }
                    thead {
                        _class "bg-[var(--fve-surface-subtle)] text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"
                        tr {
                            for column in config.columns do
                                th {
                                    _scope "col"
                                    _class (ContractHtml.classes [ "px-4 py-3"; column.headerClass |> Option.defaultValue "" ])
                                    column.heading
                                }
                        }
                    }
                    tbody {
                        _class "divide-y divide-[var(--fve-border)] text-[var(--fve-text)]"
                        for row in config.rows do
                            tr {
                                _class "hover:bg-[var(--fve-surface-hover)]"
                                for column in config.columns do
                                    td {
                                        _class (ContractHtml.classes [ "px-4 py-3"; column.cellClass |> Option.defaultValue "" ])
                                        column.cell row
                                    }
                            }
                    }
                }
        }

[<NoEquality; NoComparison>]
type SelectOption<'value> =
    private
        { value:'value
          label:string
          description:string option
          disabled:bool }

[<NoEquality; NoComparison>]
type SelectConfig<'value when 'value:equality> =
    private
        { name:string
          encode:'value -> string
          options:SelectOption<'value> list
          selected:'value option
          label:string
          labelVisuallyHidden:bool
          description:string option
          placeholder:string option
          validation:string option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Select =
    let option value label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An option label is required."
        { value = value; label = label; description = None; disabled = false }

    let describe description (option:SelectOption<'value>) = { option with description = Some description }
    let disable (option:SelectOption<'value>) = { option with disabled = true }

    let create name label encode options =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible label is required."
        { name = name
          encode = encode
          options = options
          selected = None
          label = label
          labelVisuallyHidden = false
          description = None
          placeholder = None
          validation = None
          attributes = [] }

    let withSelected selected (config:SelectConfig<'value>) = { config with selected = Some selected }
    let withVisuallyHiddenLabel (config:SelectConfig<'value>) = { config with labelVisuallyHidden = true }
    let withDescription description (config:SelectConfig<'value>) = { config with description = Some description }
    let withPlaceholder placeholder (config:SelectConfig<'value>) = { config with placeholder = Some placeholder }
    let withValidation message (config:SelectConfig<'value>) = { config with validation = Some message }
    let withAttributes attributes (config:SelectConfig<'value>) = { config with attributes = attributes }

    let render config =
        let fieldId = $"fve-select-{config.name}"
        let descriptionId = $"{fieldId}-description"
        let validationId = $"{fieldId}-validation"
        div {
            _class "grid gap-1.5"
            label {
                _for fieldId
                _class (if config.labelVisuallyHidden then "sr-only" else "text-sm font-medium text-[var(--fve-text)]")
                config.label
            }
            match config.description with
            | Some description -> p { _id descriptionId; _class "text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            select {
                _id fieldId
                _name config.name
                _dataBind config.name
                _ariaDescribedby (
                    [ if config.description.IsSome then descriptionId
                      if config.validation.IsSome then validationId ]
                    |> String.concat " ")
                _ariaInvalid config.validation.IsSome
                _class "min-h-9 w-full rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-2 text-sm text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] outline-none focus:ring-2 focus:ring-[var(--fve-brand-ring)]"
                for attribute in config.attributes do attribute
                match config.placeholder with
                | Some placeholder -> Html.option { _value ""; _selected config.selected.IsNone; placeholder }
                | None -> ()
                for choice in config.options do
                    Html.option {
                        _value (config.encode choice.value)
                        _selected (config.selected = Some choice.value)
                        _disabled choice.disabled
                        choice.label
                    }
            }
            match config.validation with
            | Some message -> p { _id validationId; _class "text-sm text-[var(--fve-critical-text)]"; message }
            | None -> ()
        }

[<RequireQualifiedAccess>]
type ComboboxSearch =
    | Static
    | Remote of endpoint:string

[<NoEquality; NoComparison>]
type ComboboxConfig<'value when 'value:equality> =
    private
        { name:string
          encode:'value -> string
          options:SelectOption<'value> list
          selected:'value option
          label:string
          labelVisuallyHidden:bool
          search:ComboboxSearch
          placeholder:string option }

[<RequireQualifiedAccess>]
module Combobox =
    let create name label encode options =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible label is required."
        { name = name
          encode = encode
          options = options
          selected = None
          label = label
          labelVisuallyHidden = false
          search = ComboboxSearch.Static
          placeholder = None }

    let withSelected selected (config:ComboboxConfig<'value>) = { config with selected = Some selected }
    let withVisuallyHiddenLabel (config:ComboboxConfig<'value>) = { config with labelVisuallyHidden = true }
    let withPlaceholder placeholder (config:ComboboxConfig<'value>) = { config with placeholder = Some placeholder }
    let withSearch search (config:ComboboxConfig<'value>) = { config with search = search }

    let render config =
        let fieldId = $"fve-combobox-{config.name}"
        let listboxId = $"{fieldId}-options"
        let signalToken = ContractHtml.signalToken config.name
        let querySignal = $"_{signalToken}Query"
        let openSignal = $"_{signalToken}Open"
        div {
            _class "relative grid gap-1.5"
            _dataSignals $"{{{querySignal}: '', {openSignal}: false}}"
            label {
                _for fieldId
                _class (if config.labelVisuallyHidden then "sr-only" else "text-sm font-medium text-[var(--fve-text)]")
                config.label
            }
            input {
                _id fieldId
                _type "search"
                _role "combobox"
                _ariaControls listboxId
                _ariaExpanded false
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                _autocomplete "off"
                _placeholder (config.placeholder |> Option.defaultValue "Search options")
                _dataBind querySignal
                match config.search with
                | ComboboxSearch.Static -> _dataOn ("input", $"${openSignal} = true")
                | ComboboxSearch.Remote endpoint -> _dataOn ("input", [ "debounce.250ms" ], $"${openSignal} = true; @get('{endpoint}')")
                _class "min-h-9 w-full rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-2 text-sm text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] outline-none focus:ring-2 focus:ring-[var(--fve-brand-ring)]"
            }
            input {
                _type "hidden"
                _name config.name
                _value (config.selected |> Option.map config.encode |> Option.defaultValue "")
                _dataBind config.name
            }
            div {
                _id listboxId
                _role "listbox"
                _dataShow $"${openSignal}"
                _class "absolute z-20 mt-1 max-h-60 w-full translate-y-full overflow-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] p-1 shadow-lg ring-1 ring-[var(--fve-border)]"
                for choice in config.options do
                    div {
                        _role "option"
                        _ariaDisabled choice.disabled
                        _class "cursor-default rounded-[var(--fve-radius-control)] px-3 py-2 text-sm text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] aria-disabled:opacity-50"
                        choice.label
                    }
            }
        }

[<RequireQualifiedAccess>]
type MenuTone =
    | Default
    | Destructive

[<NoEquality; NoComparison>]
type MenuItem<'destination> =
    private
        | Link of label:string * destination:'destination
        | Action of label:string * datastarExpression:string * tone:MenuTone
        | Separator

[<NoEquality; NoComparison>]
type DropdownMenuConfig<'destination> =
    private
        { id:string
          label:string
          items:MenuItem<'destination> list }

[<RequireQualifiedAccess>]
module MenuItem =
    let link destination label = Link(label, destination)
    let action datastarExpression label = Action(label, datastarExpression, MenuTone.Default)
    let destructiveAction datastarExpression label = Action(label, datastarExpression, MenuTone.Destructive)
    let separator<'destination> : MenuItem<'destination> = Separator

[<RequireQualifiedAccess>]
module DropdownMenu =
    let create id label items =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable menu ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A menu label is required."
        { id = id; label = label; items = items }

    let render resolve config =
        let openSignal = $"_{ContractHtml.signalToken config.id}Open"
        div {
            _class "relative inline-flex"
            _dataSignals $"{{{openSignal}: false}}"
            button {
                _type "button"
                _ariaHaspopup "menu"
                _ariaExpanded false
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                _ariaControls $"{config.id}-menu"
                _dataOn ("click", $"${openSignal} = !${openSignal}")
                _class "inline-flex min-h-9 items-center rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)]"
                config.label
            }
            div {
                _id $"{config.id}-menu"
                _role "menu"
                _dataShow $"${openSignal}"
                _dataOn ("click", [ "outside" ], $"${openSignal} = false")
                _class "absolute right-0 top-full z-20 mt-2 min-w-48 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] p-1 shadow-lg ring-1 ring-[var(--fve-border)]"
                for item in config.items do
                    match item with
                    | Link(label, destination) ->
                        a { _href (resolve destination); _role "menuitem"; _class "block rounded-[var(--fve-radius-control)] px-3 py-2 text-sm text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)]"; label }
                    | Action(label, expression, tone) ->
                        button {
                            _type "button"
                            _role "menuitem"
                            _dataOn ("click", expression)
                            _class (
                                match tone with
                                | MenuTone.Default -> "block w-full rounded-[var(--fve-radius-control)] px-3 py-2 text-left text-sm text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)]"
                                | MenuTone.Destructive -> "block w-full rounded-[var(--fve-radius-control)] px-3 py-2 text-left text-sm text-[var(--fve-critical-text)] hover:bg-[var(--fve-critical-subtle)]")
                            label
                        }
                    | Separator -> div { _role "separator"; _class "my-1 h-px bg-[var(--fve-border)]" }
            }
        }

[<NoEquality; NoComparison>]
type DialogConfig =
    private
        { id:string
          title:string
          body:HtmlElement
          description:string option
          footer:HtmlElement option }

[<RequireQualifiedAccess>]
module Dialog =
    let create id title body =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable dialog ID is required."
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A dialog title is required."
        { id = id; title = title; body = body; description = None; footer = None }

    let withDescription description (config:DialogConfig) = { config with description = Some description }
    let withFooter footer (config:DialogConfig) = { config with footer = Some footer }

    let render config =
        let titleId = $"{config.id}-title"
        let descriptionId = $"{config.id}-description"
        dialog {
            _id config.id
            _ariaLabelledby titleId
            if config.description.IsSome then _ariaDescribedby descriptionId
            _class "w-[min(32rem,calc(100%-2rem))] rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-slate-950/50"
            div {
                _class "p-6"
                h2 { _id titleId; _class "text-lg font-semibold"; config.title }
                match config.description with
                | Some description -> p { _id descriptionId; _class "mt-2 text-sm text-[var(--fve-muted-text)]"; description }
                | None -> ()
                div { _class "mt-4"; config.body }
                match config.footer with
                | Some footer -> div { _class "mt-6 flex justify-end gap-3"; footer }
                | None -> ()
            }
        }

[<NoEquality; NoComparison>]
type CollectionConfig =
    private
        { title:string
          description:string option
          actions:HtmlElement option
          toolbar:HtmlElement option
          content:HtmlElement }

[<RequireQualifiedAccess>]
module Collection =
    let create title content =
        { title = title; description = None; actions = None; toolbar = None; content = content }

    let withDescription description (config:CollectionConfig) = { config with description = Some description }
    let withActions actions (config:CollectionConfig) = { config with actions = Some actions }
    let withToolbar toolbar (config:CollectionConfig) = { config with toolbar = Some toolbar }

    let render config =
        section {
            _class "grid gap-6"
            header {
                _class "flex flex-wrap items-start justify-between gap-4"
                div {
                    h1 { _class "text-2xl font-semibold tracking-tight text-[var(--fve-text)]"; config.title }
                    match config.description with
                    | Some description -> p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
                    | None -> ()
                }
                config.actions |> Option.defaultValue empty
            }
            config.toolbar |> Option.defaultValue empty
            config.content
        }

[<NoEquality; NoComparison>]
type DetailConfig =
    private
        { title:string
          metadata:HtmlElement option
          actions:HtmlElement option
          sections:HtmlElement list }

[<RequireQualifiedAccess>]
module Detail =
    let create title sections = { title = title; metadata = None; actions = None; sections = sections }
    let withMetadata metadata (config:DetailConfig) = { config with metadata = Some metadata }
    let withActions actions (config:DetailConfig) = { config with actions = Some actions }

    let render config =
        article {
            _class "grid gap-6"
            header {
                _class "flex flex-wrap items-start justify-between gap-4 border-b border-[var(--fve-border)] pb-5"
                div {
                    h1 { _class "text-2xl font-semibold tracking-tight text-[var(--fve-text)]"; config.title }
                    config.metadata |> Option.defaultValue empty
                }
                config.actions |> Option.defaultValue empty
            }
            for detailSection in config.sections do
                section { _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-5 ring-1 ring-[var(--fve-border)]"; detailSection }
        }

[<NoEquality; NoComparison>]
type NavigationItem<'destination> =
    private
        { label:string
          destination:'destination }

[<NoEquality; NoComparison>]
type AppShellConfig<'destination when 'destination:equality> =
    private
        { productName:string
          current:'destination
          navigation:NavigationItem<'destination> list
          content:HtmlElement
          breadcrumbs:NavigationItem<'destination> list
          accountMenu:HtmlElement option
          theme:ComponentsTheme }

[<RequireQualifiedAccess>]
module NavigationItem =
    let create destination label = { label = label; destination = destination }

[<RequireQualifiedAccess>]
module AppShell =
    let create productName current navigation content =
        if String.IsNullOrWhiteSpace productName then invalidArg (nameof productName) "A product name is required."
        { productName = productName
          current = current
          navigation = navigation
          content = content
          breadcrumbs = []
          accountMenu = None
          theme = ComponentsTheme.sky }

    let withBreadcrumbs breadcrumbs (config:AppShellConfig<'destination>) = { config with breadcrumbs = breadcrumbs }
    let withAccountMenu accountMenu (config:AppShellConfig<'destination>) = { config with accountMenu = Some accountMenu }
    let withTheme theme (config:AppShellConfig<'destination>) = { config with theme = theme }

    let render resolve config =
        div {
            _class (ContractHtml.classes [ ComponentsTheme.className config.theme; "grid min-h-[36rem] bg-[var(--fve-page)] text-[var(--fve-text)] lg:grid-cols-[16rem_1fr]" ])
            aside {
                _class "hidden border-r border-[var(--fve-border)] bg-[var(--fve-surface)] p-4 lg:block"
                strong { _class "block px-3 py-2 text-base"; config.productName }
                nav {
                    _ariaLabel "Primary"
                    _class "mt-5 grid gap-1"
                    for item in config.navigation do
                        a {
                            _href (resolve item.destination)
                            if item.destination = config.current then _ariaCurrent "page"
                            _class (
                                if item.destination = config.current then
                                    "rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-subtle)] px-3 py-2 text-sm font-semibold text-[var(--fve-brand-text)]"
                                else
                                    "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)]")
                            item.label
                        }
                }
            }
            div {
                _class "min-w-0"
                header {
                    _class "flex min-h-16 items-center justify-between border-b border-[var(--fve-border)] bg-[var(--fve-surface)] px-4 sm:px-6"
                    nav {
                        _ariaLabel "Breadcrumb"
                        _class "flex items-center gap-2 text-sm text-[var(--fve-muted-text)]"
                        for breadcrumb in config.breadcrumbs do
                            a { _href (resolve breadcrumb.destination); _class "hover:text-[var(--fve-text)]"; breadcrumb.label }
                    }
                    config.accountMenu |> Option.defaultValue empty
                }
                main { _class "mx-auto max-w-7xl p-4 sm:p-6 lg:p-8"; config.content }
            }
        }
