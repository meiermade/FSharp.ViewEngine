// Deliberately whitelist fields and route labels. No headers, bodies, query strings,
// arbitrary URLs, console messages or raw error text enter an authenticated artifact.
const routes = new Set([
  '/health', '/components/button', '/components/badge', '/components/notification',
  '/components/notifications/show', '/components/month-calendar', '/components/week-calendar',
  '/components/day-calendar', '/components/year-calendar', '/examples/specification/profile',
  '/examples/application/profile', '/css/output.css', '/css/prism-tomorrow.1.29.0.min.css',
  '/scripts/datastar.1.0.2.js', '/scripts/tailwind-elements-loader.1.0.22.js',
  '/scripts/mermaid.11.16.0.min.js', '/scripts/prism.1.29.0.min.js',
  '/scripts/prism-fsharp.1.29.0.min.js', '/scripts/prism-sql.1.29.0.min.js',
  '/scripts/prism-bash.1.29.0.min.js', '/scripts/prism-json.1.29.0.min.js',
])
const methods = new Set(['GET', 'POST', 'HEAD', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'])
const types = new Set(['document', 'stylesheet', 'script', 'image', 'font', 'xhr', 'fetch', 'other'])
const errors = new Set(['net::ERR_ABORTED', 'net::ERR_EMPTY_RESPONSE', 'net::ERR_CONNECTION_RESET',
  'net::ERR_CONNECTION_REFUSED', 'net::ERR_NAME_NOT_RESOLVED', 'net::ERR_TIMED_OUT'])

export function createLoadDiagnostics(baseURL) {
  const origin = new URL(baseURL).origin
  const start = performance.now()
  const requests = new Map()
  const events = []
  let nextId = 0
  let documentId = null
  function event(phase, fields = {}) {
    events.push({ ms: Math.round(performance.now() - start), phase, ...fields })
    if (events.length > 200) events.shift()
  }
  function label(request) {
    const url = new URL(request.url())
    return {
      route: url.origin !== origin ? 'external' : routes.has(url.pathname) ? url.pathname : 'other same-origin',
      method: methods.has(request.method()) ? request.method() : 'other',
      type: types.has(request.resourceType()) ? request.resourceType() : 'other',
    }
  }
  return {
    request(request, mainDocument = false) {
      const item = { id: ++nextId, ...label(request) }
      if (mainDocument) documentId = item.id
      requests.set(request, item)
      event('requested', item)
    },
    response(request, status) {
      const item = requests.get(request)
      if (item) {
        item.status = Number.isInteger(status) && status >= 100 && status <= 599 ? status : 0
        event('response-headers', item)
      }
    },
    finished(request) {
      const item = requests.get(request)
      if (item) event('finished', item)
      requests.delete(request)
    },
    failed(request, error) {
      const item = requests.get(request)
      if (item) event('failed', { ...item, reason: errors.has(error) ? error : 'other' })
      requests.delete(request)
    },
    lifecycle(phase) {
      if (['domcontentloaded', 'load'].includes(phase)) event(phase, { documentId })
    },
    snapshot() { return { events: [...events], pending: [...requests.values()].map(item => ({ ...item })) } },
  }
}
