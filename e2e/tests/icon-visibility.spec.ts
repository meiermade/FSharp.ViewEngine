import { test, expect } from '../fixture'
import AxeBuilder from '@axe-core/playwright'

test('Icon families retain their artwork and square native dimensions @cross-browser', async ({ page }) => {
  await page.goto('/components/icon')
  const example = page.locator('#components-icon-families-panel-preview')
  for (const [family, dimension] of [['outline', 24], ['solid', 24], ['mini', 20], ['micro', 16]] as const) {
    const icon = example.locator(`svg[data-fve-icon-variant="${family}"]`)
    await expect(icon).toHaveAttribute('viewBox', `0 0 ${dimension} ${dimension}`)
    await expect(icon).toHaveCSS('width', `${dimension}px`)
    await expect(icon).toHaveCSS('height', `${dimension}px`)
    await expect(icon).toHaveAttribute('aria-hidden', 'true')
  }
  const overrides = page.locator('#components-icon-sizes-panel-preview svg')
  for (const [index, size] of [12, 16, 20, 24, 32].entries()) {
    await expect(overrides.nth(index)).toHaveCSS('width', `${size}px`)
    await expect(overrides.nth(index)).toHaveAttribute('viewBox', '0 0 24 24')
  }
  expect((await new AxeBuilder({ page }).include('main').analyze()).violations).toEqual([])
})

test('ordinary icon links follow square content while retaining native navigation @cross-browser', async ({ page }) => {
  await page.goto('/components/link')
  const variants = page.locator('#components-link-icon-variants-panel-preview')
  for (const name of ['Text', 'Solid', 'Soft', 'Outline', 'Ghost']) {
    const link = variants.getByRole('link', { name: `${name} settings`, exact: true })
    const expected = name === 'Text' ? 20 : 32
    await expect(link).toHaveCSS('width', `${expected}px`)
    await expect(link).toHaveCSS('height', `${expected}px`)
    await expect(link.locator('svg')).toHaveCSS('width', '20px')
    await expect(link).toHaveAttribute('href', '/examples/application/settings')
  }
  const sizes = page.locator('#components-link-icon-sizes-panel-preview')
  for (const [name, dimension] of [['Extra small', 24], ['Small', 28], ['Medium', 32], ['Large', 36], ['Extra large', 40]] as const) {
    const link = sizes.getByRole('link', { name: `${name} settings`, exact: true })
    await expect(link).toHaveCSS('width', `${dimension}px`)
    await expect(link).toHaveCSS('height', `${dimension}px`)
  }
  const destination = variants.getByRole('link', { name: 'Ghost settings', exact: true })
  await destination.focus()
  await page.keyboard.press('Enter')
  await expect(page).toHaveURL(/\/examples\/application\/settings$/)
  await expect(page.getByRole('heading', { name: 'General', level: 1, exact: true })).toBeVisible()
})

test('Button data-show settles, toggles and survives a server morph without taking over host visibility @cross-browser', async ({ page }) => {
  const raw = await (await page.request.get('/components/button')).text()
  await page.goto('/components/button')
  const example = page.locator('#components-button-visibility-panel-preview')
  const host = example.locator('fve-button[data-show]')
  const action = host.locator('button')
  const toggle = example.getByRole('button', { name: 'Toggle action visibility', exact: true })
  await expect(action).toBeHidden()
  const replacement = await page.evaluate(raw => new DOMParser().parseFromString(raw, 'text/html').querySelector('#components-button-visibility-panel-preview fve-button[data-show]')!.outerHTML, raw)
  await page.route('**/components/button/settled*', route => route.fulfill({ status: 200, contentType: 'text/event-stream', body: `event: datastar-patch-elements\ndata: selector #components-button-visibility-panel-preview fve-button[data-show]\ndata: elements ${replacement}\n\n` }))
  await page.locator('#components-button-basic-panel-preview').getByRole('button').click()
  await expect(action).toBeHidden()
  for (let iteration = 0; iteration < 3; iteration++) {
    await toggle.click()
    await expect(action).toBeVisible()
    await expect(host).toHaveCSS('display', 'contents')
    await action.click()
    await expect(action).toBeHidden()
    await expect(host).toHaveCSS('display', 'none')
  }
  await toggle.focus()
  await page.keyboard.press('Space')
  await expect(action).toBeVisible()
  await action.focus()
  await page.keyboard.press('Enter')
  await expect(action).toBeHidden()
  await expect(toggle).toBeEnabled()
})
