import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

test('Select retains its native-label-derived listbox name after validation and accepted server morphs @cross-browser', async ({ page }) => {
  await page.goto('/components/select')
  const region = page.locator('#components-select-form-region')
  const trigger = region.getByRole('combobox', { name: 'Status', exact: true })
  const submit = region.getByRole('button', { name: 'Validate status', exact: true })
  await submit.click()
  await expect(region.getByRole('alert')).toHaveText('Choose an available status.')
  await trigger.press('Enter')
  await expect(region.getByRole('listbox', { name: 'Status', exact: true })).toBeVisible()
  await region.getByRole('option', { name: 'Pending', exact: true }).click()
  await submit.click()
  await expect(region.locator('#components-select-result')).toHaveText('Accepted status: Pending.')
  await trigger.press('Alt+ArrowDown')
  await expect(region.getByRole('listbox', { name: 'Status', exact: true })).toBeVisible()
  await trigger.press('Escape')
  await expect(trigger).toBeFocused()
})

test('Field composes custom controls, relationships, fieldsets, and responsive rows @cross-browser', async ({ page }) => {
  await page.goto('/components/field')
  const custom = page.locator('#components-field-panel-preview')
  const amount = custom.getByRole('spinbutton', { name: 'Quantity', exact: true })
  await expect(amount).toHaveAttribute('aria-describedby', 'item-quantity-description')
  await amount.fill('125.50')
  expect(await amount.evaluate(element => {
    const form = document.createElement('form')
    form.append(element.cloneNode(true))
    return new FormData(form).get('quantity')
  })).toBe('125.50')

  const responsive = page.locator('#components-field-responsive-panel-preview')
  const invalid = responsive.getByRole('textbox', { name: 'Reference', exact: true })
  await expect(invalid).toHaveAttribute('aria-invalid', 'true')
  await expect(invalid).toHaveAttribute('aria-describedby', 'item-reference-validation')
  const group = page.locator('#components-field-group-panel-preview').getByRole('group', { name: 'Delivery preferences' })
  await expect(group.getByRole('checkbox')).toHaveCount(2)
  for (const { width, fontSize } of [{ width: 1440, fontSize: '100%' }, { width: 1440, fontSize: '200%' }, { width: 320, fontSize: '200%' }]) {
    await page.setViewportSize({ width, height: 1000 })
    await page.evaluate(size => { document.documentElement.style.fontSize = size }, fontSize)
    const spacing = await group.evaluate(fieldset => {
      const legend = fieldset.querySelector('legend')!.getBoundingClientRect()
      const description = fieldset.querySelector(':scope > p')!.getBoundingClientRect()
      const controls = fieldset.querySelector(':scope > div')!.getBoundingClientRect()
      const rem = parseFloat(getComputedStyle(document.documentElement).fontSize)
      return {
        title: (description.top - legend.bottom) / rem,
        controls: (controls.top - description.bottom) / rem,
        checkboxes: [...fieldset.querySelectorAll<HTMLInputElement>('input[type="checkbox"]')].map(input => {
          const box = input.getBoundingClientRect()
          const label = input.labels![0].getBoundingClientRect()
          return { width: box.width / rem, height: box.height / rem, inline: label.left > box.right }
        })
      }
    })
    expect(spacing.title).toBeGreaterThanOrEqual(.25)
    expect(spacing.controls).toBeGreaterThanOrEqual(.5)
    for (const checkbox of spacing.checkboxes) {
      expect(checkbox.width).toBeCloseTo(1.25, 2)
      expect(checkbox.height).toBeCloseTo(1.25, 2)
      expect(checkbox.inline).toBe(true)
    }
  }
  await page.evaluate(() => { document.documentElement.style.fontSize = '100%' })
  expect((await new AxeBuilder({ page }).include('#components-field-panel-preview').analyze()).violations).toEqual([])

  await page.setViewportSize({ width: 320, height: 844 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)

  await page.goto('/components/field-group')
  const disabled = page.getByRole('group', { name: 'Archived preferences', exact: true })
  await expect(disabled.getByRole('checkbox')).toBeDisabled()
  expect(await disabled.evaluate(fieldset => {
    const form = document.createElement('form')
    form.append(fieldset.cloneNode(true))
    return [...new FormData(form).entries()]
  })).toEqual([])
})

test('Checkbox uses native checked and mixed state and keeps read-only values successful @cross-browser', async ({ page }) => {
  await page.goto('/components/checkbox')
  const mixed = page.getByRole('checkbox', { name: 'Select all items', exact: true })
  await expect.poll(() => mixed.evaluate(element => (element as HTMLInputElement).indeterminate)).toBe(true)
  await mixed.press('Space')
  await expect(mixed).toBeChecked()
  expect(await mixed.evaluate(element => (element as HTMLInputElement).indeterminate)).toBe(false)
  const form = page.locator('#components-checkbox-form-region')
  const confirmation = form.getByRole('checkbox', { name: 'Confirm review', exact: true })
  await form.getByRole('button', { name: 'Validate confirmation' }).click()
  await expect(form.getByRole('alert')).toHaveText('Confirm the review.')
  await confirmation.check()
  await form.getByRole('button', { name: 'Validate confirmation' }).click()
  await expect(form.getByRole('status')).toHaveText('Review confirmed.')
  await expect(confirmation).toBeChecked()
  const pending = page.getByRole('checkbox', { name: 'Saving review', exact: true })
  await expect(pending).toHaveAttribute('readonly', '')
  await pending.press('Space')
  await expect(pending).toBeChecked()
  expect(await pending.evaluate(element => {
    const form = document.createElement('form')
    form.append(element.cloneNode(true))
    return new FormData(form).getAll('confirmArchivedReview')
  })).toEqual(['true'])
})

test('Select labels target the actual control and native fallback submits once without JavaScript', async ({ page, browser }) => {
  await page.goto('/components/select?fixtures=1')
  const control = page.getByRole('combobox', { name: 'Update frequency', exact: true }).and(page.locator('#nativeFrequency'))
  await expect(control).toHaveAttribute('id', 'nativeFrequency')
  await page.locator('label[for="nativeFrequency"]').click()
  await expect(control).toBeFocused()

  const context = await browser.newContext({ javaScriptEnabled: false })
  try {
    const nativePage = await context.newPage()
    await nativePage.goto(page.url())
    const native = nativePage.getByRole('combobox', { name: 'Update frequency', exact: true }).and(nativePage.locator('#nativeFrequency'))
    await expect(native).toHaveAttribute('id', 'nativeFrequency')
    await native.selectOption('daily')
    await expect(native).toHaveValue('daily')
    expect(await native.evaluate(element => {
      const root = element.closest('[data-fve-select]')!
      const form = document.createElement('form')
      root.replaceWith(form)
      form.append(root)
      return new FormData(form).getAll('nativeFrequency')
    })).toEqual(['daily'])
  } finally {
    await context.close()
  }
})

test('Input group keeps addons out of values and preserves textarea and field states @cross-browser', async ({ page }, testInfo) => {
  await page.goto('/components/input-group')
  const currency = page.locator('#components-input-group-panel-preview')
  const amount = currency.getByRole('textbox', { name: 'Weight', exact: true })
  await expect(currency.locator('.fve-input-group [aria-hidden="true"]')).toHaveCount(2)
  await amount.fill('1400')
  expect(await amount.evaluate(element => {
    const form = document.createElement('form')
    form.append(element.cloneNode(true))
    return [...new FormData(form).entries()]
  })).toEqual([['packageWeight', '1400']])

  await expect(amount).toHaveCSS('border-top-width', '0px')
  for (const direction of ['ltr', 'rtl']) {
    await currency.locator('.fve-input-group').evaluate((el, dir) => el.setAttribute('dir', dir), direction)
    const control = (await amount.boundingBox())!
    const start = (await currency.locator('[data-fve-input-group-addon="inline-start"]').boundingBox())!
    const end = (await currency.locator('[data-fve-input-group-addon="inline-end"]').boundingBox())!
    expect(direction === 'ltr' ? start.x < control.x && end.x > control.x : start.x > control.x && end.x < control.x).toBe(true)
  }
  const prefix = page.locator('#components-input-group-default-panel-preview').getByRole('textbox', { name: 'Website', exact: true })
  await expect(prefix).toHaveAccessibleDescription('https://')
  const suffix = page.locator('#components-input-group-suffix-panel-preview').getByRole('textbox', { name: 'Distance', exact: true })
  await expect(suffix).toHaveAccessibleDescription('km')
  const help = page.locator('#components-input-group-help-panel-preview')
  await help.getByRole('textbox', { name: 'Reference with help', exact: true }).focus()
  // macOS WebKit uses Option+Tab to include buttons in native focus navigation.
  await page.keyboard.press(testInfo.project.name === 'webkit' ? 'Alt+Tab' : 'Tab')
  await expect(help.getByRole('button', { name: 'Reference help', exact: true })).toBeFocused()
  await page.keyboard.press('Enter')
  const popupInput = help.getByRole('textbox', { name: 'Maximum length', exact: true })
  await expect(popupInput).toBeVisible()
  await expect(popupInput).toHaveCSS('border-top-width', '1px')
  await popupInput.focus()
  await page.keyboard.press('Escape')
  const textarea = page.locator('#components-input-group-textarea-panel-preview')
  await expect(textarea.getByRole('textbox', { name: 'Message', exact: true })).toHaveValue('The latest project notes are ready for review.')
  const clear = textarea.getByRole('button', { name: 'Clear message', exact: true })
  await expect(clear).toBeVisible()
  const messageBox = (await textarea.getByRole('textbox', { name: 'Message', exact: true }).boundingBox())!
  expect((await clear.boundingBox())!.y).toBeGreaterThanOrEqual(messageBox.y + messageBox.height)
  const header = (await textarea.locator('[data-fve-input-group-addon="block-start"]').boundingBox())!
  expect(header.y + header.height).toBeLessThanOrEqual(messageBox.y + 1)
  await clear.click()
  await expect(textarea.getByRole('textbox', { name: 'Message', exact: true })).toHaveValue('')
  await expect(textarea.getByRole('textbox', { name: 'Message', exact: true })).toBeFocused()
  const validation = page.locator('#components-input-group-validation-panel-preview')
  const invalid = validation.getByRole('textbox', { name: 'Reference', exact: true })
  await expect(invalid).toHaveAttribute('aria-invalid', 'true')
  const invalidBorder = await invalid.locator('..').evaluate(el => getComputedStyle(el).borderColor)
  await invalid.fill('123456789')
  await expect(invalid).not.toHaveAttribute('aria-invalid')
  await expect(invalid).not.toHaveAttribute('aria-describedby')
  await expect(validation.getByRole('alert')).toBeHidden()
  expect(await invalid.locator('..').evaluate(el => getComputedStyle(el).borderColor)).not.toBe(invalidBorder)
  await invalid.fill('123')
  await expect(invalid).toHaveAttribute('aria-invalid', 'true')
  await expect(validation.getByRole('alert')).toBeVisible()
  const check = page.locator('#components-input-group-check-panel-preview')
  const email = check.getByRole('textbox', { name: 'Email to check', exact: true })
  const button = check.getByRole('button', { name: 'Check email', exact: true })
  for (let run = 0; run < 2; run++) {
    await button.click()
    await expect(email).toHaveAttribute('readonly', '')
    await expect(email).toHaveAttribute('aria-busy', 'true')
    await expect(check.getByRole('status', { name: 'Checking email format' })).toBeVisible()
    await expect(check.locator('#input-group-check-result')).toHaveText('Email format is valid.')
    await expect(email).toBeEditable()
    await expect(button).toBeEnabled()
  }
  const group = email.locator('..')
  const neutral = await group.evaluate(el => getComputedStyle(el).borderColor)
  await email.fill('not-an-email')
  await button.click()
  expect(await email.evaluate(el => el.matches(':user-invalid'))).toBe(true)
  expect(await group.evaluate(el => getComputedStyle(el).borderColor)).not.toBe(neutral)
  await expect(check.locator('#input-group-check-result')).toBeEmpty()
  expect((await new AxeBuilder({ page }).include('#components-input-group-panel-preview').analyze()).violations).toEqual([])
})
