import { test, expect } from '@playwright/test'

// Review controls are opt-in; default frames still present only caller content.
test('Browser and Phone default previews do not add review controls', async ({ page }) => {
  await page.goto('/components/browser')
  const browser = page.locator('#components-browser-panel-preview')
  await expect(browser.locator('[data-fve-browser-address="true"]')).toContainText('fve.meiermade.com/components/browser')
  await expect(browser.getByText('Content preview', { exact: true })).toBeVisible()
  await expect(browser.getByRole('link')).toHaveCount(0)
  await expect(browser.getByRole('button')).toHaveCount(0)
  await page.goto('/components/phone')
  const phone = page.locator('#components-phone-panel-preview')
  await expect(phone.locator('[data-fve-phone="true"]')).toBeVisible()
  await expect(phone.getByText('Confirm arrival time.', { exact: true })).toBeVisible()
  await expect(phone.getByRole('link')).toHaveCount(0)
  await expect(phone.getByRole('button')).toHaveCount(0)
})
