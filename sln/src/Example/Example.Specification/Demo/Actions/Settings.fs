module Example.Specification.Web.Demo.Actions.Settings

open System
open Microsoft.AspNetCore.Http
open Example.Specification.Web.Demo
open Example.UI.Views.Model
open Example.UI.Views.ViewData

let organizationError (draft: OrganizationDraft) =
    if String.IsNullOrWhiteSpace draft.name then Some OrganizationError.InvalidName
    elif draft.currency <> "USD" then Some OrganizationError.InvalidCurrency
    else None

let environmentError (draft: EnvironmentDraft) =
    if String.IsNullOrWhiteSpace draft.name then Some EnvironmentError.InvalidName else None

let submit page (form: IFormCollection) =
    match page with
    | ApplicationPage.EditOrganization ->
        let draft: OrganizationDraft = { name = form["workspace"].ToString(); currency = form["currency"].ToString() }
        if (organizationError draft).IsNone then Some (Submission.result "validated")
        else Some { Submission.result "invalid" with draft = Some (EditorDraft.Organization draft) }
    | ApplicationPage.EditEnvironment id ->
        let draft: EnvironmentDraft = { environmentId = id; name = form["name"].ToString() }
        if (environmentError draft).IsNone then Some (Submission.result "validated")
        else Some { Submission.result "invalid" with draft = Some (EditorDraft.Environment draft) }
    | _ -> None
