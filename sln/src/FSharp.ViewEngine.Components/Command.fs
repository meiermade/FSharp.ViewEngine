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
      disabled:bool }

/// <category>command</category>
[<NoEquality; NoComparison>]
type CommandItem<'destination> =
    private
        | Link of CommandContent * 'destination
        | Action of CommandContent * string

/// <category>command</category>
[<RequireQualifiedAccess>]
module CommandItem =
    let private content label =
        NativeOverlay.requireText (nameof label) "A command label is required." label
        { label = label; keywords = []; description = None; leading = None; shortcut = None; disabled = false }
    let private map update = function
        | Link(content, destination) -> Link(update content, destination)
        | Action(content, expression) -> Action(update content, expression)
    let link destination label = Link(content label, destination)
    /// A trusted Datastar action. The caller owns durable state and any displayed shortcut behavior.
    let action expression label =
        NativeOverlay.requireText (nameof expression) "A command action is required." expression
        Action(content label, expression)
    let withKeywords keywords item = map (fun content -> { content with keywords = keywords }) item
    let withDescription description item = map (fun content -> { content with description = Some description }) item
    let withLeading leading item = map (fun content -> { content with leading = Some leading }) item
    /// Displays a shortcut; registering that hotkey remains caller-owned.
    let withShortcut shortcut item =
        NativeOverlay.requireText (nameof shortcut) "A command shortcut is required." shortcut
        map (fun content -> { content with shortcut = Some shortcut }) item
    let disabled item = map (fun content -> { content with disabled = true }) item

/// <category>command</category>
[<NoEquality; NoComparison>]
type CommandGroup<'destination> = private { label:string; items:CommandItem<'destination> list }

/// <category>command</category>
[<RequireQualifiedAccess>]
module CommandGroup =
    let create label items =
        NativeOverlay.requireText (nameof label) "A command group label is required." label
        { label = label; items = items }

/// <category>command</category>
[<NoEquality; NoComparison>]
type CommandConfig<'destination> =
    private
        { id:string
          label:string
          groups:CommandGroup<'destination> list
          placeholder:string
          emptyLabel:string
          attributes:HtmlAttribute list }

