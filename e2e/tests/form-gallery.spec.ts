import { expect, test } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const crossBrowser = { tag: '@cross-browser' }

test('native validation waits for blur then clears errors and accessibility state while editing', crossBrowser, async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/input')
  const gallery = page.locator('[data-docs-layout="gallery"]')
  await expect(gallery.locator('.fve-input-group, [data-fve-input-frame], .fve-search')).toHaveCount(0)
  await expect(page.getByRole('heading', { name: 'Pending', exact: true })).toHaveCount(0)
  const query = page.getByRole('searchbox', { name: 'Search', exact: true })
  await expect(query).toHaveAttribute('type', 'search')
  await expect(page.getByRole('button', { name: 'Clear Search', exact: true })).toHaveCount(0)
  await query.fill('preview')
  const required = page.locator('#components-input-required-panel-preview')
  const email = required.getByRole('textbox', { name: 'Email (required)', exact: true })
  await email.evaluate(input => {
    const form = document.createElement('form')
    input.before(form)
    form.append(input)
  })
  const error = required.getByRole('alert')
  const border = await email.evaluate(el => getComputedStyle(el).borderColor)
  expect(await email.evaluate((el: HTMLInputElement) => el.validity.valueMissing)).toBe(true)
  await expect(required.getByRole('button')).toHaveCount(0)
  await expect(error).toBeHidden()
  await expect(email).not.toHaveAttribute('aria-invalid')
  await email.fill('not-an-email')
  await expect(error).toBeHidden()
  await expect(email).not.toHaveAttribute('aria-invalid')
  await expect(email).toHaveCSS('border-color', border)
  await email.fill('')
  await email.blur()
  await expect(error).toHaveText('Enter a valid email address.')
  await expect(email).toHaveAttribute('aria-invalid', 'true')
  await expect(email).toHaveAttribute('aria-describedby', 'required-email-help required-email-error')
  expect(await email.evaluate(el => getComputedStyle(el).borderColor)).not.toBe(border)
  await email.fill('not-an-email')
  await expect(error).toBeVisible()
  await email.fill('alex@fve.meiermade.com')
  await expect(error).toBeHidden()
  await expect(email).not.toHaveAttribute('aria-invalid')
  await expect(email).toHaveAttribute('aria-describedby', 'required-email-help')
  await expect(email).toHaveCSS('border-color', border)
  await email.fill('invalid-again')
  await expect(error).toBeVisible()
  await email.evaluate((input: HTMLInputElement) => input.form!.reset())
  await expect(email).toHaveValue('')
  await expect(error).toBeHidden()
  await expect(email).not.toHaveAttribute('aria-invalid')
  await email.evaluate((input: HTMLInputElement) => input.reportValidity())
  await expect(error).toBeVisible() // Native invalid submission uses the same feedback.

  const correcting = page.locator('#components-input-validation-panel-preview')
  const existing = correcting.getByRole('textbox', { name: 'Email', exact: true })
  await expect(correcting.getByRole('alert')).toBeVisible()
  await existing.fill('andy@meiermade.com')
  await expect(correcting.getByRole('alert')).toBeHidden()
  await expect(existing).not.toHaveAttribute('aria-invalid')
  await expect(existing).not.toHaveAttribute('aria-describedby')
  await existing.fill('invalid')
  await expect(correcting.getByRole('alert')).toBeVisible()
  await existing.fill('') // This field is optional: an empty value is valid.
  await expect(correcting.getByRole('alert')).toBeHidden()
  const values = await gallery.evaluate(gallery => {
    const form = document.createElement('form')
    for (const field of gallery.querySelectorAll('input')) form.append(field.cloneNode(true))
    return Object.fromEntries(new FormData(form))
  })
  expect(values.readOnlyEmail).toBe('alex@fve.meiermade.com')
  expect(values).not.toHaveProperty('disabledEmail')
  expect(errors).toEqual([])
})

test('required validation and native form submission work without JavaScript', async ({ browser, baseURL }) => {
  const context = await browser.newContext({ javaScriptEnabled: false, baseURL })
  try {
    const page = await context.newPage()
    await page.goto('/components/input')
    const preview = page.locator('#components-input-required-panel-preview')
    const email = preview.getByRole('textbox', { name: 'Email (required)', exact: true })
    await email.evaluate((input: HTMLInputElement) => {
      const form = document.createElement('form')
      form.action = '/components/input'
      form.method = 'get'
      input.before(form)
      form.append(input)
      const submit = document.createElement('button')
      submit.type = 'submit'
      submit.textContent = 'Submit'
      form.append(submit)
    })
    const submit = preview.getByRole('button', { name: 'Submit', exact: true })
    await submit.click()
    await expect(page).toHaveURL('/components/input')
    expect(await email.evaluate((el: HTMLInputElement) => el.validity.valueMissing)).toBe(true)
    await email.fill('alex@fve.meiermade.com')
    await submit.click()
    await expect(page).toHaveURL(/\/components\/input\?requiredEmail=alex%40fve.meiermade.com$/)
  } finally {
    await context.close()
  }
})

const representativeGalleryLayouts = [
  ['input', 1440, 1, false],
  ['input-group', 320, 2, true],
  ['textarea', 390, 1, true],
  ['select', 320, 2, true],
  ['checkbox', 390, 1, false],
  ['switch', 1440, 1, true],
  ['radio-group', 320, 2, false],
] as const

for (const [slug, width, scale, dark] of representativeGalleryLayouts) {
  test(`${slug} gallery keeps a representative focused layout at ${width}px ${scale}x`, async ({ page }) => {
    await page.setViewportSize({ width, height: 1000 })
    await page.goto(`/components/${slug}`)
    await page.evaluate(({ dark, scale }) => {
      document.documentElement.classList.toggle('dark', dark)
      document.documentElement.style.fontSize = `${100 * scale}%`
    }, { dark, scale })
    await expect.poll(() => page.evaluate(() => document.getAnimations().filter(a => a instanceof CSSTransition && a.playState === 'running').length)).toBe(0)
    const gallery = page.locator('[data-docs-layout="gallery"]')
    expect((await new AxeBuilder({ page }).include('[data-docs-layout="gallery"]').analyze()).violations, `${slug}/${dark}`).toEqual([])
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
    for (const input of await gallery.locator('input:not([type="hidden"]), textarea').all()) {
      if (!await input.isVisible()) continue
      const box = await input.boundingBox()
      expect(box, `${slug} input must retain a measurable box`).not.toBeNull()
      expect(box!.x).toBeGreaterThanOrEqual(0)
      expect(box!.x + box!.width).toBeLessThanOrEqual(width + 1)
      if (slug === 'input-group') expect(box!.width).toBeGreaterThan(40)
      if (slug === 'input' || slug === 'textarea') {
        const extraSmall = await input.evaluate(element => element.closest('.fve-control-extra-small, .fve-control-small, .fve-control-medium, .fve-control-large, .fve-control-extra-large')?.classList.contains('fve-control-extra-small') ?? false)
        await expect(input).toHaveCSS('font-size', `${(extraSmall ? 12 : 14) * scale}px`)
      }
    }
  })
}
