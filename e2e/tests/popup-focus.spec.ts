import { expect, test, type Locator } from '@playwright/test'

async function paint(locator: Locator) {
  return locator.evaluate(element => {
    const style = getComputedStyle(element)
    const canvas = document.createElement('canvas')
    canvas.width = canvas.height = 1
    const context = canvas.getContext('2d')!
    const rgba = (color: string) => {
      context.clearRect(0, 0, 1, 1)
      context.fillStyle = color
      context.fillRect(0, 0, 1, 1)
      return Array.from(context.getImageData(0, 0, 1, 1).data)
    }
    const background = rgba(style.backgroundColor)
    let effectiveBackground = background
    let ancestor = element.parentElement
    while (effectiveBackground[3] === 0 && ancestor) {
      effectiveBackground = rgba(getComputedStyle(ancestor).backgroundColor)
      ancestor = ancestor.parentElement
    }
    return { background, effectiveBackground, foreground: rgba(style.color), outline: style.outlineStyle, shadow: style.boxShadow, border: style.borderTopWidth }
  })
}

for (const fixture of [
  { id: 'components-select-multiple', label: 'Team members', searchable: false },
  { id: 'components-select-search-multiple', label: 'Search members', searchable: true },
  { id: 'components-select-search-multiple-remote', label: 'Remote members', searchable: true },
]) {
  test(`${fixture.label} shares pointer and keyboard active state and clears on leave`, { tag: '@cross-browser' }, async ({ page }) => {
    const errors: string[] = []
    page.on('pageerror', error => errors.push(error.message))
    await page.goto('/components/select?fixtures=1')
    const example = page.locator(`#${fixture.id}`)
    await example.getByRole('button', { name: fixture.label, exact: true }).click()
    const active = example.locator('[role="option"][data-active="true"]')
    const riley = example.getByRole('option', { name: 'Riley Chen', exact: true })
    const jamie = example.getByRole('option', { name: 'Jamie Lee', exact: true })
    await riley.click()
    await expect(riley).toHaveAttribute('aria-selected', 'true')
    await page.mouse.move(0, 0)
    await expect(active).toHaveCount(0)
    const selectedPaint = await paint(riley)
    await jamie.hover()
    await expect(active).toHaveCount(1)
    await expect(jamie).toHaveAttribute('data-active', 'true')
    expect((await paint(riley)).background).toEqual(selectedPaint.background)
    await page.keyboard.press('ArrowDown')
    await expect(riley).toHaveAttribute('data-active', 'true')
    await expect(active).toHaveCount(1)
    await expect(example.getByRole('listbox')).toHaveCSS('outline-style', 'none')
    expect((await paint(jamie)).background).not.toEqual((await paint(riley)).background)
    // A stationary pointer over Jamie must not compete with keyboard focus on Riley.
    await page.keyboard.press('ArrowDown')
    await expect(example.getByRole('option', { name: 'Taylor Brooks', exact: true })).toHaveAttribute('data-active', 'true')
    await riley.hover()
    await expect(riley).toHaveAttribute('data-active', 'true')
    await page.mouse.move(0, 0)
    await expect(active).toHaveCount(0)
    if (fixture.searchable) {
      const search = example.getByRole('searchbox')
      await expect(search).toBeFocused()
      await search.fill('Jamie')
      await expect(jamie).toBeVisible()
      await jamie.hover()
      await expect(jamie).toHaveAttribute('data-active', 'true')
      await page.mouse.move(0, 0)
      await expect(active).toHaveCount(0)
      await expect(search).toBeFocused()
    }
    await page.keyboard.press('Escape')
    await example.getByRole('button', { name: fixture.label, exact: true }).click()
    await expect(active).toHaveCount(0)
    expect(errors).toEqual([])
  })
}

