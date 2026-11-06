import { expect, test } from '@playwright/test'

for (const width of [1440, 390]) {
  test(`size inheritance and explicit overrides align controls at ${width}px`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 1000 })
    await page.emulateMedia({ colorScheme: width === 390 ? 'dark' : 'light' })
    await page.goto('/components/input', { waitUntil: 'domcontentloaded' })
    const example = page.locator('#components-input-sizes')
    for (const [name, height] of [['Extra small', 24], ['Compact', 28], ['Standard', 32], ['Large', 36], ['Extra large', 40], ['Larger', 36]] as const) {
      const searchName = name === 'Larger' ? 'Larger search in a compact region' : `${name} search`
      for (const control of [example.getByRole('searchbox', { name: searchName, exact: true }), example.getByRole('combobox', { name: `${name} type`, exact: true }), example.getByRole('button', { name: `${name} action`, exact: true }), example.getByRole('button', { name: `${name} refresh`, exact: true })]) {
        const actual = await control.evaluate(el => {
          const style = getComputedStyle(el)
          return {
            height: el.getBoundingClientRect().height,
            size: style.fontSize, lineHeight: style.lineHeight, weight: style.fontWeight,
            iconOnly: el.hasAttribute('aria-label'), field: el.matches('input, [role=combobox]'),
          }
        })
        expect(actual.height, `${name}: ${await control.getAttribute('id')}`).toBe(height)
        if (!actual.iconOnly) {
          expect(actual.size).toBe(name === 'Extra small' ? '12px' : '14px')
          expect(actual.lineHeight).toBe(name === 'Extra small' ? '16px' : '20px')
          expect(actual.weight).toBe(actual.field ? '400' : '500')
        }
      }
    }
    await expect(example.getByText('Standard search', { exact: true })).toHaveCSS('font-size', '14px')
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    if (testInfo.project.name === 'chromium') await example.screenshot({ path: testInfo.outputPath(`control-sizes-${width}.png`) })
  })
}


test('Button sizes coordinate text and icon-only geometry without padding conflicts @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  const example = page.locator('#components-button-sizes-panel-preview')
  for (const [name, height, font, icon] of [
    ['Extra small', 24, 12, 12], ['Small', 28, 14, 16], ['Medium', 32, 14, 16], ['Large', 36, 14, 20], ['Extra large', 40, 14, 20],
  ] as const) {
    const text = example.getByRole('button', { name, exact: true })
    const square = example.getByRole('button', { name: `${name} refresh`, exact: true })
    await expect(text).toHaveCSS('height', `${height}px`)
    await expect(text).toHaveCSS('font-size', `${font}px`)
    await expect(square).toHaveCSS('height', `${height}px`)
    await expect(square).toHaveCSS('width', `${height}px`)
    await expect(square).toHaveCSS('padding', '0px')
    await expect(square.locator('svg')).toHaveCSS('width', `${icon}px`)
  }
  const square = example.getByRole('button', { name: 'Small refresh', exact: true })
  await square.evaluate(el => el.parentElement!.setAttribute('size', 'extra-large'))
  await expect(square).toHaveCSS('width', '40px')
  await expect(square).toHaveCSS('height', '40px')
  await square.evaluate(el => el.parentElement!.setAttribute('icon-only', 'false'))
  await expect(square).toHaveCSS('padding-left', '13px') // Outline border is included in the size's inset.
  await square.evaluate(el => el.parentElement!.setAttribute('icon-only', 'true'))
  await expect(square).toHaveCSS('padding', '0px')
  await expect(square).toHaveCSS('width', '40px')
})

