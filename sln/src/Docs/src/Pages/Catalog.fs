namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

/// The library distributes components, not application/documentation frameworks.
module Catalog =
    let private group label pages : NavSection =
        { label = label
          pages = pages
          sections = [] }

    let navigation =
        [ group "Actions" Components.actionRegistrations
          group
              "Feedback"
              (Components.feedbackRegistrations
               @ [ ComponentDocumentation.calloutRegistration ])
          group
              "Data display"
              (Components.dataDisplayRegistrations
               @ [ ComponentDocumentation.cardRegistration
                   Components.messageRegistration ])
          group
              "Form controls"
              (Components.formControlRegistrations
               @ [ ComponentDocumentation.fieldGroupRegistration ])
          group "Navigation" (Components.navigationRegistrations @ [ Components.bottomNavigationRegistration ])
          group "Overlays" Components.overlayRegistrations
          group
              "Layout"
              (Components.frameRegistrations
               @ [ Components.pageHeaderRegistration
                   ComponentDocumentation.sectionHeaderRegistration
                   Components.pageTopBarRegistration ])
          group
              "Code and diagrams"
              [ ComponentDocumentation.codeBlockRegistration
                ComponentDocumentation.exampleRegistration
                ComponentDocumentation.mermaidRegistration
                ComponentDocumentation.fsharpReferenceRegistration ] ]

    let componentRegistrations = navigation |> List.collect _.pages

    let overviewPage =
        DocumentationPage.create Components.overviewRegistration.id "Components"
        |> DocumentationPage.withDescription
            "Opinionated, Tailwind-styled components for F#, with Datastar-powered interactivity. Install editable source and customize it in your application."
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  "start"
                  "Get started"
                  [ fveCodeBlock {
                        _language "shell"

                        "dotnet fve init src/Acme.Components/Acme.Components.fsproj --namespace Acme.Components\ndotnet fve add button input --config src/Acme.Components/fve.json"
                    }
                    p {
                        "Styles use "

                        a {
                            _href "https://tailwindcss.com/"
                            _class "text-[var(--fve-brand-text)] underline underline-offset-2"
                            "Tailwind CSS"
                        }

                        "; interactivity uses "

                        a {
                            _href "https://data-star.dev/"
                            _class "text-[var(--fve-brand-text)] underline underline-offset-2"
                            "Datastar"
                        }

                        ". The CLI copies the source and runtime assets you need; your application owns the CSS and JavaScript builds."
                    }
                    p {
                        a {
                            _href "/components/installation"
                            "Installation"
                        }

                        " · "

                        a {
                            _href "/components/theming"
                            "Theming"
                        }

                        " · "

                        a {
                            _href "/examples"
                            "Three complete example templates"
                        }
                    } ]
              for section in navigation do
                  DocumentationSection.create
                      (section.label.ToLowerInvariant().Replace(" ", "-"))
                      section.label
                      [ div {
                            _class "docs-catalog-grid"

                            for item in section.pages do
                                a {
                                    _href item.path
                                    _class "docs-catalog-card"
                                    strong { item.title }

                                    span {
                                        _class "docs-catalog-action"
                                        "View component →"
                                    }
                                }
                        } ] ]

    let tryPage path =
        if path = Components.overviewRegistration.path then
            Some overviewPage
        else
            None
