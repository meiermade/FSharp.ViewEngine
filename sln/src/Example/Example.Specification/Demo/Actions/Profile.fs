module Example.Specification.Web.Demo.Actions.Profile

open System
open Microsoft.AspNetCore.Http
open Example.Specification.Web.Demo
open Example.UI.Views.Model
open Example.UI.Views.ViewData

let error (draft: ProfileDraft) =
    if String.IsNullOrWhiteSpace draft.name then Some ProfileError.InvalidName
    elif not (draft.email.Contains '@') then Some ProfileError.InvalidEmail
    elif not (List.contains draft.timeZone ["America/Chicago"; "America/New_York"; "UTC"]) then Some ProfileError.InvalidTimeZone
    elif not (TimeOnly.TryParse(draft.dailySummaryTime) |> fst) then Some ProfileError.InvalidDailySummaryTime
    else None

let submit page (form: IFormCollection) =
    match page with
    | ApplicationPage.EditProfile ->
        let draft: ProfileDraft =
            { name = form["name"].ToString(); email = form["email"].ToString()
              timeZone = form["timezone"].ToString(); dailySummaryTime = form["dailySummaryTime"].ToString() }
        if (error draft).IsNone then Some (Submission.result "validated")
        else Some { Submission.result "invalid" with draft = Some (EditorDraft.Profile draft) }
    | _ -> None
