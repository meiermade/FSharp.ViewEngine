import { expect, test } from '@playwright/test'

const expectedCommit = process.env.DOCS_EXPECTED_COMMIT ?? 'local'
const expectedImage = process.env.DOCS_EXPECTED_IMAGE ?? 'local'
const expectedEnvironment = process.env.DOCS_EXPECTED_ENVIRONMENT ?? (expectedCommit === 'local' ? 'local' : 'production')
const expectedCoreVersion = process.env.CORE_PACKAGE_VERSION ?? 'unreleased'
const expectedCliVersion = process.env.CLI_PACKAGE_VERSION ?? 'unreleased'
const canonicalOrigin = 'https://fve.meiermade.com'

test('health reports the deployed release identity', async ({ request }) => {
  const response = await request.get('/health')
  expect(response.status()).toBe(200)
  await expect(response.json()).resolves.toEqual({
    status: 'ok',
    environment: expectedEnvironment,
    origin: expectedEnvironment === 'local' ? 'http://127.0.0.1:5054' : canonicalOrigin,
    commit: expectedCommit,
    image: expectedImage,
    packages: {
      core: {
        id: 'FSharp.ViewEngine',
        version: expectedCoreVersion,
        tag: expectedCoreVersion === 'unreleased' ? 'unreleased' : `v${expectedCoreVersion}`,
      },
      cli: {
        id: 'FSharp.ViewEngine.Cli',
        version: expectedCliVersion,
        tag: expectedCliVersion === 'unreleased' ? 'unreleased' : `cli/v${expectedCliVersion}`,
      },
    },
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

  for (const path of ['/', '/components', '/examples/application', '/examples/specification', '/examples/api-documentation']) {
    const response = await page.goto(path)
    expect(response?.status(), path).toBe(200)
    await expect(page.locator('main')).toBeVisible()
  }

  expect(pageErrors).toEqual([])
})

test('stateless financial form accepts the public HTTPS origin without retaining values', async ({ page }) => {
  await page.goto('/examples/application/settings/organizations')
  const form = page.getByRole('form', { name: 'Organization settings' })
  const name = form.getByRole('textbox', { name: 'Name', exact: true })
  const seededName = await name.inputValue()
  await name.fill('Production smoke workspace')
  await form.getByRole('button', { name: 'Validate settings', exact: true }).click()
  await expect(page.getByText('Organization values validated', { exact: true })).toBeVisible()
  await expect(name).toHaveValue(seededName)
})
