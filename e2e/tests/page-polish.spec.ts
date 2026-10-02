import { expect, test } from '@playwright/test'

test('Page examples preserve readable content and a clear App-mode dock at 320px enlarged text @cross-browser', async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.setViewportSize({ width: 320, height: 1000 })
  await page.addInitScript(() => document.addEventListener('DOMContentLoaded', () => { document.documentElement.style.fontSize = '32px' }))
  const app = page.locator('#fve-app-mode-root')
  const dock = page.getByRole('navigation', { name: 'App mode controls', exact: true })
  await page.goto('/components/page-examples/dependency-graph?fveAppMode=app&fveAppFrame=page-workspace')
  const search = app.getByRole('searchbox', { name: 'Search dependencies', exact: true })
  await expect(search).toBeVisible()
  expect(await search.evaluate(el => el.closest('.fve-search')!.getBoundingClientRect().width)).toBeGreaterThan(220)
  expect((await search.boundingBox())!.width).toBeGreaterThan(128)
  await expect(app.getByRole('button', { name: 'Zoom in', exact: true }).locator('svg')).toHaveCSS('width', '32px')
  await expect(app.getByRole('button', { name: 'Zoom in', exact: true }).locator('svg')).toHaveCSS('height', '32px')
  await expect.poll(async () => (await app.boundingBox())!.y + (await app.boundingBox())!.height).toBeLessThan((await dock.boundingBox())!.y)
  const boxes = await dock.locator('a, button').evaluateAll(elements => elements.filter(element => element.checkVisibility()).map(element => element.getBoundingClientRect().toJSON()))
  for (const box of boxes) { expect(box.x).toBeGreaterThanOrEqual(0); expect(box.right).toBeLessThanOrEqual(320) }
  for (let i = 0; i < boxes.length; i++) for (let j = i + 1; j < boxes.length; j++) {
    expect(Math.min(boxes[i].right, boxes[j].right) - Math.max(boxes[i].left, boxes[j].left) > 1 && Math.min(boxes[i].bottom, boxes[j].bottom) - Math.max(boxes[i].top, boxes[j].top) > 1).toBe(false)
  }
  await dock.getByRole('button', { name: 'Move App mode controls to top', exact: true }).click()
  await expect.poll(async () => (await app.boundingBox())!.y).toBeGreaterThan((await dock.boundingBox())!.y + (await dock.boundingBox())!.height)
  await dock.getByRole('button', { name: 'Move App mode controls to bottom', exact: true }).click()
  await expect.poll(async () => (await app.boundingBox())!.y).toBe(0)

  await page.goto('/components/page-examples/messaging?fveAppMode=app&fveAppFrame=page-workspace')
  await expect(app.getByRole('heading', { level: 1 })).toHaveText('Beach weekend')
  await expect(app.getByRole('log')).toBeVisible()
  expect((await app.getByRole('log').boundingBox())!.height).toBeGreaterThan(100)
  await expect(app.getByText('Only participants in this conversation can see your reply.')).toHaveCount(0)
  await expect(app.getByRole('textbox', { name: 'Message', exact: true })).toHaveAttribute('rows', '1')

  await page.goto('/components/page-examples/scheduling?fveAppMode=app&fveAppFrame=page-workspace')
  const month = app.getByRole('region', { name: 'Monthly sessions scrollable dates', exact: true })
  await expect.poll(() => month.evaluate(el => el.scrollLeft)).toBeGreaterThan(0)
  await app.getByRole('link', { name: 'Week', exact: true }).click()
  const week = app.getByRole('region', { name: 'Weekly sessions scrollable times', exact: true })
  await expect.poll(() => week.evaluate(el => el.scrollLeft)).toBeGreaterThan(0)
  for (const width of [320, 1440, 390]) {
    await page.setViewportSize({ width, height: 1000 })
    await expect.poll(() => week.evaluate(el => {
      const selected = el.querySelector('[data-date="2026-09-17"]')!.getBoundingClientRect()
      const hours = el.querySelector('.fve-calendar-hours')!.getBoundingClientRect()
      return selected.left >= hours.right - 2 && selected.left < el.getBoundingClientRect().right
    })).toBe(true)
  }
  expect(errors).toEqual([])
})

test('Application examples use compact aligned context and a label-free accessible composer', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto('/components/page-examples/messaging?fveAppMode=app&fveAppFrame=page-workspace')
  const app = page.locator('#fve-app-mode-root')
  const bar = app.locator('[data-fve-page-top-bar]')
  const crumbs = bar.getByRole('navigation')
  const title = app.getByRole('heading', { level: 1 })
  expect((await bar.boundingBox())!.height).toBeLessThanOrEqual(49)
  const b = (await bar.boundingBox())!, c = (await crumbs.boundingBox())!
  expect(Math.abs(c.y + c.height / 2 - b.y - b.height / 2)).toBeLessThan(2)
  expect(Math.abs(c.x - (await title.boundingBox())!.x)).toBeLessThan(2)
  const input = app.getByRole('textbox', { name: 'Message', exact: true })
  await expect(input).toHaveAttribute('placeholder', 'Write a message…')
  await expect(input).toHaveAttribute('required', '')
  expect((await app.locator('label[for="message-compose"]').boundingBox())!.width).toBeLessThanOrEqual(1)
  await page.goto('/components/page-examples/account-management?destination=ledger-accounts')
  const shell = page.locator('#ledger-app-shell')
  await expect(shell.getByRole('button', { name: /Collapse navigation|Expand navigation/ })).toHaveCount(0)
  expect((await shell.locator('[data-fve-page-top-bar]').boundingBox())!.height).toBeLessThanOrEqual(49)
})

test('Account form actions share a bounded footer and API requests precede mobile parameters', async ({ page }) => {
  await page.goto('/components/page-examples/account-management?destination=ledger-create-account')
  const form = page.locator('#ledger-app-shell form')
  const create = form.getByRole('button', { name: 'Create account', exact: true })
  const cancel = form.getByRole('link', { name: 'Cancel', exact: true })
  await expect(create).toBeVisible()
  await expect(cancel).toBeVisible()
  expect((await create.boundingBox())!.width).toBeLessThan((await form.boundingBox())!.width / 2)
  expect(Math.abs((await create.boundingBox())!.y - (await cancel.boundingBox())!.y)).toBeLessThan(16)
  await cancel.click()
  await expect(page).toHaveURL(/destination=ledger-accounts/)
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/docs/previews/api-reference-render-view')
  const request = page.locator('[data-docs-code-panel="true"]:visible').filter({ hasText: 'Request' })
  await expect(request).toBeVisible()
  expect((await request.boundingBox())!.y).toBeLessThan((await page.locator('[data-parameter-location]').first().boundingBox())!.y)
  await expect(page.locator('[data-docs-code-panel="true"]:visible').filter({ hasText: 'Response 200' })).toHaveCount(1)
})
