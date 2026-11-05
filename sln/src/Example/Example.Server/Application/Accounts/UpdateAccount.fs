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

module internal UpdateAccount =
    let render query id =
        AccountEditor.accountEditor query (Some id)
