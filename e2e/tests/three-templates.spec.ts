import { test, expect, specProduct } from '../fixture'

for (const scenario of [
  { path: '/examples/application/accounts', label: 'Ledger', link: 'Transactions', destination: '/examples/application/transactions' },
  { path: '/examples/application/settings/general', label: 'Settings', link: 'Users', destination: '/examples/application/settings/users' },
  { path: '/examples/application/profile', label: 'Profile', link: 'Organizations', destination: '/examples/application/profile/organizations' },
  { path: '/examples/specification/accounts/view-accounts', label: 'Documentation', link: 'Overview', destination: '/examples/specification' },
  { path: '/examples/api-documentation/create-account', label: 'Documentation', link: 'Overview', destination: '/examples/api-documentation' },
]) {
  test(`mobile ${scenario.path} uses a modal navigation drawer @cross-browser`, async ({ page }) => {
    const errors: string[] = []
    page.on('pageerror', error => errors.push(error.message))
    await page.setViewportSize({ width: 390, height: 844 })
    await page.goto(scenario.path)
    const opener = page.locator('#template-navigation-dialog-trigger').getByRole('button', { name: 'Open navigation', exact: true })
    const drawer = page.getByRole('dialog', { name: `${scenario.label} navigation`, exact: true })
    const close = drawer.getByRole('button', { name: 'Close navigation', exact: true })
    await expect(opener).toBeVisible()
    await opener.focus()
    await page.keyboard.press('Enter')
    await expect(drawer).toBeVisible()
    await expect(close).toBeFocused()
    await page.keyboard.press('Shift+Tab')
    expect(await drawer.evaluate(el => el.contains(document.activeElement))).toBe(true)
    await page.keyboard.press('Escape')
    await expect(drawer).toBeHidden()
    await expect(opener).toBeFocused()
    await opener.click()
    await close.click()
    await expect(opener).toBeFocused()
    await opener.click()
    await page.mouse.click(388, 420)
    await expect(drawer).toBeHidden()
    await expect(opener).toBeFocused()
    await opener.click()
    await page.setViewportSize({ width: 1440, height: 900 })
    await expect(drawer).toBeHidden()
    await expect(opener).toBeHidden()
    await expect(page.locator('#template-desktop-navigation')).toBeVisible()
    await page.setViewportSize({ width: 390, height: 844 })
    await opener.click()
    await drawer.getByRole('link', { name: scenario.link, exact: true }).first().click()
    await expect(page).toHaveURL(url => url.pathname === scenario.destination)
    await expect(page.locator('dialog[open]')).toHaveCount(0)
    await expect(page.locator('#main-content')).toBeFocused()
    await opener.click()
    await expect(drawer).toBeVisible()
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    if (scenario.label === 'Settings' || scenario.label === 'Profile') {
      await expect(drawer.getByRole('link', { name: 'Ledger', exact: true })).toBeVisible()
      const area = drawer.getByRole('heading', { name: scenario.label, level: 2, exact: true })
      await expect(area).toBeVisible()
      expect(await area.evaluate(el => el.closest('a, button'))).toBeNull()
      await page.setViewportSize({ width: 320, height: 844 })
      await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
      await expect.poll(() => area.locator('span').evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true)
      expect(await drawer.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true)
      await drawer.getByRole('link', { name: 'Back to Ledger', exact: true }).click()
      await expect(page.getByRole('heading', { name: 'Home', level: 1, exact: true })).toBeVisible()
      await expect(drawer).toBeHidden()
    } else if (scenario.label === 'Documentation') {
      await close.click()
      await page.setViewportSize({ width: 320, height: 900 })
      await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
      const shortcuts = page.locator('[data-fve-page-top-bar-actions]')
      for (const control of await shortcuts.getByRole('link').all()) {
        const bounds = (await control.boundingBox())!
        expect(bounds.x).toBeGreaterThanOrEqual(0)
        expect(bounds.x + bounds.width).toBeLessThanOrEqual(320)
      }
      await expect(shortcuts.getByRole('link')).toHaveCount(1)
      await expect(shortcuts.getByRole('link', { name: 'View example source on GitHub', exact: true })).toBeVisible()
      await expect(shortcuts.getByRole('link', { name: 'Settings', exact: true })).toHaveCount(0)
      await expect(shortcuts.getByRole('link', { name: 'Profile', exact: true })).toHaveCount(0)
      await expect(page.getByRole('link', { name: 'Open application', exact: true })).toHaveCount(0)
      await expect(page.getByRole('link', { name: 'Read specification', exact: true })).toHaveCount(0)
      const theme = shortcuts.getByRole('button', { name: 'Choose color theme', exact: true })
      await expect(theme).toBeVisible()
      const bounds = (await theme.boundingBox())!
      expect(bounds.x).toBeGreaterThanOrEqual(0)
      expect(bounds.x + bounds.width).toBeLessThanOrEqual(320)
    }
    expect(errors).toEqual([])
  })
}

