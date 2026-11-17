namespace Docs.ComponentPages.Tabs

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
open Docs.ComponentPages.Button.View
open Docs.Common.SharedExamples
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let tabsRegistration =
        registration "components-tabs" "/components/tabs" "Tabs" "Tabs"

    let codePreviewTabs =
        fveTabs {
            _id "components-example-format"
            _ariaLabel "Example format"

            fveTabsItem {
                _value "code"
                fveTabsTrigger { "Code" }

                pre {
                    _role "region"
                    _ariaLabel "Button F# code"
                    _tabindex 0

                    _dataOn (
                        "keydown",
                        """
                        if (evt.target === el && !evt.altKey && !evt.ctrlKey && !evt.metaKey && !evt.shiftKey
                            && (evt.key === 'ArrowLeft' || evt.key === 'ArrowRight')) {
                            evt.preventDefault();
                            el.scrollBy({left: evt.key === 'ArrowLeft' ? -80 : 80});
                        }
                    """
                    )

                    _class
                        "overflow-x-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] p-4 text-sm outline-none focus-visible:outline-2 focus-visible:outline-solid focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                    "fveButton { \"Create\" }"
                }
            }

            fveTabsItem {
                _value "preview"
                fveTabsTrigger { "Preview" }

                div {
                    _class "rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] p-4"
                    fveButton { "Create" }
                }
            }
        }

    let serverTabsExampleFor selection =
        let selected = if selection = "activity" then "activity" else "summary"
        form {
            _method "get"; _action "/components/tabs"
            _data("fve-local-navigation", "true")
            fveTabs {
                _id "components-server-tabs"; _ariaLabel "Server-selected view"
                _selection TabsSelection.Server
                _value selected
                for value, label, description in [ "summary", "Summary", "The server selected the summary."; "activity", "Activity", "The server selected recent activity." ] do
                    fveTabsItem {
                        _value value
                        fveTabsTrigger { _name "tab"; _value value; label }
                        p { description }
                    }
            }
        }

    let serverTabsExample = serverTabsExampleFor "summary"

    let verticalTabs =
        fveTabs {
            _id "components-vertical-tabs"
            _ariaLabel "Workspace settings"

            fveTabsItem {
                _value "profile"

                fveTabsTrigger {
                    span {
                        _ariaHidden true
                        _class "mr-2 inline-flex size-4 items-center justify-center align-text-bottom"
                        plusIcon
                    }

                    "Profile"
                }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Update your name and profile image."
                }
            }

            fveTabsItem {
                _value "notifications"

                fveTabsTrigger {
                    span {
                        _ariaHidden true
                        _class "mr-2 inline-flex size-4 items-center justify-center align-text-bottom"
                        refreshIcon
                    }

                    "Notifications"
                }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Choose which updates send alerts."
                }
            }

            fveTabsItem {
                _value "administration"

                fveTabsTrigger {
                    span {
                        _ariaHidden true
                        _class "mr-2 inline-flex size-4 items-center justify-center align-text-bottom"
                        removeIcon
                    }

                    _disabled true
                    "Administration"
                }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "These settings require administrator access."
                }
            }

            _orientation Orientation.Vertical
        }

    let reviewTabsRegion refreshed =
        let refreshActivityButton =
            fveButton {
                _id "components-tabs-refresh"
                _dataOn ("click", "evt.currentTarget.focus(); @get('/components/tabs/review')")
                "Refresh activity"
            }

        let activity =
            if refreshed then
                div {
                    p {
                        _role "status"
                        _class "font-medium"
                        "Review refreshed"
                    }

                    p {
                        _class "mt-1 text-sm text-[var(--fve-muted-text)]"
                        "Updated just now."
                    }

                    refreshActivityButton
                }
            else
                div {
                    p {
                        _class "text-sm text-[var(--fve-muted-text)]"
                        "Three items need review."
                    }

                    refreshActivityButton
                }

        fveTabs {
            _id "components-review-tabs"
            _ariaLabel "Item sections"

            fveTabsItem {
                _value "overview"
                fveTabsTrigger { "Overview" }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "24 items updated this month"
                }
            }

            fveTabsItem {
                _value "activity"
                fveTabsTrigger { "Activity" }
                activity
            }

            fveTabsItem {
                _value "settings"
                fveTabsTrigger { "Settings" }

                a {
                    _href "/guides/theming"
                    _class "font-medium text-[var(--fve-brand-text)]"
                    "Review appearance settings"
                }
            }

            _variant TabsVariant.Underlined
        }

    let examples () =
            [ sample "tabs" "Default" [ "codePreviewTabs" ] (fieldSurface codePreviewTabs)
              sample
                  "tabs-server"
                  "Server-owned selection"
                  [ "serverTabsExampleFor"; "serverTabsExample" ]
                  (connectedCentered serverTabsExample)
              |> note "_selection TabsSelection.Server submits the trigger's native form. Only the accepted panel is rendered, with one stable panel ID; the current panel stays visible until the server responds. Use ordinary GET forms or your application's Datastar form enhancement."
              sample
                  "tabs-underlined"
                  "Underlined tabs with refresh"
                  [ "reviewTabsRegion" ]
                  (fieldSurface (reviewTabsRegion false))
              sample
                  "tabs-vertical"
                  "Vertical, icons and disabled items"
                  [ "plusIcon"; "refreshIcon"; "removeIcon"; "verticalTabs" ]
                  (fieldSurface verticalTabs) ]


    let private galleryContent : Content =
        { description = "Switch between labelled local or server-selected panels with orientation-aware keyboard navigation."
          usage = []
          sections = []
          apiCategory = Some "tabs" }

    let tabsPageFor selected =
        examples ()
        |> List.map (fun example ->
            if example.id = "components-tabs-server" then
                { example with preview = connectedCentered (serverTabsExampleFor selected) }
            else example)
        |> gallery tabsRegistration galleryContent

    let tabsPage = tabsPageFor "summary"
