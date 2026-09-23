import { expect, test, type Locator } from '@playwright/test'

const app = '/components/page-examples/'

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


test('page examples use one compact size for actions, filters, and data-entry controls', async ({ page }) => {
  await page.goto(app + 'account-management?fveAppMode=app&fveAppFrame=ledger-workflow', { waitUntil: 'domcontentloaded' })
  const root = page.locator('[data-fve-app-mode-root=true]')
  const search = root.getByRole('searchbox', { name: 'Search accounts' })
  for (const control of [search, root.getByRole('combobox', { name: 'Filter by account type' }), root.getByRole('button', { name: 'Apply filters' }), root.getByRole('link', { name: 'Create', exact: true })]) {
    const actual = await control.evaluate(el => ({
      height: el.matches('input') ? el.closest('.fve-input-frame')?.getBoundingClientRect().height ?? el.getBoundingClientRect().height : el.getBoundingClientRect().height,
      fontSize: getComputedStyle(el).fontSize,
      lineHeight: getComputedStyle(el).lineHeight,
    }))
    expect(actual).toEqual({ height: 32, fontSize: '14px', lineHeight: '20px' })
  }
  const heading = root.getByRole('heading', { name: 'Accounts', exact: true }).first()
  const home = root.getByRole('link', { name: 'Home', exact: true })
  const textLeft = await home.evaluate(el => { const range = document.createRange(); range.selectNodeContents(el); return range.getBoundingClientRect().left })
  expect(Math.abs(textLeft - (await heading.boundingBox())!.x)).toBeLessThan(1)

  await root.getByRole('link', { name: 'Create', exact: true }).click()
  await expect(root.getByRole('heading', { name: 'Create account', exact: true })).toBeVisible()
  for (const control of [root.getByRole('textbox', { name: 'Account name' }), root.getByRole('button', { name: 'Create account', exact: true })]) {
    await expect(control).toHaveCSS('font-size', '14px')
    await expect(control).toHaveCSS('line-height', '20px')
    await expect(control).toHaveCSS('min-height', '32px')
  }
})

test('other page examples keep controls compact and chart labels at application text sizes', async ({ page }) => {
  const compact = async (control: Locator) => {
    const actual = await control.evaluate(element => ({
      height: element.matches('input') ? element.closest('.fve-input-frame')?.getBoundingClientRect().height ?? element.getBoundingClientRect().height : element.getBoundingClientRect().height,
      fontSize: getComputedStyle(element).fontSize,
      lineHeight: getComputedStyle(element).lineHeight,
    }))
    expect(actual).toEqual({ height: 32, fontSize: '14px', lineHeight: '20px' })
  }

  await page.goto(app + 'dependency-graph?fveAppMode=app&fveAppFrame=page-workspace', { waitUntil: 'domcontentloaded' })
  let root = page.locator('[data-fve-app-mode-root=true]')
  for (const control of [
    root.getByRole('searchbox', { name: 'Search dependencies' }),
    root.getByRole('button', { name: 'Zoom out', exact: true }),
    root.getByRole('button', { name: 'Zoom in', exact: true }),
    root.getByRole('button', { name: 'Reset view', exact: true }),
    root.locator('[data-fve-page-header=true]').getByRole('link', { name: 'Inspect execution', exact: true }),
  ]) await compact(control)

  await page.goto(app + 'financial-reporting?fveAppMode=app&fveAppFrame=page-workspace', { waitUntil: 'domcontentloaded' })
  root = page.locator('[data-fve-app-mode-root=true]')
  for (const name of ['View account', 'All accounts', '3 months', '6 months']) {
    await compact(root.getByRole('link', { name, exact: true }))
  }
  await expect(root.getByRole('region', { name: 'Account balance chart' }).getByText('$50k', { exact: true })).toHaveCSS('font-size', '12px')
  await expect(root.getByRole('region', { name: 'Account balance chart' }).getByText('Apr', { exact: true })).toHaveCSS('font-size', '12px')

  await page.goto(app + 'messaging?fveAppMode=app&fveAppFrame=page-workspace', { waitUntil: 'domcontentloaded' })
  root = page.locator('[data-fve-app-mode-root=true]')
  await expect(root.getByRole('textbox', { name: 'Message', exact: true })).toHaveCSS('font-size', '14px')
  await expect(root.getByRole('textbox', { name: 'Message', exact: true })).toHaveCSS('line-height', '20px')
  await compact(root.getByRole('button', { name: 'Send message', exact: true }))

  await page.goto(app + 'media-management?item=photo-303&fveAppMode=app&fveAppFrame=page-workspace', { waitUntil: 'domcontentloaded' })
  root = page.locator('[data-fve-app-mode-root=true]')
  await compact(root.getByRole('textbox', { name: 'Photo name', exact: true }))
  await compact(root.getByRole('button', { name: 'Save changes', exact: true }))
})

test('server-populated search follows native form reset and unavailable states @cross-browser', async ({ page }) => {
  await page.goto(app + 'account-management?query=Operating&fveAppMode=app&fveAppFrame=ledger-workflow', { waitUntil: 'domcontentloaded' })
  const root = page.locator('[data-fve-app-mode-root=true]')
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
  await page.goto(app + 'dependency-graph?fveAppMode=app&fveAppFrame=page-workspace', { waitUntil: 'domcontentloaded' })
  const root = page.locator('[data-fve-app-mode-root=true]')
  const search = root.getByRole('searchbox', { name: 'Search dependencies' })
  const clear = root.getByRole('button', { name: 'Clear Search dependencies', includeHidden: true })
  await expect(root.locator('.fve-input-frame svg').first()).toBeVisible()
  await expect(clear).toBeHidden()
  await search.fill('customers')
  await expect(clear).toBeVisible()
  await root.getByRole('button', { name: 'Reset view' }).click()
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
