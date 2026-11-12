import { test, expect } from '../fixture'

test('HTML presentations retain their frame, numbering and accessible canvas across themes and sizes @cross-browser', async ({ page }) => {
  await page.goto('/examples/presentation')
  await expect(page.getByRole('heading', { level: 1, name: 'Geldos · Treasury review', exact: true })).toBeVisible()
  const slides = page.locator('[data-fve-slide]')
  await expect(slides).toHaveCount(5)
  await expect(page.locator('[data-fve-slide-number]')).toHaveText(['01', '02', '03', '04', '05'])
  for (const mode of ['light', 'dark']) {
    await page.evaluate(mode => (window as any).fveColorMode.set(mode), mode)
    for (const layout of [{ width: 1440, font: '100%' }, { width: 390, font: '100%' }, { width: 390, font: '200%' }]) {
      await page.setViewportSize({ width: layout.width, height: 1000 })
      await page.evaluate(font => { document.documentElement.style.fontSize = font }, layout.font)
      const frames = await slides.evaluateAll(elements => elements.map(slide => {
        const box = slide.getBoundingClientRect()
        const header = slide.querySelector(':scope > header')!.getBoundingClientRect()
        const body = slide.querySelector(':scope > div')!
        const content = body.getBoundingClientRect()
        const footer = slide.querySelector(':scope > footer')!.getBoundingClientRect()
        return { ratio: box.width / box.height, headerEnd: header.bottom, contentStart: content.top, contentEnd: content.bottom, footerStart: footer.top, footerEnd: footer.bottom, end: box.bottom, clipped: body.scrollHeight > body.clientHeight + 1 }
      }))
      for (const frame of frames) {
        expect(frame.ratio).toBeCloseTo(16 / 9, 2)
        expect(frame.headerEnd).toBeLessThanOrEqual(frame.contentStart + 1)
        expect(frame.contentEnd).toBeLessThanOrEqual(frame.footerStart + 1)
        expect(frame.footerEnd).toBeLessThanOrEqual(frame.end + 1)
        expect(frame.clipped).toBe(false)
      }
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true)
      const viewport = page.locator('[data-fve-slide-viewport]').first()
      if (layout.width < 768) {
        await viewport.evaluate(el => { el.scrollLeft = 0 })
        await viewport.focus()
        // macOS WebKit can cancel native scrolling on a brief key tap.
        await page.keyboard.down('ArrowRight')
        try { await expect.poll(() => viewport.evaluate(el => el.scrollLeft)).toBeGreaterThan(0) }
        finally { await page.keyboard.up('ArrowRight') }
      }
      await viewport.evaluate(el => { el.scrollLeft = el.scrollWidth })
      const visible = (await viewport.boundingBox())!, number = (await slides.first().locator('[data-fve-slide-number]').boundingBox())!
      expect(number.x + number.width).toBeLessThanOrEqual(visible.x + visible.width + 1)
      expect(number.x).toBeGreaterThanOrEqual(visible.x)
    }
  }
})

