import { expect, test } from '../fixture'

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

test('essential rendered assets and critical read-only navigation work @cross-browser', async ({ page }) => {
  const pageErrors: Error[] = []
  const writes: string[] = []
  page.on('pageerror', error => pageErrors.push(error))
  page.on('request', request => {
    if (!['GET', 'HEAD'].includes(request.method())) writes.push(`${request.method()} ${request.url()}`)
  })

  for (const path of ['/', '/components', '/examples/application', '/examples/api-documentation', '/examples/specification']) {
    const response = await page.goto(path)
    expect(response?.status(), path).toBe(200)
    await expect(page.locator('main')).toBeVisible()
    if (path === '/') {
      const paragraph = page.locator('[data-docs-section-content="true"] > p').first()
      await expect(paragraph).toHaveCSS('font-size', '16px')
      expect(await paragraph.evaluate(el => getComputedStyle(el).fontFamily)).toContain('Noto Sans')
      await page.evaluate(() => document.fonts.ready)
      expect(await page.evaluate(() => [...document.fonts].some(font => font.family.includes('Noto Sans') && font.status === 'loaded'))).toBe(true)
      await expect(page.locator('.docs-home-logo img')).toBeVisible()
      expect(await page.locator('.docs-home-logo img').evaluate((img: HTMLImageElement) => img.complete && img.naturalWidth > 0)).toBe(true)
    }
  }

  // A real GET journey exercises the deployed navigation runtime without review-state permutations or submissions.
  await page.evaluate(() => { document.documentElement.dataset.smokeNavigation = 'retained' })
  await page.getByRole('navigation', { name: 'Documentation', exact: true }).getByRole('link', { name: 'View profile', exact: true }).click()
  await expect(page).toHaveURL(url => url.pathname === '/examples/specification/profile')
  await expect(page.getByRole('heading', { name: 'Profile', exact: true, level: 1 })).toBeVisible()
  await expect(page.locator('html')).toHaveAttribute('data-smoke-navigation', 'retained')
  expect(writes).toEqual([])
  expect(pageErrors).toEqual([])
})
