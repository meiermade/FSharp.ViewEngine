import { test, expect } from '../fixture'
import AxeBuilder from '@axe-core/playwright'

test('Marketing is a standalone Geldos example with connected product navigation @cross-browser', async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/examples')
  const opened = page.waitForEvent('popup')
  await page.getByRole('link', { name: 'Marketing example (opens in a new tab)', exact: true }).click()
  const site = await opened
  site.on('pageerror', error => errors.push(error.message))
  await expect(site.getByRole('heading', { name: 'Your financial operating system', level: 1, exact: true })).toBeVisible()
  await expect(site.locator('[data-example-marketing]')).toHaveAttribute('data-example-marketing', 'geldos')
  await expect(site.getByRole('img', { name: /Geldos showing account balances/ })).toBeVisible()
  expect(await site.getByRole('img', { name: /Geldos showing account balances/ }).evaluate((img: HTMLImageElement) => img.naturalWidth)).toBeGreaterThan(0)
  await expect(site.getByRole('banner').getByRole('link', { name: 'Sign up', exact: true })).toHaveAttribute('href', '/examples/marketing/signup')
  await expect(site.locator('a[href^="/examples/application"]')).toHaveCount(0)
  await site.getByRole('main').getByRole('link', { name: 'Sign up', exact: true }).first().click()
  await expect(site).toHaveURL(/\/examples\/marketing\/signup$/)
  await expect(site.getByRole('heading', { name: 'Sign up for Geldos', exact: true })).toBeVisible()
  await expect(site.getByRole('main').locator('form')).toHaveCount(0)
  await site.getByRole('main').getByRole('link', { name: 'Back to Geldos', exact: true }).click()
  await site.getByRole('banner').getByRole('link', { name: 'Features', exact: true }).click()
  await expect(site).toHaveURL(/\/examples\/marketing\/features$/)
  await expect(site.getByRole('heading', { name: 'One record. A clearer picture.', exact: true })).toBeVisible()
  await site.getByRole('banner').getByRole('link', { name: 'Pricing', exact: true }).click()
  await expect(site.getByRole('heading', { name: 'Hosted for you, or hosted by you.', exact: true })).toBeVisible()
  await expect(site.getByRole('heading', { name: 'Self-hosted', exact: true })).toBeVisible()
  await site.getByText('Can I run it on my own infrastructure?', { exact: true }).click()
  await expect(site.getByText(/Self-hosting gives you control/)).toBeVisible()
  await expect(site.locator('a[href^="/examples/application"]')).toHaveCount(0)
  await site.getByRole('banner').getByRole('link', { name: 'Log in', exact: true }).click()
  await expect(site).toHaveURL(/\/examples\/marketing\/login$/)
  await expect(site.getByRole('heading', { name: 'Log in to Geldos', exact: true })).toBeVisible()
  await expect(site.getByRole('main').locator('form')).toHaveCount(0)
  expect(errors).toEqual([])
  await site.close()
})

test('Marketing product screenshot follows the chosen theme without changing its geometry @cross-browser', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'light' })
  await page.goto('/examples/marketing')
  await page.evaluate(() => document.fonts.ready)
  const image = page.getByRole('img', { name: /Geldos showing account balances/ })
  let geometry: { width: number, height: number } | undefined
  for (const mode of ['dark', 'light', 'dark']) {
    await page.evaluate(mode => (window as any).fveColorMode.set(mode), mode)
    await expect(image).toHaveCount(1)
    await expect(image).toHaveAttribute('src', `/examples/assets/images/geldos-overview${mode === 'dark' ? '-dark' : ''}.png`)
    await expect.poll(() => image.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth === 1440 && el.naturalHeight === 960)).toBe(true)
    const box = (await image.boundingBox())!
    const current = { width: box.width, height: box.height }
    if (geometry) expect(current).toEqual(geometry)
    geometry = current
  }
})

test('Account detail uses shared section rhythm, paginated records and chart date navigation @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1100 })
  await page.goto('/examples/application/accounts/101')
  const sections = page.locator('main [data-fve-section-stack] > section')
  await expect(sections).toHaveCount(4)
  expect(await sections.first().getAttribute('aria-label')).toBe('Details')
  await expect(page.getByRole('region', { name: 'Details', exact: true }).getByRole('link', { name: 'Edit account', exact: true })).toBeVisible()
  await expect(page.locator('#home-balance-chart')).toHaveAttribute('data-ready', 'true')
  const metrics = await sections.evaluateAll(elements => elements.map((el, index) => {
    const header = el.querySelector('header')!, content = header.nextElementSibling!
    return { gap: content.getBoundingClientRect().top - header.getBoundingClientRect().bottom,
      previous: index ? el.getBoundingClientRect().top - elements[index - 1].getBoundingClientRect().bottom : 0 }
  }))
  for (const metric of metrics) expect(metric.gap).toBeCloseTo(16, 0)
  for (const metric of metrics.slice(1)) expect(metric.previous).toBeCloseTo(64, 0)
  const transactions = page.getByRole('table', { name: 'Operating checking transactions', exact: true })
  await expect(transactions.locator('tbody tr')).toHaveCount(10)
  await page.getByRole('navigation', { name: 'Account transactions', exact: true }).getByRole('link', { name: 'Next', exact: true }).click()
  await expect(page).toHaveURL(url => url.searchParams.get('transactionPage') === '2')
  await expect(transactions.locator('tbody tr')).toHaveCount(5)
  const dates = page.getByRole('form', { name: 'Chart date range', exact: true })
  await dates.getByLabel('Start date', { exact: true }).fill('2026-09-14')
  await dates.getByLabel('End date', { exact: true }).fill('2026-09-17')
  await dates.getByRole('button', { name: 'Apply range', exact: true }).click()
  await expect(page).toHaveURL(url => url.pathname === '/examples/application/accounts/101' && url.searchParams.get('startDate') === '2026-09-14')
  await expect(transactions.locator('tbody tr')).toHaveCount(3)
  await expect(transactions).toContainText('Cloud hosting')
  await page.getByRole('button', { name: 'More actions', exact: true }).click()
  await expect(page.getByRole('menuitem', { name: 'Delete', exact: true })).toBeVisible()
  await page.keyboard.press('Escape')
  expect((await new AxeBuilder({ page }).include('main').analyze()).violations).toEqual([])
})

