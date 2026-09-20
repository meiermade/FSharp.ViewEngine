import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const components = [
  ['button', 'Button.create'], ['icon-button', 'IconButton.create'], ['badge', 'Badge.create'],
  ['status', 'Status.create'], ['loading-indicator', 'LoadingIndicator.create'], ['progress', 'Progress.create'], ['empty-state', 'EmptyState.create'],
  ['action-cluster', 'ActionCluster.create'], ['row-actions', 'RowActions.create'],
  ['table', 'Table.create'], ['description-list', 'DescriptionList.create'], ['metric', 'Metric.text'],
  ['pagination', 'Pagination.create'], ['avatar', 'Avatar.create'], ['copy-reveal', 'CopyReveal.create'],
  ['input', 'Input.create'], ['file-selection', 'FileSelection.create'], ['tag-input', 'TagInput.create'], ['textarea', 'Textarea.create'], ['form-layouts', 'Input.create'],
  ['error-summary', 'ErrorSummary.create'], ['notice', 'Notice.create'], ['notification', 'Notification.create'], ['select', 'Select.create'],
  ['checkbox', 'Checkbox.create'], ['switch', 'Switch.create'],
  ['toggle-button', 'ToggleButton.create'], ['tabs', 'Tabs.create'], ['radio-group', 'RadioGroup.create'], ['choice-cards', 'ChoiceCards.single'],
  ['dropdown-menu', 'DropdownMenu.create'], ['dialog', 'Dialog.create'], ['confirmation-dialog', 'ConfirmationDialog.create'],
  ['drawer', 'Drawer.create'], ['floating-panel', 'FloatingPanel.create'], ['breadcrumbs', 'Breadcrumbs.create'], ['side-nav', 'SideNav.create'],
  ['page-top-bar', 'PageTopBar.create'], ['page-header', 'PageHeader.create'], ['section', 'Section.create'],
  ['page', 'Page.create'], ['collection', 'Collection.create'], ['detail', 'Detail.create'], ['app-shell', 'AppShell.create'],
  ['bottom-navigation', 'BottomNavigation.create'], ['bulk-actions', 'BulkActions.create'], ['upload', 'UploadList.create'],
  ['steps', 'Steps.create'], ['first-steps', 'FirstSteps.create'], ['calendar', 'Calendar.create'], ['media-library', 'MediaLibrary.create'],
  ['page-examples/account-management', 'AppShell.create'],
  ['page-examples/dependency-graph', 'svg'], ['page-examples/execution-detail', 'traceSpans'],
  ['page-examples/financial-reporting', 'polyline'], ['page-examples/messaging', 'Textarea.create'],
  ['page-examples/operations-dashboard', 'FirstSteps.create'], ['page-examples/scheduling', 'Calendar.create'],
  ['page-examples/media-management', 'MediaLibrary.create'],
]

