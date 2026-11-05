namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Svg
open type Datastar

[<NoEquality; NoComparison>]
type private CommandContent =
    { label:string
      keywords:string list
      description:string option
      leading:HtmlElement option
      shortcut:string option
      disabled:bool
      readOnly:bool
      body:HtmlElement
      attributes:HtmlAttribute list }

/// <category>command</category>
[<NoEquality; NoComparison>]
type internal CommandItem =
    private
        | Link of CommandContent * string
        | Action of CommandContent * string

/// <category>command</category>
[<NoEquality; NoComparison>]
type internal CommandGroup = private { label:string; items:CommandItem list; attributes:HtmlAttribute list }

/// <category>command</category>
[<NoEquality; NoComparison>]
type internal CommandConfig =
    private
        { id:string
          label:string
          groups:CommandGroup list
          placeholder:string
          emptyLabel:string
          attributes:HtmlAttribute list }

/// <category>command</category>
[<AutoOpen>]
module CommandAttributes =
    type Html with
        static member _keywords(values:string list) = Html._attr("keywords", System.Text.Json.JsonSerializer.Serialize values)
        static member _shortcut(value:string) = Html._attr("shortcut", value)
        static member _emptyLabel(value:string) = Html._attr("empty-label", value)

type internal CommandItemElement(item:CommandItem) =
    inherit HtmlElement()
    member _.Item = item
    override _.Render _ = invalidOp "fveCommandItem belongs inside fveCommandGroup."

type internal CommandGroupElement(group:CommandGroup) =
    inherit HtmlElement()
    member _.Group = group
    override _.Render _ = invalidOp "fveCommandGroup belongs inside fveCommand or fveCommandDialog."

module internal CommandParts =
    let private flag name attributes =
        match ComponentElement.attribute name attributes with Some {Value = ValueNone} -> true | Some {Value = ValueSome value} -> value <> "false" | None -> false

    let item authored children =
        let attributes = ComponentElement.attributes authored
        let parts, body = ComponentParts.split "CommandItem" children
        let label = ComponentElement.value "label" attributes |> Option.defaultWith (fun () -> ComponentContent.plainText "Rich command items without _label" body)
        NativeOverlay.requireText "label" "A command label is required." label
        let keywords =
            match ComponentElement.value "keywords" attributes with
            | None -> []
            | Some value ->
                try
                    let values = System.Text.Json.JsonSerializer.Deserialize<string[]>(value)
                    if isNull values || (values |> Array.exists isNull) then invalidArg "keywords" "Command keywords must be strings."
                    Array.toList values
                with :? System.Text.Json.JsonException -> invalidArg "keywords" "Command keywords must be an array of strings."
        let optional name = ComponentElement.value name attributes |> Option.map (fun value -> NativeOverlay.requireText name $"A supplied {name} cannot be empty." value; value)
        match ComponentElement.value "id" attributes with
        | Some id when String.IsNullOrWhiteSpace id || (id |> Seq.exists Char.IsWhiteSpace) -> invalidArg "id" "Command item IDs must be stable and contain no whitespace."
        | _ -> ()
        let content = {
            label = label
            keywords = keywords
            description = optional "description"
            leading = Map.tryFind "leading" parts
            shortcut = optional "shortcut"
            disabled = flag "disabled" attributes
            readOnly = flag "readonly" attributes || flag "aria-disabled" attributes
            body = if body.IsEmpty then Html.text label else ComponentContent.fragment body
            attributes = ComponentHtml.safeAttributes ["label"; "keywords"; "description"; "shortcut"; "disabled"; "readonly"; "aria-disabled"] attributes }
        let item =
            match ComponentElement.value "href" attributes with
            | Some href -> Link(content, href)
            | None ->
                if not (attributes |> List.exists (fun attribute -> attribute.Name.StartsWith("data-on:click", StringComparison.OrdinalIgnoreCase))) then invalidArg "data-on:click" "A command action needs a native click action."
                Action(content, ComponentElement.value "data-on:click" attributes |> Option.defaultValue "void 0")
        CommandItemElement(item) :> HtmlElement

    let group attributes (children:HtmlElement list) =
        let items = children |> List.choose (function :? CommandItemElement as item -> Some item.Item | :? NoopElement -> None | _ -> invalidArg "children" "Command groups accept fveCommandItem children.")
        CommandGroupElement { label = ComponentElement.required "label" attributes; items = items; attributes = ComponentHtml.safeAttributes ["label"] attributes } :> HtmlElement

    let configuration attributes (children:HtmlElement list) =
        let id = ComponentElement.required "id" attributes
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg "id" "Command IDs cannot contain whitespace."
        let groups = children |> List.choose (function :? CommandGroupElement as group -> Some group.Group | :? NoopElement -> None | _ -> invalidArg "children" "Use fveCommandGroup children.")
        let emptyLabel = ComponentElement.value "empty-label" attributes |> Option.defaultValue "No results found."
        NativeOverlay.requireText "empty-label" "An empty-result label is required." emptyLabel
        { id = id
          label = ComponentElement.required "aria-label" attributes
          groups = groups
          placeholder = ComponentElement.value "placeholder" attributes |> Option.defaultValue "Type a command or search…"
          emptyLabel = emptyLabel
          attributes = ComponentHtml.safeAttributes ["aria-label"; "placeholder"; "empty-label"] attributes }

    let leading attributes children =
        let content = if List.isEmpty attributes then ComponentContent.fragment children else ComponentElement.render "span" [] attributes children
        ComponentPart("CommandItem", "leading", content) :> HtmlElement

