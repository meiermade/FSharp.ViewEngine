module TailwindTests

open FSharp.ViewEngine
open Expecto
open type Html
open type TailwindElements

let private expectedElements =
    [
        "elAutocomplete", "el-autocomplete"
        "elCommandGroup", "el-command-group"
        "elCommandList", "el-command-list"
        "elCommandPalette", "el-command-palette"
        "elCommandPreview", "el-command-preview"
        "elCopyable", "el-copyable"
        "elDefaults", "el-defaults"
        "elDialog", "el-dialog"
        "elDialogBackdrop", "el-dialog-backdrop"
        "elDialogPanel", "el-dialog-panel"
        "elDisclosure", "el-disclosure"
        "elDropdown", "el-dropdown"
        "elMenu", "el-menu"
        "elNoResults", "el-no-results"
        "elOption", "el-option"
        "elOptions", "el-options"
        "elPopover", "el-popover"
        "elPopoverGroup", "el-popover-group"
        "elSelect", "el-select"
        "elSelectedContent", "el-selectedcontent"
        "elTabGroup", "el-tab-group"
        "elTabList", "el-tab-list"
        "elTabPanels", "el-tab-panels"
    ]

let private expectedElementHelpers = expectedElements |> List.map fst |> Set.ofList

[<Tests>]
let tests =
    testList "Tailwind Plus Elements Tests" [
        test "TailwindElements exposes the complete 1.0.22 element inventory" {
            let actual =
                typeof<TailwindElements>.GetProperties()
                |> Array.map _.Name
                |> Array.filter (fun name -> name.StartsWith("el"))
                |> Set.ofArray

            Expect.equal actual expectedElementHelpers "all 23 published custom elements"
        }

        test "Tailwind Plus custom elements render correctly" {
            let actual =
                div {
                    elAutocomplete {
                        elOptions {
                            elOption { "Autocomplete option" }
                        }
                    }
                    elCommandPalette {
                        elCommandList {
                            elDefaults { "Defaults" }
                            elCommandGroup { "Group" }
                        }
                        elNoResults { "No results" }
                        elCommandPreview { "Preview" }
                    }
                    elCopyable { "Copy me" }
                    elDialog {
                        dialog {
                            elDialogBackdrop { }
                            elDialogPanel { "Panel" }
                        }
                    }
                    elDisclosure { "Disclosure" }
                    elDropdown { elMenu { "Menu item" } }
                    elPopoverGroup { elPopover { "Popover" } }
                    elSelect { elSelectedContent { "Selected" } }
                    elTabGroup {
                        elTabList { "Tabs" }
                        elTabPanels { "Panels" }
                    }
                }
                |> Render.toString

            for helper, tag in expectedElements do
                Expect.stringContains actual $"<{tag}" helper
                Expect.stringContains actual $"</{tag}>" helper
        }

        test "Tailwind Plus custom attributes serialize correctly" {
            let actual =
                elPopover {
                    _popover
                    _anchor "bottom start"
                    _anchorStrategy "fixed"
                }
                |> Render.toString

            Expect.equal
                actual
                "<el-popover popover anchor=\"bottom start\" anchor-strategy=\"fixed\"></el-popover>"
                "presence and valued attributes"
        }

        test "Tailwind compatibility type is removed" {
            let legacyType = typeof<TailwindElements>.Assembly.GetType("FSharp.ViewEngine.Tailwind")
            Expect.isNull legacyType "TailwindElements is the only public type"
        }
    ]
