import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

test.beforeEach(async ({ page }) => {
  // Observe the public application hook, without depending on the host's private renderer internals.
  await page.addInitScript(() => document.addEventListener('chart-render', (event: any) => {
    ;((window as any).charts ??= {})[event.target.id] = event.detail.chart
  }))
})

test('Chart lazily renders line, column, horizontal bar and Sankey options across themes and sizes @cross-browser', async ({ page }) => {
  const errors: string[] = []
  const scripts: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  page.on('request', request => { if (request.resourceType() === 'script') scripts.push(request.url()) })
  await page.goto('/components/button')
  await expect(page.locator('fve-button button').first()).toBeAttached()
  expect(scripts.filter(url => /(?:\/chart-|\/echarts)/.test(url))).toEqual([])

  await page.goto('/components/chart')
  for (const id of ['line-chart', 'column-chart', 'bar-chart', 'sankey-chart']) {
    const chart = page.locator(`#${id}`)
    await expect(chart).toHaveAttribute('data-ready', 'true')
    await expect(chart.locator('svg')).toHaveCount(1)
  }
  expect(await page.evaluate(() => ['line-chart', 'column-chart', 'bar-chart', 'sankey-chart'].map(id => {
    const option = (window as any).charts[id].getOption()
    return { series: option.series[0].type, x: option.xAxis?.[0]?.type, y: option.yAxis?.[0]?.type }
  }))).toEqual([
    { series: 'line', x: 'category', y: 'value' },
    { series: 'bar', x: 'category', y: 'value' },
    { series: 'bar', x: 'value', y: 'category' },
    { series: 'sankey', x: undefined, y: undefined },
  ])
  expect(scripts.some(url => /echarts/.test(url))).toBe(true)
  const sankey = page.locator('#sankey-chart')
  for (const name of ['Income', 'Costs', 'Savings']) await expect(sankey.getByText(name, { exact: true })).toBeVisible()
  await expect(page.locator('#empty-values-chart')).toContainText('No chart data.')
  const originalFont = await sankey.locator('svg text').first().evaluate(el => parseFloat(getComputedStyle(el).fontSize))
  await page.evaluate(() => (window as any).fveColorMode.set('light'))
  const lightColor = await page.evaluate(() => (window as any).charts['line-chart'].getOption().textStyle.color)
  await page.evaluate(() => { (window as any).originalLine = (window as any).charts['line-chart'] })
  await page.evaluate(() => (window as any).fveColorMode.set('dark'))
  await expect.poll(() => page.evaluate(() => (window as any).charts['line-chart'].getOption().textStyle.color)).not.toBe(lightColor)
  await page.setViewportSize({ width: 320, height: 900 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  await expect.poll(() => sankey.locator('svg text').first().evaluate(el => parseFloat(getComputedStyle(el).fontSize))).toBeGreaterThanOrEqual(originalFont * 1.9)
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBe(320)
  expect(await page.locator('p code').evaluateAll(elements => elements.flatMap(element => {
    const paragraph = element.parentElement!.getBoundingClientRect()
    return [...element.getClientRects()].filter(rect => rect.left < paragraph.left - 1 || rect.right > paragraph.right + 1).map(() => element.textContent)
  }))).toEqual([])
  expect(await page.evaluate(() => (window as any).charts['line-chart'] === (window as any).originalLine)).toBe(true)
  expect((await new AxeBuilder({ page }).include('#components-chart-default-panel-preview').analyze()).violations).toEqual([])
  expect(errors).toEqual([])
})

test('Chart updates types and datasets on one instance and forwards only requested ECharts events @cross-browser', async ({ page }) => {
  await page.goto('/components/chart')
  const chart = page.locator('#interactive-values-chart')
  const example = page.locator('#components-chart-interaction-panel-preview')
  await expect(chart).toHaveAttribute('data-ready', 'true')
  await page.evaluate(() => { (window as any).originalInteractive = (window as any).charts['interactive-values-chart'] })
  await example.getByRole('button', { name: 'Show columns', exact: true }).click()
  await expect.poll(() => page.evaluate(() => (window as any).charts['interactive-values-chart'].getOption().series[0].type)).toBe('bar')
  await chart.scrollIntoViewIfNeeded()
  const clickBar = async (month: string, value: number) => {
    const position = await page.evaluate(({ month, value }) => {
      const instance = (window as any).charts['interactive-values-chart']
      const box = instance.getDom().getBoundingClientRect()
      const [x, y] = instance.convertToPixel({ xAxisIndex: 0, yAxisIndex: 0 }, [month, value])
      return { x: box.x + x, y: box.y + y }
    }, { month, value })
    await page.mouse.click(position.x, position.y)
  }
  await clickBar('Feb', 18)
  await expect(example.getByRole('status')).toHaveText('Feb: 36')
  await chart.evaluate(el => new Promise<void>(resolve => {
    el.addEventListener('chart-render', () => resolve(), { once: true })
    el.setAttribute('events', '[]')
  }))
  await clickBar('Jan', 12)
  await expect(example.getByRole('status')).toHaveText('Feb: 36')
  await chart.evaluate(el => new Promise<void>(resolve => {
    el.addEventListener('chart-render', () => resolve(), { once: true })
    el.setAttribute('events', '["click"]')
  }))
  await clickBar('Jan', 12)
  await expect(example.getByRole('status')).toHaveText('Jan: 24')

  await chart.evaluate(el => el.setAttribute('options', document.getElementById('sankey-chart')!.getAttribute('options')!))
  await expect(chart.getByText('Income', { exact: true })).toBeVisible()
  expect(await page.evaluate(() => (window as any).charts['interactive-values-chart'].getOption().xAxis ?? [])).toEqual([])
  await example.getByRole('button', { name: 'Show line', exact: true }).click()
  await expect.poll(() => page.evaluate(() => (window as any).charts['interactive-values-chart'].getOption().series[0].type)).toBe('line')
  await chart.evaluate(el => el.setAttribute('options', JSON.stringify({
    xAxis: { type: 'category' }, yAxis: { type: 'value' },
    legend: {}, dataZoom: [{ type: 'inside' }],
    dataset: { source: [['month', 'revenue'], ['Apr', 54], ['May', 62]] }, series: [{ name: 'Revenue', type: 'bar' }],
  })))
  await expect(chart.getByText('Apr', { exact: true })).toBeVisible()
  await expect(chart.getByText('May', { exact: true })).toBeVisible()
  await page.evaluate(() => {
    const instance = (window as any).charts['interactive-values-chart']
    instance.dispatchAction({ type: 'legendUnSelect', name: 'Revenue' })
    instance.dispatchAction({ type: 'dataZoom', start: 10, end: 90 })
  })
  const interaction = () => page.evaluate(() => {
    const option = (window as any).charts['interactive-values-chart'].getOption()
    return { selected: option.legend[0].selected.Revenue, start: option.dataZoom[0].start, end: option.dataZoom[0].end }
  })
  expect(await interaction()).toEqual({ selected: false, start: 10, end: 90 })
  await page.evaluate(() => (window as any).fveColorMode.set('dark'))
  await expect.poll(interaction).toEqual({ selected: false, start: 10, end: 90 })
  await chart.evaluate(el => el.parentElement!.style.setProperty('--fve-brand-solid', '#7c3aed'))
  await expect.poll(() => page.evaluate(() => (window as any).charts['interactive-values-chart'].getOption().color[0])).toBe('rgba(124,58,237,1)')
  expect(await interaction()).toEqual({ selected: false, start: 10, end: 90 })
  expect(await page.evaluate(() => (window as any).charts['interactive-values-chart'] === (window as any).originalInteractive)).toBe(true)

  // A newly morphed example must bind its ref to the new host, not the disposed chart.
  await page.locator('#nav-components-table').click()
  await expect(chart).toHaveCount(0)
  await page.locator('#nav-components-chart').click()
  await expect(chart).toHaveAttribute('data-ready', 'true')
  await example.getByRole('button', { name: 'Show columns', exact: true }).click()
  await expect.poll(() => page.evaluate(() => (window as any).charts['interactive-values-chart'].getOption().series[0].type)).toBe('bar')
})

test('Chart releases empty, invalid and disconnected instances and recovers with valid options @cross-browser', async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/chart')
  const chart = page.locator('#line-chart')
  await expect(chart).toHaveAttribute('data-ready', 'true')
  const options = (await chart.getAttribute('options'))!
  await page.evaluate(() => { (window as any).releasedChart = (window as any).charts['line-chart'] })
  await chart.evaluate(el => el.setAttribute('empty', 'true'))
  await expect(chart).toContainText('No chart data.')
  expect(await page.evaluate(() => (window as any).releasedChart.isDisposed())).toBe(true)
  await chart.evaluate(el => el.setAttribute('empty', 'false'))
  await expect(chart.locator('svg')).toHaveCount(1)
  await chart.evaluate(el => el.setAttribute('options', '[]'))
  await expect(chart).toContainText('Chart data is unavailable.')
  await expect(chart).not.toHaveAttribute('data-ready', 'true')
  await chart.evaluate((el, options) => el.setAttribute('options', options), options)
  await expect(chart.locator('svg')).toHaveCount(1)
  await chart.evaluate(el => {
    el.addEventListener('chart-error', (event: any) => { (window as any).chartFailure = event.detail.error.message }, { once: true })
    el.setAttribute('options', JSON.stringify({ xAxis: {}, yAxis: {}, series: [{ type: 'custom', renderItem: {}, data: [[1, 2]] }] }))
  })
  await expect(chart).toContainText('Chart options could not be rendered.')
  expect(await page.evaluate(() => (window as any).chartFailure)).toBeTruthy()
  await chart.evaluate((el, options) => el.setAttribute('options', options), options)
  await expect(chart.locator('svg')).toHaveCount(1)
  await page.evaluate(() => { (window as any).releasedChart = (window as any).charts['line-chart'] })
  await chart.evaluate(el => el.replaceWith(el.cloneNode(true)))
  await expect(chart).toHaveAttribute('data-ready', 'true')
  await expect(chart.locator('svg')).toHaveCount(1)
  expect(await page.evaluate(() => (window as any).releasedChart.isDisposed())).toBe(true)
  expect(errors).toEqual([])
})
