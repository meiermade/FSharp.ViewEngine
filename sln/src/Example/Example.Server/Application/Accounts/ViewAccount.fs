namespace Example.Server.ApplicationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain
open Controls
open TransactionTable

module internal ViewAccount =
    let chart (query: Query) =
        let selected =
            query.records.accounts
            |> List.tryFind (fun account -> string account.id = query.account)
            |> Option.defaultValue query.records.accounts.Head

        let range =
            match query.range with
            | "30" -> 30
            | "7" -> 7
            | _ -> 90

        let endDate = DateOnly(2026, 9, 18)
        let startDate = endDate.AddDays(-range)

        let balanceAt at =
            selected.openingBalance
            + (query.records.transactions
               |> List.filter (fun transaction -> transaction.accountId = selected.id && transaction.date <= at)
               |> List.sumBy _.amount)

        let actual =
            [ for index in 0..8 ->
                  startDate.AddDays(range * index / 8), balanceAt (startDate.AddDays(range * index / 8)) ]

        let factor =
            match query.comparison with
            | "optimistic" -> 1.5M
            | "pessimistic" -> -0.5M
            | _ -> 1M

        let scenario =
            [ for index in 0..8 ->
                  startDate.AddDays(range * index / 8), selected.openingBalance + decimal index * 120M * factor ]

        let values = (actual @ scenario) |> List.map snd
        let lower = (List.min values) - 500M
        let upper = (List.max values) + 500M

        let point index value =
            $"{40 + index * 90},{210 - int ((value - lower) / (upper - lower) * 170M)}"

        let points values =
            values
            |> List.mapi (fun index (_, value) -> point index value)
            |> String.concat " "

        fveCard {
            div {
                _class "grid gap-4"

                div {
                    _class "flex flex-wrap items-start justify-between gap-3"

                    div {
                        p {
                            _class "text-xs text-[var(--fve-muted-text)]"
                            "Balance · " + selected.name
                        }

                        p {
                            _class "mt-1 text-2xl font-semibold tabular-nums"
                            money (balance query.records selected)
                        }
                    }

                    div {
                        _class "flex gap-4 text-xs text-[var(--fve-muted-text)]"

                        span {
                            _class "text-[var(--fve-brand-text)]"
                            "━ Actual"
                        }

                        span {
                            "┄ "
                            + (match query.comparison with
                               | "optimistic" -> "Optimistic"
                               | "pessimistic" -> "Pessimistic"
                               | _ -> "Expected")
                            + " scenario"
                        }
                    }
                }

                svg {
                    _viewBox "0 0 800 240"
                    _role "img"
                    _ariaLabel ("Actual and scenario balances for " + selected.name)
                    _class "h-56 w-full"
                    Svg.titleElement { "Actual and scenario balances." }

                    for y in [ 40; 125; 210 ] do
                        line {
                            _x1 "40"
                            _x2 "760"
                            _y1 (string y)
                            _y2 (string y)
                            _stroke "var(--fve-border)"
                        }

                    polyline {
                        _points (points scenario)
                        _fill "none"
                        _stroke "var(--fve-muted-text)"
                        _strokeWidth "2"
                        _strokeDasharray "6 5"
                    }

                    polyline {
                        _points (points actual)
                        _fill "none"
                        _stroke "var(--fve-brand-solid)"
                        _strokeWidth "3"
                    }
                }

                div {
                    _class "flex justify-between text-xs text-[var(--fve-muted-text)]"
                    span { date startDate }
                    span { date endDate }
                }
            }
        }

    let accountDetail (query: Query) id =
        let account = findAccount id

        div {
            _class "grid gap-6"

            fveDescriptionList {
                fveDescriptionListItem {
                    _label "Type"
                    string account.accountType
                }

                fveDescriptionListItem {
                    _label "Commodity"
                    "USD"
                }

                fveDescriptionListItem {
                    _label "Parent account"
                    groupLabel account.accountType
                }

                fveDescriptionListItem {
                    _label "Source"
                    "Manual"
                }

                fveDescriptionListItem {
                    _label "Month-end observed balance"
                    "Not required"
                }

                fveDescriptionListItem {
                    _label "Balance"
                    money (balance query.records account)
                }

                _columns DescriptionListColumns.Three
            }

            chart { query with account = string id }

            Layout.section
                "Transactions"
                (transactionTable
                    query
                    (account.name + " transactions")
                    (query.records.transactions |> List.filter (fun transaction -> transaction.accountId = id))
                    false)
        }
