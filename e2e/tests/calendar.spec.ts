import { test, expect } from '../fixture'
import AxeBuilder from '@axe-core/playwright'

for (const [view, width, scale, dark] of [
  ['month', 1600, 1, false],
  ['month', 390, 1, true],
  ['week', 1600, 1, true],
  ['week', 390, 1, true],
  ['day', 320, 2, true],
] as const) {
    test(`Calendar ${view} has representative geometry at ${width}px ${scale}x text`, async ({ page }) => {
      await page.setViewportSize({ width, height: 1000 })
      await page.goto(`/components/${view}-calendar`)
      await page.evaluate(scale => { document.documentElement.style.fontSize = `${100 * scale}%` }, scale)
      const preview = `#components-${view}-calendar-events-panel-preview`
      const calendar = page.locator(`${preview} .fve-calendar`)
      await expect(calendar).toHaveAttribute('data-view', view)
      await expect(calendar.getByRole('link', { name: /Coastal trail lesson/ })).toBeVisible()
      await expect(calendar.locator('time[aria-current="date"]')).toHaveAttribute('datetime', '2026-09-17')
      const geometry = await calendar.evaluate(el => {
        const dates = el.querySelector('.fve-calendar-days')!
        const first = el.querySelector('[data-event="lesson-201"]')!.getBoundingClientRect()
        const second = el.querySelector('[data-event="camp-017"]')!.getBoundingClientRect()
        const event = el.querySelector('[data-event="lesson-201"] a')!
        return {
          columns: getComputedStyle(dates).gridTemplateColumns.split(' ').length,
          count: el.querySelectorAll('.fve-calendar-days > [data-date]').length,
          visibleCount: [...el.querySelectorAll<HTMLElement>('.fve-calendar-days > [data-date]')].filter(day => getComputedStyle(day).display !== 'none').length,
          first: { x: first.x, right: first.right, y: first.y, height: first.height },
          second: { x: second.x, y: second.y },
          scrollable: el.querySelector('.fve-calendar-body')!.scrollWidth > el.querySelector('.fve-calendar-body')!.clientWidth,
          overflow: el.scrollWidth - el.clientWidth,
          font: parseFloat(getComputedStyle(event.querySelector('strong, span:not([aria-hidden])')!).fontSize),
        }
      })
      expect(geometry.count).toBe(view === 'month' ? 35 : view === 'week' ? 7 : 1)
      expect(geometry.overflow).toBeLessThanOrEqual(1)
      expect(geometry.font).toBeGreaterThanOrEqual((view === 'month' ? 12 : 14) * scale)
      expect(geometry.columns).toBe(view === 'day' ? 1 : 7)
      expect(geometry.visibleCount).toBe(geometry.count)
      if (width < 1600) expect(geometry.scrollable).toBe(true)
      if (view !== 'month') {
        expect(geometry.first.right).toBeLessThanOrEqual(geometry.second.x)
        // 30-minute start difference / 90-minute duration, allowing the event gutters.
        expect(Math.abs((geometry.second.y - geometry.first.y) / (geometry.first.height + 2) - 1 / 3)).toBeLessThan(.015)
      }
      await page.evaluate(dark => {
        document.documentElement.classList.toggle('dark', dark)
        document.documentElement.dataset.theme = dark ? 'dark' : 'light'
      }, dark)
      expect((await new AxeBuilder({ page }).include(preview).analyze()).violations).toEqual([])
      await calendar.getByRole('link', { name: /Coastal trail lesson/ }).focus()
      await page.keyboard.press('Enter')
      await expect(page).toHaveURL(`/components/${view}-calendar`)
      await expect(calendar.getByRole('link', { name: /Coastal trail lesson/ })).toBeFocused()
    })
}

test('Week calendar aligns hourly rows across empty and populated all-day columns @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 1600, height: 1000 })
  await page.goto('/components/week-calendar')
  const calendar = page.locator('#components-week-calendar-default-panel-preview .fve-calendar')
  await expect(calendar.getByRole('list', { name: 'Thursday, September 17 all-day events', exact: true })).toContainText('Coastal trail lesson')
  const timed = calendar.getByRole('list', { name: /timed events$/ })
  await expect(timed).toHaveCount(7)
  const tops = await timed.evaluateAll(lists => lists.map(list => list.getBoundingClientRect().top))
  expect(Math.max(...tops) - Math.min(...tops)).toBeLessThanOrEqual(1)
  const firstHour = await calendar.locator('.fve-calendar-hours > span').first().boundingBox()
  expect(Math.abs(firstHour!.y - tops[0])).toBeLessThanOrEqual(1)
})

