import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

// Include ancestor scrolling: selecting a lower row or using the header may legitimately scroll it.
const tableY = async (page: import('@playwright/test').Page) => page.locator('table:visible').first().evaluate(table => {
  let y = table.getBoundingClientRect().y
  for (let parent = table.parentElement; parent; parent = parent.parentElement) y += parent.scrollTop
  return y
})

test('collection notifications never shift tables and are delivered once @cross-browser', async ({ page }) => {
  for (const scenario of [
    { kind: 'accounts', record: 'Unassigned expense', width: 1440, color: 'light', scale: '100%', title: '1 account deleted' },
    { kind: 'transactions', record: 'Office supplies', width: 320, color: 'dark', scale: '200%', title: '1 transaction deleted' },
  ] as const) {
    await page.setViewportSize({ width: scenario.width, height: 1000 })
    await page.emulateMedia({ colorScheme: scenario.color })
    await page.goto(`/examples/application/${scenario.kind}`)
    await page.locator('html').evaluate((el, scale) => { el.style.fontSize = scale; el.dataset.notificationDocument = 'retained' }, scenario.scale)
    const restore = page.getByRole('button', { name: 'Restore records', exact: true })
    await expect(restore).toBeDisabled()
    expect((await restore.boundingBox())!.height).toBe(scenario.scale === '200%' ? 64 : 32)
    const before = await tableY(page)
    await page.getByRole('checkbox', { name: `Select ${scenario.record}`, exact: true }).check()
    await page.getByRole('button', { name: 'Delete selected', exact: true }).click()
    await page.getByRole('dialog', { name: `Delete 1 ${scenario.kind.slice(0, -1)}?`, exact: true })
      .getByRole('button', { name: 'Delete', exact: true }).click()
    const region = page.getByRole('region', { name: 'Collection notifications', exact: true })
    const notification = region.locator('[data-fve-notification]')
    await expect(notification.getByRole('status')).toContainText(scenario.title)
    await expect(notification).toHaveAttribute('data-fve-notification-ttl', '5000')
    await expect(region).toHaveCSS('position', 'fixed')
    await expect(restore).toBeEnabled()
    expect(await tableY(page)).toBeCloseTo(before, 0)
    expect(await notification.evaluate(el => el.contains(document.activeElement))).toBe(false)
    expect((await new AxeBuilder({ page }).include('#template-collection-notifications').analyze()).violations).toEqual([])
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    const firstId = await notification.getAttribute('id')
    await page.mouse.move(0, 0)
    await expect(notification).toHaveCount(0, { timeout: 8000 })
    expect(await tableY(page)).toBeCloseTo(before, 0)
    await restore.click()
    await expect(notification.getByRole('status')).toContainText('Records restored')
    expect(await notification.getAttribute('id')).not.toBe(firstId)
    await expect(restore).toBeDisabled()
    expect(await tableY(page)).toBeCloseTo(before, 0)
    await notification.getByRole('button', { name: 'Dismiss Records restored', exact: true }).click()
    await expect(notification).toHaveCount(0)
    expect(await tableY(page)).toBeCloseTo(before, 0)
    await expect(page.locator('html')).toHaveAttribute('data-notification-document', 'retained')
    const navigation = page.getByRole('button', { name: 'Open navigation', exact: true })
    if (await navigation.isVisible()) await navigation.click()
    await page.getByRole('link', { name: 'Home', exact: true }).first().click()
    await expect(page.getByRole('heading', { name: 'Home', exact: true })).toBeVisible()
    await page.goBack()
    await expect(page.getByRole('heading', { name: scenario.kind === 'accounts' ? 'Accounts' : 'Transactions', exact: true })).toBeVisible()
    await expect(notification).toHaveCount(0)
    await page.reload()
    await expect(notification).toHaveCount(0)
  }
})

test('Specification delivers action feedback only inside its product document', async ({ page }) => {
  await page.goto('/examples/specification/transactions/view-transactions')
  const product = page.frameLocator('#spec-product-frame')
  const office = product.getByRole('checkbox', { name: 'Select Office supplies', exact: true })
  await office.check()
  await product.getByRole('button', { name: 'Delete selected', exact: true }).click()
  await product.getByRole('dialog', { name: 'Delete 1 transaction?', exact: true }).getByRole('button', { name: 'Delete', exact: true }).click()
  const notification = product.locator('[data-fve-notification]')
  await expect(notification.getByRole('status')).toContainText('1 transaction deleted')
  await expect(page.locator('[data-fve-notification]')).toHaveCount(0)
  await expect(office).toHaveCount(0)
  await page.reload()
  await expect(notification).toHaveCount(0)
  await expect(office).toHaveCount(0)
  await product.getByRole('button', { name: 'Restore records', exact: true }).click()
  await expect(notification.getByRole('status')).toContainText('Records restored')
  await expect(office).toBeVisible()
})

test('a genuinely stale selection produces a persistent dismissible notification without moving the table @cross-browser', async ({ page }) => {
  await page.goto('/examples/application/transactions')
  const before = await tableY(page)
  await page.getByRole('checkbox', { name: 'Select Office supplies', exact: true }).check()
  const other = await page.context().newPage()
  try {
    await other.goto('/examples/application/transactions/203/delete')
    await other.getByRole('dialog', { name: 'Delete Office supplies?', exact: true }).getByRole('button', { name: 'Delete', exact: true }).click()
    await expect(other.locator('[data-fve-notification]').getByRole('status')).toContainText('1 transaction deleted')
  } finally { await other.close() }
  await page.bringToFront()
  await page.getByRole('button', { name: 'Delete selected', exact: true }).click()
  const notification = page.locator('[data-fve-notification]')
  await expect(notification.getByRole('alert')).toContainText('Choose available records')
  await expect(notification).toHaveAttribute('data-fve-notification-persistent', 'true')
  expect(await notification.evaluate(el => el.contains(document.activeElement))).toBe(false)
  expect(await tableY(page)).toBeCloseTo(before, 0)
  await page.mouse.move(0, 0)
  await page.waitForTimeout(5500) // Longer than success lifetime; errors must not expire.
  await expect(notification).toBeVisible()
  await notification.getByRole('button', { name: 'Dismiss Choose available records', exact: true }).click()
  await expect(notification).toHaveCount(0)
  expect(await tableY(page)).toBeCloseTo(before, 0)
  await page.reload()
  await expect(notification).toHaveCount(0)
  await expect(page.getByRole('checkbox', { name: 'Select Client payment', exact: true })).toBeVisible()
})
