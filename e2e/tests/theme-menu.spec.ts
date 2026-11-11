import { test, expect } from '../fixture'
import type { Locator } from '@playwright/test'

async function expectCenteredIcon(control: Locator) {
  await expect(control).toHaveCSS('width', '32px')
  await expect(control).toHaveCSS('height', '32px')
  await expect.poll(() => control.evaluate(el => {
    const icon = [...el.querySelectorAll('svg')].find(svg => svg.getBoundingClientRect().width > 0)!
    const button = el.getBoundingClientRect(), bounds = icon.getBoundingClientRect()
    return Math.max(Math.abs(bounds.x + bounds.width / 2 - button.x - button.width / 2), Math.abs(bounds.y + bounds.height / 2 - button.y - button.height / 2))
  })).toBeLessThanOrEqual(0.5)
}

for (const [context, path] of [
  ['catalog', '/components/button'],
  ['Specification', '/examples/specification'],
  ['API documentation', '/examples/api-documentation'],
]) {
  test(`${context} theme menu retains keyboard selection and focus @cross-browser`, async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 900 })
    await page.emulateMedia({ colorScheme: 'light' })
    await page.goto(path)
    const trigger = page.getByRole('button', { name: 'Choose color theme', exact: true })
    await expectCenteredIcon(trigger)
    const repository = page.getByRole('link', { name: context === 'catalog' ? 'View repository on GitHub' : 'View example source on GitHub', exact: true })
    await expectCenteredIcon(repository)
    await trigger.focus()
    await page.keyboard.press('ArrowDown')
    const menu = page.getByRole('menu', { name: 'Choose color theme', exact: true })
    const system = menu.getByRole('menuitemradio', { name: 'System', exact: true })
    await expect(system).toHaveAttribute('aria-checked', 'true')
    await expect(system).toBeFocused()
    await expect(trigger.locator('svg:visible')).toHaveCount(1)
    await page.keyboard.press('End')
    await expect(menu.getByRole('menuitemradio', { name: 'Dark', exact: true })).toBeFocused()
    await page.keyboard.press('Enter')
    await expect(menu).toBeHidden()
    await expect(trigger).toBeFocused()
    await expect(page.locator('html')).toHaveClass(/dark/)
    await expect(trigger.locator('svg:visible')).toHaveCount(1)
    await expectCenteredIcon(trigger)
    await trigger.press('ArrowDown')
    await expect(menu.getByRole('menuitemradio', { name: 'Dark', exact: true })).toHaveAttribute('aria-checked', 'true')
    await page.keyboard.press('Escape')
    await expect(trigger).toBeFocused()
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
  })
}
