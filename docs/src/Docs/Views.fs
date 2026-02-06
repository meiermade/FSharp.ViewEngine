module Docs.Views

open FSharp.ViewEngine
open type Html
open type Alpine
open type Tailwind

type Page =
    { title:string }

let magnifyingGlassIcon = raw """
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" fill="currentColor" class="size-4">
      <path fill-rule="evenodd" d="M9.965 11.026a5 5 0 1 1 1.06-1.06l2.755 2.754a.75.75 0 1 1-1.06 1.06l-2.755-2.754ZM10.5 7a3.5 3.5 0 1 1-7 0 3.5 3.5 0 0 1 7 0Z" clip-rule="evenodd" />
    </svg>
    """
let menuIcon = raw """
    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="h-6 w-6">
      <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5"/>
    </svg>
    """
let githubIcon = raw """<svg aria-hidden="true" viewBox="0 0 16 16" class="h-6 w-6 fill-slate-400 group-hover:fill-slate-500 dark:group-hover:fill-slate-300"><path d="M8 0C3.58 0 0 3.58 0 8C0 11.54 2.29 14.53 5.47 15.59C5.87 15.66 6.02 15.42 6.02 15.21C6.02 15.02 6.01 14.39 6.01 13.72C4 14.09 3.48 13.23 3.32 12.78C3.23 12.55 2.84 11.84 2.5 11.65C2.22 11.5 1.82 11.13 2.49 11.12C3.12 11.11 3.57 11.7 3.72 11.94C4.44 13.15 5.59 12.81 6.05 12.6C6.12 12.08 6.33 11.73 6.56 11.53C4.78 11.33 2.92 10.64 2.92 7.58C2.92 6.71 3.23 5.99 3.74 5.43C3.66 5.23 3.38 4.41 3.82 3.31C3.82 3.31 4.49 3.1 6.02 4.13C6.66 3.95 7.34 3.86 8.02 3.86C8.7 3.86 9.38 3.95 10.02 4.13C11.55 3.09 12.22 3.31 12.22 3.31C12.66 4.41 12.38 5.23 12.3 5.43C12.81 5.99 13.12 6.7 13.12 7.58C13.12 10.65 11.25 11.33 9.47 11.53C9.76 11.78 10.01 12.26 10.01 13.01C10.01 14.08 10 14.94 10 15.21C10 15.42 10.15 15.67 10.55 15.59C13.71 14.53 16 11.53 16 8C16 3.58 12.42 0 8 0Z"></path></svg>"""
let sunIcon = raw """<svg class="h-5 w-5 dark:hidden stroke-sky-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M12 3v2.25m6.364.386-1.591 1.591M21 12h-2.25m-.386 6.364-1.591-1.591M12 18.75V21m-4.773-4.227-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z"/></svg>"""
let moonIcon = raw """<svg class="hidden h-5 w-5 dark:block stroke-sky-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M21.752 15.002A9.72 9.72 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 3 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 0 0 9.002-5.998Z"/></svg>"""
let sunIconSmall = raw """<svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M12 3v2.25m6.364.386-1.591 1.591M21 12h-2.25m-.386 6.364-1.591-1.591M12 18.75V21m-4.773-4.227-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z"/></svg>"""
let moonIconSmall = raw """<svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M21.752 15.002A9.72 9.72 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 3 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 0 0 9.002-5.998Z"/></svg>"""
let monitorIcon = raw """<svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M9 17.25v1.007a3 3 0 0 1-.879 2.122L7.5 21h9l-.621-.621A3 3 0 0 1 15 18.257V17.25m6-12V15a2.25 2.25 0 0 1-2.25 2.25H5.25A2.25 2.25 0 0 1 3 15V5.25m18 0A2.25 2.25 0 0 0 18.75 3H5.25A2.25 2.25 0 0 0 3 5.25m18 0V12a2.25 2.25 0 0 1-2.25 2.25H5.25A2.25 2.25 0 0 1 3 12V5.25"/></svg>"""