test('focused Dropdown menu recipes keep menu semantics and working local state @cross-browser', async ({ page }) => {
  await page.goto('/components/dropdown-menu')
  const choices = page.locator('#components-dropdown-menu-checkboxes')
  await choices.getByRole('button', { name: 'Notification preferences', exact: true }).click()
  const email = page.getByRole('menuitemcheckbox', { name: 'Email alerts', exact: true })
  const push = page.getByRole('menuitemcheckbox', { name: 'Push alerts', exact: true })
  await expect(email).toBeChecked()
  await push.click()
  await expect(push).toBeChecked()
  await expect(choices.getByRole('status')).toHaveText('Email: on · Push: on')
  await email.click()
  await expect(email).not.toBeChecked()
  await page.keyboard.press('Escape')
  const radio = page.locator('#components-dropdown-menu-radio')
  await radio.getByRole('button', { name: 'Sort preview', exact: true }).click()
  await page.getByRole('menuitemradio', { name: 'Status', exact: true }).click()
  await expect(radio.getByRole('status')).toHaveText('Sort by: Status')
  await radio.getByRole('button', { name: 'Sort preview', exact: true }).click()
  await expect(page.getByRole('menuitemradio', { checked: true })).toHaveText('Status')
  await page.keyboard.press('Escape')
  await page.locator('#components-dropdown-menu-disabled').getByRole('button', { name: 'Available actions', exact: true }).click()
  await expect(page.getByRole('menuitem', { name: 'Export report Choose a saved report first.', exact: true })).toBeDisabled()
  await page.keyboard.press('Escape')
  const draft = page.locator('#components-dropdown-menu-destructive')
  const note = draft.getByRole('textbox', { name: 'Draft note', exact: true })
  await draft.getByRole('button', { name: 'Draft actions', exact: true }).click()
  await page.getByRole('menuitem', { name: 'Clear draft', exact: true }).click()
  await expect(note).toHaveValue('')
  await draft.getByRole('button', { name: 'Draft actions', exact: true }).click()
  await page.getByRole('menuitem', { name: 'Restore sample', exact: true }).click()
  await expect(note).toHaveValue('Review September reports.')
  await page.locator('#components-dropdown-menu-shortcuts').getByRole('button', { name: 'Find documentation', exact: true }).press('ArrowDown')
  await page.keyboard.press('p')
  await expect(page.getByRole('menuitem', { name: 'Popover', exact: true })).toBeFocused()
  await page.keyboard.press('Escape')
})

test('the advanced native Popover trigger works without JavaScript', async ({ browser, baseURL }) => {
  const context = await browser.newContext({ baseURL, javaScriptEnabled: false })
  try {
    const page = await context.newPage()
    await page.goto('/components/popover')
    await expect(page.locator('#profile-popover-content')).toBeHidden()
    const trigger = page.getByRole('button', { name: 'Workspace information', exact: true })
    await expect(page.locator('#native-workspace-popover-content')).toBeHidden()
    await trigger.click()
    const panel = page.getByRole('dialog', { name: 'Workspace information', exact: true })
    await expect(panel).toContainText('You have owner access in this workspace.')
    await page.keyboard.press('Escape')
    await expect(panel).toBeHidden()
  } finally { await context.close() }
})

test('dropdown uses one focus target across pointer, keyboard and pointer leave', { tag: '@cross-browser' }, async ({ page }) => {
  await page.goto('/components/dropdown-menu')
  const example = page.locator('#components-dropdown-menu-panel-preview')
  const trigger = example.getByRole('button', { name: 'Actions', exact: true })
  await trigger.click()
  const menu = example.getByRole('menu', { name: 'Actions', exact: true })
  const active = menu.locator('[role="menuitem"]:focus')
  const items = menu.locator('[role="menuitem"]:not([aria-disabled="true"])')
  await expect(menu).toBeFocused()
  await expect(active).toHaveCount(0)
  await items.nth(0).hover()
  await expect(items.nth(0)).toBeFocused()
  const highlight = await paint(items.nth(0))
  await page.keyboard.press('ArrowDown')
  await expect(items.nth(1)).toBeFocused()
  expect((await paint(items.nth(0))).background).not.toEqual(highlight.background)
  await page.mouse.move(0, 0)
  await items.nth(0).hover()
  await expect(items.nth(0)).toBeFocused()
  await page.mouse.move(0, 0)
  await expect(menu).toBeFocused()
  await expect(active).toHaveCount(0)
  await page.keyboard.press('ArrowDown')
  await expect(items.nth(0)).toBeFocused()
  await page.keyboard.press('Escape')
  await trigger.press('Enter')
  await expect(items.nth(0)).toBeFocused()
})

