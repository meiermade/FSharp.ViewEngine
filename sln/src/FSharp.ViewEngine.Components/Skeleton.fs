namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>skeleton</category>
[<RequireQualifiedAccess>]
type SkeletonShape =
    | Text
    | Rectangle
    | Circle

/// <category>skeleton</category>
[<NoEquality; NoComparison>]
type SkeletonConfig =
    private
        { shape:SkeletonShape
          width:string option
          height:string option
          attributes:HtmlAttribute list }

/// <category>skeleton</category>
[<NoEquality; NoComparison>]
type SkeletonRegionConfig =
    private
        { label:string
          content:HtmlElement
          attributes:HtmlAttribute list }

/// <category>skeleton</category>
[<RequireQualifiedAccess>]
module Skeleton =
    /// Creates one full-width, 1rem-high text-line placeholder, hidden from assistive technology.
    let create () =
        { shape = SkeletonShape.Text
          width = None
          height = None
          attributes = [] }

    let withShape shape (config:SkeletonConfig) = { config with shape = shape }

    let withSize width height (config:SkeletonConfig) =
        if String.IsNullOrWhiteSpace width then invalidArg (nameof width) "A CSS width is required."
        if String.IsNullOrWhiteSpace height then invalidArg (nameof height) "A CSS height is required."
        { config with width = Some width; height = Some height }

    let withAttributes attributes (config:SkeletonConfig) = { config with attributes = attributes }

    let render (config:SkeletonConfig) =
        let shapeClasses, defaultWidth, defaultHeight =
            match config.shape with
            | SkeletonShape.Text -> "rounded-full", "100%", "1rem"
            | SkeletonShape.Rectangle -> "rounded-[var(--fve-radius-panel)]", "100%", "6rem"
            | SkeletonShape.Circle -> "shrink-0 rounded-full", "2.5rem", "2.5rem"
        let style =
            [ "width: " + (config.width |> Option.defaultValue defaultWidth)
              "height: " + (config.height |> Option.defaultValue defaultHeight) ]
            |> String.concat "; "
        span {
            _ariaHidden true
            _style style
            _class ("block max-w-full animate-pulse bg-[var(--fve-surface-subtle)] motion-reduce:animate-none " + shapeClasses)
            for attribute in ComponentHtml.safeAttributes [ "class"; "style"; "aria-hidden" ] config.attributes do attribute
        }

/// <category>skeleton</category>
[<RequireQualifiedAccess>]
module SkeletonRegion =
    /// Creates one full-width busy status owner around consumer-authored placeholder layout.
    /// Keep content presentational; each Skeleton remains hidden from assistive technology.
    let create label content =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A loading label is required."
        { label = label; content = content; attributes = [] }

    let withAttributes attributes (config:SkeletonRegionConfig) = { config with attributes = attributes }

    let render (config:SkeletonRegionConfig) =
        div {
            _role "status"
            _ariaBusy true
            _ariaLive "polite"
            _class "w-full min-w-0"
            for attribute in ComponentHtml.safeAttributes [ "class"; "role"; "aria-busy"; "aria-live" ] config.attributes do attribute
            span { _class "sr-only"; config.label }
            config.content
        }
