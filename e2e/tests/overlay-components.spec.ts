import { test, expect } from '../fixture'
import { delayLocalNavigation } from './helpers/navigation-delay'
import AxeBuilder from '@axe-core/playwright'

for (const [width, scale] of [[1440, 1], [390, 1], [320, 2]]) {
  test(`Floating panel minimizes, dismisses, restores and remains bounded at ${width}px ${scale}x text`, async ({ page }, testInfo) => {
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

test('Tooltip supplements pointer and keyboard triggers without taking focus @cross-browser', async ({ page }) => {
  await page.goto('/components/tooltip')
  const preview = page.locator('#components-tooltip-panel-preview')
  const trigger = preview.getByRole('button', { name: 'Archive', exact: true })
  const tooltip = preview.getByRole('tooltip')
  await expect(trigger).toHaveAccessibleDescription('Moves the account out of active lists.')
  await trigger.hover()
  await expect(tooltip).toBeVisible()
  await tooltip.hover()
  await expect(tooltip).toBeVisible()
  await expect(trigger).not.toBeFocused()
  await page.keyboard.down('Escape')
  await page.keyboard.down('Escape')
  await page.keyboard.up('Escape')
  await expect(tooltip).toBeHidden()
  await page.mouse.move(0, 0)
  await trigger.hover()
  await tooltip.hover()
  await page.mouse.move(0, 0)
  await expect(tooltip).toBeHidden()
  await trigger.focus()
  await expect(tooltip).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(tooltip).toBeHidden()
  await expect(trigger).toBeFocused()
  expect((await new AxeBuilder({ page }).include('#components-tooltip-panel-preview').analyze()).violations).toEqual([])

  const bottomPreview = page.locator('#components-tooltip-bottom-panel-preview')
  const bottomTrigger = bottomPreview.getByRole('button', { name: 'Archive', exact: true })
  await expect(bottomPreview.locator('kbd')).toHaveCount(0)
  await bottomTrigger.evaluate(element => element.scrollIntoView({ block: 'center' }))
  await bottomTrigger.focus()
  const bottomTooltip = bottomPreview.getByRole('tooltip')
  await expect(bottomTooltip).toBeVisible()
  const triggerBounds = await bottomTrigger.boundingBox()
  const tooltipBounds = await bottomTooltip.boundingBox()
  expect(tooltipBounds!.y).toBeGreaterThanOrEqual(triggerBounds!.y + triggerBounds!.height)
  await page.keyboard.press('Escape')
  await expect(bottomTooltip).toBeHidden()
  await expect(bottomTrigger).toBeFocused()

  const navigation = page.getByRole('complementary', { name: 'Documentation navigation', exact: true })
  await navigation.getByRole('link', { name: 'Popover', exact: true }).click()
  await expect(page).toHaveURL('/components/popover')
  await navigation.getByRole('link', { name: 'Tooltip', exact: true }).click()
  await expect(page).toHaveURL('/components/tooltip')
  await expect(trigger).toHaveAccessibleDescription('Moves the account out of active lists.')
  await trigger.focus()
  await expect(tooltip).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(tooltip).toBeHidden()
})

test('Popover owns top-layer dismissal and optional content focus @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/components/popover')
  const preview = page.locator('#components-popover-focus-panel-preview')
  const trigger = preview.getByRole('button', { name: 'Edit profile', exact: true })
  await trigger.click()
  const dialog = page.getByRole('dialog', { name: 'Edit profile', exact: true })
  await expect(dialog).toBeVisible()
  await expect(dialog.getByRole('textbox', { name: 'Display name' })).toBeFocused()
  const bounds = (await dialog.boundingBox())!
  expect(bounds.x).toBeGreaterThanOrEqual(0)
  expect(bounds.x + bounds.width).toBeLessThanOrEqual(390)
  await page.keyboard.press('Escape')
  await expect(dialog).toBeHidden()
  await expect(trigger).toBeFocused()
  expect((await new AxeBuilder({ page }).include('#components-popover-panel-preview').analyze()).violations).toEqual([])
})

test('Message rows remain accessible layout-only content @cross-browser', async ({ page }) => {
  await page.goto('/components/message')
  await expect(page.getByRole('article', { name: /^Message from / })).toHaveCount(4)
  expect((await new AxeBuilder({ page }).include('[data-docs-layout="gallery"]').analyze()).violations).toEqual([])
})

test('Consumer-authored checklists retain native destinations and Floating panel recovery @cross-browser', async ({ page }) => {
  await page.goto('/components/floating-panel')
  const preview = page.locator('#components-floating-panel-checklist-panel-preview')
  await expect(preview.getByRole('heading', { name: 'Setup checklist', exact: true })).toBeVisible()
  await expect(preview.getByRole('link', { name: 'Open examples', exact: true })).toHaveAttribute('href', '/examples')
  await preview.getByRole('button', { name: 'Dismiss Setup checklist', exact: true }).click()
  const restore = preview.getByRole('button', { name: 'Restore Setup checklist', exact: true })
  await expect(restore).toBeFocused()
  await restore.click()
  await expect(preview.getByRole('heading', { name: 'Setup checklist', exact: true })).toBeFocused()
})

test('Notification keeps only the latest server-confirmed instance @cross-browser', async ({ page }) => {
  await page.goto('/components/notification')
  const preview = page.locator('#components-notification-panel-preview')
  const region = preview.locator('[data-fve-notification-region]')
  const notifications = region.locator('[data-fve-notification]')
  const trigger = preview.getByRole('button', { name: 'Show notification', exact: true })

  await expect(notifications).toHaveCount(0)
  for (let sequence = 1; sequence <= 4; sequence++) {
    const response = page.waitForResponse(response => new URL(response.url()).pathname === '/components/notifications/show' && response.request().method() === 'POST')
    await trigger.click()
    expect((await response).status()).toBe(200)
    await expect(notifications).toHaveCount(1)
    await expect(region.locator(`#saved-notification-${sequence}`)).toBeVisible()
    if (sequence > 1) await expect(region.locator(`#saved-notification-${sequence - 1}`)).toHaveCount(0)
  }
  await expect(notifications.getByRole('status')).toContainText('Server-confirmed notification 4')
  await expect(region).not.toHaveAttribute('data-fve-notification-limit')
  await expect(region).not.toHaveAttribute('data-fve-notification-expanded')

  await region.evaluate(element => {
    ;(window as any).__dismissedNotification = null
    element.addEventListener('fve-notification-dismiss', (event: Event) => {
      ;(window as any).__dismissedNotification = (event as CustomEvent).detail
    }, { once: true })
  })
  const dismissed = region.locator('#saved-notification-4')
  await dismissed.getByRole('button', { name: 'Dismiss Changes saved', exact: true }).focus()
  await dismissed.getByRole('button', { name: 'Dismiss Changes saved', exact: true }).click()
  await expect(notifications).toHaveCount(0)
  expect(await page.evaluate(() => (window as any).__dismissedNotification)).toEqual({ id: 'saved-notification-4', reason: 'dismiss' })
  expect(await page.evaluate(() => document.activeElement?.closest('#saved-notification-4') === null)).toBe(true)

  await trigger.click()
  await expect(notifications).toHaveCount(1)
  await expect(region.locator('#saved-notification-5')).toBeVisible()

  const actions = page.locator('#components-notification-actions-panel-preview')
  await expect(actions.locator('[data-fve-notification-persistent="true"]')).toHaveCount(1)
  await actions.getByRole('button', { name: 'Undo', exact: true }).click()
  await expect(actions.getByRole('status')).toContainText('The invitation remains pending.')
  const timing = page.locator('#components-notification-timing-panel-preview')
  await expect(timing.locator('[data-fve-notification]')).toHaveCount(1)
  await expect(timing.locator('[data-fve-notification-ttl="8000"]')).toContainText('Review needed')

  expect((await new AxeBuilder({ page }).include('#components-notification-panel-preview').analyze()).violations).toEqual([])
})

test('Notification default lifetime pauses for hover and focus within it and survives Datastar navigation @cross-browser', async ({ page, baseURL }) => {
  const navigation = await delayLocalNavigation(baseURL!)
  try {
    await page.goto(`${navigation?.origin ?? ''}/components/notification`)
    let preview = page.locator('#components-notification-panel-preview')
    await preview.getByRole('button', { name: 'Show notification', exact: true }).click()
    let notification = preview.locator('#saved-notification-1')
    await notification.getByRole('status').hover()
    await page.waitForTimeout(5200)
    await expect(notification).toBeVisible()
    await notification.getByRole('button', { name: 'Dismiss Changes saved', exact: true }).focus()
    await preview.getByRole('button', { name: 'Show notification', exact: true }).hover()
    await page.waitForTimeout(1200)
    await expect(notification).toBeVisible()
    await preview.getByRole('button', { name: 'Show notification', exact: true }).focus()
    await expect(notification).toHaveCount(0, { timeout: 5500 })

    const badgeResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/components/badge' && response.request().headers()['datastar-request'] === 'true')
    await page.locator('#nav-components-badge').click()
    expect((await badgeResponse).status()).toBe(200)
    // Response headers precede the morph; wait for its committed destination.
    await expect(page).toHaveURL(/\/components\/badge$/)
    const notificationResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/components/notification' && response.request().headers()['datastar-request'] === 'true')
    await page.locator('#nav-components-notification').click()
    expect((await notificationResponse).status()).toBe(200)
    await expect(page).toHaveURL(/\/components\/notification$/)
    preview = page.locator('#components-notification-panel-preview')
    await expect(preview.locator('[data-fve-notification]')).toHaveCount(0)
    await preview.getByRole('button', { name: 'Show notification', exact: true }).click()
    notification = preview.locator('[data-fve-notification]')
    await expect(notification).toHaveCount(1)
    await expect(notification.getByRole('status')).toContainText('Server-confirmed notification')
    await notification.getByRole('status').hover()
    await page.waitForTimeout(1200)
    await expect(notification).toBeVisible()
    if (navigation) expect(navigation.delayed).toEqual(['/components/badge', '/components/notification'])
  } finally {
    await navigation?.close()
  }
})

test('Notification stays bottom-end, readable, and motion-safe at narrow 200% text', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 320, height: 900 })
  await page.emulateMedia({ reducedMotion: 'reduce' })
  await page.goto('/components/notification')
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  const preview = page.locator('#components-notification-panel-preview')
  const trigger = preview.getByRole('button', { name: 'Show notification', exact: true })
  await trigger.click()
  const region = preview.locator('[data-fve-notification-region]')
  const notifications = region.locator('[data-fve-notification]')
  await expect(notifications).toHaveCount(1)
  await notifications.getByRole('button', { name: 'Dismiss Changes saved', exact: true }).focus()

  const layout = await region.evaluate((element, previewSelector) => {
    const region = element.getBoundingClientRect()
    const preview = document.querySelector(previewSelector)!.getBoundingClientRect()
    return {
      region: { left: region.left, right: region.right, top: region.top, bottom: region.bottom },
      preview: { left: preview.left, right: preview.right, top: preview.top, bottom: preview.bottom },
      overflow: element.scrollWidth - element.clientWidth,
    }
  }, '#components-notification-panel-preview')
  expect(layout.region.right).toBeLessThanOrEqual(layout.preview.right)
  expect(layout.region.top).toBeGreaterThanOrEqual(layout.preview.top)
  expect(layout.region.bottom).toBeLessThanOrEqual(layout.preview.bottom)
  expect(layout.region.left).toBeGreaterThanOrEqual(layout.preview.left)
  expect(layout.overflow).toBeLessThanOrEqual(1)
  expect(await notifications.evaluate(element => getComputedStyle(element).transitionProperty)).toBe('none')

  for (const theme of ['light', 'dark']) {
    await page.evaluate(theme => {
      document.documentElement.classList.toggle('dark', theme === 'dark')
      document.documentElement.dataset.theme = theme
    }, theme)
    expect((await new AxeBuilder({ page }).include('#components-notification-panel-preview').analyze()).violations).toEqual([])
    await preview.screenshot({ path: testInfo.outputPath(`notification-320-200-${theme}.png`) })
  }
})
