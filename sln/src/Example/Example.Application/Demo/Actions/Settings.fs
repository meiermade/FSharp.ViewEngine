module Example.Application.Web.Demo.Actions.Settings

open Microsoft.AspNetCore.Http
open Example.Application.Web.Demo
open Example.UI.Views.Model
open Example.Application.Web
open Example.UI.Views.ViewData

let submit page (form: IFormCollection) =
    match page with
    | ApplicationPage.EditOrganization ->
        let request: OrganizationDraft = { name = form["workspace"].ToString(); currency = form["currency"].ToString() }
        let valid = (Projection.organizationError request).IsNone
        Some { Submission.result (if valid then "validated" else "invalid") with draft = if valid then None else Some(EditorDraft.Organization request) }
    | ApplicationPage.EditEnvironment id ->
        let request: EnvironmentDraft = { environmentId = id; name = form["name"].ToString() }
        let valid = (Projection.environmentError request).IsNone
        Some { Submission.result (if valid then "validated" else "invalid") with draft = if valid then None else Some(EditorDraft.Environment request) }
    | _ -> None
