namespace Example.Server.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open Model
open Ledger.Domain
open Workflow

module internal Overview =
    let render (workflows: Workflow list) (query: Query) =
        let specUrl page query state appMode =
            specificationHref page state { query with appMode = appMode }

        div {
            _class "grid gap-12"

            Layout.documentationSection
                "financial-workspace"
                "Financial workspace"
                (Layout.prose (
                    div {
                        p {
                            "Ledger is a financial application for reviewing account balances and transactions within an organization, environment and ledger."
                        }

                        p {
                            "Choose a named workflow to review its Wireframe, Sequence and Rules. The fixture toolbar selects states of that workflow; expand the same Browser frame to follow its connected product journey at full viewport size."
                        }
                    }
                ))

            Layout.documentationSection
                "start-here"
                "Start here"
                (div {
                    _class "grid gap-4 sm:grid-cols-2"

                    for workflow in workflows do
                        div {
                            Layout.link (specUrl workflow.key query "" false) workflow.label

                            p {
                                _class "mt-1 text-sm text-[var(--fve-muted-text)]"
                                workflow.description
                            }
                        }
                })

            Layout.documentationSection
                "system"
                "System"
                (Layout.prose (p { "Application, Specification and API reference share the same USD financial model." }))

            Layout.link (Architecture.href "architecture") "Explore the system context"
        }
