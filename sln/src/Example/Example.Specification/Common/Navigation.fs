module Example.Specification.Web.Common.Navigation

open System
open System.Text.Json
open FSharp.ViewEngine
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar.DependencyInjection
open type Datastar

module Actions = Example.UI.Views.Navigation

type Intent = Update | Restore | Filter | Review | Submit

let private root = "document.getElementById('example-navigation-root')"

let initialScript =
    $"document.addEventListener('DOMContentLoaded', () => {{ {root}.renderedHref = location.pathname + location.search; }}, {{ once: true }});"

let private restoreAction = $"""
const href = location.pathname + location.search;
const owner = {root};
const pending = window.fvePageRequest;
pending?.abort();
if (pending || (owner.updateTarget ? owner.updateTarget !== href : owner.renderedHref !== href)) {{
  owner.updateRequest?.abort();
  const request = window.fvePageRequest = new AbortController();
  @get(location.href, {{ filterSignals: {{ exclude: /.*/ }}, requestCancellation: request, retry: 'never' }})
    .finally(() => {{ if (window.fvePageRequest === request) delete window.fvePageRequest; }});
}}
"""

let private failureAction = $"""
const owner = {root};
if (owner.updateTarget && owner.updateSource === evt.detail.el && ((evt.detail.type === 'error' && evt.detail.argsRaw?.status) || evt.detail.type === 'retries-failed')) {{
  const target = owner.updateTarget;
  delete owner.updateTarget;
  if (Number(evt.detail.argsRaw?.status) === 404) location.assign(target);
  else {{ const status = document.getElementById('docs-navigation-status'); status.hidden = false; status.textContent = 'The update could not be loaded. Try again or refresh.'; }}
}}
"""

let bodyAttributes =
    [ _dataOn ("popstate", ["window"], restoreAction)
      _dataOn ("datastar-fetch", ["window"], failureAction)
      _dataOn ("fve-fixture-location", ["window"], Actions.request "review" "evt.detail.href") ]

// A named action control may shadow the form.action property.
let formSubmitAction pendingSignal =
    $"if (!${pendingSignal}) {{ @post(el.getAttribute('action'), {{ contentType: 'form', headers: {{ 'X-FVE-Interaction': 'submit' }}, retry: 'never' }}) }}"

let tryIntent (context: HttpContext) =
    if context.Request.Headers["datastar-request"].ToString() <> "true" then None
    else
        match context.Request.Headers["X-FVE-Interaction"].ToString() with
        | "update" -> Some Update
        | "restore" -> Some Restore
        | "filter" -> Some Filter
        | "review" -> Some Review
        | "submit" when not (HttpMethods.IsGet context.Request.Method) -> Some Submit
        | _ when HttpMethods.IsGet context.Request.Method -> Some Update
        | _ -> None

let private execute (service: IDatastarService) script cancellationToken =
    service.ExecuteScriptAsync(script, ExecuteScriptOptions(AutoRemove = true), cancellationToken)

let redirect (destination: string) : HttpHandler =
    fun next context ->
        if HttpMethods.IsPost context.Request.Method then
            (setStatusCode 303 >=> setHttpHeader "Location" destination >=> text "") next context
        else
            redirectTo false destination next context

let respond intent rootHtml metadataHtml : HttpHandler =
    fun _ context -> task {
        let service = context.RequestServices.GetRequiredService<IDatastarService>()
        let cancellationToken = context.RequestAborted
        if not cancellationToken.IsCancellationRequested then
            // A native history traversal must never replay a cached event stream as a document.
            context.Response.OnStarting(fun () ->
                context.Response.Headers.CacheControl <- "private, no-store"
                System.Threading.Tasks.Task.CompletedTask)
            let publicQuery = QueryString.Create(context.Request.Query |> Seq.filter (fun entry -> not (List.contains (entry.Key.ToLowerInvariant()) ["datastar"; "fveappreturn"])))
            let href = JsonSerializer.Serialize(string context.Request.PathBase + string context.Request.Path + string publicQuery)
            let commit =
                match intent with
                | Update | Filter -> $"if (location.pathname + location.search !== {href}) history.pushState(history.state, '', {href});"
                | Submit | Review | Restore -> ""
            let completed =
                match intent with
                | Update -> "update"
                | Review -> "review"
                | _ -> "restore"
            if intent = Update || intent = Restore then
                do! execute service "document.querySelectorAll('dialog[open][data-fve-navigation-dialog]').forEach(dialog => dialog.close('fve-navigation'));" cancellationToken
            do! service.PatchElementsAsync(rootHtml, cancellationToken = cancellationToken)
            do! service.PatchElementsAsync(metadataHtml, cancellationToken = cancellationToken)
            let effect = $"""
(() => {{
  const root = {root};
  const previousHref = root.renderedHref || location.href;
  const restored = {if intent = Update then "true" else "false"} && location.pathname + location.search === {href} && previousHref !== {href};
  {commit}
  root.renderedHref = {if intent = Review then href else "location.pathname + location.search"};
  delete root.updateTarget;
  const status = document.getElementById('docs-navigation-status');
  if (status) {{ status.hidden = true; status.textContent = ''; }}
  window.fveColorMode?.refresh();
  requestAnimationFrame(() => {{
    if ({if intent = Submit then "true" else "false"}) {{
      const dialog = root.querySelector('dialog[open]');
      const invalid = dialog?.querySelector('[autofocus][aria-invalid=true], [aria-invalid=true] input') || root.querySelector('[autofocus][aria-invalid=true]');
      invalid?.focus({{preventScroll: true}});
    }}
    if ({if intent = Update then "true" else "false"} && new URL(previousHref, location.href).pathname !== location.pathname) {{
      const main = document.getElementById('main-content');
      root.querySelectorAll('[data-fve-page-scroll]').forEach(region => region.scrollTo(0, 0));
      main?.focus({{preventScroll:true}});
    }}
    root.dispatchEvent(new CustomEvent('fve-interaction-complete', {{bubbles: true, detail: {{intent: restored ? 'restore' : '{completed}', previousHref: new URL(previousHref, location.href).href, href: location.href}}}}));
  }});
}})();
"""
            do! execute service effect cancellationToken
        return Some context
    }
