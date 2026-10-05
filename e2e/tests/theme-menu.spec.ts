import { test, expect } from '@playwright/test'

for (const [context, path, label] of [
  ['catalog', '/components/button', 'Choose color theme'],
  ['Example viewer', '/examples/application/accounts', 'Choose color theme'],
  ['App mode', '/examples/specification/home?appMode=1', 'Choose theme'],
]) {
  test(`${context} theme menu retains keyboard selection and focus @cross-browser`, async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 900 })
    await page.emulateMedia({ colorScheme: 'light' })
    await page.goto(path)
    const trigger = page.getByRole('button', { name: label, exact: true })
    await trigger.focus()
    await page.keyboard.press('ArrowDown')
    const menu = page.getByRole('menu', { name: label, exact: true })
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
    await trigger.press('ArrowDown')
    await expect(menu.getByRole('menuitemradio', { name: 'Dark', exact: true })).toHaveAttribute('aria-checked', 'true')
    await page.keyboard.press('Escape')
    await expect(trigger).toBeFocused()
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
  })
}
