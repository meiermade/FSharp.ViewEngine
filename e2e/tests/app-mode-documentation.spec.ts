import { test, expect } from '@playwright/test'

// Fullscreen review belongs to the authored Specification; these presentation components stay static.
test('Browser and Phone present caller content without adding fullscreen controls', async ({ page }) => {
  await page.goto('/components/browser')
  await expect(page.locator('[data-fve-browser-address="true"]')).toContainText('fve.meiermade.com/components/browser')
  await expect(page.getByRole('link', { name: /in App mode$/ })).toHaveCount(0)
  await page.goto('/components/phone')
  await expect(page.locator('#components-phone [data-fve-phone="true"]')).toBeVisible()
  await expect(page.getByRole('link', { name: /in App mode$/ })).toHaveCount(0)
})
