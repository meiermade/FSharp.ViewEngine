namespace Example.Server

open FSharp.ViewEngine
open Example.Components
open type Html

/// Product copy, branding, prices and destinations belong to this consumer.
module Marketing =
    let private home = "/examples/marketing"
    let private signup = home + "/signup"
    let private login = home + "/login"
    let private source = "https://github.com/meiermade/geldos"
    let paths = [ home; home + "/features"; home + "/pricing"; login; signup ]

    let title (path:string) =
        match path.TrimEnd('/').Split('/') |> Array.last with
        | "features" -> "Features"
        | "pricing" -> "Pricing"
        | "login" -> "Log in"
        | "signup" -> "Sign up"
        | _ -> "Geldos"

    let private link (href:string) (label:string) =
        fveLink { _href href; _variant LinkVariant.Ghost; _size Size.Medium; label }

    let private cta (href:string) (label:string) =
        fveLink {
            _href href; _variant LinkVariant.Solid; _size Size.ExtraLarge
            _style "--fve-control-min-height:2.75rem;--fve-control-font-size:1rem"
            _class "px-5"
            label
        }

    let private brand =
        a {
            _href home; _ariaLabel "Geldos home"
            _class "inline-flex min-h-9 shrink-0 items-center gap-2 rounded-sm text-base font-semibold focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
            img { _src "/examples/assets/images/geldos.svg"; _alt ""; _width "24"; _height "24"; _class "size-6" }
            "Geldos"
        }

    let private navigation path =
        fragment {
            for href, label in [home + "/features", "Features"; home + "/pricing", "Pricing"; "/examples/specification", "Docs"] do
                fveLink {
                    _href href; _variant LinkVariant.Ghost; _size Size.Medium
                    if path = href then _ariaCurrent "page"
                    label
                }
        }

    let private header path (actions:HtmlElement list) =
        header {
            _class "bg-[var(--fve-background)]"
            div {
                _class "mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-x-6 gap-y-3 px-4 py-4 sm:px-6"
                brand
                nav {
                    _ariaLabel "Product"; _class "hidden items-center gap-1 md:flex"
                    navigation path
                    for action in actions do action
                }
                nav {
                    _ariaLabel "Account"; _class "flex min-w-0 flex-wrap items-center justify-end gap-2"
                    link login "Log in"
                    fveLink { _href signup; _variant LinkVariant.Solid; _size Size.Medium; "Sign up" }
                }
                details {
                    _class "w-full md:hidden"
                    summary { _class "min-h-9 cursor-pointer py-2 text-sm font-semibold"; "Menu" }
                    nav {
                        _ariaLabel "Mobile product"; _class "flex flex-wrap items-center gap-2 py-3"
                        navigation path
                        for action in actions do action
                    }
                }
            }
        }

    let private footer =
        footer {
            _class "border-t border-[var(--fve-border)]"
            div {
                _class "mx-auto grid max-w-5xl gap-8 px-4 py-10 sm:px-6 md:grid-cols-[1fr_auto]"
                div {
                    brand
                    p { _class "mt-3 max-w-sm text-base leading-7 text-[var(--fve-muted-text)]"; "Your financial operating system." }
                }
                nav {
                    _ariaLabel "Footer"; _class "flex flex-wrap content-start gap-2 md:max-w-md"
                    link (home + "/features") "Features"
                    link (home + "/pricing") "Pricing"
                    link "/examples/specification" "Docs"
                    link "/examples/api-documentation" "API reference"
                    link source "GitHub"
                    link (source + "/blob/main/LICENSE") "Apache-2.0 license"
                }
                p { _class "text-sm text-[var(--fve-muted-text)] md:col-span-2"; "© Meier Made, LLC" }
            }
        }

    let private intro (heading:string) (description:string) =
        div {
            _class "max-w-3xl"
            h1 { _class "font-serif text-2xl font-medium leading-tight tracking-tight min-[24rem]:text-3xl sm:text-6xl"; heading }
            p { _class "mt-6 max-w-2xl text-lg leading-8 text-[var(--fve-muted-text)]"; description }
        }

    let private productVisual =
        div {
            _class "overflow-hidden rounded-xl border border-[var(--fve-border)] bg-[var(--fve-surface)] shadow-sm"
            img {
                _src "/examples/assets/images/geldos-overview.png"
                _alt "Geldos showing account balances, a financial forecast and recent transactions"
                _width "1440"; _height "960"; _class "h-auto w-full"
            }
        }

    let private featureStory =
        section {
            _ariaLabel "Your financial past, present and future"
            _class "mx-auto grid max-w-5xl gap-10 px-4 py-14 sm:px-6 sm:py-20 md:grid-cols-3 md:gap-8"
            for heading, description, items in
                [ "Past", "Keep your financial data in one place and understand how you got here.", ["Explore historical balances, income and spending"; "Trace account balances to their transactions"; "Keep personal and business books separate"]
                  "Present", "See where you stand across banking, investments and your books.", ["Track assets, liabilities, income and expenses"; "Review transactions that need attention"; "Create reports from the same financial record"]
                  "Future", "Understand how today's decisions could change what comes next.", ["Project account balances and cash flows"; "Compare expected and alternative scenarios"; "Plan with the full financial picture"] ] do
                div {
                    h2 { _class "font-serif text-3xl font-medium tracking-tight"; heading }
                    p { _class "mt-4 text-base leading-7 text-[var(--fve-muted-text)]"; description }
                    ul { _class "mt-5 list-disc space-y-3 pl-5 text-base leading-7"; for item in items do li { item } }
                }
        }

    let private questions =
        section {
            _id "questions"; _ariaLabel "Frequently asked questions"
            _class "border-y border-[var(--fve-border)] bg-[var(--fve-surface)]"
            div {
                _class "mx-auto grid max-w-5xl gap-8 px-4 py-14 sm:px-6 sm:py-20 md:grid-cols-[1fr_1.5fr]"
                h2 { _class "font-serif text-2xl font-medium tracking-tight min-[24rem]:text-3xl sm:text-4xl"; "Frequently asked questions" }
                div {
                    for question, answer in
                        [ "Is Geldos for personal or business finances?", "Both. Separate ledgers keep each set of books distinct, with banking, accounting, planning and reporting in one system."
                          "Does Geldos move money or make trades?", "No. Geldos helps you understand banking activity, investments and financial records. It doesn't execute transfers or trades or hold funds."
                          "Can I run it on my own infrastructure?", "Yes. Geldos is open-source software under Apache-2.0. Self-hosting gives you control of your deployment, data and upgrades."
                          "What's included in Geldos Cloud?", "Managed hosting, sign-in, upgrades and automated file and institution synchronization. The first month is $1, then $149 per month with five active users included. Additional active users are $10 per month, plus applicable taxes."
                          "Can I use the API?", "Yes. Use the API to work with your financial resources and connect Geldos to your own tools." ] do
                        fveDisclosure { _label question; _name "geldos-faq"; p { answer } }
                }
            }
        }

    let private finalCta =
        section {
            _class "mx-auto max-w-5xl px-4 py-14 sm:px-6"
            div {
                _class "rounded-xl bg-[var(--fve-brand-subtle)] px-5 py-10 sm:px-10"
                h2 { _class "max-w-2xl font-serif text-3xl font-medium tracking-tight sm:text-4xl"; "Put your finances in one system." }
                p { _class "mt-4 max-w-2xl text-base leading-7 text-[var(--fve-muted-text)]"; "Banking, investing, accounting, planning and reporting. All integrated. All in one place." }
                div { _class "mt-7"; cta signup "Sign up" }
            }
        }

    let private pricing =
        section {
            _ariaLabel "Hosting options"
            _class "bg-[var(--fve-brand-subtle)]"
            div {
                _class "mx-auto max-w-5xl px-4 py-14 sm:px-6 sm:py-20"
                h2 { _class "max-w-2xl font-serif text-3xl font-medium tracking-tight sm:text-4xl"; "Run Geldos your way." }
                p { _class "mt-4 max-w-2xl text-base leading-7 text-[var(--fve-muted-text)]"; "Use Geldos Cloud or run the open-source software on your own infrastructure." }
                div {
                    _class "mt-8 grid gap-px overflow-hidden rounded-xl border border-[var(--fve-border)] bg-[var(--fve-border)] md:grid-cols-2"
                    for name, price, period, description, features, href, action in
                        [ "Geldos Cloud", "$1", "first month", "We manage the infrastructure, so you can focus on your finances.", ["Then $149/month plus applicable taxes"; "Five active users included"; "$10/month per additional active user"; "Automated file and institution synchronization"], signup, "Sign up"
                          "Self-hosted", "$0", "software subscription", "Run Geldos with your own infrastructure and access controls.", ["Apache-2.0 licensed software"; "Your infrastructure and operating costs"; "Manual and API workflows"; "Control your data and upgrades"], source, "View source" ] do
                        article {
                            _class "flex min-w-0 flex-col bg-[var(--fve-background)] p-5 sm:p-8"
                            h3 { _class "text-xl font-semibold"; name }
                            p { _class "mt-5 flex flex-wrap items-baseline gap-2"; span { _class "text-4xl font-semibold tracking-tight"; price }; span { _class "text-base text-[var(--fve-muted-text)]"; period } }
                            p { _class "mt-5 text-base leading-7 text-[var(--fve-muted-text)]"; description }
                            ul { _class "my-7 list-disc space-y-3 pl-5 text-base leading-7"; for feature in features do li { feature } }
                            div {
                                _class "mt-auto pt-2"
                                if href = signup then cta href action
                                else fveLink { _href href; _variant LinkVariant.Outline; _size Size.ExtraLarge; _style "--fve-control-min-height:2.75rem;--fve-control-font-size:1rem"; action }
                            }
                        }
                }
            }
        }

    let render (path:string) (actions:HtmlElement list) =
        let placeholder = path = signup || path = login
        div {
            _data("example-marketing", "geldos")
            _class (ComponentsTheme.className Layout.theme + " min-h-dvh bg-[var(--fve-background)] text-[var(--fve-text)] [overflow-wrap:anywhere]")
            a { _href "#main-content"; _class "fixed left-4 top-4 z-50 -translate-y-[200%] rounded-md bg-[var(--fve-surface)] p-3 focus:translate-y-0"; "Skip to content" }
            header path actions
            main {
                _id "main-content"; _tabindex -1; _class "min-w-0"
                if placeholder then
                    section {
                        _class "mx-auto min-h-[50dvh] max-w-5xl px-4 py-14 sm:px-6 sm:py-20"
                        intro (if path = signup then "Sign up for Geldos" else "Log in to Geldos") "Your financial operating system."
                        p { _class "mt-8 max-w-xl text-base leading-7 text-[var(--fve-muted-text)]"; "This example doesn't create accounts or sign you in." }
                        div { _class "mt-6 flex flex-wrap gap-3"; link home "Back to Geldos"; link (if path = signup then login else signup) (if path = signup then "Log in" else "Sign up") }
                    }
                else
                    section {
                        _class "mx-auto max-w-5xl px-4 pb-10 pt-14 sm:px-6 sm:pb-14 sm:pt-20"
                        if path.EndsWith("/pricing") then
                            intro "Hosted for you, or hosted by you." "The same financial operating system, with a deployment that fits how you work."
                        elif path.EndsWith("/features") then
                            intro "One record. A clearer picture." "Understand your financial past, see where you stand today and plan what comes next."
                        else
                            intro "Your financial operating system" "Banking, investing, accounting, planning and reporting. All integrated. All in one place."
                            div { _class "mt-7"; cta signup "Sign up" }
                            div { _class "mt-12 rounded-xl bg-[var(--fve-brand-subtle)] p-3 sm:p-5"; productVisual }
                    }
                    if not (path.EndsWith("/pricing")) then featureStory
                    pricing
                    questions
                    finalCta
            }
            footer
        }
