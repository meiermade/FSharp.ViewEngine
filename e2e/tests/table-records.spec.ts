import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

test('Table selection publishes caller keys and supports keyboard and select all @cross-browser', async ({ page }) => {
  await page.goto('/components/table')
  const selection = page.locator('#team-member-selection')
  await selection.evaluate(element => {
    element.addEventListener('fve-table-selection-change', event => {
      (window as any).selectedKeys = (event as CustomEvent).detail.keys
    })
  })
  const all = selection.getByRole('checkbox', { name: 'Select all rows on this page' })
  const alex = selection.getByRole('checkbox', { name: 'Select Alex Morgan', exact: true })
  await alex.focus()
  await alex.press('Space')
  await expect(alex).toBeChecked()
  await expect.poll(() => page.evaluate(() => (window as any).selectedKeys)).toEqual(['1'])
  await expect(all).toBeChecked({ indeterminate: true })
  await all.check()
  await expect(selection.locator('tbody input:checked')).toHaveCount(4)
  await all.uncheck()
  await expect(selection.locator('input:checked')).toHaveCount(0)
})

test('financial table preserves native selection while recomposing into accessible mobile records', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto('/examples/application/accounts')
  const table = page.getByRole('table', { name: 'Account hierarchy', exact: true })
  const selected = table.getByRole('checkbox', { name: 'Select Unassigned expense', exact: true })
  await selected.check()
  await expect(selected).toHaveAttribute('value', '105')
  const identity = await selected.elementHandle()
  await page.setViewportSize({ width: 390, height: 1000 })
  await page.locator('html').evaluate(el => el.style.fontSize = '200%')
  await expect(selected).toBeChecked()
  expect(await selected.evaluate((element, original) => element === original, identity)).toBe(true)
  const row = table.getByRole('row').filter({ has: page.getByRole('checkbox', { name: 'Select Unassigned expense', exact: true }) })
  await expect(row.getByRole('rowheader')).toContainText('Unassigned expense')
  await selected.scrollIntoViewIfNeeded()
  const label = row.locator('label')
  const target = await label.boundingBox()
  expect(target!.width).toBeGreaterThanOrEqual(24)
  expect(target!.height).toBeGreaterThanOrEqual(24)
  await label.click()
  await expect(selected).not.toBeChecked()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  expect((await new AxeBuilder({ page }).include('[data-fve-table]').analyze()).violations).toEqual([])
})
