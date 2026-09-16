namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type private ApplicationActionTarget<'destination> =
    | Destination of 'destination
    | Command of string

[<NoEquality; NoComparison>]
type ApplicationAction<'destination> =
    private
        { label:string
          target:ApplicationActionTarget<'destination>
          variant:ButtonVariant
          leading:HtmlElement option
          iconOnly:bool
          disabled:bool
          pending:bool
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module ApplicationAction =
    let private action label target =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An application-action label is required."
        { label = label
          target = target
          variant = ButtonVariant.Secondary
          leading = None
          iconOnly = false
          disabled = false
          pending = false
          attributes = [] }

    let link destination label = action label (Destination destination)

    let command datastarExpression label =
        if String.IsNullOrWhiteSpace datastarExpression then invalidArg (nameof datastarExpression) "A Datastar action expression is required."
        action label (Command datastarExpression)

    let withVariant variant (action:ApplicationAction<'destination>) = { action with variant = variant }
    let withLeading leading (action:ApplicationAction<'destination>) = { action with leading = Some leading }
    let asIconOnly icon (action:ApplicationAction<'destination>) = { action with leading = Some icon; iconOnly = true }
    let disabled (action:ApplicationAction<'destination>) = { action with disabled = true }
    let pending (action:ApplicationAction<'destination>) = { action with pending = true }
    let withAttributes attributes (action:ApplicationAction<'destination>) = { action with attributes = attributes }

[<NoEquality; NoComparison>]
type ActionClusterConfig<'destination> =
    private
        { id:string
          direct:ApplicationAction<'destination> list
          overflow:MenuItem<'destination> list }

[<RequireQualifiedAccess>]
module ActionCluster =
    let private validateDirect (actions:ApplicationAction<'destination> list) =
        if actions.Length > 2 then invalidArg (nameof actions) "An action cluster supports at most two direct actions. Place additional actions in overflow."
        let primaryCount = actions |> List.filter (fun action -> action.variant = ButtonVariant.Primary) |> List.length
        if primaryCount > 1 then invalidArg (nameof actions) "An action cluster supports at most one primary action."
        actions

    let create id directActions =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable action-cluster ID is required."
        { id = id; direct = validateDirect directActions; overflow = [] }

    let private trimSeparators items =
        items
        |> List.skipWhile MenuItem.isSeparator
        |> List.rev
        |> List.skipWhile MenuItem.isSeparator
        |> List.rev

    let internal normalizeOverflow items =
        let destructive, regular = items |> List.partition MenuItem.isDestructive
        let regular = regular |> trimSeparators
        match regular, destructive with
        | [], destructive -> destructive
        | regular, [] -> regular
        | regular, destructive -> regular @ [ MenuItem.separator ] @ destructive

    let withOverflow items (config:ActionClusterConfig<'destination>) =
        { config with overflow = normalizeOverflow items }

    let private actionAsOverflowItem (action:ApplicationAction<'destination>) =
        let item =
            match action.target, action.variant with
            | Command expression, ButtonVariant.Destructive -> MenuItem.destructiveAction expression action.label
            | Command expression, _ -> MenuItem.action expression action.label
            | Destination destination, ButtonVariant.Destructive -> MenuItem.destructiveLink destination action.label
            | Destination destination, _ -> MenuItem.link destination action.label
        let item = action.leading |> Option.map (fun leading -> item |> MenuItem.withLeading leading) |> Option.defaultValue item
        let item = if action.disabled then item |> MenuItem.disabled else item
        let item = if action.pending then item |> MenuItem.pending else item
        item |> MenuItem.withClass "@min-[400px]:hidden"

    let private renderAction resolve (action:ApplicationAction<'destination>) =
        let unavailable = action.disabled || action.pending
        let classes =
            ComponentHtml.classes [
                ButtonStyles.baseClasses
                if action.iconOnly then ComponentHtml.iconButtonSizeClasses ControlSize.Medium else ComponentHtml.sizeClasses ControlSize.Medium
                ButtonStyles.variantClasses action.variant ]
        let content =
            fragment {
                if action.pending then
                    ComponentHtml.loadingGlyph ControlSize.Medium
                else
                    action.leading |> Option.defaultValue empty
                if not action.iconOnly then action.label
            }

        match action.target with
        | Destination destination ->
            a {
                if not unavailable then _href (resolve destination)
                if action.iconOnly then _ariaLabel action.label
                if unavailable then _ariaDisabled true
                if action.pending then _ariaBusy true
                _class classes
                for attribute in ComponentHtml.safeAttributes [ "href"; "aria-label"; "aria-disabled"; "aria-busy"; "class" ] action.attributes do attribute
                content
            }
        | Command expression ->
            button {
                _type "button"
                _disabled unavailable
                if action.iconOnly then _ariaLabel action.label
                if action.pending then _ariaBusy true
                if not unavailable then _dataOn ("click", expression)
                _class classes
                for attribute in ComponentHtml.safeAttributes [ "type"; "disabled"; "aria-label"; "aria-busy"; "class"; "data-on:" ] action.attributes do attribute
                content
            }

    let render resolve (config:ActionClusterConfig<'destination>) =
        let priorityIndex =
            config.direct
            |> List.tryFindIndex (fun action -> action.variant = ButtonVariant.Primary)
            |> Option.defaultValue 0
        let compactOverflow =
            config.direct
            |> List.indexed
            |> List.choose (fun (index, action) -> if index = priorityIndex then None else Some(actionAsOverflowItem action))
        let overflow = normalizeOverflow (compactOverflow @ config.overflow)
        div {
            _id config.id
            _attr ("data-fve-action-cluster", "true")
            _class "flex shrink-0 items-center justify-end gap-2"
            for index, action in config.direct |> List.indexed do
                if index = priorityIndex then
                    renderAction resolve action
                else
                    span { _class "hidden @min-[400px]:inline-flex"; renderAction resolve action }
            if not overflow.IsEmpty then
                div {
                    if config.overflow.IsEmpty then _class "@min-[400px]:hidden"
                    DropdownMenu.create $"{config.id}-overflow" "More actions" overflow
                    |> DropdownMenu.asOverflow
                    |> DropdownMenu.render resolve
                }
        }

[<NoEquality; NoComparison>]
type RowActionsConfig<'destination> =
    private
        { id:string
          label:string
          items:MenuItem<'destination> list }

[<RequireQualifiedAccess>]
module RowActions =
    let create id rowLabel items =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable row-actions ID is required."
        if String.IsNullOrWhiteSpace rowLabel then invalidArg (nameof rowLabel) "An accessible row label is required."
        if List.isEmpty items then invalidArg (nameof items) "Row actions require at least one menu item."
        { id = id; label = $"More actions for {rowLabel}"; items = ActionCluster.normalizeOverflow items }

    let render resolve config =
        DropdownMenu.create config.id config.label config.items
        |> DropdownMenu.asOverflow
        |> DropdownMenu.render resolve
