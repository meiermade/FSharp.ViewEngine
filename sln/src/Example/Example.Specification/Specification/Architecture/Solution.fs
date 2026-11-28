namespace Example.Specification.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open Example.Specification.Web
open type Html

open Presentation

module internal Solution =
    let private solution =
        graph
            [ "flowchart LR"
              "  Browser[\"Browser<br/>External runtime\"]:::person"
              "  subgraph Product[\"Geldos solution\"]"
              "    Server[\"Example.Application<br/>Marketing and application\"]:::executable"
              "    Spec[\"Example.Specification<br/>Executable workflows and fixtures\"]:::executable"
              "    Docs[\"Example.Documentation<br/>Resource guides and API reference\"]:::executable"
              "    Deck[\"Example.Presentation<br/>Hosted product deck\"]:::executable"
              "    Domain[\"Example.Domain<br/>F# library project<br/>Financial and workspace facts, balance and deletion rules\"]:::project"
              "    Components[\"Example.UI<br/>Source-installed F# project<br/>Installed controls and shared product views\"]:::project"
              "    Server --> Components"
              "    Server -->|Use cases| Domain"
              "    Spec -->|Architecture types| Domain"
              "    Spec -->|Fixture view models| Components"
              "    Docs --> Components"
              "    Docs -->|Sample payloads| Domain"
              "    Deck --> Components"
              "  end"
              "  Browser -.->|HTTP GET and POST| Server"
              "  Browser -.->|HTTP GET and POST| Spec"
              "  Browser -.->|HTTP GET| Docs"
              "  Browser -.->|HTTP GET| Deck"
              "  class Server,Domain,Components navigable"
              click "Server" "architecture/solution/server"
              click "Domain" "architecture/solution/domain"
              click "Components" "architecture/solution/components" ]

    let contents =
        [ "projects", "Projects"
          "project-inventory", "Project inventory"
          "requirements", "Requirements" ]

    let render () =
        div {
            _class "grid gap-12"

            Layout.documentationSection
                "projects"
                "Projects"
                (diagram "Geldos project dependencies and HTTP boundary" solution)

            Layout.documentationSection
                "project-inventory"
                "Project inventory"
                (fveDescriptionList {
                    fveDescriptionListItem {
                        _label "Example.Domain"

                        Layout.inlineLink (href "architecture/solution/domain") "Financial types and invariants"
                    }

                    fveDescriptionListItem {
                        _label "Application use cases"

                        Layout.inlineLink
                            (href "architecture/solution/application")
                            "Validation operations and outcomes"
                    }

                    fveDescriptionListItem {
                        _label "Example.UI"

                        Layout.inlineLink
                            (href "architecture/solution/components")
                            "Installed controls and shared app/Spec product views"
                    }

                    fveDescriptionListItem {
                        _label "Example.Application"

                        Layout.inlineLink
                            (href "architecture/solution/server")
                            "HTTP hosting for Marketing and Application"
                    }

                    fveDescriptionListItem {
                        _label "Example.Specification"
                        "A standalone review host that supplies fixtures to the product views."
                    }

                    fveDescriptionListItem {
                        _label "Example.Documentation"
                        "An independently hosted documentation site for the financial resources and illustrative API."
                    }

                    fveDescriptionListItem {
                        _label "Example.Presentation"
                        "A standalone host for the same deck that runs offline as a script."
                    }
                })

            requirements
                [ "Application and Specification SHALL use the same UI views. UI SHALL own their view models and reference neither Application nor Domain."
                  "The Application host maps domain outcomes to view models. Specification supplies fixture state and references Domain only to show actual types in Architecture."
                  "Application, Specification, Documentation, and Presentation SHALL run independently. Marketing and Application share one host. Spec is normally local-only; the FVE catalog publishes a fixture-only demo."
                  "Solid relationships SHALL represent project references; dashed relationships SHALL represent HTTP requests." ]
        }
