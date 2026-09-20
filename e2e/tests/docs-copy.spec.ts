import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

for (const [surface, route, label] of [
  ['installation', '/docs', 'Copy code'],
  ['article', '/getting-started/first-view', 'Copy code'],
  ['API example', '/docs/components/api-reference', 'Copy Create customer'],
  ['gallery', '/components/page-examples/messaging', 'Copy Messaging workspace code'],
]) {
  test(`${surface} uses the shared copy icon and announces clipboard results @cross-browser`, async ({ page }) => {
    const errors: string[] = []
    page.on('pageerror', error => errors.push(error.message))
    await page.goto(route)
    if (surface === 'gallery') await page.getByRole('tab', { name: 'Code', exact: true }).first().click()
    if (surface === 'installation') {
      await expect(page.getByText('This local candidate consolidates the former Docs package.', { exact: false })).toHaveCount(0)
    }
    await page.evaluate(() => {
      Object.defineProperty(navigator, 'clipboard', { configurable: true, value: {
        writeText: async (value: string) => {
          if ((window as any).__denyCopy) throw new Error('Clipboard denied')
          ;(window as any).__copiedSource = value
        },
      } })
    })
    const copy = page.getByRole('button', { name: label, exact: true }).first()
    const icon = (state: string) => copy.locator(`[data-docs-copy-icon="${state}"]`)
    const source = await copy.evaluate(button => button.closest('.docs-copyable-code')!.querySelector('[data-docs-copy-source]')!.textContent)
    await expect(copy).toHaveAttribute('title', label)
    await expect(copy).toHaveText('')
    await expect(copy).toHaveCSS('width', '32px')
    await expect(copy).toHaveCSS('height', '32px')
    await expect(icon('copy')).toBeVisible()
    await expect(icon('success')).toBeHidden()
    await expect(icon('error')).toBeHidden()
    await copy.focus()
    await page.keyboard.press('Enter')
    await expect(icon('success')).toBeVisible()
    await expect(icon('copy')).toBeHidden()
    await expect(copy.getByRole('status')).toHaveText('Copied')
    await expect(copy).toHaveAttribute('title', 'Copied')
    await expect(copy).toBeFocused()
    expect(await page.evaluate(() => (window as any).__copiedSource)).toBe(source)
    await page.evaluate(() => { (window as any).__denyCopy = true })
    await copy.click()
    await expect(icon('error')).toBeVisible()
    await expect(icon('success')).toBeHidden()
    await expect(copy.getByRole('status')).toHaveText('Copy failed')
    await expect(copy).toHaveAttribute('title', 'Copy failed')
    await expect(copy).toHaveAttribute('title', label)
    await expect(icon('copy')).toBeVisible()
    await expect(copy.getByRole('status')).toHaveText('')
    expect(errors).toEqual([])
  })
}

for (const [width, dark] of [[1440, false], [390, true]] as const) {
  test(`installation copy icons remain accessible at ${width}px @cross-browser`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 900 })
    await page.emulateMedia({ colorScheme: dark ? 'dark' : 'light' })
    await page.goto('/docs')
    const installation = page.locator('section').filter({ has: page.getByRole('heading', { name: 'Installation', level: 2, exact: true }) })
    await expect(installation.getByRole('button', { name: 'Copy code', exact: true })).toHaveCount(2)
    expect((await new AxeBuilder({ page }).include('section:has(> #installation)').analyze()).violations).toEqual([])
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    await installation.screenshot({ path: testInfo.outputPath(`installation-copy-${width}.png`) })
  })
}
