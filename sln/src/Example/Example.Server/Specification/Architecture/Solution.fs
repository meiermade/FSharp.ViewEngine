namespace Example.Server.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html

open Presentation

module internal Solution =
    let private solution =
        graph
            [ "flowchart LR"
              "  Browser[\"Browser<br/>External runtime\"]:::person"
              "  subgraph Product[\"Geldos solution\"]"
              "    Server[\"Example.Server<br/>Executable F# project<br/>HTML, forms, Specification and API reference\"]:::executable"
              "    Application[\"Example.Application<br/>F# library project<br/>Submission validation and batch deletion\"]:::project"
              "    Domain[\"Example.Domain<br/>F# library project<br/>Financial and workspace facts, balance and deletion rules\"]:::project"
              "    Components[\"Example.Components<br/>Source-installed F# project<br/>Reusable HTML controls and Rocket chart\"]:::project"
              "    Server -->|Project reference| Application"
              "    Server -->|Project reference| Components"
              "    Application -->|Project reference| Domain"
              "  end"
              "  Browser -.->|HTTP GET and POST| Server"
              "  class Server,Application,Domain,Components navigable"
              click "Server" "architecture/solution/server"
              click "Application" "architecture/solution/application"
              click "Domain" "architecture/solution/domain"
              click "Components" "architecture/solution/components" ]

    let contents =
        [ "projects", "Projects"
          "project-inventory", "Project inventory"
          "rules", "Rules" ]

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
                        _label "Example.Application"

                        Layout.inlineLink
                            (href "architecture/solution/application")
                            "Validation operations and outcomes"
                    }

                    fveDescriptionListItem {
                        _label "Example.Components"

                        Layout.inlineLink
                            (href "architecture/solution/components")
                            "Installed HTML building blocks and Rocket chart"
                    }

                    fveDescriptionListItem {
                        _label "Example.Server"

                        Layout.inlineLink
                            (href "architecture/solution/server")
                            "HTTP hosting and the three rendered surfaces"
                    }
                })

            rules
                [ "Compile-time references SHALL flow Server → Application → Domain; Server SHALL also reference Components."
                  "Domain and Application SHALL remain independent of Giraffe, HTML and browser state."
                  "Example.Server SHALL be the only executable project; it SHALL run without Docs."
                  "Solid relationships SHALL represent project references; the dashed relationship SHALL represent HTTP requests." ]
        }
