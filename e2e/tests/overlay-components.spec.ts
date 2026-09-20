import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

for (const [width, scale] of [[1440, 1], [390, 1], [320, 2]]) {
  test(`Floating panel minimizes, dismisses, restores and remains bounded at ${width}px ${scale}x text @cross-browser`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 900 })
    await page.goto('/components/floating-panel')
    await page.evaluate(scale => { document.documentElement.style.fontSize = `${100 * scale}%` }, scale)
    const preview = page.locator('#components-floating-panel-panel-preview')
    const panel = preview.locator('#workspace-guide')
    await expect(panel).toBeVisible()
    await expect(panel).not.toHaveAttribute('role', 'dialog')
    await expect(page.locator('dialog[open]')).toHaveCount(0)
    await preview.locator('[data-fve-floating-panel-root]').evaluate(root => {
      ;(window as any).__floatingPanelStates = []
      root.addEventListener('fve-floating-panel-state', (event: Event) => {
        ;(window as any).__floatingPanelStates.push((event as CustomEvent).detail)
      })
    })
    const bounds = await panel.evaluate((element, preview) => {
      const panel = element.getBoundingClientRect()
      const boundary = document.querySelector(preview)!.getBoundingClientRect()
      return { left: panel.left, right: panel.right, top: panel.top, bottom: panel.bottom, boundary }
    }, '#components-floating-panel-panel-preview')
    expect(bounds.left).toBeGreaterThanOrEqual(bounds.boundary.left)
    expect(bounds.right).toBeLessThanOrEqual(bounds.boundary.right)
    expect(bounds.top).toBeGreaterThanOrEqual(bounds.boundary.top)
    expect(bounds.bottom).toBeLessThanOrEqual(bounds.boundary.bottom)
    expect(await panel.evaluate(element => element.scrollWidth - element.clientWidth)).toBeLessThanOrEqual(1)
    for (const action of await panel.getByRole('button').all()) {
      const actionBox = await action.boundingBox()
      expect(actionBox).not.toBeNull()
      expect(actionBox!.x).toBeGreaterThanOrEqual(bounds.left)
      expect(actionBox!.x + actionBox!.width).toBeLessThanOrEqual(bounds.right)
    }
    await preview.getByRole('button', { name: 'Minimize Workspace guide', exact: true }).click()
    const open = preview.getByRole('button', { name: 'Open Workspace guide', exact: true })
    await expect(open).toBeVisible()
    await expect(open).toBeFocused()
    await open.click()
    await expect(preview.getByRole('heading', { name: 'Workspace guide', exact: true })).toBeFocused()
    await preview.getByRole('button', { name: 'Dismiss Workspace guide', exact: true }).click()
    const restore = preview.getByRole('button', { name: 'Restore Workspace guide', exact: true })
    await expect(restore).toBeVisible()
    await expect(restore).toBeFocused()
    await restore.click()
    await expect(panel).toBeVisible()
    await expect(preview.getByRole('heading', { name: 'Workspace guide', exact: true })).toBeFocused()
    expect(await page.evaluate(() => (window as any).__floatingPanelStates)).toEqual([
      { id: 'workspace-guide', state: 'minimized' },
      { id: 'workspace-guide', state: 'open' },
      { id: 'workspace-guide', state: 'dismissed' },
      { id: 'workspace-guide', state: 'open' },
    ])
    for (const theme of ['light', 'dark']) {
      await page.evaluate(theme => {
        document.documentElement.classList.toggle('dark', theme === 'dark')
        document.documentElement.dataset.theme = theme
      }, theme)
      expect((await new AxeBuilder({ page }).include('#components-floating-panel-panel-preview').analyze()).violations).toEqual([])
      await preview.screenshot({ path: testInfo.outputPath(`floating-panel-${width}-${scale}-${theme}.png`) })
    }
  })
}

