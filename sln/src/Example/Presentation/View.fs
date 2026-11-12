namespace Example.Presentation

open FSharp.ViewEngine
open Example.Components
open Ledger.Domain
open type Html

module View =
    let private heading (title:string) (subtitle:string) =
        fveSlideHeader {
            fveSlideTitle { title }
            fveSlideSubtitle { subtitle }
        }

    let private money (value:decimal) = value.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo "en-US")

    let private metric (label:string) (value:string) (description:string) =
        div {
            _class "grid content-start gap-[1cqw] border-t-4 border-[var(--fve-brand-text)] pt-[1.5cqw]"
            dt { _class "text-[length:1.25cqw] text-[var(--fve-muted-text)]"; label }
            dd {
                _class "m-0 grid gap-[1cqw]"
                strong { _class "text-[length:3cqw] font-semibold tracking-tight"; value }
                p { _class "text-[length:1.25cqw] text-[var(--fve-muted-text)]"; description }
            }
        }

    let private reviewSlide (attributes:HtmlAttribute list) (footer:string) (children:HtmlElement list) =
        fveSlide {
            yield! attributes
            fveSlideBranding { strong { _class "text-[length:1.5625cqw] tracking-tight"; "Geldos" } }
            fveSlideFooter { footer }
            yield! children
        }

    let deck =
        let account = initialRecords.accounts |> List.find (fun row -> row.id = 101)
        let home = Ledger.Home.readHome { records=initialRecords; account="101"; period="90"; comparison="expected"; startDate=""; endDate="" }
        fvePresentation {
            _label "Geldos treasury review"
            _style "--fve-slide-cover-background:#141414"
            reviewSlide [_id "review-cover"; _variant SlideVariant.Cover] "Geldos · Treasury review · September 2026" [
                fveSlideHeader {
                    fveSlideTitle {
                        span { _class "block"; "Know your position." }
                        span { _class "block"; "Choose your next move." }
                    }
                    fveSlideSubtitle { "A treasury review built around one financial record." }
                }
                fveSlideContent {
                    p { _class "max-w-[58cqw] pt-[5cqw] text-[length:2.5cqw] leading-snug"; "From today's cash position to the decisions that shape the next quarter." }
                }
            ]
            reviewSlide [_id "cash-position"] "Source: Geldos example ledger · 90 days ending September 18, 2026" [
                heading "Start with the current position." "The same accounts and activity used throughout Geldos."
                fveSlideContent {
                    dl {
                        _class "grid grid-cols-3 gap-[3cqw] pt-[4cqw]"
                        metric account.name (money (balance initialRecords account)) "Opening balance plus recorded activity."
                        metric "Activity in the period" (string home.transactions.Length) "Transactions within the selected 90-day window."
                        metric "Ledger accounts" (string initialRecords.accounts.Length) "One chart of accounts for the whole organization."
                    }
                }
            ]
            reviewSlide [_id "decision-flow"] "Geldos · Treasury review · September 2026" [
                heading "Turn the ledger into a decision." "A simple sequence keeps the conversation focused."
                fveSlideContent {
                    ol {
                        _class "grid list-none grid-cols-3 gap-[2cqw] p-0 pt-[3cqw]"
                        for number, title, description in
                            [ "01", "Understand", "Review balances and identify the movements behind them."
                              "02", "Compare", "Use a common period and explicit scenario for every option."
                              "03", "Decide", "Record the next action and the evidence that supports it." ] do
                            li {
                                _class "grid content-start gap-[1cqw] border-t border-[var(--fve-border)] pt-[1.5cqw]"
                                span { _class "text-[length:3cqw] font-semibold text-[var(--fve-brand-text)]"; number }
                                h3 { _class "m-0 text-[length:2cqw] font-semibold"; title }
                                p { _class "m-0"; description }
                            }
                    }
                }
            ]
            reviewSlide [_id "compare-options"] "Geldos · Treasury review · September 2026" [
                heading "Compare options on equal terms." "Two directions, one set of assumptions."
                fveSlideContent {
                    div {
                        _class "grid h-full grid-cols-2 gap-[3cqw]"
                        for title, points in
                            [ "Retain liquidity", ["Keep cash available for near-term commitments."; "Preserve flexibility while the outlook changes."]
                              "Put cash to work", ["Match planned outflows to the same dated ledger."; "Compare expected returns with the cost of reduced flexibility."] ] do
                            section {
                                _class "grid content-start gap-[1.5cqw] border-t-4 border-[var(--fve-brand-text)] bg-[var(--fve-surface-subtle)] p-[2cqw]"
                                h3 { _class "m-0 text-[length:2.5cqw] font-semibold"; title }
                                ul { _class "grid list-disc gap-[1.5cqw] pl-[2cqw]"; for point in points do li { point } }
                            }
                    }
                }
            ]
            reviewSlide [_id "review-close"; _variant SlideVariant.Cover] "Geldos · Treasury review · September 2026" [
                heading "Make the next action explicit." "A clear decision is more useful than another spreadsheet."
                fveSlideContent {
                    p { _class "max-w-[58cqw] pt-[5cqw] text-[length:2.5cqw] leading-snug"; "Agree the action, its owner and the evidence to review next time." }
                }
            ]
        }

    let render theme (actions:HtmlElement list) =
        div {
            _class (ComponentsTheme.className theme + " min-h-screen bg-[var(--fve-background)] text-[var(--fve-text)]")
            header {
                _class "flex flex-wrap items-center justify-between gap-4 border-b border-[var(--fve-border)] px-6 py-4"
                h1 { _class "m-0 min-w-0 text-base font-semibold"; "Geldos · Treasury review" }
                nav { _ariaLabel "Presentation navigation"; _class "flex items-center gap-4"; a { _href "/examples/application"; _class "text-sm underline underline-offset-4"; "Application" }; yield! actions }
            }
            main { _id "main-content"; _class "min-w-0"; deck }
        }
