import { test, expect } from '../fixture'

test('gallery opens isolated templates with source download and native history', async ({ page }) => {
  await page.goto('/examples')
  const popup = page.waitForEvent('popup')
  await page.getByRole('link', { name: 'Application example (opens in a new tab)', exact: true }).click()
  const app = await popup
  await expect(app.getByRole('heading', { name: 'Home', exact: true, level: 1 })).toBeVisible()
  expect(await app.evaluate(() => window.opener)).toBeNull()
  await app.locator('#template-desktop-navigation').getByRole('link', { name: 'Accounts', exact: true }).click()
  await expect(app.getByRole('navigation', { name: 'Example view' })).toHaveCount(0)
  const download = app.waitForEvent('download')
  await app.getByRole('link', { name: 'Download example source ZIP', exact: true }).click()
  expect((await download).suggestedFilename()).toBe('ledger-examples.zip')
  await app.getByRole('button', { name: 'Choose color theme', exact: true }).click()
  await app.getByRole('menuitemradio', { name: 'Dark', exact: true }).click()
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
  await page.goto('/examples/specification/accounts/view-accounts')
  await page.getByRole('tab', { name: 'Assets', exact: true }).click()
  const assets = page.getByRole('tabpanel', { name: 'Assets', exact: true })
  await expect(assets.getByRole('link', { name: 'Operating checking', exact: true })).toBeVisible()
  await expect(assets.getByRole('link', { name: 'Unassigned expense', exact: true })).toHaveCount(0)
  await page.getByRole('tab', { name: 'Hierarchy', exact: true }).click()
  await page.getByRole('link', { name: 'Open View accounts · Hierarchy in App mode', exact: true }).click()
  await expect(page).toHaveURL(/appMode=1/)
  await expect(page.getByRole('navigation', { name: 'App mode controls' })).toBeVisible()
  await expect(page.getByRole('navigation', { name: 'Example view' })).toHaveCount(0)
  await page.getByRole('link', { name: 'Unassigned expense', exact: true }).click()
  await expect(page).toHaveURL(/accounts\/view-account.*resource=105.*appMode=1/)
  await page.getByRole('link', { name: 'Edit account', exact: true }).click()
  const dialog = page.getByRole('dialog', { name: 'Edit account', exact: true })
  await expect(dialog).toBeVisible()
  await expect(dialog.getByRole('navigation', { name: 'App mode controls' })).toBeVisible()
  await dialog.getByRole('textbox', { name: /^Name/ }).fill('Tax reserve')
  await dialog.getByRole('button', { name: 'Update', exact: true }).click()
  await expect(dialog.getByText('Use a unique name between 1 and 80 characters.', { exact: true })).toBeVisible()
  await dialog.getByRole('button', { name: 'Move App mode controls to top', exact: true }).click()
  await dialog.getByRole('textbox', { name: /^Name/ }).fill('Unused expense label')
  await dialog.getByRole('button', { name: 'Update', exact: true }).click()
  await expect(dialog.getByText('Account validated', { exact: true })).toBeVisible()
  await expect(page).toHaveURL(/fveAppDock=top/)
  await expect(dialog.getByRole('textbox', { name: /^Name/ })).toHaveValue('Unassigned expense')
  await page.keyboard.press('Escape')
  await expect(page).toHaveURL(/accounts\/view-account.*resource=105.*appMode=1.*fveAppDock=top/)
  await page.getByRole('button', { name: 'More actions', exact: true }).click()
  await page.getByRole('menuitem', { name: 'Delete', exact: true }).click()
  const deletion = page.getByRole('dialog', { name: 'Delete Unassigned expense?', exact: true })
  await expect(deletion).toBeVisible()
  await expect(deletion.getByRole('link', { name: 'Exit App mode', exact: true })).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(page).toHaveURL(/accounts\/view-account.*resource=105.*appMode=1.*fveAppDock=top/)
  await page.getByRole('link', { name: 'Exit App mode', exact: true }).click()
  await expect(page).toHaveURL(/accounts\/view-account.*resource=105/)
  expect(new URL(page.url()).searchParams.has('appMode')).toBe(false)
  await expect(page.getByRole('heading', { name: 'View account', exact: true, level: 1 })).toBeVisible()
})

