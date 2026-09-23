import { expect, test } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const families = [
  ['Primitives', '/components/primitives'],
  ['Application', '/components/application'],
  ['Documentation', '/docs'],
] as const

const crossBrowser = { tag: '@cross-browser' }

test.beforeEach(async ({ page }) => {
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )
})

test('delivered catalog families expose Application examples and preserve morph history', crossBrowser, async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components')
  const cards = page.locator('#page-content .docs-catalog-grid')
  await expect(cards.getByRole('link')).toHaveCount(3)
  for (const [name, path] of families) {
    await expect(cards.getByRole('link', { name: new RegExp(`^${name} `) })).toHaveAttribute('href', path)
    await expect(page.getByRole('button', { name: `Toggle ${name} section`, exact: true })).toBeVisible()
  }
  const componentsToggle = page.getByRole('button', { name: 'Toggle FSharp.ViewEngine.Components section', exact: true })
  await expect(componentsToggle).toHaveAttribute('aria-expanded', 'true')
  await componentsToggle.click()
  for (const [name] of families) {
    await expect(page.getByRole('button', { name: `Toggle ${name} section`, exact: true })).toBeHidden()
  }
  await expect(page.getByRole('button', { name: 'Toggle Project section', exact: true })).toBeVisible()
  await componentsToggle.click()
  for (const [name] of families) {
    await expect(page.getByRole('button', { name: `Toggle ${name} section`, exact: true })).toBeVisible()
  }
  await page.evaluate(() => { (window as unknown as { catalogSession: string }).catalogSession = 'retained' })
  await cards.getByRole('link', { name: /^Application / }).click()
  await expect(page).toHaveURL('/components/application')
  await expect(page.getByRole('heading', { name: 'Application', level: 1, exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Toggle Application section', exact: true })).toHaveAttribute('aria-expanded', 'true')
  await expect(componentsToggle).toHaveAttribute('aria-expanded', 'true')
  await expect(page.locator('#page-content .docs-catalog-card')).toHaveCount(21)
  await expect(page.locator('#page-content .docs-catalog-card[href="/components/page-examples/account-management"]')).toContainText('Account management')
  await expect(page.locator('#page-content .docs-catalog-card[href="/components/calendar"]')).toHaveCount(0)
  await expect(page.locator('#page-content .docs-catalog-card[href="/components/media-library"]')).toContainText('Media library')
  await expect(page.getByText(/connects the financial workspace, collections, matching record details, forms, actions, reports and settings/)).toBeVisible()
  await page.locator('#page-content .docs-catalog-card[href="/components/collection"]').click()
  await expect(page.getByRole('heading', { name: 'Collection', level: 1, exact: true })).toBeVisible()
  await expect(page.getByRole('navigation', { name: 'Breadcrumb', exact: true })).toContainText('Application')
  await expect(page.locator('#components-collection [role="tabpanel"]:visible')).toHaveCount(1)
  await page.goBack()
  await expect(page).toHaveURL('/components/application')
  await expect(page.getByRole('heading', { name: 'Application', level: 1, exact: true })).toBeVisible()
  await page.goBack()
  await expect(page).toHaveURL('/components')
  expect(await page.evaluate(() => (window as unknown as { catalogSession: string }).catalogSession)).toBe('retained')
  expect(errors).toEqual([])
})

test('catalog search and pagers use family destinations without pretending unfinished demos exist', crossBrowser, async ({ page }) => {
  await page.goto('/components/application')
  await page.getByRole('button', { name: 'Search documentation', exact: true }).click()
  const dialog = page.getByRole('dialog', { name: 'Search documentation', exact: true })
  await dialog.getByRole('searchbox').fill('Documentation')
  const result = dialog.locator('[data-docs-search-entry][href="/docs"]')
  await expect(result).toBeVisible()
  await result.focus()
  await page.keyboard.press('Enter')
  await expect(page).toHaveURL('/docs')
  await expect(page.getByRole('heading', { name: 'Documentation', level: 1, exact: true })).toBeVisible()
  await page.reload()
  await expect(page.getByRole('button', { name: 'Toggle Documentation section', exact: true })).toHaveAttribute('aria-expanded', 'true')
})

for (const [dark, width, scale] of [[false, 1440, 1], [true, 390, 1], [false, 320, 2]] as const) {
    test(`catalog indexes and navigation remain accessible: ${dark ? 'dark' : 'light'} ${width}px ${scale}x`, async ({ page }) => {
      await page.setViewportSize({ width, height: 960 })
      for (const [name, path] of [['Components', '/components'], ...families]) {
        await page.goto(path)
        await page.evaluate(({ dark, scale }) => {
          document.documentElement.classList.toggle('dark', dark)
          document.documentElement.style.fontSize = `${scale * 100}%`
        }, { dark, scale })
        await expect(page.getByRole('heading', { name, level: 1, exact: true })).toBeVisible()
        await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
        expect((await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']).analyze()).violations, `${name}/${dark}/${width}/${scale}`).toEqual([])
      }
      if (width < 1000) {
        await page.getByRole('button', { name: 'Open navigation', exact: true }).click()
        for (const [name] of families) {
          const toggle = page.getByRole('button', { name: `Toggle ${name} section`, exact: true })
          await toggle.scrollIntoViewIfNeeded()
          await expect(toggle).toBeVisible()
        }
        await page.keyboard.press('Escape')
        await expect(page.getByRole('button', { name: 'Open navigation', exact: true })).toBeFocused()
      }
    })
}
