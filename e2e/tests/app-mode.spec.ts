import { test, expect } from '@playwright/test'

test('App mode expands the connected Ledger workflow without Docs chrome and preserves review navigation @cross-browser', async ({ page }) => {
  await page.goto('/components/page-examples/account-management?destination=ledger-accounts')
  const documentRequests: string[] = []
  page.on('request', request => {
    if (request.resourceType() === 'document') documentRequests.push(request.url())
  })
  await page.evaluate(() => { (window as typeof window & { appModeDocument?: string }).appModeDocument = 'retained' })

  const frame = page.locator('[data-fve-app-mode-frame-id="ledger-workflow"]')
  await expect(frame).toBeVisible()
  await frame.getByRole('button', { name: 'Open Ledger account workflow in App mode' }).click()

  const root = page.locator('[data-fve-app-mode-root="true"]')
  const controls = page.locator('[data-fve-app-mode-controls="true"]')
  await expect(root).toBeVisible()
  await expect(root).toHaveAttribute('data-fve-app-mode-frame', 'ledger-workflow')
  await expect(controls).toBeVisible()
  await expect(page.locator('.spec-shell')).not.toBeVisible()
  await expect(page).toHaveURL(/fveAppMode=app.*fveAppFrame=ledger-workflow/)
  await expect.poll(() => page.evaluate(() => (window as typeof window & { appModeDocument?: string }).appModeDocument)).toBe('retained')

  await controls.getByRole('link', { name: 'Next: Operating checking' }).click()
  await expect(root).toContainText('Operating checking')
  await expect(page).toHaveURL(/destination=ledger-account-2048.*fveAppMode=app/)
  await expect.poll(() => page.evaluate(() => (window as typeof window & { appModeDocument?: string }).appModeDocument)).toBe('retained')

  const moveDock = controls.getByRole('button', { name: 'Move App mode controls to top' })
  await expect(moveDock.locator('path')).toHaveAttribute('d', /^M10\.53 4\.47/)
  await moveDock.click()
  await expect(controls).toHaveAttribute('data-fve-app-dock', 'top')
  await expect(controls.getByRole('button', { name: 'Move App mode controls to bottom' }).locator('path')).toHaveAttribute('d', /^M9\.47 15\.53/)

  await controls.getByRole('link', { name: 'Exit App mode' }).click()
  await expect(page).not.toHaveURL(/fveAppMode=/)
  await expect(page.locator('.spec-shell')).toBeVisible()
  await expect(page.locator('[data-fve-app-mode-root="true"]')).toHaveCount(0)
  await expect.poll(() => page.evaluate(() => (window as typeof window & { appModeDocument?: string }).appModeDocument)).toBe('retained')
  expect(documentRequests).toEqual([])
})
