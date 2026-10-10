namespace Docs.Examples

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Text.Json
open FSharp.ViewEngine
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar.DependencyInjection
open type Html
open type Datastar

/// Consumer-owned progressive navigation, shared by the catalog and downloadable host.
module Navigation =
    type Enhancement =
        { initialScript:string
          clickAction:string
          submitAction:string
          restoreAction:string
          fetchLifecycle:string
          appModeExitAction:string }

    type Intent = Navigate | Restore

    let private intentHeader = "X-FVE-Navigation-Intent"
    let private clientHeader = "X-FVE-Navigation-Client"
    let private sequenceHeader = "X-FVE-Navigation-Sequence"
    let private latestSequences = ConcurrentDictionary<Guid, int64>()

    let private requestOptions intent =
        $"{{ filterSignals: {{ exclude: /.*/ }}, headers: {{ '{intentHeader}': '{intent}', '{clientHeader}': document.body.dataset.navigationClient, '{sequenceHeader}': (document.body.dataset.navigationSequence = String((Number(document.body.dataset.navigationSequence) || 0) + 1)) }}, requestCancellation: window.fveNavigationController, retry: 'never', retryMaxCount: 0 }}"

    let private saveScroll =
        "const fveScrollRoot = document.querySelector('[data-docs-main], [data-fve-page-scroll], [data-fve-app-mode-root]'); window.history.replaceState(Object.assign({}, window.history.state || {}, { fveDocsScroll: [fveScrollRoot?.scrollLeft ?? window.scrollX, fveScrollRoot?.scrollTop ?? window.scrollY] }), '', window.location.href);"

    let enhancement =
        { initialScript = "window.history.scrollRestoration = 'manual'; document.addEventListener('DOMContentLoaded', () => { document.body.dataset.navigationClient = window.crypto.randomUUID(); document.body.dataset.navigationSequence = '0'; const root = document.querySelector('[data-fve-navigation-root]'); const documentToken = window.crypto.randomUUID(); if (root) root.dataset.navigationDocument = documentToken; window.history.replaceState(Object.assign({}, window.history.state || {}, { fveDocsDocument: documentToken, fveDocsIndex: window.history.state?.fveDocsIndex ?? 0, fveDocsScroll: [window.scrollX, window.scrollY] }), '', window.location.href); document.body.dataset.navigationIndex = String(window.history.state.fveDocsIndex); }, { once: true });"
          clickAction =
            $"""
const link = evt.target.closest?.('a[href]');
if (!evt.defaultPrevented && evt.button === 0 && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && link && !link.closest('[data-example-native-navigation]') && (!link.target || link.target === '_self') && !link.hasAttribute('download')) {{
  const target = new URL(link.href, window.location.origin);
  if (target.origin === window.location.origin && target.pathname === window.location.pathname && target.search === window.location.search && target.hash) {{
    window.fsharpDocsFragments?.navigate(evt, target.hash);
  }} else if (target.origin === window.location.origin && target.protocol.startsWith('http') && !target.hash) {{
    if (document.querySelector('[data-fve-navigation-root]')?.dataset.templateEmbedded === 'true' && target.pathname.startsWith('/examples/')) target.searchParams.set('embedded', '1');
    const frame = document.body.dataset.fveAppModeFrame;
    if (frame && !link.hasAttribute('data-fve-app-mode-exit')) {{
      target.searchParams.set('fveAppMode', 'app');
      target.searchParams.set('fveAppFrame', frame);
    }}
    evt.preventDefault();
    if (!window.dispatchEvent(new CustomEvent('fve-before-navigate', {{cancelable: true}}))) return;
    window.fveNavigationController?.abort(); window.fveNavigationController = new AbortController();
    {saveScroll}
    $navigationPending = true;
    document.querySelector('[data-fve-navigation-root]')?.setAttribute('aria-busy', 'true');
    $navigationTarget = target.pathname + target.search;
    @get($navigationTarget, {requestOptions "push"});
  }}
}}
"""
          submitAction =
            $"""
const form = evt.target;
const submitter = evt.submitter;
const method = submitter?.hasAttribute('formmethod') ? submitter.formMethod : form.method;
const targetName = submitter?.hasAttribute('formtarget') ? submitter.formTarget : form.target;
const target = new URL(submitter?.hasAttribute('formaction') ? submitter.formAction : form.action, window.location.origin);
if (!evt.defaultPrevented && !form.closest('[data-example-native-navigation]') && method.toLowerCase() === 'get' && (!targetName || targetName === '_self') && target.origin === window.location.origin) {{
  target.search = '';
  for (const [name, value] of new FormData(form, submitter)) target.searchParams.append(name, value);
  if (document.querySelector('[data-fve-navigation-root]')?.dataset.templateEmbedded === 'true') target.searchParams.set('embedded', '1');
  const frame = document.body.dataset.fveAppModeFrame;
  if (frame) {{
    target.searchParams.set('fveAppMode', 'app');
    target.searchParams.set('fveAppFrame', frame);
  }}
  evt.preventDefault();
  window.fveNavigationController?.abort(); window.fveNavigationController = new AbortController();
  {saveScroll}
  $navigationPending = true;
  document.querySelector('[data-fve-navigation-root]')?.setAttribute('aria-busy', 'true');
  $navigationTarget = target.pathname + target.search;
  @get($navigationTarget, {requestOptions "push"});
}}
"""
          restoreAction =
            $"""
const root = document.querySelector('[data-fve-navigation-root]');
if (!$navigationPending && evt.state?.fveDocsDocument && evt.state.fveDocsDocument === root?.dataset.navigationDocument) {{
  const restoredScroll = evt.state.fveDocsScroll ?? [0, 0];
  const scrollRoot = document.querySelector('[data-docs-main], [data-fve-page-scroll], [data-fve-app-mode-root]');
  scrollRoot ? scrollRoot.scrollTo({{ left: restoredScroll[0], top: restoredScroll[1], behavior: 'instant' }}) : window.scrollTo({{ left: restoredScroll[0], top: restoredScroll[1], behavior: 'instant' }});
  window.fsharpDocsFragments?.show(window.location.hash);
}} else {{
  if (!window.dispatchEvent(new CustomEvent('fve-before-navigate', {{cancelable: true}}))) {{
    const delta = Number(document.body.dataset.navigationIndex) - evt.state?.fveDocsIndex;
    if (Number.isFinite(delta) && delta !== 0) window.history.go(delta);
    else window.history.forward();
    return;
  }}
  window.fveNavigationController?.abort(); window.fveNavigationController = new AbortController();
  $navigationPending = true;
  root?.setAttribute('aria-busy', 'true');
  $navigationTarget = window.location.pathname + window.location.search;
  @get($navigationTarget, {requestOptions "restore"});
}}
"""
          fetchLifecycle =
            "evt.detail?.el === document.body && ((evt.detail.type === 'error' && evt.detail.argsRaw?.status) || evt.detail.type === 'retries-failed') ? ($navigationPending = false, $navigationTarget = '', document.querySelector('[data-fve-navigation-root]')?.removeAttribute('aria-busy'), document.getElementById('docs-navigation-status').hidden = false, document.getElementById('docs-navigation-status').textContent = 'The page could not be loaded. Try the link again or refresh.') : null"
          appModeExitAction =
            "evt.key == 'Escape' && !evt.defaultPrevented && !document.querySelector(':popover-open, [data-fve-app-mode-root] button[aria-controls][aria-expanded=true], [data-fve-app-mode-root] dialog[open]') ? document.querySelector('[data-fve-app-mode-exit]')?.click() : null" }

    let bodyAttributes =
        [ _dataSignals "{sideNavOpen: false, navigationPending: false, navigationTarget: ''}"
          _dataOn ("click", enhancement.clickAction)
          _dataOn ("submit", enhancement.submitAction)
          _dataOn ("popstate__window", enhancement.restoreAction)
          _dataOn ("datastar-fetch", enhancement.fetchLifecycle) ]

    let private clientSequence (context:HttpContext) =
        match Guid.TryParse(context.Request.Headers[clientHeader].ToString()), Int64.TryParse(context.Request.Headers[sequenceHeader].ToString()) with
        | (true, client), (true, sequence) when sequence > 0L -> Some(client, sequence)
        | _ -> None

    let private isLatestIntent context =
        match clientSequence context with
        | Some(client, sequence) ->
            match latestSequences.TryGetValue client with
            | true, latest -> latest = sequence
            | _ -> false
        | None -> true

    let tryIntent (context:HttpContext) =
        let intent =
            if context.Request.Headers["datastar-request"].ToString() <> "true" then None
            else
                match context.Request.Headers[intentHeader].ToString() with
                | "push" -> Some Navigate
                | "restore" -> Some Restore
                | _ -> None
        if intent.IsSome then
            match clientSequence context with
            | Some(client, sequence) -> latestSequences.AddOrUpdate(client, sequence, fun _ current -> max current sequence) |> ignore
            | None -> ()
        intent

    let private cleanPublicHref (context:HttpContext) =
        let query = QueryString.Create(
            context.Request.Query
            |> Seq.collect (fun pair ->
                pair.Value
                |> Seq.choose (fun value ->
                    if pair.Key.Equals("datastar", StringComparison.OrdinalIgnoreCase) || pair.Key = "fveAppReturn" || pair.Key = "fveAppTransition" then None
                    else Some(KeyValuePair(pair.Key, value)))))
        string context.Request.PathBase + string context.Request.Path + query.ToUriComponent()

    let private responseScript intent publicHref returnFocusId documentToken =
        let encodedHref = JsonSerializer.Serialize publicHref
        let encodedFocus = returnFocusId |> Option.map JsonSerializer.Serialize |> Option.defaultValue "null"
        let encodedDocumentToken = JsonSerializer.Serialize documentToken
        let history, scroll =
            match intent with
            | Navigate ->
                $"window.history.pushState({{ fveDocsDocument: {encodedDocumentToken}, fveDocsIndex: (Number(document.body.dataset.navigationIndex) || 0) + 1, fveDocsScroll: [0, 0] }}, '', {encodedHref});",
                "for (const element of document.querySelectorAll('[data-docs-main], [data-fve-page-scroll], [data-docs-page-viewport], [data-docs-page-layout], [data-docs-custom-rail], [data-fve-app-mode-root]')) element.scrollTo({ top: 0, left: 0, behavior: 'instant' }); window.scrollTo({ top: 0, left: 0, behavior: 'instant' });"
            | Restore ->
                $"window.history.replaceState(Object.assign({{}}, window.history.state || {{}}, {{ fveDocsDocument: {encodedDocumentToken} }}), '', window.location.href);",
                "const restoredScroll = window.history.state?.fveDocsScroll ?? [0, 0]; const restoredRoot = document.querySelector('[data-docs-main], [data-fve-page-scroll], [data-fve-app-mode-root]'); restoredRoot ? restoredRoot.scrollTo({ left: restoredScroll[0], top: restoredScroll[1], behavior: 'instant' }) : window.scrollTo({ left: restoredScroll[0], top: restoredScroll[1], behavior: 'instant' });"
        $"""
(function() {{
const previousHref = window.location.href;
{history}
document.body.dataset.navigationIndex = String(window.history.state?.fveDocsIndex ?? 0);
const navigationRoot = document.querySelector('[data-fve-navigation-root]');
if (navigationRoot) navigationRoot.dataset.navigationDocument = {encodedDocumentToken};
{scroll}
window.fsharpDocsMobileNav?.close();
document.getElementById('template-navigation-dialog')?.close();
navigationRoot?.removeAttribute('aria-busy');
const navigationStatus = document.getElementById('docs-navigation-status');
if (navigationStatus) {{ navigationStatus.hidden = true; navigationStatus.textContent = ''; }}
window.fveColorMode?.refresh();
window.initializeDocsToc?.();
window.fsharpDocsFragments?.show(window.location.hash);
const returnFocus = {encodedFocus};
requestAnimationFrame(() => {{
  (returnFocus ? document.getElementById(returnFocus) : document.getElementById('main-content') ?? document.getElementById('fve-app-mode-root'))?.focus?.({{ preventScroll: true }});
  navigationRoot?.dispatchEvent(new CustomEvent('fve-navigation-complete', {{ bubbles: true, detail: {{ intent: '{if intent = Navigate then "navigate" else "restore"}', previousHref, href: window.location.href }} }}));
}});
}})();
"""

    let respond intent rootHtml metadataHtml : HttpHandler =
        fun _ context -> task {
            let service = context.RequestServices.GetRequiredService<IDatastarService>()
            if context.RequestAborted.IsCancellationRequested then
                return Some context
            elif not (isLatestIntent context) then
                do! service.ExecuteScriptAsync("void 0", ExecuteScriptOptions(AutoRemove = true), context.RequestAborted)
                return Some context
            else
                let returnFocusId =
                    match context.Request.Query.TryGetValue "fveAppReturn" with
                    | true, value when not (String.IsNullOrWhiteSpace(string value)) -> Some("fve-fixture-" + string value + "-launcher")
                    | _ -> None
                do! service.PatchElementsAsync(rootHtml, cancellationToken = context.RequestAborted)
                do! service.PatchElementsAsync(metadataHtml, cancellationToken = context.RequestAborted)
                do! service.ExecuteScriptAsync(
                    responseScript intent (cleanPublicHref context) returnFocusId (Guid.NewGuid().ToString("N")),
                    ExecuteScriptOptions(AutoRemove = true),
                    context.RequestAborted)
                do! service.PatchSignalsAsync(
                    {| sideNavOpen = false; navigationPending = false; navigationTarget = "" |},
                    context.RequestAborted)
                return Some context
        }
