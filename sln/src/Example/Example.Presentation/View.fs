namespace Example.Presentation

open FSharp.ViewEngine
open Example.Components
open type Html

module View =
    let home = "/examples/presentation"
    let title = "Geldos · Product overview"

    type HostedLinks = { application: string; docs: string }

    let private logo source (classes: string) =
        img { _src source; _alt ""; _width "24"; _height "24"; _class classes }

    let private slide logoSource (id: string) (chapter: string) (footer: string) (content: HtmlElement) =
        fveSlide {
            _id id
            fveSlideHeader {
                p { _class "m-0 text-[length:1.25cqw] font-semibold uppercase tracking-widest text-[var(--fve-brand-text)]"; text chapter }
            }
            fveSlideBranding {
                strong {
                    _class "flex items-center gap-[0.4cqw] text-[length:1.5625cqw] tracking-tight"
                    logo logoSource "size-[2.25cqw] shrink-0"
                    "Geldos"
                }
            }
            fveSlideFooter { text footer }
            content
        }

    let private titleBlock (headline: string) (description: string) =
        fragment {
            fveSlideTitle {
                _class "max-w-[32cqw] text-balance font-serif text-[length:4.35cqw] font-medium leading-[1.05] tracking-tight"
                text headline
            }
            p { _class "m-0 max-w-[30cqw] text-[length:1.8cqw] leading-snug text-[var(--fve-muted-text)]"; text description }
        }

    let private productVisual hosted asset name (width: string) (height: string) (description: string) =
        figure {
            _class "m-0 min-w-0 rounded-[1.25cqw] bg-[var(--fve-brand-subtle)] p-[1.4cqw]"
            for mode in (if hosted then ["light"; "dark"] else ["light"]) do
                img {
                    _src (asset $"presentation/{name}-{mode}.webp")
                    _alt description
                    _width width; _height height
                    _class (
                        "h-auto w-full rounded-[0.65cqw] border border-[var(--fve-border)] bg-[var(--fve-surface)] shadow-sm "
                        + (if not hosted then "block" elif mode = "light" then "block dark:hidden" else "hidden dark:block"))
                }
        }

    let private split (copy: HtmlElement) (visual: HtmlElement) =
        fveSlideContent {
            _class "grid grid-cols-[minmax(0,5fr)_minmax(0,9fr)] items-center gap-[3cqw]"
            div { _class "grid content-center justify-items-start gap-[1.8cqw]"; copy }
            visual
        }

    let private primaryLink (href: string) (label: string) =
        a {
            _href href
            _class "inline-flex items-center rounded-[0.5cqw] bg-[var(--fve-brand-text)] px-[1.3cqw] py-[0.65cqw] font-semibold text-[var(--fve-surface)] no-underline hover:opacity-90 focus-visible:outline-2 focus-visible:outline-offset-4"
            text label
        }

    let deck (links: HostedLinks option) asset =
        let hosted = links.IsSome
        let logoSource = asset "presentation/geldos.svg"
        let productSlide = slide logoSource
        let visual name description = productVisual hosted asset name "1368" "810" description
        fvePresentation {
            _label "Geldos product overview"
            productSlide "product-cover" "Product overview" "Geldos" (
                split
                    (titleBlock "Your financial operating system" "Banking, investing, accounting and planning in one place.")
                    (productVisual hosted asset "overview" "1728" "972" "Geldos Home with the balance for Operating checking and a chart of actual and expected balances."))
            productSlide "product-past" "Past" "Geldos · Transactions" (
                split
                    (titleBlock "Understand what changed" "See the money coming in and going out. Trace each change back to the transaction behind it.")
                    (visual "transactions" "Transactions for Operating checking include a $3,200 client payment and an $89 cloud hosting expense."))
            productSlide "product-present" "Present" "Geldos · Accounts" (
                split
                    (titleBlock "See where you stand" "See balances across your ledger, then open a group to see the accounts that make up its total.")
                    (visual "accounts" "The Accounts hierarchy shows Operating checking, Savings and Tax reserve under Assets and the business credit card under Liabilities."))
            productSlide "product-future" "Future" "Geldos · Home" (
                split
                    (fragment {
                        yield titleBlock "Compare what could happen" "Start from the same account balance and switch scenarios to see how different assumptions change the outlook."
                        for destinations in Option.toList links do
                            yield primaryLink (destinations.application + "?account=101&comparison=optimistic") "Try the scenarios"
                    })
                    (visual "scenarios" "Home’s balance chart shows recorded activity and the optimistic scenario for Operating checking over the same 90-day window."))
            productSlide "product-explore" "Next step" "Geldos" (
                split
                    (fragment {
                        yield titleBlock "Explore Geldos" "Open the example ledger, or see how the same accounts and transactions are represented in the API."
                        match links with
                        | Some destinations ->
                            yield div {
                                _class "flex flex-wrap items-center gap-x-[1.5cqw] gap-y-[0.75cqw]"
                                primaryLink (destinations.application + "?account=101") "Application demo"
                                a {
                                    _href destinations.docs
                                    _class "text-[var(--fve-brand-text)] underline decoration-1 underline-offset-4 focus-visible:outline-2 focus-visible:outline-offset-4"
                                    "API reference"
                                }
                            }
                        | None ->
                            yield p { _class "m-0 font-semibold text-[var(--fve-brand-text)]"; "Application demo · API reference" }
                    })
                    (div {
                        _class "grid min-h-[25cqw] place-content-center justify-items-center rounded-[1.25cqw] bg-[var(--fve-brand-subtle)] p-[2cqw]"
                        logo logoSource "size-[10cqw]"
                        p { _class "m-0 font-serif text-[length:4cqw] font-medium tracking-tight"; "Geldos" }
                    }))
        }

    let render links theme (actions: HtmlElement list) =
        let asset path = home + "/images/" + path
        div {
            _class (ComponentsTheme.className theme + " min-h-screen bg-[var(--fve-background)] text-[var(--fve-text)]")
            header {
                _class "flex flex-wrap items-center justify-between gap-4 border-b border-[var(--fve-border)] px-6 py-4"
                h1 {
                    _class "m-0 flex min-w-0 items-center gap-1 text-base font-semibold"
                    logo (asset "presentation/geldos.svg") "size-6 shrink-0"
                    text title
                }
                nav { _ariaLabel "Presentation navigation"; _class "flex items-center gap-4"; a { _href links.application; _class "text-sm underline underline-offset-4"; "Application" }; yield! actions }
            }
            main { _id "main-content"; _class "min-w-0"; deck (Some links) asset }
        }