test('gallery opens isolated consumer templates with GitHub source and native history', async ({ page }) => {
  await page.goto('/examples')
  const popup = page.waitForEvent('popup')
  await page.getByRole('link', { name: 'Application example (opens in a new tab)', exact: true }).click()
  const app = await popup
  await expect(app.getByRole('heading', { name: 'Home', exact: true, level: 1 })).toBeVisible()
  expect(await app.evaluate(() => window.opener)).toBeNull()
  await app.locator('#template-desktop-navigation').getByRole('link', { name: 'Accounts', exact: true }).click()
  await expect(app.getByRole('heading', { name: 'Accounts', exact: true, level: 1 })).toBeVisible()
  await expect(app.getByRole('navigation', { name: 'Example view' })).toHaveCount(0)
  const source = app.getByRole('link', { name: 'View example source on GitHub', exact: true })
  await expect(source).toHaveAttribute('href', /^https:\/\/github\.com\/meiermade\/FSharp\.ViewEngine(?:\/tree\/[^/]+\/sln\/src\/Example)?$/)
  await expect(source).toHaveAttribute('target', '_blank')
  await expect(app.locator('script[type="module"][src="/examples/assets/scripts/app.js"]')).toHaveCount(1)
  await expect(app.locator('fve-button[data-rocket-host]').first()).toBeAttached()
  await expect(app.locator('script[src$="/datastar.1.0.4.js"]')).toHaveCount(0)
  await expect(source).toHaveText('')
  await app.getByRole('link', { name: 'Profile', exact: true }).click()
  await app.getByRole('radiogroup', { name: 'Appearance', exact: true }).getByText('Dark', { exact: true }).click()
  await app.getByRole('link', { name: 'Back to Ledger', exact: true }).click()
  await app.locator('#template-desktop-navigation').getByRole('link', { name: 'Accounts', exact: true }).click()
  await expect(app.getByRole('heading', { name: 'Accounts', exact: true, level: 1 })).toBeVisible()
  await app.getByRole('link', { name: 'Operating checking', exact: true }).click()
  await expect(app.getByRole('heading', { name: 'Operating checking', exact: true, level: 1 })).toBeVisible()
  await app.goBack()
  await expect(app.getByRole('heading', { name: 'Accounts', exact: true, level: 1 })).toBeVisible()
  await app.goForward()
  await expect(app.getByRole('heading', { name: 'Operating checking', exact: true, level: 1 })).toBeVisible()
  await expect(app.locator('html')).toHaveClass(/dark/)
  await app.close()
})

