import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

test('App shell teaches layout while populated workflows live in Page examples', async ({ page }) => {
  await page.goto('/components/app-shell', { waitUntil: 'domcontentloaded' })
  const sidebar = page.locator('#layout-sidebar')
  await expect(sidebar.getByText('Page content', { exact: true })).toBeVisible()
  await expect(sidebar.getByRole('table')).toHaveCount(0)
  await expect(sidebar.getByRole('searchbox', { name: 'Search accounts', exact: true })).toHaveCount(0)
  await page.evaluate(() => { (window as any).__shellDocument = 'preserved' })
  await sidebar.getByRole('navigation', { name: 'Constrained workspace navigation', exact: true }).getByRole('link', { name: 'Projects', exact: true }).click()
  await expect(page).toHaveURL('/components/app-shell?section=projects')
  await expect(sidebar.getByRole('heading', { name: 'Projects', exact: true })).toBeVisible()
  await expect(sidebar.getByRole('navigation', { name: 'Constrained workspace navigation', exact: true }).getByRole('link', { name: 'Projects', exact: true })).toHaveAttribute('aria-current', 'page')
  expect(await page.evaluate(() => (window as any).__shellDocument)).toBe('preserved')
  await page.goBack()
  await expect(sidebar.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible()

  await page.getByRole('link', { name: 'Account management', exact: true }).last().click()
  await expect(page).toHaveURL('/components/page-examples/account-management')
  const workflow = page.locator('#ledger-app-shell')
  await expect(workflow.getByRole('searchbox', { name: 'Search accounts', exact: true })).toBeVisible()
  await workflow.getByRole('link', { name: 'Create', exact: true }).click()
  await expect(page).toHaveURL(/page-examples\/account-management\?destination=ledger-create-account$/)
  await expect(workflow.getByRole('heading', { name: 'Create account', exact: true })).toBeVisible()
})

test('Application and Documentation Page examples navigation groups have independent identities', async ({ page }) => {
  await page.goto('/components/page-examples/account-management')
  const application = page.locator('#nav-fsharp-viewengine-components-application-page-examples')
  const documentation = page.locator('#nav-fsharp-viewengine-components-documentation-page-examples')
  await expect(application).toHaveAttribute('aria-expanded', 'true')
  await expect(documentation).toHaveAttribute('aria-expanded', 'false')
  expect(await application.getAttribute('aria-controls')).not.toBe(await documentation.getAttribute('aria-controls'))
  await application.click()
  await expect(application).toHaveAttribute('aria-expanded', 'false')
  await expect(documentation).toHaveAttribute('aria-expanded', 'false')
  expect(await page.locator('#side-nav').getByRole('button', { name: 'Toggle Integration examples section', exact: true }).count()).toBe(0)
})

test('Page examples keep supporting documentation focused on their building blocks', async ({ page }) => {
  await page.goto('/components/page-examples/account-management')
  await expect(page.getByRole('heading', { name: 'Explore the pages', exact: true })).toHaveCount(0)
  await expect(page.getByText('Explore the dashboard, accounts, matching details', { exact: false })).toHaveCount(0)
  await expect(page.getByRole('heading', { name: 'Built with', exact: true })).toBeVisible()

  await page.goto('/components/page-examples/dependency-graph')
  await expect(page.getByText('Use the URL-backed review tabs', { exact: false })).toHaveCount(0)
  await expect(page.getByText('The copied example contains', { exact: false })).toHaveCount(0)
  await expect(page.getByText('Graphs and financial series', { exact: false })).toHaveCount(0)
  await expect(page.getByRole('link', { name: 'Example source and host integration', exact: true })).toHaveCount(0)
  await expect(page.getByRole('heading', { name: 'Built with', exact: true })).toBeVisible()

  const previewPanel = page.locator('.spec-example-preview').filter({ has: page.locator('[data-page-example-preview="true"]') })
  await expect(previewPanel).toHaveCSS('border-top-width', '0px')
  await expect(page.getByRole('navigation', { name: 'Example review state' })).toBeVisible()

  for (const [slug, title, components] of [
    ['dependency-graph', 'Dependency graph', ['App shell', 'Page', 'Input', 'Description list', 'Status']],
    ['execution-detail', 'Execution detail', ['App shell', 'Page', 'Table', 'Description list', 'Notice']],
    ['financial-reporting', 'Financial reporting', ['App shell', 'Page', 'Metric', 'Table']],
    ['messaging', 'Messaging', ['App shell', 'Page', 'Side nav', 'Textarea']],
    ['operations-dashboard', 'Operations dashboard', ['App shell', 'Page', 'Metric', 'Table', 'First steps']],
    ['scheduling', 'Scheduling', ['App shell', 'Page', 'Calendar', 'Description list']],
    ['media-management', 'Media management', ['App shell', 'Page', 'Media library', 'Input', 'Textarea', 'File selection']],
  ] as const) {
    await page.goto(`/components/page-examples/${slug}`)
    const buildingBlocks = page.getByRole('navigation', { name: `${title} building blocks`, exact: true })
    await expect(buildingBlocks.getByRole('link')).toHaveText(components)
  }
})

for (const [width, scale, dark] of [[1440, 1, false], [390, 1, true], [320, 2, true]] as const) {
  test(`minimal shell layouts reflow at ${width}px ${scale}x`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 1000 })
    // These native layouts do not depend on the optional asynchronously loaded Tailwind Plus CDN.
    await page.goto('/components/app-shell', { waitUntil: 'domcontentloaded' })
    if (dark) {
      await page.getByRole('button', { name: 'Choose color theme' }).click()
      await page.getByRole('menuitemradio', { name: 'Dark', exact: true }).click()
    }
    await page.evaluate(scale => { document.documentElement.style.fontSize = `${16 * scale}px` }, scale)
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    const bottom = page.locator('#layout-bottom-bottom')
    if (width < 768) {
      await bottom.scrollIntoViewIfNeeded()
      await expect(bottom.getByRole('link')).toHaveCount(3)
      await expect(bottom.getByRole('tab')).toHaveCount(0)
      const bounds = await bottom.getByRole('link').evaluateAll(links => links.map(link => {
        const box = link.getBoundingClientRect()
        return { left: box.left, right: box.right }
      }))
      for (const boundsOfLink of bounds) {
        expect(boundsOfLink.left).toBeGreaterThanOrEqual(0)
        expect(boundsOfLink.right).toBeLessThanOrEqual(width)
      }
      await page.locator('#layout-sidebar').getByRole('button', { name: 'Open navigation', exact: true }).click()
      await expect(page.locator('#layout-sidebar').getByRole('navigation', { name: 'Constrained workspace navigation', exact: true })).toBeVisible()
      await page.keyboard.press('Escape')
      await expect(page.locator('#layout-sidebar').getByRole('button', { name: 'Open navigation', exact: true })).toBeFocused()
    }
    const results = await new AxeBuilder({ page }).analyze()
    expect(results.violations).toEqual([])
    if (testInfo.project.name === 'chromium') {
      await testInfo.attach(`app-shell-${width}-${scale}`, { body: await page.screenshot({ fullPage: true }), contentType: 'image/png' })
    }
  })
}
