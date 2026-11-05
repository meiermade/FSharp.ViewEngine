import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const shortcut = process.platform === 'darwin' ? 'Meta+K' : 'Control+K'

test('Catalog Command ranks Table in Components and navigates after morphs @cross-browser', async ({ page }) => {
  await page.goto('/components/week-calendar')
  await page.evaluate(() => { (window as any).__commandDocument = 'same-document' })
  await page.keyboard.press(shortcut)
  const dialog = page.getByRole('dialog', { name: 'Search documentation', exact: true })
  const input = dialog.getByRole('combobox', { name: 'Search documentation', exact: true })
  await expect(input).toBeFocused()
  await input.fill('table')
  const components = dialog.getByRole('group', { name: 'Components', exact: true })
  const table = components.getByRole('option', { name: 'Table', exact: true })
  await expect(components.getByRole('option').first()).toHaveAttribute('href', '/components/table')
  await expect(table).toHaveAttribute('aria-selected', 'true')
  await expect(dialog.getByRole('group', { name: 'Search results', exact: true })).toBeVisible()
  await expect(dialog.getByRole('option', { name: /Encoding and trusted content|^Datastar/ })).toHaveCount(0)
  const box = await dialog.boundingBox()
  expect(Math.abs(box!.x + box!.width / 2 - page.viewportSize()!.width / 2)).toBeLessThanOrEqual(1)
  await input.press('Enter')
  await expect(page).toHaveURL('/components/table')
  expect(await page.evaluate(() => (window as any).__commandDocument)).toBe('same-document')
  await expect(dialog).not.toBeVisible()
  const main = page.getByRole('main')
  await expect(main).toBeFocused()
  await expect(main).toHaveCSS('outline-style', 'none')
  await page.keyboard.press('Tab')
  // Native Tab order differs by engine; the reached control must retain its indicator.
  const control = main.locator(':focus')
  await expect(control).toHaveCount(1)
  await expect(control).toHaveCSS('outline-style', 'solid')
  await expect(control).toHaveCSS('outline-width', '2px')
  await page.keyboard.press(shortcut)
  await expect(input).toBeFocused()
  await input.fill('comm')
  await expect(components.getByRole('option').first()).toHaveAttribute('href', '/components/command')
  await input.press('Escape')
  await expect(dialog).not.toBeVisible()
  await expect(page.getByRole('button', { name: 'Search documentation', exact: true })).toBeFocused()

  for (const key of ['Meta+K', 'Control+K']) {
    await page.keyboard.press(key)
    await expect(input).toBeFocused()
    await expect(input).toHaveValue('')
    await expect(table).toBeVisible()

    await input.fill('no-such-document')
    await expect(dialog.getByText('No results found.', { exact: true })).toBeVisible()
    await page.keyboard.press(key)
    await expect(input).toHaveValue('')
    await expect(input).toBeFocused()
    await expect(dialog.getByText('No results found.', { exact: true })).not.toBeVisible()
    await expect(dialog.getByRole('option').first()).toHaveAttribute('aria-selected', 'true')

    await input.fill('table')
    await input.press('Escape')
    await expect(dialog).not.toBeVisible()
  }

  // Exercise both navigation to Select and selecting the already-current page.
  for (let attempt = 0; attempt < 2; attempt++) {
    await page.keyboard.press(shortcut)
    await input.fill('sel')
    await expect(dialog.getByRole('option').first()).toHaveAttribute('href', '/components/select')
    const navigation = page.locator('[data-fve-navigation-root]')
    const previousDocument = await navigation.getAttribute('data-navigation-document')
    await input.press('Enter')
    await expect(page).toHaveURL('/components/select')
    // The URL is already correct on the second attempt; wait for that response's committed document.
    await expect(navigation).not.toHaveAttribute('data-navigation-document', previousDocument!)
    await expect(dialog).not.toBeVisible()
    await page.keyboard.press(shortcut)
    await expect(input).toHaveValue('')
    await expect(input).toBeFocused()
    await expect(dialog.getByRole('option').first()).toHaveAttribute('aria-selected', 'true')
    await input.press('Escape')
  }
})

