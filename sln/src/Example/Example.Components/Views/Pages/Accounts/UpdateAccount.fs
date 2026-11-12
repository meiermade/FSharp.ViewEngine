namespace Example.Components.Views.Pages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain

module internal UpdateAccount =
    let render query id =
        AccountEditor.accountEditor query (Some id)