test.describe('component gallery code', () => {
  for (const [id, api] of components) {
    test(`${id} exposes named previews and complete copyable code @cross-browser`, async ({ page, context, browserName }, testInfo) => {
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
      await page.goto(`/components/${id}`)
      const gallery = page.locator('.docs-gallery-layout')
      await expect(gallery).toBeVisible()
      await expect(page.locator('.spec-toc-nav, .spec-mobile-toc-nav')).toHaveCount(0)
      await expect(page.getByRole('heading', { name: /^(Usage|Example setup|Accessibility)$/ })).toHaveCount(0)
      await expect(page.getByRole('link', { name: 'Imports and supporting code', exact: true })).toHaveCount(0)
      const examples = gallery.locator('[data-docs-example="true"]')
      for (const example of await examples.all()) {
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
        const copy = example.getByRole('button', { name: /^Copy .+ code$/ })
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
      expect(errors).toEqual([])
    })
  }
})

test('Dedicated action pages preserve menu feedback and stable bulk selection @cross-browser', async ({ page }) => {
  await page.goto('/components/action-cluster')
  const actionCluster = page.locator('#components-action-cluster-panel-preview')
  await actionCluster.getByRole('button', { name: 'More actions', exact: true }).first().click()
  await actionCluster.getByRole('menuitem', { name: 'Archive account', exact: true }).click()
  await expect(actionCluster.locator('output')).toHaveText('Archive requested.')

  await page.goto('/components/row-actions')
  const rowActions = page.locator('#components-row-actions-panel-preview')
  await rowActions.getByRole('button', { name: 'More actions for Operating checking', exact: true }).click()
  await rowActions.getByRole('menuitem', { name: 'Archive account', exact: true }).click()
  await expect(rowActions.locator('output')).toHaveText('Archive requested for Operating checking.')

  await page.goto('/components/bulk-actions')
  const bulkActions = page.locator('#components-bulk-actions-panel-preview')
  await bulkActions.getByRole('checkbox', { name: 'Select Alex Morgan', exact: true }).check()
  await bulkActions.getByRole('button', { name: 'Queue review', exact: true }).click()
  await expect(bulkActions.locator('#bulk-action-feedback')).toHaveText('Queued IDs: 1')
})

test('Collection filters narrow the actual rendered account rows @cross-browser', async ({ page }) => {
  await page.goto('/components/collection')
  const table = page.locator('#accounts-selection')
  const visibleNames = () => table.locator('tbody tr').evaluateAll(rows =>
    rows.filter(row => row.getClientRects().length).map(row => row.querySelector('th')?.textContent?.trim()))

  const search = page.getByRole('searchbox', { name: 'Search accounts', exact: true })
  await search.fill('assets')
  await expect.poll(visibleNames).toEqual(['Assets'])
  await expect(page).toHaveURL(/query=assets/)

  await page.reload()
  await expect(page.getByRole('searchbox', { name: 'Search accounts', exact: true })).toHaveValue('assets')
  await expect.poll(visibleNames).toEqual(['Assets'])

  await page.getByRole('button', { name: 'Clear filters', exact: true }).click()
  await expect.poll(visibleNames).toHaveLength(6)
  await expect(page).not.toHaveURL(/query=/)

  await page.getByRole('searchbox', { name: 'Search accounts', exact: true }).fill('nothing-here')
  await expect(page.getByText('No accounts match these filters. Clear filters to restore all accounts.')).toBeVisible()
  await page.getByRole('button', { name: 'Clear filters', exact: true }).click()

  await page.getByRole('button', { name: 'Refresh filters', exact: true }).click()
  await expect(page.getByRole('status').filter({ hasText: 'Refreshing filter options' })).toBeVisible()
  await expect(page.getByRole('searchbox', { name: 'Search accounts', exact: true })).toBeDisabled()
  await expect(page.getByRole('searchbox', { name: 'Search accounts', exact: true })).toBeEnabled()

  await page.getByRole('button', { name: 'Simulate filter error', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('Filter options are temporarily unavailable.')
  await page.getByRole('button', { name: 'Retry filters', exact: true }).click()
  await expect(page.getByRole('alert')).toBeHidden()

  await page.getByRole('combobox', { name: 'Filter by account type', exact: true }).selectOption('liability')
  await expect.poll(visibleNames).toEqual(['Liabilities'])
  await expect(page).toHaveURL(/accountType=liability/)
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

test('Free-form tags add, reject, remove, and submit repeated native values @cross-browser', async ({ page }) => {
  await page.goto('/components/tag-input')
  const example = page.locator('#components-tag-input-panel-preview')
  const entry = example.getByRole('textbox', { name: 'Tags', exact: true })
  await entry.fill('priority')
  await entry.press('Enter')
  await expect(example.getByRole('button', { name: 'Remove priority', exact: true })).toBeVisible()
  await expect(example.getByRole('status')).toHaveText('priority added.')

  await entry.fill('priority')
  await example.getByRole('button', { name: 'Add tag', exact: true }).click()
  await expect(example.getByRole('status')).toHaveText('That tag has already been added.')
  await expect(example.locator('input[type="hidden"][name="tags"][value="priority"]')).toHaveCount(1)

  await entry.evaluate(element => {
    const event = new Event('paste', { bubbles: true, cancelable: true })
    Object.defineProperty(event, 'clipboardData', { value: { getData: () => 'owner,\nblocked' } })
    element.dispatchEvent(event)
  })
  await expect(example.getByRole('button', { name: 'Remove owner', exact: true })).toBeVisible()
  await expect(example.getByRole('button', { name: 'Remove blocked', exact: true })).toBeVisible()

  await example.getByRole('button', { name: 'Remove priority', exact: true }).click()
  await expect(example.getByRole('button', { name: 'Remove priority', exact: true })).toHaveCount(0)
  await expect(entry).toBeFocused()
})

test('Calendar gallery keeps day, week, month and year views independent @cross-browser', async ({ page }) => {
  await page.goto('/components/calendar')
  await expect(page.getByRole('heading', { name: 'Month view', exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Week view', exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Day view', exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Year view', exact: true })).toBeVisible()
  await expect(page.getByRole('navigation', { name: 'Calendar example states', exact: true })).toHaveCount(0)
  await expect(page.locator('#components-calendar-week-panel-preview').getByText('Overlaps the trail lesson by one hour')).toBeVisible()
  await expect(page.locator('#components-calendar-year-panel-preview .fve-calendar-year-month')).toHaveCount(12)
})

test('Media library shares stable selection with page-level bulk actions @cross-browser', async ({ page }) => {
  await page.goto('/components/media-library')
  const example = page.locator('#components-media-library-panel-preview')
  const detail = example.getByRole('checkbox', { name: 'Select Trail pack detail', exact: true })
  await detail.check()
  const libraryStatus = example.locator('[data-fve-media-library] output[role="status"]')
  await expect(libraryStatus).toHaveText('2 selected')
  await expect(example.getByRole('button', { name: 'Set primary', exact: true })).toBeVisible()
  await example.getByRole('button', { name: 'Clear selection', exact: true }).click()
  await expect(libraryStatus).toHaveText('0 selected')
  await expect(detail).not.toBeChecked()

  await detail.check()
  await example.getByRole('textbox', { name: 'Alt text', exact: true }).fill('Updated strap detail')
  await example.getByRole('button', { name: 'Save alt text', exact: true }).click()
  await expect(example.getByRole('img', { name: 'Updated strap detail', exact: true })).toBeVisible()
  await example.getByRole('button', { name: 'Set primary', exact: true }).click()
  await expect(example.getByRole('status').last()).toHaveText('Primary asset: trail-detail')
  await example.getByRole('button', { name: 'Apply demo replacement', exact: true }).click()
  await expect(example.getByRole('img', { name: 'Updated strap detail', exact: true })).toHaveAttribute('src', '/favicon-32x32.png')

  await example.getByRole('button', { name: 'Simulate loading', exact: true }).click()
  await expect(example.getByRole('status', { name: '' }).filter({ hasText: 'Loading media' })).toBeVisible()
  await example.getByRole('button', { name: 'Simulate error', exact: true }).click()
  await expect(example.getByRole('alert')).toContainText('Media could not be loaded.')
  await example.getByRole('button', { name: 'Retry media', exact: true }).click()
  await expect(example.getByRole('alert')).toBeHidden()
  await example.getByRole('button', { name: 'Show empty', exact: true }).click()
  await expect(example.getByText('No media assets. Upload an image to begin.')).toBeVisible()
  await example.getByRole('button', { name: 'Restore media', exact: true }).click()
  await expect(example.getByRole('region', { name: 'Selected media actions', exact: true })).toBeVisible()
})

test('Former graph-and-trace links reach the complete dependency workspace @cross-browser', async ({ page }) => {
  await page.goto('/components/page-examples/graph-and-trace')
  await expect(page).toHaveURL('/components/page-examples/dependency-graph')
  const workspace = page.locator('[data-fve-fixture-id="page-workspace"]')
  await expect(workspace.getByRole('searchbox', { name: 'Search dependencies' })).toBeVisible()
  await expect(workspace.getByRole('button', { name: /Simulate/ })).toHaveCount(0)
  await expect(page.locator('#page-content')).toContainText('dependency')
  // Working graph/span, financial, message, and review-state journeys live in workspace-pages.spec.ts.
})

test('Rich choice cards retain native selection and disabled semantics @cross-browser', async ({ page }) => {
  await page.goto('/components/choice-cards')
  const example = page.locator('#components-choice-cards-panel-preview')
  const selfService = example.getByRole('radio', { name: /Self-service/, exact: false })
  const assisted = example.getByRole('radio', { name: /Staff assisted/, exact: false })
  const unavailable = example.getByRole('radio', { name: /Managed service/, exact: false })
  await expect(assisted).toBeChecked()
  await selfService.check()
  await expect(selfService).toBeChecked()
  await expect(assisted).not.toBeChecked()
  await expect(unavailable).toBeDisabled()
})

test('Hierarchical tables disclose only their matching descendants @cross-browser', async ({ page }) => {
  await page.goto('/components/table')
  const example = page.locator('#components-table-hierarchy-panel-preview')
  const checking = example.getByRole('link', { name: 'Operating checking', exact: true })
  await expect(checking).toBeVisible()

  await example.getByRole('button', { name: 'Toggle Cash', exact: true }).click()
  await expect(checking).toBeHidden()
  await expect(example.getByRole('link', { name: 'Accounts receivable', exact: true })).toBeVisible()

  await example.getByRole('button', { name: 'Toggle Cash', exact: true }).click()
  await expect(checking).toBeVisible()
})

test('Operational application examples preserve native input and recoverable actions @cross-browser', async ({ page }) => {
  await page.goto('/components/first-steps')
  const firstSteps = page.locator('#components-first-steps-panel-preview')
  await firstSteps.getByRole('button', { name: 'Minimize First steps', exact: true }).click()
  await expect(firstSteps.getByRole('region', { name: 'First steps', exact: true })).toBeHidden()
  await firstSteps.getByRole('button', { name: 'Open First steps', exact: true }).click()
  await expect(firstSteps.getByRole('region', { name: 'First steps', exact: true })).toBeVisible()

  await page.goto('/components/file-selection')
  const file = page.locator('#statement-files')
  await file.setInputFiles([
    { name: 'checking.csv', mimeType: 'text/csv', buffer: Buffer.from('date,amount') },
    { name: 'card.ofx', mimeType: 'application/x-ofx', buffer: Buffer.from('OFX') },
  ])
  await expect(page.locator('#statement-files-selected')).toContainText('checking.csv')
  await expect(page.locator('#statement-files-selected')).toContainText('card.ofx')
  await page.locator('#components-file-selection-panel-preview').getByRole('button', { name: 'Clear selected files', exact: true }).click()
  await expect(file).toHaveValue('')

  await page.goto('/components/upload')
  await page.locator('#components-upload-panel-preview').getByRole('button', { name: 'Retry', exact: true }).click()
  await expect(page.locator('#components-upload-panel-preview').getByRole('status').last()).toHaveText('Retry queued for savings-july.csv.')

  await page.goto('/components/progress')
  await expect(page.getByRole('progressbar', { name: 'Statement import', exact: true }).first()).toHaveAttribute('value', '68')

  await page.goto('/components/steps')
  const steps = page.locator('#components-steps-panel-preview')
  await expect(steps.getByRole('navigation', { name: 'Period close progress', exact: true }).locator('[aria-current="step"]')).toContainText('Reconcile statements')
  await steps.getByRole('button', { name: 'Continue', exact: true }).click()
  await expect(steps.getByRole('alert')).toContainText('The current step has not changed.')
  await expect(steps.getByRole('navigation', { name: 'Period close progress', exact: true }).locator('[aria-current="step"]')).toContainText('Reconcile statements')
  await steps.getByRole('textbox', { name: 'Reconciliation note', exact: true }).fill('Statement totals agree.')
  await steps.getByRole('button', { name: 'Continue', exact: true }).click()
  await expect(steps.getByRole('status')).toHaveText('Reconciliation is ready for server validation.')

  await page.goto('/components/copy-reveal')
  const identity = page.locator('#components-copy-reveal-panel-preview')
  const credential = page.locator('#demo-token')
  const reveal = identity.getByRole('button', { name: 'Reveal', exact: true })
  await expect(credential).toHaveAttribute('type', 'password')
  await reveal.focus()
  await page.keyboard.press('Space')
  await expect(credential).toHaveAttribute('type', 'text')
  await expect(identity.getByRole('button', { name: 'Hide', exact: true })).toBeFocused()

  await page.evaluate(() => Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: () => Promise.reject(new Error('denied')) } }))
  await identity.getByRole('button', { name: 'Copy', exact: true }).click()
  await expect(identity.getByRole('status')).toHaveText('Could not copy Demo API token. Check clipboard permissions.')
  await page.evaluate(() => Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: () => Promise.resolve() } }))
  await identity.getByRole('button', { name: 'Copy', exact: true }).click()
  await expect(identity.getByRole('status')).toHaveText('Demo API token copied.')
})

test('AppShell mobile bottom navigation remains visible link navigation above page scroll @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/components/page-examples/account-management')

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

for (const id of ['button', 'select', 'side-nav', 'bottom-navigation', 'table', 'input', 'breadcrumbs', 'collection', 'detail', 'app-shell', 'calendar']) {
  test(`${id} gallery remains accessible in narrow themes and resized text @cross-browser`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width: 390, height: 1000 })
    await page.goto(`/components/${id}`)
    const gallery = page.locator('.docs-gallery-layout')
    for (const theme of ['Light', 'Dark']) {
      await page.getByRole('button', { name: 'Choose color theme' }).click()
      await page.getByRole('menuitemradio', { name: theme, exact: true }).click()
      await expect.poll(() => page.evaluate(() => document.getAnimations().filter(animation => animation instanceof CSSTransition && animation.playState === 'running').length)).toBe(0)
      expect((await new AxeBuilder({ page }).include('.docs-gallery-layout').analyze()).violations).toEqual([])
      if (['button', 'select', 'bottom-navigation', 'calendar'].includes(id)) {
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
    if (id === 'calendar' || id === 'bottom-navigation') {
      for (const control of await gallery.locator('.spec-example-preview a, .spec-example-preview button').all()) {
        if (!await control.isVisible()) continue
        const box = (await control.boundingBox())!
        expect(box.x, await control.textContent()).toBeGreaterThanOrEqual(0)
        expect(box.x + box.width, await control.textContent()).toBeLessThanOrEqual(321)
      }
    }
    if (id === 'button' || id === 'calendar') await page.screenshot({ path: testInfo.outputPath(`${id}-dark-320-200.png`) })
  })
}

test('denied gallery copying reports failure without losing the source @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  await page.evaluate(() => {
    navigator.clipboard.writeText = async () => { throw new DOMException('Permission denied', 'NotAllowedError') }
  })
  const example = page.locator('#components-button')
  await example.getByRole('tab', { name: 'Code', exact: true }).click()
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
