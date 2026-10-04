import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

test('error summary sample exposes the public API and its field connection @cross-browser', async ({ page }, testInfo) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/error-summary')
  const example = page.locator('[data-docs-example="true"]').first()
  const summary = example.getByRole('alert')
  const email = example.getByRole('textbox', { name: 'Email address' })
  await expect(summary).toBeVisible()
  await expect(email).toHaveValue('not-an-email')
  await expect(email).toHaveAttribute('aria-invalid', 'true')
  await expect(email).toHaveAccessibleDescription('Enter a valid email address.')
  const link = summary.getByRole('link', { name: 'Email address: Enter a valid email address.' })
  await expect(link).toHaveAttribute('href', '#summary-email')
  await link.focus()
  await page.keyboard.press('Enter')
  await expect(email).toBeFocused()
  await expect(page).toHaveURL(/#summary-email$/)
  await email.fill('alex@fve.meiermade.com')
  await expect(summary).toBeVisible() // Presentation does not run application validation.
  await expect(email).toHaveAttribute('aria-invalid', 'true')
  const toolbar = example.locator(':scope > [data-docs-example-toolbar="true"]')
  await toolbar.getByRole('tab', { name: 'Code', exact: true }).click()
  const code = example.locator('code').first()
  for (const api of ['Input.withId "summary-email"', 'Input.withValidation emailError', 'FieldError.create (Input.id email)', 'ErrorSummary.create', 'ErrorSummary.render', 'Input.render email']) {
    await expect(code).toContainText(api)
  }
  await expect(code).not.toContainText('contactFormRegion')
  await expect(code).not.toContainText('fullBleedThemedSurface')
  await example.screenshot({ path: testInfo.outputPath('error-summary-code.png') })
  await toolbar.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(email).toHaveValue('alex@fve.meiermade.com')
  await page.getByRole('link', { name: 'Input', exact: true }).click()
  await expect(page).toHaveURL(/\/components\/input$/)
  await expect(page.locator('#components-input-panel-preview').getByRole('textbox', { name: 'Email', exact: true })).toBeVisible()
  expect(errors).toEqual([])
})

test('Docs document history still fetches changed pages @cross-browser', async ({ page }) => {
  await page.goto('/components/input')
  await page.getByRole('link', { name: 'Textarea', exact: true }).click()
  await expect(page).toHaveURL(/\/components\/textarea$/)
  await expect(page.getByRole('textbox', { name: 'Payment instructions' })).toBeVisible()
  await page.goBack()
  await expect(page).toHaveURL(/\/components\/input$/)
  await expect(page.locator('#components-input-panel-preview').getByRole('textbox', { name: 'Email', exact: true })).toBeVisible()
  await page.goForward()
  await expect(page).toHaveURL(/\/components\/textarea$/)
  await expect(page.getByRole('textbox', { name: 'Payment instructions' })).toBeVisible()
})

test('financial account search clears without submitting or changing the current results @cross-browser', async ({ page }) => {
  await page.goto('/examples/application/accounts')
  const root = page.locator('#main-content')
  const query = root.getByRole('searchbox', { name: 'Search accounts', exact: true })
  const clear = root.getByRole('button', { name: 'Clear Search accounts', includeHidden: true })
  const rows = root.getByRole('table').locator('tbody tr')
  const initialCount = await rows.count()
  expect(initialCount).toBeGreaterThan(1)
  await expect(clear).toBeHidden()
  await query.fill('Tax')
  await query.press('Enter')
  await expect(root.getByRole('link', { name: 'Tax reserve', exact: true })).toBeVisible()
  await expect(root.getByRole('link', { name: 'Operating checking', exact: true })).toHaveCount(0)
  const filteredCount = await rows.count()
  const filteredUrl = page.url()
  await clear.focus()
  await page.keyboard.press('Enter')
  await expect(query).toHaveValue('')
  await expect(query).toBeFocused()
  await expect(clear).toBeHidden()
  await expect(page).toHaveURL(filteredUrl)
  await expect(rows).toHaveCount(filteredCount) // Clearing edits the query; submitting search owns the result update.
  await query.press('Enter')
  await expect(rows).toHaveCount(initialCount)
  expect(await query.getAttribute('aria-haspopup')).toBeNull()
})

test('native textarea editing states preserve successful values only @cross-browser', async ({ page }) => {
  await page.goto('/components/textarea')
  const editable = page.getByRole('textbox', { name: 'Payment instructions' })
  await expect(editable).toBeEditable()
  await expect(editable).toHaveAttribute('maxlength', '400')
  await editable.fill('Invoice INV-2048\nSecond line')
  await expect(page.getByRole('heading', { name: 'Read-only', exact: true })).toHaveCount(0)
  await expect(page.getByRole('textbox', { name: 'Checking notes' })).not.toBeEditable()
  await expect(page.getByRole('textbox', { name: 'Checking notes' })).toHaveAttribute('aria-busy', 'true')
  await expect(page.getByRole('textbox', { name: 'Unavailable notes' })).toBeDisabled()
  const values = await page.locator('[data-docs-layout="gallery"]').evaluate(element => {
    const form = document.createElement('form')
    for (const field of element.querySelectorAll('textarea')) form.append(field.cloneNode(true))
    return Object.fromEntries(new FormData(form))
  })
  expect(values).toMatchObject({ message: '', invalidMessage: '', instructions: 'Invoice INV-2048\nSecond line', pendingNotes: 'Please quote invoice INV-2048.' })
  expect(values).not.toHaveProperty('unavailableNotes')
})

for (const component of ['input', 'textarea', 'error-summary', 'notice']) {
  test(`${component} remains accessible in narrow light/dark and resized text`, async ({ page }, testInfo) => {
    const errors: string[] = []
    page.on('pageerror', error => errors.push(error.message))
    await page.setViewportSize({ width: 390, height: 1000 })
    await page.goto(`/components/${component}`)
    const preview = page.locator(`#components-${component}-panel-preview`)
    await expect(preview).toBeVisible()
    for (const theme of ['Light', 'Dark']) {
      await page.getByRole('button', { name: 'Choose color theme' }).click()
      await page.getByRole('menuitemradio', { name: theme, exact: true }).click()
      // Theme transitions interpolate both foreground and background; audit the settled theme.
      await page.waitForFunction(() => !document.getAnimations().some(animation => {
        const target = (animation.effect as KeyframeEffect | null)?.target
        return animation.playState === 'running' && Number.isFinite(animation.effect?.getComputedTiming().endTime)
          && target instanceof Element && target.checkVisibility() && target.closest('[data-docs-layout="gallery"]')
      }))
      expect((await new AxeBuilder({ page }).include('[data-docs-layout="gallery"]').analyze()).violations).toEqual([])
      await preview.screenshot({ path: testInfo.outputPath(`${component}-${theme.toLowerCase()}-390.png`) })
    }
    await page.setViewportSize({ width: 320, height: 1000 })
    await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    for (const control of await preview.locator('input, textarea, button, a').all()) {
      if (await control.isVisible()) {
        const box = (await control.boundingBox())!
        expect(box.x).toBeGreaterThanOrEqual(0)
        expect(box.x + box.width).toBeLessThanOrEqual(321)
      }
    }
    expect(errors).toEqual([])
  })
}
