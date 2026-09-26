namespace FSharp.ViewEngine.Components.Documentation

open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type DocsVersion =
    { label:string
      href:string }

[<RequireQualifiedAccess>]
module DocsVersion =
    let create label href : DocsVersion = { label = label; href = href }

module VersionView =
    let selector current versions =
        nav {
            _ariaLabel "Documentation version"
            _class "flex flex-wrap gap-1.5 [&>a]:rounded-md [&>a]:border [&>a]:border-[var(--fve-border)] [&>a]:px-2 [&>a]:py-1 [&>a]:text-sm [&>a]:text-[var(--fve-muted-text)] [&>a]:no-underline [&>a[aria-current=page]]:bg-[var(--fve-brand-subtle)] [&>a[aria-current=page]]:font-bold [&>a[aria-current=page]]:text-[var(--fve-brand-text)]"
            for version in versions do
                a {
                    _href version.href
                    if version.label = current then _ariaCurrent "page"
                    version.label
                }
        }
