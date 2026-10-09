import { test, expect } from '../fixture'
import AxeBuilder from '@axe-core/playwright'

const representativeComponents = ['button', 'select', 'page-top-bar', 'textarea'] as const

test('representative component galleries share complete copyable code behavior', async ({ page, context }) => {
  await context.grantPermissions(['clipboard-write'])
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.addInitScript(() => {
    const write = navigator.clipboard.writeText.bind(navigator.clipboard)
    navigator.clipboard.writeText = async text => {
      await write(text)
      ;(window as any).__copiedExample = text
    }
  })

  for (const id of representativeComponents) {
    await page.goto(`/components/${id}`)
    const gallery = page.locator('[data-docs-layout="gallery"]')
    await expect(gallery).toBeVisible()
    await expect(page.locator('[data-docs-toc="true"]')).toHaveCount(2)
    const example = gallery.locator('[data-docs-example="true"]').first()
    const toolbar = example.locator(':scope > [data-docs-example-toolbar="true"]')
    await expect(toolbar.getByRole('heading', { level: 2 })).toBeVisible()
    await expect(toolbar.getByRole('tab', { name: 'Preview', exact: true })).toHaveAttribute('aria-selected', 'true')
    const desktopPreset = toolbar.getByRole('button', { name: 'Desktop preview' })
    const mobilePreset = toolbar.getByRole('button', { name: 'Mobile preview' })
    const frame = example.locator('[data-docs-preview-frame="true"]')
    await expect(desktopPreset).toHaveAttribute('aria-pressed', 'true')
    await mobilePreset.click()
    await expect(mobilePreset).toHaveAttribute('aria-pressed', 'true')
    await expect(frame).toHaveCSS('width', '390px')
    const handle = example.getByRole('separator', { name: /^Resize .+ preview$/ })
    const handleBox = await handle.boundingBox()
    await page.mouse.move(handleBox!.x + handleBox!.width / 2, handleBox!.y + handleBox!.height / 2)
    await page.mouse.down()
    await page.mouse.move(handleBox!.x - 40, handleBox!.y + handleBox!.height / 2)
    await page.mouse.up()
    await expect.poll(() => frame.evaluate(element => element.getBoundingClientRect().width)).toBeLessThan(390)
    await handle.focus()
    await handle.press('End')
    const fullWidth = await frame.evaluate(element => element.getBoundingClientRect().width)
    await handle.press('ArrowLeft')
    await expect.poll(() => frame.evaluate(element => element.getBoundingClientRect().width)).toBeLessThan(fullWidth)
    await toolbar.getByRole('tab', { name: 'Code', exact: true }).click()
    const code = example.locator('[data-docs-copy-source]')
    await expect(code).toBeVisible()
    const source = await code.textContent()
    const copy = example.getByRole('button', { name: /^Copy .+ code$/ })
    await copy.click()
    await expect(copy).toHaveAttribute('data-copied', 'true')
    expect(await page.evaluate(() => (window as any).__copiedExample)).toBe(source)
  }

  expect(errors).toEqual([])
})

test('documentation preview frame tracks the pointer and starts structural examples at full width @cross-browser', async ({ page }) => {
  await page.goto('/components/section-header')
  const example = page.locator('#components-sectionHeaderWithActions')
  await example.scrollIntoViewIfNeeded()
  const panel = example.locator('[data-docs-example-preview="true"]')
  const frame = example.locator('[data-docs-preview-frame="true"]')
  const handle = example.getByRole('separator', { name: 'Resize Description, actions and divider preview' })
  const section = frame.locator('[data-fve-section-header="true"]')

  const panelBox = await panel.boundingBox()
  const initialFrameBox = await frame.boundingBox()
  const initialSectionBox = await section.boundingBox()
  expect(initialFrameBox!.width).toBeCloseTo(panelBox!.width, 0)
  expect(initialSectionBox!.width).toBeCloseTo(await section.evaluate(element => {
    const parent = element.parentElement!
    const style = getComputedStyle(parent)
    return parent.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight)
  }), 0)
  await expect(frame).toHaveCSS('border-right-width', '1px')
  await expect(panel).toHaveCSS('border-right-width', '0px')

  const initialHandleBox = await handle.boundingBox()
  const pointerY = Math.min(initialHandleBox!.y + initialHandleBox!.height / 2, page.viewportSize()!.height - 10)
  await page.mouse.move(initialHandleBox!.x + initialHandleBox!.width / 2, pointerY)
  await page.mouse.down()
  await page.mouse.move(initialHandleBox!.x + initialHandleBox!.width / 2 - 160, pointerY, { steps: 8 })
  await page.mouse.up()

  const resizedFrameBox = await frame.boundingBox()
  const resizedHandleBox = await handle.boundingBox()
  expect(resizedFrameBox!.width - initialFrameBox!.width).toBeCloseTo(-160, 0)
  expect(resizedHandleBox!.x - initialHandleBox!.x).toBeCloseTo(-160, 0)

  await example.getByRole('button', { name: 'Desktop preview' }).click()
  await expect.poll(async () => (await frame.boundingBox())!.width).toBeCloseTo(panelBox!.width, 0)
  await example.getByRole('button', { name: 'Mobile preview' }).click()
  await expect(frame).toHaveCSS('width', '390px')
})

