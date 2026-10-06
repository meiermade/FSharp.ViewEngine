import http from 'node:http'
import { test, expect } from '../fixture'

test('load diagnostics distinguish a pending document body from received headers', async ({ page, loadDiagnostics }) => {
  let release!: () => void
  const bodyReady = new Promise<void>(resolve => { release = resolve })
  const server = http.createServer(async (_request, response) => {
    response.writeHead(200, { 'Content-Type': 'text/html' })
    response.flushHeaders()
    await bodyReady
    response.end('<!doctype html><title>Ready</title><p>Document body loaded</p>')
  })
  await new Promise<void>(resolve => server.listen(0, '127.0.0.1', resolve))
  try {
    await page.goto(`http://127.0.0.1:${(server.address() as { port: number }).port}/`, { waitUntil: 'commit' })
    await expect.poll(() => loadDiagnostics.snapshot().pending.some(r => r.type === 'document' && r.status === 200)).toBe(true)
    expect(loadDiagnostics.snapshot().events.some(e => e.phase === 'load' && e.documentId !== null)).toBe(false)
    release()
    await page.waitForLoadState('load')
    await expect(page.getByText('Document body loaded', { exact: true })).toBeVisible()
    await expect.poll(() => loadDiagnostics.snapshot().pending.some(r => r.type === 'document')).toBe(false)
    expect(loadDiagnostics.snapshot().events.some(e => e.phase === 'load' && e.documentId !== null)).toBe(true)
  } finally {
    release()
    server.closeAllConnections()
    await new Promise<void>(resolve => server.close(() => resolve()))
  }
})
