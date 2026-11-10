namespace Example.Server.ApplicationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain
open Controls

module internal Collection =
    let private pending = "_example_collection_pending"
    let private submissionAttributes =
        [ _dataIndicator pending; _dataOn ("submit", Navigation.formSubmitAction pending) ]
    let private pendingButtonAttributes blocked =
        [ _dataAttr ("disabled", if blocked then "true" else "$" + pending)
          _dataAttr ("aria-busy", "$" + pending + " ? 'true' : null") ]

    let restoreAction (query: Query) page =
        let pristine = query.records = initialRecords
        form {
            _id (elementId query "template-restore-records-form")
            _method "post"
            _action (collectionHref query page)
            _class "inline-flex max-w-full"
            for attribute in submissionAttributes do attribute
            editorContextFields query
            button {
                _type "submit"; _name "action"; _value "reset-records"
                _disabled pristine
                for attribute in pendingButtonAttributes pristine do attribute
                _class "inline-flex min-h-[var(--fve-control-min-height)] max-w-full items-center justify-center rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-transparent px-3 py-[max(0px,calc(var(--fve-control-padding-block)-1px))] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium enabled:hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                "Restore records"
            }
        }

    let notifications (query: Query) =
        fveNotificationRegion {
            _id (elementId query "template-collection-notifications")
            _ariaLabel "Collection notifications"
            match query.notification with
            | None -> ()
            | Some result ->
                let title, message, color =
                    match result.state with
                    | "selection-deleted" ->
                        let noun = if result.count = 1 then result.resource.Substring(0, result.resource.Length - 1) else result.resource
                        $"{result.count} {noun} deleted", "The selected records were removed.", Color.Success
                    | "selection-reset" -> "Records restored", "You can select and delete records again.", Color.Success
                    | _ -> "Choose available records", "The selection is empty or contains records that are no longer available. Nothing was deleted.", Color.Error
                let id = elementId query ("template-result-" + result.id)
                let announcement = if color = Color.Error then LiveAnnouncement.Assertive else LiveAnnouncement.Polite
                let page = if result.resource = "accounts" then ApplicationPage.Accounts else ApplicationPage.Transactions
                div {
                    div {
                        _id (id + "-enhanced")
                        _hidden true
                        _dataInit "el.hidden = false"
                        fveNotification {
                            _id id; _label title; _color color; _announcement announcement
                            if color = Color.Error then _persistent true
                            else _ttl 5000
                            p { message }
                        }
                    }
                    noscript {
                        fveNotice {
                            _id (id + "-native"); _label title; _color color; _announcement announcement
                            _class "pointer-events-auto shadow-lg"
                            p { message }
                            fveNoticeActions {
                                fveLink {
                                    _href (collectionHref { query with state = "" } page)
                                    _ariaLabel ("Dismiss " + title)
                                    "Dismiss"
                                }
                            }
                        }
                    }
                }
        }

    let private deletionConfirmation (query: Query) (kind: string) page native =
        let suffix = if native then "-native" else ""
        let id = elementId query ("template-" + kind + "-delete-selection" + suffix)
        let formId, cancelId = id + "-form", id + "-cancel"
        let triggerId = elementId query ("template-" + kind + "-delete-selected")
        let noun = if query.selection.Length = 1 then kind.Substring(0, kind.Length - 1) else kind
        let label = $"Delete {query.selection.Length} {noun}?"
        let blocked = query.state = "delete-blocked"
        let body = fragment {
            if blocked then
                notice query (id + "-blocked") "Nothing was deleted"
                    "Accounts with a balance or recorded transactions cannot be deleted. Cancel and change the selection." Color.Error
            fveDescriptionList {
                for key in query.selection do
                    if kind = "accounts" then
                        let account = query.records.accounts |> List.find (fun row -> string row.id = key)
                        fveDescriptionListItem {
                            _label account.name
                            $"{string account.accountType} · {money (balance query.records account)}"
                        }
                    else
                        let transaction = query.records.transactions |> List.find (fun row -> string row.id = key)
                        fveDescriptionListItem {
                            _label transaction.description
                            $"{date transaction.date} · {money transaction.amount}"
                        }
            }
            form {
                _id formId
                _method "post"
                _action (collectionHref query page)
                for attribute in submissionAttributes do attribute
                editorContextFields query
                for key in query.selection do hidden (if kind = "accounts" then "accountIds" else "transactionIds") key
            }
        }
        let actions = fragment {
            button {
                _id cancelId
                _type "submit"
                _form formId
                _name "action"
                _value "cancel-deletion"
                _class "inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] px-3 text-sm hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                if not native then _dataOn ("click", $"evt.preventDefault(); document.getElementById('{id}').close()")
                "Cancel"
            }
            formSubmit Color.Error
                ([ _form formId; _name "action"; _value "delete-selected"; _disabled blocked ] @ pendingButtonAttributes blocked)
                "Delete"
        }
        if native then
            Layout.section label (fragment { body; div { _class "mt-4 flex flex-wrap justify-end gap-3"; actions } })
        else
            fveDialog {
                _id id
                _label label
                _initialFocus cancelId
                _dismissOnBackdrop true
                _class "max-sm:w-[calc(100%-1rem)] max-sm:max-w-[calc(100%-1rem)] max-sm:[&>div]:p-3 [&_h2]:break-words"
                _dataPreserveAttr "open"
                _dataInit $"queueMicrotask(() => {{ if (el.isConnected && !el.open) el.showModal(); document.getElementById('{cancelId}')?.focus() }})"
                _dataOn ("close", $"document.getElementById('{triggerId}')?.focus(); el.remove()")
                body
                fveDialogFooter { _class "flex-wrap"; actions }
            }
    // This collection composition belongs to Ledger, not the installed Components library.

    type CollectionFilter =
        { name: string
          label: string
          value: string
          options: (string * string) list }

    let filterQuery name value (query: Query) =
        match name with
        | "accountType" -> { query with accountType = value }
        | "status" -> { query with status = value }
        | "account" -> { query with account = value }
        | _ -> query

    let collection (query: Query) kind (filters: CollectionFilter list) (rows: HtmlElement) =
        let page =
            if kind = "accounts" then
                ApplicationPage.Accounts
            else
                ApplicationPage.Transactions

        let active =
            filters
            |> List.filter (fun filter ->
                List.contains filter.name query.filters
                || not (filterValues filter.value).IsEmpty)

        let query =
            { query with
                filters = active |> List.map _.name }

        let available =
            filters
            |> List.filter (fun filter -> not (List.contains filter.name query.filters))

        let selectionId = elementId query ("template-" + kind + "-selection")
        let formId = elementId query ("template-" + kind + "-batch-form")

        let countSignal =
            (elementId query ("template-" + kind + "-selection-count")).Replace('-', '_')

        let deleteSelected native =
            formSubmit Color.Error
                ([ _id (elementId query ("template-" + kind + "-delete-selected" + (if native then "-native" else ""))); _form formId; _name "action"; _value "prepare-delete" ] @ pendingButtonAttributes false)
                "Delete selected"

        let clearFilters =
            active
            |> List.fold
                (fun query filter -> filterQuery filter.name "all" query)
                { query with filters = []; state = "" }

        div {
            _attr ("data-ledger-collection", kind)
            _dataPreserveAttr "data-ledger-editing-filter data-ledger-filter-focus"

            _dataOn (
                "keydown",
                "if (evt.key === 'Escape' || evt.key === 'Tab') { delete el.dataset.ledgerEditingFilter; delete el.dataset.ledgerFilterFocus }"
            )

            _dataOn (
                "pointerdown",
                [ "window" ],
                "if (el.dataset.ledgerEditingFilter && evt.target.closest('[data-ledger-filter-group]')?.dataset.ledgerFilterGroup !== el.dataset.ledgerEditingFilter) { delete el.dataset.ledgerEditingFilter; delete el.dataset.ledgerFilterFocus }"
            )

            _class "group/collection flex min-h-0 flex-1 flex-col"
            _attr ("data-signals__ifmissing", $"{{{countSignal}:0,{pending}:false}}")
            _dataOn ("fve-table-selection-change", $"${countSignal} = evt.detail.keys.length")

            _dataOn (
                "fve-navigation-complete",
                [ "window" ],
                (if query.state = "selection-deleted" || query.state = "selection-reset" then
                    $"document.getElementById('{selectionId}')?.dispatchEvent(new CustomEvent('fve-selection-clear')); ${countSignal} = 0;\n"
                 else "") + """
if (evt.detail.intent !== 'navigate') return;
const previous = new URL(evt.detail.previousHref), current = new URL(evt.detail.href);
if (previous.pathname !== current.pathname) return;
const before = (previous.searchParams.get('filters') || '').split(',');
const added = (current.searchParams.get('filters') || '').split(',').find(name => name && !before.includes(name));
const editing = el.dataset.ledgerEditingFilter;
const focusId = el.dataset.ledgerFilterFocus;
delete el.dataset.ledgerEditingFilter;
delete el.dataset.ledgerFilterFocus;
const control = [...el.querySelectorAll('[data-ledger-filter]')].find(control => control.dataset.ledgerFilter === (editing || added));
if (control?.getClientRects().length) {
  const menu = document.getElementById(control.getAttribute('aria-controls'));
  if ((added || editing) && !menu.matches(':popover-open')) control.click();
  (document.getElementById(focusId) || menu).focus({preventScroll: true});
} else if (focusId) document.getElementById(focusId)?.focus({preventScroll: true});
            """
            )

            _dataOn (
                "click",
                $"const link = evt.target.closest('a[href]'); if (link?.closest('[data-ledger-applied-filters]') && evt.button === 0 && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey) document.getElementById('{selectionId}')?.dispatchEvent(new CustomEvent('fve-selection-clear'))"
            )

            div {
                _id (elementId query ("template-" + kind + "-toolbar"))
                _attr ("data-ledger-collection-toolbar", "true")
                _class "shrink-0 bg-[var(--fve-background)] pb-4"
                if query.state = "delete-confirm" || query.state = "delete-blocked" then
                    deletionConfirmation query kind page false
                    noscript { deletionConfirmation query kind page true }
                form {
                    _id (elementId query ("template-" + kind + "-filters-form"))
                    _method "get"
                    _action (applicationHref query page)

                    _ariaLabel (
                        if kind = "accounts" then
                            "Account filters"
                        else
                            "Transaction filters"
                    )

                    _class
                        "flex min-w-0 flex-wrap items-center gap-3 group-has-[[data-fve-table]_input[name]:checked]/collection:hidden"

                    _dataShow $"${countSignal} == 0"

                    contextFields
                        { query with
                            specState = "default"
                            resource = "" }

                    for name, value in collectionQueryPairs query do
                        if name <> "search" then
                            hidden name value

                    div {
                        _class "min-w-0 flex-[1_1_14rem]"

                        fveField {
                            fveLabel {
                                _for (
                                    elementId
                                        query
                                        ("template-"
                                         + (if kind = "accounts" then "account" else "transaction")
                                         + "-search")
                                )

                                _class "sr-only"
                                Html.text ("Search " + kind)
                            }

                            fveInputGroup {
                                _attr ("data-collection-search", "")
                                _class "has-[input:placeholder-shown]:[&_[data-search-clear]]:invisible has-[input:disabled]:[&_[data-search-clear]]:invisible has-[input:read-only]:[&_[data-search-clear]]:invisible"
                                fveInput {
                                    _id (elementId query ("template-" + (if kind = "accounts" then "account" else "transaction") + "-search"))
                                    _name "search"
                                    _type InputType.Search
                                    _class "[&::-webkit-search-cancel-button]:appearance-none"
                                    _value query.search
                                    _placeholder ("Search " + kind)
                                }
                                fveInputGroupAddon {
                                    _ariaHidden true
                                    fveIcon { _name IconName.MagnifyingGlass; _size Size.Small }
                                }
                                fveInputGroupAddon {
                                    _position InputGroupPosition.InlineEnd
                                    button {
                                        _type "button"
                                        _attr ("data-search-clear", "")
                                        _ariaLabel ("Clear Search " + kind)
                                        _class "flex size-6 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                                        _dataOn ("click", "const field = el.closest('[data-collection-search]').querySelector('input'); if (!field.disabled && !field.readOnly) { field.value = ''; field.focus(); field.dispatchEvent(new Event('input', {bubbles: true})); field.dispatchEvent(new Event('change', {bubbles: true})); }")
                                        fveIcon { _name IconName.XMark; _variant IconVariant.Micro }
                                    }
                                }
                            }
                        }
                    }

                    if available.IsEmpty then
                        fveButton {
                            _disabled true
                            "Add filter"
                        }
                    else
                        fveDropdownMenu {
                            _id (elementId query ("template-" + kind + "-add-filter"))
                            _ariaLabel "Add filter"

                            fveDropdownMenuTrigger {

                                _class
                                    "[&_svg]:size-[var(--fve-control-icon-size,1rem)] [&_svg]:shrink-0 inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center gap-2 rounded-[var(--fve-radius-control)] ring-1 ring-inset ring-[var(--fve-border)] bg-transparent px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                                fun (triggerAttributes: HtmlAttribute list) ->
                                    button {
                                        for attribute in triggerAttributes do
                                            attribute

                                        span {
                                            _class "inline-flex items-center gap-2"
                                            Layout.icon IconName.Plus
                                            "Add filter"
                                        }
                                    }

                            }

                            for filter in available do
                                fveDropdownMenuItem {
                                    _href (
                                        collectionHref
                                            { query with
                                                filters = query.filters @ [ filter.name ]
                                                state = "" }
                                            page
                                    )

                                    filter.label
                                }
                        }
                }

                div {
                    _id (elementId query ("template-" + kind + "-selection-toolbar"))
                    _attr ("data-ledger-selection-toolbar", "true")
                    _class "flex min-w-0 flex-wrap items-center gap-3"
                    _dataShow $"${countSignal} > 0"
                    _style "display:none"

                    span {
                        _class "text-sm font-medium"
                        _dataText $"${countSignal} + ' selected'"
                    }

                    deleteSelected false

                    fveButton {
                        _variant ButtonVariant.Outline

                        _dataOn (
                            "click",
                            $"document.getElementById('{selectionId}').dispatchEvent(new CustomEvent('fve-selection-clear')); requestAnimationFrame(() => el.closest('[data-ledger-collection]').querySelector('input[type=search]').focus())"
                        )

                        "Clear selection"
                    }
                }

                noscript {
                    div {
                        _class
                            "hidden flex-wrap items-center gap-3 group-has-[[data-fve-table]_input[name]:checked]/collection:flex"

                        span {
                            _class "text-sm font-medium"
                            "Selected records"
                        }

                        deleteSelected true
                        Layout.secondaryLink (collectionHref { query with state = "" } page) "Clear selection"
                    }
                }

                if not active.IsEmpty then
                    div {
                        _attr ("data-ledger-applied-filters", "true")
                        _class "mt-3 flex min-w-0 flex-wrap items-center gap-2"

                        for filter in active do
                            let removeId = elementId query ("template-" + kind + "-remove-" + filter.name)

                            let removed =
                                filterQuery
                                    filter.name
                                    "all"
                                    { query with
                                        filters = query.filters |> List.filter ((<>) filter.name)
                                        state = "" }

                            form {
                                _id removeId
                                _method "get"
                                _action (applicationHref query page)
                                _class "hidden"

                                contextFields
                                    { removed with
                                        specState = "default"
                                        resource = "" }

                                for name, value in collectionQueryPairs removed do
                                    hidden name value
                            }

                            let selected = filterValues filter.value

                            let labels =
                                filter.options
                                |> List.choose (fun (value, label) ->
                                    if List.contains value selected then Some label else None)

                            let summary =
                                if labels.IsEmpty then "Any"
                                elif labels.Length > 2 then $"{labels.Length} selected"
                                else String.concat ", " labels

                            let menuId = elementId query ("template-" + kind + "-filter-" + filter.name)
                            let filterFormId = menuId + "-form"

                            let apply values keepOpen =
                                let focus = if keepOpen then "el.id" else "'" + menuId + "-trigger'"

                                let editing =
                                    if keepOpen then
                                        "root.dataset.ledgerEditingFilter = '" + filter.name + "';"
                                    else
                                        "delete root.dataset.ledgerEditingFilter;"

                                $"""
const root = el.closest('[data-ledger-collection]');
const form = document.getElementById('{filterFormId}');
const field = form.elements.namedItem('{filter.name}');
{values};
root.dataset.ledgerFilterFocus = {focus};
{editing}
document.getElementById('{selectionId}')?.dispatchEvent(new CustomEvent('fve-selection-clear'));
form.requestSubmit();
                                """

                            div {
                                _style "display:none"
                                _dataShow "true"
                                _attr ("data-ledger-filter-group", filter.name)

                                _class (
                                    "min-w-0 max-w-full [&_[role=group]>div]:min-w-24 [&_[data-ledger-filter]]:min-w-0 [&_[data-ledger-filter]]:w-full [&_[data-ledger-filter]]:max-w-full [&_[role=menu]>:first-child]:text-xs [&_[role=menu]>:first-child]:text-[var(--fve-muted-text)]"
                                    + (if selected.IsEmpty then
                                           " [&_[data-ledger-filter]]:text-[var(--fve-muted-text)]"
                                       else
                                           " [&_[data-ledger-filter]]:bg-[var(--fve-brand-subtle)] [&_[data-ledger-filter]]:text-[var(--fve-brand-text)] [&_[data-ledger-filter]]:ring-[var(--fve-brand-ring)]")
                                )

                                form {
                                    _id filterFormId
                                    _method "get"
                                    _action (applicationHref query page)
                                    _class "hidden"

                                    contextFields
                                        { query with
                                            state = ""
                                            specState = "default"
                                            resource = "" }

                                    for name, value in collectionQueryPairs query do
                                        hidden name value
                                }

                                let menu =
                                    fveDropdownMenu {
                                        _id menuId
                                        _ariaLabel (filter.label + " filter values")
                                        _alignment DropdownMenuAlignment.Start

                                        fveDropdownMenuTrigger {

                                            _class
                                                "[&_svg]:size-[var(--fve-control-icon-size,1rem)] [&_svg]:shrink-0 inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center gap-2 rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-transparent px-3 py-[max(0px,calc(var(--fve-control-padding-block)-1px))] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                                            _attr ("data-ledger-filter", filter.name)
                                            _data ("fve-group-control", "outline")

                                            fun (triggerAttributes: HtmlAttribute list) ->
                                                button {
                                                    for attribute in triggerAttributes do
                                                        attribute

                                                    span {
                                                        _class
                                                            "inline-flex min-w-0 max-w-[min(18rem,60vw)] items-center gap-2"

                                                        span {
                                                            _class "truncate"
                                                            summary
                                                        }

                                                        Layout.icon IconName.ChevronDown
                                                    }
                                                }

                                        }

                                        fveDropdownMenuItem {
                                            _dataOn ("click", apply "field.value = 'all'" false)
                                            _disabled selected.IsEmpty
                                            "Clear selection"
                                        }

                                        fveDropdownMenuSeparator { () }

                                        for value, label in filter.options do
                                            let encoded = System.Text.Json.JsonSerializer.Serialize value

                                            let toggle =
                                                $"const values = field.value.split(',').filter(value => value && value !== 'all'); field.value = (values.includes({encoded}) ? values.filter(value => value !== {encoded}) : values.concat({encoded})).join(',') || 'all'"

                                            fveDropdownMenuItem {
                                                _dataOn ("click", (apply toggle true))
                                                _role "menuitemcheckbox"
                                                label
                                                _ariaChecked (List.contains value selected)
                                            }
                                    }

                                fveButtonGroup {
                                    _ariaLabel (filter.label + " filter")
                                    filter.label
                                    menu

                                    fveButton {
                                        _ariaLabel ("Remove " + filter.label + " filter")
                                        _type "submit"
                                        _form removeId

                                        span {
                                            _ariaHidden true
                                            _class "inline-flex shrink-0"
                                            Layout.icon IconName.XMark
                                        }
                                    }
                                }
                            }

                            div {
                                _dataShow "false"

                                form {
                                    _method "get"
                                    _action (applicationHref query page)
                                    _class "flex flex-wrap items-center gap-3"

                                    contextFields
                                        { query with
                                            state = ""
                                            specState = "default"
                                            resource = "" }

                                    for name, value in collectionQueryPairs query do
                                        if name <> filter.name then
                                            hidden name value

                                    fieldset {
                                        legend { filter.label }

                                        for value, optionLabel in filter.options do
                                            label {
                                                _class "inline-flex items-center gap-2 text-sm"

                                                input {
                                                    _type "checkbox"
                                                    _name filter.name
                                                    _value value
                                                    _checked (List.contains value selected)
                                                    _class "size-4 accent-[var(--fve-brand-solid)]"
                                                }

                                                text optionLabel
                                            }
                                    }

                                    button {
                                        _type "submit"

                                        _class
                                            "inline-flex min-h-8 items-center rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-sm font-medium ring-1 ring-inset ring-[var(--fve-border)]"

                                        Html.text ("Apply " + filter.label + " filter")
                                    }

                                    Layout.link (collectionHref removed page) ("Remove " + filter.label + " filter")
                                }
                            }

                        Layout.link (collectionHref clearFilters page) "Clear all"
                    }
            }

            form {
                _id formId
                _method "post"
                _action (collectionHref query page)
                for attribute in submissionAttributes do attribute
                _ariaLabel ("Selected " + kind + " actions")
                _class "flex min-h-0 flex-1 flex-col"
                contextFields query

                for name, value in collectionQueryPairs query do
                    hidden name value

                rows
            }
        }
