import { expect, test } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const crossBrowser = { tag: '@cross-browser' }

test('flat component catalog preserves enhanced navigation and history', crossBrowser, async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components')
  await page.evaluate(() => { (window as any).catalogSession = 'retained' })
  const cards = page.locator('#page-content .docs-catalog-grid')
  for (const [name, path] of [['Button', '/components/button'], ['Page top bar', '/components/page-top-bar'], ['Field group', '/components/field-group']]) {
    await expect(cards.locator(`a[href="${path}"]`)).toHaveAccessibleName(new RegExp(`^${name} `))
  }
  await expect(cards.locator('a[href="/components/primitives"], a[href="/components/application"], a[href="/docs"]')).toHaveCount(0)
  await cards.locator('a[href="/components/button"]').click()
  await expect(page.getByRole('heading', { name: 'Button', level: 1, exact: true })).toBeVisible()
  await page.goBack()
  await expect(page).toHaveURL('/components')
  await expect(cards.locator('a[href="/components/button"]')).toBeVisible()
  expect(await page.evaluate(() => (window as any).catalogSession)).toBe('retained')
  expect(errors).toEqual([])
})

test('search reaches a dedicated component and Examples exposes exactly three templates', crossBrowser, async ({ page }) => {
  await page.goto('/components')
  await page.getByRole('button', { name: 'Search documentation', exact: true }).click()
  const dialog = page.getByRole('dialog', { name: 'Search documentation', exact: true })
  await dialog.getByRole('combobox').fill('Page top bar')
  const result = dialog.locator('[data-fve-command-item][href="/components/page-top-bar"]')
  await expect(result).toBeVisible()
  await result.focus()
  await page.keyboard.press('Enter')
  await expect(page.getByRole('heading', { name: 'Page top bar', level: 1, exact: true })).toBeVisible()
  await page.getByRole('link', { name: 'Examples', exact: true }).click()
  const cards = page.locator('a[data-example-template]')
  await expect(cards).toHaveCount(3)
  for (const path of ['/examples/application', '/examples/specification', '/examples/api-documentation']) {
    await expect(cards.and(page.locator(`a[href="${path}"]`))).toBeVisible()
  }
})

test('component and example indexes reflow with narrow enlarged text', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 960 })
  for (const path of ['/components', '/examples']) {
    await page.goto(path)
    await page.evaluate(() => {
      document.documentElement.classList.add('dark')
      document.documentElement.style.fontSize = '200%'
    })
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
    expect((await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']).analyze()).violations).toEqual([])
  }
  await page.getByRole('button', { name: 'Open navigation', exact: true }).click()
  await expect(page.getByRole('link', { name: 'Examples', exact: true })).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(page.getByRole('button', { name: 'Open navigation', exact: true })).toBeFocused()
})
