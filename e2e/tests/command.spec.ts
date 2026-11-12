import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const shortcut = process.platform === 'darwin' ? 'Meta+K' : 'Control+K'

test('Catalog Command ranks Table in Components and navigates after morphs @cross-browser', async ({ page }) => {
  await page.goto('/components/week-calendar')
  const searchTrigger = page.getByRole('button', { name: 'Search documentation', exact: true })
  await expect(searchTrigger.locator('svg')).toBeVisible()
  await expect(searchTrigger).toHaveText(/^(?:⌘K|Ctrl\+K)$/)
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
  await expect(dialog.getByRole('group', { name: 'Search results', exact: true })).toHaveCount(0)
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

test('Catalog search separates guides, components and examples without flooding page-name queries @cross-browser', async ({ page }) => {
  await page.goto('/components/installation')
  await page.keyboard.press(shortcut)
  const dialog = page.getByRole('dialog', { name: 'Search documentation', exact: true })
  const input = dialog.getByRole('combobox')
  const guides = dialog.getByRole('group', { name: 'Guides', exact: true })
  const components = dialog.getByRole('group', { name: 'Components', exact: true })
  const examples = dialog.getByRole('group', { name: 'Examples', exact: true })
  await input.fill('stand')
  await expect(guides.getByRole('option')).toHaveCount(1)
  await expect(guides.getByRole('option').first()).toHaveAttribute('href', '/guides/scripts')
  await expect(guides.getByRole('option').first().locator('svg')).toBeVisible()
  await input.fill('presentation')
  await expect(components.locator('[href="/components/presentation"]')).toBeVisible()
  await expect(guides.locator('[href="/guides/presentations"]')).toBeVisible()
  const presentation = examples.locator('[href="/examples/presentation"]')
  await expect(presentation).toBeVisible()
  await expect(presentation).toHaveAttribute('target', '_blank')
  await input.fill('presentation pdf')
  await expect(guides.getByRole('option').first()).toHaveAttribute('href', '/guides/presentations')
  await expect(dialog.getByRole('group', { name: 'Sections', exact: true }).locator('[href="/guides/presentations#printing"]')).toBeVisible()
  await input.fill('examples')
  await expect(examples.getByRole('option')).toHaveCount(5)
  await expect(dialog.getByRole('group', { name: 'Pages', exact: true }).locator('[href="/examples"]')).toBeVisible()
  await input.fill('presentation')
  const popupPromise = page.waitForEvent('popup')
  await presentation.click()
  const popup = await popupPromise
  await expect(popup).toHaveURL('/examples/presentation')
  await expect(popup.getByRole('heading', { name: 'Geldos · Treasury review', exact: true, level: 1 })).toBeVisible()
  await popup.close()
  await expect(page).toHaveURL('/components/installation')
  await expect(dialog).toBeHidden()
  await page.setViewportSize({ width: 320, height: 900 })
  await page.locator('html').evaluate(el => { el.style.fontSize = '200%' })
  await page.keyboard.press(shortcut)
  await input.fill('examples')
  await expect(examples.getByRole('option')).toHaveCount(5)
  await expect.poll(() => dialog.getByRole('listbox').evaluate(el => el.scrollWidth - el.clientWidth)).toBeLessThanOrEqual(1)
})

for (const [name, start, term, destination] of [
  ['specification', '/examples/specification?organization=example-household&environment=production&ledger=alex', 'Example.Domain', '/examples/specification/architecture/solution/domain'],
  ['API documentation', '/examples/api-documentation', 'Create account', '/examples/api-documentation/create-account'],
] as const) {
  test(`Template ${name} search supports click, shortcuts, navigation and morphs @cross-browser`, async ({ page }) => {
    await page.goto(start)
    await page.evaluate(() => { document.documentElement.dataset.searchDocument = 'retained' })
    const trigger = page.getByRole('button', { name: `Search ${name}`, exact: true })
    await expect(trigger.locator('svg')).toBeVisible()
    await expect(trigger).toHaveText(/^(?:⌘K|Ctrl\+K)$/)
    await trigger.click()
    const dialog = page.getByRole('dialog', { name: `Search ${name}`, exact: true })
    const input = dialog.getByRole('combobox', { name: `Search ${name}`, exact: true })
    await expect(input).toBeFocused()
    await input.fill('no-such-page')
    await expect(dialog.getByRole('status')).toHaveText('No results found.')
    await input.fill(term)
    const result = dialog.getByRole('option', { name: term, exact: true })
    await expect(result).toHaveAttribute('aria-selected', 'true')
    await input.press('Enter')
    await expect(page).toHaveURL(url => url.pathname === destination)
    await expect(page.locator('html')).toHaveAttribute('data-search-document', 'retained')
    await expect(dialog).toBeHidden()
    if (name === 'specification') expect(new URL(page.url()).searchParams.get('ledger')).toBe('alex')
    // The URL commits before navigation's animation-frame focus restoration.
    // Finish that transition before starting another dialog interaction.
    await expect(page.getByRole('main')).toBeFocused()
    for (const key of ['Meta+K', 'Control+K']) {
      await page.keyboard.press(key)
      await expect(input).toBeFocused()
      await expect(input).toHaveValue('')
      await input.press('Escape')
      await expect(dialog).toBeHidden()
      await expect(trigger).toBeFocused()
    }
    await page.setViewportSize({ width: 320, height: 900 })
    await page.locator('html').evaluate(html => html.style.fontSize = '200%')
    await trigger.click()
    await expect(input).toBeFocused()
    await expect(dialog.getByRole('option').first()).toHaveCSS('white-space', 'normal')
    const bounds = (await dialog.boundingBox())!
    expect(bounds.x).toBeGreaterThanOrEqual(0)
    expect(bounds.x + bounds.width).toBeLessThanOrEqual(320)
    await input.press('Escape')
    await expect(trigger).toBeFocused()
  })
}

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
