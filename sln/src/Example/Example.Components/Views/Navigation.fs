module Example.Components.Views.Navigation

open FSharp.ViewEngine
open type Datastar

// Only controls that update the current surface use this request. Regular links are native.
let request intent href =
    $"""
const owner = document.getElementById('example-navigation-root');
owner.updateRequest?.abort();
owner.updateRequest = new AbortController();
owner.updateTarget = {href};
owner.updateSource = el;
@get(owner.updateTarget, {{ filterSignals: {{ exclude: /.*/ }}, headers: {{ 'X-FVE-Interaction': '{intent}' }}, requestCancellation: owner.updateRequest, retry: 'never' }});
"""

let private follow intent destination =
    $"""
const link = {destination};
if (link && !evt.defaultPrevented && evt.button === 0 && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && (!link.target || link.target === '_self') && !link.hasAttribute('download')) {{
  const target = new URL(link.href);
  if (target.origin === location.origin && target.pathname === location.pathname && !target.hash) {{
    evt.preventDefault();
    {request intent "target.pathname + target.search"}
  }}
}}
"""

let filterLink = _dataOn ("click", follow "filter" "el")
let menuLinks = _dataOn ("click", follow "update" "evt.target.closest('a[href]')")

let private form intent =
    _dataOn ("submit", $"""
const target = new URL(evt.submitter?.hasAttribute('formaction') ? evt.submitter.formAction : el.getAttribute('action') || location.href, location.href);
if (target.origin !== location.origin || target.pathname !== location.pathname) return;
target.search = new URLSearchParams(new FormData(el, evt.submitter));
{request intent "target.pathname + target.search"}
""")

let getForm = form "update"
let filterForm = form "filter"
