import { test, expect, type Locator } from '@playwright/test'

// Compare resolved CSS colors rather than browser-specific rgb/oklch serialization.
async function usesToken(element: Locator, property: 'backgroundColor' | 'color', token: string) {
  return element.evaluate((node, { property, token }) => {
    const probe = document.createElement('span')
    probe.style[property] = `var(${token})`
    node.append(probe)
    const matches = getComputedStyle(node)[property] === getComputedStyle(probe)[property]
    probe.remove()
    return matches
  }, { property, token })
}

for (const mode of ['light', 'dark'] as const) {
  for (const width of [390, 1440]) {
    test(`theme dropdown inherits header and dock appearance ${mode} ${width}px`, async ({ page }, testInfo) => {
      await page.setViewportSize({ width, height: 900 })
      await page.emulateMedia({ colorScheme: mode })
      await page.goto('/docs/components/fixture')
      const trigger = page.getByRole('button', { name: 'Choose color theme', exact: true })
      const menu = page.getByRole('menu', { name: 'Choose color theme', exact: true })
      await expect(page.locator('[data-docs-top-actions="true"]')).toHaveClass(/fve-theme-sky/)
      await expect(trigger).toHaveText('')
      await expect(trigger).toHaveCSS('width', '32px')
      await expect(trigger.locator('svg:visible')).toHaveCount(1)

      for (const context of ['header', 'dock']) {
        if (context === 'dock') {
          await page.getByRole('link', { name: 'Open Create a view in App mode' }).click()
          const dock = page.getByRole('navigation', { name: 'App mode controls' })
          await expect(dock).toHaveClass(/fve-theme-sky/)
          await expect(dock).toHaveAttribute('data-fve-color-mode', mode === 'dark' ? 'light' : 'dark')
          await expect(dock.getByRole('button', { name: 'Choose color theme' })).toBeVisible()
        }
        await trigger.hover()
        await page.mouse.down()
        await expect.poll(() => usesToken(trigger, 'backgroundColor', '--fve-surface-active')).toBe(true)
        await expect(trigger).toHaveCSS('outline-style', 'none')
        await trigger.screenshot({ path: testInfo.outputPath(`${context}-pressed-${mode}-${width}.png`) })
        await page.mouse.up()
        await expect(menu).toBeVisible()
        await expect.poll(() => usesToken(menu, 'backgroundColor', '--fve-surface')).toBe(true)
        const selected = menu.getByRole('menuitemradio', { name: 'System', exact: true })
        const light = menu.getByRole('menuitemradio', { name: 'Light', exact: true })
        await expect(selected).toHaveAttribute('aria-checked', 'true')
        await expect(selected).toHaveCSS('background-color', 'rgba(0, 0, 0, 0)')
        await selected.hover()
        await expect(selected).toBeFocused()
        const darkSurface = context === 'header' ? mode === 'dark' : mode === 'light'
        if (darkSurface) {
          await expect.poll(() => usesToken(selected, 'backgroundColor', '--fve-brand-text')).toBe(true)
          await expect.poll(() => usesToken(selected, 'color', '--fve-surface')).toBe(true)
        } else {
          await expect.poll(() => usesToken(selected, 'backgroundColor', '--fve-brand-solid')).toBe(true)
          await expect(selected).toHaveCSS('color', 'rgb(255, 255, 255)')
        }
        const selectedStyle = await selected.evaluate(node => ({ bg: getComputedStyle(node).backgroundColor, fg: getComputedStyle(node).color }))
        await light.hover()
        await expect(light).toBeFocused()
        await expect.poll(() => light.evaluate(node => ({ bg: getComputedStyle(node).backgroundColor, fg: getComputedStyle(node).color }))).toEqual(selectedStyle)
        await expect(selected).toHaveAttribute('aria-checked', 'true')
        await expect(selected).toHaveCSS('background-color', 'rgba(0, 0, 0, 0)')
        await page.screenshot({ path: testInfo.outputPath(`${context}-${mode}-${width}.png`) })
        // Enter keyboard navigation before testing keyboard-visible focus return;
        // Firefox preserves pointer-origin focus when only dismissing a hovered item.
        await light.press('ArrowUp')
        await expect(selected).toBeFocused()
        await selected.press('Escape')
        await expect(trigger).toBeFocused()
        await expect(trigger).toHaveCSS('outline-style', 'solid')
        await expect(trigger).toHaveCSS('outline-width', '2px')
        await trigger.press('ArrowDown')
        await expect(selected).toBeFocused()
        await selected.press('End')
        await expect(menu.getByRole('menuitemradio', { name: 'Dark', exact: true })).toBeFocused()
        await page.keyboard.press('Home')
        await page.keyboard.press('Enter')
        await expect(menu).not.toBeVisible()
        await expect(trigger).toBeFocused()
      }
      await page.getByRole('link', { name: 'Exit App mode', exact: true }).click()
      await expect(page.locator('[data-docs-top-actions="true"]').getByRole('button', { name: 'Choose color theme' })).toBeVisible()
      await expect(trigger).toHaveCount(1)
    })
  }
}
