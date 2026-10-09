import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const examples = [
  ['components-table-panel-preview', 'Default'],
  ['components-table-comfortable-panel-preview', 'Comfortable rows'],
  ['components-table-status-panel-preview', 'With status values'],
  ['components-table-selection-panel-preview', 'With checkboxes'],
  ['components-table-scrollable-panel-preview', 'Scrollable rows and sticky headings'],
  ['components-table-mobile-panel-preview', 'Stacked on mobile'],
  ['components-table-sorting-panel-preview', 'Sortable records'],
  ['components-table-hierarchy-panel-preview', 'Hierarchical accounts and aggregates'],
  ['components-table-empty-panel-preview', 'Empty state'],
] as const

test('table examples render focused native semantics', async ({ page }) => {
  await page.goto('/components/table')
  await expect(page.locator('[data-docs-example="true"]')).toHaveCount(examples.length)
  for (const [id, title] of examples) {
    const preview = page.locator(`#${id}`)
    const example = preview.locator('..')
    await expect(example.getByRole('heading', { name: title, exact: true })).toBeVisible()
    await expect(preview.getByRole('table')).toHaveCount(id === 'components-table-empty-panel-preview' ? 0 : 1)
    await expect(preview.getByRole('link')).toHaveCount(id === 'components-table-sorting-panel-preview' ? 2 : id === 'components-table-hierarchy-panel-preview' ? 7 : 0)
    await expect(preview.getByRole('button')).toHaveCount(id === 'components-table-hierarchy-panel-preview' ? 3 : 0)
    await expect(preview.getByRole('checkbox')).toHaveCount(id === 'components-table-selection-panel-preview' ? 5 : 0)
  }
  const simple = page.locator('#components-table-panel-preview')
  await expect(simple.getByRole('columnheader')).toHaveText(['Name', 'Email', 'Role'])
  await expect(simple.getByRole('rowheader')).toHaveCount(4)
  for (const name of ['Alex Morgan', 'Jamie Lee', 'Riley Chen', 'Sam Rivera']) await expect(simple.getByRole('rowheader', { name, exact: true })).toBeVisible()
  const simpleRow = await simple.locator('tbody tr').first().boundingBox()
  const comfortableRow = await page.locator('#components-table-comfortable tbody tr').first().boundingBox()
  expect(comfortableRow!.height).toBeGreaterThan(simpleRow!.height)
  await expect(page.locator('#components-table-status .docs-components-preview').getByText('Active', { exact: true })).toHaveCount(3)
  await expect(page.locator('#components-table-status .docs-components-preview').getByText('Invited', { exact: true })).toBeVisible()
  await expect(page.locator('#components-table-empty .docs-components-preview').getByText('No team members', { exact: true })).toBeVisible()
  await expect(page.locator('#components-table-empty .docs-components-preview').getByRole('region', { name: 'Empty team members', exact: true })).toBeVisible()
})

test('sortable table headers expose current state and leave ordering to the consumer route @cross-browser', async ({ page }) => {
  await page.goto('/components/table')
  const example = page.locator('#components-table-sorting')
  const table = example.getByRole('table', { name: 'Sortable team members', exact: true })
  await expect(table.getByRole('columnheader', { name: /Name/ })).toHaveAttribute('aria-sort', 'ascending')
  await expect(table.getByRole('columnheader', { name: /Role/ })).not.toHaveAttribute('aria-sort')
  await expect(table.getByRole('rowheader').first()).toHaveText('Alex Morgan')
  await page.evaluate(() => { (window as any).__tableSortDocumentMarker = true })
  const sortResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/components/table/sort')
  await table.getByRole('columnheader', { name: /Role/ }).getByRole('link').click()
  expect((await sortResponse).status()).toBe(200)
  await expect(page).toHaveURL('/components/table')
  expect(await page.evaluate(() => (window as any).__tableSortDocumentMarker)).toBe(true)
  const sorted = page.locator('#components-table-sorting').getByRole('table', { name: 'Sortable team members', exact: true })
  await expect(sorted.getByRole('columnheader', { name: /Role/ })).toHaveAttribute('aria-sort', 'ascending')
  await expect(sorted.getByRole('rowheader').first()).toHaveText('Sam Rivera')
  await page.setViewportSize({ width: 390, height: 844 })
  const mobileSort = page.locator('#components-table-sorting').getByRole('group', { name: 'Sort Sortable team members', exact: true })
  await expect(mobileSort).toBeVisible()
  await expect(mobileSort.getByRole('link', { name: /Sort by Name; currently unsorted/ })).toBeVisible()
})