test('spec App mode follows an exact account through validation cancel and current-state exit @cross-browser', async ({ page }) => {
  const product = specProduct(page)
  const pageErrors: string[] = []
  page.on('pageerror', error => pageErrors.push(error.message))
  await page.goto('/examples/specification/accounts/view-accounts')
  await expect(page.getByRole('tablist')).toHaveCount(0)
  await page.getByRole('button', { name: /^Review state:/ }).click()
  await page.getByRole('menuitem', { name: 'Assets', exact: true }).click()
  const assets = product
  await expect(assets.getByRole('link', { name: 'Operating checking', exact: true })).toBeVisible()
  await expect(assets.getByRole('link', { name: 'Unassigned expense', exact: true })).toHaveCount(0)
  await page.getByRole('button', { name: /^Review state:/ }).click()
  await page.getByRole('menuitem', { name: 'Hierarchy', exact: true }).click()
  await expect(page.locator('[data-fve-browser-address]')).not.toContainText('accountType=Asset')
  const address = await page.locator('[data-fve-browser-address]').innerText()
  await page.getByRole('link', { name: 'Expand View accounts · Hierarchy', exact: true }).click()
  await expect(page.locator('[data-fve-browser-address]')).toHaveText(address)
  await expect(page).toHaveURL(/appMode=1/)
  await expect(page.getByRole('navigation', { name: 'App mode controls' })).toBeVisible()
  await expect(page.getByRole('navigation', { name: 'Example view' })).toHaveCount(0)
  await product.getByRole('link', { name: 'Unassigned expense', exact: true }).click()
  await expect(page).toHaveURL(/accounts\/view-account.*resource=105.*appMode=1/)
  await product.getByRole('link', { name: 'Edit account', exact: true }).click()
  const dialog = product.getByRole('dialog', { name: 'Edit account', exact: true })
  await expect(dialog).toBeVisible()
  await expect(dialog.getByRole('navigation', { name: 'App mode controls' })).toHaveCount(0)
  await expect(page.locator('[data-fve-browser-toolbar]').getByRole('navigation', { name: 'App mode controls' })).toBeVisible()
  await dialog.getByRole('textbox', { name: /^Name/ }).fill('Tax reserve')
  const update = dialog.getByRole('button', { name: 'Update', exact: true })
  await update.click()
  await expect(update).toBeDisabled()
  await expect(update).toHaveAttribute('aria-busy', 'true')
  await expect(update.locator('.animate-spin')).toBeVisible()
  await expect(dialog.getByText('An account named “Tax reserve” already exists.', { exact: true })).toBeVisible()
  await expect(dialog.getByRole('textbox', { name: /^Name/ })).toHaveValue('Tax reserve')
  await expect(dialog.getByRole('textbox', { name: /^Name/ })).toBeFocused()
  await expect(dialog.locator('[data-fve-notice]')).toHaveCount(0)
  await expect(update).toBeEnabled()
  await expect(update.locator('.animate-spin')).toBeHidden()
  await dialog.getByRole('textbox', { name: /^Name/ }).fill('Unused expense label')
  await dialog.getByRole('button', { name: 'Update', exact: true }).click()
  await expect(dialog).toHaveCount(0)
  await expect(page).toHaveURL(/accounts\/view-account.*resource=105.*appMode=1/)
  await product.locator('[data-fve-page-header="true"]').getByRole('link', { name: 'Delete', exact: true }).click()
  const deletion = product.getByRole('dialog', { name: 'Delete Unassigned expense?', exact: true })
  await expect(deletion).toBeVisible()
  await expect(deletion.getByRole('link', { name: /^Restore / })).toHaveCount(0)
  await expect(page.locator('[data-fve-browser-toolbar]').getByRole('link', { name: /^Restore / })).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(page).toHaveURL(/accounts\/view-account.*resource=105.*appMode=1/)
  await page.getByRole('link', { name: /^Restore / }).click()
  await expect(page).toHaveURL(url => url.pathname.endsWith('/accounts/view-account') && url.searchParams.get('resource') === '105' && !url.searchParams.has('appMode'))
  await expect(page.getByRole('heading', { name: 'View account', exact: true, level: 1 })).toBeVisible()
  expect(pageErrors).toEqual([])
})