test('composed dropdown triggers use the supplied native or Rocket control and preserve caller cancellation', { tag: '@cross-browser' }, async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/dropdown-menu')
  const rocket = page.getByRole('button', { name: 'More page actions', exact: true })
  await expect(rocket).toHaveAttribute('id', 'components-menu-outline-trigger')
  await expect(rocket.locator('button')).toHaveCount(0)
  await rocket.press('ArrowDown')
  const rocketMenu = page.getByRole('menu', { name: 'More page actions', exact: true })
  await expect(rocketMenu.getByRole('menuitem').first()).toBeFocused()
  await expect(rocket).toHaveAttribute('aria-expanded', 'true')
  const triggerBox = await rocket.boundingBox()
  const menuBox = await rocketMenu.boundingBox()
  expect(triggerBox).not.toBeNull()
  expect(menuBox).not.toBeNull()
  expect(Math.abs(menuBox!.y - triggerBox!.y)).toBeLessThan(200)
  await page.keyboard.press('Escape')
  await expect(rocket).toBeFocused()
  await expect(rocket).toHaveAttribute('aria-expanded', 'false')

  const example = page.locator('#components-dropdown-menu-custom')
  const native = example.getByRole('button', { name: 'Personal workspace', exact: true })
  await expect(native).toHaveAttribute('id', 'workspace-menu-control')
  await expect(example.locator('button button, fve-button')).toHaveCount(0)
  await native.click()
  const nativeMenu = example.getByRole('menu', { name: 'Personal workspace', exact: true })
  await expect(nativeMenu).toBeVisible()
  await expect(example.getByText('Trigger activations: 1', { exact: true })).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(native).toBeFocused()
  await native.press('ArrowDown')
  await expect(nativeMenu.getByRole('menuitem', { name: 'Side navigation', exact: true })).toBeFocused()
  await page.keyboard.press('Escape')
  await native.evaluate(element => element.addEventListener('click', event => event.preventDefault(), { once: true, capture: true }))
  await native.click()
  await expect(nativeMenu).not.toBeVisible()
  await expect(example.getByText('Trigger activations: 2', { exact: true })).toBeVisible()
  expect(errors).toEqual([])
})

test('a supplied native dropdown trigger retains popover navigation without JavaScript', { tag: '@cross-browser' }, async ({ browser, baseURL }) => {
  const context = await browser.newContext({ javaScriptEnabled: false })
  try {
    const page = await context.newPage()
    await page.goto(new URL('/components/dropdown-menu', baseURL).href)
    const example = page.locator('#components-dropdown-menu-custom')
    await example.getByRole('button', { name: 'Personal workspace', exact: true }).click()
    const menu = example.getByRole('menu', { name: 'Personal workspace', exact: true })
    await expect(menu).toBeVisible()
    await menu.getByRole('menuitem', { name: 'Side navigation', exact: true }).click()
    await expect(page).toHaveURL(/\/components\/side-nav$/)
  } finally {
    await context.close()
  }
})

function contrast(a: number[], b: number[]) {
  const luminance = (rgb: number[]) => rgb.slice(0, 3).map(value => {
    const channel = value / 255
    return channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4
  }).reduce((sum, value, index) => sum + value * [0.2126, 0.7152, 0.0722][index], 0)
  const first = luminance(a), second = luminance(b)
  return (Math.max(first, second) + 0.05) / (Math.min(first, second) + 0.05)
}

test('Select popups remain visually raised above editor drawers in both themes', { tag: '@cross-browser' }, async ({ page }) => {
  for (const dark of [false, true]) {
    await page.goto('/examples/application/accounts/new')
    const drawer = page.getByRole('dialog', { name: 'Create account', exact: true })
    await expect(drawer.getByRole('textbox', { name: /^Name/ })).toBeFocused()
    await page.evaluate(dark => document.documentElement.classList.toggle('dark', dark), dark)
    const trigger = drawer.getByRole('combobox', { name: 'Parent account', exact: true })
    await trigger.click()
    const popup = drawer.locator('[popover]:popover-open')
    await expect(popup).toBeVisible()
    const panelPaint = await paint(drawer)
    const popupPaint = await paint(popup)
    expect(panelPaint.background).not.toEqual(popupPaint.background)
    expect(panelPaint.background).toEqual((await paint(page.locator('[data-template-shell]'))).background)
    expect(popupPaint.background).toEqual((await paint(drawer.getByRole('textbox', { name: /^Name/ }))).background)
    expect(popupPaint.background[3]).toBe(255)
    expect(popupPaint.border).toBe('1px')
    expect(popupPaint.shadow).not.toBe('none')
    const active = popup.getByRole('option', { name: 'Assets', exact: true })
    const activePaint = await paint(active)
    expect(contrast(activePaint.background, activePaint.foreground)).toBeGreaterThanOrEqual(4.5)
    const otherPaint = await paint(popup.getByRole('option', { name: 'Liabilities', exact: true }))
    expect(contrast(popupPaint.background, otherPaint.foreground)).toBeGreaterThanOrEqual(4.5)
    await page.keyboard.press('Escape')
    await expect(popup).toBeHidden()
    await expect(trigger).toBeFocused()
    await expect(drawer).toBeVisible()
  }
})