test('First steps composes Floating panel and every initial state has a recovery path @cross-browser', async ({ page }) => {
  await page.goto('/components/first-steps')
  const examples = page.locator('[data-docs-example="true"]')
  await expect(examples).toHaveCount(3)
  const open = page.locator('#components-first-steps-panel-preview')
  await expect(open.getByRole('heading', { name: 'First steps', exact: true })).toBeVisible()
  await expect(open).toContainText('Complete: Review imported accounts')
  await open.getByRole('button', { name: 'Minimize First steps', exact: true }).click()
  await expect(open.getByRole('button', { name: 'Open First steps', exact: true })).toBeVisible()
  const minimized = page.locator('#components-first-steps-minimized-panel-preview')
  await expect(minimized.getByRole('button', { name: 'Open First steps', exact: true })).toBeVisible()
  await minimized.getByRole('button', { name: 'Open First steps', exact: true }).click()
  await expect(minimized.getByRole('heading', { name: 'First steps', exact: true })).toBeVisible()
  const dismissed = page.locator('#components-first-steps-dismissed-panel-preview')
  await expect(dismissed.getByRole('button', { name: 'Restore First steps', exact: true })).toBeVisible()
  await dismissed.getByRole('button', { name: 'Restore First steps', exact: true }).click()
  await expect(dismissed.getByRole('heading', { name: 'First steps', exact: true })).toBeFocused()
})

test('Notification announces content, supports actions and dismisses without retaining hidden focus @cross-browser', async ({ page }) => {
  await page.goto('/components/notification')
  const simple = page.locator('#components-notification-panel-preview')
  const notification = simple.locator('#saved-notification')
  await expect(simple.getByRole('status')).toContainText('Changes saved')
  await expect(notification).toBeVisible()
  expect((await new AxeBuilder({ page }).include('#components-notification-panel-preview').analyze()).violations).toEqual([])
  await simple.locator('[data-fve-notification-region]').evaluate(region => {
    ;(window as any).__dismissedNotification = null
    region.addEventListener('fve-notification-dismiss', (event: Event) => {
      ;(window as any).__dismissedNotification = (event as CustomEvent).detail.id
    }, { once: true })
  })
  const dismiss = simple.getByRole('button', { name: 'Dismiss Changes saved', exact: true })
  await dismiss.focus()
  await dismiss.click()
  await expect(notification).toBeHidden()
  expect(await page.evaluate(() => (window as any).__dismissedNotification)).toBe('saved-notification')
  expect(await page.evaluate(() => !!document.activeElement?.closest('[data-fve-notification]'))).toBe(false)
  const actions = page.locator('#components-notification-actions-panel-preview')
  await expect(actions.getByRole('link', { name: 'Review workspace', exact: true })).toHaveAttribute('href', '/components/page-examples/operations-dashboard')
  await expect(actions.getByRole('button', { name: 'Decline', exact: true })).toBeVisible()
})

test('viewport First steps clears the App-mode dock and does not trap application focus @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/components/page-examples/operations-dashboard?state=setup&fveAppMode=app&fveAppFrame=page-workspace')
  const panel = page.locator('#fieldwork-first-steps')
  const dock = page.locator('[data-fve-app-mode-controls="true"]')
  await expect(panel).toBeVisible()
  await expect(dock).toBeVisible()
  await expect(dock.getByRole('combobox', { name: 'Review state' })).toContainText('Setup')
  const [panelBox, dockBox] = await Promise.all([panel.boundingBox(), dock.boundingBox()])
  expect(panelBox).not.toBeNull()
  expect(dockBox).not.toBeNull()
  expect(panelBox!.y + panelBox!.height).toBeLessThanOrEqual(dockBox!.y)
  await panel.getByRole('button', { name: 'Minimize Finish your workspace', exact: true }).click()
  await expect(page.getByRole('link', { name: 'View schedule', exact: true })).toBeVisible()
  await page.getByRole('link', { name: 'View schedule', exact: true }).focus()
  await expect(page.getByRole('link', { name: 'View schedule', exact: true })).toBeFocused()
})
