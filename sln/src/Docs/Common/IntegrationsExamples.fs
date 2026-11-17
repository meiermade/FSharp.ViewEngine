namespace Docs.Common

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

module IntegrationsExamples =
    let traceViewerIntegrationExample =
        div {
            _dataSignals "{tracestate: 'ready'}"
            _class "grid gap-3"

            div {
                _class "flex flex-wrap gap-2"

                for state, label in
                    [ "ready", "Show trace"
                      "pending", "Load trace"
                      "empty", "Show empty trace"
                      "error", "Simulate trace error" ] do
                    button {
                        _type "button"
                        _dataOn ("click", $"$tracestate = '{state}'")

                        _class
                            "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)]"

                        label
                    }
            }

            p {
                _dataShow "$tracestate == 'pending'"
                _role "status"
                _class "text-sm text-[var(--fve-muted-text)]"
                "Loading trace…"
            }

            p {
                _dataShow "$tracestate == 'empty'"
                _class "text-sm text-[var(--fve-muted-text)]"
                "No spans matched this trace query."
            }

            div {
                _dataShow "$tracestate == 'error'"
                _role "alert"

                _class
                    "flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-critical-subtle)] p-3 text-sm text-[var(--fve-critical-text)]"

                span { "Trace data could not be loaded." }

                button {
                    _type "button"
                    _dataOn ("click", "$tracestate = 'ready'")

                    _class
                        "rounded-[var(--fve-radius-control)] px-3 py-2 font-semibold ring-1 ring-[var(--fve-critical-ring)]"

                    "Retry trace"
                }
            }

            figure {
                _dataShow "$tracestate == 'ready'"
                _class "grid gap-3"

                div {
                    _tabindex 0
                    _ariaLabel "Scrollable trace diagram"

                    _class
                        "overflow-x-auto rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-4 ring-1 ring-[var(--fve-border)]"

                    svg {
                        _viewBox "0 0 720 180"
                        _role "img"
                        _ariaLabel "Request trace from browser through API and database"
                        _class "min-w-[40rem] w-full"

                        line {
                            _x1 120
                            _y1 90
                            _x2 600
                            _y2 90
                            _stroke "currentColor"
                            _strokeWidth 2
                        }

                        for x, label, duration in
                            [ 120, "Browser", "0 ms"; 360, "API", "42 ms"; 600, "Database", "18 ms" ] do
                            circle {
                                _cx x
                                _cy 90
                                _r 34
                                _fill "var(--fve-brand-subtle)"
                                _stroke "var(--fve-brand-solid)"
                                _strokeWidth 2
                            }

                            textElement {
                                _x x
                                _y 86
                                _textAnchor "middle"
                                _fill "currentColor"
                                label
                            }

                            textElement {
                                _x x
                                _y 108
                                _textAnchor "middle"
                                _fill "currentColor"
                                duration
                            }
                    }
                }

                figcaption {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Trace request-84f2 · 60 ms total. The ordered list remains the accessible source of truth."
                }

                ol {
                    _class "grid gap-2"

                    for label, detail in
                        [ "Browser", "GET /accounts · starts at 0 ms"
                          "API", "Authorization and query · 42 ms"
                          "Database", "SELECT accounts · 18 ms" ] do
                        li {
                            _class
                                "flex items-start justify-between gap-3 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] p-3 text-sm"

                            strong { label }

                            span {
                                _class "text-right text-[var(--fve-muted-text)]"
                                detail
                            }
                        }
                }
            }
        }

    let financialChartIntegrationExample =
        div {
            _dataSignals "{chartperiod: '30d'}"
            _class "grid gap-4"

            div {
                _class "flex flex-wrap gap-2"

                for value, label in [ "30d", "30 days"; "90d", "90 days" ] do
                    button {
                        _type "button"
                        _dataOn ("click", $"$chartperiod = '{value}'")
                        _dataAttr ("aria-pressed", $"$chartperiod == '{value}' ? 'true' : 'false'")

                        _class
                            "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] aria-pressed:bg-[var(--fve-brand-subtle)] aria-pressed:text-[var(--fve-brand-text)]"

                        label
                    }
            }

            div {
                _tabindex 0
                _ariaLabel "Scrollable balance chart"

                _class
                    "overflow-x-auto rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-4 ring-1 ring-[var(--fve-border)]"

                for value, label, points in
                    [ "30d", "Thirty-day balance: $24,820", "0,130 120,112 240,122 360,76 480,58 600,32"
                      "90d", "Ninety-day balance: $24,820", "0,150 120,138 240,96 360,118 480,70 600,32" ] do
                    svg {
                        _viewBox "0 0 600 180"
                        _role "img"
                        _ariaLabel label
                        _dataShow $"$chartperiod == '{value}'"
                        _class "min-w-[36rem] w-full"

                        line {
                            _x1 0
                            _y1 160
                            _x2 600
                            _y2 160
                            _stroke "var(--fve-border)"
                            _strokeWidth 1
                        }

                        polyline {
                            _points points
                            _fill "none"
                            _stroke "var(--fve-brand-solid)"
                            _strokeWidth 4
                            _vectorEffect "non-scaling-stroke"
                        }
                    }
            }

            table {
                _class "w-full text-sm"

                caption {
                    _class "text-left font-semibold text-[var(--fve-text)]"
                    "Balance data"
                }

                thead {
                    tr {
                        th {
                            _scope "col"
                            _class "py-2 text-left"
                            "Period"
                        }

                        th {
                            _scope "col"
                            _class "py-2 text-right"
                            "Closing balance"
                        }
                    }
                }

                tbody {
                    tr {
                        _dataShow "$chartperiod == '30d'"

                        td {
                            _class "py-2"
                            "30 days"
                        }

                        td {
                            _class "py-2 text-right"
                            "$24,820"
                        }
                    }

                    tr {
                        _dataShow "$chartperiod == '90d'"

                        td {
                            _class "py-2"
                            "90 days"
                        }

                        td {
                            _class "py-2 text-right"
                            "$24,820"
                        }
                    }
                }
            }
        }

    let messagingIntegrationExample =
        div {
            _dataSignals "{thread: 'northwind'}"
            _class "grid min-w-0 gap-4 md:grid-cols-[16rem_minmax(0,1fr)]"

            nav {
                _ariaLabel "Conversations"

                ul {
                    _class "grid gap-2"

                    for value, label, preview in
                        [ "northwind", "Northwind", "Can we move pickup?"
                          "contoso", "Contoso", "Thanks for the update." ] do
                        li {
                            button {
                                _type "button"
                                _dataOn ("click", $"$thread = '{value}'")
                                _dataAttr ("aria-pressed", $"$thread == '{value}' ? 'true' : 'false'")

                                _class
                                    "w-full rounded-[var(--fve-radius-control)] p-3 text-left ring-1 ring-[var(--fve-border)] aria-pressed:bg-[var(--fve-brand-subtle)]"

                                strong {
                                    _class "block"
                                    label
                                }

                                span {
                                    _class "block truncate text-sm text-[var(--fve-muted-text)]"
                                    preview
                                }
                            }
                        }
                }
            }

            section {
                _ariaLabel "Current conversation"
                _class "grid min-w-0 gap-3"

                div {
                    _dataShow "$thread == 'northwind'"

                    h3 {
                        _class "font-semibold"
                        "Northwind"
                    }

                    p {
                        _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface-subtle)] p-3"
                        "Can we move pickup to 10:30 AM?"
                    }
                }

                div {
                    _dataShow "$thread == 'contoso'"

                    h3 {
                        _class "font-semibold"
                        "Contoso"
                    }

                    p {
                        _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface-subtle)] p-3"
                        "Thanks for the update."
                    }
                }

                fveField {
                    fveLabel {
                        _for "reply"
                        "Reply"
                    }

                    fveTextarea {
                        _id "reply"
                        _name "reply"
                        _ariaDescribedby (String.concat " " [ ("reply") + "-description" ])
                    }

                    fveFieldDescription {
                        _id ("reply" + "-description")
                        "Messages in this bounded example stay local to the rendered page."
                    }
                }

                fveButton {
                    _dataOn (
                        "click",
                        "document.getElementById('message-feedback').textContent = 'Message queued in this resettable demo.'"
                    )

                    "Send message"
                }

                p {
                    _id "message-feedback"
                    _role "status"
                    _ariaLive "polite"
                    _class "text-sm text-[var(--fve-muted-text)]"
                }
            }
        }
