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
            _class "docs-version-selector"
            for version in versions do
                a {
                    _href version.href
                    if version.label = current then _ariaCurrent "page"
                    version.label
                }
        }
