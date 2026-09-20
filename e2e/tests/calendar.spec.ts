import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

for (const view of ['month', 'week', 'day']) {
  for (const [width, scale] of [[1600, 1], [390, 1], [320, 2]]) {
    test(`Calendar ${view} has focused geometry and a narrow agenda at ${width}px ${scale}x text @cross-browser`, async ({ page }, testInfo) => {
      await page.setViewportSize({ width, height: 1000 })
      await page.goto('/components/calendar')
      await page.evaluate(scale => { document.documentElement.style.fontSize = `${100 * scale}%` }, scale)
      const preview = `#components-calendar-${view}-panel-preview`
      const calendar = page.locator(`${preview} .fve-calendar`)
      await expect(calendar).toHaveAttribute('data-view', view)
      await expect(calendar.locator('[data-event="lesson-201"]')).toHaveCount(1)
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
          overflow: el.scrollWidth - el.clientWidth,
          font: parseFloat(getComputedStyle(event.querySelector('strong')!).fontSize),
        }
      })
      expect(geometry.count).toBe(view === 'month' ? 35 : view === 'week' ? 7 : 1)
      expect(geometry.overflow).toBeLessThanOrEqual(1)
      expect(geometry.font).toBeGreaterThanOrEqual(14 * scale)
      if (width === 1600) {
        expect(geometry.columns).toBe(view === 'day' ? 1 : 7)
        expect(geometry.visibleCount).toBe(geometry.count)
        if (view !== 'month') {
          expect(geometry.first.right).toBeLessThanOrEqual(geometry.second.x)
          // 30-minute start difference / 90-minute duration, allowing the event gutters.
          expect(Math.abs((geometry.second.y - geometry.first.y) / (geometry.first.height + 2) - 1 / 3)).toBeLessThan(.015)
        }
      } else {
        expect(geometry.columns).toBe(1)
        expect(Math.abs(geometry.first.x - geometry.second.x)).toBeLessThanOrEqual(1)
        expect(geometry.second.y).toBeGreaterThanOrEqual(geometry.first.y + geometry.first.height)
      }
      for (const theme of ['light', 'dark']) {
        await page.evaluate(theme => {
          document.documentElement.classList.toggle('dark', theme === 'dark')
          document.documentElement.dataset.theme = theme
        }, theme)
        expect((await new AxeBuilder({ page }).include(preview).analyze()).violations).toEqual([])
        await calendar.screenshot({ path: testInfo.outputPath(`calendar-${view}-${width}-${scale}-${theme}.png`) })
      }
      await calendar.getByRole('link', { name: /Coastal trail lesson/ }).focus()
      await page.keyboard.press('Enter')
      await expect(page).toHaveURL(/page-examples\/scheduling\?item=lesson-201/)
      await expect(page.locator('[data-fve-fixture-id="page-workspace"]')).toContainText('Maya and Sam')
    })
  }
}

for (const [width, scale, columns] of [[1600, 1, 3], [390, 1, 1], [320, 2, 1]]) {
  test(`Calendar year shows twelve compact months at ${width}px ${scale}x text @cross-browser`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 1000 })
    await page.goto('/components/calendar')
    await page.evaluate(scale => { document.documentElement.style.fontSize = `${100 * scale}%` }, scale)
    const preview = '#components-calendar-year-panel-preview'
    const calendar = page.locator(`${preview} .fve-calendar`)
    await expect(calendar).toHaveAttribute('data-view', 'year')
    await expect(calendar.locator('.fve-calendar-year-month')).toHaveCount(12)
    await expect(calendar.getByRole('heading', { name: 'January', exact: true })).toBeVisible()
    await expect(calendar.getByRole('heading', { name: 'December', exact: true })).toBeVisible()
    await expect(calendar.getByRole('link', { name: /Thursday, September 17, 2026, 2 events/ })).toBeVisible()
    await expect(calendar.locator('a.fve-calendar-year-date')).toHaveCount(4)
    const geometry = await calendar.evaluate(el => {
      const year = el.querySelector('.fve-calendar-year')!
      return {
        columns: getComputedStyle(year).gridTemplateColumns.split(' ').length,
        months: year.querySelectorAll('.fve-calendar-year-month').length,
        dates: year.querySelectorAll('.fve-calendar-year-day').length,
        overflow: el.scrollWidth - el.clientWidth,
      }
    })
    expect(geometry.columns).toBe(columns)
    expect(geometry.months).toBe(12)
    expect(geometry.dates).toBe(504)
    expect(geometry.overflow).toBeLessThanOrEqual(1)
    for (const theme of ['light', 'dark']) {
      await page.evaluate(theme => {
        document.documentElement.classList.toggle('dark', theme === 'dark')
        document.documentElement.dataset.theme = theme
      }, theme)
      expect((await new AxeBuilder({ page }).include(preview).analyze()).violations).toEqual([])
      await calendar.screenshot({ path: testInfo.outputPath(`calendar-year-${width}-${scale}-${theme}.png`) })
    }
  })
}