let private pageHeader =
    header {
        _class [
            "sticky top-0 z-50 flex flex-none flex-wrap items-center justify-between"
            "bg-white px-4 py-5 shadow-md shadow-slate-900/5 transition duration-500"
            "sm:px-6 lg:px-8 dark:shadow-none dark:bg-slate-900/75 dark:backdrop-blur"
        ]
        // Left section: hamburger + logo
        div {
            _class "flex items-center gap-4"
            // Mobile menu button (hidden on desktop)
            div {
                _class "flex lg:hidden"
                button {
                    _type "button"
                    _class "relative text-slate-500 hover:text-slate-600 dark:text-slate-400 dark:hover:text-slate-300"
                    menuIcon
                }
            }
            // Logo
            a {
                _href "/"
                _class "flex items-center gap-2 text-sm font-semibold tracking-wider text-slate-700 dark:text-white"
                img { _src "/logo.png"; _alt "FSharp.ViewEngine"; _class "h-6 w-6" }
                "FSharp.ViewEngine"
            }
        }
        // Right section with theme toggle and GitHub
        div {
            _class "relative flex basis-0 justify-end gap-6 sm:gap-8 md:grow"
            // Theme toggle dropdown
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
                    _class [
                        "flex h-6 w-6 items-center justify-center rounded-lg shadow-md ring-1"
                        "shadow-black/5 ring-black/5 dark:bg-slate-700 dark:ring-white/5"
                        "dark:ring-inset"
                    ]
                    _xOn ("click", "open = !open")
                    // Light mode icon (sun)
                    sunIcon
                    moonIcon
                }
                // Dropdown menu
                div {
                    _xShow "open"
                    _xOn ("click.away", "open = false")
                    _xTransition ()
                    _class [
                        "absolute right-0 top-full mt-3 w-36 overflow-hidden rounded-lg"
                        "bg-white py-1 text-sm font-semibold text-slate-700 shadow-lg ring-1"
                        "ring-slate-900/10 dark:bg-slate-800 dark:text-slate-300 dark:ring-0"
                        "dark:highlight-white/5"
                    ]
                    // Light option
                    button {
                        _type "button"
                        _class "flex w-full items-center gap-2 px-3 py-2 hover:bg-slate-100 dark:hover:bg-slate-700/50"
                        _xOn ("click", "theme = 'light'; open = false")
                        _xBind ("class", "theme === 'light' ? 'text-sky-500' : ''")
                        sunIconSmall
                        text "Light"
                    }
                    // Dark option
                    button {
                        _type "button"
                        _class "flex w-full items-center gap-2 px-3 py-2 hover:bg-slate-100 dark:hover:bg-slate-700/50"
                        _xOn ("click", "theme = 'dark'; open = false")
                        _xBind ("class", "theme === 'dark' ? 'text-sky-500' : ''")
                        moonIconSmall
                        text "Dark"
                    }
                    // System option
                    button {
                        _type "button"
                        _class "flex w-full items-center gap-2 px-3 py-2 hover:bg-slate-100 dark:hover:bg-slate-700/50"
                        _xOn ("click", "theme = 'system'; open = false")
                        _xBind ("class", "theme === 'system' ? 'text-sky-500' : ''")
                        monitorIcon
                        text "System"
                    }
                }
            }
            // GitHub link
            a {
                _href "https://github.com/meiermade/FSharp.ViewEngine"
                _class "group"
                githubIcon
            }
        }
    }

let private navLink (currentPath: string) (href': string) (label': string) =
    let isActive = currentPath = href'
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
            _href href'
            label'
        }
    }

