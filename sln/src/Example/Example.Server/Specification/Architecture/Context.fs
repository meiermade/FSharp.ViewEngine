namespace Example.Server.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open Example.Server
open type Html

open Presentation

module internal Context =
    let private context =
        graph
            [ "flowchart LR"
              "  Operator[\"Financial operator<br/>Person<br/>Reviews accounts and activity\"]:::person"
              "  Developer[\"Application developer<br/>Person<br/>Reviews workflows and HTTP contracts\"]:::person"
              "  Ledger[\"Geldos<br/>Software system<br/>Financial application, executable specification and API reference\"]:::system"
              "  Operator -->|Uses financial pages| Ledger"
              "  Developer -->|Reviews specification and cURL examples| Ledger"
              "  class Ledger navigable"
              click "Ledger" "architecture/solution" ]

    let contents =
        [ "system-context", "System context"
          "system-boundary", "System boundary"
          "rules", "Rules" ]

    let render () =
        div {
            _class "grid gap-12"
            Layout.documentationSection "system-context" "System context" (diagram "Geldos system context" context)

            Layout.documentationSection
                "system-boundary"
                "System boundary"
                (Layout.prose (
                    p {
                        "Geldos provides a financial workspace, an executable specification and a cURL API reference over one shared set of USD account and transaction fixtures."
                    }
                ))

            rules
                [ "The three surfaces SHALL describe the same accounts, transactions and workspace contexts."
                  "Financial submissions SHALL validate finite outcomes without durably retaining private values or mutating the seeded records."
                  "The Geldos node SHALL open the Solution overview." ]
        }
