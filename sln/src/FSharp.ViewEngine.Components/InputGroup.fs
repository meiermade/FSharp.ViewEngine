namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

/// <category>input-group</category>
[<RequireQualifiedAccess>]
type InputGroupPosition =
    | Leading
    | Trailing

/// <category>input-group</category>
[<RequireQualifiedAccess>]
type InputGroupControl =
    | Input of InputType
    | Textarea of rows:int

/// <category>input-group</category>
[<NoEquality; NoComparison>]
type InputGroupAddon =
    private
        { position:InputGroupPosition
          content:HtmlElement
          decorative:bool }

/// <category>input-group</category>
[<NoEquality; NoComparison>]
type InputGroupConfig =
    private
        { id:string
          name:string
          label:string
          control:InputGroupControl
          value:string
          addons:InputGroupAddon list
          footer:HtmlElement option
          description:string option
          validation:string option
          required:bool
          disabled:bool
          pending:bool
          attributes:HtmlAttribute list }

/// <category>input-group</category>
[<RequireQualifiedAccess>]
module InputGroupAddon =
    let text position value =
        { position = position
          content = span { _class "min-w-0 [overflow-wrap:anywhere]"; TextField.requiredText (nameof value) value }
          decorative = true }

    let icon position icon = { position = position; content = icon; decorative = true }
    let keyboard position hint = { position = position; content = Kbd.create hint |> Kbd.render; decorative = true }
    let action position action = { position = position; content = action; decorative = false }

/// <summary>
/// A unified input or textarea frame with decorative and interactive addons.
/// </summary>
/// <remarks>
/// Decorative addons are not focus targets or form values.
/// Interactive addons require accessible names and keep native Tab order.
/// Controls and addons wrap when their preferred widths no longer fit; footers keep their own row.
/// </remarks>
/// <category>input-group</category>
[<RequireQualifiedAccess>]
module InputGroup =
    let create id name label =
        { id = TextField.stableId id
          name = TextField.requiredText (nameof name) name
          label = TextField.requiredText (nameof label) label
          control = InputGroupControl.Input InputType.Text
          value = ""
          addons = []
          footer = None
          description = None
          validation = None
          required = false
          disabled = false
          pending = false
          attributes = [] }

    let withInputType kind (config:InputGroupConfig) = { config with control = InputGroupControl.Input kind }

    let asTextarea rows (config:InputGroupConfig) =
        if rows < 1 then invalidArg (nameof rows) "A textarea needs at least one row."
        { config with control = InputGroupControl.Textarea rows }

    let withValue value (config:InputGroupConfig) = { config with value = value }
    let withAddon addon (config:InputGroupConfig) = { config with addons = config.addons @ [ addon ] }
    let withFooter footer (config:InputGroupConfig) = { config with footer = Some footer }
    let withDescription description (config:InputGroupConfig) = { config with description = Some(TextField.requiredText (nameof description) description) }
    let withValidation message (config:InputGroupConfig) = { config with validation = Some(TextField.requiredText (nameof message) message) }
    let required (config:InputGroupConfig) = { config with required = true }
    let disabled (config:InputGroupConfig) = { config with disabled = true }
    let pending (config:InputGroupConfig) = { config with pending = true }
    let withAttributes attributes (config:InputGroupConfig) = { config with attributes = attributes }

    let private inputType = function
        | InputType.Text -> "text" | InputType.Email -> "email" | InputType.Telephone -> "tel"
        | InputType.Password -> "password" | InputType.Number -> "number" | InputType.Search -> "search"
        | InputType.Url -> "url" | InputType.Date -> "date" | InputType.Time -> "time" | InputType.DateTimeLocal -> "datetime-local"

    let render (config:InputGroupConfig) =
        let field =
            Field.create config.id config.label
            |> (match config.description with Some description -> Field.withDescription description | None -> id)
            |> (match config.validation with Some message -> Field.withValidation message | None -> id)
            |> (if config.required then Field.required else id)
            |> (if config.disabled then Field.disabled else id)
            |> (if config.pending then Field.pending else id)
        let renderAddon addon =
            span {
                if addon.decorative then _ariaHidden true
                _class (ComponentHtml.classes [
                    "flex min-w-0 max-w-full shrink-0 items-center justify-center text-sm text-[var(--fve-muted-text)]"
                    if addon.decorative then "pointer-events-none" ])
                addon.content
            }
        let control (fieldAttributes:HtmlAttribute list) : HtmlElement =
            let leading = config.addons |> List.filter (fun addon -> addon.position = InputGroupPosition.Leading)
            let trailing = config.addons |> List.filter (fun addon -> addon.position = InputGroupPosition.Trailing)
            div {
                _class (ComponentHtml.classes [
                    "fve-input-group flex min-w-0 flex-wrap items-center gap-x-2 gap-y-2 overflow-hidden rounded-[var(--fve-radius-control)] border bg-[var(--fve-surface)] px-3 has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2"
                    if config.validation.IsSome then "border-[var(--fve-critical-ring)] has-[:focus-visible]:outline-[var(--fve-critical-ring)]" else "border-[var(--fve-border)] has-[:focus-visible]:outline-[var(--fve-brand-ring)]"
                    if config.disabled then "opacity-50"
                    if config.pending then "bg-[var(--fve-neutral-subtle)]" ])
                if not (List.isEmpty leading) then
                    div {
                        _class "flex min-w-0 max-w-full flex-wrap items-center gap-2"
                        for addon in leading do renderAddon addon
                    }
                match config.control with
                | InputGroupControl.Input kind ->
                    input {
                        for attribute in fieldAttributes do attribute
                        _name config.name
                        _type (inputType kind)
                        _value config.value
                        _class "w-full min-h-[calc(var(--fve-control-min-height)-2px)] min-w-0 max-w-full flex-[1_0_8rem] border-0 bg-transparent py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] text-[var(--fve-text)] outline-none placeholder:text-[var(--fve-muted-text)]"
                    }
                | InputGroupControl.Textarea rows ->
                    textarea {
                        for attribute in fieldAttributes do attribute
                        _name config.name
                        _rows rows
                        _class "w-full min-h-24 min-w-0 max-w-full flex-[1_0_8rem] resize-y border-0 bg-transparent py-3 text-sm leading-6 text-[var(--fve-text)] outline-none placeholder:text-[var(--fve-muted-text)]"
                        config.value
                    }
                if not (List.isEmpty trailing) then
                    div {
                        _class "flex min-w-0 max-w-full flex-wrap items-center gap-2"
                        for addon in trailing do renderAddon addon
                    }
                match config.footer with
                | Some footer -> div { _class "-mx-3 flex w-[calc(100%+1.5rem)] min-w-0 flex-none flex-wrap items-center justify-between gap-2 border-t border-[var(--fve-border)] px-3 py-2"; footer }
                | None -> ()
            }
        field
        |> Field.withAttributes config.attributes
        |> Field.render control
