import { expect, test, type Page } from '@playwright/test'

const productionOrigin = 'https://fsharpviewengine.meiermade.com'

const routes = [
  { path: '/', heading: 'FSharp.ViewEngine' },
  { path: '/installation', heading: 'Installation' },
  { path: '/custom', heading: 'Custom Elements & Attributes' },
  { path: '/usage', heading: 'Usage' },
  { path: '/extensions/alpine', heading: 'Alpine.js' },
  { path: '/extensions/datastar', heading: 'Datastar' },
  { path: '/extensions/htmx', heading: 'HTMX' },
  { path: '/extensions/svg', heading: 'SVG' },
  { path: '/extensions/tailwind-elements', heading: 'Tailwind Plus Elements' },
  { path: '/benchmarks', heading: 'Benchmarks' },
  { path: '/changelog', heading: 'Changelog' },
]

function captureBrowserErrors(page: Page) {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  page.on('console', message => {
    if (message.type() === 'error') errors.push(message.text())
  })
  return errors
}

test.describe('public documentation routes', () => {
  for (const route of routes) {
    test(`GET ${route.path} renders`, async ({ page }) => {
      const browserErrors = captureBrowserErrors(page)
      const response = await page.goto(route.path, { waitUntil: 'domcontentloaded' })

      expect(response?.status(), `${route.path} status`).toBe(200)
      await expect(page.getByRole('heading', { level: 1, name: route.heading, exact: true })).toBeVisible()
      await expect(page.locator('article')).toBeVisible()
      const canonicalURL = route.path === '/' ? productionOrigin : `${productionOrigin}${route.path}`
      await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', canonicalURL)
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
      expect(browserErrors, `${route.path} browser errors`).toEqual([])
    })
  }
})

test('health and pinned application assets are available', async ({ request }) => {
  const health = await request.get('/health')
  expect(health.status()).toBe(200)
  expect(await health.text()).toBe('ok')

  const css = await request.get('/css/output.css')
  expect(css.status()).toBe(200)
  expect(await css.text()).toContain('tailwindcss v4.3.3')

  const alpine = await request.get('/scripts/alpinejs.3.15.12.min.js')
  expect(alpine.status()).toBe(200)
  expect(await alpine.text()).toContain('version:"3.15.12"')
})

test('mobile navigation opens, closes, and does not overflow', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/', { waitUntil: 'domcontentloaded' })

  const drawer = page.locator('[x-show="mobileNavOpen"]')
  await expect(drawer).toBeHidden()
  await page.getByRole('button', { name: 'Open navigation' }).click()
  await expect(drawer).toBeVisible()
  await page.getByRole('button', { name: 'Close navigation' }).click()
  await expect(drawer).toBeHidden()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})

test('benchmark tables remain readable without page overflow on mobile', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/benchmarks', { waitUntil: 'domcontentloaded' })

  const comparison = page.getByRole('figure', { name: 'Build and render comparison' })
  await expect(comparison).toBeVisible()
  await expect(comparison).toContainText('FSharp.ViewEngine')
  await expect(comparison).toContainText('1.35× as long')
  await expect(comparison.locator('[style^="width:"]')).toHaveCount(4)
  await expect(page.getByRole('table')).toHaveCount(7)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})

test('theme selection persists across navigation', async ({ page }) => {
  await page.goto('/', { waitUntil: 'domcontentloaded' })
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('button', { name: 'Dark', exact: true }).click()

  await expect(page.locator('html')).toHaveClass(/dark/)
  expect(await page.evaluate(() => localStorage.getItem('theme'))).toBe('dark')

  await page.goto('/installation', { waitUntil: 'domcontentloaded' })
  await expect(page.locator('html')).toHaveClass(/dark/)
})

test('removed Tailwind documentation route returns 404', async ({ request }) => {
  const response = await request.get('/extensions/tailwind')
  expect(response.status()).toBe(404)
})
