namespace Example.UI.Views

open System
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open type Html
open type Svg
open type Datastar
open Model
open ViewData

module internal UpdateAccount =
    let render query id =
        AccountEditor.accountEditor query (Some id)
