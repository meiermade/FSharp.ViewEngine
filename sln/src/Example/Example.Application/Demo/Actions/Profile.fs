module Example.Application.Web.Demo.Actions.Profile

open Microsoft.AspNetCore.Http
open Example.Application.Web.Demo
open Example.UI.Views.Model
open Example.Application.Web
open Example.UI.Views.ViewData

let submit page (form: IFormCollection) =
    match page with
    | ApplicationPage.EditProfile ->
        let request: ProfileDraft =
            { name = form["name"].ToString(); email = form["email"].ToString()
              timeZone = form["timezone"].ToString(); dailySummaryTime = form["dailySummaryTime"].ToString() }
        let valid = (Projection.profileError request).IsNone
        Some { Submission.result (if valid then "validated" else "invalid") with draft = if valid then None else Some(EditorDraft.Profile request) }
    | _ -> None