test('Scheduling owns date, view, today and history navigation @cross-browser', async ({ page }) => {
  await page.goto('/components/page-examples/scheduling?view=month&range=0')
  const calendar = page.getByRole('region', { name: 'Schedule', exact: true })
  await calendar.getByRole('link', { name: 'Next', exact: true }).click()
  await expect(calendar).toContainText('October 2026')
  await expect(page).toHaveURL(/range=30/)
  await calendar.getByRole('link', { name: 'Day', exact: true }).click()
  await expect(calendar).toContainText('Saturday, October 17, 2026')
  await expect(calendar).toContainText('No events in this range.')
  await page.goBack()
  await expect(calendar).toHaveAttribute('data-view', 'month')
  await calendar.getByRole('link', { name: 'Today', exact: true }).click()
  await expect(calendar).toContainText('September 2026')
  await calendar.getByRole('link', { name: 'Show Friday, September 18, 2026', exact: true }).click()
  await expect(calendar).toHaveAttribute('data-view', 'day')
  await expect(calendar).toContainText('Cornering fundamentals')
  await expect(calendar.locator('[data-event="lesson-201"]')).toHaveCount(0)
})

test('Scheduling crosses years and preserves date in App mode @cross-browser', async ({ page }) => {
  await page.goto('/components/page-examples/scheduling?view=month&range=91&fveAppMode=app&fveAppFrame=page-workspace')
  const calendar = page.getByRole('region', { name: 'Schedule', exact: true })
  await expect(calendar).toContainText('December 2026')
  await calendar.getByRole('link', { name: 'Next', exact: true }).click()
  await expect(calendar).toContainText('January 2027')
  await expect(page).toHaveURL(/fveAppMode=app/)
  await calendar.getByRole('link', { name: 'Year', exact: true }).click()
  await expect(calendar).toHaveAttribute('data-view', 'year')
  await expect(calendar).toContainText('2027')
  await calendar.getByRole('link', { name: 'Day', exact: true }).click()
  await expect(calendar).toContainText('Sunday, January 17, 2027')
  await calendar.getByRole('link', { name: 'Today', exact: true }).click()
  await expect(calendar).toContainText('Thursday, September 17, 2026')
  await expect(calendar.getByRole('link', { name: /Coastal trail lesson/ })).toBeVisible()
})

test('Calendar has four dedicated view examples and belongs to Data display @cross-browser', async ({ page }) => {
  await page.goto('/components/primitives')
  await expect(page.locator('#page-content .docs-catalog-card[href="/components/calendar"]')).toContainText('Calendar')
  await page.locator('#page-content .docs-catalog-card[href="/components/calendar"]').click()
  await expect(page.locator('[data-docs-example="true"]')).toHaveCount(4)
  for (const view of ['month', 'week', 'day', 'year']) {
    await expect(page.locator(`#components-calendar-${view}-panel-preview .fve-calendar`)).toHaveAttribute('data-view', view)
  }
  await page.locator('#components-calendar-day').getByRole('tab', { name: 'Code', exact: true }).click()
  await expect(page.locator('#components-calendar-day-panel-code')).toContainText('TimeOnly(10, 0)')
  await page.locator('#components-calendar-year').getByRole('tab', { name: 'Code', exact: true }).click()
  await expect(page.locator('#components-calendar-year-panel-code')).toContainText('CalendarView.Year')
})
