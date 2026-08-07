namespace Docs.Pages

open Docs.Common

module Registry =
    let navigation =
        [ { label = "Getting started"
            pages = [ Home.page; Installation.page; Custom.page; Usage.page ] }
          { label = "Extensions"
            pages = [ Svg.page; Datastar.page; Htmx.page; Alpine.page; TailwindElements.page ] }
          { label = "Project"
            pages = [ Benchmarks.page; Changelog.page ] } ]

    let all = navigation |> List.collect _.pages

    let aliases =
        all
        |> List.collect (fun page -> page.aliases |> List.map (fun alias -> alias, page.path))
