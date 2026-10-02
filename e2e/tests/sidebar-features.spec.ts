import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

test('SideNav nested groups, badges, and item actions remain distinct @cross-browser', async ({ page }) => {
  await page.goto('/components/side-nav')
  const preview = page.locator('#components-side-nav-panel-preview')
  const navigation = preview.getByRole('navigation', { name: 'Ledger primary navigation' })
  const reporting = preview.locator('details').filter({ has: page.getByText('Reporting', { exact: true }) })
  await expect(reporting).toHaveAttribute('open', '')
  await expect(reporting.getByRole('link', { name: 'Reports', exact: true })).toBeVisible()
  await expect(navigation.getByText('6', { exact: true })).toBeVisible()
  const accounts = navigation.getByRole('link', { name: 'Accounts', exact: true })
  await expect(accounts).toHaveAttribute('aria-current', 'page')
  const before = page.url()
  const pin = navigation.getByRole('button', { name: 'Pin Accounts', exact: true })
  await pin.click()
  await expect(pin).toHaveAttribute('aria-pressed', 'true')
  expect(page.url()).toBe(before)
  expect((await new AxeBuilder({ page }).include('#components-side-nav-panel-preview').analyze()).violations).toEqual([])
})
