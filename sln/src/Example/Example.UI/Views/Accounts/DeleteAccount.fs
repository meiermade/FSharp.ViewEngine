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
open Controls
open Overlay

module internal DeleteAccount =
    let render query id =
        deletionConfirmation
            query
            (ApplicationPage.DeleteAccount id)
            (findAccount query.records id).name
            (text "")
            ((findAccount query.records id).canDelete)
