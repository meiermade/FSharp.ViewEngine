namespace Example.Server.ApplicationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
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