test('Command supports grouped matching, keyboard actions, unavailable and empty results @cross-browser', async ({ page }) => {
  await page.goto('/components/command')
  const grouped = page.locator('#components-command-groups-panel-preview')
  await grouped.getByRole('combobox').fill('schedule')
  await expect(grouped.getByRole('option')).toHaveCount(1)
  await expect(grouped.getByRole('option')).toContainText('Month calendar')
  await expect(grouped.getByRole('group', { name: 'Page examples' })).not.toBeVisible()

  const actions = page.locator('#components-command-actions-panel-preview')
  const input = actions.getByRole('combobox')
  await input.focus()
  await input.press('End')
  await expect(actions.getByRole('option', { name: 'Profile settings', exact: true })).toHaveAttribute('aria-selected', 'true')
  await input.press('Enter')
  await expect(actions.getByRole('status').filter({ hasText: 'Profile settings selected.' })).toBeVisible()
  await input.fill('unavailable')
  await expect(actions.getByRole('option')).toBeDisabled()
  await input.press('Enter')
  await expect(actions.getByRole('status').filter({ hasText: 'Profile settings selected.' })).toBeVisible()
  await input.fill('no-such-command')
  await expect(actions.getByText('No results found.', { exact: true })).toBeVisible()
  await expect(input).toHaveAttribute('aria-expanded', 'false')
  await input.press('Escape')
  await expect(input).toHaveValue('')
  expect((await new AxeBuilder({ page }).include('#components-command-actions-panel-preview').analyze()).violations).toEqual([])

  const trigger = page.locator('#components-command-palette-panel-preview').getByRole('button', { name: 'Open command palette', exact: true })
  await trigger.click()
  const dialog = page.getByRole('dialog', { name: 'Navigate the catalog', exact: true })
  await expect(dialog.getByRole('combobox')).toBeFocused()
  await dialog.getByRole('combobox').press('ArrowDown')
  await expect(dialog.getByRole('option', { name: 'Month calendar', exact: true })).toHaveAttribute('aria-selected', 'true')
  await dialog.getByRole('combobox').press('Escape')
  await expect(trigger).toBeFocused()
})

test('Command palette stays bounded in dark 320px / 200% text @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 1000 })
  await page.goto('/components/month-calendar')
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%'; document.documentElement.classList.add('dark'); document.documentElement.dataset.theme = 'dark' })
  await page.keyboard.press(shortcut)
  const dialog = page.getByRole('dialog', { name: 'Search documentation', exact: true })
  await dialog.getByRole('combobox').fill('table')
  const geometry = await dialog.evaluate(el => {
    const box = el.getBoundingClientRect()
    return { left: box.left, right: box.right, top: box.top, bottom: box.bottom, overflow: el.scrollWidth - el.clientWidth }
  })
  expect(geometry.left).toBeGreaterThanOrEqual(0)
  expect(geometry.right).toBeLessThanOrEqual(320)
  expect(geometry.top).toBeGreaterThanOrEqual(0)
  expect(geometry.bottom).toBeLessThanOrEqual(1000)
  expect(geometry.overflow).toBeLessThanOrEqual(1)
  expect((await new AxeBuilder({ page }).include('#docs-search-dialog').analyze()).violations).toEqual([])
  await dialog.getByRole('combobox').fill('no-such-document')
  await expect(dialog.getByText('No results found.', { exact: true })).toBeVisible()
})

test('Command keeps stable results in place and only changes the active option @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  await page.getByRole('button', { name: 'Search documentation', exact: true }).click()
  const input = page.locator('#docs-search-input')
  await input.fill('button')
  await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))))
  const updates = await input.evaluate(async input => {
    const root = input.closest('[data-fve-command]')!
    const changes = { children: 0, selection: 0 }
    const observer = new MutationObserver(records => {
      for (const record of records) {
        if (record.type === 'childList') changes.children++
        if (record.attributeName === 'aria-selected') changes.selection++
      }
    })
    observer.observe(root, { subtree: true, childList: true, attributes: true, attributeFilter: ['aria-selected'] })
    input.dispatchEvent(new Event('input', { bubbles: true }))
    await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))
    const unchanged = { ...changes }
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }))
    await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))
    const navigated = { children: changes.children - unchanged.children, selection: changes.selection - unchanged.selection }
    observer.disconnect()
    return { unchanged, navigated }
  })
  expect(updates).toEqual({ unchanged: { children: 0, selection: 0 }, navigated: { children: 0, selection: 2 } })
})
