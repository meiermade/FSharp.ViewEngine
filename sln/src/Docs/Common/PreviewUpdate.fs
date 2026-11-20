module Docs.Web.PreviewUpdate

open System.Text.Json
open FSharp.ViewEngine
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar.DependencyInjection
open type Datastar

let private requestWith owner href options =
    $"""
const preview = document.getElementById('{owner}');
preview.updateRequest?.abort();
preview.updateRequest = new AbortController();
@get({href}, {{ filterSignals: {{ exclude: /.*/ }}, requestCancellation: preview.updateRequest, retry: 'never'{options} }});
"""

let private request owner href = requestWith owner href ""

let restore owner =
    _dataOn ("popstate", ["window"], $"""
if (el.tokenSearch !== undefined && el.tokenSearch !== location.search) {{
  {requestWith owner "location.href" ", headers: { 'X-FVE-Preview-Restore': 'true' }"}
}}
""")

let link owner before =
    _dataOn ("click", $"""
if (!evt.defaultPrevented && evt.button === 0 && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey) {{
  evt.preventDefault();
  {before}
  {request owner "el.href"}
}}
""")

let form owner =
    _dataOn ("submit", $"""
const target = new URL(el.getAttribute('action') || location.href, location.href);
target.search = new URLSearchParams(new FormData(el, evt.submitter));
{request owner "target.href"}
""")

let requested (context: HttpContext) = context.Request.Headers["Datastar-Request"].ToString() = "true"

let private patch historyAction (fragments: HtmlElement list) effect : HttpHandler =
    fun _ context -> task {
        let service = context.RequestServices.GetRequiredService<IDatastarService>()
        let cancellationToken = context.RequestAborted
        for fragment in fragments do
            do! service.PatchElementsAsync(Render.toString fragment, cancellationToken = cancellationToken)
        let query = QueryString.Create(context.Request.Query |> Seq.filter (fun entry -> not (System.String.Equals(entry.Key, "datastar", System.StringComparison.OrdinalIgnoreCase))))
        let url = JsonSerializer.Serialize(string context.Request.PathBase + string context.Request.Path + string query)
        do! service.ExecuteScriptAsync($"history.{historyAction}(history.state, '', {url} + location.hash); {effect}", ExecuteScriptOptions(AutoRemove = true), cancellationToken)
        return Some context
    }

let respond fragments effect = patch "replaceState" fragments effect
let respondWithHistory fragments effect : HttpHandler =
    fun next context ->
        let historyAction = if context.Request.Headers["X-FVE-Preview-Restore"].ToString() = "true" then "replaceState" else "pushState"
        patch historyAction fragments effect next context
