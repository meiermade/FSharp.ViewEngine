namespace Docs.ComponentPages.Drawer

open System
open System.IO
open System.Text.Json
open Docs.Common
open Docs.Web
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar.DependencyInjection

open Docs.Common.Handler

module Handler =

    let private componentDrawerPatch: HttpHandler =
        let html = Docs.ComponentPages.Drawer.View.patchedItemDrawerContent |> Render.toString

        setHttpHeader "Content-Type" "text/html; charset=utf-8"
        >=> setBodyFromString html

    let get : HttpHandler =
        choose [
            route "/components/drawers/item" >=> componentDrawerPatch
        ]