test('Presentation viewer fits existing slides, supports keyboard controls and restores its page @cross-browser', async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/examples/presentation')
  const deck = page.locator('fve-presentation')
  const position = deck.getByRole('status')
  const expand = deck.getByRole('button', { name: 'Expand presentation', exact: true })
  const restore = deck.getByRole('button', { name: 'Restore presentation', exact: true })
  const next = deck.getByRole('button', { name: 'Next slide', exact: true })
  const previous = deck.getByRole('button', { name: 'Previous slide', exact: true })
  await expect(expand).toBeVisible()
  await expect(previous).toBeHidden()
  await expect(next).toBeHidden()
  await expect(position).toBeHidden()
  await page.locator('#cash-position > div').evaluate(el => {
    const note = document.createElement('input')
    note.setAttribute('aria-label', 'Review note')
    note.value = 'Keep this draft'
    el.append(note)
    ;(window as any).originalCashSlide = document.querySelector('#cash-position')
  })
  await page.locator('#cash-position').scrollIntoViewIfNeeded()
  await expand.click()
  await expect(position).toHaveText('2 of 5')
  await expect(previous).toBeVisible()
  await expect(next).toBeVisible()
  await expect(deck).toHaveAttribute('role', 'dialog')
  await expect(deck.locator('[data-fve-slide-viewport]:visible')).toHaveCount(1)
  await expect(restore).toBeFocused()
  await next.click()
  await expect(position).toHaveText('3 of 5')
  await expect(next).toBeFocused()
  await previous.click()
  await expect(position).toHaveText('2 of 5')
  await expect(previous).toBeFocused()
  await page.keyboard.press('Shift+Tab')
  await expect(deck.getByRole('textbox', { name: 'Review note' })).toBeFocused()
  await page.keyboard.press('Tab')
  await expect(previous).toBeFocused()
  await page.keyboard.press('End')
  await expect(position).toHaveText('5 of 5')
  await expect(next).toBeDisabled()
  await page.keyboard.press('ArrowRight')
  await expect(position).toHaveText('5 of 5')
  await page.keyboard.press('Home')
  await expect(previous).toBeDisabled()
  await page.keyboard.press('ArrowRight')
  await expect(position).toHaveText('2 of 5')
  await deck.getByRole('textbox', { name: 'Review note' }).focus()
  await page.keyboard.press('ArrowRight')
  await expect(position).toHaveText('2 of 5')
  for (const width of [1440, 390]) {
    await page.setViewportSize({ width, height: 800 })
    await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
    await expect.poll(() => page.locator('#cash-position').evaluate(el => {
      const r = el.getBoundingClientRect()
      return r.left >= -1 && r.right <= innerWidth + 1 && r.top >= 0 && r.bottom <= innerHeight + 1 && Math.abs(r.width / r.height - 16 / 9) < .01
    })).toBe(true)
    await expect.poll(async () => {
      const slide = (await page.locator('#cash-position').boundingBox())!
      const button = (await restore.boundingBox())!
      return Math.abs(slide.x + slide.width - button.x - button.width)
    }).toBeLessThanOrEqual(1)
    const slide = (await page.locator('#cash-position').boundingBox())!
    const restored = (await restore.boundingBox())!
    const gap = await page.evaluate(() => parseFloat(getComputedStyle(document.documentElement).fontSize))
    expect(slide.y - restored.y - restored.height).toBeCloseTo(gap, 0)
    const controls = await deck.locator('[data-fve-presentation-controls]').boundingBox()
    expect(controls!.x).toBeGreaterThanOrEqual(0)
    expect(controls!.x + controls!.width).toBeLessThanOrEqual(width + 1)
    const boxes = await Promise.all([previous, next, restore].map(button => button.boundingBox()))
    for (const box of boxes.slice(1)) {
      expect(box!.y).toBeCloseTo(boxes[0]!.y, 1)
      expect(box!.height).toBeCloseTo(boxes[0]!.height, 1)
    }
  }
  await page.keyboard.press('Escape')
  await expect(deck).toHaveAttribute('role', 'group')
  await expect(deck.locator('[data-fve-slide-viewport]:visible')).toHaveCount(5)
  await expect(expand).toBeFocused()
  await expect(previous).toBeHidden()
  await expect(next).toBeHidden()
  await expect(position).toBeHidden()
  await expect(deck.getByRole('textbox', { name: 'Review note' })).toHaveValue('Keep this draft')
  expect(await page.evaluate(() => (window as any).originalCashSlide === document.querySelector('#cash-position'))).toBe(true)
  await expand.click()
  await restore.click()
  await expect(expand).toBeFocused()
  await expand.click()
  await deck.evaluate(el => el.remove())
  await expect.poll(() => page.evaluate(() => document.body.style.overflow)).toBe('')
  await page.getByRole('link', { name: 'Application', exact: true }).click()
  await expect(page).toHaveURL('/examples/application')
  expect(errors).toEqual([])
})

test('Presentation print layout uses full-size pages without host chrome or scrolling @cross-browser', async ({ browser }) => {
  const context = await browser.newContext({ javaScriptEnabled: false, viewport: { width: 390, height: 800 } })
  try {
    const page = await context.newPage()
    await page.goto('/examples/presentation')
    await page.emulateMedia({ media: 'print' })
    await expect(page.getByRole('heading', { level: 1, name: 'Geldos · Treasury review', exact: true })).toBeHidden()
    await expect(page.locator('[data-fve-presentation-controls]')).toBeHidden()
    const frames = await page.locator('[data-fve-slide]').evaluateAll(elements => elements.map(el => {
      const box = el.getBoundingClientRect(), frame = el.closest('[data-fve-slide-frame]')!, body = el.children[1]
      return { x: box.x, y: box.y, width: box.width, height: box.height, transform: getComputedStyle(frame).transform,
        shadow: getComputedStyle(frame).boxShadow, overflow: getComputedStyle(body).overflowY,
        clipped: body.scrollHeight > body.clientHeight + 1 }
    }))
    expect(frames).toHaveLength(5)
    for (const [index, frame] of frames.entries()) {
      expect(frame.x).toBe(0)
      expect(frame.y).toBeCloseTo(index * 720, 0)
      expect(frame.width).toBe(1280)
      expect(frame.height).toBe(720)
      expect(frame.transform).toBe('none')
      expect(frame.shadow).toBe('none')
      expect(frame.overflow).toBe('visible')
      expect(frame.clipped).toBe(false)
    }
    await page.emulateMedia({ media: 'screen' })
    await expect(page.getByRole('heading', { level: 1, name: 'Geldos · Treasury review', exact: true })).toBeVisible()
  } finally { await context.close() }
})

