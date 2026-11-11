import { test, expect, type Locator } from '@playwright/test'

// Resolve modern CSS colors through canvas and composite transparent ancestor surfaces.
const contrast = (control: Locator, property: 'color' | 'outlineColor' = 'color') => control.evaluate((el, property) => {
  const canvas = document.createElement('canvas')
  canvas.width = canvas.height = 1
  const context = canvas.getContext('2d')!
  const rgba = (color: string) => {
    context.clearRect(0, 0, 1, 1)
    context.fillStyle = color
    context.fillRect(0, 0, 1, 1)
    return [...context.getImageData(0, 0, 1, 1).data]
  }
  const blend = (front: number[], back: number[]) => front.slice(0, 3).map((v, i) => v * front[3] / 255 + back[i] * (1 - front[3] / 255))
  const ancestors: Element[] = []
  for (let node: Element | null = el; node; node = node.parentElement) ancestors.unshift(node)
  const background = ancestors.reduce((back, node) => blend(rgba(getComputedStyle(node).backgroundColor), back), [255, 255, 255])
  const foreground = blend(rgba(getComputedStyle(el)[property]), background)
  const luminance = (rgb: number[]) => rgb.map(v => v / 255).map(v => v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4).reduce((sum, v, i) => sum + v * [0.2126, 0.7152, 0.0722][i], 0)
  const a = luminance(foreground), b = luminance(background)
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05)
}, property)

// Assert browser output rather than Tailwind class spelling.
const translation = (control: Locator) => control.evaluate(el => getComputedStyle(el).translate)

test('badges expose independent readable colors and variants without live-region behavior @cross-browser', async ({ page }) => {
  await page.goto('/components/badge')
  const basic = page.locator('#components-badge-panel-preview').getByText('Internal', { exact: true })
  const colors = page.locator('#components-badge-colors-panel-preview span.inline-flex')
  const variants = page.locator('#components-badge-variants-panel-preview span.inline-flex')
  await expect(colors).toHaveCount(7)
  await expect(variants).toHaveCount(4)
  await expect(basic).not.toHaveAttribute('role', /status|alert/)
  for (const dark of [false, true]) {
    await page.evaluate(dark => document.documentElement.classList.toggle('dark', dark), dark)
    const basicStyle = await basic.evaluate(el => ({ shadow: getComputedStyle(el).boxShadow, background: getComputedStyle(el).backgroundColor }))
    expect(basicStyle.shadow).toBe('none')
    expect(basicStyle.background).not.toBe('rgba(0, 0, 0, 0)')
    for (const badge of await colors.all()) await expect.poll(() => contrast(badge)).toBeGreaterThanOrEqual(4.5)
    for (const badge of await variants.all()) await expect.poll(() => contrast(badge)).toBeGreaterThanOrEqual(4.5)
    const [solid, soft, outline, ghost] = await variants.all()
    expect(await solid.evaluate(el => getComputedStyle(el).backgroundColor)).not.toBe(await soft.evaluate(el => getComputedStyle(el).backgroundColor))
    expect(await outline.evaluate(el => getComputedStyle(el).boxShadow)).toContain('inset')
    expect(await ghost.evaluate(el => getComputedStyle(el).backgroundColor)).toBe('rgba(0, 0, 0, 0)')
  }
})

