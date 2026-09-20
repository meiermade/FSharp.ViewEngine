import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const examples = [
  ['components-description-list', 6, 3],
  ['components-description-list-four-columns', 8, 4],
  ['components-description-list-supporting-text', 2, 2],
] as const

test('description lists show full-width detail grids with shared label and value typography @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto('/components/description-list')
  await expect(page.locator('[data-docs-example="true"]')).toHaveCount(3)
  for (const [id, fields, columns] of examples) {
    const example = page.locator(`#${id}`)
    const preview = example.locator('.docs-components-preview')
    const list = preview.locator('dl')
    await expect(list.getByRole('term')).toHaveCount(fields)
    await expect(list.getByRole('definition')).toHaveCount(fields)
    const bounds = await list.boundingBox()
    const frame = await preview.boundingBox()
    expect(bounds!.width).toBeGreaterThan(frame!.width * 0.9)
    expect(bounds!.x - frame!.x).toBeCloseTo(24, 0)
    expect(await list.evaluate(element => getComputedStyle(element).gridTemplateColumns.split(' ').length)).toBe(columns)
    await expect(list.getByRole('term').first()).toHaveCSS('font-size', '12px')
    await expect(list.getByRole('term').first()).toHaveCSS('text-transform', 'uppercase')
    await expect(list.getByRole('term').first()).toHaveCSS('letter-spacing', '0.3px')
    await expect(list.getByRole('definition').first()).toHaveCSS('font-size', '14px')
    await expect(list.getByRole('definition').first()).toHaveCSS('font-weight', '400')
    await expect(list.getByRole('heading')).toHaveCount(0)
    for (const field of await list.locator(':scope > div').all()) {
      await expect(field).toHaveCSS('border-top-width', '0px')
      await expect(field).toHaveCSS('background-color', 'rgba(0, 0, 0, 0)')
    }
    const statuses = id === 'components-description-list' ? ['Active'] : id === 'components-description-list-four-columns' ? ['Active', 'Up to date'] : []
    for (const label of statuses) {
      const badge = list.getByText(label, { exact: true })
      await expect(badge).toBeVisible()
      expect((await badge.boundingBox())!.width).toBeLessThan((await badge.locator('..').boundingBox())!.width)
    }
    await example.getByRole('tab', { name: 'Code', exact: true }).click()
    const code = example.locator('[data-docs-copy-source]')
    await expect(code).toContainText('DescriptionList.create')
    await expect(code).not.toContainText('ShellDestination')
    await expect(code).not.toContainText('detailsSurface')
    await example.getByRole('tab', { name: 'Preview', exact: true }).click()
  }
})

test('description lists retain readable fields in narrow themes and resized text @cross-browser', async ({ page }, testInfo) => {
  for (const [width, scale] of [[1440, 1], [800, 1], [390, 1], [320, 2]]) {
    await page.setViewportSize({ width, height: 1000 })
    await page.goto('/components/description-list')
    if (scale === 2) await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
    for (const theme of ['Light', 'Dark']) {
      await page.getByRole('button', { name: 'Choose color theme' }).click()
      await page.getByRole('menuitemradio', { name: theme, exact: true }).click()
      await expect.poll(() => page.evaluate(() => document.getAnimations().filter(animation => animation instanceof CSSTransition && animation.playState === 'running').length)).toBe(0)
      for (const [id, count, desktopColumns] of examples) {
        const list = page.locator(`#${id} .docs-components-preview dl`)
        const columns = width >= 1280 ? desktopColumns : width >= 640 ? 2 : 1
        expect(await list.evaluate(element => getComputedStyle(element).gridTemplateColumns.split(' ').length)).toBe(columns)
        await expect(list.getByRole('term')).toHaveCount(count)
        await expect(list.getByRole('term').first()).toHaveCSS('font-size', `${12 * scale}px`)
        await expect(list.getByRole('definition').first()).toHaveCSS('font-size', `${14 * scale}px`)
        expect(await list.evaluate(element => [element, ...element.querySelectorAll('dt, dd')].every(node => node.scrollWidth <= node.clientWidth + 1))).toBe(true)
        if (columns === 1) {
          const boxes = await list.getByRole('term').evaluateAll(elements => elements.map(element => element.getBoundingClientRect().top))
          expect(boxes.every((top, index) => index === 0 || top > boxes[index - 1])).toBe(true)
        }
      }
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
      expect((await new AxeBuilder({ page }).include('.docs-gallery-layout').analyze()).violations).toEqual([])
      if (width !== 800) await page.screenshot({ path: testInfo.outputPath(`description-list-${theme.toLowerCase()}-${width}-${scale}x.png`) })
    }
  }
})

test('composed account and transaction details inherit the same field styling @cross-browser', async ({ page }) => {
  for (const width of [1440, 390]) {
    await page.setViewportSize({ width, height: 1000 })
    for (const route of ['/components/detail', '/components/page-examples/account-management?destination=ledger-account-2048', '/components/page-examples/account-management?destination=ledger-transaction-201']) {
      await page.goto(route)
      const detail = page.locator('.docs-components-preview').getByRole('region', { name: 'Detail', exact: true })
      await expect(detail.getByRole('heading', { name: 'Detail', exact: true })).toHaveCount(1)
      const list = detail.locator('dl')
      await expect(list.getByRole('term').first()).toHaveCSS('text-transform', 'uppercase')
      await expect(list.getByRole('term').first()).toHaveCSS('font-size', '12px')
      await expect(list.getByRole('definition').first()).toHaveCSS('font-size', '14px')
      await expect(list.getByRole('definition').first()).toHaveCSS('font-weight', '400')
      expect(await list.evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true)
    }
  }
})
