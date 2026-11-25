namespace Docs.Common

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar

module FramesExamples =
    let fixtureWindowControl (navigation: HtmlAttribute list) (path: string) (id: string) (expanded: bool) =
        let label =
            (if expanded then "Restore " else "Expand ")
            + (if id = "browser-review" then
                   "Browser fixture"
               else
                   "Phone fixture")

        let separator = if path.Contains('?') then "&" else "?"

        fveLink {
            _id ("fve-fixture-" + id + (if expanded then "-restorer" else "-launcher"))
            yield! navigation
            _variant LinkVariant.Outline
            _color Color.Neutral
            _size Size.Small
            _href (
                if expanded then
                    path + separator + "fveAppReturn=" + id
                else
                    path + separator + "expanded=1"
            )

            _ariaLabel label
            _title label

            if expanded then
                _data ("fve-app-mode-exit", "true")
            else
                _data ("review-expand", "true")

                _dataInit (
                    "if (new URL(location.href).searchParams.get('fveAppReturn') === "
                    + System.Text.Json.JsonSerializer.Serialize id
                    + ") { el.focus({preventScroll:true}); el.scrollIntoView({block:'center'}) }"
                )

            fveIcon {
                _name (if expanded then IconName.ArrowsPointingIn else IconName.ArrowsPointingOut)
                _variant IconVariant.Mini
            }
        }

    let fixtureAppearance (id: string) =
        let signal = "_" + id.Replace('-', '_') + "_mode"

        div {
            _dataSignals (signal, ["root"; "ifmissing"], "window.fveColorMode.current()")
            _dataInit $"${signal} = window.fveColorMode.current()"
            _dataOn ("fve-color-mode", ["window"], $"${signal} = window.fveColorMode.current()")

            fveDropdownMenu {
                _id id

                fveDropdownMenuTrigger {
                    _ariaLabel "Choose fixture theme"

                    fveButton {
                        _variant ButtonVariant.Outline
                        _size Size.Small
                        "Theme"
                    }
                }

                for value, caption in [ "system", "System"; "light", "Light"; "dark", "Dark" ] do
                    let isChecked = value = "system"

                    fveDropdownMenuItem {
                        _role "menuitemradio"
                        _ariaChecked isChecked
                        _dataAttr ("aria-checked", $"${signal} == '{value}' ? 'true' : 'false'")
                        _dataOn ("click", $"window.fveColorMode.set('{value}')")
                        caption
                    }
            }
        }
