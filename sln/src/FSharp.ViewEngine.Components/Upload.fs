namespace FSharp.ViewEngine.Components.Application

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open type Html

[<RequireQualifiedAccess>]
type UploadState = Queued | Uploading of value:int * maximum:int | Complete | Failed of message:string | Cancelled

[<NoEquality; NoComparison>]
type UploadItem = private { id:string; name:string; detail:string option; state:UploadState; actions:HtmlElement option }

[<RequireQualifiedAccess>]
module UploadItem =
    let create id name state =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable upload item ID is required."
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "An upload file name is required."
        { id = id; name = name; detail = None; state = state; actions = None }
    let withDetail detail (item:UploadItem) = { item with detail = Some detail }
    let withActions actions (item:UploadItem) = { item with actions = Some actions }

[<NoEquality; NoComparison>]
type UploadListConfig = private { label:string; items:UploadItem list }

[<RequireQualifiedAccess>]
module UploadList =
    let create label items =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An upload-list label is required."
        { label = label; items = items }
    let render config =
        section {
            _ariaLabel config.label
            _class "grid gap-3"
            h2 {
                _class "text-base font-semibold text-[var(--fve-text)]"
                config.label
            }
            if List.isEmpty config.items then
                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "No files queued."
                }
            else
                ul {
                    _class "divide-y divide-[var(--fve-border)] rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] ring-1 ring-[var(--fve-border)]"
                    for item in config.items do
                        li {
                            _id item.id
                            _class "grid gap-2 p-3 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center"
                            div {
                                _class "min-w-0"
                                p {
                                    _class "truncate text-sm font-semibold text-[var(--fve-text)]"
                                    item.name
                                }
                                match item.detail with
                                | Some detail ->
                                    p {
                                        _class "text-sm text-[var(--fve-muted-text)]"
                                        detail
                                    }
                                | None -> ()
                                match item.state with
                                | UploadState.Uploading (value, maximum) ->
                                    Progress.create ("Uploading " + item.name) value maximum
                                    |> Progress.withValueText (string (value * 100 / maximum) + "%")
                                    |> Progress.render
                                | UploadState.Queued ->
                                    p {
                                        _class "text-sm text-[var(--fve-muted-text)]"
                                        "Queued"
                                    }
                                | UploadState.Complete ->
                                    p {
                                        _role "status"
                                        _class "text-sm text-[var(--fve-positive-text)]"
                                        "Uploaded"
                                    }
                                | UploadState.Failed message ->
                                    p {
                                        _role "alert"
                                        _class "text-sm text-[var(--fve-critical-text)]"
                                        message
                                    }
                                | UploadState.Cancelled ->
                                    p {
                                        _class "text-sm text-[var(--fve-muted-text)]"
                                        "Cancelled"
                                    }
                            }
                            match item.actions with
                            | Some actions -> actions
                            | None -> ()
                        }
                }
        }