test('workspace menu and appearance preferences survive App mode document navigation', async ({ page }) => {
  await page.goto('/examples/specification/home?appMode=1')
  const controls = page.getByRole('navigation', { name: 'App mode controls' })
  await controls.getByRole('button', { name: 'Move App mode controls to top', exact: true }).click()
  await controls.getByRole('button', { name: 'Choose theme', exact: true }).click()
  await page.getByRole('menuitemradio', { name: 'Dark', exact: true }).click()
  await expect(page.locator('html')).toHaveClass(/dark/)
  await page.getByRole('button', { name: 'Open workspace menu: Alex', exact: true }).click()
  await page.getByRole('menuitem', { name: 'Jordan', exact: true }).click()
  await expect(page).toHaveURL(/ledger=jordan.*appMode=1.*fveAppDock=top/)
  await expect(page.locator('html')).toHaveClass(/dark/)
  await page.getByRole('link', { name: 'Andrew Meier', exact: true }).click()
  await expect(page).toHaveURL(/specification\/profile.*ledger=jordan.*appMode=1/)
  // The SSR radios are visible before the saved appearance and handlers initialize.
  await expect(page.getByRole('radio', { name: 'Dark', exact: true })).toBeChecked()
  const light = page.getByRole('radio', { name: 'Light', exact: true })
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
    // System must follow the OS, not simply select a light fallback.
    await page.emulateMedia({ colorScheme: choice === 'System' ? 'dark' : 'light' })
    await page.addInitScript(saved => {
      if (!localStorage.getItem('fsharp-viewengine-docs-navigation-color-mode')) {
        localStorage.setItem('fsharp-viewengine-docs-navigation-color-mode', saved)
      }
    }, saved)
    let release!: () => void
    const moduleReady = new Promise<void>(resolve => { release = resolve })
    await page.route('**/scripts/datastar.1.0.2.js', async route => {
      await moduleReady
      await route.continue()
    })
    try {
      // Observe native SSR interaction while the module is still unavailable, not after hydration.
      await page.goto('/examples/specification/profile?appMode=1', { waitUntil: 'commit' })
      const group = page.getByRole('radiogroup', { name: 'Appearance', exact: true })
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
      expect(await page.evaluate(() => localStorage.getItem('fsharp-viewengine-docs-navigation-color-mode'))).toBe(saved)
      const radio = page.getByRole('radio', { name: choice, exact: true })
      await expect(radio).toBeVisible()
      const system = page.getByRole('radio', { name: 'System', exact: true })
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
      expect(await page.evaluate(() => localStorage.getItem('fsharp-viewengine-docs-navigation-color-mode'))).toBe(mode)
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
  await page.getByRole('region', { name: 'Ledger project dependencies and HTTP boundary', exact: true }).locator('svg a').filter({ hasText: 'Ledger.Application' }).click()
  await expect(page).toHaveURL(/architecture\/solution\/application/)
  await expect(page.getByRole('heading', { name: 'Operations', exact: true })).toBeVisible()
  await expect(page.getByText(/UseCases\/Operations.fs owns|UseCases\/Operations.fs validates/)).toBeVisible()
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
  await page.setViewportSize({ width: 320, height: 900 })
  await page.goto('/examples/specification/accounts/update-account?appMode=1&resource=105')
  await page.locator('html').evaluate(el => el.style.fontSize = '200%')
  const dialog = page.getByRole('dialog', { name: 'Edit account', exact: true })
  await expect(dialog.getByRole('button', { name: 'Update', exact: true })).toBeVisible()
  await expect(dialog.getByRole('link', { name: 'Exit App mode', exact: true })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  await dialog.getByRole('textbox', { name: /^Name/ }).fill('Expense validation')
  await dialog.getByRole('button', { name: 'Update', exact: true }).click()
  await expect(dialog.getByText('Account validated', { exact: true })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
})