test('popup focus retains system-color boundaries and active-target outlines in forced colors', { tag: '@cross-browser' }, async ({ page }) => {
  await page.emulateMedia({ forcedColors: 'active' })
  expect(await page.evaluate(() => matchMedia('(forced-colors: active)').matches)).toBe(true)
  for (const kind of ['select', 'searchable-select', 'dropdown-menu']) {
    await page.goto(kind === 'dropdown-menu' ? '/components/dropdown-menu' : '/components/select?fixtures=1')
    const example = page.locator(kind === 'dropdown-menu' ? '#components-dropdown-menu-panel-preview' : kind === 'searchable-select' ? '#components-select-search-multiple' : '#components-select-multiple')
    const control = kind === 'dropdown-menu' ? example.getByRole('button', { name: 'Actions', exact: true }) : example.getByRole('button', { name: kind === 'select' ? 'Team members' : 'Search members', exact: true })
    if (kind !== 'dropdown-menu') {
      await page.keyboard.press('Tab')
      await control.focus()
      await expect(control).toHaveCSS('outline-style', 'solid')
      await expect(control).toHaveCSS('outline-width', '2px')
    }
    await control.click()
    await page.keyboard.press('ArrowDown')
    const popup = kind === 'dropdown-menu' ? example.getByRole('menu', { name: 'Actions', exact: true }) : example.locator('[popover="auto"]')
    const active = kind === 'dropdown-menu' ? popup.locator('[role="menuitem"]:focus') : popup.locator('[role="option"][data-active="true"]')
    await expect(popup).toHaveCSS('border-top-style', 'solid')
    await expect(popup).toHaveCSS('border-top-width', '1px')
    await expect(active).toHaveCSS('outline-style', 'solid')
    await expect(active).toHaveCSS('outline-width', '2px')
    await expect(active).not.toHaveCSS('outline-color', 'rgba(0, 0, 0, 0)')
    await page.keyboard.press('Escape')
    await expect(control).toBeFocused()
    if (kind === 'dropdown-menu') {
      await expect(control).toHaveCSS('outline-style', 'solid')
      await expect(control).toHaveCSS('outline-width', '2px')
    }
  }
})

