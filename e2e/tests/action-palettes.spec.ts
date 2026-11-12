import { test, expect } from '@playwright/test'

// Flat control fills, including the translucent image wash, composited over their real ancestors.
const paint = (element: Element) => {
  const canvas = document.createElement('canvas'), ctx = canvas.getContext('2d', { willReadFrequently: true })!
  canvas.width = canvas.height = 1
  const chain: Element[] = []
  for (let node: Element | null = element; node; node = node.parentElement) chain.unshift(node)
  ctx.fillStyle = '#fff'; ctx.fillRect(0, 0, 1, 1)
  for (const node of chain) {
    const style = getComputedStyle(node)
    ctx.fillStyle = style.backgroundColor; ctx.fillRect(0, 0, 1, 1)
    if (style.backgroundImage.startsWith('linear-gradient(')) {
      const body = style.backgroundImage.slice(16)
      let depth = 0, end = 0
      for (; end < body.length; end++) {
        if (body[end] === '(') depth++
        if (body[end] === ')') depth--
        if (body[end] === ',' && depth === 0) break
      }
      ctx.fillStyle = body.slice(0, end); ctx.fillRect(0, 0, 1, 1)
    }
  }
  const background = [...ctx.getImageData(0, 0, 1, 1).data]
  ctx.clearRect(0, 0, 1, 1)
  const style = getComputedStyle(element)
  ctx.fillStyle = style.color; ctx.fillRect(0, 0, 1, 1)
  return { background, foreground: [...ctx.getImageData(0, 0, 1, 1).data], opacity: style.opacity, width: element.getBoundingClientRect().width, height: element.getBoundingClientRect().height }
}

const luminance = (rgb: number[]) => rgb.slice(0, 3).map(c => {
  c /= 255
  return c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4
}).reduce((sum, c, i) => sum + c * [.2126, .7152, .0722][i], 0)
const contrast = (state: ReturnType<typeof paint>) => {
  const [a, b] = [luminance(state.background), luminance(state.foreground)].sort((a, b) => b - a)
  return (a + .05) / (b + .05)
}
const difference = (a: ReturnType<typeof paint>, b: ReturnType<typeof paint>) => Math.max(...(['background', 'foreground'] as const).flatMap(key => a[key].slice(0, 3).map((value, i) => Math.abs(value - b[key][i]))))

for (const kind of ['button', 'link'] as const) for (const mode of ['light', 'dark'] as const) {
  test(`${kind} every built-in color and variant has readable, distinguishable ${mode} interaction states @cross-browser`, async ({ page }) => {
    await page.emulateMedia({ colorScheme: mode })
    await page.goto('/components/' + kind)
    const panel = page.locator(`#components-${kind}-color-variants-panel-preview`)
    const controls = panel.getByRole(kind === 'button' ? 'button' : 'link')
    await expect(controls).toHaveCount(kind === 'button' ? 28 : 35)
    for (const control of await controls.all()) {
      const label = await control.getAttribute('aria-label')
      const settle = () => control.evaluate(async el => { await Promise.all(el.getAnimations().map(a => a.finished.catch(() => {}))) })
      await control.scrollIntoViewIfNeeded()
      await page.mouse.move(0, 0); await settle()
      const normal = await control.evaluate(paint)
      await control.hover(); await settle()
      const hovered = await control.evaluate(paint)
      await page.mouse.down(); await settle()
      const pressed = await control.evaluate(paint)
      await page.mouse.move(0, 0); await page.mouse.up()
      for (const state of [normal, hovered, pressed]) {
        expect(contrast(state), `${label} contrast`).toBeGreaterThanOrEqual(4.5)
        expect(state.opacity, `${label} content stays opaque`).toBe('1')
        expect(state.width).toBeCloseTo(normal.width, 1)
        expect(state.height).toBeCloseTo(normal.height, 1)
      }
      // A guard against the old 3–4-channel-level change; rendered comparison is also reviewed.
      expect(difference(normal, hovered), `${label} visible hover change`).toBeGreaterThanOrEqual(12)
      expect(difference(hovered, pressed), `${label} stronger pressed change`).toBeGreaterThanOrEqual(8)
    }
  })
}
