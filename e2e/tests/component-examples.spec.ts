import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const components = [
  ['button', 'Button.create'], ['icon-button', 'IconButton.create'], ['badge', 'Badge.create'],
  ['status', 'Status.create'], ['loading-indicator', 'LoadingIndicator.create'], ['empty-state', 'EmptyState.create'],
  ['table', 'Table.create'], ['description-list', 'DescriptionList.create'], ['metric', 'Metric.text'],
  ['pagination', 'Pagination.create'], ['input', 'Input.create'], ['textarea', 'Textarea.create'], ['form-layouts', 'Input.create'],
  ['error-summary', 'ErrorSummary.create'], ['notice', 'Notice.create'], ['select', 'Select.create'],
  ['checkbox', 'Checkbox.create'], ['switch', 'Switch.create'],
  ['toggle-button', 'ToggleButton.create'], ['tabs', 'Tabs.create'], ['radio-group', 'RadioGroup.create'],
  ['dropdown-menu', 'DropdownMenu.create'], ['dialog', 'Dialog.create'], ['confirmation-dialog', 'ConfirmationDialog.create'],
  ['drawer', 'Drawer.create'], ['breadcrumbs', 'Breadcrumbs.create'], ['side-nav', 'SideNav.create'],
  ['page-top-bar', 'PageTopBar.create'], ['page-header', 'PageHeader.create'], ['section', 'Section.create'],
  ['page', 'Page.create'], ['collection', 'Collection.create'], ['detail', 'Detail.create'], ['app-shell', 'AppShell.create'],
]

test('all component galleries expose named previews and complete copyable code @cross-browser', async ({ page, context, browserName }, testInfo) => {
  if (browserName === 'chromium') await context.grantPermissions(['clipboard-write'])
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  // Observe the real clipboard implementation; Chromium requires an explicit automation permission.
  await page.addInitScript(() => {
    const write = navigator.clipboard.writeText.bind(navigator.clipboard)
    navigator.clipboard.writeText = async text => {
      await write(text)
      ;(window as any).__copiedExample = text
    }
  })
  let exampleCount = 0
  for (const [id, api] of components) {
    await page.goto(`/components/${id}`)
    const gallery = page.locator('.docs-gallery-layout')
    await expect(gallery).toBeVisible()
    await expect(page.locator('.spec-toc-nav, .spec-mobile-toc-nav')).toHaveCount(0)
    await expect(page.getByRole('heading', { name: /^(Usage|Example setup|Accessibility)$/ })).toHaveCount(0)
    await expect(page.getByRole('link', { name: 'Imports and supporting code', exact: true })).toHaveCount(0)
    const examples = gallery.locator('[data-docs-example="true"]')
    for (const example of await examples.all()) {
      exampleCount++
      const toolbar = example.locator(':scope > .spec-example-toolbar')
      await expect(toolbar.getByRole('heading', { level: 2 })).toBeVisible()
      await expect(toolbar.getByRole('tab').first()).toHaveText('Preview')
      await expect(toolbar.getByRole('tab', { name: 'Preview', exact: true })).toHaveAttribute('aria-selected', 'true')
      await toolbar.getByRole('tab', { name: 'Code', exact: true }).click()
      const code = example.locator('[data-docs-copy-source]')
      await expect(code).toBeVisible()
      await expect(code).toContainText('open FSharp.ViewEngine.Components')
      await expect(code).not.toContainText('FSharp.ViewEngine.Docs')
      await expect(code).not.toContainText('themedSurface')
      await expect(code).not.toContainText('fullBleedThemedSurface')
      const copy = toolbar.getByRole('button', { name: /^Copy .+ code$/ })
      await copy.click()
      await expect(copy).toHaveAttribute('data-copied', 'true')
      expect(await page.evaluate(() => (window as any).__copiedExample)).toBe(await code.textContent())
      await toolbar.getByRole('tab', { name: 'Code', exact: true }).focus()
      await page.keyboard.press('Home')
      await expect(toolbar.getByRole('tab', { name: 'Preview', exact: true })).toBeFocused()
      await expect(toolbar.getByRole('tab', { name: 'Preview', exact: true })).toHaveAttribute('aria-selected', 'true')
    }
    await expect(examples.first().locator('code').first()).toContainText(api)
    if (['button', 'breadcrumbs', 'collection', 'app-shell'].includes(id)) {
      await examples.first().scrollIntoViewIfNeeded()
      await page.screenshot({ path: testInfo.outputPath(`${id}-gallery.png`) })
    }
  }
  expect(exampleCount).toBe(115)
  expect(errors).toEqual([])
})

test('Collection filters narrow the actual rendered account rows @cross-browser', async ({ page }) => {
  await page.goto('/components/collection')
  const table = page.locator('#accounts-selection')
  const visibleNames = () => table.locator('tbody tr').evaluateAll(rows =>
    rows.filter(row => row.getClientRects().length).map(row => row.querySelector('th')?.textContent?.trim()))

  await page.getByRole('searchbox', { name: 'Search accounts', exact: true }).fill('assets')
  await expect.poll(visibleNames).toEqual(['Assets'])

  await page.getByRole('searchbox', { name: 'Search accounts', exact: true }).fill('')
  await page.getByRole('combobox', { name: 'Filter by account type', exact: true }).selectOption('liability')
  await expect.poll(visibleNames).toEqual(['Liabilities'])
})