test('Resizable panels support pointer and keyboard resizing @cross-browser', async ({ page }) => {
  await page.goto('/components/resizable')
  const panels = page.locator('#account-workspace-panels')
  const leading = panels.locator(':scope > div').first()
  const handle = panels.locator('[data-fve-resize-handle="true"]')
  await expect(handle).toHaveAttribute('role', 'separator')
  await expect(handle).toHaveAttribute('aria-orientation', 'vertical')
  await handle.focus()
  await handle.press('ArrowRight')
  await expect(handle).toHaveAttribute('aria-valuenow', '36')
  const keyboardWidth = await leading.evaluate(element => element.getBoundingClientRect().width)
  const handleBox = await handle.boundingBox()
  await page.mouse.move(handleBox!.x + handleBox!.width / 2, handleBox!.y + handleBox!.height / 2)
  await page.mouse.down()
  await page.mouse.move(handleBox!.x + 48, handleBox!.y + handleBox!.height / 2)
  await page.mouse.up()
  await expect.poll(() => leading.evaluate(element => element.getBoundingClientRect().width)).toBeGreaterThan(keyboardWidth)
  await handle.press('Enter')
  await expect(handle).toHaveAttribute('aria-valuenow', '0')
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

test('Calendar component galleries stay focused without connected view switching @cross-browser', async ({ page }) => {
  await page.goto('/components/week-calendar')
  await expect(page.getByRole('heading', { name: 'Week calendar', level: 1, exact: true })).toBeVisible()
  await expect(page.getByRole('navigation', { name: /calendar view/ })).toHaveCount(0)
  await expect(page.locator('#components-week-calendar-events-panel-preview').getByText('Overlaps the trail lesson by one hour')).toBeVisible()
  await page.goto('/components/year-calendar')
  await expect(page.locator('#components-year-calendar-events-panel-preview [data-fve-month-calendar="compact"]')).toHaveCount(12)
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

  await example.getByRole('button', { name: 'Collapse Cash', exact: true }).click()
  await expect(checking).toBeHidden()
  await expect(example.getByRole('link', { name: 'Accounts receivable', exact: true })).toBeVisible()

  await example.getByRole('button', { name: 'Expand Cash', exact: true }).click()
  await expect(checking).toBeVisible()
})

test('Operational application examples preserve native input and recoverable actions @cross-browser', async ({ page }) => {
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

test('Bottom navigation remains visible native link navigation @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/components/bottom-navigation')

  const navigation = page.locator('#components-bottom-navigation-panel-preview').getByRole('navigation', { name: 'Primary navigation', exact: true })
  await navigation.evaluate(element => element.scrollIntoView({ block: 'end' }))
  await expect(navigation).toBeVisible()
  await expect(navigation.getByRole('link')).toHaveCount(4)
  await expect(navigation.getByRole('link', { name: 'Accounts', exact: true })).toHaveAttribute('aria-current', 'page')
  await expect(navigation.getByRole('tab')).toHaveCount(0)
  expect(await navigation.evaluate(element => {
    const bounds = element.getBoundingClientRect()
    return element.contains(document.elementFromPoint(bounds.left + bounds.width / 2, bounds.top + bounds.height / 2))
  })).toBe(true)
})

for (const id of ['button', 'select', 'side-nav', 'bottom-navigation', 'table', 'input', 'breadcrumbs', 'page-top-bar', 'day-calendar', 'week-calendar', 'month-calendar', 'year-calendar', 'date-picker', 'command', 'notice', 'skeleton', 'metric', 'toggle-button', 'toggle-group', 'button-group', 'tabs', 'resizable']) {
  test(`${id} gallery remains accessible in narrow themes and resized text`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width: 390, height: 1000 })
    await page.goto(`/components/${id}`)
    const gallery = page.locator('[data-docs-layout="gallery"]')
    for (const theme of ['Light', 'Dark']) {
      await page.getByRole('button', { name: 'Choose color theme' }).click()
      await page.getByRole('menuitemradio', { name: theme, exact: true }).click()
      await expect.poll(() => page.evaluate(() => document.getAnimations().filter(animation => animation instanceof CSSTransition && animation.playState === 'running').length)).toBe(0)
      expect((await new AxeBuilder({ page }).include('[data-docs-layout="gallery"]').analyze()).violations).toEqual([])
      if (['button', 'select', 'bottom-navigation', 'month-calendar', 'year-calendar', 'date-picker'].includes(id)) {
        await page.screenshot({ path: testInfo.outputPath(`${id}-${theme.toLowerCase()}-390.png`) })
      }
    }
    await page.setViewportSize({ width: 320, height: 1000 })
    await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    if (id === 'tabs') {
      const code = gallery.getByRole('region', { name: 'Button F# code', exact: true })
      await code.focus()
      await page.keyboard.press('ArrowRight')
      await expect.poll(() => code.evaluate(el => el.scrollLeft)).toBeGreaterThan(0)
    }
    for (const toolbar of await gallery.locator('[data-docs-example-toolbar="true"]').all()) {
      for (const control of await toolbar.getByRole('tab').or(toolbar.getByRole('button', { name: /^Copy / })).all()) {
        const box = (await control.boundingBox())!
        expect(box.x).toBeGreaterThanOrEqual(0)
        expect(box.x + box.width).toBeLessThanOrEqual(321)
        await expect(control).toBeEnabled()
      }
    }
    if (id.endsWith('-calendar') || id === 'bottom-navigation') {
      for (const control of await gallery.locator('[data-docs-example-preview="true"] a:not([role="grid"] a), [data-docs-example-preview="true"] button:not([role="grid"] button)').all()) {
        if (!await control.isVisible()) continue
        await control.scrollIntoViewIfNeeded()
        const { left, right, text, scrollport } = await control.evaluate(el => {
          const box = el.getBoundingClientRect()
          const region = el.closest('.fve-calendar-body')?.getBoundingClientRect()
          return {
            left: box.left, right: box.right, text: el.textContent,
            scrollport: region ? { left: region.left, right: region.right } : null,
          }
        })
        if (scrollport) {
          // A genuine two-dimensional grid may contain cards wider than the mobile viewport.
          expect(Math.min(right, scrollport.right) - Math.max(left, scrollport.left), text).toBeGreaterThan(0)
          expect(scrollport.left).toBeGreaterThanOrEqual(0)
          expect(scrollport.right).toBeLessThanOrEqual(321)
        } else {
          expect(left, text).toBeGreaterThanOrEqual(0)
          expect(right, text).toBeLessThanOrEqual(321)
        }
      }
    }
    if (id === 'button' || id === 'month-calendar') await page.screenshot({ path: testInfo.outputPath(`${id}-dark-320-200.png`) })
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
  await expect(example.locator('[data-docs-copy-source]')).toContainText('Button.create (ButtonContent.Text "Create account")')
  await example.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(example.getByRole('button', { name: 'Create account', exact: true })).toHaveCount(3)
})

