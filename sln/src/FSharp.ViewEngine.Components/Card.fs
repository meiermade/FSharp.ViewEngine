namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

/// <category>card</category>
[<RequireQualifiedAccess>]
type CardSize = Regular | Small

/// <category>card</category>
[<NoEquality; NoComparison>]
type CardConfig =
    private
        { content:HtmlElement
          header:HtmlElement option
          media:HtmlElement option
          footer:HtmlElement option
          size:CardSize
          attributes:HtmlAttribute list }

/// <summary>A presentational surface. Content, headings, images and actions remain ordinary HTML.</summary>
/// <category>card</category>
[<RequireQualifiedAccess>]
module Card =
    let create content =
        { content=content; header=None; media=None; footer=None; size=CardSize.Regular; attributes=[] }
    let withHeader header (config:CardConfig) = { config with header=Some header }
    let withMedia media (config:CardConfig) = { config with media=Some media }
    let withFooter footer (config:CardConfig) = { config with footer=Some footer }
    let withSize size (config:CardConfig) = { config with size=size }
    let withAttributes attributes (config:CardConfig) = { config with attributes=attributes }
    let render config =
        let inset = if config.size=CardSize.Small then "p-3" else "p-5"
        div {
            _attr("data-fve-card", "true")
            _class "min-w-0 overflow-hidden rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[var(--fve-surface)] text-[var(--fve-text)]"
            for attribute in ComponentHtml.safeAttributes ["class"; "data-fve-card"] config.attributes do attribute
            match config.media with Some media -> div { _class "min-w-0"; media } | None -> ()
            match config.header with Some header -> div { _class inset; header } | None -> ()
            div { _class inset; config.content }
            match config.footer with
            | Some footer -> div { _class ("flex min-w-0 flex-wrap items-center gap-2 border-t border-[var(--fve-border)] " + inset); footer }
            | None -> ()
        }