test('Collection bulk actions receive and clear selected table identities @cross-browser', async ({ page }) => {
  await page.goto('/components/collection')
  await page.getByRole('checkbox', { name: 'Select Assets', exact: true }).check()

  const actions = page.getByRole('region', { name: 'Selected account actions', exact: true })
  await expect(actions).toContainText('1 selected')
  await page.getByRole('button', { name: 'Queue review', exact: true }).click()
  await expect(page.locator('#account-bulk-audit')).toHaveText('Queued review for account IDs: 101')

  await page.getByRole('button', { name: 'Clear selection', exact: true }).click()
  await expect(page.getByRole('checkbox', { name: 'Select Assets', exact: true })).not.toBeChecked()
  await expect(actions).toBeHidden()
})

test('AppShell mobile bottom navigation remains visible link navigation above page scroll @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/components/app-shell')

  const navigation = page.locator('#ledger-bottom-navigation')
  await navigation.evaluate(element => element.scrollIntoView({ block: 'end' }))
  await expect(navigation).toBeVisible()
  await expect(navigation.getByRole('link')).toHaveCount(4)
  await expect(navigation.getByRole('link', { name: 'Accounts', exact: true })).toHaveAttribute('aria-current', 'page')
  await expect(navigation.getByRole('tab')).toHaveCount(0)
  expect(await navigation.evaluate(element => {
    const bounds = element.getBoundingClientRect()
    return document.elementFromPoint(bounds.left + bounds.width / 2, bounds.top + bounds.height / 2)?.closest('#ledger-bottom-navigation') === element
  })).toBe(true)
})

test('gallery toolbars and examples remain accessible in narrow themes and resized text @cross-browser', async ({ page }, testInfo) => {
  for (const id of ['button', 'select', 'side-nav', 'table', 'input', 'breadcrumbs', 'collection', 'detail', 'app-shell']) {
    await page.setViewportSize({ width: 390, height: 1000 })
    await page.goto(`/components/${id}`)
    const gallery = page.locator('.docs-gallery-layout')
    for (const theme of ['Light', 'Dark']) {
      await page.getByRole('button', { name: 'Choose color theme' }).click()
      await page.getByRole('menuitemradio', { name: theme, exact: true }).click()
      await expect.poll(() => page.evaluate(() => document.getAnimations().filter(animation => animation instanceof CSSTransition && animation.playState === 'running').length)).toBe(0)
      expect((await new AxeBuilder({ page }).include('.docs-gallery-layout').analyze()).violations).toEqual([])
      if (['button', 'select'].includes(id)) {
        await page.screenshot({ path: testInfo.outputPath(`${id}-${theme.toLowerCase()}-390.png`) })
      }
    }
    await page.setViewportSize({ width: 320, height: 1000 })
    await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    for (const toolbar of await gallery.locator('.spec-example-toolbar').all()) {
      for (const control of await toolbar.getByRole('tab').or(toolbar.getByRole('button', { name: /^Copy / })).all()) {
        const box = (await control.boundingBox())!
        expect(box.x).toBeGreaterThanOrEqual(0)
        expect(box.x + box.width).toBeLessThanOrEqual(321)
        await expect(control).toBeEnabled()
      }
    }
    if (id === 'button') await page.screenshot({ path: testInfo.outputPath('button-dark-320-200.png') })
  }
})

test('denied gallery copying reports failure without losing the source @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  await page.evaluate(() => {
    navigator.clipboard.writeText = async () => { throw new DOMException('Permission denied', 'NotAllowedError') }
  })
  const example = page.locator('#components-button')
  const copy = example.getByRole('button', { name: 'Copy Primary buttons code' })
  await copy.click()
  await expect(copy).toHaveAttribute('data-copy-error', 'true')
  await expect(copy).toHaveText('Copy failed')
  await example.getByRole('tab', { name: 'Code', exact: true }).click()
  await expect(example.locator('[data-docs-copy-source]')).toContainText('Button.create "Create account"')
  await example.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(example.getByRole('button', { name: 'Create account', exact: true })).toHaveCount(3)
})

test('gallery code switches preserve independent edited previews @cross-browser', async ({ page }) => {
  await page.goto('/components/input')
  const name = page.locator('#components-input').getByRole('textbox', { name: 'Email', exact: true })
  const query = page.getByRole('searchbox', { name: 'Search', exact: true })
  await name.fill('Alex Rivera')
  await query.fill('Savings')
  const formToolbar = page.locator('#components-input .spec-example-toolbar')
  const searchToolbar = page.locator('#components-search-input .spec-example-toolbar')
  await formToolbar.getByRole('tab', { name: 'Code', exact: true }).click()
  await expect(query).toHaveValue('Savings')
  await searchToolbar.getByRole('tab', { name: 'Code', exact: true }).click()
  await formToolbar.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(name).toHaveValue('Alex Rivera')
  await searchToolbar.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(query).toHaveValue('Savings')
  await expect(page).toHaveURL(/\/components\/input$/)
})
