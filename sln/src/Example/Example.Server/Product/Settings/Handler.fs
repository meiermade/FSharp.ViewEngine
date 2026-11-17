module Example.Server.Product.Settings.Handler

open Microsoft.AspNetCore.Http
open Example.Server.Product
open Example.Components.Views.Model
open Ledger.Domain
open Ledger.Operations

let tryPage = function
    | ["settings"] -> Some ApplicationPage.Settings
    | ["settings"; "general"; "edit"] -> Some ApplicationPage.EditOrganization
    | ["settings"; "environments"; id] when tryEnvironment id |> Option.isSome -> Some(ApplicationPage.Environment id)
    | ["settings"; "environments"; id; "edit"] when tryEnvironment id |> Option.isSome -> Some(ApplicationPage.EditEnvironment id)
    | ["settings"; key] when settingsSections |> List.exists (fst >> (=) key) -> Some(ApplicationPage.SettingsSection key)
    | _ -> None

let submit page (form: IFormCollection) =
    match page with
    | ApplicationPage.EditOrganization ->
        let request = { name = form["workspace"].ToString(); currency = form["currency"].ToString() }
        let valid = validateOrganization request |> Result.isOk
        Some { Submission.result (if valid then "validated" else "invalid") with draft = if valid then None else Some(EditorDraft.Organization request) }
    | ApplicationPage.EditEnvironment id ->
        let request = { environmentId = id; name = form["name"].ToString() }
        let valid = validateEnvironment request |> Result.isOk
        Some { Submission.result (if valid then "validated" else "invalid") with draft = if valid then None else Some(EditorDraft.Environment request) }
    | _ -> None
