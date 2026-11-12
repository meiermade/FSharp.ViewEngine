import { test, expect } from '../fixture'
import AxeBuilder from '@axe-core/playwright'

test('example toolbar actions share color and scale while preserving link and menu semantics', async ({ page }) => {
  for (const colorScheme of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme })
    for (const route of ['application/accounts', 'specification', 'api-documentation']) {
      await page.setViewportSize({ width: 1440, height: 900 })
      await page.goto('/examples/' + route)
      const source = page.getByRole('link', { name: 'View example source on GitHub', exact: true })
      const peer = route.startsWith('application')
        ? page.getByRole('link', { name: 'Settings', exact: true })
        : page.getByRole('button', { name: 'Choose color theme', exact: true })
      await expect(source.locator('svg')).toHaveCSS('width', '20px')
      await expect(source.locator('svg')).toHaveCSS('color', await peer.locator('svg:visible').evaluate(el => getComputedStyle(el).color))
      await expect(source).toHaveAttribute('target', '_blank')
      await expect(source).toHaveAttribute('rel', /noopener/)
      await expect(source).toHaveAttribute('href', /^https:\/\/github\.com\/meiermade\/FSharp\.ViewEngine(?:\/|$)/)
      if (!route.startsWith('application')) {
        await expect(peer.locator('svg:visible')).toHaveAttribute('data-fve-icon-variant', 'mini')
      }
      await page.setViewportSize({ width: 390, height: 844 })
      const navigation = page.getByRole('button', { name: 'Open navigation', exact: true })
      await expect(navigation.locator('svg')).toHaveAttribute('data-fve-icon-variant', 'mini')
      await navigation.click()
      const dialog = page.getByRole('dialog', { name: / navigation$/ })
      await expect(dialog).toBeVisible()
      await expect(dialog.getByRole('button', { name: 'Close navigation', exact: true }).locator('svg')).toHaveAttribute('data-fve-icon-variant', 'mini')
      await page.keyboard.press('Escape')
      await expect(navigation).toBeFocused()
    }
  }
})

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

test('ordinary icon links retain natural padding and native navigation @cross-browser', async ({ page }) => {
  await page.goto('/components/link')
  const variants = page.locator('#components-link-icon-variants-panel-preview')
  for (const name of ['Text', 'Solid', 'Soft', 'Outline', 'Ghost']) {
    const link = variants.getByRole('link', { name: `${name} settings`, exact: true })
    await expect(link).toHaveCSS('width', name === 'Text' ? '20px' : '40px')
    await expect(link).toHaveCSS('height', name === 'Text' ? '20px' : '32px')
    await expect(link.locator('svg')).toHaveCSS('width', '20px')
    await expect(link).toHaveAttribute('href', '/examples/application/settings')
  }
  const sizes = page.locator('#components-link-icon-sizes-panel-preview')
  for (const [name, width, height] of [['Extra small', 32, 24], ['Small', 32, 28], ['Medium', 36, 32], ['Large', 40, 36], ['Extra large', 44, 40]] as const) {
    const link = sizes.getByRole('link', { name: `${name} settings`, exact: true })
    await expect(link).toHaveCSS('width', `${width}px`)
    await expect(link).toHaveCSS('height', `${height}px`)
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
