namespace Example.Components.Views

open System
open System.Text
open System.Text.Json
open FSharp.ViewEngine
open Example.Components
open type Html
open type Datastar

/// Application-owned selection for the currently rendered record keys.
type internal RecordSelectionView =
    { attributes: HtmlAttribute list
      heading: HtmlElement
      status: HtmlElement
      rowAttributes: string -> HtmlAttribute list
      cell: string -> string -> HtmlElement
      groupCell: string -> string -> string list -> HtmlElement }

module internal RecordSelection =
    let token (value: string) =
        "v"
        + (Encoding.UTF8.GetBytes value
           |> Array.map (fun value -> value.ToString("x2"))
           |> String.concat "")

    let private json value = JsonSerializer.Serialize value

    let view id formName selectedKeys (records: (string * string) list) =
        let keys = records |> List.map fst

        if
            records
            |> List.exists (fun (key, label) -> String.IsNullOrWhiteSpace key || String.IsNullOrWhiteSpace label)
        then
            invalidArg (nameof records) "Each rendered selection requires a key and label."

        if List.distinct keys |> List.length <> keys.Length then
            invalidArg (nameof records) "Rendered selection keys must be unique."

        let initial = keys |> List.filter (fun key -> List.contains key selectedKeys)
        let signal = "_table_" + token id + "_selected"
        let selected = "$" + signal
        let all = $"({keys.Length} > 0 && {selected}.length === {keys.Length})"
        let empty = if keys.IsEmpty then "true" else "false"

        let checkbox id accessibleLabel name value checkedValue disabled effect change =
            label {
                _class
                    "fve-table-selection-target inline-flex size-7 cursor-pointer items-center justify-center align-middle"

                fveCheckbox {
                    _id id
                    _ariaLabel accessibleLabel
                    _value value
                    _checked checkedValue
                    _disabled disabled

                    if name <> "" then
                        _name name

                    _class "fve-table-checkbox"
                    _style "inline-size:1rem;block-size:1rem;margin:0"
                    _dataEffect effect
                    _dataOn ("change", change)
                }
            }

        { attributes =
            [ _id id
              _dataSignals (signal, ["ifmissing"], json initial)
              _dataEffect
                  $"if ({selected}.some(key => !{json keys}.includes(key))) {{ {selected} = {selected}.filter(key => {json keys}.includes(key)); }} el.dispatchEvent(new CustomEvent('fve-table-selection-change', {{bubbles:true, detail:{{keys:Array.from({selected})}}}}))"
              _dataOn ("fve-selection-clear", selected + " = []") ]
          heading =
            fveTableHead {
                _class "fve-table-selection w-px"

                checkbox
                    (id + "-all")
                    "Select all rows on this page"
                    ""
                    "all"
                    (keys.Length > 0 && initial.Length = keys.Length)
                    true
                    $"el.disabled = {empty}; el.checked = {all}; el.indeterminate = {selected}.length > 0 && !{all}"
                    $"{selected} = el.checked ? {json keys} : []"

                span {
                    _ariaHidden true
                    _class "fve-table-mobile-label hidden"
                    "Select all"
                }
            }
          status =
            output {
                _role "status"
                _ariaLive "polite"
                _class "sr-only"
                _dataText (selected + ".length + ' selected'")
                $"{initial.Length} selected"
            }
          rowAttributes =
            fun key ->
                [ _id (id + "-row-" + token key)
                  _dataAttr ("data-selected", selected + ".includes(" + json key + ") ? 'true' : 'false'") ]
          cell =
            fun key label ->
                fveTableCell {
                    _class "fve-table-selection w-px"

                    checkbox
                        (id + "-select-" + token key)
                        ("Select " + label)
                        formName
                        key
                        (List.contains key initial)
                        false
                        $"el.checked = {selected}.includes({json key})"
                        $"{selected} = el.checked ? [...{selected}.filter(key => key !== {json key}), {json key}] : {selected}.filter(key => key !== {json key})"
                }
          groupCell =
            fun key label members ->
                let subset = json members
                let disabled = if members.IsEmpty then "true" else "false"
                let any = $"{selected}.some(key => {subset}.includes(key))"
                let all = $"({members.Length} > 0 && {subset}.every(key => {selected}.includes(key)))"
                fveTableCell {
                    _class "fve-table-selection w-px"
                    checkbox
                        (id + "-select-" + token key)
                        ("Select " + label)
                        ""
                        key
                        false
                        true
                        $"el.disabled = {disabled}; el.checked = {all}; el.indeterminate = {any} && !{all}"
                        $"{selected} = el.checked ? [...new Set([...{selected}, ...{subset}])] : {selected}.filter(key => !{subset}.includes(key))"
                } }

/// The application owns its no-JavaScript view; the installed Table stays a thin presentation host.
module internal RecordTable =
    let render title attributes (headings: HtmlElement list) (rows: HtmlElement list) (extras: HtmlElement list) =
        let headings = fveTableRow { yield! headings }

        fragment {
            fveTable {
                _ariaLabel title
                yield! (attributes: HtmlAttribute list)
                yield! extras
                fveTableHeader { headings }
                fveTableBody { yield! rows }
            }

            noscript {
                div {
                    _class "w-full min-w-0 overflow-x-auto"
                    yield! extras

                    table {
                        _attr ("data-fve-table", "")
                        _ariaLabel title

                        _class
                            "w-full border-collapse text-left text-sm [&_th]:p-3 [&_td]:p-3 [&_tr]:border-b [&_tr]:border-[var(--fve-border)]"

                        caption {
                            _class "sr-only"
                            title
                        }

                        thead { headings }
                        tbody { yield! rows }
                    }
                }
            }
        }