for (const scenario of [
  { path: 'accounts/101', title: 'Operating checking', blocked: true },
  { path: 'transactions/201', title: 'Client payment', blocked: false },
]) {
  test(`detail Delete action opens the existing confirmation for ${scenario.title} @cross-browser`, async ({ page }) => {
    const workspace = 'organization=example-household&environment=production&ledger=alex'
    await page.goto(`/examples/application/${scenario.path}?${workspace}`)
    const header = page.locator('[data-fve-page-header="true"]')
    const remove = header.getByRole('link', { name: 'Delete', exact: true })
    await expect(remove).toBeVisible()
    await expect(header.getByRole('button', { name: 'More actions', exact: true })).toHaveCount(0)
    await expect(header.getByRole('link', { name: 'Edit account', exact: true })).toHaveCount(scenario.blocked ? 1 : 0)
    await remove.focus()
    await page.keyboard.press('Enter')
    const dialog = page.getByRole('dialog', { name: `Delete ${scenario.title}?`, exact: true })
    await expect(dialog).toBeVisible()
    await expect(page).toHaveURL(url => url.searchParams.get('organization') === 'example-household' && url.searchParams.get('environment') === 'production' && url.searchParams.get('ledger') === 'alex')
    if (scenario.blocked) {
      await expect(dialog.getByText('Account cannot be deleted', { exact: true })).toBeVisible()
      await expect(dialog.getByRole('button', { name: 'Delete', exact: true })).toHaveCount(0)
      await page.keyboard.press('Escape')
      await expect(dialog).toHaveCount(0)
      await expect(page).toHaveURL(url => url.pathname === `/examples/application/${scenario.path}`)
      await expect(page.getByRole('heading', { name: scenario.title, level: 1, exact: true })).toBeVisible()
      await expect(header.getByRole('link', { name: 'Delete', exact: true })).toBeVisible()
    } else {
      await dialog.getByRole('button', { name: 'Delete', exact: true }).click()
      await expect(dialog).toHaveCount(0)
      await expect(page).toHaveURL(url => url.pathname === '/examples/application/transactions')
      await expect(page.locator('[data-fve-notification]').getByRole('status')).toContainText('1 transaction deleted')
      await expect(page.getByRole('checkbox', { name: 'Select Client payment', exact: true })).toHaveCount(0)
    }
  })
}

