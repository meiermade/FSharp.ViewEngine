module Docs.Web.PreviewUpdate

open System
open FSharp.ViewEngine
open Giraffe
open Microsoft.AspNetCore.Http
open type Datastar

let private request owner href =
    $"""
const preview = document.getElementById('{owner}');
preview.updateRequest?.abort();
preview.updateRequest = new AbortController();
const patchUrl = new URL({href}, location.href);
patchUrl.pathname += '/patch';
@get(patchUrl.href, {{ filterSignals: {{ exclude: /.*/ }}, requestCancellation: preview.updateRequest, retry: 'never' }});
"""

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
evt.preventDefault();
const target = new URL(el.getAttribute('action') || location.href, location.href);
target.search = new URLSearchParams(new FormData(el, evt.submitter));
{request owner "target.href"}
""")

let private patch commit (fragments: HtmlElement list) effect : HttpHandler =
    fun next context ->
        let path = string context.Request.Path
        let publicPath = path.Substring(0, path.Length - "/patch".Length)
        let url = History.urlFor publicPath context
        let html = fragments |> List.map Render.toString |> String.concat "\n"
        (History.patchElements html >=> commit url effect) next context

let respond fragments effect = patch History.replaceUrl fragments effect
let respondWithHistory fragments effect = patch History.pushPageUrl fragments effect
