import { test, expect } from '@playwright/test'

// Clone actual server-authored controls before adding ordinary application callbacks.
// This exercises the library's guards, not a test-owned replacement implementation.
test('readonly guards survive caller click callbacks and once modifiers @cross-browser', async ({ page }) => {
  for (const route of ['checkbox', 'switch', 'radio-group', 'file-selection']) {
    await page.goto(`/components/${route}`)
    const source = page.locator(route === 'file-selection' ? 'input[type=file]' : 'input[readonly]').first()
    await expect(source).toBeAttached()
    await source.evaluate(input => {
      const clone = input.cloneNode(true) as HTMLInputElement
      clone.id = 'readonly-callback-contract'
      clone.name = 'readonlyValue'
      clone.readOnly = true
      clone.checked = true
      clone.style.cssText = 'position:static;opacity:1;width:24px;height:24px'
      clone.setAttribute('data-on:click', "el.dataset.calls = String(Number(el.dataset.calls || '0') + 1)")
      clone.setAttribute('data-on:click__once', "el.dataset.once = String(Number(el.dataset.once || '0') + 1)")
      const form = document.createElement('form')
      form.append(clone)
      document.body.append(form)
    })
    const control = page.locator('#readonly-callback-contract')
    for (let calls = 1; calls <= 2; calls++) {
      const cancelled = await control.evaluate(input => !input.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true })))
      expect(cancelled, route).toBe(true)
      await expect(control).toHaveAttribute('data-calls', String(calls))
      await expect(control).toHaveAttribute('data-once', '1')
      if (route !== 'file-selection') await expect(control).toBeChecked()
    }
    expect(await control.evaluate((input: HTMLInputElement) => input.disabled)).toBe(false)
    if (route !== 'file-selection') {
      expect(await control.evaluate((input: HTMLInputElement) => new FormData(input.form!).has('readonlyValue'))).toBe(true)
    }
  }
})

test('DatePicker and CopyReveal icon controls honor the five inherited size presets @cross-browser', async ({ page }) => {
  const sizes = [
    { height: 24, font: 12, line: 16, block: 4, inline: 8 },
    { height: 28, font: 14, line: 20, block: 4, inline: 8 },
    { height: 32, font: 14, line: 20, block: 6, inline: 10 },
    { height: 36, font: 14, line: 20, block: 8, inline: 12 },
    { height: 40, font: 14, line: 20, block: 10, inline: 14 },
  ]
  for (const [route, preview, selector] of [
    ['date-picker', '#components-date-picker-panel-preview', '[aria-haspopup="dialog"]'],
    ['copy-reveal', '#components-copy-reveal-icons-panel-preview', 'button'],
  ]) {
    await page.goto(`/components/${route}`)
    const region = page.locator(preview)
    const controls = region.locator(selector)
    for (const size of sizes) {
      await region.evaluate((el, size) => {
        const theme = el.querySelector('.fve-components') ?? el
        for (const [name, value] of Object.entries({
          'min-height': size.height, 'font-size': size.font, 'line-height': size.line,
          'padding-block': size.block, 'padding-inline': size.inline,
        })) (theme as HTMLElement).style.setProperty(`--fve-control-${name}`, `${value}px`)
      }, size)
      for (const control of await controls.all()) {
        await expect(control).toHaveCSS('height', `${size.height}px`)
        await expect(control).toHaveCSS('font-size', `${size.font}px`)
        if (route === 'copy-reveal') {
          await expect(control).toHaveCSS('width', `${size.height}px`)
          await expect(control).toHaveCSS('padding', '0px')
        }
      }
    }
  }
})
