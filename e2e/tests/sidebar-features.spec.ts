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

test('AppShell icon rail retains names and survives mobile navigation state changes @cross-browser', async ({ page }) => {
  await page.goto('/components/app-shell')
  const shell = page.locator('#layout-sidebar')
  const sideNav = shell.locator('#layout-sidebar-navigation')
  const collapse = sideNav.getByRole('button', { name: 'Collapse navigation' })
  const expandedWidth = (await sideNav.boundingBox())!.width
  await collapse.click()
  await expect(sideNav).toHaveAttribute('data-fve-collapsed', 'true')
  await expect(sideNav.getByRole('button', { name: 'Expand navigation' })).toBeVisible()
  await expect.poll(async () => (await sideNav.boundingBox())!.width).toBeLessThan(expandedWidth)
  await expect(sideNav.getByRole('link', { name: 'Dashboard', exact: true })).toHaveAttribute('title', 'Dashboard')

  await page.setViewportSize({ width: 390, height: 844 })
  await expect(sideNav.getByRole('button', { name: 'Expand navigation' })).toBeHidden()
  const open = shell.getByRole('button', { name: 'Open navigation' })
  await open.click()
  await expect(sideNav).toHaveAttribute('role', 'dialog')
  await expect(sideNav).toHaveAttribute('aria-modal', 'true')
  await expect(sideNav.getByRole('link', { name: 'Dashboard', exact: true })).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(open).toBeFocused()

  await page.setViewportSize({ width: 1280, height: 900 })
  await expect(sideNav).toHaveAttribute('data-fve-collapsed', 'true')
  await sideNav.getByRole('button', { name: 'Expand navigation' }).click()
  await expect(sideNav).toHaveAttribute('data-fve-collapsed', 'false')
})