for (const scenario of [
  { collection: 'accounts', record: 'Operating checking', action: 'Edit account', dialog: 'Edit account', workflow: 'accounts/update-account', resource: '101', background: 'Accounts' },
  { collection: 'accounts', record: 'Unassigned expense', action: 'Delete account', dialog: 'Delete Unassigned expense?', workflow: 'accounts/delete-account', resource: '105', background: 'Accounts' },
  { collection: 'transactions', record: 'Client payment', action: 'Delete transaction', dialog: 'Delete Client payment?', workflow: 'transactions/delete-transaction', resource: '201', background: 'Transactions' },
]) {
  test(`collection-origin ${scenario.action} survives App-mode exit, expansion and state URLs @cross-browser`, async ({ page }) => {
    const product = specProduct(page)
    await page.goto(`/examples/specification/${scenario.collection}/view-${scenario.collection}?appMode=1`)
    await product.getByRole('button', { name: `Actions for ${scenario.record}`, exact: true }).click()
    await product.getByRole('menuitem', { name: scenario.action, exact: true }).click()
    const dialog = product.getByRole('dialog', { name: scenario.dialog, exact: true })
    await expect(dialog).toBeVisible()
    // The iframe can render its dialog before the parent commits the mirrored location.
    await expect(page).toHaveURL(url =>
      url.pathname === `/examples/specification/${scenario.workflow}` &&
      url.searchParams.getAll('from').length === 1 && url.searchParams.get('from') === scenario.collection)
    const assertOrigin = (href: string) => {
      const url = new URL(href, page.url())
      expect(url.searchParams.getAll('from')).toEqual([scenario.collection])
      return url
    }
    assertOrigin(page.url())
    await expect(product.locator('[data-template-shell] h1')).toHaveText(scenario.background)
    const controls = page.locator('[data-fve-browser-toolbar]').getByRole('navigation', { name: 'App mode controls' })
    await controls.getByRole('button', { name: /^Review state:/ }).click()
    for (const link of await controls.getByRole('menuitem').all()) assertOrigin((await link.getAttribute('href'))!)
    await page.keyboard.press('Escape')
    await page.locator('[data-fve-browser-toolbar]').getByRole('link', { name: /^Restore / }).click()
    await expect(page).toHaveURL(url => !url.searchParams.has('appMode'))
    const exited = assertOrigin(page.url())
    expect(exited.pathname).toBe(`/examples/specification/${scenario.workflow}`)
    expect(exited.searchParams.get('resource')).toBe(scenario.resource)
    expect(exited.searchParams.has('appMode')).toBe(false)
    const fixture = page.locator('[data-review-fixture]')
    await expect(product.locator('[data-template-shell] h1')).toHaveText(scenario.background)
    await fixture.getByRole('link', { name: /^Expand / }).click()
    await expect(page).toHaveURL(url => url.searchParams.get('appMode') === '1')
    assertOrigin(page.url())
    await expect(dialog).toBeVisible()
    await expect(product.locator('[data-template-shell] h1')).toHaveText(scenario.background)
    await dialog.getByRole('button', { name: scenario.action === 'Edit account' ? 'Update' : 'Delete', exact: true }).click()
    await expect(dialog).toHaveCount(0)
    if (scenario.action === 'Edit account') {
      await expect(page).toHaveURL(url => url.pathname === '/examples/specification/accounts/view-accounts')
    } else {
      await expect(page).toHaveURL(url => url.pathname === `/examples/specification/${scenario.collection}/view-${scenario.collection}`)
      await expect(product.getByText(scenario.collection === 'accounts' ? '1 account deleted' : '1 transaction deleted', { exact: true })).toBeVisible()
      await expect(product.getByRole('checkbox', { name: `Select ${scenario.record}`, exact: true })).toHaveCount(0)
    }
    await expect(product.locator('[data-template-shell] h1')).toHaveText(scenario.background)
  })
}

test('workspace menu and appearance preferences survive App mode document navigation', async ({ page }) => {
  const product = specProduct(page)
  await page.goto('/examples/specification/home?appMode=1')
  await expect(page.locator('[data-fve-browser-toolbar]').getByRole('button', { name: /theme/i })).toHaveCount(0)
  await expect(product.getByRole('button', { name: 'Choose color theme', exact: true })).toHaveCount(0)
  await product.getByRole('link', { name: 'Profile', exact: true }).click()
  await product.getByRole('radiogroup', { name: 'Appearance', exact: true }).getByText('Dark', { exact: true }).click()
  await product.getByRole('link', { name: 'Back to Ledger', exact: true }).click()
  await expect(page.locator('html')).toHaveClass(/dark/)
  await product.getByRole('button', { name: 'Open workspace menu: Alex', exact: true }).click()
  await product.getByRole('menuitem', { name: 'Jordan', exact: true }).click()
  await expect(page).toHaveURL(/ledger=jordan.*appMode=1/)
  await expect(page.locator('html')).toHaveClass(/dark/)
  await product.getByRole('link', { name: 'Profile', exact: true }).click()
  await expect(page).toHaveURL(/specification\/profile.*ledger=jordan.*appMode=1/)
  // The SSR radios are visible before the saved appearance and handlers initialize.
  await expect(product.getByRole('radio', { name: 'Dark', exact: true })).toBeChecked()
  const light = product.getByRole('radio', { name: 'Light', exact: true })
  await light.focus()
  await page.keyboard.press('Space')
  await expect(light).toBeChecked()
  await expect(page.locator('html')).not.toHaveClass(/dark/)
  await page.reload()
  await expect(light).toBeChecked()
  await expect(page.locator('html')).not.toHaveClass(/dark/)
  await expect(page.getByRole('navigation', { name: 'App mode controls' })).toBeVisible()
})

