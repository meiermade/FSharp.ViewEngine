module Example.Server.Product.Profile.Handler

open Microsoft.AspNetCore.Http
open Example.Server.Product
open Example.Components.Views.Model
open Ledger.Operations

let tryPage = function
    | ["profile"] -> Some ApplicationPage.Profile
    | ["profile"; "edit"] -> Some ApplicationPage.EditProfile
    | ["profile"; "organizations"] -> Some ApplicationPage.ProfileOrganizations
    | _ -> None

let submit page (form: IFormCollection) =
    match page with
    | ApplicationPage.EditProfile ->
        let request =
            { name = form["name"].ToString(); email = form["email"].ToString()
              timeZone = form["timezone"].ToString(); dailySummaryTime = form["dailySummaryTime"].ToString() }
        let valid = validateProfile request |> Result.isOk
        Some { Submission.result (if valid then "validated" else "invalid") with draft = if valid then None else Some(EditorDraft.Profile request) }
    | _ -> None
