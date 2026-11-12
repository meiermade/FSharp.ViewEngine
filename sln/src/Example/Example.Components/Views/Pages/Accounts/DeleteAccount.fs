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
open Controls
open Overlay

module internal DeleteAccount =
    let render query id =
        deletionConfirmation
            query
            (ApplicationPage.DeleteAccount id)
            (findAccount id).name
            (text "")
            (canDeleteAccount query.records id)