test('row-action recipes match the application compact trigger and row typography @cross-browser', async ({ page }) => {
  for (const [path, selector] of [
    ['/examples/application/accounts', 'main'],
    ['/components/button', '#components-button-row-actions'],
    ['/components/dropdown-menu', '#components-dropdown-menu-row-actions'],
    ['/components/table', '#components-table-row-actions'],
  ]) {
    await page.goto(path)
    const trigger = page.locator(selector).getByRole('button', { name: 'Actions for Unassigned expense', exact: true })
    await expect(trigger).toHaveCSS('width', '28px')
    await expect(trigger).toHaveCSS('height', '28px')
    await expect(trigger.locator('svg')).toHaveCSS('width', '16px')
    await trigger.click()
    const entry = page.getByRole('menuitem', { name: 'Open account', exact: true })
    await expect(entry).toHaveCSS('font-size', '14px')
    await page.keyboard.press('Escape')
    await expect(trigger).toBeFocused()
  }
})

test('application page and section headers keep a clear hierarchy without crowding actions', async ({ page }) => {
  for (const width of [1440, 390]) {
    await page.setViewportSize({ width, height: 1000 })
    await page.emulateMedia({ colorScheme: width === 390 ? 'dark' : 'light' })
    await page.goto('/examples/application/accounts/101?organization=example-household&environment=production&ledger=alex', { waitUntil: 'domcontentloaded' })
    const header = page.locator('[data-fve-page-header="true"]')
    const title = header.getByRole('heading', { level: 1, name: 'Operating checking', exact: true })
    const section = page.getByRole('heading', { level: 2, name: 'Transactions', exact: true })
    const edit = header.getByRole('link', { name: 'Edit account', exact: true })
    await expect(title).toHaveCSS('font-size', '30px')
    await expect(section).toHaveCSS('font-size', '24px')
    await expect(edit).toBeInViewport()
    await expect(edit).toHaveCSS('font-size', '14px')
    const titleBounds = (await title.boundingBox())!
    const actionBounds = (await edit.boundingBox())!
    expect(actionBounds.height).toBe(32)
    expect(titleBounds.x + titleBounds.width <= actionBounds.x || titleBounds.y + titleBounds.height <= actionBounds.y).toBe(true)
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  }
})

test('linked detail and table values align with their labels without action padding', async ({ page }) => {
  for (const width of [1440, 390]) {
    await page.setViewportSize({ width, height: 1000 })
    await page.emulateMedia({ colorScheme: width === 390 ? 'dark' : 'light' })
    await page.goto('/examples/application/transactions/201')
    const accountField = page.locator('dl > div').filter({ has: page.locator('dt').filter({ hasText: /^Account$/ }) })
    const account = accountField.getByRole('link', { name: 'Operating checking', exact: true })
    await expect(account).toHaveCSS('padding-left', '0px')
    await expect(account).toHaveCSS('padding-top', '0px')
    const label = (await accountField.locator('dt').boundingBox())!
    const value = (await account.boundingBox())!
    expect(value.x).toBe(label.x)
    const date = page.locator('dl > div').filter({ has: page.locator('dt').filter({ hasText: /^Date$/ }) })
    expect((await accountField.locator('dd').boundingBox())!.height).toBe((await date.locator('dd').boundingBox())!.height)
    await account.focus()
    await page.keyboard.press('Enter')
    await expect(page.getByRole('heading', { name: 'Operating checking', exact: true, level: 1 })).toBeVisible()
    const table = page.getByRole('table', { name: 'Operating checking transactions', exact: true })
    const transaction = table.getByRole('link', { name: 'Client payment', exact: true })
    const related = table.getByRole('link', { name: 'Operating checking', exact: true }).first()
    for (const link of [transaction, related]) {
      await expect(link).toHaveCSS('padding-left', '0px')
      await expect(link).toHaveCSS('padding-top', '0px')
    }
    if (width === 1440) {
      const heading = table.getByRole('columnheader', { name: 'Description', exact: true })
      const start = await heading.evaluate(el => el.getBoundingClientRect().x + parseFloat(getComputedStyle(el).paddingLeft))
      expect((await transaction.boundingBox())!.x).toBe(start)
    }
    await transaction.focus()
    await page.keyboard.press('Enter')
    await expect(page.getByRole('heading', { name: 'Client payment', exact: true, level: 1 })).toBeVisible()
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  }
})

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
  await search.press('Enter')
  await expect(search).toHaveValue('')
  await expect(clear).toBeHidden()
})
