namespace Example.Specification.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open Example.Specification
open type Html
open Model
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
                            "Geldos is a financial operating system for reviewing account balances and transactions within an organization, environment and ledger."
                        }

                        p {
                            "Choose a workflow and explore its Fixture. Sequence diagrams explain cross-boundary behavior; optional Requirements cover only non-obvious behavior not already shown or specified in Architecture. The fixture toolbar selects states; expand the same Browser frame to follow the connected product journey at full viewport size."
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
                            Layout.inlineLink (specUrl workflow.key query "" false) workflow.label

                            p {
                                _class "mt-1 text-sm text-[var(--fve-muted-text)]"
                                workflow.description
                            }
                        }
                })

            Layout.documentationSection
                "system"
                "System"
                (Layout.prose (p { "Marketing, Application, Specification and API reference share the same Geldos identity and USD financial model." }))

            Layout.inlineLink (Architecture.href "architecture") "Explore the system context"
        }