for (const dark of [false, true]) {
  for (const viewport of [{ width: 1440, height: 1000, scale: 1 }, { width: 320, height: 900, scale: 2 }]) {
    test(`popup focus uses distinct fills ${dark ? 'dark' : 'light'} ${viewport.width}px ${viewport.scale}x`, async ({ page }, testInfo) => {
      const errors: string[] = []
      page.on('pageerror', error => errors.push(error.message))
      await page.setViewportSize(viewport)
      for (const kind of ['select', 'searchable-select', 'dropdown-menu']) {
        await page.goto(kind === 'dropdown-menu' ? '/components/dropdown-menu' : '/components/select?fixtures=1')
        await page.evaluate(({ dark, scale }) => {
          document.documentElement.classList.toggle('dark', dark)
          document.documentElement.style.fontSize = `${scale * 100}%`
        }, { dark, scale: viewport.scale })
        const example = page.locator(kind === 'dropdown-menu' ? '#components-dropdown-menu-panel-preview' : kind === 'searchable-select' ? '#components-select-search-multiple' : '#components-select-multiple')
        const control = kind === 'dropdown-menu' ? example.getByRole('button', { name: 'Actions', exact: true }) : example.getByRole('button', { name: kind === 'select' ? 'Team members' : 'Search members', exact: true })
        if (kind !== 'dropdown-menu') {
          await page.keyboard.press('Tab')
          await control.focus()
          await expect(control).toHaveCSS('outline-style', 'solid')
          await expect(control).toHaveCSS('outline-width', '2px')
          await expect(control).toHaveCSS('outline-offset', '-2px')
        }
        await control.click()
        await page.mouse.move(0, 0)
        const popup = kind === 'dropdown-menu' ? example.getByRole('menu', { name: 'Actions', exact: true }) : example.locator('[popover="auto"]')
        await expect(popup).toBeVisible()
        const active = kind === 'dropdown-menu' ? popup.locator('[role="menuitem"]:focus') : popup.locator('[role="option"][data-active="true"]')
        await expect(active).toHaveCount(0)
        await page.keyboard.press('ArrowDown')
        await expect(active).toHaveCount(1)
        await expect(popup).toHaveCSS('outline-style', 'none')
        if (kind !== 'dropdown-menu') await expect(popup.getByRole('listbox')).toHaveCSS('outline-style', 'none')
        await expect(popup).toHaveCSS('border-top-width', '1px')
        // Only the elevation shadow remains; no ring spread around the panel.
        expect((await paint(popup)).shadow).not.toMatch(/0px 0px 0px [1-9]/)
        await expect(active).toHaveCSS('box-shadow', 'none')
        await expect(active).toHaveCSS('outline-style', 'none')
        await expect.poll(async () => {
          const colors = await paint(active)
          return contrast(colors.background, colors.foreground)
        }).toBeGreaterThanOrEqual(4.5)
        const panelColors = await paint(popup)
        expect(contrast((await paint(active)).background, panelColors.background)).toBeGreaterThanOrEqual(3)

        if (kind === 'select') {
          const alex = popup.getByRole('option', { name: 'Alex Morgan', exact: true })
          await page.keyboard.press('Home')
          await page.keyboard.press('Space')
          await expect(alex).toHaveAttribute('aria-selected', 'true')
          await expect(alex).toHaveAttribute('data-active', 'true')
          const selectedActive = await paint(alex)
          expect(contrast(selectedActive.background, selectedActive.foreground)).toBeGreaterThanOrEqual(4.5)
          await page.keyboard.press('ArrowDown')
          const selected = await paint(alex)
          expect(selected.background).not.toEqual(selectedActive.background)
          const focused = await paint(active)
          expect(focused.background).not.toEqual(selected.background)
          await expect(alex).toContainText('✓')
          await expect(alex).toHaveCSS('font-weight', '600')
          await expect(alex).toHaveCSS('box-shadow', 'none')
        }
        if (kind !== 'dropdown-menu') {
          // Compare unselected rows: persistent selection has its own independent checked paint.
          if (kind === 'searchable-select') await page.keyboard.press('End')
          // Pointer and keyboard update the same active row; leaving clears it.
          const riley = popup.getByRole('option', { name: 'Riley Chen', exact: true })
          const rest = await paint(riley)
          const keyboardActive = await paint(active)
          await riley.hover()
          await expect(riley).toHaveAttribute('data-active', 'true')
          await expect(active).toHaveCount(1)
          await expect.poll(async () => (await paint(riley)).background).toEqual(keyboardActive.background)
          await expect.poll(async () => (await paint(riley)).foreground).toEqual(keyboardActive.foreground)
          await page.mouse.move(0, 0)
          await expect.poll(async () => (await paint(riley)).background).toEqual(rest.background)
          await expect(active).toHaveCount(0)
        }
        await popup.screenshot({ path: testInfo.outputPath(`${kind}-${dark ? 'dark' : 'light'}-${viewport.width}-${viewport.scale}x.png`) })
        await page.keyboard.press('Escape')
        await expect(popup).toBeHidden()
        await expect(control).toBeFocused()
        if (kind === 'dropdown-menu') {
          // The supplied Button owns its focus ring; menu items retain popup outlines.
          await expect.poll(async () => (await paint(control)).shadow).toMatch(/0px 0px 0px 4px/)
          const controlPaint = await paint(control)
          expect(contrast(controlPaint.effectiveBackground, controlPaint.foreground)).toBeGreaterThanOrEqual(4.5)
        }
      }
      expect(errors).toEqual([])
    })
  }
}