test('Toggle controls apply Small, Medium and Large geometry without invalid group ARIA @cross-browser', async ({ page }) => {
  await page.goto('/components/toggle-button')
  for (const [id, height] of [['components-compact-rows', 32], ['toggle-soft', 28], ['toggle-outline', 32], ['toggle-ghost', 36]] as const) {
    await expect.poll(async () => (await page.locator(`#${id}`).boundingBox())!.height).toBeCloseTo(height, 1)
  }
  await expect(page.locator('#toggle-outline')).toHaveCSS('border-top-width', '1px')
  await page.goto('/components/toggle-group')
  const group = page.locator('#components-alignment-default')
  await expect(group).not.toHaveAttribute('aria-orientation')
  for (const button of await group.getByRole('button').all()) expect((await button.boundingBox())!.height).toBeCloseTo(32, 1)
  for (const button of await page.locator('#components-formatting-group').getByRole('button').all()) {
    expect((await button.boundingBox())!.height).toBeCloseTo(28, 1)
    await expect(button).toHaveCSS('border-inline-start-width', '1px')
    expect(parseFloat(await button.evaluate(el => getComputedStyle(el).borderTopLeftRadius))).toBeGreaterThan(0)
  }
  const [first, middle, last] = await group.getByRole('button').all()
  expect(parseFloat(await first.evaluate(el => getComputedStyle(el).borderTopLeftRadius))).toBeGreaterThan(0)
  await expect(first).toHaveCSS('border-top-right-radius', '0px')
  for (const button of [middle, last]) await expect(button).toHaveCSS('border-inline-start-width', '0px')
  await expect(middle).toHaveCSS('border-top-left-radius', '0px')
  await expect(middle).toHaveCSS('border-top-right-radius', '0px')
  await expect(last).toHaveCSS('border-top-left-radius', '0px')
  expect(parseFloat(await last.evaluate(el => getComputedStyle(el).borderTopRightRadius))).toBeGreaterThan(0)
  const boxes = await Promise.all([first.boundingBox(), middle.boundingBox(), last.boundingBox()])
  expect(boxes[1]!.x - boxes[0]!.x - boxes[0]!.width).toBeCloseTo(0, 1)
  expect(boxes[2]!.x - boxes[1]!.x - boxes[1]!.width).toBeCloseTo(0, 1)
  const vertical = page.locator('#components-view-group')
  const verticalButtons = await vertical.getByRole('button').all()
  for (const button of verticalButtons.slice(1)) {
    await expect(button).toHaveCSS('border-block-start-width', '0px')
    await expect(button).toHaveCSS('border-top-left-radius', '0px')
  }
  await group.evaluate(el => el.setAttribute('dir', 'rtl'))
  expect(parseFloat(await first.evaluate(el => getComputedStyle(el).borderTopRightRadius))).toBeGreaterThan(0)
  await expect(first).toHaveCSS('border-top-left-radius', '0px')
  await group.evaluate(el => el.removeAttribute('dir'))
  const connected = page.locator('#connected-formats')
  const connectedButtons = await connected.getByRole('button').all()
  await expect(connected.getByRole('button', { pressed: true })).toHaveText(['Bold', 'Italic'])
  expect(parseFloat(await connectedButtons[0].evaluate(el => getComputedStyle(el).borderTopLeftRadius))).toBeGreaterThan(0)
  await expect(connectedButtons[1]).toHaveCSS('border-top-left-radius', '0px')
  expect(parseFloat(await connectedButtons[2].evaluate(el => getComputedStyle(el).borderTopRightRadius))).toBeGreaterThan(0)
  await expect(connected.locator('input[type="hidden"]')).toHaveCount(2)
  const only = page.locator('#single-toggle-item').getByRole('button')
  for (const corner of ['border-top-left-radius', 'border-top-right-radius', 'border-bottom-left-radius', 'border-bottom-right-radius']) {
    expect(parseFloat(await only.evaluate((el, corner) => getComputedStyle(el).getPropertyValue(corner), corner))).toBeGreaterThan(0)
  }
  await expect(only).toHaveCSS('border-inline-start-width', '1px')
  await page.setViewportSize({ width: 320, height: 1000 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  const frame = (await page.locator('#components-toggle-group-default-panel-preview').boundingBox())!
  const region = (await group.boundingBox())!
  expect(region.x).toBeGreaterThanOrEqual(frame.x)
  expect(region.x + region.width).toBeLessThanOrEqual(frame.x + frame.width)
  await group.getByRole('button', { name: 'Center', exact: true }).focus()
  await page.keyboard.press('ArrowRight')
  const right = group.getByRole('button', { name: 'Right', exact: true })
  await expect(right).toBeFocused()
  await expect.poll(async () => {
    const box = (await right.boundingBox())!
    return box.x >= region.x && box.x + box.width <= region.x + region.width + 1
  }).toBe(true)
})

test('outlined fields and pagination use real borders without growing their control heights @cross-browser', async ({ page }) => {
  for (const [route, selector, height] of [
    ['/components/tag-input', '#components-tag-input-panel-preview [data-fve-tag-entry]', 32],
    ['/components/copy-reveal', '#demo-token', 32],
    ['/components/file-selection', '#components-file-selection-panel-preview input[type=file]', 32],
    ['/components/pagination', '#components-pagination-panel-preview [aria-current=page]', 36],
  ] as const) {
    await page.goto(route)
    const control = page.locator(selector)
    await expect(control).toHaveCSS('border-top-width', '1px')
    await expect(control).toHaveCSS('border-bottom-width', '1px')
    await expect.poll(async () => (await control.boundingBox())!.height).toBe(height)
  }
})

test('Select retains a contrasting keyboard outline in light and dark modes @cross-browser', async ({ page }) => {
  await page.goto('/components/select')
  const control = page.locator('[data-docs-example-preview="true"]').first().getByRole('combobox')
  for (const dark of [false, true]) {
    await page.evaluate(dark => document.documentElement.classList.toggle('dark', dark), dark)
    await control.focus()
    await expect(control).toBeFocused()
    expect(await control.evaluate(el => el.matches(':focus-visible'))).toBe(true)
    await expect(control).toHaveCSS('outline-width', '2px')
    await expect(control).toHaveCSS('outline-style', 'solid')
    await expect.poll(() => contrast(control, 'outlineColor')).toBeGreaterThanOrEqual(3)
  }
})

test('Metric previews center single content and reflow multiple metrics locally @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 1600, height: 1000 })
  await page.goto('/components/metric')
  const basic = page.locator('#components-metric-default-panel-preview')
  const frame = (await basic.boundingBox())!
  const label = (await basic.getByText('Total items', { exact: true }).boundingBox())!
  expect(Math.abs(label.x + label.width / 2 - frame.x - frame.width / 2)).toBeLessThanOrEqual(2)

  const example = page.locator('#components-metric-multiple')
  const group = example.getByRole('group', { name: 'Monthly activity metrics', exact: true })
  const labels = ['Views', 'Downloads', 'Items'].map(name => group.getByText(name, { exact: true }))
  await expect(group.getByText('September 2026', { exact: true })).toHaveCount(3)
  await expect(group.getByText('48,000', { exact: true })).toBeVisible()
  await expect(group.getByText('3,120', { exact: true })).toBeVisible()
  await expect(group.getByText('1,280', { exact: true })).toBeVisible()
  const desktop = await Promise.all(labels.map(el => el.boundingBox()))
  expect(Math.max(...desktop.map(box => box!.y)) - Math.min(...desktop.map(box => box!.y))).toBeLessThanOrEqual(1)
  await example.getByRole('button', { name: 'Mobile preview', exact: true }).click()
  const mobile = await Promise.all(labels.map(el => el.boundingBox()))
  expect(mobile[1]!.y).toBeGreaterThan(mobile[0]!.y + mobile[0]!.height)
  expect(mobile[2]!.y).toBeGreaterThan(mobile[1]!.y + mobile[1]!.height)
  await example.getByRole('button', { name: 'Desktop preview', exact: true }).click()
  await page.setViewportSize({ width: 320, height: 1200 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  expect(await group.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
})

test('Skeleton reserves visible geometry and respects reduced motion @cross-browser', async ({ page }) => {
  await page.goto('/components/skeleton')
  const basic = page.locator('#components-skeleton-default-panel-preview').getByRole('status')
  const line = basic.locator('[aria-hidden="true"]')
  await expect(line).toHaveCount(1)
  expect((await line.boundingBox())!.width).toBeGreaterThanOrEqual(160)
  expect((await line.boundingBox())!.height).toBeGreaterThanOrEqual(16)

  const avatar = page.locator('#components-skeleton-avatar-panel-preview').getByRole('status')
  const shapes = avatar.locator('[aria-hidden="true"]')
  await expect(shapes).toHaveCount(3)
  const [circle, first, second] = await Promise.all((await shapes.all()).map(el => el.boundingBox()))
  expect(circle!.width).toBeCloseTo(circle!.height)
  expect(first!.width).toBeGreaterThanOrEqual(150)
  expect(second!.width).toBeLessThan(first!.width)
  expect(first!.x).toBeGreaterThan(circle!.x + circle!.width)
  expect(first!.y).toBeLessThan(circle!.y + circle!.height)
  expect(await shapes.first().evaluate(el => parseFloat(getComputedStyle(el).borderRadius))).toBeGreaterThanOrEqual(circle!.width / 2)
  const card = page.locator('#components-skeleton-card-panel-preview').getByRole('status').locator('[aria-hidden="true"]').first()
  await expect(card).toHaveCSS('border-radius', '12px')
  const explicit = page.locator('#components-skeleton-dimensions-panel-preview').getByRole('status').locator('[aria-hidden="true"]')
  await explicit.evaluate(el => el.classList.add('w-full', 'h-8'))
  await expect(explicit).toHaveCSS('height', '16px')
  const sizes = await explicit.evaluate(el => ({ width: el.getBoundingClientRect().width, parent: el.parentElement!.getBoundingClientRect().width }))
  expect(sizes.width / sizes.parent).toBeCloseTo(0.75, 2)
  for (const id of ['skeleton-default', 'skeleton-text', 'skeleton-avatar', 'skeleton-card', 'skeleton-dimensions']) {
    const preview = page.locator(`#components-${id}-panel-preview`)
    await expect(preview.getByRole('status')).toHaveCount(1)
    await expect(preview.getByRole('status')).toHaveAttribute('aria-busy', 'true')
    for (const placeholder of await preview.getByRole('status').locator('[aria-hidden="true"]').all()) {
      await expect(placeholder).toHaveCSS('animation-name', 'pulse')
      expect((await placeholder.boundingBox())!.width).toBeGreaterThan(0)
    }
  }
  await page.emulateMedia({ reducedMotion: 'reduce' })
  await expect(line).toHaveCSS('animation-name', 'none')
  await page.setViewportSize({ width: 320, height: 1000 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  for (const preview of await page.locator('[data-docs-example-preview]').all()) {
    expect(await preview.getByRole('status').evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true)
    for (const placeholder of await preview.getByRole('status').locator('[aria-hidden="true"]').all()) {
      await expect(placeholder).toHaveCSS('animation-name', 'none')
      expect((await placeholder.boundingBox())!.width).toBeGreaterThan(0)
    }
  }
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
})

test('notices share palette variants without changing announcement or interaction behavior @cross-browser', async ({ page }) => {
  await page.goto('/components/notice')
  const basic = page.locator('#components-notice-panel-preview [data-fve-notice]')
  const colors = page.locator('#components-notice-colors-panel-preview [data-fve-notice]')
  const variants = page.locator('#components-notice-variants-panel-preview [data-fve-notice]')
  await expect(colors).toHaveCount(7)
  await expect(variants).toHaveCount(4)
  const staticPreviews = page.locator('#components-notice-panel-preview, #components-notice-colors-panel-preview, #components-notice-variants-panel-preview')
  await expect(staticPreviews.locator('[role="alert"], [role="status"]')).toHaveCount(0)
  const announcement = page.locator('#components-notice-announcement-panel-preview')
  await announcement.getByRole('button', { name: 'Update status', exact: true }).click()
  await expect(announcement.getByRole('status')).toContainText('Ready to continue.')
  for (const dark of [false, true]) {
    await page.evaluate(dark => document.documentElement.classList.toggle('dark', dark), dark)
    await expect(basic).toHaveCSS('box-shadow', 'none')
    expect(await basic.evaluate(el => getComputedStyle(el).backgroundColor)).not.toBe('rgba(0, 0, 0, 0)')
    for (const notice of [...await colors.all(), ...await variants.all()]) {
      await expect.poll(() => contrast(notice)).toBeGreaterThanOrEqual(4.5)
    }
    const [solid, soft, outline, ghost] = await variants.all()
    const background = (el: Locator) => el.evaluate(el => getComputedStyle(el).backgroundColor)
    expect(await background(solid)).not.toBe(await background(soft))
    await expect(soft).toHaveCSS('box-shadow', 'none')
    await expect(outline).toHaveCSS('background-color', 'rgba(0, 0, 0, 0)')
    expect(await outline.evaluate(el => getComputedStyle(el).boxShadow)).toContain('inset')
    await expect(ghost).toHaveCSS('background-color', 'rgba(0, 0, 0, 0)')
    await expect(ghost).toHaveCSS('box-shadow', 'none')
    const before = await background(solid)
    await solid.hover()
    expect(await background(solid)).toBe(before)
    const link = solid.getByRole('link', { name: 'Read guidance', exact: true })
    await expect.poll(() => contrast(link)).toBeGreaterThanOrEqual(4.5)
    await expect(link).toHaveAttribute('href', '/components/notice')
  }
  await page.setViewportSize({ width: 320, height: 1000 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  for (const notice of [...await colors.all(), ...await variants.all()]) {
    expect(await notice.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true)
  }
})

test('action palettes retain contrast, independent overrides and mode-aware custom colors @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  const lead = page.locator('#components-button-basic-panel-preview').getByRole('button', { name: 'Sync', exact: true })
  await lead.click()
  await expect(lead).toBeDisabled()
  await expect(lead).toHaveAttribute('aria-busy', 'true')
  await expect(lead).toBeEnabled()
  const colors = page.locator('#components-button-colors-panel-preview').getByRole('button')
  const variants = page.locator('#components-button-variants-panel-preview').getByRole('button')
  const custom = page.locator('#components-button-custom-panel-preview').getByRole('button')
  await expect(colors).toHaveCount(7)
  for (const dark of [false, true]) {
    await page.evaluate(dark => document.documentElement.classList.toggle('dark', dark), dark)
    for (const controls of [colors, variants, custom]) {
      for (const control of await controls.all()) {
        await page.mouse.move(0, 0)
        await expect.poll(() => contrast(control)).toBeGreaterThanOrEqual(4.5)
        const before = await control.evaluate(el => getComputedStyle(el).backgroundColor)
        await control.hover()
        await expect.poll(() => control.evaluate(el => getComputedStyle(el).backgroundColor)).not.toBe(before)
        await expect.poll(() => contrast(control)).toBeGreaterThanOrEqual(4.5)
        await page.mouse.down()
        await expect.poll(() => contrast(control)).toBeGreaterThanOrEqual(4.5)
        await page.mouse.up()
      }
    }
    const pending = page.locator('#components-button-error-pending-panel-preview').getByRole('button')
    await pending.click()
    await expect(pending).toBeDisabled()
    await expect(pending).toHaveAttribute('aria-busy', 'true')
    const glyph = await pending.locator('.animate-spin').evaluate(el => ({ border: getComputedStyle(el).borderTopColor, text: getComputedStyle(el.parentElement!).color }))
    expect(glyph.border).toBe(glyph.text)
    await expect(pending).toBeEnabled()
    await expect.poll(() => custom.first().evaluate(el => getComputedStyle(el).colorScheme)).toBe(dark ? 'dark' : 'light')
  }

  const primary = colors.filter({ hasText: /^Primary$/ })
  const secondary = colors.filter({ hasText: /^Secondary$/ })
  const background = (control: Locator) => control.evaluate(el => getComputedStyle(el).backgroundColor)
  await page.mouse.move(0, 0)
  const primaryBefore = await background(primary), secondaryBefore = await background(secondary)
  await primary.evaluate(el => el.parentElement!.style.setProperty('--fve-primary-solid', '#123456'))
  await expect.poll(() => background(primary)).toBe('rgb(18, 52, 86)')
  expect(await background(secondary)).toBe(secondaryBefore)
  await secondary.evaluate(el => el.parentElement!.style.setProperty('--fve-secondary-solid', '#654321'))
  await expect.poll(() => background(secondary)).toBe('rgb(101, 67, 33)')
  expect(await background(primary)).not.toBe(primaryBefore)
  expect(await background(primary)).toBe('rgb(18, 52, 86)')

  // A light preview nested in a dark page must not inherit dark Custom values.
  await custom.first().evaluate(el => el.closest('.fve-components')!.setAttribute('data-fve-color-mode', 'light'))
  await expect.poll(() => custom.first().evaluate(el => getComputedStyle(el).colorScheme)).toBe('light')
  await expect.poll(() => background(custom.first())).toBe('rgb(154, 52, 18)')
})

test('button groups keep compact seams, separate independent groups, and operate their joined menu @cross-browser', async ({ page }) => {
  await page.goto('/components/button-group')
  const preview = page.locator('#components-button-group-panel-preview')
  const back = preview.getByRole('group', { name: 'Back' })
  const message = preview.getByRole('group', { name: 'Message actions' })
  const scheduling = preview.getByRole('group', { name: 'Scheduling actions' })
  await expect(back.getByRole('button')).toHaveCount(1)
  await expect(message.getByRole('button')).toHaveText(['Archive', 'Report'])
  await expect(scheduling.getByRole('button')).toHaveCount(2)
  for (const dark of [false, true]) {
    await page.evaluate(dark => document.documentElement.classList.toggle('dark', dark), dark)
    for (const group of [back, message, scheduling]) {
      for (const control of await group.getByRole('button').all()) await expect.poll(() => contrast(control)).toBeGreaterThanOrEqual(4.5)
    }
  }
  await page.evaluate(() => document.documentElement.classList.remove('dark'))

  const boxes = async (group: Locator) => Promise.all((await group.getByRole('button').all()).map(control => control.boundingBox()))
  for (const group of [message, scheduling]) {
    const [first, last] = await group.getByRole('button').all()
    for (const corner of ['border-top-right-radius', 'border-bottom-right-radius']) await expect(first).toHaveCSS(corner, '0px')
    for (const corner of ['border-top-left-radius', 'border-bottom-left-radius']) await expect(last).toHaveCSS(corner, '0px')
    await expect(first).toHaveCSS('border-inline-start-width', '1px')
    await expect(last).toHaveCSS('border-inline-start-width', '0px')
  }
  const [archive, report] = await boxes(message)
  const [snooze, menuTrigger] = await boxes(scheduling)
  expect(report!.x - archive!.x - archive!.width).toBeCloseTo(0, 1)
  expect(menuTrigger!.x - snooze!.x - snooze!.width).toBeCloseTo(0, 1)
  const independentGap = await Promise.all([back.boundingBox(), message.boundingBox()])
  expect(independentGap[1]!.x - (independentGap[0]!.x + independentGap[0]!.width)).toBeGreaterThanOrEqual(7)
  expect(await scheduling.getByRole('button', { name: 'More scheduling actions' }).evaluate(el => getComputedStyle(el).boxShadow)).not.toBe('none')

  const trigger = scheduling.getByRole('button', { name: 'More scheduling actions' })
  await trigger.focus()
  await page.keyboard.press('ArrowDown')
  const menu = preview.getByRole('menu', { name: 'More scheduling actions' })
  await expect(menu).toBeVisible()
  await expect(menu.getByRole('menuitem').first()).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(trigger).toBeFocused()

  const exports = page.locator('#components-button-group-select-panel-preview').getByRole('group', { name: 'Export options' })
  const format = exports.getByRole('combobox', { name: 'Export format' })
  for (const corner of ['border-top-left-radius', 'border-top-right-radius', 'border-bottom-left-radius', 'border-bottom-right-radius']) {
    await expect(format).toHaveCSS(corner, '0px')
  }
  await format.focus()
  await page.keyboard.press('ArrowDown')
  expect(await exports.locator('[data-fve-select-content]').evaluate(el => parseFloat(getComputedStyle(el).borderTopLeftRadius))).toBeGreaterThan(0)
  await exports.getByRole('option', { name: 'JSON', exact: true }).click()
  await expect(exports.locator('input[name="groupExportFormat"]')).toHaveValue('json')
  await expect(format).toBeFocused()
  await expect(exports.getByRole('button', { name: 'Export', exact: true })).toBeEnabled()

  const vertical = page.locator('#components-button-group-vertical-panel-preview').getByRole('group', { name: 'Record view' })
  const [summary, activity, files] = await boxes(vertical)
  expect(activity!.y - summary!.y - summary!.height).toBeCloseTo(0, 1)
  expect(files!.y - activity!.y - activity!.height).toBeCloseTo(0, 1)
  for (const control of await vertical.getByRole('button').all()) {
    await expect(control).toHaveCSS('border-inline-start-width', '1px')
  }
  for (const control of (await vertical.getByRole('button').all()).slice(1)) {
    await expect(control).toHaveCSS('border-block-start-width', '0px')
    await expect(control).toHaveCSS('border-top-left-radius', '0px')
    await expect(control).toHaveCSS('border-top-right-radius', '0px')
  }

  const custom = page.locator('#components-button-group-custom-panel-preview').getByRole('group', { name: 'Custom control actions' })
  const native = custom.getByRole('button', { name: 'Native action', exact: true })
  const details = custom.getByRole('button', { name: 'Details', exact: true })
  await expect(native).toHaveCSS('border-top-right-radius', '0px')
  await expect(details).toHaveCSS('border-top-left-radius', '0px')
  await expect(details).toHaveCSS('border-inline-start-width', '0px')
  await details.click()
  const independent = custom.getByRole('button', { name: 'Independent action', exact: true })
  await expect(independent).toBeVisible()
  for (const corner of ['border-top-left-radius', 'border-top-right-radius', 'border-bottom-left-radius', 'border-bottom-right-radius']) {
    expect(parseFloat(await independent.evaluate((el, corner) => getComputedStyle(el).getPropertyValue(corner), corner))).toBeGreaterThan(0)
  }
  await expect(independent).toHaveCSS('border-inline-start-width', '1px')
  await expect(independent).toHaveCSS('border-block-start-width', '1px')
  await independent.focus()
  await page.keyboard.press('Escape')
  await expect(details).toBeFocused()
  await scheduling.evaluate(el => { el.setAttribute('dir', 'rtl') })
  await expect(trigger).toHaveCSS('border-top-right-radius', '0px')
  expect(parseFloat(await trigger.evaluate(el => getComputedStyle(el).borderTopLeftRadius))).toBeGreaterThan(0)
  await scheduling.evaluate(el => { el.removeAttribute('dir') })

  await page.setViewportSize({ width: 320, height: 1000 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  // Segments must retain their intrinsic widths rather than compress labels to avoid scrolling.
  expect(await message.evaluate(el => el.scrollWidth - el.clientWidth)).toBeGreaterThan(0)
  const frame = (await preview.boundingBox())!
  const region = (await message.boundingBox())!
  expect(region.x).toBeGreaterThanOrEqual(frame.x)
  expect(region.x + region.width).toBeLessThanOrEqual(frame.x + frame.width)
  for (const control of await message.getByRole('button').all()) {
    await control.focus()
    await expect.poll(async () => {
      const box = (await control.boundingBox())!
      return box.x >= region.x && box.x + box.width <= region.x + region.width + 1
    }).toBe(true)
  }
  await message.evaluate(el => { el.scrollLeft = 0 })
  await message.focus()
  await expect(message).toBeFocused()
  await page.keyboard.press('ArrowRight')
  await expect.poll(() => message.evaluate(el => el.scrollLeft)).toBeGreaterThan(0)
})

test.describe('native button group Select fallback', () => {
  test.use({ javaScriptEnabled: false })
  test('keeps the middle segment square without JavaScript @cross-browser', async ({ page }) => {
    await page.goto('/components/button-group')
    const group = page.getByRole('group', { name: 'Export options', exact: true })
    const select = group.getByRole('combobox', { name: 'Export format', exact: true })
    await expect(select).toBeVisible()
    for (const corner of ['border-top-left-radius', 'border-top-right-radius', 'border-bottom-left-radius', 'border-bottom-right-radius']) {
      await expect(select).toHaveCSS(corner, '0px')
    }
    await select.selectOption('json')
    await expect(select).toHaveValue('json')
  })
})

test('Link variants retain native keyboard and new-tab navigation @cross-browser', async ({ page }) => {
  await page.goto('/components/link')
  const plain = page.locator('#components-link-basic-panel-preview').getByRole('link', { name: 'Read documentation', exact: true })
  await expect(plain).toHaveAttribute('href', '/components')
  await expect(plain).toHaveCSS('text-decoration-line', 'underline')
  const variants = page.locator('#components-link-variants-panel-preview')
  await expect(variants.getByRole('link')).toHaveCount(5)
  await expect(variants.getByRole('button')).toHaveCount(0)
  await expect(variants.getByRole('link', { name: 'Solid', exact: true })).toHaveJSProperty('tagName', 'A')

  const newTab = page.locator('#components-link-new-tab-panel-preview').getByRole('link', { name: 'Open examples in a new tab', exact: true })
  const opened = page.waitForEvent('popup')
  await newTab.click()
  const popup = await opened
  await expect(popup).toHaveURL(/\/examples$/)
  await popup.close()
  await expect(page).toHaveURL(/\/components\/link$/)

  await plain.focus()
  await expect(plain).toBeFocused()
  await plain.press('Enter')
  await expect(page).toHaveURL(/\/components$/)
  await expect(page.getByRole('heading', { level: 1, name: 'Components', exact: true })).toBeVisible()
})

test('pending buttons center an overlay without changing size or accessible names @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  for (const fontSize of ['100%', '200%']) {
    await page.evaluate(value => { document.documentElement.style.fontSize = value }, fontSize)
    for (const [id, name] of [['button-basic', 'Sync'], ['button-icon-pending', 'Refresh']]) {
      const control = page.locator(`#components-${id}-panel-preview`).getByRole('button', { name, exact: true })
      const before = (await control.boundingBox())!
      let release!: () => void
      const held = new Promise<void>(resolve => { release = resolve })
      // Exercise the real handler, but keep its response pending until geometry is
      // measured: CI round trips can otherwise outlast the one-second example.
      await page.route('**/components/button/settled*', async route => {
        const response = await route.fetch()
        await held
        await route.fulfill({ response })
      }, { times: 1 })
      try {
        await control.click()
        await expect(control).toBeDisabled()
        await expect(control).toHaveAttribute('aria-busy', 'true')
        await expect(control).toHaveAccessibleName(name)
        await expect(control.locator('.animate-spin')).toBeVisible()
        const { during, glyph } = await control.evaluate(element => {
          const box = element.getBoundingClientRect(), spinner = element.querySelector('.animate-spin')!.getBoundingClientRect()
          return { during: { x: box.x, y: box.y, width: box.width, height: box.height }, glyph: { x: spinner.x, y: spinner.y, width: spinner.width, height: spinner.height } }
        })
        expect(during.width).toBeCloseTo(before.width, 1)
        expect(during.height).toBeCloseTo(before.height, 1)
        expect(Math.abs(glyph.x + glyph.width / 2 - during.x - during.width / 2)).toBeLessThan(1)
        expect(Math.abs(glyph.y + glyph.height / 2 - during.y - during.height / 2)).toBeLessThan(1)
      } finally { release() }
      await expect(control).toBeEnabled()
      const after = (await control.boundingBox())!
      expect(after.width).toBeCloseTo(before.width, 1)
      expect(after.height).toBeCloseTo(before.height, 1)
    }
  }
})

test('Button composes ordinary text and icon children while CopyReveal can use icon actions @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  const content = page.locator('#components-button-content-panel-preview')
  await expect(content.getByRole('button', { name: 'Text', exact: true })).toBeVisible()
  const iconOnly = content.getByRole('button', { name: 'Refresh', exact: true })
  await expect(iconOnly).toBeVisible()
  await expect(iconOnly.locator('svg')).toHaveCount(1)
  const iconBox = await iconOnly.boundingBox()
  expect(iconBox).toBeTruthy()
  expect(iconBox!.width).toBeGreaterThanOrEqual(24)
  expect(iconBox!.height).toBeGreaterThanOrEqual(24)
  const leading = content.getByRole('button', { name: 'New branch', exact: true }).locator('svg')
  const trailing = content.getByRole('button', { name: 'Continue', exact: true }).locator('svg')
  await expect(leading).toHaveCount(1)
  await expect(trailing).toHaveCount(1)
  expect((await iconOnly.locator('svg').boundingBox())!.width).toBe(16)
  expect((await leading.boundingBox())!.width).toBe(16)
  expect((await trailing.boundingBox())!.width).toBe(16)
  await expect(content.getByRole('button', { name: 'Custom content', exact: true })).toBeVisible()

  await page.goto('/components/copy-reveal')
  const reveal = page.locator('#components-copy-reveal-icons-panel-preview')
  const secret = reveal.getByLabel('Icon action demo token')
  await expect(secret).toHaveAttribute('type', 'password')
  await reveal.getByRole('button', { name: 'Reveal', exact: true }).click()
  await expect(secret).toHaveAttribute('type', 'text')
  await expect(reveal.getByRole('button', { name: 'Hide', exact: true })).toBeVisible()
  await expect(reveal.getByRole('button', { name: 'Copy', exact: true })).toBeVisible()
})

test('Rocket Button appearance bindings preserve native behavior and consumer styles @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  const button = page.locator('#components-button-basic-panel-preview').getByRole('button', { name: 'Sync', exact: true })
  await expect(button).toBeEnabled()
  await button.evaluate(el => {
    const host = el.parentElement!
    host.style.margin = '1px'
    host.setAttribute('data-attr:color', "'error'")
    host.setAttribute('data-attr:variant', "'soft'")
    host.setAttribute('data-attr:size', "'large'")
  })
  await expect.poll(() => button.evaluate(el => el.getBoundingClientRect().height)).toBe(36)
  await expect.poll(() => button.evaluate(el => getComputedStyle(el).backgroundColor)).not.toBe('rgba(0, 0, 0, 0)')
  expect(await button.evaluate(el => el.style.margin)).toBe('1px')
  await button.evaluate(el => el.parentElement!.focus())
  await expect(button).toBeFocused()
  const settled = page.waitForResponse(response => response.url().includes('/components/button/settled'))
  await button.evaluate(el => el.parentElement!.click())
  await expect(button).toBeDisabled()
  await expect(button.locator('.animate-spin')).toBeVisible()
  await expect(button.locator('.animate-spin')).toHaveCSS('width', '20px')
  await settled
  await expect(button).toBeEnabled()
  await expect(button).toHaveAccessibleName('Sync')
})

test('button press feedback respects keyboard, reduced motion, unavailable controls and popup anchors @cross-browser', async ({ page }) => {
  await page.goto('/components/button-group')
  await page.getByRole('link', { name: 'Button', exact: true }).first().click()
  await expect(page).toHaveURL('/components/button')
  const button = page.locator('#components-button-panel-preview').getByRole('button').first()
  await button.focus()
  const before = await button.boundingBox()
  await page.keyboard.down('Space')
  await expect.poll(() => translation(button)).toBe('0px 1px')
  const pressed = await button.boundingBox()
  expect(pressed!.y - before!.y).toBeCloseTo(1)
  expect(pressed!.height).toBe(before!.height)
  await page.keyboard.up('Space')
  await expect.poll(() => translation(button)).toBe('none')
  await page.keyboard.down('Enter')
  await expect.poll(() => translation(button)).toBe('0px 1px')
  await page.keyboard.up('Enter')
  await expect.poll(() => translation(button)).toBe('none')
  await page.keyboard.down('Space')
  await expect.poll(() => translation(button)).toBe('0px 1px')
  await page.keyboard.press('Tab')
  await expect.poll(() => translation(button)).toBe('none')
  await page.keyboard.up('Space')
  await button.focus()
  await page.emulateMedia({ reducedMotion: 'reduce' })
  await page.keyboard.down('Space')
  expect(await translation(button)).toBe('none')
  await page.keyboard.up('Space')
  await page.emulateMedia({ reducedMotion: 'no-preference' })
  for (const id of ['button-disabled', 'button-pending']) {
    const unavailable = page.locator(`#components-${id}-panel-preview`).getByRole('button')
    if (id === 'button-pending') await unavailable.click()
    await expect(unavailable).toBeDisabled()
    expect(await translation(unavailable)).toBe('none')
    if (id === 'button-pending') await expect(unavailable).toBeEnabled()
  }

  await button.hover()
  await page.mouse.down()
  await expect.poll(() => translation(button)).toBe('0px 1px')
  await page.mouse.up()
  await expect.poll(() => translation(button)).toBe('none')

  const icon = page.locator('#components-button-icon-primary-panel-preview').getByRole('button', { name: 'Create', exact: true })
  await icon.hover()
  await page.mouse.down()
  await expect.poll(() => translation(icon)).toBe('0px 1px')
  await page.mouse.up()

  await page.goto('/components/dropdown-menu')
  const popup = page.locator('#components-dropdown-menu-panel-preview').getByRole('button', { name: 'Actions', exact: true })
  await popup.hover()
  await page.mouse.down()
  expect(await translation(popup)).toBe('none')
  await page.mouse.up()
})
