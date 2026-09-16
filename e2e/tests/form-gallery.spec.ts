import { expect, test } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const crossBrowser = { tag: '@cross-browser' }

test('Input gallery teaches single fields with accessible adornments and native clear events', crossBrowser, async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/input')
  const examples = page.locator('[data-docs-example="true"]')
  await expect(examples).toHaveCount(13)
  for (const example of await examples.all()) {
    await expect(example.locator('input:not([type="hidden"])')).toHaveCount(1)
    await expect(example.locator('form, textarea')).toHaveCount(0)
    await expect(example.locator('[data-docs-copy-source]')).not.toContainText('contactFormRegion')
  }
  const website = page.locator('#components-input-prefix').getByRole('textbox', { name: 'Website', exact: true })
  await expect(website).toHaveAccessibleDescription('https://')
  await website.fill('example.com')
  const price = page.locator('#components-input-suffix').getByRole('textbox', { name: 'Price', exact: true })
  await expect(price).toHaveAccessibleDescription('USD')
  await expect(price).toHaveAttribute('inputmode', 'decimal')
  await price.fill('12.50')
  await price.focus()
  await expect(price.locator('..')).toHaveCSS('outline-width', '2px')
  const iconField = page.locator('#components-input-icon').getByRole('textbox', { name: 'Email', exact: true })
  await expect(iconField).toHaveAccessibleName('Email')
  await expect(page.locator('#components-input-icon [aria-hidden="true"] svg')).toHaveCount(1)
  const query = page.getByRole('searchbox', { name: 'Search', exact: true })
  const clear = page.getByRole('button', { name: 'Clear Search', exact: true, includeHidden: true })
  await expect(clear).toBeDisabled()
  await query.fill('preview')
  await query.evaluate(input => {
    input.addEventListener('input', () => { input.setAttribute('data-native-input', 'true') })
    input.addEventListener('change', () => { input.setAttribute('data-native-change', 'true') })
  })
  await clear.focus()
  await page.keyboard.press('Enter')
  await expect(query).toBeFocused()
  await expect(query).toHaveValue('')
  await expect(query).toHaveAttribute('data-native-input', 'true')
  await expect(query).toHaveAttribute('data-native-change', 'true')
  await expect(clear).toBeDisabled()
  const values = await page.locator('.docs-gallery-layout').evaluate(gallery => {
    const form = document.createElement('form')
    for (const field of gallery.querySelectorAll('input')) form.append(field.cloneNode(true))
    return Object.fromEntries(new FormData(form))
  })
  expect(values.website).toBe('example.com')
  expect(values.price).toBe('12.50')
  expect(values.memberId).toBe('MEM-2048')
  expect(values.pendingEmail).toBe('alex@example.com')
  expect(values).not.toHaveProperty('disabledEmail')
  expect(errors).toEqual([])
})

for (const [title, prefix, endpoint] of [
  ['Contact details in two columns', 'contact-grid-', '/components/forms/contact/grid'],
  ['Contact details in sections', 'contact-sectioned-', '/components/forms/contact/sectioned'],
]) {
  test(`${title} validates independently and preserves its layout through morphs`, crossBrowser, async ({ page }) => {
    await page.goto('/components/form-layouts')
    const first = page.getByRole('form', { name: 'Contact details', exact: true })
    await first.getByRole('textbox', { name: 'Contact name', exact: true }).fill('Keep this separate form')
    const form = page.getByRole('form', { name: title, exact: true })
    await form.getByRole('textbox', { name: 'Email address', exact: true }).fill('invalid')
    await form.getByRole('textbox', { name: 'Notes', exact: true }).fill('Keep this note')
    const response = page.waitForResponse(response => new URL(response.url()).pathname === endpoint)
    await form.getByRole('button', { name: 'Validate details', exact: true }).click()
    expect((await response).status()).toBe(200)
    const summary = page.locator(`[id="${prefix}errors"]`)
    await expect(summary).toBeFocused()
    await expect(summary.getByRole('link')).toHaveCount(2)
    await summary.getByRole('link', { name: 'Email address: Enter a valid email address.', exact: true }).click()
    const email = form.getByRole('textbox', { name: 'Email address', exact: true })
    await expect(email).toBeFocused()
    await email.fill('alex@example.com')
    await form.getByRole('textbox', { name: 'Contact name', exact: true }).fill('Alex Morgan')
    await form.getByRole('button', { name: 'Validate details', exact: true }).click()
    await expect(page.locator(`[id="${prefix}result"]`)).toContainText('This example does not save your data.')
    await expect(summary).toHaveCount(0)
    await expect(form.getByRole('textbox', { name: 'Notes', exact: true })).toHaveValue('Keep this note')
    await expect(first.getByRole('textbox', { name: 'Contact name', exact: true })).toHaveValue('Keep this separate form')
    expect((await new AxeBuilder({ page }).include(`[id="components-${prefix}region"]`).analyze()).violations).toEqual([])
  })
}

for (const [width, scale] of [[1440, 1], [390, 1], [320, 2]]) {
  test(`form galleries keep focused layouts and readable fields at ${width}px ${scale}x`, crossBrowser, async ({ page }, testInfo) => {
    for (const slug of ['input', 'textarea', 'select', 'checkbox', 'switch', 'radio-group', 'form-layouts']) {
      await page.setViewportSize({ width, height: 1000 })
      await page.goto(`/components/${slug}`)
      for (const dark of [false, true]) {
        await page.evaluate(({ dark, scale }) => {
          document.documentElement.classList.toggle('dark', dark)
          document.documentElement.style.fontSize = `${100 * scale}%`
        }, { dark, scale })
        await expect.poll(() => page.evaluate(() => document.getAnimations().filter(a => a instanceof CSSTransition && a.playState === 'running').length)).toBe(0)
        const gallery = page.locator('.docs-gallery-layout')
        expect((await new AxeBuilder({ page }).include('.docs-gallery-layout').analyze()).violations, `${slug}/${dark}`).toEqual([])
        await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
        for (const input of await gallery.locator('input:not([type="hidden"]), textarea').all()) {
          if (!await input.isVisible()) continue
          const box = (await input.boundingBox())!
          expect(box.x).toBeGreaterThanOrEqual(0)
          expect(box.x + box.width).toBeLessThanOrEqual(width + 1)
          if (slug === 'input' || slug === 'textarea') await expect(input).toHaveCSS('font-size', `${16 * scale}px`)
        }
        if (slug === 'input' || slug === 'form-layouts') {
          await page.screenshot({ path: testInfo.outputPath(`${slug}-${dark ? 'dark' : 'light'}-${width}-${scale}x.png`) })
        }
      }
    }
  })
}
