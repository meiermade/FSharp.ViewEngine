import { test, expect, specProduct } from '../fixture'

test('Rocket chart selects inclusive dates through the server and survives theme, history and remounts @cross-browser', async ({ page }) => {
  const errors: string[] = []
  const filters: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  page.on('request', request => {
    if (request.headers()['x-fve-navigation-intent'] === 'filter') filters.push(request.url())
  })
  await page.goto('/examples/application?account=101&range=90&comparison=optimistic')
  const chart = page.locator('#home-balance-chart')
  await expect(chart).toHaveAttribute('data-ready', 'true')
  await expect(chart.locator('svg')).toHaveCount(1)
  await expect(page.getByRole('table', { name: 'Transactions for the selected account' }).locator('tbody tr')).toHaveCount(15)
  await page.evaluate(() => { document.documentElement.dataset.chartSession = 'retained' })
  const positions = await chart.evaluate(async host => {
    const echarts = await import('/examples/assets/scripts/echarts.6.1.0.esm.min.js')
    const plot = host.shadowRoot!.querySelector('.plot') as HTMLElement
    const instance = echarts.getInstanceByDom(plot)!
    ;(window as any).originalHomeChart = instance
    const box = plot.getBoundingClientRect()
    return [14, 17].map(date => ({
      x: box.x + (instance.convertToPixel({ xAxisIndex: 0 }, Date.UTC(2026, 8, date, 12)) as number),
      y: box.y + box.height / 2,
    }))
  })
  await page.mouse.move(positions[0].x, positions[0].y)
  await page.mouse.down()
  await page.mouse.move(positions[1].x, positions[1].y, { steps: 12 })
  expect(filters).toHaveLength(0)
  await page.mouse.up()
  await expect(page).toHaveURL(url => url.searchParams.get('startDate') === '2026-09-14' && url.searchParams.get('endDate') === '2026-09-17')
  await expect(page.locator('#home-start')).toHaveValue('2026-09-14')
  const rows = page.getByRole('table', { name: 'Transactions for the selected account' }).locator('tbody tr')
  await expect(rows).toHaveCount(3)
  await expect(rows.first()).toContainText('Cloud hosting')
  await expect(rows.last()).toContainText('Design software')
  expect(filters).toHaveLength(1)
  await expect(page.locator('html')).toHaveAttribute('data-chart-session', 'retained')
  expect(await chart.evaluate(async host => {
    const echarts = await import('/examples/assets/scripts/echarts.6.1.0.esm.min.js')
    const instance = echarts.getInstanceByDom(host.shadowRoot!.querySelector('.plot') as HTMLElement)!
    const option = instance.getOption() as any
    const zoom = option.dataZoom[0]
    return { same: instance === (window as any).originalHomeChart, start: zoom.startValue, end: zoom.endValue,
      actualStep: option.series[0].step, closingPoint: option.series[0].data.at(-1) }
  })).toEqual({ same: true, start: Date.UTC(2026, 8, 14), end: Date.UTC(2026, 8, 18) - 1,
    actualStep: 'end', closingPoint: [Date.UTC(2026, 8, 19) - 1, 23666] })
  for (const mode of ['dark', 'light']) {
    await page.evaluate(mode => (window as any).fveColorMode.set(mode), mode)
    await expect(page.locator('#home-end')).toHaveValue('2026-09-17')
    const strokes = () => chart.locator('svg path[fill="none"]').evaluateAll(paths => paths
      .filter(path => (path.getAttribute('d') || '').length > 100).map(path => getComputedStyle(path).stroke))
    await page.mouse.move(0, 0)
    expect(await strokes()).toHaveLength(2)
    await chart.locator('svg').hover({ position: { x: 200, y: 100 } })
    await expect.poll(strokes).toHaveLength(2)
    for (const stroke of await strokes()) expect(stroke).not.toMatch(/none|transparent|rgba\([^)]*,\s*0\)/)
  }
  await page.reload()
  await expect(chart).toHaveAttribute('data-ready', 'true')
  await expect(rows).toHaveCount(3)
  const dates = page.getByRole('form', { name: 'Chart date range' })
  await dates.getByLabel(/^Start date/).fill('2026-09-18')
  await dates.getByLabel(/^End date/).fill('2026-09-18')
  await dates.getByRole('button', { name: 'Apply range', exact: true }).press('Enter')
  await expect(rows).toHaveCount(1)
  await expect(rows).toContainText('Client payment')
  await page.goBack()
  await expect(rows).toHaveCount(3)
  await page.goForward()
  await expect(rows).toHaveCount(1)
  await page.getByRole('link', { name: 'Reset zoom', exact: true }).click()
  await expect(page).toHaveURL(url => !url.searchParams.has('startDate') && url.searchParams.get('comparison') === 'optimistic')
  await expect(rows).toHaveCount(15)
  await chart.evaluate(async host => {
    const echarts = await import('/examples/assets/scripts/echarts.6.1.0.esm.min.js')
    ;(window as any).leavingChart = echarts.getInstanceByDom(host.shadowRoot!.querySelector('.plot') as HTMLElement)
  })
  await page.getByRole('link', { name: 'Accounts', exact: true }).first().click()
  await expect(page.getByRole('heading', { name: 'Accounts', exact: true, level: 1 })).toBeVisible()
  expect(await page.evaluate(() => (window as any).leavingChart.isDisposed())).toBe(true)
  await page.goBack()
  await expect(chart).toHaveAttribute('data-ready', 'true')
  await expect(chart.locator('svg')).toHaveCount(1)
  await dates.getByLabel(/^Start date/).fill('2026-09-14')
  await dates.getByLabel(/^End date/).fill('2026-09-17')
  await dates.getByRole('button', { name: 'Apply range', exact: true }).click()
  await expect(rows).toHaveCount(3)
  await chart.locator('svg').dblclick({ position: { x: 200, y: 100 } })
  await expect(page).toHaveURL(url => url.searchParams.get('startDate') === '2026-06-21' && url.searchParams.get('endDate') === '2026-09-18')
  await expect(rows).toHaveCount(15)
  await page.setViewportSize({ width: 320, height: 800 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '32px' })
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBe(320)
  await expect(chart.locator('svg text').first()).toHaveCSS('font-size', '24px')
  expect(errors).toEqual([])
})

