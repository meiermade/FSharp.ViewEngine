import { test, expect, type Locator } from '@playwright/test'

const centers = async (trigger: Locator, option: Locator) => {
  const triggerBox = await trigger.boundingBox()
  const optionBox = await option.boundingBox()
  return {
    triggerX: triggerBox!.x + 12,
    triggerY: triggerBox!.y + triggerBox!.height / 2,
    optionX: optionBox!.x + 12,
    optionY: optionBox!.y + optionBox!.height / 2,
  }
}

test('ordinary Select aligns its selected option over the closed value and tracks page scroll @cross-browser', async ({ page }) => {
  await page.goto('/components/select')
  const panel = page.locator('#components-select-panel-preview')
  const trigger = panel.getByRole('combobox', { name: 'Update frequency' })
  await trigger.click()
  const selected = panel.getByRole('option', { selected: true })
  await expect(selected).toHaveText('Weekly✓')
  let position = await centers(trigger, selected)
  expect(Math.abs(position.triggerX - position.optionX)).toBeLessThanOrEqual(1)
  expect(Math.abs(position.triggerY - position.optionY)).toBeLessThanOrEqual(1)

  await page.evaluate(() => scrollBy(0, 80))
  position = await centers(trigger, selected)
  expect(Math.abs(position.triggerY - position.optionY)).toBeLessThanOrEqual(1)
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(trigger).toContainText('Daily')
  await expect(trigger).toBeFocused()
})

test('Select trigger-edge and touch positioning remain visible @cross-browser', async ({ browser }) => {
  const context = await browser.newContext({ viewport: { width: 390, height: 844 }, hasTouch: true })
  const page = await context.newPage()
  await page.goto('/components/select')
  const selectedPanel = page.locator('#components-select-panel-preview')
  const selectedTrigger = selectedPanel.getByRole('combobox', { name: 'Update frequency' })
  await selectedTrigger.scrollIntoViewIfNeeded()
  await selectedTrigger.tap()
  const popup = selectedPanel.locator('[popover]:visible')
  const triggerBox = (await selectedTrigger.boundingBox())!
  const popupBox = (await popup.boundingBox())!
  expect(popupBox.y).toBeGreaterThanOrEqual(triggerBox.y + triggerBox.height - 1)
  expect(popupBox.x).toBeGreaterThanOrEqual(0)
  expect(popupBox.x + popupBox.width).toBeLessThanOrEqual(390)
  await page.keyboard.press('Escape')

  const edgePanel = page.locator('#components-select-edge-panel-preview')
  const edgeTrigger = edgePanel.getByRole('combobox', { name: 'Export format' })
  await edgeTrigger.scrollIntoViewIfNeeded()
  await edgeTrigger.tap()
  const edgePopup = edgePanel.locator('[popover]:visible')
  const edgeTriggerBox = (await edgeTrigger.boundingBox())!
  const edgePopupBox = (await edgePopup.boundingBox())!
  expect(Math.abs(edgePopupBox.x + edgePopupBox.width - (edgeTriggerBox.x + edgeTriggerBox.width))).toBeLessThanOrEqual(1)
  expect(edgePopupBox.y).toBeGreaterThanOrEqual(edgeTriggerBox.y + edgeTriggerBox.height - 1)
  await context.close()
})