/// <summary>
/// A grouped, locally searchable command menu with real links, trusted actions and combobox keyboard behavior.
/// Query and active-option state are ephemeral; the consumer owns available commands and their effects.
/// </summary>
/// <category>command</category>
[<RequireQualifiedAccess>]
module Command =
    let create id label groups =
        NativeOverlay.requireText (nameof id) "A stable command ID is required." id
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "Command IDs cannot contain whitespace."
        NativeOverlay.requireText (nameof label) "An accessible command label is required." label
        { id = id; label = label; groups = groups; placeholder = "Type a command or search…"; emptyLabel = "No results found."; attributes = [] }
    let withPlaceholder placeholder (config:CommandConfig<'destination>) = { config with placeholder = placeholder }
    let withEmptyState label config =
        NativeOverlay.requireText (nameof label) "An empty-result label is required." label
        { config with emptyLabel = label }
    let withAttributes attributes (config:CommandConfig<'destination>) = { config with attributes = attributes }

    let private initialize = """
const root = el;
const words = value => value.toLowerCase().match(/[\p{L}\p{N}]+/gu) || [];
root._fveCommandSelect = item => {
  root._fveCommandActive = item?.id || '';
  root.querySelectorAll('[data-fve-command-item]').forEach(option => option.setAttribute('aria-selected', String(option === item)));
  const input = root.querySelector('[data-fve-command-input]');
  if (item) input.setAttribute('aria-activedescendant', item.id); else input.removeAttribute('aria-activedescendant');
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
    for (const entry of ranked) { entry.item.hidden = !Number.isFinite(entry.score); group.append(entry.item); }
    group.hidden = !ranked.some(entry => Number.isFinite(entry.score));
  }
  const items = [...root.querySelectorAll('[data-fve-command-item]')].filter(item => !item.hidden && !item.closest('[data-fve-command-group]').hidden && item.getAttribute('aria-disabled') !== 'true');
  root._fveCommandSelect((preserve && items.find(item => item.id === root._fveCommandActive)) || items[0]);
  const hasResults = [...root.querySelectorAll('[data-fve-command-group]')].some(group => !group.hidden);
  root.querySelector('[role=listbox]').hidden = !hasResults;
  input.setAttribute('aria-expanded', String(hasResults));
  root.querySelector('[data-fve-command-empty]').hidden = hasResults;
  return items;
};
root._fveCommandUpdate();
"""
    let private keyboard = """
if (evt.target.matches('[data-fve-command-input]') && !evt.isComposing && !evt.ctrlKey && !evt.metaKey && !evt.altKey) {
  if (['ArrowDown','ArrowUp','Home','End','Enter'].includes(evt.key)) {
    evt.preventDefault();
    const items = el._fveCommandUpdate(true);
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
    let private renderContent framed resolve config =
        let inputId = config.id + "-input"
        let listId = config.id + "-list"
        let hasItems = config.groups |> List.exists (fun group -> not (List.isEmpty group.items))
        div {
            _id config.id
            _data ("fve-command", "true")
            _class ("flex min-h-0 min-w-0 w-full flex-col overflow-hidden bg-[var(--fve-surface)] text-[var(--fve-text)] " + (if framed then "rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)]" else ""))
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
                        _class "py-1 not-first:border-t not-first:border-[var(--fve-border)]"
                        div { _ariaHidden true; _class "px-2 py-1.5 text-xs font-medium text-[var(--fve-muted-text)]"; group.label }
                        for itemIndex, item in group.items |> List.indexed do
                            let content = match item with Link(content, _) | Action(content, _) -> content
                            let attributes = [
                                _id $"{config.id}-item-{groupIndex}-{itemIndex}"
                                _role "option"
                                _ariaSelected false
                                _tabindex -1
                                _data ("fve-command-item", "true")
                                _data ("fve-command-label", content.label)
                                _data ("fve-command-keywords", String.concat " " content.keywords)
                                _data ("fve-command-description", defaultArg content.description "")
                                _data ("fve-command-order", string itemIndex)
                                _ariaDisabled content.disabled
                                _class "flex min-h-8 w-full cursor-pointer items-center gap-2 rounded-[var(--fve-radius-control)] border-0 bg-transparent px-2 py-1.5 text-left text-sm text-[var(--fve-text)] no-underline outline-none aria-selected:bg-[var(--fve-surface-active)] hover:bg-[var(--fve-surface-hover)] aria-disabled:cursor-not-allowed aria-disabled:opacity-50"
                                _dataOn ("pointermove", "if (el.getAttribute('aria-disabled') !== 'true') el.closest('[data-fve-command]')._fveCommandSelect(el)")
                                if content.disabled then _dataOn ("click", "evt.preventDefault(); evt.stopPropagation()") ]
                            let body = fragment {
                                match content.leading with
                                | Some leading -> span { _ariaHidden true; _class "inline-flex size-4 shrink-0 items-center justify-center [&>svg]:size-4"; leading }
                                | None -> ()
                                span {
                                    _class "min-w-0 flex-1"
                                    span { _class "block"; content.label }
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
                                    _href (resolve destination)
                                    body
                                }
                            | Action(_, expression) ->
                                button {
                                    for attribute in attributes do attribute
                                    _type "button"
                                    if content.disabled then _disabled true else _dataOn ("click", expression)
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
    let render resolve config = renderContent true resolve config
    /// Attributes for a consumer-authored button that opens this command's dialog and focuses its input.
    let triggerAttributes config = NativeOverlay.triggerAttributes (config.id + "-dialog") (Some(config.id + "-input"))
    let trigger label config = NativeOverlay.trigger (config.id + "-dialog") (Some(config.id + "-input")) label
    /// An optional native-dialog composition, not a separate command implementation.
    let renderDialog resolve config =
        let dialogId = config.id + "-dialog"
        dialog {
            _id dialogId
            _ariaLabel config.label
            _ariaModal true
            _dataOn ("close", NativeOverlay.restoreFocusExpression dialogId)
            _dataOn ("click", NativeOverlay.dismissOnBackdropExpression dialogId)
            _class "m-auto max-h-[calc(100dvh-2rem)] w-[min(32rem,calc(100vw-2rem))] overflow-hidden rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[var(--fve-surface)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-[var(--fve-overlay-backdrop)] backdrop:backdrop-blur-[2px]"
            renderContent false resolve config
        }