test('Specification uses the same Rocket chart and server-owned range @cross-browser', async ({ page }) => {
  await page.goto('/examples/specification/home')
  const fixture = specProduct(page)
  await expect(fixture.locator('fve-chart')).toHaveAttribute('data-ready', 'true')
  const dates = fixture.getByRole('form', { name: 'Chart date range' })
  await dates.getByLabel(/^Start date/).fill('2026-09-14')
  await dates.getByLabel(/^End date/).fill('2026-09-17')
  await dates.getByRole('button', { name: 'Apply range', exact: true }).click()
  await expect(page).toHaveURL(url => url.pathname === '/examples/specification/home' && url.searchParams.get('endDate') === '2026-09-17')
  await expect(fixture.getByRole('table', { name: 'Transactions for the selected account' }).locator('tbody tr')).toHaveCount(3)
  await expect(fixture.locator('fve-chart')).toHaveAttribute('data-ready', 'true')
})

test('Home date filtering has a native GET fallback without JavaScript', async ({ browser }) => {
  const context = await browser.newContext({ javaScriptEnabled: false })
  const page = await context.newPage()
  await page.goto('/examples/application?account=101&range=30')
  const dates = page.getByRole('form', { name: 'Chart date range' })
  await dates.getByLabel(/^Start date/).fill('2026-09-14')
  await dates.getByLabel(/^End date/).fill('2026-09-17')
  await dates.getByRole('button', { name: 'Apply range', exact: true }).click()
  const rows = page.getByRole('table', { name: 'Transactions for the selected account' }).locator('tbody tr')
  await expect(rows).toHaveCount(3)
  await page.getByRole('link', { name: 'Reset zoom', exact: true }).click()
  await expect(page).toHaveURL(url => !url.searchParams.has('startDate') && url.searchParams.get('range') === '30')
  await expect(rows).toHaveCount(7)
  await context.close()
})
