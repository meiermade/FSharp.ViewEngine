import { test, expect } from '@playwright/test'

const paint = (el: Element) => {
  const s = getComputedStyle(el)
  return { image: s.backgroundImage, background: s.backgroundColor, color: s.color, opacity: s.opacity }
}

test('neutral presentation actions highlight without fading their contents on an active dark surface @cross-browser', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark' })
  await page.goto('/examples/presentation')
  await page.getByRole('button', { name: 'Expand presentation', exact: true }).click()
  const next = page.getByRole('button', { name: 'Next slide', exact: true })
  await page.mouse.move(0, 0)
  const normal = await next.evaluate(paint)
  await next.hover()
  await expect.poll(() => next.evaluate(el => getComputedStyle(el).backgroundImage)).not.toBe('none')
  expect(await next.evaluate(paint)).toMatchObject({ background: normal.background, color: normal.color, opacity: normal.opacity })
  const hovered = await next.evaluate(el => getComputedStyle(el).backgroundImage)
  await page.mouse.down()
  const pressed = await next.evaluate(el => getComputedStyle(el).backgroundImage)
  expect(pressed).not.toBe(hovered)
  await page.mouse.move(0, 0)
  await page.mouse.up()
  const previous = page.getByRole('button', { name: 'Previous slide', exact: true })
  await expect(previous).toBeDisabled()
  await previous.hover({ force: true })
  await expect(previous).toHaveCSS('background-image', 'none')
})

test('native selection controls and disclosures add feedback without obscuring state @cross-browser', async ({ page }) => {
  for (const [route, selector] of [['checkbox', '#emailUpdates'], ['radio-group', '#contact-phone']]) {
    await page.goto('/components/' + route)
    const input = page.locator(selector)
    if (route === 'checkbox') await input.uncheck()
    await page.mouse.move(0, 0)
    const border = await input.evaluate(el => getComputedStyle(el).borderColor)
    await input.hover()
    await expect.poll(() => input.evaluate(el => getComputedStyle(el).borderColor)).not.toBe(border)
    await input.check()
    await expect(input).toBeChecked()
    const image = await input.evaluate(el => getComputedStyle(el).backgroundImage)
    await page.mouse.move(0, 0)
    await input.hover()
    await expect(input).toHaveCSS('background-image', image)
    await expect(input).toHaveCSS('opacity', '1')
  }
  await page.goto('/components/switch')
  const toggle = page.locator('#productUpdates'), track = page.locator('#productUpdates-host > span')
  await toggle.uncheck()
  await page.mouse.move(0, 0)
  const ring = await track.evaluate(el => getComputedStyle(el).boxShadow)
  await toggle.hover()
  await expect.poll(() => track.evaluate(el => getComputedStyle(el).boxShadow)).not.toBe(ring)
  await toggle.check()
  await expect(toggle).toBeChecked()
  await page.goto('/components/choice-cards')
  const card = page.locator('#components-choice-cards-default-panel-preview label').last()
  await card.hover()
  await expect.poll(() => card.evaluate(el => getComputedStyle(el).backgroundImage)).not.toBe('none')
  await card.click()
  await expect(card.locator('input')).toBeChecked()
  await page.goto('/components/disclosure')
  const summary = page.locator('#components-disclosure-default-panel-preview summary').first()
  await summary.hover()
  await expect(summary).not.toHaveCSS('background-image', 'none')
  await summary.click()
  await expect(summary.locator('..')).toHaveAttribute('open', '')
})

test('independent controls retain their base and selected surfaces while adding hover feedback @cross-browser', async ({ page }) => {
  for (const mode of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: mode })
    for (const [route, selector] of [
      ['select', '#components-select-default-panel-preview [data-fve-select-trigger-control]'],
      ['tabs', '#components-tabs-panel-preview [role=tab][aria-selected=false]'],
      ['tabs', '#components-tabs-panel-preview [role=tab][aria-selected=true]'],
      ['toggle-button', '#components-toggle-button-panel-preview fve-toggle-button button'],
      ['button', '[data-docs-copy-button]'],
    ]) {
      await page.goto('/components/' + route)
      const control = route === 'button'
        ? page.getByRole('button', { name: 'Copy code', exact: true }).first()
        : page.locator(selector).first()
      await control.scrollIntoViewIfNeeded()
      await page.mouse.move(0, 0)
      const normal = await control.evaluate(paint)
      await control.hover()
      await expect.poll(() => control.evaluate(el => getComputedStyle(el).backgroundImage), `${mode}: ${route} ${selector}`).not.toBe('none')
      expect(await control.evaluate(paint)).toMatchObject({ background: normal.background, opacity: normal.opacity })
      await control.evaluate(el => el.setAttribute('disabled', ''))
      await expect(control).toHaveCSS('background-image', 'none')
    }
  }
})
