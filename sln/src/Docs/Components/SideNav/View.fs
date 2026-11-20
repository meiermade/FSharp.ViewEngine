namespace Docs.ComponentPages.SideNav

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
open Docs.Common.SharedExamples
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let sideNavRegistration =
        registration "components-side-nav" "/components/side-nav" "Side nav" "Side nav"

    let private ledgerMark =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path d="M4 3.75A1.75 1.75 0 0 1 5.75 2h8.5A1.75 1.75 0 0 1 16 3.75v12.5A1.75 1.75 0 0 1 14.25 18h-8.5A1.75 1.75 0 0 1 4 16.25V3.75Zm3 1.5A.75.75 0 0 0 7.75 6h4.5a.75.75 0 0 0 0-1.5h-4.5a.75.75 0 0 0-.75.75Zm0 4A.75.75 0 0 0 7.75 10h4.5a.75.75 0 0 0 0-1.5h-4.5a.75.75 0 0 0-.75.75Zm0 4a.75.75 0 0 0 .75.75h2.5a.75.75 0 0 0 0-1.5h-2.5a.75.75 0 0 0-.75.75Z"/></svg>"""

    let navigationGlyph = fveIcon { _name IconName.Stop; _variant IconVariant.Mini }

    let shellItem destination (label: string) =
        fveSideNavItem {
            _href (shellDestinationUrl destination)
            label
            fveSideNavLeading { navigationGlyph }
        }

    type DefaultSideNavDestination =
        | Overview
        | Settings

    let defaultSideNavUrl =
        function
        | Overview -> "/components/side-nav"
        | Settings -> "/guides/theming"

    let defaultSideNav =
        fveSideNav {
            _id "default-side-navigation"
            _ariaLabel "Primary navigation"

            fveSideNavContent {
                fveSideNavItem {
                    _href (defaultSideNavUrl Overview)
                    "Overview"
                }

                fveSideNavItem {
                    _href (defaultSideNavUrl Settings)
                    "Settings"
                }
            }
        }

    // Resolve your destination values to URLs.

    let breadcrumbsExample resolve home collection detail =
        fveBreadcrumbs {
            _id "item-breadcrumbs"
            _ariaLabel "Breadcrumb"

            fveBreadcrumbItem {
                _href (resolve home)
                "Home"
            }

            fveBreadcrumbItem {
                _href (resolve collection)
                "Items"
            }

            fveBreadcrumbItem {
                _href (resolve detail)
                "Project notes"
            }
        }

    let defaultSideNavPreview = div { defaultSideNav } |> themedSurface

    let sideNavStructureExample =
        fveSideNav {
            _id "structure-navigation"
            _ariaLabel "Navigation structure"
            _persistenceKey "structure-navigation-preferences"

            fveSideNavContent {
                fveSideNavItem {
                    _href "/components"
                    "Home"
                }

                fveSideNavSection {
                    _label "Workspace"

                    fveSideNavItem {
                        _href "/components/table#components-table-hierarchy"
                        "Items"
                    }

                    fveSideNavItem {
                        _href "/components/month-calendar"
                        "Calendar"
                    }
                }

                fveSideNavGroup {
                    _label "Documentation"

                    fveSideNavItem {
                        _href "/examples/specification"
                        "Specification"
                    }

                    fveSideNavGroup {
                        _label "Components"

                        fveSideNavItem {
                            _href "/components/button"
                            "Button"
                        }

                        fveSideNavItem {
                            _href "/components/table"
                            "Table"
                        }

                        _id "structure-components"
                        _expanded true
                    }

                    _id "structure-documentation"
                    _expanded true
                }
            }

            _value "/components/table#components-table-hierarchy"
        }

    let sideNavStructurePreview = div { sideNavStructureExample } |> themedSurface

    let sideNavRowIcon (name: IconName) = fveIcon { _name name; _size Size.Medium }

    let sideNavSlotsExample =
        let workspaceMenu =
            fveDropdownMenu {
                _id "slot-workspace"
                _ariaLabel "Choose workspace"
                fveDropdownMenuTrigger { fveButton { "Choose workspace" } }
                _alignment DropdownMenuAlignment.Start

                fveDropdownMenuItem {
                    _href "/components/side-nav?workspace=alex"
                    "Alex"
                }

                fveDropdownMenuItem {
                    _href "/components/side-nav?workspace=jordan"
                    "Jordan"
                }

                fveDropdownMenuSeparator { () }

                fveDropdownMenuItem {
                    _href "/examples/application/profile/organizations"
                    "Organizations"
                }
            }

        fveSideNav {
            _id "slot-navigation"
            _ariaLabel "Application navigation"

            fveSideNavHeader {
                _label "Workspace"

                a {
                    _href "/components"
                    _class "flex items-center gap-3 text-sm font-semibold"
                    "Workspace"
                }
            }

            fveSideNavContext {
                fveSideNavRow {
                    _label "Alex"
                    workspaceMenu

                    fveSideNavLeading {
                        sideNavRowIcon IconName.Squares2x2
                    }

                    fveSideNavTrailing {
                        fveBadge {
                            _ariaLabel "2 workspaces"
                            "2"
                        }
                    }
                }
            }

            fveSideNavContent {
                fveSideNavItem {
                    _href "/components"
                    "Home"
                }

                fveSideNavSection {
                    _label "Workspace"

                    fveSideNavItem {
                        _href "/components/table#components-table-hierarchy"
                        "Items"
                    }

                    fveSideNavItem {
                        _href "/components/month-calendar"
                        "Calendar"
                    }
                }
            }

            _value "/components/table#components-table-hierarchy"

            fveSideNavFooter {
                fveSideNavRow {
                    _href "/examples/application/settings"
                    "Settings"

                    fveSideNavLeading {
                        sideNavRowIcon IconName.Cog6Tooth
                    }
                }

                fveSeparator { () }

                fveSideNavRow {
                    _href "/examples/application/profile"
                    "Andrew Meier"

                    fveSideNavLeading {
                        fveAvatar {
                            _ariaLabel "Andrew Meier"
                            _ariaHidden true
                            "AM"
                        }
                    }

                    _description "andrew@meiermade.com"
                }
            }
        }

    let sideNavSlotsPreview =
        div {
            _class (
                ComponentsTheme.sky
                |> ComponentsTheme.withDensity Density.Compact
                |> ComponentsTheme.className
            )

            div {
                _class "h-96 overflow-hidden rounded-xl ring-1 ring-[var(--fve-border)]"
                sideNavSlotsExample
            }
        }

    let ledgerNavigation showMobileWorkspace current =
        let navigationCurrent =
            match current with
            | LedgerAccount _
            | LedgerCreateAccount -> LedgerAccounts
            | LedgerHome
            | LedgerAccounts
            | LedgerReports
            | LedgerSettings -> current
            | _ -> LedgerHome

        let workspace =
            div {
                p {
                    _class "text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"
                    "Workspace"
                }

                p {
                    _class "mt-1 truncate text-sm font-semibold"
                    "Meier Made"
                }
            }

        let compactBrand =
            a {
                _href (shellDestinationUrl LedgerHome)
                _ariaLabel "Ledger home"

                _class
                    "flex size-8 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] text-white outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"

                ledgerMark
            }

        let navigationHeader =
            fveSideNavHeader {
                _label "Ledger"

                a {
                    _href (shellDestinationUrl LedgerHome)

                    _class
                        "flex min-w-0 items-center gap-3 rounded-[var(--fve-radius-control)] outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"

                    span {
                        _ariaHidden true

                        _class
                            "flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] text-white"

                        ledgerMark
                    }

                    strong {
                        _class "truncate text-base font-semibold"
                        "Ledger"
                    }
                }

                fveSideNavHeaderCompact { compactBrand }
            }

        let accountAction =
            fveButton {
                _variant ButtonVariant.Ghost
                _size Size.Small
                _ariaLabel "Pin Accounts"

                _dataOn (
                    "click",
                    "el.setAttribute('aria-pressed', el.getAttribute('aria-pressed') != 'true' ? 'true' : 'false')"
                )

                _ariaPressed false
                fveIcon { _name IconName.MapPin; _variant IconVariant.Micro }
            }

        fveSideNav {
            _id "ledger-side-navigation"
            _ariaLabel "Ledger primary navigation"
            Html._width SideNavWidth.Standard

            match current with
            | LedgerTransaction _ -> ()
            | _ -> _value (shellDestinationUrl navigationCurrent)

            navigationHeader

            fveSideNavContent {
                fveSideNavSection {
                    _label "Manage"
                    shellItem LedgerHome "Dashboard"

                    fveSideNavItem {
                        _href (shellDestinationUrl LedgerAccounts)
                        fveSideNavLeading { navigationGlyph }

                        fveSideNavBadge {
                            fveBadge {
                                _color Color.Neutral
                                "6"
                            }
                        }

                        fveSideNavAction { accountAction }
                        "Accounts"
                    }
                }

                fveSideNavSection {
                    _label "Analyze"

                    fveSideNavGroup {
                        _label "Reporting"
                        _expanded true
                        fveSideNavLeading { navigationGlyph }
                        shellItem LedgerReports "Reports"
                    }
                }

                fveSideNavSection {
                    _label "Configure"
                    shellItem LedgerSettings "Settings"
                }
            }

            fveSideNavContext {
                div {
                    _class "px-4 py-3"
                    workspace
                }
            }

            if showMobileWorkspace then
                fveSideNavMobileContext { workspace }

            fveSideNavFooter {
                fveSideNavRow {
                    _href (shellDestinationUrl LedgerSettings)

                    fveSideNavLeading {
                        fveAvatar { _ariaHidden true; _size Size.ExtraSmall; "AM" }
                    }

                    "Andy Meier"
                }
            }

            fveSideNavCompactFooter {
                fveLink {
                    _href (shellDestinationUrl LedgerSettings)
                    _ariaLabel "Andy Meier profile"
                    _variant LinkVariant.Ghost
                    _size Size.Small
                    _padding Padding.Uniform
                    fveAvatar { _ariaHidden true; _size Size.ExtraSmall; "AM" }
                }
            }
        }

    let groupedLedgerNavigation = ledgerNavigation true LedgerAccounts

    let nestedSideNav =
        let action =
            fveButton {
                _variant ButtonVariant.Ghost
                _ariaLabel "Pin items"
                _ariaPressed false

                _dataOn (
                    "click",
                    "el.setAttribute('aria-pressed', el.getAttribute('aria-pressed') !== 'true' ? 'true' : 'false')"
                )
                fveIcon { _name IconName.MapPin; _variant IconVariant.Micro }
            }

        fveSideNav {
            _id "nested-item-navigation"
            _ariaLabel "Workspace navigation"
            fveSideNavHeader { _label "Workspace" }

            fveSideNavContent {
                fveSideNavItem {
                    _href "/components"
                    "Home"
                }

                fveSideNavItem {
                    _href "/components/table#components-table-hierarchy"
                    "Items"
                    fveSideNavBadge { fveBadge { "24" } }
                    fveSideNavAction { action }
                }

                fveSideNavGroup {
                    _label "Documentation"

                    fveSideNavItem {
                        _href "/components/button"
                        "Button"
                    }

                    fveSideNavGroup {
                        _label "Content"

                        fveSideNavItem {
                            _href "/components/table"
                            "Table"
                        }

                        fveSideNavItem {
                            _href "/components/card"
                            "Card"
                        }

                        _id "nested-content"
                    }

                    _id "nested-documentation"
                    _expanded true
                }
            }

            _value "/components/table#components-table-hierarchy"
        }

    let sideNavigationPreview =
        div {
            _class "h-[36rem] overflow-hidden rounded-xl ring-1 ring-[var(--fve-border)]"
            nestedSideNav
        }
        |> themedSurface

    let sideNavExample = nestedSideNav

    let examples () =
            [ sample
                  "side-nav-default"
                  "Default"
                  [ "DefaultSideNavDestination"; "defaultSideNavUrl"; "defaultSideNav" ]
                  defaultSideNavPreview
              sample
                  "side-nav-structure"
                  "Sections and collapsible groups"
                  [ "sideNavStructureExample" ]
                  sideNavStructurePreview
              sample
                  "side-nav-slots"
                  "Header, context, content and footer"
                  [ "sideNavRowIcon"; "sideNavSlotsExample" ]
                  sideNavSlotsPreview
              sample "side-nav" "Nested navigation with badges and actions" [ "nestedSideNav"; "sideNavExample" ] sideNavigationPreview ]


    let private galleryContent : Content =
        { description = "Compose optional header, context, scrolling content and footer slots. Use SideNavItem for links, SideNavSection for static headings, and SideNavGroup for collapsible groups. SideNavRow links have no default trailing indicator; menu rows retain a downward chevron. Add an avatar with withLeading, secondary text or email with withDescription, and optional trailing content with withTrailing. Sections use small, muted labels without indentation, and groups guide nested children with vertical lines."
          usage = []
          sections = [
              titledSection
                  "expansion-and-scroll"
                  "Expansion and scroll"
                  [ prose
                        "Groups stay independently open or closed during navigation, history and refresh. Their initial state opens the active branch; remembered choices take precedence, including an explicit collapse of that branch. Content scroll is also remembered for the current browser tab."
                    prose
                        "Preferences use sessionStorage and default to the stable SideNav ID. Use _persistenceKey to separate unrelated navigation trees that reuse an ID, or to share preferences between responsive presentations of one tree. Give groups stable _id values when their order changes; otherwise their structural position and label identify them."
                    prose
                        "Include SideNavPreferences.assetsWithNonce once in the document head before the application module to restore saved disclosures before the first paint. Pass the host's CSP nonce, or use SideNavPreferences.assets() when no nonce is required. The initializer also preserves native toggles made while the application module is loading."
                    code "fsharp" "head {\n    SideNavPreferences.assetsWithNonce (Some nonce)\n    // Your stylesheet and application module follow.\n}"
                    prose
                        "_expandedSignal keeps the host responsible for initializing and persisting that group's state. When storage is unavailable, toggles still work in the current document. Without JavaScript, native links and details/summary retain their server-rendered defaults, but browser preferences are not restored." ]
          ]
          apiCategory = Some "side-nav" }

    let sideNavPage = gallery sideNavRegistration galleryContent (examples ())
