import { test as base, expect } from '@playwright/test'
import { createLoadDiagnostics } from './scripts/load-diagnostics.mjs'

type LoadDiagnostics = ReturnType<typeof createLoadDiagnostics>
export const test = base.extend<{ loadDiagnostics: LoadDiagnostics }>({
  loadDiagnostics: [async ({ page, baseURL }, use, testInfo) => {
    const diagnostics = createLoadDiagnostics(baseURL!)
    page.on('request', request => diagnostics.request(request, request.isNavigationRequest() && request.frame() === page.mainFrame()))
    page.on('response', response => diagnostics.response(response.request(), response.status()))
    page.on('requestfinished', request => diagnostics.finished(request))
    page.on('requestfailed', request => diagnostics.failed(request, request.failure()?.errorText))
    page.on('domcontentloaded', () => diagnostics.lifecycle('domcontentloaded'))
    page.on('load', () => diagnostics.lifecycle('load'))
    await use(diagnostics)
    if (testInfo.status !== testInfo.expectedStatus) {
      await testInfo.attach('load-diagnostics', {
        body: Buffer.from(JSON.stringify(diagnostics.snapshot(), null, 2)),
        contentType: 'application/json',
      })
    }
  }, { auto: true }],
})
export { expect }
