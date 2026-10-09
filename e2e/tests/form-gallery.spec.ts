import { expect, test } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const crossBrowser = { tag: '@cross-browser' }

test('Input gallery teaches single fields with accessible adornments and native clear events', crossBrowser, async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/input')
  const examples = page.locator('[data-docs-example="true"]')
  await expect(examples).toHaveCount(12)
  for (const example of await examples.filter({ hasNot: page.getByRole('searchbox', { name: 'Compact search', exact: true }) }).all()) {
    await expect(example.locator('input:not([type="hidden"])')).toHaveCount(1)
    await expect(example.locator('form, textarea')).toHaveCount(0)
  }
  const website = page.locator('#components-input-prefix').getByRole('textbox', { name: 'Website', exact: true })
  await expect(website).toHaveAccessibleDescription('https://')
  await website.fill('fve.meiermade.com')
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
  await expect(clear).toBeHidden()
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
  await expect(clear).toBeHidden()
  const values = await page.locator('[data-docs-layout="gallery"]').evaluate(gallery => {
    const form = document.createElement('form')
    for (const field of gallery.querySelectorAll('input')) form.append(field.cloneNode(true))
    return Object.fromEntries(new FormData(form))
  })
  expect(values.website).toBe('fve.meiermade.com')
  expect(values.price).toBe('12.50')
  expect(values.pendingEmail).toBe('alex@fve.meiermade.com')
  expect(values).not.toHaveProperty('disabledEmail')
  expect(errors).toEqual([])
})

const representativeGalleryLayouts = [
  ['input', 1440, 1, false],
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
      if (slug === 'input' || slug === 'textarea') {
        const compact = await input.evaluate(element => element.closest('.fve-control-small, .fve-control-medium, .fve-control-large')?.classList.contains('fve-control-small') ?? false)
        await expect(input).toHaveCSS('font-size', `${(compact ? 14 : 16) * scale}px`)
      }
    }
  })
}
