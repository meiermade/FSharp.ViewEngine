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

test('breadcrumb middle overflow preserves context, keyboard access and narrow ancestors @cross-browser', async ({ page }) => {
  await page.goto('/components/breadcrumbs')
  const trail = page.locator('#middle-breadcrumbs')
  await expect(trail.locator(':scope > ol > li > a')).toHaveText(['Home', 'Navigation'])
  await expect(trail.locator('[aria-current="page"]')).toHaveText('Breadcrumbs')
  const trigger = trail.getByRole('button', { name: 'Show hidden breadcrumbs' })
  await trigger.focus()
  await page.keyboard.press('ArrowDown')
  const menu = trail.getByRole('menu', { name: 'Show hidden breadcrumbs' })
  await expect(menu.getByRole('menuitem')).toHaveCount(2)
  await expect(menu.getByRole('menuitem').first()).toBeFocused()
  await page.keyboard.press('ArrowDown')
  await expect(menu.getByRole('menuitem').last()).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(trigger).toBeFocused()
  await expect(menu).toBeHidden()
  await trigger.click()
  await menu.getByRole('menuitem').first().click()
  await expect(page).toHaveURL(/\/components\/breadcrumbs$/)
  await expect(menu).toBeHidden()

  const configured = page.locator('#visible-breadcrumbs')
  await expect(configured.locator(':scope > ol > li > a')).toHaveText(['Home', 'Components', 'Navigation'])
  await page.setViewportSize({ width: 390, height: 844 })
  await trigger.click()
  await expect(menu.getByRole('menuitem')).toHaveCount(4)
  await page.keyboard.press('Escape')
  await expect(trigger).toBeFocused()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
})

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
  for (const [id, height] of [['components-compact-rows', 40], ['toggle-soft', 32], ['toggle-outline', 40], ['toggle-ghost', 48]] as const) {
    await expect.poll(async () => (await page.locator(`#${id}`).boundingBox())!.height).toBe(height)
  }
  await page.goto('/components/toggle-group')
  const group = page.locator('#components-alignment-default')
  await expect(group).not.toHaveAttribute('aria-orientation')
  for (const button of await group.getByRole('button').all()) expect((await button.boundingBox())!.height).toBe(40)
  for (const button of await page.locator('#components-formatting-group').getByRole('button').all()) expect((await button.boundingBox())!.height).toBe(32)
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
  const label = (await basic.getByText('Available balance', { exact: true }).boundingBox())!
  expect(Math.abs(label.x + label.width / 2 - frame.x - frame.width / 2)).toBeLessThanOrEqual(2)

  const example = page.locator('#components-metric-multiple')
  const group = example.getByRole('group', { name: 'Monthly financial metrics', exact: true })
  const labels = ['Revenue', 'Expenses', 'Net income'].map(name => group.getByText(name, { exact: true }))
  await expect(group.getByText('September 2026', { exact: true })).toHaveCount(3)
  await expect(group.getByText('$48,000', { exact: true })).toBeVisible()
  await expect(group.getByText('$31,200', { exact: true })).toBeVisible()
  await expect(group.getByText('$16,800', { exact: true })).toBeVisible()
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
  for (const id of ['skeleton-default', 'skeleton-text', 'skeleton-avatar', 'skeleton-card']) {
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
  await expect(page.locator('[data-fve-notice] [role="alert"], [data-fve-notice] [role="status"]')).toHaveCount(0)
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
    await expect(pending).toBeDisabled()
    await expect(pending).toHaveAttribute('aria-busy', 'true')
    const glyph = await pending.locator('[aria-hidden="true"]').evaluate(el => ({ border: getComputedStyle(el).borderTopColor, text: getComputedStyle(el.parentElement!).color }))
    expect(glyph.border).toBe(glyph.text)
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
  const [archive, report] = await boxes(message)
  const [snooze, menuTrigger] = await boxes(scheduling)
  expect(Math.abs(archive!.x + archive!.width - report!.x)).toBeLessThanOrEqual(1)
  expect(Math.abs(snooze!.x + snooze!.width - menuTrigger!.x)).toBeLessThanOrEqual(1)
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

  const vertical = page.locator('#components-button-group-vertical-panel-preview').getByRole('group', { name: 'Record view' })
  const [summary, activity, files] = await boxes(vertical)
  expect(Math.abs(summary!.y + summary!.height - activity!.y)).toBeLessThanOrEqual(1)
  expect(Math.abs(activity!.y + activity!.height - files!.y)).toBeLessThanOrEqual(1)

  await page.setViewportSize({ width: 320, height: 1000 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
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

test('Button renders each content mode while CopyReveal can use icon actions @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  const content = page.locator('#components-button-content-panel-preview')
  await expect(content.getByRole('button', { name: 'Text', exact: true })).toBeVisible()
  const iconOnly = content.getByRole('button', { name: 'Refresh', exact: true })
  await expect(iconOnly).toBeVisible()
  await expect(iconOnly.locator('svg')).toHaveCount(1)
  const iconBox = await iconOnly.boundingBox()
  expect(iconBox).toBeTruthy()
  expect(iconBox!.width).toBeCloseTo(iconBox!.height)
  await expect(content.getByRole('button', { name: 'New branch', exact: true }).locator('svg')).toHaveCount(1)
  await expect(content.getByRole('button', { name: 'Continue', exact: true }).locator('svg')).toHaveCount(1)
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
    await expect(unavailable).toBeDisabled()
    expect(await translation(unavailable)).toBe('none')
  }

  await button.hover()
  await page.mouse.down()
  await expect.poll(() => translation(button)).toBe('0px 1px')
  await page.mouse.up()
  await expect.poll(() => translation(button)).toBe('none')

  const icon = page.locator('#components-button-icon-primary-panel-preview').getByRole('button', { name: 'Add account', exact: true })
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