test('table selection is native page-scoped and independent of the other examples @cross-browser', async ({ page }) => {
  await page.goto('/components/table')
  const example = page.locator('#components-table-selection')
  const selection = example.locator('[data-fve-table]')
  const all = selection.getByRole('checkbox', { name: 'Select all rows on this page' })
  const alex = selection.getByRole('checkbox', { name: 'Select Alex Morgan', exact: true })
  const guest = selection.getByRole('checkbox', { name: 'Select Sam Rivera', exact: true })
  await expect(guest).toBeEnabled()
  await alex.focus()
  await alex.press('Space')
  await expect(all).toBeChecked({ indeterminate: true })
  await expect(selection.getByRole('status')).toHaveText('1 selected')
  await all.check()
  await expect(selection.getByRole('status')).toHaveText('4 selected')
  await expect(guest).toBeChecked()
  const values = await selection.evaluate(element => {
    const form = document.createElement('form')
    form.append(element.cloneNode(true))
    return new FormData(form).getAll('memberIds')
  })
  expect(values).toEqual(['1', '2', '3', '4'])
  await example.getByRole('tab', { name: 'Code', exact: true }).click()
  await example.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(all).toBeChecked()
  await all.uncheck()
  await expect(selection.getByRole('status')).toHaveText('0 selected')
  await expect(page.locator('#components-table-mobile').getByRole('checkbox')).toHaveCount(0)
})

test('bounded table rows scroll without moving their heading @cross-browser', async ({ page }) => {
  await page.goto('/components/table')
  const preview = page.locator('#components-table-scrollable-panel-preview')
  const scroll = preview.getByRole('region', { name: 'Team members in a bounded viewport', exact: true })
  const header = preview.getByRole('columnheader', { name: 'Name', exact: true })
  const top = (await header.boundingBox())!.y
  await scroll.evaluate(element => { element.scrollTop = element.scrollHeight })
  await expect.poll(() => scroll.evaluate(element => element.scrollTop)).toBeGreaterThan(0)
  expect(Math.abs((await header.boundingBox())!.y - top)).toBeLessThanOrEqual(1)
})

test('table examples preserve scrolling and one-tree mobile records across themes and text sizes', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto('/components/table')
  const mobile = page.locator('#components-table-mobile .docs-components-preview')
  const first = mobile.locator('tbody tr').first()
  const original = await first.elementHandle()
  await expect(first).toHaveCSS('display', 'table-row')
  for (const [width, scale] of [[1440, 1], [390, 1], [320, 2]]) {
    await page.setViewportSize({ width, height: 1000 })
    await page.evaluate(scale => { document.documentElement.style.fontSize = scale === 2 ? '200%' : '' }, scale)
    for (const theme of ['Light', 'Dark']) {
      await page.getByRole('button', { name: 'Choose color theme' }).click()
      await page.getByRole('menuitemradio', { name: theme, exact: true }).click()
      await expect.poll(() => page.evaluate(() => document.getAnimations().filter(animation => animation instanceof CSSTransition && animation.playState === 'running').length)).toBe(0)
      await expect(first).toHaveCSS('display', width < 640 ? 'grid' : 'table-row')
      expect(await first.evaluate((element, identity) => element === identity, original)).toBe(true)
      await expect(mobile.getByRole('rowheader')).toHaveCount(4)
      if (width < 640) {
        await expect(first.getByText('Email', { exact: true })).toBeVisible()
        await expect(first.getByText('Role', { exact: true })).toBeVisible()
        const primary = await first.getByRole('rowheader').boundingBox()
        const label = await first.getByText('Email', { exact: true }).boundingBox()
        expect(Math.abs(primary!.x - label!.x)).toBeLessThanOrEqual(1)
        expect(await mobile.getByRole('table').evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true)
        const scroll = page.locator('#components-table-panel-preview .fve-table-scroll')
        await expect(scroll).toHaveAttribute('tabindex', '0')
        expect(await scroll.evaluate(element => element.scrollWidth > element.clientWidth)).toBe(true)
        await scroll.evaluate(element => { element.scrollLeft = 0 })
        await scroll.focus()
        await expect(scroll).toBeFocused()
        // WebKit cancels an instantaneous down/up before the first native scroll frame.
        await page.keyboard.down('ArrowRight')
        try {
          await expect.poll(() => scroll.evaluate(element => element.scrollLeft)).toBeGreaterThan(0)
        } finally {
          await page.keyboard.up('ArrowRight')
        }
      }
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
      expect((await new AxeBuilder({ page }).include('[data-docs-layout="gallery"]').analyze()).violations).toEqual([])
      await page.locator(width === 1440 ? '#components-table-panel-preview' : '#components-table-mobile').evaluate(element => element.scrollIntoView({ block: 'start' }))
      await page.screenshot({ path: testInfo.outputPath(`tables-${theme.toLowerCase()}-${width}-${scale}x.png`) })
    }
  }
})
