import { expect, test } from '@playwright/test'

for (const width of [1440, 390]) {
  test(`size inheritance and explicit overrides align controls at ${width}px`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 1000 })
    await page.emulateMedia({ colorScheme: width === 390 ? 'dark' : 'light' })
    await page.goto('/components/input', { waitUntil: 'domcontentloaded' })
    const example = page.locator('#components-input-sizes')
    for (const [name, height] of [['Compact', 32], ['Standard', 40], ['Large', 48], ['Larger', 48]] as const) {
      const searchName = name === 'Larger' ? 'Larger search in a compact region' : `${name} search`
      for (const control of [example.getByRole('searchbox', { name: searchName, exact: true }), example.getByRole('combobox', { name: `${name} type`, exact: true }), example.getByRole('button', { name: `${name} action`, exact: true }), example.getByRole('button', { name: `${name} refresh`, exact: true })]) {
        const actual = await control.evaluate(el => {
          const style = getComputedStyle(el)
          return {
            height: el.matches('input') ? el.closest('.fve-input-frame')!.getBoundingClientRect().height : el.getBoundingClientRect().height,
            size: style.fontSize, lineHeight: style.lineHeight, weight: style.fontWeight,
            iconOnly: el.hasAttribute('aria-label'), field: el.matches('input, [role=combobox]'),
          }
        })
        expect(actual.height, `${name}: ${await control.getAttribute('id')}`).toBe(height)
        if (!actual.iconOnly) {
          expect(actual.size).toBe(name === 'Compact' ? '14px' : '16px')
          expect(actual.lineHeight).toBe(name === 'Compact' ? '20px' : '24px')
          expect(actual.weight).toBe(actual.field ? '400' : '500')
        }
      }
    }
    await expect(example.getByText('Standard search', { exact: true })).toHaveCSS('font-size', '14px')
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    if (testInfo.project.name === 'chromium') await example.screenshot({ path: testInfo.outputPath(`control-sizes-${width}.png`) })
  })
}


test('server-populated search follows native form reset and unavailable states @cross-browser', async ({ page }) => {
  await page.goto('/examples/application/accounts?search=Operating', { waitUntil: 'domcontentloaded' })
  const root = page.locator('#main-content')
  const search = root.getByRole('searchbox', { name: 'Search accounts' })
  const clear = root.getByRole('button', { name: 'Clear Search accounts', includeHidden: true })
  await expect(search).toHaveValue('Operating')
  await expect(clear).toBeVisible()
  await clear.click()
  await expect(search).toHaveValue('')
  await expect(clear).toBeHidden()
  await search.evaluate((el: HTMLInputElement) => el.form!.reset())
  await expect(search).toHaveValue('Operating')
  await expect(clear).toBeVisible()
  for (const property of ['disabled', 'readOnly'] as const) {
    await search.evaluate((el: HTMLInputElement, property) => { el[property] = true }, property)
    await expect(clear).toBeHidden()
    await search.evaluate((el: HTMLInputElement, property) => { el[property] = false }, property)
    await expect(clear).toBeVisible()
  }
  await clear.click()
  await root.getByRole('button', { name: 'Apply filters' }).click()
  await expect(search).toHaveValue('')
  await expect(clear).toBeHidden()
})

test('shared search has an icon and clearing follows typing, bound resets, and native values @cross-browser', async ({ page }) => {
  await page.goto('/components/input', { waitUntil: 'domcontentloaded' })
  const root = page.locator('[data-docs-layout="gallery"]')
  const search = root.getByRole('searchbox', { name: 'Search', exact: true })
  const clear = root.getByRole('button', { name: 'Clear Search', includeHidden: true })
  await expect(root.locator('.fve-input-frame svg').first()).toBeVisible()
  await expect(clear).toBeHidden()
  await search.fill('customers')
  await expect(clear).toBeVisible()
  await search.evaluate((el: HTMLInputElement) => { el.value = '' })
  await expect(search).toHaveValue('')
  await expect(clear).toBeHidden()
  await search.fill('orders')
  await clear.focus()
  await page.keyboard.press('Enter')
  await expect(search).toBeFocused()
  await expect(search).toHaveValue('')
  await expect(clear).toBeHidden()
  // Value changes need not originate in an input event (autofill / bound updates).
  await search.evaluate((el: HTMLInputElement) => { el.value = 'customers' })
  await expect(clear).toBeVisible()
  await clear.click()
  await expect(search).toHaveValue('')
})