let private sidebarNavigation (currentPath: string) =
    nav {
        _class "text-base lg:text-sm"
        ul {
            _role "list"
            _class "space-y-9"
            li {
                h2 {
                    _class "font-display font-medium text-slate-900 dark:text-white"
                    "Getting started"
                }
                ul {
                    _role "list"
                    _class [
                        "mt-2 space-y-2 border-l-2 border-slate-100"
                        "lg:mt-4 lg:space-y-4 lg:border-slate-200 dark:border-slate-800"
                    ]
                    navLink currentPath "/" "Introduction"
                    navLink currentPath "/installation" "Installation"
                    navLink currentPath "/quickstart" "Quickstart"
                }
            }
            li {
                h2 {
                    _class "font-display font-medium text-slate-900 dark:text-white"
                    "Extensions"
                }
                ul {
                    _role "list"
                    _class [
                        "mt-2 space-y-2 border-l-2 border-slate-100"
                        "lg:mt-4 lg:space-y-4 lg:border-slate-200 dark:border-slate-800"
                    ]
                    navLink currentPath "/extensions/alpine" "Alpine"
                    navLink currentPath "/extensions/datastar" "Datastar"
                    navLink currentPath "/extensions/htmx" "HTMX"
                    navLink currentPath "/extensions/svg" "SVG"
                    navLink currentPath "/extensions/tailwind" "Tailwind"
                }
            }
        }
    }

let private sidebar (currentPath: string) =
    div {
        _class "hidden lg:relative lg:block lg:flex-none"
        div {
            _class [
                "sticky top-[4.75rem] -ml-0.5 h-[calc(100vh-4.75rem)] w-64"
                "overflow-y-auto overflow-x-hidden py-16 pl-0.5 pr-8 xl:w-72 xl:pr-16"
            ]
            sidebarNavigation currentPath
        }
    }

let private tableOfContents (headings: (string * string) list) =
    if List.isEmpty headings then
        empty
    else
        nav {
            _class "sticky top-[4.75rem] -mr-6 w-56 flex-none overflow-y-auto py-16 pr-6"
            h2 {
                _class "font-display text-sm font-medium text-zinc-900 dark:text-white"
                "On this page"
            }
            ul {
                _role "list"
                _class "mt-4 space-y-3 text-sm"
                for (title', anchor) in headings do
                    li {
                        a {
                            _href $"#{anchor}"
                            _class "text-zinc-500 hover:text-zinc-600 dark:text-zinc-400 dark:hover:text-zinc-300"
                            title'
                        }
                    }
            }
        }

let layout (pageTitle: string) (currentPath: string) (headings: (string * string) list) (content: string) =
    html {
        _lang "en"
        _class "h-full antialiased"
        head {
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            title pageTitle
            script { js "let t=localStorage.getItem('theme');if(t==='dark'||(!t||t==='system')&&window.matchMedia('(prefers-color-scheme: dark)').matches){document.documentElement.classList.add('dark')}" }
            link { _rel "stylesheet"; _href "/css/output.css" }
            script { _src "https://unpkg.com/alpinejs@3.x.x/dist/cdn.min.js"; _defer true }
            script { _src "https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/prism.min.js" }
            link { _rel "stylesheet"; _href "https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism-tomorrow.min.css" }
            script { _src "https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-fsharp.min.js" }
        }
        body {
            _class "min-h-full bg-white dark:bg-slate-900"
            pageHeader
            div {
                _class [
                    "relative mx-auto flex max-w-8xl justify-center"
                    "sm:px-2 lg:px-8 xl:px-12"
                ]
                sidebar currentPath
                div {
                    _class [
                        "min-w-0 max-w-3xl flex-auto px-4 py-16"
                        "lg:max-w-none lg:pl-8 lg:pr-0 xl:px-16"
                    ]
                    article {
                        div {
                            _class "mb-8"
                            p {
                                _class "font-display text-sm font-medium text-sky-500"
                                if currentPath.StartsWith("/extensions") then
                                    "Extensions"
                                else
                                    "Getting started"
                            }
                        }
                        div {
                            _class "prose prose-slate dark:prose-invert max-w-none"
                            raw content
                        }
                    }
                }
                div {
                    _class [
                        "hidden xl:sticky xl:top-[4.75rem] xl:-mr-6 xl:block"
                        "xl:h-[calc(100vh-4.75rem)] xl:flex-none xl:overflow-y-auto"
                        "xl:py-16 xl:pr-6"
                    ]
                    tableOfContents headings
                }
            }
        }
    }
