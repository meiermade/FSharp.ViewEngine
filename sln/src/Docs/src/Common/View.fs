namespace Docs.Common

open System
open FSharp.ViewEngine
open type Alpine
open type Html

module View =
    let private menuIcon =
        raw """<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="h-6 w-6"><path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5"/></svg>"""

    let private xMarkIcon =
        raw """<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="h-6 w-6"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12"/></svg>"""

    let private githubIcon =
        raw """<svg aria-hidden="true" viewBox="0 0 16 16" class="h-6 w-6 fill-slate-400 group-hover:fill-slate-500 dark:group-hover:fill-slate-300"><path d="M8 0C3.58 0 0 3.58 0 8C0 11.54 2.29 14.53 5.47 15.59C5.87 15.66 6.02 15.42 6.02 15.21C6.02 15.02 6.01 14.39 6.01 13.72C4 14.09 3.48 13.23 3.32 12.78C3.23 12.55 2.84 11.84 2.5 11.65C2.22 11.5 1.82 11.13 2.49 11.12C3.12 11.11 3.57 11.7 3.72 11.94C4.44 13.15 5.59 12.81 6.05 12.6C6.12 12.08 6.33 11.73 6.56 11.53C4.78 11.33 2.92 10.64 2.92 7.58C2.92 6.71 3.23 5.99 3.74 5.43C3.66 5.23 3.38 4.41 3.82 3.31C3.82 3.31 4.49 3.1 6.02 4.13C6.66 3.95 7.34 3.86 8.02 3.86C8.7 3.86 9.38 3.95 10.02 4.13C11.55 3.09 12.22 3.31 12.22 3.31C12.66 4.41 12.38 5.23 12.3 5.43C12.81 5.99 13.12 6.7 13.12 7.58C13.12 10.65 11.25 11.33 9.47 11.53C9.76 11.78 10.01 12.26 10.01 13.01C10.01 14.08 10 14.94 10 15.21C10 15.42 10.15 15.67 10.55 15.59C13.71 14.53 16 11.53 16 8C16 3.58 12.42 0 8 0Z"></path></svg>"""

    let private sunIcon =
        raw """<svg class="h-5 w-5 text-sky-500 dark:hidden" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path d="M10 2a.75.75 0 0 1 .75.75v1.5a.75.75 0 0 1-1.5 0v-1.5A.75.75 0 0 1 10 2ZM10 15a.75.75 0 0 1 .75.75v1.5a.75.75 0 0 1-1.5 0v-1.5A.75.75 0 0 1 10 15ZM10 7a3 3 0 1 0 0 6 3 3 0 0 0 0-6ZM15.657 5.404a.75.75 0 1 0-1.06-1.06l-1.061 1.06a.75.75 0 0 0 1.06 1.06l1.06-1.06ZM6.464 14.596a.75.75 0 1 0-1.06-1.06l-1.06 1.06a.75.75 0 0 0 1.06 1.06l1.06-1.06ZM18 10a.75.75 0 0 1-.75.75h-1.5a.75.75 0 0 1 0-1.5h1.5A.75.75 0 0 1 18 10ZM5 10a.75.75 0 0 1-.75.75h-1.5a.75.75 0 0 1 0-1.5h1.5A.75.75 0 0 1 5 10ZM14.596 15.657a.75.75 0 0 0 1.06-1.06l-1.06-1.061a.75.75 0 1 0-1.06 1.06l1.06 1.06ZM5.404 6.464a.75.75 0 0 0 1.06-1.06l-1.06-1.06a.75.75 0 1 0-1.061 1.06l1.06 1.06Z"/></svg>"""

    let private moonIcon =
        raw """<svg class="hidden h-5 w-5 stroke-sky-500 dark:block" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M21.752 15.002A9.72 9.72 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 3 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 0 0 9.002-5.998Z"/></svg>"""

    let private sunIconSmall =
        raw """<svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M12 3v2.25m6.364.386-1.591 1.591M21 12h-2.25m-.386 6.364-1.591-1.591M12 18.75V21m-4.773-4.227-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z"/></svg>"""

    let private moonIconSmall =
        raw """<svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M21.752 15.002A9.72 9.72 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 3 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 0 0 9.002-5.998Z"/></svg>"""

    let private monitorIcon =
        raw """<svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M9 17.25v1.007a3 3 0 0 1-.879 2.122L7.5 21h9l-.621-.621A3 3 0 0 1 15 18.257V17.25m6-12V15a2.25 2.25 0 0 1-2.25 2.25H5.25A2.25 2.25 0 0 1 3 15V5.25m18 0A2.25 2.25 0 0 0 18.75 3H5.25A2.25 2.25 0 0 0 3 5.25m18 0V12a2.25 2.25 0 0 1-2.25 2.25H5.25A2.25 2.25 0 0 1 3 12V5.25"/></svg>"""

    let private pageHeader =
        header {
            _class [
                "sticky top-0 z-50 flex flex-none flex-wrap items-center justify-between"
                "bg-white/75 px-4 py-5 shadow-md shadow-slate-900/5 backdrop-blur transition duration-500"
                "sm:px-6 lg:px-8 dark:bg-slate-900/75 dark:shadow-none dark:backdrop-blur"
            ]
            div {
                _class "flex items-center gap-4"
                div {
                    _class "flex lg:hidden"
                    button {
                        _type "button"
                        _ariaLabel "Open navigation"
                        _xOn ("click", "mobileNavOpen = true")
                        _class "relative cursor-pointer rounded-lg p-1 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-700"
                        menuIcon
                    }
                }
                a {
                    _href "/"
                    _class "flex items-center gap-2 text-sm font-semibold tracking-wider text-slate-700 dark:text-white"
                    img { _src "/logo.svg"; _alt "FSharp.ViewEngine"; _class "h-6 w-6" }
                    "FSharp.ViewEngine"
                }
            }
            div {
                _class "relative flex basis-0 items-center justify-end gap-6 sm:gap-8 md:grow"
                div {
                    _class "relative z-10"
                    _xData "{ open: false, theme: localStorage.getItem('theme') || 'system' }"
                    _xInit """
                        $watch('theme', (val) => {
                            localStorage.setItem('theme', val);
                            if (val === 'dark' || (val === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
                                document.documentElement.classList.add('dark');
                            } else {
                                document.documentElement.classList.remove('dark');
                            }
                        });
                        if (theme === 'dark' || (theme === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
                            document.documentElement.classList.add('dark');
                        }
                    """
                    button {
                        _type "button"
                        _ariaLabel "Choose color theme"
                        _class "flex h-8 w-8 cursor-pointer items-center justify-center rounded-lg p-1 transition-colors hover:bg-slate-100 dark:hover:bg-slate-700"
                        _xOn ("click", "open = !open")
                        sunIcon
                        moonIcon
                    }
                    div {
                        _xShow "open"
                        _xCloak
                        _xOn ("click.away", "open = false")
                        _xTransition ()
                        _class [
                            "absolute right-0 top-full mt-3 w-36 overflow-hidden rounded-lg"
                            "bg-white py-1 text-sm font-semibold text-slate-700 shadow-lg ring-1"
                            "ring-slate-900/10 dark:bg-slate-800 dark:text-slate-300 dark:ring-0"
                        ]
                        for value, label, icon in [ "light", "Light", sunIconSmall; "dark", "Dark", moonIconSmall; "system", "System", monitorIcon ] do
                            button {
                                _type "button"
                                _class "flex w-full items-center gap-2 px-3 py-2 hover:bg-slate-100 dark:hover:bg-slate-700/50"
                                _xOn ("click", $"theme = '{value}'; open = false")
                                _xBind ("class", $"theme === '{value}' ? 'text-sky-500' : ''")
                                icon
                                label
                            }
                    }
                }
                a {
                    _href "https://github.com/meiermade/FSharp.ViewEngine"
                    _ariaLabel "FSharp.ViewEngine on GitHub"
                    _class "group"
                    githubIcon
                }
            }
        }

    let private navLink currentPath (page:DocPage) =
        let isActive = currentPath = page.path || List.contains currentPath page.aliases
        li {
            _class "relative"
            a {
                _class [
                    "block w-full pl-3.5 before:pointer-events-none before:absolute"
                    "before:-left-1 before:top-1/2 before:h-1.5 before:w-1.5"
                    "before:-translate-y-1/2 before:rounded-full"
                    if isActive then
                        "font-semibold text-sky-500 before:bg-sky-500"
                    else
                        "text-slate-500 before:hidden before:bg-slate-300 hover:text-slate-600"
                        + " hover:before:block dark:text-slate-400 dark:before:bg-slate-700 dark:hover:text-slate-300"
                ]
                _href page.path
                _xOn ("click", "mobileNavOpen = false")
                page.navLabel
            }
        }

    let private sidebarNavigation navigation currentPath =
        nav {
            _ariaLabel "Documentation"
            _class "text-base lg:text-sm"
            ul {
                _role "list"
                _class "space-y-9"
                for navSection in navigation do
                    li {
                        h2 {
                            _class "font-display font-medium text-slate-900 dark:text-white"
                            navSection.label
                        }
                        ul {
                            _role "list"
                            _class "mt-2 space-y-2 border-l-2 border-slate-100 lg:mt-4 lg:space-y-4 lg:border-slate-200 dark:border-slate-800"
                            for page in navSection.pages do
                                navLink currentPath page
                        }
                    }
            }
        }

    let private sidebar navigation currentPath =
        div {
            _class "hidden lg:relative lg:block lg:flex-none"
            div {
                _class "sticky top-[4.75rem] -ml-0.5 h-[calc(100vh-4.75rem)] w-64 overflow-y-auto py-16 pl-0.5 pr-8 xl:w-72 xl:pr-16"
                sidebarNavigation navigation currentPath
            }
        }

    let private tableOfContents page =
        let headings = DocPage.tableOfContents page
        if List.isEmpty headings then
            empty
        else
            nav {
                _ariaLabel "On this page"
                _class "sticky top-[4.75rem] -mr-6 w-56 flex-none overflow-y-auto py-16 pr-6"
                h2 {
                    _class "font-display text-sm font-medium text-zinc-900 dark:text-white"
                    "On this page"
                }
                ul {
                    _role "list"
                    _class "mt-4 space-y-3 text-sm"
                    for heading in headings do
                        li {
                            _class (if heading.level = 3 then "pl-3" else "")
                            a {
                                _href $"#{heading.id}"
                                _class "text-zinc-500 hover:text-zinc-600 dark:text-zinc-400 dark:hover:text-zinc-300"
                                heading.title
                            }
                        }
                }
            }

    let rec private renderInline content =
        match content with
        | Text value -> text value
        | Strong children -> strong { for child in children do renderInline child }
        | InlineContent.Code value -> code { value }
        | Link(label, href) -> a { _href href; label }

    let private renderNode node =
        match node with
        | Heading heading ->
            match heading.level with
            | 2 -> h2 { _id heading.id; heading.title }
            | 3 -> h3 { _id heading.id; heading.title }
            | _ -> h4 { _id heading.id; heading.title }
        | Paragraph children -> p { for child in children do renderInline child }
        | UnorderedList items -> ul { for item in items do li { for child in item do renderInline child } }
        | OrderedList items -> ol { for item in items do li { for child in item do renderInline child } }
        | BarChart chart ->
            figure {
                _ariaLabel chart.label
                _class "not-prose my-8 rounded-xl border border-slate-200 bg-slate-50/60 p-5 sm:p-6 dark:border-slate-700 dark:bg-slate-800/40"
                figcaption {
                    p {
                        _class "font-semibold text-slate-900 dark:text-white"
                        chart.title
                    }
                    p {
                        _class "mt-1 text-sm text-slate-600 dark:text-slate-300"
                        chart.description
                    }
                }
                div {
                    _class "mt-6 space-y-5"
                    for bar in chart.bars do
                        div {
                            div {
                                _class "mb-2 flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 text-sm"
                                span {
                                    _class [
                                        "font-medium"
                                        if bar.highlighted then "text-sky-600 dark:text-sky-400" else "text-slate-700 dark:text-slate-200"
                                    ]
                                    bar.label
                                }
                                span {
                                    _class "font-mono text-xs tabular-nums text-slate-600 dark:text-slate-300"
                                    $"{bar.duration} · {bar.comparison}"
                                }
                            }
                            div {
                                _ariaHidden true
                                _class "h-3 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-700"
                                div {
                                    _class [
                                        "h-full rounded-full"
                                        if bar.highlighted then "bg-sky-500" else "bg-slate-400 dark:bg-slate-500"
                                    ]
                                    _style ("width: " + string (Math.Clamp(bar.widthPercent, 0, 100)) + "%")
                                }
                            }
                        }
                }
            }
        | DataTable(headers, rows) ->
            div {
                _class "not-prose my-8 overflow-x-auto rounded-xl ring-1 ring-slate-200 dark:ring-slate-700"
                table {
                    _class "min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-700"
                    thead {
                        _class "bg-slate-50 dark:bg-slate-800/60"
                        tr {
                            for index, header in List.indexed headers do
                                th {
                                    _scope "col"
                                    _class [
                                        "whitespace-nowrap px-4 py-3 font-semibold text-slate-900 dark:text-white"
                                        if index = 0 then "text-left" else "text-right"
                                    ]
                                    header
                                }
                        }
                    }
                    tbody {
                        _class "divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-900"
                        for row in rows do
                            tr {
                                _class "hover:bg-slate-50/70 dark:hover:bg-slate-800/40"
                                for index, value in List.indexed row do
                                    td {
                                        _class [
                                            "whitespace-nowrap px-4 py-3"
                                            if index = 0 then
                                                "font-medium text-slate-700 dark:text-slate-200"
                                            else
                                                "text-right font-mono tabular-nums text-slate-600 dark:text-slate-300"
                                        ]
                                        value
                                    }
                            }
                    }
                }
            }
        | CodeBlock(language, source) ->
            let prismLanguage = if language = "fs" then "fsharp" else language
            pre {
                _class $"language-{prismLanguage}"
                code { _class $"language-{prismLanguage}"; source }
            }

    let document navigation page =
        let siteUrl = "https://fsharpviewengine.meiermade.com"
        let pageUrl = if page.path = "/" then siteUrl else siteUrl + page.path
        let socialDescription = "A minimal, fast view engine for F#. Documentation and examples for FSharp.ViewEngine."
        let socialImageUrl = siteUrl + "/android-chrome-512x512.png"

        html {
            _lang "en"
            _class "h-full antialiased"
            head {
                meta { _charset "utf-8" }
                meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
                title page.browserTitle
                link { _rel "canonical"; _href pageUrl }
                meta { _name "description"; _content socialDescription }
                meta { _property "og:type"; _content "website" }
                meta { _property "og:site_name"; _content "FSharp.ViewEngine" }
                meta { _property "og:title"; _content page.browserTitle }
                meta { _property "og:description"; _content socialDescription }
                meta { _property "og:url"; _content pageUrl }
                meta { _property "og:image"; _content socialImageUrl }
                meta { _property "og:image:alt"; _content "FSharp.ViewEngine logo" }
                meta { _name "twitter:card"; _content "summary_large_image" }
                meta { _name "twitter:title"; _content page.browserTitle }
                meta { _name "twitter:description"; _content socialDescription }
                meta { _name "twitter:image"; _content socialImageUrl }
                meta { _name "twitter:image:alt"; _content "FSharp.ViewEngine logo" }
                script { js "let t=localStorage.getItem('theme');if(t==='dark'||(!t||t==='system')&&window.matchMedia('(prefers-color-scheme: dark)').matches){document.documentElement.classList.add('dark')}" }
                link { _rel "stylesheet"; _href "/css/output.css" }
                script { _src "/scripts/alpinejs.3.15.12.min.js"; _defer true }
                script { _src "https://cdnjs.cloudflare.com/ajax/libs/prism/1.30.0/prism.min.js" }
                link { _rel "stylesheet"; _href "https://cdnjs.cloudflare.com/ajax/libs/prism/1.30.0/themes/prism-tomorrow.min.css" }
                script { _src "https://cdnjs.cloudflare.com/ajax/libs/prism/1.30.0/components/prism-fsharp.min.js" }
            }
            body {
                _class "min-h-full bg-white dark:bg-slate-900"
                _xData "{ mobileNavOpen: false }"
                _xOn ("keydown.escape.window", "mobileNavOpen = false")
                pageHeader
                div {
                    _xShow "mobileNavOpen"
                    _xCloak
                    _xTransition ()
                    _class "fixed inset-0 z-[70] lg:hidden"
                    div {
                        _class "absolute inset-0 bg-slate-950/60 backdrop-blur-sm"
                        _xOn ("click", "mobileNavOpen = false")
                    }
                    div {
                        _class "absolute inset-y-0 left-0 w-full max-w-xs overflow-y-auto bg-white px-6 py-5 shadow-2xl ring-1 ring-slate-900/10 dark:bg-slate-900 dark:ring-white/10"
                        div {
                            _class "mb-6 flex items-center justify-between"
                            a {
                                _href "/"
                                _class "flex items-center gap-2 text-sm font-semibold tracking-wider text-slate-700 dark:text-white"
                                _xOn ("click", "mobileNavOpen = false")
                                img { _src "/logo.svg"; _alt "FSharp.ViewEngine"; _class "h-6 w-6" }
                                "FSharp.ViewEngine"
                            }
                            button {
                                _type "button"
                                _ariaLabel "Close navigation"
                                _class "cursor-pointer rounded p-1 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-700"
                                _xOn ("click", "mobileNavOpen = false")
                                xMarkIcon
                            }
                        }
                        sidebarNavigation navigation page.path
                    }
                }
                div {
                    _id "app"
                    _class "relative mx-auto flex max-w-8xl justify-center sm:px-2 lg:px-8 xl:px-12"
                    sidebar navigation page.path
                    div {
                        _class "min-w-0 max-w-3xl flex-auto px-4 pt-6 pb-12 lg:max-w-none lg:pl-8 lg:pr-0 xl:px-16"
                        article {
                            div {
                                _class "mb-8"
                                p {
                                    _class "font-display text-sm font-medium text-sky-500"
                                    page.category
                                }
                            }
                            div {
                                _class "prose prose-slate max-w-none dark:prose-invert [&_h1]:scroll-mt-28 [&_h2]:scroll-mt-28 [&_h3]:scroll-mt-28"
                                h1 { page.title }
                                for node in page.nodes do
                                    renderNode node
                            }
                        }
                    }
                    div {
                        _class "hidden xl:sticky xl:top-[4.75rem] xl:-mr-6 xl:block xl:h-[calc(100vh-4.75rem)] xl:flex-none xl:overflow-y-auto xl:py-12 xl:pr-6"
                        tableOfContents page
                    }
                }
            }
        }
