import { test, expect } from '../fixture'

test('theming teaches focused recipes and leaves complete setup in Installation', { tag: '@cross-browser' }, async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 950 })
  await page.goto('/guides/theming#appearance')
  await expect(page).toHaveURL(/\/guides\/theming#appearance$/)
  await expect(page.getByRole('heading', { name: 'Add an appearance picker', exact: true })).toBeInViewport()
  const railBottoms = () => page.evaluate(() => ({
    documentScroll: window.scrollY,
    leftGap: Math.round(innerHeight - document.querySelector('[data-docs-side-nav]')!.getBoundingClientRect().bottom),
    rightGap: Math.round(innerHeight - document.querySelector('[data-docs-toc-rail]')!.getBoundingClientRect().bottom),
  }))
  await expect.poll(railBottoms).toEqual({ documentScroll: 0, leftGap: 0, rightGap: 0 })
  await expect(page.getByRole('heading', { level: 1, name: 'Theming', exact: true })).toBeVisible()
  await expect(page.locator('a[href="/components/theme-switcher"]')).toHaveCount(0)
  const profileExample = page.getByRole('link', { name: 'Profile example', exact: true })
  await expect(profileExample).toHaveAttribute('href', '/examples/application/profile')
  const source = page.locator('#appearance').locator('..')
  await expect(source).toContainText('ColorMode.assets "my-app-appearance" ColorMode.System')
  await expect(source).toContainText('/css/output.css')
  await expect(page.locator('main')).not.toContainText('npm install')
  await expect(page.locator('main')).not.toContainText('appearanceDocument')
  const themedSave = page.locator('#guide-themedControls-panel-preview').getByRole('button', { name: 'Save', exact: true })
  await expect(themedSave).toHaveCSS('min-height', '28px')
  expect(await themedSave.evaluate(el => getComputedStyle(el).getPropertyValue('--fve-brand-solid'))).not.toBe('')
  const choices = page.getByRole('radiogroup', { name: 'Appearance', exact: true })
  await choices.getByRole('radio', { name: 'Dark', exact: true }).check()
  await expect(page.locator('html')).toHaveClass(/dark/)
  await page.emulateMedia({ colorScheme: 'dark' })
  await choices.getByRole('radio', { name: 'System', exact: true }).check()
  await expect(page.locator('html')).toHaveClass(/dark/)
  expect(await page.evaluate(() => (window as any).fveColorMode.current())).toBe('system')
  await profileExample.click()
  await expect(page).toHaveURL(/\/examples\/application\/profile$/)
  await expect(page.getByRole('radiogroup', { name: 'Appearance', exact: true })).toBeVisible()
})

test('Using signals demonstrates value binding, reactive attributes and a button action', { tag: '@cross-browser' }, async ({ page }) => {
  await page.goto('/guides/using-signals')
  await expect(page).toHaveURL(/\/guides\/using-signals$/)
  const example = page.locator('[data-docs-example="true"]').first()
  await expect(example.getByRole('tab')).toHaveText(['Preview', 'Code'])
  await expect(example.locator('[data-docs-example-title]')).toHaveClass('sr-only')
  const preview = page.locator('#guide-usingSignals-panel-preview')
  const input = preview.getByRole('textbox', { name: 'Name', exact: true })
  const reset = preview.getByRole('button', { name: 'Reset', exact: true })
  await expect(reset).toBeDisabled()
  await input.fill('Ada')
  await expect(preview).toContainText('Preview: Ada')
  await expect(reset).toBeEnabled()
  await reset.click()
  await expect(input).toHaveValue('')
  await expect(preview).toContainText('Preview: Untitled')
  await expect(reset).toBeDisabled()
  await expect(page.getByRole('link', { name: 'Datastar guide', exact: true })).toHaveAttribute('href', '/extensions/datastar')
})

test('retired appearance demo links reach the app picker, which reflows at enlarged text', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 900 })
  for (const path of ['/guides/theming/document', '/components/theming/document']) {
    await page.goto(path)
    await expect(page).toHaveURL(/\/examples\/application\/profile$/)
  }
  await page.locator('html').evaluate(element => { element.style.fontSize = '200%' })
  const choices = page.getByRole('radiogroup', { name: 'Appearance', exact: true })
  await choices.getByText('Dark', { exact: true }).click()
  await expect(choices.getByRole('radio', { name: 'Dark', exact: true })).toBeChecked()
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(320)
})

test('the application appearance picker preserves its preference across reloads and tabs', { tag: '@cross-browser' }, async ({ page, context }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.emulateMedia({ colorScheme: 'light' })
  await page.goto('/examples/application/profile')
  const choices = page.getByRole('radiogroup', { name: 'Appearance', exact: true })
  await expect(choices.getByRole('radio', { name: 'System', exact: true })).toBeChecked()

  await choices.getByText('Dark', { exact: true }).click()
  await expect(page.locator('html')).toHaveClass(/dark/)
  expect(await page.evaluate(() => localStorage.getItem('financial-example-appearance'))).toBe('dark')
  expect(await page.evaluate(() => localStorage.getItem('fsharp-viewengine-docs-navigation-color-mode'))).toBeNull()

  await choices.getByText('System', { exact: true }).click()
  await expect(page.locator('html')).not.toHaveClass(/dark/)
  await page.emulateMedia({ colorScheme: 'dark' })
  await expect(page.locator('html')).toHaveClass(/dark/)
  await expect(page.locator('html')).toHaveAttribute('data-color-mode', 'system')
  await expect(choices.getByRole('radio', { name: 'System', exact: true })).toBeChecked()

  const sibling = await context.newPage()
  try {
    await sibling.goto('/examples/application/profile')
    await expect(sibling.locator('html')).toHaveAttribute('data-color-mode', 'system')
    await choices.getByText('Light', { exact: true }).click()
    await expect(page.locator('html')).not.toHaveClass(/dark/)
    await expect(sibling.locator('html')).toHaveAttribute('data-color-mode', 'light')
    await expect(sibling.getByRole('radiogroup', { name: 'Appearance', exact: true }).getByRole('radio', { name: 'Light', exact: true })).toBeChecked()
    await page.reload()
    await expect(choices.getByRole('radio', { name: 'Light', exact: true })).toBeChecked()
    await expect(page.locator('html')).not.toHaveClass(/dark/)
  } finally {
    await sibling.close()
  }
  expect(errors).toEqual([])
})
