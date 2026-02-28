module TailwindTests

open FSharp.ViewEngine
open Expecto
open type Html
open type Tailwind

[<Tests>]
let tests =
  testList "Tailwind Tests" [
    test "Tailwind custom elements render correctly" {
        let actual =
            div {
                elAutocomplete { _id "ac"; "search" }
                elDropdown { elMenu { "item" } }
                elDialog {
                    elDialogBackdrop { }
                    elDialogPanel { "panel content" }
                }
                elCommandPalette {
                    elCommandList {
                        elCommandGroup { "group" }
                    }
                    elCommandPreview { "preview" }
                }
                elDefaults { }
                elNoResults { "No results" }
                elTabGroup {
                    elTabList { "tabs" }
                    elTabPanels { "panels" }
                }
            } |> Render.toString
        Expect.stringContains actual "<el-autocomplete id=\"ac\">search</el-autocomplete>" "el-autocomplete"
        Expect.stringContains actual "<el-dropdown><el-menu>item</el-menu></el-dropdown>" "el-dropdown/menu"
        Expect.stringContains actual "<el-dialog>" "el-dialog"
        Expect.stringContains actual "<el-dialog-backdrop></el-dialog-backdrop>" "el-dialog-backdrop"
        Expect.stringContains actual "<el-dialog-panel>panel content</el-dialog-panel>" "el-dialog-panel"
        Expect.stringContains actual "<el-command-palette>" "el-command-palette"
        Expect.stringContains actual "<el-command-list>" "el-command-list"
        Expect.stringContains actual "<el-command-group>group</el-command-group>" "el-command-group"
        Expect.stringContains actual "<el-command-preview>preview</el-command-preview>" "el-command-preview"
        Expect.stringContains actual "<el-defaults></el-defaults>" "el-defaults"
        Expect.stringContains actual "<el-no-results>No results</el-no-results>" "el-no-results"
        Expect.stringContains actual "<el-tab-group>" "el-tab-group"
        Expect.stringContains actual "<el-tab-list>tabs</el-tab-list>" "el-tab-list"
        Expect.stringContains actual "<el-tab-panels>panels</el-tab-panels>" "el-tab-panels"
    }

    test "Tailwind _popover and _anchor attributes" {
        let actual = div { _popover; _anchor "bottom" } |> Render.toString
        Expect.stringContains actual "popover" "_popover"
        Expect.stringContains actual "anchor=\"bottom\"" "_anchor"
    }
  ]
