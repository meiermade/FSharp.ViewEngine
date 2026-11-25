namespace Docs.ComponentPages.Button

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
open Docs.Common.SamplesExamples
open Docs.Common.SharedExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let buttonRegistration =
        registration "components-button" "/components/button" "Button" "Button"

    let pendingSyncButton =
        fveButton {
            _color Color.Primary
            _variant ButtonVariant.Solid
            _dataIndicator ("_sync_pending", ["root"])
            _dataOn ("click", "@get('/components/button/settled', {filterSignals: {exclude: /.*/}, retry: 'never'})")
            "Sync"
        }

    let disabledDeleteButton =
        fveButton {
            _color Color.Error
            _variant ButtonVariant.Solid
            _disabled true
            "Delete"
        }

    let plusIcon = fveIcon { _name IconName.Plus; _variant IconVariant.Micro }


    let removeIcon = fveIcon { _name IconName.Trash; _variant IconVariant.Micro }

    let createIconOnlyButton =
        fveButton {
            _color Color.Primary
            _variant ButtonVariant.Solid
            _ariaLabel "Create"
            plusIcon
        }

    let refreshingIconOnlyButton =
        fveButton {
            _variant ButtonVariant.Ghost
            _ariaLabel "Refresh"
            _dataIndicator ("_refresh_pending", ["root"])
            _dataOn ("click", "@get('/components/button/settled', {filterSignals: {exclude: /.*/}, retry: 'never'})")
            refreshIcon
        }

    let disabledRemoveIconOnlyButton =
        fveButton {
            _color Color.Error
            _variant ButtonVariant.Solid
            _ariaLabel "Remove"
            _disabled true
            removeIcon
        }

    let uniformPaddingButtons =
        form {
            _method "get"
            _action "/components/button"
            _class "flex flex-wrap items-center justify-center gap-4"
            for size, label in [ Size.ExtraSmall, "Extra small"; Size.Small, "Small"; Size.Medium, "Medium"; Size.Large, "Large"; Size.ExtraLarge, "Extra large" ] do
                for kind in [ "button"; "submit"; "reset" ] do
                    fveButton {
                        _type kind
                        _size size
                        _padding Padding.Uniform
                        _ariaLabel (label + " " + kind)
                        refreshIcon
                    }
        }

    let backIcon = fveIcon { _name IconName.ChevronLeft; _variant IconVariant.Micro }

    let basicButton =
        fveButton {
            _dataIndicator ("_basic_sync_pending", ["root"])
            _dataOn ("click", "@get('/components/button/settled', {filterSignals: {exclude: /.*/}, retry: 'never'})")
            "Sync"
        }

    let linkIconVariants =
        div {
            _class "flex flex-wrap items-center justify-center gap-6"
            for label, variant in [ "Text", LinkVariant.Text; "Solid", LinkVariant.Solid; "Soft", LinkVariant.Soft; "Outline", LinkVariant.Outline; "Ghost", LinkVariant.Ghost ] do
                div {
                    _class "grid justify-items-center gap-3"
                    fveLink {
                        _href "/examples/application/settings"
                        _variant variant; _color Color.Neutral; _size Size.Medium
                        _ariaLabel (label + " settings")
                        fveIcon { _name IconName.Cog6Tooth; _variant IconVariant.Mini }
                    }
                    span { _class "text-sm"; label }
                }
        }

    let linkIconSizes =
        div {
            _class "flex flex-wrap items-center justify-center gap-6"
            for size, label in [ Size.ExtraSmall, "Extra small"; Size.Small, "Small"; Size.Medium, "Medium"; Size.Large, "Large"; Size.ExtraLarge, "Extra large" ] do
                div {
                    _class "grid justify-items-center gap-3"
                    fveLink {
                        _href "/examples/application/settings"; _variant LinkVariant.Outline; _size size
                        _ariaLabel (label + " settings")
                        fveIcon { _name IconName.Cog6Tooth; _variant IconVariant.Micro }
                    }
                    span { _class "text-sm"; label }
                }
        }

    let buttonVisibilityExample =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"
            _attr ("data-signals__root", "{actionVisible: false}")
            fveButton { _dataOn("click", "$actionVisible = !$actionVisible"); "Toggle action visibility" }
            fveButton {
                _dataShow "$actionVisible"
                _ariaLabel "Refresh visible action"
                _dataOn("click", "$actionVisible = false")
                fveIcon { _name IconName.ArrowPath; _variant IconVariant.Mini }
            }
        }

    let buttonSizeExamples =
        div {
            _class "flex min-w-0 flex-wrap items-end justify-center gap-6"
            for size, label in
                [ Size.ExtraSmall, "Extra small"
                  Size.Small, "Small"
                  Size.Medium, "Medium"
                  Size.Large, "Large"
                  Size.ExtraLarge, "Extra large" ] do
                div {
                    _class "flex flex-col items-center gap-3"
                    fveButton {
                        _size size; _color Color.Primary; _variant ButtonVariant.Solid
                        label
                    }
                    fveButton {
                        _size size; _variant ButtonVariant.Outline
                        _ariaLabel (label + " refresh")
                        refreshIcon
                    }
                }
        }

    let buttonColorExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, palette in
                [ "Primary", Color.Primary
                  "Secondary", Color.Secondary
                  "Success", Color.Success
                  "Warning", Color.Warning
                  "Error", Color.Error
                  "Info", Color.Info
                  "Neutral", Color.Neutral ] do
                fveButton {
                    _color palette
                    _variant ButtonVariant.Solid
                    label
                }
        }

    let buttonVariantExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, treatment in
                [ "Solid", ButtonVariant.Solid
                  "Soft", ButtonVariant.Soft
                  "Outline", ButtonVariant.Outline
                  "Ghost", ButtonVariant.Ghost ] do
                fveButton {
                    _color Color.Primary
                    _variant treatment
                    label
                }
        }

    let actionColors =
        [ Color.Primary, "Primary"; Color.Secondary, "Secondary"; Color.Success, "Success"
          Color.Warning, "Warning"; Color.Error, "Error"; Color.Info, "Info"; Color.Neutral, "Neutral" ]

    let actionPaletteGrid (rows: (string * HtmlElement list) list) =
        div {
            _class "grid min-w-0 w-full gap-6"
            for label, controls in rows do
                div {
                    _data("action-variant", label.ToLowerInvariant())
                    _class "grid min-w-0 gap-3"
                    h3 { _class "text-sm font-semibold"; label }
                    div { _class "flex min-w-0 flex-wrap items-center gap-3"; yield! controls }
                }
        }

    let buttonPaletteExamples =
        [ for variant, treatment in [ "Solid", ButtonVariant.Solid; "Soft", ButtonVariant.Soft; "Outline", ButtonVariant.Outline; "Ghost", ButtonVariant.Ghost ] do
              variant,
              [ for color, label in actionColors do
                    fveButton {
                        _variant treatment; _color color
                        _ariaLabel (variant + " " + label)
                        _data("action-color", label.ToLowerInvariant())
                        label
                    } ] ]
        |> actionPaletteGrid

    let customButtonPalette: ColorPalette =
        { light =
            { solid = "#9a3412"
              solidHover = "#7c2d12"
              solidActive = "#431407"
              onSolid = "#ffffff"
              soft = "#fff7ed"
              softHover = "#ffedd5"
              softActive = "#fed7aa"
              text = "#9a3412"
              border = "#9a3412"
              focus = "#9a3412" }
          dark =
            { solid = "#fdba74"
              solidHover = "#fed7aa"
              solidActive = "#ffedd5"
              onSolid = "#431407"
              soft = "#431407"
              softHover = "#5c2209"
              softActive = "#7c2d12"
              text = "#ffedd5"
              border = "#fdba74"
              focus = "#fdba74" } }

    let customPaletteButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, treatment in
                [ "Solid", ButtonVariant.Solid
                  "Soft", ButtonVariant.Soft
                  "Outline", ButtonVariant.Outline
                  "Ghost", ButtonVariant.Ghost ] do
                fveButton {
                    _color Color.Custom
                    _palette customButtonPalette
                    _variant treatment
                    label
                }
        }

    let pendingErrorButton =
        fveButton {
            _color Color.Error
            _variant ButtonVariant.Solid
            _dataIndicator ("_delete_pending", ["root"])
            _dataOn ("click", "@get('/components/button/settled', {filterSignals: {exclude: /.*/}, retry: 'never'})")
            "Delete"
        }

    let iconOnlyButtonVariantExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, treatment in
                [ "Solid", ButtonVariant.Solid
                  "Soft", ButtonVariant.Soft
                  "Outline", ButtonVariant.Outline
                  "Ghost", ButtonVariant.Ghost ] do
                fveButton {
                    _color Color.Secondary
                    _variant treatment
                    _ariaLabel (label + " refresh")
                    refreshIcon
                }
        }

    let buttonContentExamples =
        let continueIcon = fveIcon { _name IconName.ChevronRight; _variant IconVariant.Micro }

        div {
            _class "flex flex-wrap items-center justify-center gap-3"
            fveButton { "Text" }

            fveButton {
                _ariaLabel "Refresh"
                refreshIcon
            }

            fveButton {
                plusIcon
                "New branch"
            }

            fveButton {
                "Continue"
                continueIcon
            }

            fveButton {
                strong { "Custom" }
                span { " content" }
            }
        }

    let primaryButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"

            for size in [ Size.Small; Size.Medium; Size.Large ] do
                fveButton {
                    _color Color.Primary
                    _variant ButtonVariant.Solid
                    _size size
                    "Create"
                }
        }

    let secondaryButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"

            for size in [ Size.Small; Size.Medium; Size.Large ] do
                fveButton {
                    _color Color.Secondary
                    _variant ButtonVariant.Soft
                    _size size
                    "View reports"
                }
        }

    let ghostButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"

            for size in [ Size.Small; Size.Medium; Size.Large ] do
                fveButton {
                    _variant ButtonVariant.Ghost
                    _size size
                    "Cancel"
                }
        }

    let nativeGhostReset =
        form {
            _class "flex flex-wrap items-end justify-center gap-3"
            div {
                _class "grid gap-2"
                label { _for "ghost-reset-name"; _class "text-sm font-medium"; "Example name" }
                fveInput { _id "ghost-reset-name"; _name "name"; _value "Example" }
            }
            fveButton {
                _type "reset"
                _variant ButtonVariant.Ghost
                _size Size.Medium
                "Reset"
            }
        }

    let destructiveButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"

            for size in [ Size.Small; Size.Medium; Size.Large ] do
                fveButton {
                    _color Color.Error
                    _variant ButtonVariant.Solid
                    _size size
                    "Delete"
                }
        }

    let examples () =
            [ sample "button-basic" "Default" [ "basicButton" ] (centered basicButton)
              |> note "Submit and reset Buttons include a styled native form control before enhancement, so browser validation and form submission work without JavaScript. Rocket enhances that same content with pending and interaction behavior."
              sample "button-sizes" "Sizes" [ "refreshIcon"; "buttonSizeExamples" ] (centered buttonSizeExamples)
              |> note "Extra small, Small, Medium, Large and Extra large are 24, 28, 32, 36 and 40px at the default root size. Horizontal and vertical padding follow the shared size tokens independently, with borders included in the insets. Content determines width; icon-only controls do not need to be square. Supply an accessible name for icon-only controls. Medium is the 32px application default."
              sample "button-colors" "Colors" [ "buttonColorExamples" ] (centered buttonColorExamples)
              sample "button-variants" "Variants" [ "buttonVariantExamples" ] (centered buttonVariantExamples)
              |> note "Ghost buttons stay flat, without a decorative shadow at rest, on hover or while pressed. Subtle state backgrounds and the keyboard focus ring remain visible, including native submit/reset controls."
              sample "button-padding" "Uniform padding" [ "refreshIcon"; "uniformPaddingButtons" ] (centered uniformPaddingButtons)
              |> note "Padding.Uniform uses the size’s vertical inset on every side. A square child produces a square control, including native submit/reset controls. Omit padding or use Padding.Default for ordinary text padding. Minimum dimensions and border compensation still apply."
              sample "button-color-variants" "All color variants" [ "actionColors"; "actionPaletteGrid"; "buttonPaletteExamples" ] (fieldSurface buttonPaletteExamples)
              |> note "Compare each variant’s idle, hover and pressed state. Built-in Soft actions use a visible resting tint in light mode. Custom palettes and soft-color token overrides retain their authored values. Solid and tinted fills retain their color identity; text/icons stay opaque. Focus, selected and unavailable states remain independent."
              sample "button-pending" "Pending" [ "pendingSyncButton" ] (centered pendingSyncButton)
              |> note "Click Sync to see its loading state."
              sample
                  "button-content"
                  "Children"
                  [ "plusIcon"; "refreshIcon"; "buttonContentExamples" ]
                  (centered buttonContentExamples)
              |> note
                  "SVG icons follow the control size: 12px for Extra small, 16px for Small and Medium, and 20px for Large and Extra large at the default root size."
              sample
                  "button-icon-variants"
                  "Icon-only variants"
                  [ "refreshIcon"; "iconOnlyButtonVariantExamples" ]
                  (centered iconOnlyButtonVariantExamples)
              sample
                  "button-custom"
                  "Custom palette"
                  [ "customButtonPalette"; "customPaletteButtons" ]
                  (centered customPaletteButtons)
              sample "button" "Primary buttons" [ "primaryButtons" ] (centered primaryButtons)
              sample "button-secondary" "Secondary buttons" [ "secondaryButtons" ] (centered secondaryButtons)
              sample "button-ghost" "Ghost buttons" [ "ghostButtons" ] (centered ghostButtons)
              sample "button-native-reset" "Native Ghost reset" [ "nativeGhostReset" ] (centered nativeGhostReset)
              |> note "Change the name, then Reset to restore its initial value. The flat native reset button retains its control sizing and works before enhancement or without JavaScript."
              sample "button-destructive" "Destructive buttons" [ "destructiveButtons" ] (centered destructiveButtons)
              sample "button-error-pending" "Destructive pending" [ "pendingErrorButton" ] (centered pendingErrorButton)
              |> note "Click Delete to see its loading state."
              sample "button-disabled" "Disabled buttons" [ "disabledDeleteButton" ] (centered disabledDeleteButton)
              sample
                  "button-icon-primary"
                  "Primary icon-only"
                  [ "plusIcon"; "createIconOnlyButton" ]
                  (centered createIconOnlyButton)
              sample
                  "button-icon-pending"
                  "Pending icon-only"
                  [ "refreshIcon"; "refreshingIconOnlyButton" ]
                  (centered refreshingIconOnlyButton)
              |> note "Click the button to see its loading state."
              sample
                  "button-icon-disabled"
                  "Disabled icon-only"
                  [ "removeIcon"; "disabledRemoveIconOnlyButton" ]
                  (centered disabledRemoveIconOnlyButton)
              sample "button-visibility" "Datastar visibility" [ "buttonVisibilityExample" ] (centered buttonVisibilityExample)
              |> note "The caller owns data-show. Toggle visibility without changing Button presentation or its native control."
              sample "button-row-actions" "Row actions"
                  [ "RowActionAccount"; "rowActionAccounts"; "moreActionsIcon"; "rowActionMenu"; "rowActionButtons" ]
                  (connectedGallerySurface (div { _class "p-3 sm:p-6"; rowActionButtons }))
              |> note "Use a 28px-high Small ghost button with a horizontal-ellipsis icon child and the default control padding. Name it for its record; preserve hover contrast on hovered and selected backgrounds. Open, Edit and Delete navigate to the working Application example." ]


    let private galleryContent : Content =
        { description = "Trigger actions and submit forms with explicit text, icon, color, variant, size, and pending content."
          usage = []
          sections = [
              titledSection "accessibility" "Labels and focus"
                  [ prose "Text content names the button. For icon-only actions, provide _ariaLabel; see the icon-only examples above." ]
          ]
          apiCategory = Some "button" }

    let buttonPage = gallery buttonRegistration galleryContent (examples ())