test('Account overlays preserve the selected chart, scenario and transaction page @cross-browser', async ({ page }) => {
  const context = { range: '90', comparison: 'optimistic', startDate: '2026-07-01', endDate: '2026-09-17', transactionPage: '2' }
  const detail = '/examples/application/accounts/101'
  const assertContext = async (path: string) => {
    await expect(page).toHaveURL(url => url.pathname === path && Object.entries(context).every(([key, value]) => url.searchParams.get(key) === value))
    // Inspect the inert background as well as the returned page.
    const dates = page.locator('form[aria-label="Chart date range"]')
    await expect(dates.getByLabel('Start date', { exact: true })).toHaveValue(context.startDate)
    await expect(dates.getByLabel('End date', { exact: true })).toHaveValue(context.endDate)
    await expect(page.locator('form[aria-label="Account balance controls"] [name="comparison"]')).toHaveValue('optimistic')
    await expect(page.locator('nav[aria-label="Account transactions"]')).toContainText('Showing 11–')
  }
  const edit = page.getByRole('link', { name: 'Edit account', exact: true })
  await page.goto(detail + '?' + new URLSearchParams(context))
  await assertContext(detail)
  await edit.click()
  await assertContext(detail + '/edit')
  await page.getByRole('dialog', { name: 'Edit account', exact: true }).getByRole('link', { name: 'Cancel', exact: true }).click()
  await assertContext(detail)
  await edit.click()
  await page.getByRole('dialog', { name: 'Edit account', exact: true }).getByLabel('Name', { exact: true }).focus()
  await page.keyboard.press('Escape')
  await assertContext(detail)
  await page.getByRole('button', { name: 'More actions', exact: true }).click()
  await page.getByRole('menuitem', { name: 'Delete', exact: true }).click()
  await assertContext(detail + '/delete')
  await page.getByRole('dialog').getByRole('link', { name: 'Cancel', exact: true }).click()
  await assertContext(detail)
  await edit.click()
  await page.getByRole('dialog', { name: 'Edit account', exact: true }).getByRole('button', { name: 'Update', exact: true }).click()
  await assertContext(detail)
  await expect(page.getByRole('dialog')).toHaveCount(0)
})

test('Geldos owns charcoal dark neutrals without changing light mode or the catalog', async ({ page }) => {
  for (const route of ['/examples/marketing', '/examples/application/accounts/101', '/examples/specification', '/examples/api-documentation']) {
    await page.goto(route)
    await page.evaluate(() => (window as any).fveColorMode.set('dark'))
    const theme = page.locator('.geldos-theme').first()
    await expect(theme).toHaveCSS('--fve-background', '#141414')
    await expect(theme).toHaveCSS('--fve-surface', '#1c1c1c')
    await expect(theme).toHaveCSS('--fve-surface-hover', '#242424')
    await expect(theme).toHaveCSS('--fve-border', '#363636')
    await expect(theme).toHaveCSS('--fve-brand-solid', '#10b981')
    await page.evaluate(() => (window as any).fveColorMode.set('light'))
    await expect(theme).toHaveCSS('--fve-brand-solid', '#047857')
    const light = await theme.evaluate(el => {
      const before = getComputedStyle(el).getPropertyValue('--fve-background')
      el.classList.remove('geldos-theme')
      const foundation = getComputedStyle(el).getPropertyValue('--fve-background')
      el.classList.add('geldos-theme')
      return { before, foundation }
    })
    expect(light.before).toBe(light.foundation)
  }
  await page.goto('/components/icon')
  await page.evaluate(() => (window as any).fveColorMode.set('dark'))
  await expect(page.locator('.geldos-theme')).toHaveCount(0)
  await expect(page.locator('.fve-components').first()).not.toHaveCSS('--fve-background', '#141414')
})

test('Disclosure retains native grouping and keyboard behavior without JavaScript', async ({ browser }) => {
  const context = await browser.newContext({ javaScriptEnabled: false })
  try {
    const page = await context.newPage()
    await page.goto('/components/disclosure')
    const first = page.locator('summary').filter({ hasText: 'Can I change the content?' })
    await first.focus()
    await first.press('Enter')
    await expect(page.getByText('Yes. Supply any semantic HTML as the disclosure body.', { exact: true })).toBeVisible()
    await expect(page.getByText('No. Native details and summary own keyboard interaction and expanded state.', { exact: true })).toBeHidden()
  } finally { await context.close() }
})
