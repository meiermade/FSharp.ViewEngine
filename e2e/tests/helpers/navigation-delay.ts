import http from 'node:http'

// Use the real local server and native HTTP streaming: response headers remain observable
// while the genuine SSE body is withheld. A fulfilled/mock response cannot model that boundary.
export async function delayLocalNavigation(baseURL: string) {
  const upstream = new URL(baseURL)
  if (upstream.protocol !== 'http:' || !['127.0.0.1', 'localhost'].includes(upstream.hostname)) return null
  if (process.env.CF_ACCESS_CLIENT_ID || process.env.CF_ACCESS_CLIENT_SECRET) throw new Error('Local delay proxy must not receive Access credentials')
  const delayed: string[] = []
  const server = http.createServer((incoming, outgoing) => {
    const target = new URL(incoming.url!, upstream)
    if (target.origin !== upstream.origin) { outgoing.writeHead(400).end(); return }
    const headers = { ...incoming.headers, host: upstream.host }
    delete headers.cookie
    delete headers.authorization
    const request = http.request(target, { method: incoming.method, headers }, response => {
      outgoing.writeHead(response.statusCode!, response.headers)
      outgoing.flushHeaders()
      const path = target.pathname
      if (incoming.headers['datastar-request'] === 'true' && ['/components/badge', '/components/notification'].includes(path)) {
        delayed.push(path)
        setTimeout(() => { if (!outgoing.destroyed) response.pipe(outgoing) }, 400)
      } else response.pipe(outgoing)
      response.on('error', () => outgoing.destroy())
    })
    request.on('error', () => outgoing.destroy())
    outgoing.on('close', () => request.destroy())
    incoming.pipe(request)
  })
  await new Promise<void>((resolve, reject) => {
    server.once('error', reject)
    server.listen(0, '127.0.0.1', resolve)
  })
  const address = server.address() as { port: number }
  return {
    origin: `http://127.0.0.1:${address.port}`,
    delayed,
    async close() {
      server.closeAllConnections()
      await new Promise<void>(resolve => server.close(() => resolve()))
    },
  }
}