test('gallery code switches preserve independent edited previews @cross-browser', async ({ page }) => {
  await page.goto('/components/input')
  const name = page.locator('#components-input-panel-preview').getByRole('textbox', { name: 'Email', exact: true })
  const query = page.getByRole('searchbox', { name: 'Search', exact: true })
  await name.fill('Alex Rivera')
  await query.fill('Savings')
  const formToolbar = page.locator('#components-input-panel-preview').locator('..').locator(':scope > [data-docs-example-toolbar="true"]')
  const searchToolbar = page.locator('#components-search-input [data-docs-example-toolbar="true"]')
  await formToolbar.getByRole('tab', { name: 'Code', exact: true }).click()
  await expect(query).toHaveValue('Savings')
  await searchToolbar.getByRole('tab', { name: 'Code', exact: true }).click()
  await formToolbar.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(name).toHaveValue('Alex Rivera')
  await searchToolbar.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(query).toHaveValue('Savings')
  await expect(page).toHaveURL(/\/components\/input$/)
})

test('Item metadata interaction stays independent of its stretched row link @cross-browser', async ({ page }) => {
  await page.goto('/components/item')
  const preview = page.locator('#components-item-linked-metadata-panel-preview')
  const edit = preview.getByRole('button', { name: 'Edit', exact: true })
  expect(await edit.evaluate(element => element.closest('a') === null)).toBe(true)
  await edit.click()
  const dialog = page.getByRole('dialog', { name: 'Edit account name', exact: true })
  await expect(dialog.getByRole('textbox', { name: 'Account name', exact: true })).toBeFocused()
  await expect(page).toHaveURL('/components/item')
  await page.keyboard.press('Escape')
  await expect(dialog).toBeHidden()
  await expect(edit).toBeFocused()
  // Component galleries deliberately suppress destination activation; copied Items retain this native link.
  await expect(preview.getByRole('link', { name: 'Operating account', exact: true })).toHaveAttribute('href', '/examples/application/accounts/101')
})
