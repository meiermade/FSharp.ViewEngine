import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

test('native contact values survive server rejection and linked correction @cross-browser', async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/form-layouts')
  const form = page.getByRole('form', { name: 'Contact details', exact: true })
  await expect(form).toBeVisible()
  const documentFetches: string[] = []
  page.on('request', request => {
    if (request.method() === 'GET' && new URL(request.url()).pathname === '/components/form-layouts') documentFetches.push(request.url())
  })
  const name = form.getByRole('textbox', { name: 'Contact name' })
  const email = form.getByRole('textbox', { name: 'Email address' })
  const notes = form.getByRole('textbox', { name: 'Notes' })
  await email.fill('not-an-email')
  await notes.fill('Keep this note after validation. <Not markup>')
  const response = page.waitForResponse(response => response.url().endsWith('/components/forms/contact'))
  await form.getByRole('button', { name: 'Validate details' }).click()
  expect((await response).status()).toBe(200)
  const summary = page.getByRole('alert').filter({ hasText: 'Check your contact details' })
  await expect(summary).toBeVisible()
  await expect(summary).toBeFocused()
  await expect(summary.getByRole('link')).toHaveCount(2)
  await expect(notes).toHaveValue('Keep this note after validation. <Not markup>')
  await expect(email).toHaveValue('not-an-email')
  await summary.getByRole('link', { name: 'Email address: Enter a valid email address.' }).click()
  await expect(email).toBeFocused()
  await expect(page).toHaveURL(/#contact-email$/)
  await page.goBack()
  await expect(page).toHaveURL(/\/components\/form-layouts$/)
  await expect(notes).toHaveValue('Keep this note after validation. <Not markup>')
  await page.goForward()
  await expect(page).toHaveURL(/#contact-email$/)
  await expect(notes).toHaveValue('Keep this note after validation. <Not markup>')
  expect(documentFetches).toEqual([])
  await name.fill('Alex Rivera')
  await form.getByRole('button', { name: 'Validate details' }).click()
  await expect(summary.getByRole('link')).toHaveCount(1)
  await expect(name).toHaveValue('Alex Rivera')
  await email.fill('alex@example.test')
  expect(await form.evaluate(form => Object.fromEntries(new FormData(form as HTMLFormElement)))).toEqual({
    contactName: 'Alex Rivera', email: 'alex@example.test', notes: 'Keep this note after validation. <Not markup>',
  })
  await form.getByRole('button', { name: 'Validate details' }).click()
  await expect(page.getByRole('status').filter({ hasText: 'Details are valid' })).toBeVisible()
  await expect(summary).toHaveCount(0)
  await expect(email).toHaveAttribute('aria-invalid', 'false')
  await expect(notes).toHaveValue('Keep this note after validation. <Not markup>')
  expect((await new AxeBuilder({ page }).include('#components-contact-region').analyze()).violations).toEqual([])
  expect(errors).toEqual([])
})

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
  await email.fill('alex@example.test')
  await expect(summary).toBeVisible() // Presentation does not run application validation.
  await expect(email).toHaveAttribute('aria-invalid', 'true')
  const toolbar = example.locator(':scope > .spec-example-toolbar')
  await toolbar.getByRole('tab', { name: 'Code', exact: true }).click()
  const code = example.locator('code').first()
  for (const api of ['Input.withId "summary-email"', 'Input.withValidation emailError', 'FieldError.create (Input.id email)', 'ErrorSummary.create', 'ErrorSummary.render', 'Input.render email']) {
    await expect(code).toContainText(api)
  }
  await expect(code).not.toContainText('contactFormRegion')
  await expect(code).not.toContainText('fullBleedThemedSurface')
  await example.screenshot({ path: testInfo.outputPath('error-summary-code.png') })
  await toolbar.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(email).toHaveValue('alex@example.test')
  await page.getByRole('link', { name: 'Input', exact: true }).click()
  await expect(page).toHaveURL(/\/components\/input$/)
  await expect(page.locator('#components-input').getByRole('textbox', { name: 'Email', exact: true })).toBeVisible()
  expect(errors).toEqual([])
})

