import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

test('Field composes custom controls, relationships, fieldsets, and responsive rows @cross-browser', async ({ page }) => {
  await page.goto('/components/field')
  const custom = page.locator('#components-field-panel-preview')
  const amount = custom.getByRole('spinbutton', { name: 'Adjustment amount', exact: true })
  await expect(amount).toHaveAttribute('aria-describedby', 'adjustment-amount-description')
  await amount.fill('125.50')
  expect(await amount.evaluate(element => {
    const form = document.createElement('form')
    form.append(element.cloneNode(true))
    return new FormData(form).get('adjustmentAmount')
  })).toBe('125.50')

  const responsive = page.locator('#components-field-responsive-panel-preview')
  const invalid = responsive.getByRole('textbox', { name: 'Tax reference', exact: true })
  await expect(invalid).toHaveAttribute('aria-invalid', 'true')
  await expect(invalid).toHaveAttribute('aria-describedby', 'tax-reference-validation')
  const group = page.locator('#components-field-group-panel-preview').getByRole('group', { name: 'Delivery preferences' })
  await expect(group.getByRole('checkbox')).toHaveCount(2)
  expect((await new AxeBuilder({ page }).include('#components-field-panel-preview').analyze()).violations).toEqual([])

  await page.setViewportSize({ width: 320, height: 844 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
})

test('Input group keeps addons out of values and preserves textarea and field states @cross-browser', async ({ page }) => {
  await page.goto('/components/input-group')
  const currency = page.locator('#components-input-group-panel-preview')
  const amount = currency.getByRole('textbox', { name: 'Budget', exact: true })
  await expect(currency.locator('.fve-input-group [aria-hidden="true"]')).toHaveCount(2)
  await amount.fill('1400')
  expect(await amount.evaluate(element => {
    const form = document.createElement('form')
    form.append(element.cloneNode(true))
    return [...new FormData(form).entries()]
  })).toEqual([['budgetAmount', '1400']])

  const emptyAddons = page.locator('#components-input-group-default-panel-preview .fve-input-group')
  const emptyInput = emptyAddons.getByRole('textbox', { name: 'Account reference', exact: true })
  expect((await emptyInput.boundingBox())!.width / (await emptyAddons.boundingBox())!.width).toBeGreaterThan(.9)
  const textarea = page.locator('#components-input-group-textarea-panel-preview')
  await expect(textarea.getByRole('textbox', { name: 'Message', exact: true })).toHaveValue('The September close is ready for review.')
  const send = textarea.getByRole('button', { name: 'Send', exact: true })
  await expect(send).toBeVisible()
  const messageBox = (await textarea.getByRole('textbox', { name: 'Message', exact: true }).boundingBox())!
  expect((await send.boundingBox())!.y).toBeGreaterThanOrEqual(messageBox.y + messageBox.height)
  const invalid = page.locator('#components-input-group-validation-panel-preview').getByRole('textbox', { name: 'Routing number', exact: true })
  await expect(invalid).toHaveAttribute('aria-invalid', 'true')
  const pending = page.locator('#components-input-group-pending-panel-preview').getByRole('textbox', { name: 'Domain', exact: true })
  await expect(pending).toHaveAttribute('readonly', '')
  await expect(pending).toHaveAttribute('aria-busy', 'true')
  expect((await new AxeBuilder({ page }).include('#components-input-group-panel-preview').analyze()).violations).toEqual([])
})
