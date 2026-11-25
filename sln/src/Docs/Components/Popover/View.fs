namespace Docs.ComponentPages.Popover

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
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let popoverRegistration =
        registration "components-popover" "/components/popover" "Popover" "Popover"

    let profilePopover =
        fvePopover {
            _id "profile-popover"
            fvePopoverTrigger { fveButton { "Profile details" } }
            fvePopoverContent {
                _tabindex 0
                _autofocus true
                _class "grid gap-3"
                h3 { _class "font-semibold"; "Personal workspace" }
                p { _class "text-sm text-[var(--fve-muted-text)]"; "Profile information and workspace access." }
                dl {
                    _class "grid grid-cols-1 gap-x-4 gap-y-2 break-words text-sm sm:grid-cols-2"
                    dt { _class "text-[var(--fve-muted-text)]"; "Access" }
                    dd { "Owner" }
                    dt { _class "text-[var(--fve-muted-text)]"; "Environment" }
                    dd { "Production" }
                }
            }
        }

    let previewSizePopover =
        div {
            _class "grid w-full justify-items-start gap-4"
            _attr ("data-signals__root__ifmissing", "{_popover_preview_width: 240, _popover_preview_height: 120}")
            fvePopover {
                _id "preview-size-popover"
                _focusOnOpen true
                fvePopoverTrigger { fveButton { "Preview size" } }
                fvePopoverContent {
                    form {
                        _ariaLabel "Preview dimensions"
                        _class "grid gap-3"
                        _dataOn ("submit", "evt.preventDefault()")
                        _dataOn ("reset", "$_popover_preview_width = 240; $_popover_preview_height = 120")
                        h3 { _class "font-semibold"; "Preview dimensions" }
                        p { _class "text-sm text-[var(--fve-muted-text)]"; "Changes resize the sample immediately." }
                        for key, label, value, minimum, maximum in
                            [ "width", "Width", "240", "120", "400"
                              "height", "Height", "120", "64", "240" ] do
                            fveField {
                                fveLabel { _for ("popover-preview-" + key); label }
                                fveInput {
                                    _id ("popover-preview-" + key); _name key; _type InputType.Number
                                    _value value; _min minimum; _max maximum; _required true
                                    _dataBind ("_popover_preview_" + key, ["root"])
                                }
                            }
                        fveButton { _type "reset"; "Reset size" }
                    }
                }
            }
            div {
                _ariaLabel "Size preview"
                _class "flex items-center justify-center rounded border border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] text-sm text-[var(--fve-muted-text)]"
                _style "width:240px;height:120px;min-width:120px;max-width:min(400px,100%);min-height:64px;max-height:240px"
                _dataStyle ("width", "$_popover_preview_width + 'px'")
                _dataStyle ("height", "$_popover_preview_height + 'px'")
                "Preview"
            }
        }

    let alignedPopover =
        fvePopover {
            _id "filter-popover"
            _alignment PopoverAlignment.End
            _side PopoverSide.Top
            fvePopoverTrigger { fveButton { "Filter details" } }
            fvePopoverContent {
                _ariaLabel "Filter details"
                _tabindex 0
                _autofocus true
                h3 { _class "font-semibold"; "Active items" }
                p { _class "mt-2 text-sm text-[var(--fve-muted-text)]"; "Showing items updated this month." }
            }
        }

    let nativeTriggerPopover =
        fvePopover {
            _id "native-workspace-popover"
            fvePopoverTrigger {
                _ariaLabel "Workspace information"
                fun (attributes: HtmlAttribute list) ->
                    button {
                        for attribute in attributes do attribute
                        _class "inline-flex size-9 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-sm font-semibold text-[var(--fve-brand-text)]"
                        span { _ariaHidden true; "AM" }
                    }
            }
            fvePopoverContent {
                _tabindex 0
                _autofocus true
                h3 { _class "font-semibold"; "Personal workspace" }
                p { _class "mt-2 text-sm text-[var(--fve-muted-text)]"; "You have owner access in this workspace." }
            }
        }

    let examples () =
            [ sample "popover" "Content panel" [ "profilePopover" ] (centered profilePopover)
              sample "popover-focus" "With form" [ "previewSizePopover" ] (detailsSurface previewSizePopover)
              |> note "Tab moves through ordinary fields and buttons; Escape returns to the trigger. The preview size is local UI state, not a saved profile."
              sample "popover-alignment" "Side and alignment" [ "alignedPopover" ] (centered alignedPopover)
              sample "popover-native-trigger" "Custom native trigger" [ "nativeTriggerPopover" ] (centered nativeTriggerPopover)
              |> note "Use this factory only when authoring a custom native control. Apply the supplied attributes; Popover retains its relationships, positioning and dismissal. The native trigger also works without JavaScript." ]


    let private galleryContent : Content =
        { description = "Present labelled interactive content adjacent to a trigger with focus and dismissal ownership."
          usage = [
              p {
                  "Use Popover for contextual content and forms with normal Tab navigation. It does not block the rest of the page. For a list of commands or compact choices, use "
                  a { _href "/components/dropdown-menu"; _class "text-[var(--fve-brand-text)] underline"; "Dropdown menu" }
                  "."
              }
          ]
          sections = []
          apiCategory = Some "popover" }

    let popoverPage = gallery popoverRegistration galleryContent (examples ())
