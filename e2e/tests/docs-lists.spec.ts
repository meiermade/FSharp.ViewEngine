import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

for (const [path, text, marker] of [
  ['/components/interaction-and-server-state', 'Keep product routes', 'disc'],
  ['/docs/previews/sections--prose--and-lists', 'Add the package', 'decimal'],
]) {
  test(`Docs ${marker} markers survive Preflight in light, dark, narrow and resized layouts @cross-browser`, async ({ page }, testInfo) => {
    const errors: string[] = []
    page.on('pageerror', error => errors.push(error.message))
    await page.goto(path)
    const list = page.getByRole('list').filter({ hasText: text })
    await expect(list).toHaveCount(1)
    await list.evaluate(element => { element.id = 'list-marker-under-test' })
    for (const width of [1400, 390]) {
      await page.setViewportSize({ width, height: 1000 })
      for (const theme of ['Light', 'Dark']) {
        await page.getByRole('button', { name: 'Choose color theme' }).click()
        await page.getByRole('menuitemradio', { name: theme, exact: true }).click()
        await expect(list).toHaveCSS('list-style-type', marker)
        await expect(list).toHaveCSS('list-style-position', 'outside')
        for (const item of await list.getByRole('listitem').all()) {
          await expect(item).toHaveCSS('display', 'list-item')
          await expect(item).toHaveCSS('list-style-type', marker)
        }
        expect((await new AxeBuilder({ page }).include('#list-marker-under-test').analyze()).violations).toEqual([])
        await list.screenshot({ path: testInfo.outputPath(`${marker}-${theme.toLowerCase()}-${width}.png`) })
      }
    }
    await page.setViewportSize({ width: 320, height: 1000 })
    await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
    await expect(list).toHaveCSS('list-style-type', marker)
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    const geometry = await list.evaluate(el => ({
      left: el.getBoundingClientRect().left,
      right: el.getBoundingClientRect().right,
      padding: parseFloat(getComputedStyle(el).paddingLeft),
      font: parseFloat(getComputedStyle(el).fontSize),
    }))
    expect(geometry.left).toBeGreaterThanOrEqual(0)
    expect(geometry.right).toBeLessThanOrEqual(321)
    expect(geometry.padding).toBeGreaterThanOrEqual(geometry.font)
    expect(errors).toEqual([])
  })
}
