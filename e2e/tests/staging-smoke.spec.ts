import { expect, test } from '@playwright/test'

const stagingOrigin = 'https://fve.meiermade.net'
const configuredOrigin = new URL(process.env.DOCS_E2E_BASE_URL ?? 'http://127.0.0.1:5054').origin
const expectedCommit = process.env.DOCS_EXPECTED_COMMIT ?? ''
const expectedImage = process.env.DOCS_EXPECTED_IMAGE ?? ''
const accessClientId = process.env.CF_ACCESS_CLIENT_ID ?? ''
const accessClientSecret = process.env.CF_ACCESS_CLIENT_SECRET ?? ''
const hasAccessCredentials = Boolean(accessClientId && accessClientSecret)
const stagingConfigured = configuredOrigin === stagingOrigin
  && Boolean(expectedCommit && expectedImage)
  && hasAccessCredentials

if (hasAccessCredentials && configuredOrigin !== stagingOrigin) {
  throw new Error(`Staging smoke credentials may only target ${stagingOrigin}`)
}

test.describe('protected staging smoke', () => {
  test.skip(!stagingConfigured, 'requires the deployed staging release and scoped Access credentials')

  test('anonymous requests are rejected by Cloudflare Access', async () => {
    const response = await fetch(stagingOrigin, { redirect: 'manual' })
    expect([302, 403]).toContain(response.status)
    if (response.status === 302) {
      expect(response.headers.get('location')).toContain('/cdn-cgi/access/login')
    }
  })

  test('authenticated health reports the exact staging release identity', async ({ page }) => {
    const response = await page.goto('/health')
    expect(response?.status()).toBe(200)
    if (!response) throw new Error('Health navigation returned no response')
    await expect(response.json()).resolves.toEqual({
      status: 'ok',
      environment: 'staging',
      origin: stagingOrigin,
      commit: expectedCommit,
      image: expectedImage,
      packages: {
        core: { id: 'FSharp.ViewEngine', version: 'unreleased', tag: 'unreleased' },
        components: { id: 'FSharp.ViewEngine.Components', version: 'unreleased', tag: 'unreleased' },
      },
    })
  })

  test('representative protected documentation surfaces render without browser errors', async ({ page }) => {
    const pageErrors: Error[] = []
    page.on('pageerror', error => pageErrors.push(error))

    for (const path of ['/', '/components', '/docs']) {
      const response = await page.goto(path)
      expect(response?.status(), path).toBe(200)
      await expect(page.locator('main')).toBeVisible()
    }

    expect(pageErrors).toEqual([])
  })

  test('protected fixture mutation remains session isolated', async ({ page }) => {
    await page.goto('/components/page-examples/account-management?destination=ledger-settings')
    await page.getByRole('textbox', { name: 'Workspace name', exact: true }).fill('Staging smoke workspace')
    await page.getByRole('button', { name: 'Save settings', exact: true }).click()
    await expect(page.locator('#ledger-app-shell')).toContainText('Settings saved')
  })
})
