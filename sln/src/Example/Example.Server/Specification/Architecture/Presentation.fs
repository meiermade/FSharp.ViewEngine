namespace Example.Server.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open Example.Server
open type Html

module internal Presentation =
    let href page = "/examples/specification/" + page

    let click node page =
        $"  click {node} href \"{href page}\" _self"

    let graph lines =
        String.concat
            "\n"
            (lines
             @ [ "  classDef person fill:#f3f4f6,stroke:#6b7280,color:#111827"
                 "  classDef system fill:#059669,stroke:#047857,color:#ffffff,stroke-width:2px"
                 "  classDef project fill:#ecfdf5,stroke:#059669,color:#064e3b"
                 "  classDef executable fill:#d1fae5,stroke:#047857,color:#064e3b,stroke-width:2px"
                 "  classDef navigable stroke:#059669,stroke-width:3px" ])

    let diagram label (source: string) =
        div {
            _role "region"
            _ariaLabel label
            _tabindex 0

            _class
                "min-w-0 overflow-x-auto rounded-xl focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

            fveMermaid {
                _c4 true
                source
            }
        }

    let rules (items: string list) =
        Layout.documentationSection
            "rules"
            "Rules"
            (Layout.prose (
                ul {
                    for item in items do
                        li { item }
                }
            ))

    let contracts heading (description: string) (dependencies: string list) (code: string) requirements =
        div {
            _class "grid gap-12"
            Layout.documentationSection "responsibility" "Responsibility" (Layout.prose (p { description }))

            Layout.documentationSection
                "dependencies"
                "Dependencies"
                (Layout.prose (
                    ul {
                        for dependency in dependencies do
                            li { dependency }
                    }
                ))

            Layout.documentationSection
                "contract"
                heading
                (fveCodeBlock {
                    _language "fsharp"
                    code
                })

            rules requirements
        }
