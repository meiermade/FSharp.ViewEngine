import { expect, test, specProduct } from '../fixture'

const expectedCommit = process.env.DOCS_EXPECTED_COMMIT ?? 'local'
const expectedImage = process.env.DOCS_EXPECTED_IMAGE ?? 'local'
const configuredOrigin = new URL(process.env.DOCS_E2E_BASE_URL ?? 'http://127.0.0.1:5054').origin
const stagingOrigin = 'https://fve.meiermade.net'
const expectedEnvironment = process.env.DOCS_EXPECTED_ENVIRONMENT ?? (expectedCommit === 'local' ? 'local' : configuredOrigin === stagingOrigin ? 'staging' : 'production')
const expectedCoreVersion = process.env.CORE_PACKAGE_VERSION ?? 'unreleased'
const expectedCliVersion = process.env.CLI_PACKAGE_VERSION ?? 'unreleased'
const canonicalOrigin = 'https://fve.meiermade.com'

test('health reports the deployed release identity', async ({ request }) => {
  const response = await request.get('/health')
  expect(response.status()).toBe(200)
  await expect(response.json()).resolves.toEqual({
    status: 'ok',
    environment: expectedEnvironment,
    origin: expectedEnvironment === 'local' ? 'http://127.0.0.1:5054' : expectedEnvironment === 'staging' ? stagingOrigin : canonicalOrigin,
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
  const example = await request.get('/examples/health')
  expect(example.status()).toBe(200)
  await expect(example.json()).resolves.toEqual({
    status: 'ok',
    commit: expectedCommit,
    image: process.env.EXPECTED_EXAMPLE_IMAGE ?? 'local',
  })
})

test('discovery endpoints advertise canonical public documentation', async ({ request }) => {
  const sitemap = await request.get('/sitemap.xml')
  expect(sitemap.status()).toBe(200)
  expect(sitemap.headers()['content-type']).toContain('application/xml')
  expect(await sitemap.text()).toContain(`<loc>${canonicalOrigin}/</loc>`)

  const robots = await request.get('/robots.txt')
  expect(robots.status()).toBe(200)
  expect(await robots.text()).toContain(`Sitemap: ${canonicalOrigin}/sitemap.xml`)
  const examples = await request.get('/examples/sitemap.xml')
  expect(examples.status()).toBe(200)
  expect(await examples.text()).toContain(`<loc>${configuredOrigin}/examples/application</loc>`)
})

test('production permits the Marketing login example while blocking scanner probes', async ({ request }) => {
  test.skip(expectedEnvironment !== 'production', 'The custom scanner WAF applies only to production')
  expect(configuredOrigin).toBe(canonicalOrigin)
  for (const path of ['/examples/marketing/login', '/examples/marketing/login?datastar=%7B%7D']) {
    const response = await request.get(path, { maxRedirects: 0 })
    expect(response.status(), path).toBe(200)
    expect(await response.text()).toContain('Log in to Geldos')
  }
  for (const path of ['/login', '/examples/marketing/login/', '/examples/marketing/login.php', '/examples/marketing/LOGIN', '/examples/marketing%2Flogin', '/admin', '/.env']) {
    const response = await request.get(path, { maxRedirects: 0 })
    expect(response.status(), path).toBe(403)
  }
})

test('essential rendered assets and critical read-only navigation work @cross-browser', async ({ page, browserName }) => {
  const expectPublishedAssets = expectedEnvironment !== 'local'
  const delivered = new Set<string>()
  page.on('response', response => {
    const url = new URL(response.url())
    if (url.origin === configuredOrigin && response.ok()) delivered.add(url.pathname)
  })
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
    if (expectPublishedAssets && (path === '/' || path === '/examples/application')) {
      const prefix = path === '/' ? '' : '/examples'
      const app = page.locator(`head script[type="module"][src^="${prefix}/js/app"]`)
      const css = page.locator(`head link[rel="stylesheet"][href^="${prefix}/css/output"]`)
      await expect(app).toHaveAttribute('src', new RegExp(`^${prefix}/js/app-[A-Z0-9]{8}\\.js$`))
      await expect(css).toHaveAttribute('href', new RegExp(`^${prefix}/css/output-[0-9a-f]{16}\\.css$`))
      await expect.poll(async () => delivered.has((await app.getAttribute('src'))!) && delivered.has((await css.getAttribute('href'))!)).toBe(true)
      if (path === '/examples/application') {
        await expect(page.locator('#home-balance-chart')).toHaveAttribute('data-ready', 'true')
        expect([...delivered]).toContainEqual(expect.stringMatching(/^\/examples\/js\/chart-[A-Z0-9]{8}\.js$/))
      }
    }
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
  const profileDocument = page.waitForResponse(response => response.request().isNavigationRequest() && response.frame() === page.mainFrame() && new URL(response.url()).pathname === '/examples/specification/profile')
  await page.getByRole('navigation', { name: 'Documentation', exact: true }).getByRole('link', { name: 'View profile', exact: true }).click()
  expect((await profileDocument).headers()['content-type']).toContain('text/html')
  await expect(page).toHaveURL(url => url.pathname === '/examples/specification/profile')
  await expect(specProduct(page).getByRole('heading', { name: 'General', exact: true, level: 1 })).toBeVisible()
  if (expectedEnvironment === 'staging' && browserName === 'chromium') {
    await page.goto('/examples/specification/accounts/view-accounts?appMode=1')
    const product = specProduct(page)
    const account = product.getByRole('heading', { name: 'Unassigned expense', exact: true, level: 1 })
    await product.getByRole('button', { name: 'Expand Expenses', exact: true }).click()
    await product.getByRole('link', { name: 'Unassigned expense', exact: true }).click()
    await expect(page).toHaveURL(/\/view-account\?.*resource=105.*appMode=1/)
    await page.goBack()
    await expect(product.getByRole('heading', { name: 'Accounts', exact: true, level: 1 })).toBeVisible()
    await page.goForward()
    await expect(account).toBeVisible()
    await expect(page).toHaveURL(/\/view-account\?.*resource=105.*appMode=1/)
    await product.locator('html').evaluate(el => { el.dataset.stagingDocument = 'retained' })
    await account.click()
    await page.keyboard.press('Escape')
    await expect(page.getByRole('link', { name: /^Expand View account ·/ })).toBeFocused()
    await expect(page).toHaveURL(url => url.pathname.endsWith('/view-account') && url.searchParams.get('resource') === '105' && !url.searchParams.has('appMode'))
    await expect(product.locator('html')).toHaveAttribute('data-staging-document', 'retained')
  }
  // Retain the engine-sensitive Select/focus check on both deployment origins.
  await page.goto('/components/select')
  const panel = page.locator('#components-select-default-panel-preview')
  const trigger = panel.getByRole('combobox', { name: 'Update frequency' })
  await trigger.click()
  const daily = panel.getByRole('option', { name: 'Daily', exact: true, selected: true })
  await expect(daily).toBeVisible()
  await expect(daily).toHaveAttribute('data-active', 'true')
  await expect(panel.getByRole('option', { name: 'Monthly', exact: true })).toHaveAttribute('aria-disabled', 'true')
  await page.keyboard.press('ArrowDown')
  await expect(panel.getByRole('option', { name: 'Weekly', exact: true })).toHaveAttribute('data-active', 'true')
  await page.keyboard.press('Enter')
  await expect(trigger).toContainText('Weekly')
  await expect(panel.locator('input[name="updateFrequencyDefault"]')).toHaveValue('weekly')
  await expect(trigger).toBeFocused()
  await trigger.click()
  await expect(panel.getByRole('option', { name: 'Weekly', exact: true, selected: true })).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(trigger).toBeFocused()
  for (const section of ['Components', 'Actions']) {
    const toggle = page.locator('#side-nav').getByLabel(`Toggle ${section} section`, { exact: true })
    if (await toggle.getAttribute('aria-expanded') !== 'true') await toggle.click()
  }
  await page.getByRole('link', { name: 'Button', exact: true }).first().click()
  await expect(page).toHaveURL(/\/components\/button$/)
  await expect(page.getByRole('heading', { name: 'Button', level: 1, exact: true })).toBeVisible()
  const identity = await page.request.get('/health')
  expect(identity.status()).toBe(200)
  const health = await identity.json()
  expect(health.commit).toBe(expectedCommit)
  expect(health.image).toBe(expectedImage)
  expect(health.environment).toBe(expectedEnvironment)
  expect(writes).toEqual([])
  expect(pageErrors).toEqual([])
})

test('staging rejects anonymous requests through Cloudflare Access', async () => {
  test.skip(expectedEnvironment !== 'staging', 'Access applies only to protected staging')
  expect(configuredOrigin).toBe(stagingOrigin)
  const response = await fetch(stagingOrigin, { redirect: 'manual' })
  expect([302, 403]).toContain(response.status)
  if (response.status === 302) expect(response.headers.get('location')).toContain('/cdn-cgi/access/login')
})