test('Day, Week and full Month keep scrollable grids on mobile @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 1000 })
  for (const view of ['day', 'week', 'month']) {
    await page.goto(`/components/${view}-calendar`)
    const calendar = page.locator(`#components-${view}-calendar-events-panel-preview .fve-calendar`)
    const body = calendar.getByRole('region', { name: /scrollable (times|dates)$/ })
    if (view === 'week') {
      // Overlapping cards must have readable lanes, not just technically overflow.
      expect((await calendar.locator('.fve-calendar-day').first().boundingBox())!.width).toBeGreaterThanOrEqual(250)
    }
    await body.focus()
    await page.keyboard.press('ArrowRight')
    await expect.poll(() => body.evaluate(el => el.scrollLeft)).toBeGreaterThan(0)
    if (view !== 'month') {
      const before = await body.boundingBox()
      const labels = await calendar.locator('.fve-calendar-hours').boundingBox()
      expect(Math.abs(labels!.x - before!.x - 1)).toBeLessThanOrEqual(1)
      await body.evaluate(el => { el.scrollTop = 200 })
      const header = await calendar.locator('.fve-calendar-date').first().boundingBox()
      expect(Math.abs(header!.y - before!.y - 1)).toBeLessThanOrEqual(1)
    } else {
      await calendar.getByRole('link', { name: 'Show Sunday, September 27, 2026', exact: true }).focus()
      const visible = await body.evaluate(el => {
        const box = el.getBoundingClientRect(), focus = document.activeElement!.getBoundingClientRect()
        return focus.left >= box.left && focus.right <= box.right
      })
      expect(visible).toBe(true)
    }
  }
})

for (const [width, scale, columns] of [[1600, 1, 2], [320, 2, 1]] as const) {
  test(`Calendar year shows twelve compact months at ${width}px ${scale}x text`, async ({ page }) => {
    await page.setViewportSize({ width, height: 1000 })
    await page.goto('/components/year-calendar')
    await page.evaluate(scale => { document.documentElement.style.fontSize = `${100 * scale}%` }, scale)
    const preview = '#components-year-calendar-events-panel-preview'
    const calendar = page.locator(`${preview} .fve-calendar`)
    await expect(calendar).toHaveAttribute('data-view', 'year')
    await expect(calendar.locator('[data-fve-month-calendar="compact"]')).toHaveCount(12)
    await expect(calendar.getByRole('heading', { name: 'January', exact: true })).toBeVisible()
    await expect(calendar.getByRole('heading', { name: 'December', exact: true })).toBeVisible()
    await expect(calendar.getByRole('link', { name: /Thursday, September 17, 2026, 2 events/ })).toBeVisible()
    await expect(calendar.locator('[data-fve-month-calendar="compact"] a')).toHaveCount(4)
    await expect(calendar.locator('section[aria-label$="September 2026"] time[datetime="2026-09-01"]')).toHaveText('1')
    await expect(calendar.locator('input')).toHaveCount(0)
    await expect(calendar.getByRole('link', { name: 'Previous month', exact: true })).toHaveCount(0)
    const geometry = await calendar.evaluate(el => {
      const year = el.querySelector('.fve-calendar-year')!
      return {
        columns: getComputedStyle(year).gridTemplateColumns.split(' ').length,
        months: year.querySelectorAll('[data-fve-month-calendar="compact"]').length,
        dates: year.querySelectorAll('[role="gridcell"]').length,
        overflow: el.scrollWidth - el.clientWidth,
        overflowingDates: [...year.querySelectorAll<HTMLElement>('[role="gridcell"]')].filter(date => date.scrollWidth > date.clientWidth + 1).length,
      }
    })
    expect(geometry.overflowingDates).toBe(0)
    expect(geometry.columns).toBe(columns)
    expect(geometry.months).toBe(12)
    expect(geometry.dates).toBe(441)
    expect(geometry.overflow).toBeLessThanOrEqual(1)
  })
}

test('Compact Month calendar provides display navigation without creating form state @cross-browser', async ({ page }) => {
  await page.goto('/components/month-calendar')
  const preview = '#components-month-calendar-compact-panel-preview'
  const month = page.locator(`${preview} [data-fve-month-calendar="compact"]`)
  await expect(month.getByRole('gridcell')).toHaveCount(42)
  await expect(month.locator('time[datetime="2026-09-01"]')).toHaveText('1')
  await expect(month.locator('time[datetime="2026-09-17"]')).toHaveText('17')
  await expect(month.locator('input')).toHaveCount(0)
  const date = month.getByRole('link', { name: /Selected: Thursday, September 17, 2026, 2 events/ })
  await expect(date).toBeVisible()
  await date.click()
  await expect(page).toHaveURL('/components/month-calendar')
  expect((await new AxeBuilder({ page }).include(preview).analyze()).violations).toEqual([])

  await page.setViewportSize({ width: 320, height: 1000 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  await month.focus()
  await expect(month).toBeFocused()
  await month.getByRole('link', { name: 'Show Sunday, September 20, 2026', exact: true }).focus()
  const geometry = await month.evaluate(el => {
    const card = el.getBoundingClientRect()
    const focused = document.activeElement!.getBoundingClientRect()
    return { scrollable: el.scrollWidth > el.clientWidth, left: focused.left - card.left, right: focused.right - card.right }
  })
  expect(geometry.scrollable).toBe(true)
  expect(geometry.left).toBeGreaterThanOrEqual(0)
  expect(geometry.right).toBeLessThanOrEqual(1)
})
