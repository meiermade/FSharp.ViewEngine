import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

test('SideNav slots distinguish links and menus and preserve avatar-sized profile content', async ({ page }) => {
  await page.goto('/components/side-nav')
  const preview = page.locator('#components-side-nav-slots-panel-preview')
  const settings = preview.getByRole('link', { name: 'Settings', exact: true })
  const profile = preview.getByRole('link', { name: 'Andrew Meier andrew@meiermade.com', exact: true })
  await expect(settings.locator('svg')).toHaveCount(1) // Leading gear only, no disclosure icon.
  await expect(profile.locator('svg')).toHaveCount(0)
  await expect(profile).toHaveAttribute('href', '/examples/application/profile')
  await expect(profile.getByText('Andrew Meier', { exact: true })).toHaveCSS('font-size', '14px')
  await expect(profile.getByText('andrew@meiermade.com', { exact: true })).toHaveCSS('font-size', '12px')
  const avatar = profile.getByText('AM', { exact: true }).locator('..')
  await expect(avatar).toHaveCSS('width', '40px')
  await expect(avatar).toHaveCSS('height', '40px')
  const workspace = preview.getByRole('button', { name: 'Choose workspace', exact: true })
  await expect(workspace.getByLabel('2 workspaces')).toBeVisible()
  await expect(workspace.locator('svg')).toHaveCount(2) // Leading workspace icon and menu chevron.
  await workspace.focus()
  await page.keyboard.press('Enter')
  await expect(preview.getByRole('menuitem', { name: 'Jordan', exact: true })).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(workspace).toBeFocused()
  await expect(workspace).toHaveAttribute('aria-expanded', 'false')
})

test('SideNav nested groups, badges, and item actions remain distinct @cross-browser', async ({ page }) => {
  await page.goto('/components/side-nav')
  const preview = page.locator('#components-side-nav-panel-preview')
  const navigation = preview.getByRole('navigation', { name: 'Workspace navigation' })
  const reporting = preview.locator('details').filter({ has: page.getByText('Documentation', { exact: true }) })
  await expect(reporting).toHaveAttribute('open', '')
  await expect(reporting.getByRole('link', { name: 'Button', exact: true })).toBeVisible()
  await expect(navigation.getByText('24', { exact: true })).toBeVisible()
  const accounts = navigation.getByRole('link', { name: 'Items', exact: true })
  await expect(accounts).toHaveAttribute('aria-current', 'page')
  const before = page.url()
  const pin = navigation.getByRole('button', { name: 'Pin items', exact: true })
  await pin.click()
  await expect(pin).toHaveAttribute('aria-pressed', 'true')
  expect(page.url()).toBe(before)
  expect((await new AxeBuilder({ page }).include('#components-side-nav-panel-preview').analyze()).violations).toEqual([])
})