test('Docs document history still fetches changed pages @cross-browser', async ({ page }) => {
  await page.goto('/components/input')
  await page.getByRole('link', { name: 'Textarea', exact: true }).click()
  await expect(page).toHaveURL(/\/components\/textarea$/)
  await expect(page.getByRole('textbox', { name: 'Payment instructions' })).toBeVisible()
  await page.goBack()
  await expect(page).toHaveURL(/\/components\/input$/)
  await expect(page.locator('#components-input').getByRole('textbox', { name: 'Email', exact: true })).toBeVisible()
  await page.goForward()
  await expect(page).toHaveURL(/\/components\/textarea$/)
  await expect(page.getByRole('textbox', { name: 'Payment instructions' })).toBeVisible()
})

test('query-only search clears through native input events and retains focus @cross-browser', async ({ page }) => {
  await page.goto('/components/form-layouts')
  const query = page.getByRole('searchbox', { name: 'Search accounts' })
  const clear = page.getByRole('button', { name: 'Clear Search accounts', includeHidden: true })
  await expect(query).toBeVisible()
  await expect(clear).toBeDisabled()
  await query.fill('Savings')
  const results = page.getByRole('list', { name: 'Matching accounts' })
  await expect(results.getByRole('listitem').filter({ visible: true })).toHaveCount(1)
  await expect(results.getByText('Savings', { exact: true })).toBeVisible()
  await expect(page.getByRole('status').filter({ hasText: '1 matching accounts' })).toBeVisible()
  await expect(clear).toBeEnabled()
  await clear.focus()
  await page.keyboard.press('Enter')
  await expect(query).toHaveValue('')
  await expect(query).toBeFocused()
  await expect(results.getByRole('listitem').filter({ visible: true })).toHaveCount(3)
  await expect(clear).toBeDisabled()
  await expect(page.getByRole('form', { name: 'Contact details', exact: true }).getByRole('textbox', { name: 'Contact name' })).toHaveValue('')
  expect(await query.getAttribute('aria-haspopup')).toBeNull()
})

test('native textarea editing states preserve successful values only @cross-browser', async ({ page }) => {
  await page.goto('/components/textarea')
  const editable = page.getByRole('textbox', { name: 'Payment instructions' })
  await expect(editable).toBeEditable()
  await expect(editable).toHaveAttribute('maxlength', '400')
  await editable.fill('Invoice INV-2048\nSecond line')
  await expect(page.getByRole('textbox', { name: 'Accepted notes' })).not.toBeEditable()
  await expect(page.getByRole('textbox', { name: 'Checking notes' })).not.toBeEditable()
  await expect(page.getByRole('textbox', { name: 'Checking notes' })).toHaveAttribute('aria-busy', 'true')
  await expect(page.getByRole('textbox', { name: 'Unavailable notes' })).toBeDisabled()
  const values = await page.locator('.docs-gallery-layout').evaluate(element => {
    const form = document.createElement('form')
    for (const field of element.querySelectorAll('textarea')) form.append(field.cloneNode(true))
    return Object.fromEntries(new FormData(form))
  })
  expect(values).toEqual({ message: '', invalidMessage: '', instructions: 'Invoice INV-2048\nSecond line', acceptedNotes: 'Approved for the current period.', pendingNotes: 'Please quote invoice INV-2048.' })
})

for (const component of ['input', 'textarea', 'error-summary', 'notice', 'form-layouts']) {
  test(`${component} remains accessible in narrow light/dark and resized text @cross-browser`, async ({ page }, testInfo) => {
    const errors: string[] = []
    page.on('pageerror', error => errors.push(error.message))
    await page.setViewportSize({ width: 390, height: 1000 })
    await page.goto(`/components/${component}`)
    const preview = page.locator(`#components-${component}-panel-preview`)
    await expect(preview).toBeVisible()
    for (const theme of ['Light', 'Dark']) {
      await page.getByRole('button', { name: 'Choose color theme' }).click()
      await page.getByRole('menuitemradio', { name: theme, exact: true }).click()
      expect((await new AxeBuilder({ page }).include('.docs-gallery-layout').analyze()).violations).toEqual([])
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