/// <summary>
/// A grouped, locally searchable command menu with real links, trusted actions and combobox keyboard behavior.
/// Query and active-option state are ephemeral; the consumer owns available commands and their effects.
/// </summary>
/// <remarks>
/// Shortcut labels do not register hotkeys. Matching ranks titles before keywords and descriptions;
/// group order, available commands and trusted action effects remain consumer-owned.
/// </remarks>
/// <category>command</category>
[<RequireQualifiedAccess>]
module Command =
    /// Ordinary attributes for a consumer-owned native button, addressed by command ID rather than configuration.
    let triggerAttributes (commandId:string) =
        NativeOverlay.requireText (nameof commandId) "A stable command ID is required." commandId
        NativeOverlay.triggerAttributes (commandId + "-dialog") (Some(commandId + "-input"))

    let private initialize = """
const root = el;
const words = value => value.toLowerCase().match(/[\p{L}\p{N}]+/gu) || [];
root._fveCommandItems = () => [...root.querySelectorAll('[data-fve-command-item]')].filter(item => !item.hidden && !item.closest('[data-fve-command-group]').hidden && item.getAttribute('aria-disabled') !== 'true');
root._fveCommandSelect = item => {
  const previous = root.querySelector('[data-fve-command-item][aria-selected=true]');
  if (previous && previous !== item) previous.setAttribute('aria-selected', 'false');
  if (item && item.getAttribute('aria-selected') !== 'true') item.setAttribute('aria-selected', 'true');
  root._fveCommandActive = item?.id || '';
  const input = root.querySelector('[data-fve-command-input]');
  if (item) {
    if (input.getAttribute('aria-activedescendant') !== item.id) input.setAttribute('aria-activedescendant', item.id);
  } else if (input.hasAttribute('aria-activedescendant')) input.removeAttribute('aria-activedescendant');
};
root._fveCommandUpdate = (preserve = false) => {
  const input = root.querySelector('[data-fve-command-input]');
  const query = input.value.trim().toLowerCase();
  const terms = words(query);
  for (const group of root.querySelectorAll('[data-fve-command-group]')) {
    const entries = [...group.querySelectorAll('[data-fve-command-item]')];
    const score = item => {
      if (!query) return 0;
      const label = item.dataset.fveCommandLabel.toLowerCase();
      const labels = words(label);
      const keywords = words(item.dataset.fveCommandKeywords);
      const description = words(item.dataset.fveCommandDescription);
      if (label === query) return 0;
      if (label.startsWith(query)) return 1;
      if (terms.length && terms.every(term => labels.some(word => word.startsWith(term)))) return 2;
      if (terms.length && terms.every(term => [...labels, ...keywords].some(word => word.startsWith(term)))) return 3;
      if (terms.length && terms.every(term => [...labels, ...keywords, ...description].includes(term))) return 4;
      return Infinity;
    };
    const ranked = entries.map(item => ({item, score: score(item)})).sort((a,b) => a.score - b.score || Number(a.item.dataset.fveCommandOrder) - Number(b.item.dataset.fveCommandOrder));
    let cursor = entries[0] || null;
    for (const entry of ranked) {
      const hidden = !Number.isFinite(entry.score);
      if (entry.item.hidden !== hidden) entry.item.hidden = hidden;
      if (entry.item !== cursor) group.insertBefore(entry.item, cursor);
      cursor = entry.item.nextElementSibling;
    }
    const hidden = !ranked.some(entry => Number.isFinite(entry.score));
    if (group.hidden !== hidden) group.hidden = hidden;
  }
  const items = root._fveCommandItems();
  root._fveCommandSelect((preserve && items.find(item => item.id === root._fveCommandActive)) || items[0]);
  const hasResults = [...root.querySelectorAll('[data-fve-command-group]')].some(group => !group.hidden);
  const listbox = root.querySelector('[role=listbox]');
  const empty = root.querySelector('[data-fve-command-empty]');
  if (listbox.hidden === hasResults) listbox.hidden = !hasResults;
  if (input.getAttribute('aria-expanded') !== String(hasResults)) input.setAttribute('aria-expanded', String(hasResults));
  if (empty.hidden !== hasResults) empty.hidden = hasResults;
  return items;
};
root._fveCommandUpdate();
"""
    let private keyboard = """
if (evt.target.matches('[data-fve-command-input]') && !evt.isComposing && !evt.ctrlKey && !evt.metaKey && !evt.altKey) {
  if (['ArrowDown','ArrowUp','Home','End','Enter'].includes(evt.key)) {
    evt.preventDefault();
    const items = el._fveCommandItems();
    const index = items.findIndex(item => item.id === el._fveCommandActive);
    if (evt.key === 'Enter') items[index]?.click();
    else {
      const next = evt.key === 'Home' ? items[0] : evt.key === 'End' ? items.at(-1) : items.at((index + (evt.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length);
      el._fveCommandSelect(next);
      next?.scrollIntoView({block:'nearest', inline:'nearest'});
    }
  } else if (evt.key === 'Escape' && !el.closest('dialog')) {
    const input = el.querySelector('[data-fve-command-input]');
    input.value = ''; el._fveCommandUpdate();
  }
}
"""
    let private renderContent framed (config:CommandConfig) =
        let inputId = config.id + "-input"
        let listId = config.id + "-list"
        let hasItems = config.groups |> List.exists (fun group -> not (List.isEmpty group.items))
        div {
            _id config.id
            _data ("fve-command", "true")
            _class ("flex min-h-0 min-w-0 w-full flex-col overflow-hidden text-[var(--fve-text)] " + (if framed then "rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[var(--fve-surface)]" else "bg-[var(--fve-background)]") + " " + ComponentContent.classes config.attributes)
            for attribute in ComponentHtml.safeAttributes [ "id"; "class"; "data-init"; "data-on:input"; "data-on:keydown"; "data-on:click"; "data-on:focusin" ] config.attributes do attribute
            _dataInit initialize
            _dataOn ("input", "el._fveCommandUpdate()")
            _dataOn ("focusin", "if (evt.target.matches('[data-fve-command-input]')) el._fveCommandUpdate(true)")
            _dataOn ("keydown", keyboard)
            _dataOn ("click", "if (!evt.ctrlKey && !evt.metaKey && !evt.altKey && !evt.shiftKey && evt.target.closest('[data-fve-command-item]:not([aria-disabled=true])')) el.closest('dialog')?.close()")
            div {
                _class "flex shrink-0 items-center gap-2 border-b border-[var(--fve-border)] px-3"
                svg {
                    _viewBox "0 0 24 24"
                    _fill "none"
                    _ariaHidden true
                    _class "size-4 shrink-0 text-[var(--fve-muted-text)]"
                    _attr ("stroke", "currentColor")
                    _attr ("stroke-width", "1.5")
                    path { _attr ("stroke-linecap", "round"); _attr ("stroke-linejoin", "round"); _d "m21 21-4.34-4.34M11 19a8 8 0 1 1 0-16 8 8 0 0 1 0 16Z" }
                }
                input {
                    _id inputId
                    _type "text"
                    _role "combobox"
                    _ariaLabel config.label
                    _ariaControls listId
                    _ariaExpanded hasItems
                    _attr ("aria-autocomplete", "list")
                    _data ("fve-command-input", "true")
                    _placeholder config.placeholder
                    _autocomplete "off"
                    _class "h-11 min-w-0 flex-1 border-0 bg-transparent px-0 text-base text-[var(--fve-text)] placeholder:text-[var(--fve-muted-text)] focus:outline-none"
                }
            }
            div {
                _id listId
                _role "listbox"
                _hidden (not hasItems)
                _ariaLabel config.label
                _class "max-h-[min(24rem,calc(100dvh-8rem))] min-h-0 overflow-y-auto p-1"
                for groupIndex, group in config.groups |> List.indexed do
                    div {
                        _role "group"
                        _ariaLabel group.label
                        _hidden (List.isEmpty group.items)
                        _data ("fve-command-group", "true")
                        _class ("py-1 not-first:border-t not-first:border-[var(--fve-border)] " + ComponentContent.classes group.attributes)
                        for attribute in ComponentHtml.safeAttributes ["class"; "role"; "aria-label"; "hidden"; "data-fve-command-group"] group.attributes do attribute
                        div { _ariaHidden true; _class "px-2 py-1.5 text-xs font-medium text-[var(--fve-muted-text)]"; group.label }
                        for itemIndex, item in group.items |> List.indexed do
                            let content = match item with Link(content, _) | Action(content, _) -> content
                            let unavailable = content.disabled || content.readOnly
                            let defaults = [
                                _id (ComponentElement.value "id" content.attributes |> Option.defaultValue $"{config.id}-item-{groupIndex}-{itemIndex}")
                                _role "option"
                                _ariaSelected false
                                _tabindex -1
                                _data ("fve-command-item", "true")
                                _data ("fve-command-label", content.label)
                                _data ("fve-command-keywords", String.concat " " content.keywords)
                                _data ("fve-command-description", defaultArg content.description "")
                                _data ("fve-command-order", string itemIndex)
                                _ariaDisabled unavailable
                                _class "flex min-h-8 w-full cursor-pointer items-center gap-2 rounded-[var(--fve-radius-control)] border-0 bg-transparent px-2 py-1.5 text-left text-sm text-[var(--fve-text)] no-underline outline-none aria-selected:bg-[var(--fve-surface-active)] hover:bg-[var(--fve-surface-hover)] aria-disabled:cursor-not-allowed aria-disabled:opacity-50"
                                _dataOn ("pointermove", "if (el.getAttribute('aria-disabled') !== 'true') el.closest('[data-fve-command]')._fveCommandSelect(el)")
                                if unavailable then _dataOn ("click", "evt.preventDefault(); evt.stopPropagation()") ]
                            let authored =
                                ComponentHtml.safeAttributes ["id"; "type"; "role"; "href"; "aria-selected"; "tabindex"; "data-fve-command-item"; "data-fve-command-label"; "data-fve-command-keywords"; "data-fve-command-description"; "data-fve-command-order"; "data-on:pointermove"; "data-on:click"] content.attributes
                                |> List.filter (fun attribute -> not unavailable || not (attribute.Name.StartsWith("data-on:click", StringComparison.OrdinalIgnoreCase) || attribute.Name.StartsWith("data-on:keydown", StringComparison.OrdinalIgnoreCase)))
                            let attributes = ComponentElement.attributes (defaults @ authored)
                            let body = fragment {
                                if ComponentElement.value "aria-busy" content.attributes = Some "true" then ComponentHtml.loadingGlyph Size.Small
                                else
                                    match content.leading with
                                    | Some leading -> span { _ariaHidden true; _class "inline-flex size-4 shrink-0 items-center justify-center [&>svg]:size-4"; leading }
                                    | None -> ()
                                span {
                                    _class "min-w-0 flex-1"
                                    span { _class "block"; content.body }
                                    match content.description with
                                    | Some description -> span { _class "block text-xs text-[var(--fve-muted-text)]"; description }
                                    | None -> ()
                                }
                                match content.shortcut with
                                | Some shortcut -> kbd { _class "ml-auto shrink-0 font-mono text-xs text-[var(--fve-muted-text)]"; shortcut }
                                | None -> ()
                            }
                            match item with
                            | Link(_, destination) ->
                                a {
                                    for attribute in attributes do attribute
                                    if not unavailable then _href destination
                                    body
                                }
                            | Action(_, expression) ->
                                button {
                                    for attribute in attributes do attribute
                                    _type (ComponentElement.value "type" content.attributes |> Option.defaultValue "button")
                                    if content.disabled then _disabled true
                                    if not unavailable then _dataOn ("click", expression)
                                    body
                                }
                    }
            }
            div {
                _data ("fve-command-empty", "true")
                _hidden hasItems
                _role "status"
                _ariaLive "polite"
                _class "px-3 py-6 text-center text-sm text-[var(--fve-muted-text)]"
                config.emptyLabel
            }
        }
    let internal render attributes children = renderContent true (CommandParts.configuration attributes children)

    let internal renderDialog attributes children =
        let config = CommandParts.configuration attributes children
        let dialogId = config.id + "-dialog"
        dialog {
            _id dialogId
            _ariaLabel config.label
            _ariaModal true
            _data("fve-initial-focus", config.id + "-input")
            _dataOn ("close", NativeOverlay.restoreFocusExpression dialogId)
            _dataOn ("click", NativeOverlay.dismissOnBackdropExpression dialogId)
            _class "m-auto max-h-[calc(100dvh-2rem)] w-[min(32rem,calc(100vw-2rem))] overflow-hidden rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[var(--fve-background)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-[var(--fve-overlay-backdrop)] backdrop:backdrop-blur-[2px]"
            renderContent false config
        }

/// <category>command</category>
[<AutoOpen>]
module CommandDsl =
    let fveCommand = ComponentElementBuilder(Command.render)
    let fveCommandDialog = ComponentElementBuilder(Command.renderDialog)
    let fveCommandGroup = ComponentElementBuilder(CommandParts.group)
    let fveCommandItem = ComponentElementBuilder(CommandParts.item)
    let fveCommandItemLeading = ComponentElementBuilder(CommandParts.leading)
