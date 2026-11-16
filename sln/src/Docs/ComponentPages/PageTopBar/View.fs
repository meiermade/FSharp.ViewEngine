namespace Docs.ComponentPages.PageTopBar

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
    let pageTopBarRegistration =
        applicationRegistration "components-page-top-bar" "/components/page-top-bar" "Page top bar" "Page top bar"

    let pageTopBarExample (breadcrumbs: HtmlElement) =
        fvePageTopBar {
            div {
                _class "flex min-w-0 flex-1 items-center"
                breadcrumbs
            }
        }

    let renderPageTopBar breadcrumbs = pageTopBarExample breadcrumbs

    let pageTopBar breadcrumbs = pageTopBarExample breadcrumbs

    let pageTopBarPreview =
        div {
            fveBreadcrumbs {
                _id "page-bar-breadcrumbs"
                _ariaLabel "Breadcrumb"

                fveBreadcrumbItem {
                    _href "/components"
                    "Home"
                }

                fveBreadcrumbItem {
                    _href "/components/table"
                    "Items"
                }

                fveBreadcrumbItem {
                    _href "/components/card"
                    "Project notes"
                }
            }
            |> renderPageTopBar
        }
        |> themedSurface

    let shellTopBarExample =
        div {
            _class "@container/fve-shell"

            fvePageTopBar {
                fvePageTopBarBrand {
                    strong {
                        _class "text-base font-semibold"
                        "Workspace"
                    }
                }

                fveBreadcrumbs {
                    _id "shell-bar-breadcrumbs"
                    _ariaLabel "Shell breadcrumb"

                    fveBreadcrumbItem {
                        _href "/components"
                        "Home"
                    }

                    fveBreadcrumbItem {
                        _href "/components/table"
                        "Items"
                    }
                }

                fvePageTopBarActions {
                    a {
                        _href "/examples/application/profile"
                        _class "whitespace-nowrap text-sm font-medium"
                        "View profile"
                    }
                }
            }

            div {
                _class "flex min-h-32"

                div {
                    _class "hidden w-60 shrink-0 @3xl/fve-shell:block"

                    fveSideNav {
                        _id "shell-top-bar-navigation"
                        _ariaLabel "Workspace"

                        fveSideNavContent {
                            fveSideNavItem {
                                _href "/components"
                                "Home"
                            }

                            fveSideNavItem {
                                _href "/components/table"
                                "Items"
                            }
                        }

                        Html._width SideNavWidth.Standard
                    }
                }

                div {
                    _class "min-w-0 flex-1 px-4 py-6 sm:px-6 lg:px-8"

                    p {
                        _class "text-sm text-[var(--fve-muted-text)]"
                        "Page region"
                    }
                }
            }
        }

    let examples () =
            [ sample "page-top-bar" "With breadcrumbs" [ "pageTopBarExample"; "renderPageTopBar" ] pageTopBarPreview
              sample
                  "page-top-bar-shell"
                  "Full-width shell bar"
                  [ "shellTopBarExample" ]
                  (shellTopBarExample |> themedSurface)
              |> note "Set --fve-shell-sidebar-width on the bar or its containing region to match the consumer-owned sidebar. The brand region defaults to 15rem and appears at wide shell sizes." ]


    let private galleryContent : Content =
        { description = "Compose compact page context and actions above the primary page heading."
          usage = []
          sections = []
          apiCategory = Some "page-top-bar" }

    let pageTopBarPage = gallery pageTopBarRegistration galleryContent (examples ())