for (const { saved, choice, activation } of [
  { saved: 'dark', choice: 'Light', activation: 'Space' },
  { saved: 'dark', choice: 'System', activation: 'Space' },
  { saved: 'light', choice: 'System', activation: 'label' },
  { saved: 'dark', choice: 'Light', activation: 'ArrowDown' },
]) {
  test(`Profile appearance honors native activation before delayed initialization (${saved} → ${choice}, ${activation}) @cross-browser`, async ({ page }) => {
    const product = specProduct(page)
    // System must follow the OS, not simply select a light fallback.
    await page.emulateMedia({ colorScheme: choice === 'System' ? 'dark' : 'light' })
    await page.addInitScript(saved => {
      if (!localStorage.getItem('financial-example-appearance')) {
        localStorage.setItem('financial-example-appearance', saved)
      }
    }, saved)
    let release!: () => void
    const moduleReady = new Promise<void>(resolve => { release = resolve })
    await page.route('**/scripts/datastar-rocket.1.0.4.js', async route => {
      await moduleReady
      await route.continue()
    })
    try {
      // Observe native SSR interaction while the module is still unavailable, not after hydration.
      await page.goto('/examples/specification/profile?appMode=1', { waitUntil: 'commit' })
      const group = product.getByRole('radiogroup', { name: 'Appearance', exact: true })
      const clickText = async (text: string) => {
        const target = group.getByText(text, { exact: true })
        await expect(target).toBeVisible()
        await target.evaluate(el => el.scrollIntoView({ block: 'center', behavior: 'instant' }))
        const box = await target.boundingBox()
        expect(box).not.toBeNull()
        // WebKit withholds animation frames while this module is pending. Use a
        // real native pointer, not locator.click's frame-based stability wait.
        await page.mouse.click(box!.x + box!.width / 2, box!.y + box!.height / 2)
      }
      // A bubbled non-input click must not overwrite the preference with undefined.
      await clickText('Appearance')
      expect(await page.evaluate(() => localStorage.getItem('financial-example-appearance'))).toBe(saved)
      const radio = product.getByRole('radio', { name: choice, exact: true })
      await expect(radio).toBeVisible()
      const system = product.getByRole('radio', { name: 'System', exact: true })
      if (choice === 'System' || activation === 'ArrowDown') await expect(system).toBeChecked()
      if (activation === 'label') {
        await clickText(choice)
      } else {
        await (activation === 'ArrowDown' ? system : radio).focus()
        await page.keyboard.press(activation)
      }
      const mode = choice.toLowerCase()
      await expect(radio).toBeChecked()
      await expect(page.locator('html')).toHaveAttribute('data-color-mode', mode)
      expect(await page.evaluate(() => localStorage.getItem('financial-example-appearance'))).toBe(mode)
      release()
      await page.waitForLoadState('load')
      await expect(radio).toBeChecked()
      await expect(page.locator('html')).toHaveClass(choice === 'System' ? /dark/ : /^(?!.*\bdark\b)/)
      await page.reload()
      await expect(radio).toBeChecked()
      await expect(page.locator('html')).toHaveAttribute('data-color-mode', mode)
      await expect(page.locator('html')).toHaveClass(choice === 'System' ? /dark/ : /^(?!.*\bdark\b)/)
    } finally {
      release()
    }
  })
}

test('rendered architecture nodes navigate from system context to project contracts', async ({ page }) => {
  await page.goto('/examples/specification/architecture')
  await page.getByRole('region', { name: 'Ledger system context', exact: true }).locator('svg a').click()
  await expect(page).toHaveURL(/architecture\/solution/)
  await page.getByRole('region', { name: 'Ledger project dependencies and HTTP boundary', exact: true }).locator('svg a').filter({ hasText: 'Example.Application' }).click()
  await expect(page).toHaveURL(/architecture\/solution\/application/)
  await expect(page.getByRole('heading', { name: 'Operations', exact: true })).toBeVisible()
  await expect(page.getByText(/Example.Application\/Operations.fs validates/)).toBeVisible()
})

