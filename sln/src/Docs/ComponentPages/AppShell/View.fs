namespace Docs.ComponentPages.AppShell

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
open Docs.ComponentPages.SideNav.View
open Docs.Common.SharedExamples
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let appShellRegistration =
        applicationRegistration "components-app-shell" "/components/app-shell" "App shell" "App shell"

    type ShellLayoutDestination =
        | Dashboard
        | Projects
        | Preferences

    let shellLayoutLabel =
        function
        | Dashboard -> "Dashboard"
        | Projects -> "Projects"
        | Preferences -> "Settings"

    let shellLayoutUrl =
        function
        | Dashboard -> "/components/app-shell"
        | Projects -> "/components/app-shell?section=projects"
        | Preferences -> "/components/app-shell?section=settings"

    let shellLayoutExample id width bottomNavigation collapsible current =
        let layoutLabel =
            if bottomNavigation then
                "Full-width workspace"
            else
                "Constrained workspace"

        let header =
            fveSideNavHeader {
                _label "Workspace"

                fveSideNavHeaderCompact {
                    span {
                        _ariaHidden true
                        _class "font-semibold"
                        "W"
                    }
                }
            }

        let navigation =
            fveSideNav {
                _id (id + "-navigation")
                _ariaLabel (layoutLabel + " navigation")
                header

                fveSideNavContent {
                    fveSideNavItem {
                        _href (shellLayoutUrl Dashboard)
                        "Dashboard"
                        fveSideNavLeading { navigationGlyph }
                    }

                    fveSideNavItem {
                        _href (shellLayoutUrl Projects)
                        "Projects"
                        fveSideNavLeading { navigationGlyph }
                    }

                    fveSideNavItem {
                        _href (shellLayoutUrl Preferences)
                        "Settings"
                        fveSideNavLeading { navigationGlyph }
                    }
                }

                _value (shellLayoutUrl current)

                fveSideNavFooter {
                    fveSideNavRow {
                        _href (shellLayoutUrl Preferences)
                        "Andy Meier"
                    }
                }

                fveSideNavCompactFooter {
                    a {
                        _href (shellLayoutUrl Preferences)
                        _ariaLabel "Andy Meier profile"

                        _class
                            "flex size-8 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs font-semibold"

                        "AM"
                    }
                }
            }

        let content =
            Page.create
                (fvePageHeader { _label (shellLayoutLabel current) })
                (div {
                    _class
                        "grid min-h-80 place-items-center rounded-lg border border-dashed border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] p-6 text-center text-sm text-[var(--fve-muted-text)]"

                    "Page content"
                })
            |> Page.withWidth width
            |> Page.withTopBar (
                fvePageTopBar {
                    div {
                        _class "flex min-w-0 flex-1 items-center"

                        fveBreadcrumbs {
                            _id (id + "-breadcrumbs")
                            _ariaLabel (layoutLabel + " breadcrumb")

                            if current <> Dashboard then
                                fveBreadcrumbItem {
                                    _href (shellLayoutUrl Dashboard)
                                    "Dashboard"
                                }

                            fveBreadcrumbItem {
                                _href (shellLayoutUrl current)
                                shellLayoutLabel current
                            }
                        }
                    }
                }
            )
            |> Page.render

        let shell =
            (AppShell.create (id) (navigation) (content)
             |> AppShell.withBoundary (AppShellBoundary.Container)
             |> AppShell.asPreview ((layoutLabel + " layout preview")))

        let shell =
            if collapsible then
                shell |> AppShell.withCollapsibleNavigation
            else
                shell

        let shell =
            if bottomNavigation then
                shell
                |> AppShell.withMobileBottomNavigation (
                    fveBottomNavigation {
                        _id (id + "-bottom")
                        _ariaLabel "Workspace quick navigation"

                        fveBottomNavigationItem {
                            _href (shellLayoutUrl Dashboard)
                            "Dashboard"
                        }

                        fveBottomNavigationItem {
                            _href (shellLayoutUrl Projects)
                            "Projects"
                        }

                        fveBottomNavigationItem {
                            _href (shellLayoutUrl Preferences)
                            "Settings"
                        }
                    }
                )
            else
                shell

        div {
            _class "h-[36rem]"
            shell |> AppShell.render
        }

    let appShellExamples current =
        let source =
            [ "navigationGlyph"
              "ShellLayoutDestination"
              "shellLayoutLabel"
              "shellLayoutUrl"
              "shellLayoutExample" ]

        let preview _ (content: HtmlElement) = themedPreview content

        let withUsage usage (example: ComponentExample) =
            { example with
                source = example.source + "\n\n" + usage }

        [ sample
              "app-shell"
              "Collapsible sidebar with constrained content"
              source
              (preview "app-shell-sidebar" (shellLayoutExample "layout-sidebar" PageWidth.Reading false true current))
          |> withUsage "shellLayoutExample \"layout-sidebar\" PageWidth.Reading false true Dashboard"
          sample
              "app-shell-bottom"
              "Full-width content with mobile bottom navigation"
              source
              (preview "app-shell-bottom" (shellLayoutExample "layout-bottom" PageWidth.Full true false current))
          |> withUsage "shellLayoutExample \"layout-bottom\" PageWidth.Full true false Dashboard" ]

    let examples () = appShellExamples Dashboard


    let private galleryContent : Content =
        { description = "Frame application navigation, responsive chrome, content, and optional mobile destinations."
          usage = []
          sections = [
              titledSection
                  "complete-pages"
                  "Complete page examples"
                  [ p {
                        "For populated pages and a connected workflow, explore "

                        a {
                            _href "/examples/application"
                            "Application"
                        }

                        ". These shell examples intentionally show only the layout."
                    } ]
          ]
          apiCategory = Some "app-shell" }

    let appShellPageFor current =
        gallery appShellRegistration galleryContent (appShellExamples current)

    let appShellPage = appShellPageFor Dashboard
