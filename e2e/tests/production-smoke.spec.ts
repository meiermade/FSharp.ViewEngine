import { expect, test } from '@playwright/test'

const expectedCommit = process.env.DOCS_EXPECTED_COMMIT ?? 'local'
const expectedImage = process.env.DOCS_EXPECTED_IMAGE ?? 'local'
const expectedEnvironment = process.env.DOCS_EXPECTED_ENVIRONMENT ?? (expectedCommit === 'local' ? 'local' : 'production')
const canonicalOrigin = 'https://fsharpviewengine.meiermade.com'

test('health reports the deployed release identity', async ({ request }) => {
  const response = await request.get('/health')
  expect(response.status()).toBe(200)
  await expect(response.json()).resolves.toEqual({
    status: 'ok',
    environment: expectedEnvironment,
    commit: expectedCommit,
    image: expectedImage,
  })
})

test('canonical discovery endpoints remain public', async ({ request }) => {
  const sitemap = await request.get('/sitemap.xml')
  expect(sitemap.status()).toBe(200)
  expect(sitemap.headers()['content-type']).toContain('application/xml')
  expect(await sitemap.text()).toContain(`<loc>${canonicalOrigin}/</loc>`)

  const robots = await request.get('/robots.txt')
  expect(robots.status()).toBe(200)
  expect(await robots.text()).toContain(`Sitemap: ${canonicalOrigin}/sitemap.xml`)
})

test('representative documentation surfaces render without browser errors', async ({ page }) => {
  const pageErrors: Error[] = []
  page.on('pageerror', error => pageErrors.push(error))

  for (const path of ['/', '/components', '/docs']) {
    const response = await page.goto(path)
    expect(response?.status(), path).toBe(200)
    await expect(page.locator('main')).toBeVisible()
  }

  expect(pageErrors).toEqual([])
})

test('session-isolated fixture forms accept the public HTTPS origin', async ({ page }) => {
  await page.goto('/components/page-examples/account-management?destination=ledger-settings')
  await page.getByRole('textbox', { name: 'Workspace name', exact: true }).fill('Production smoke workspace')
  await page.getByRole('button', { name: 'Save settings', exact: true }).click()
  await expect(page.locator('#ledger-app-shell')).toContainText('Settings saved')
})
