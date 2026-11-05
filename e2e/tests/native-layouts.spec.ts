import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

test('Native headers and description lists retain semantic parts, actions and narrow reflow @cross-browser', async ({ page }) => {
  await page.goto('/components/page-header')
  const header = page.locator('#components-page-header-panel-preview')
  await expect(header.getByRole('heading', { name: 'Project notes', level: 1, exact: true })).toBeVisible()
  await header.getByRole('button', { name: 'Refresh', exact: true }).click()
  await expect(header.getByRole('status')).toHaveText('Refreshes: 1')
  await page.setViewportSize({ width: 320, height: 1000 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  const lines = await header.getByRole('heading', { name: 'Project notes', exact: true }).evaluate(title =>
    title.getBoundingClientRect().height / parseFloat(getComputedStyle(title).lineHeight))
  expect(lines).toBeLessThanOrEqual(2)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)

  await page.goto('/components/section-header')
  const section = page.locator('#components-sectionHeaderWithActions-panel-preview')
  await expect(section.getByRole('heading', { name: 'Recent activity', level: 2, exact: true })).toBeVisible()
  await expect(section.getByRole('button', { name: 'Export', exact: true })).toBeDisabled()

  await page.goto('/components/description-list')
  const details = page.locator('#components-description-list-panel-preview')
  await expect(details.locator('dl > div > dt')).toHaveCount(6)
  await expect(details.locator('dl > div > dd')).toHaveCount(6)
  await expect(details.getByRole('link', { name: 'Andrew Meier' })).toHaveAttribute('href', '/examples/application/profile')
  expect((await new AxeBuilder({ page }).include('#components-description-list-panel-preview').analyze()).violations).toEqual([])

  await page.goto('/components/page-top-bar')
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  const bar = page.locator('#components-page-top-bar-shell-panel-preview')
  const breadcrumb = (await bar.getByRole('navigation', { name: 'Shell breadcrumb' }).boundingBox())!
  const action = (await bar.getByRole('link', { name: 'View profile', exact: true }).boundingBox())!
  const frame = (await bar.boundingBox())!
  expect(action.y).toBeGreaterThanOrEqual(breadcrumb.y + breadcrumb.height)
  expect(action.x + action.width).toBeLessThanOrEqual(frame.x + frame.width)
})