test('Standalone slides print at the page origin even inside a padded scrolling host @cross-browser', async ({ browser }) => {
  const context = await browser.newContext({ javaScriptEnabled: false })
  try {
    const page = await context.newPage()
    await page.goto('/components/slide')
    await page.evaluate(() => {
      const slide = document.querySelector('[data-fve-slide-viewport]')!
      const host = document.createElement('div')
      host.style.cssText = 'width:320px;height:240px;padding:32px;overflow:auto'
      host.append(slide)
      document.body.replaceChildren(host)
      document.body.style.cssText = 'margin:8px;padding:24px'
    })
    await page.emulateMedia({ media: 'print' })
    const box = (await page.locator('[data-fve-slide]').boundingBox())!
    expect(box).toEqual({ x: 0, y: 0, width: 1280, height: 720 })
  } finally { await context.close() }
})

test('PDF printing includes every slide and restores the expanded viewer and focus', async ({ page }) => {
  await page.goto('/examples/presentation')
  await page.evaluate(() => document.fonts.ready)
  await page.getByRole('button', { name: 'Expand presentation', exact: true }).click()
  const next = page.getByRole('button', { name: 'Next slide', exact: true })
  await next.click()
  await expect(page.getByRole('status')).toHaveText('2 of 5')
  const pdf = await page.pdf({ preferCSSPageSize: true, printBackground: true, displayHeaderFooter: false, tagged: true, outline: true })
  expect(pdf.subarray(0, 5).toString()).toBe('%PDF-')
  expect(pdf.toString('latin1').match(/\/Type\s*\/Page\b/g)).toHaveLength(5)
  await expect(page.locator('fve-presentation')).toHaveAttribute('role', 'dialog')
  await expect(page.getByRole('status')).toHaveText('2 of 5')
  await expect(next).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(page.getByRole('button', { name: 'Expand presentation', exact: true })).toBeVisible()
  await page.evaluate(() => {
    const slide = document.querySelector('[data-fve-slide-viewport]')!
    document.body.replaceChildren(slide)
    document.body.style.padding = '24px'
  })
  const standalone = await page.pdf({ preferCSSPageSize: true, printBackground: true })
  expect(standalone.toString('latin1').match(/\/Type\s*\/Page\b/g)).toHaveLength(1)
})

test('Slides render without JavaScript and surplus body content remains scrollable @cross-browser', async ({ browser }) => {
  const context = await browser.newContext({ javaScriptEnabled: false, viewport: { width: 1440, height: 1000 } })
  try {
    const page = await context.newPage()
    await page.goto('/examples/presentation')
    const slide = page.locator('#cash-position')
    await expect(slide.getByRole('heading', { name: 'Start with the current position.', exact: true })).toBeVisible()
    await expect(slide.locator('[data-fve-slide-number]')).toHaveText('02')
    const body = slide.locator(':scope > div')
    await slide.getByRole('heading', { level: 2 }).evaluate(el => { el.textContent = 'Review the current position against the decisions, constraints and commitments that shape the next quarter.' })
    await slide.locator('header p').evaluate(el => { el.textContent = 'Keep a clear title and supporting context while sharing the same account model, reporting period and financial assumptions across every part of the conversation.' })
    await slide.locator('footer > div').evaluate(el => { el.textContent += ' · Prepared for the treasury review using the same recorded balances and activity as the application. Assumptions and decisions remain consumer-owned.' })
    const regions = await slide.evaluate(el => {
      const body = el.children[1].getBoundingClientRect()
      return { header: el.querySelector('header')!.getBoundingClientRect().bottom, top: body.top, height: body.height, bottom: body.bottom, footer: el.querySelector('footer')!.getBoundingClientRect().top }
    })
    expect(regions.header).toBeLessThanOrEqual(regions.top + 1)
    expect(regions.height).toBeGreaterThan(0)
    expect(regions.bottom).toBeLessThanOrEqual(regions.footer + 1)
    await body.evaluate(el => {
      for (let i = 0; i < 40; i++) { const p = document.createElement('p'); p.textContent = `Additional note ${i + 1}`; el.append(p) }
    })
    await expect(body).toHaveCSS('overflow-y', 'auto')
    expect(await body.evaluate(el => el.scrollHeight > el.clientHeight)).toBe(true)
    await body.scrollIntoViewIfNeeded()
    await body.evaluate(el => { el.scrollTop = el.scrollHeight })
    await expect(body.getByText('Additional note 40', { exact: true })).toBeInViewport()
    await expect(slide.locator(':scope > footer')).toContainText('Source: Geldos example ledger')
  } finally { await context.close() }
})
