namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar
[<NoEquality; NoComparison>]
type NavigationItem<'destination> =
    private
        { label:string
          destination:'destination }

[<NoEquality; NoComparison>]
type AppShellConfig<'destination when 'destination:equality> =
    private
        { productName:string
          current:'destination
          navigation:NavigationItem<'destination> list
          content:HtmlElement
          breadcrumbs:NavigationItem<'destination> list
          accountMenu:HtmlElement option
          theme:ComponentsTheme }

[<RequireQualifiedAccess>]
module NavigationItem =
    let create destination label = { label = label; destination = destination }

[<RequireQualifiedAccess>]
module AppShell =
    let create productName current navigation content =
        if String.IsNullOrWhiteSpace productName then invalidArg (nameof productName) "A product name is required."
        { productName = productName
          current = current
          navigation = navigation
          content = content
          breadcrumbs = []
          accountMenu = None
          theme = ComponentsTheme.sky }

    let withBreadcrumbs breadcrumbs (config:AppShellConfig<'destination>) = { config with breadcrumbs = breadcrumbs }
    let withAccountMenu accountMenu (config:AppShellConfig<'destination>) = { config with accountMenu = Some accountMenu }
    let withTheme theme (config:AppShellConfig<'destination>) = { config with theme = theme }

    let render resolve config =
        div {
            _class (ComponentHtml.classes [ ComponentsTheme.className config.theme; "grid min-h-[36rem] bg-[var(--fve-page)] text-[var(--fve-text)] lg:grid-cols-[16rem_1fr]" ])
            aside {
                _class "hidden border-r border-[var(--fve-border)] bg-[var(--fve-surface)] p-4 lg:block"
                strong { _class "block px-3 py-[var(--fve-control-padding-block)] text-base"; config.productName }
                nav {
                    _ariaLabel "Primary"
                    _class "mt-5 grid gap-1"
                    for item in config.navigation do
                        a {
                            _href (resolve item.destination)
                            if item.destination = config.current then _ariaCurrent "page"
                            _class (
                                if item.destination = config.current then
                                    "rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-subtle)] px-3 py-[var(--fve-control-padding-block)] text-sm font-semibold text-[var(--fve-brand-text)]"
                                else
                                    "rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-sm text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)]")
                            item.label
                        }
                }
            }
            div {
                _class "min-w-0"
                header {
                    _class "flex min-h-16 items-center justify-between border-b border-[var(--fve-border)] bg-[var(--fve-surface)] px-4 sm:px-6"
                    nav {
                        _ariaLabel "Breadcrumb"
                        _class "flex items-center gap-2 text-sm text-[var(--fve-muted-text)]"
                        for breadcrumb in config.breadcrumbs do
                            a { _href (resolve breadcrumb.destination); _class "hover:text-[var(--fve-text)]"; breadcrumb.label }
                    }
                    config.accountMenu |> Option.defaultValue empty
                }
                main { _class "mx-auto max-w-7xl p-4 sm:p-6 lg:p-8"; config.content }
            }
        }
