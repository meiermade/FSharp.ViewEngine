import { test } from 'node:test'
import assert from 'node:assert/strict'
import { createLoadDiagnostics } from './load-diagnostics.mjs'
const origin = 'http://127.0.0.1:5054'
function request(url, method = 'GET', type = 'document') {
  return { url: () => url, method: () => method, resourceType: () => type,
    headers() { throw new Error('Headers must never be read') },
    postData() { throw new Error('Bodies must never be read') } }
}

test('response headers do not imply completed document loading', () => {
  const d = createLoadDiagnostics(origin), r = request(origin + '/components/month-calendar')
  d.request(r, true)
  d.response(r, 200)
  assert.deepEqual(d.snapshot().pending, [{ id: 1, route: '/components/month-calendar', method: 'GET', type: 'document', status: 200 }])
  assert.deepEqual(d.snapshot().events.map(e => e.phase), ['requested', 'response-headers'])
  d.finished(r)
  d.lifecycle('domcontentloaded')
  d.lifecycle('load')
  assert.equal(d.snapshot().pending.length, 0)
  assert.equal(d.snapshot().events.at(-1).documentId, 1)
  assert.deepEqual(d.snapshot().events.map(e => e.phase), ['requested', 'response-headers', 'finished', 'domcontentloaded', 'load'])
})

test('authenticated artifacts omit secret URLs, private values and arbitrary error text', () => {
  const d = createLoadDiagnostics(origin)
  for (const url of [
    'http://user:SECRET@127.0.0.1:5054/components/month-calendar?token=SECRET#SECRET',
    origin + '/examples/application/accounts/SECRET?value=SECRET',
    origin + '/scripts/SECRET.js',
    'https://SECRET.invalid/assets/SECRET?key=SECRET',
  ]) {
    const r = request(url, 'SECRET', 'SECRET')
    d.request(r)
    d.response(r, 'SECRET')
    d.failed(r, 'network failure contains SECRET')
  }
  const json = JSON.stringify(d.snapshot())
  assert.ok(!json.includes('SECRET'))
  assert.ok(!json.includes('user'))
  assert.ok(!json.includes('token'))
  assert.ok(json.includes('/components/month-calendar'))
  assert.ok(json.includes('external'))
  assert.equal(d.snapshot().pending.length, 0)
})

test('diagnostics bound history while retaining unfinished requests', () => {
  const d = createLoadDiagnostics(origin), r = request(origin + '/css/output.css', 'GET', 'stylesheet')
  d.request(r)
  for (let i = 0; i < 250; i++) d.lifecycle('load')
  assert.equal(d.snapshot().events.length, 200)
  assert.equal(d.snapshot().pending[0].route, '/css/output.css')
  d.failed(r, 'net::ERR_CONNECTION_RESET')
  assert.equal(d.snapshot().events.at(-1).reason, 'net::ERR_CONNECTION_RESET')
})