test('API examples inherit the selected theme and separate code from the page surface @cross-browser', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'light' })
  await page.goto('/examples/api-documentation/create-account')
  const examples = page.getByRole('complementary', { name: 'Request and response examples', exact: true })
  const code = examples.locator('pre').first()
  const string = examples.locator('.token.string').first()
  await expect(string).toBeAttached()
  const backgrounds: string[] = []
  const syntax: string[] = []
  for (const theme of ['Light', 'Dark']) {
    await page.getByRole('button', { name: 'Choose color theme', exact: true }).click()
    await page.getByRole('menuitemradio', { name: theme, exact: true }).click()
    await expect(examples).toHaveCSS('color-scheme', theme.toLowerCase())
    const pageBackground = await examples.evaluate(el => getComputedStyle(el).backgroundColor)
    await expect(code).not.toHaveCSS('background-color', pageBackground)
    backgrounds.push(await code.evaluate(el => getComputedStyle(el).backgroundColor))
    syntax.push(await string.evaluate(el => getComputedStyle(el).color))
    await page.getByRole('tab', { name: '400', exact: true }).click()
    await expect(page.getByRole('tabpanel', { name: '400', exact: true })).toContainText('validation_failed')
    await page.getByRole('tab', { name: '201', exact: true }).click()
  }
  expect(backgrounds[0]).not.toBe(backgrounds[1])
  expect(syntax[0]).not.toBe(syntax[1])
})

test('API response tabs and copied cURL remain usable in a narrow layout', async ({ page, context }) => {
  await context.grantPermissions(['clipboard-read', 'clipboard-write'])
  await page.setViewportSize({ width: 320, height: 844 })
  await page.goto('/examples/api-documentation/create-account')
  await page.getByRole('tab', { name: '400', exact: true }).click()
  await expect(page.getByRole('tabpanel', { name: '400', exact: true })).toContainText('validation_failed')
  await page.getByRole('button', { name: /Copy.*code/ }).first().click()
  await expect.poll(() => page.evaluate(() => navigator.clipboard.readText())).toContain('curl')
  const copied = await page.evaluate(() => navigator.clipboard.readText())
  expect(copied).toContain('$API_ORIGIN')
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
})

test('shared shell and editor keep actions reachable at narrow enlarged text', async ({ page }) => {
  const product = specProduct(page)
  await page.setViewportSize({ width: 320, height: 900 })
  await page.goto('/examples/specification/accounts/update-account?appMode=1&resource=105')
  await page.locator('html').evaluate(el => el.style.fontSize = '200%')
  await product.locator('html').evaluate(el => el.style.fontSize = '200%')
  const dialog = product.getByRole('dialog', { name: 'Edit account', exact: true })
  await expect(dialog.getByRole('button', { name: 'Update', exact: true })).toBeVisible()
  const toolbar = page.locator('[data-fve-browser-toolbar]')
  await expect(toolbar.getByRole('link', { name: /^Restore / })).toBeVisible()
  const controls = await toolbar.locator('a, button').evaluateAll(elements => elements
    .filter(el => el.getClientRects().length).map(el => {
      const rect = el.getBoundingClientRect()
      return { name: el.getAttribute('aria-label'), left: rect.left, right: rect.right }
    }))
  for (const control of controls) {
    expect(control.left, control.name!).toBeGreaterThanOrEqual(0)
    expect(control.right, control.name!).toBeLessThanOrEqual(320)
  }
  await toolbar.getByRole('button', { name: /^Review state:/ }).click()
  await expect(page.getByRole('menuitem', { name: 'Name already exists', exact: true })).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(dialog).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  await dialog.getByRole('textbox', { name: /^Name/ }).fill('Expense validation')
  await dialog.getByRole('button', { name: 'Update', exact: true }).click()
  await expect(dialog).toHaveCount(0)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
})
