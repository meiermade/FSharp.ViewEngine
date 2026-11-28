module Docs.Web.History

open System.Text.Json
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection

let requested (context: HttpContext) = context.Request.Headers["Datastar-Request"].ToString() = "true"

let urlFor path (context: HttpContext) =
    let query = QueryString.Create(context.Request.Query |> Seq.filter (fun entry -> entry.Key.ToLowerInvariant() <> "datastar"))
    string context.Request.PathBase + path + string query

let private execute (datastar: IDatastarService) (context: HttpContext) script =
    datastar.ExecuteScriptAsync(script, ExecuteScriptOptions(AutoRemove = true), context.RequestAborted)

let private patch before html : HttpHandler =
    fun next context -> task {
        let datastar = context.RequestServices.GetRequiredService<IDatastarService>()
        if before <> "" then do! execute datastar context before
        do! datastar.PatchElementsAsync(html, cancellationToken = context.RequestAborted)
        return! next context
    }

let patchElements html = patch "" html
let patchPage html = patch "document.querySelectorAll('dialog[open][data-fve-navigation-dialog]').forEach(dialog => dialog.close('fve-navigation'));" html

let private commit method url effect : HttpHandler =
    let script = $"""
(() => {{
  const previous = window.fveRenderedPage ?? location.pathname + location.search;
  const url = {JsonSerializer.Serialize(url: string)};
  if (location.pathname + location.search !== url) history.{method}(history.state, '', url);
  window.fveRenderedPage = url;
  {effect}
}})();
"""
    fun _ context -> task {
        let datastar = context.RequestServices.GetRequiredService<IDatastarService>()
        do! execute datastar context script
        return Some context
    }

let pushPageUrl url effect = commit "pushState" url effect
let replaceUrl url effect = commit "replaceState" url effect
