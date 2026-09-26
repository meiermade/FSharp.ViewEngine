import { test, expect, type Locator } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

async function expectContactAlignment(form: Locator) {
  const fields = await form.locator('input[name="contactName"], input[name="email"]').evaluateAll(inputs => inputs.map(input => {
    const box = input.getBoundingClientRect()
    const label = document.querySelector(`label[for="${input.id}"]`)!.getBoundingClientRect()
    return { top: box.top, height: box.height, gap: box.top - label.bottom }
  }))
  expect(fields).toHaveLength(2)
  expect(Math.abs(fields[0].top - fields[1].top)).toBeLessThanOrEqual(1)
  for (const field of fields) {
    expect(field.height).toBe(40)
    expect(field.gap).toBe(6)
  }
}

for (const title of ['Contact details in two columns', 'Contact details in sections']) {
  test(`${title} aligns controls before and after asymmetric validation @cross-browser`, async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 1000 })
    await page.goto('/components/form-layouts')
    const form = page.getByRole('form', { name: title, exact: true })
    await expectContactAlignment(form)
    const name = form.getByRole('textbox', { name: 'Contact name', exact: true })
    const email = form.getByRole('textbox', { name: 'Email address', exact: true })
    await name.fill('Andy Meier')
    await email.fill('invalid')
    await form.getByRole('button', { name: 'Validate details', exact: true }).click()
    await expect(email).toHaveAttribute('aria-invalid', 'true')
    await expect(name).toHaveAttribute('aria-invalid', 'false')
    await expectContactAlignment(form)
    await name.fill('')
    await email.fill('andy@fve.meiermade.com')
    await form.getByRole('button', { name: 'Validate details', exact: true }).click()
    await expect(name).toHaveAttribute('aria-invalid', 'true')
    await expect(email).toHaveAttribute('aria-invalid', 'false')
    await expectContactAlignment(form)
    await name.fill('Andy Meier')
    await form.getByRole('button', { name: 'Validate details', exact: true }).click()
    await expect(name).toHaveAttribute('aria-invalid', 'false')
    await expectContactAlignment(form)
    await expect(page.getByRole('heading', { name: 'Search with results', exact: true })).toHaveCount(0)
    await expect(page.locator('[data-docs-example="true"]')).toHaveCount(3)
  })
}

for (const [width, scale] of [[1440, 1], [390, 1], [320, 2]]) {
  test(`mixed field wrappers retain natural geometry at ${width}px ${scale}x text`, async ({ page, request }, testInfo) => {
    await page.setViewportSize({ width, height: 1000 })
    await page.goto('/components/form-layouts')
    const response = await request.get('/components/select')
    expect(response.ok()).toBeTruthy()
    // Compose the actual server-rendered public fields in an ordinary consumer grid.
    // Only the outer layout is supplied here; component markup/styles remain untouched.
    const textareaHeight = await page.evaluate(({ html, scale }) => {
      document.documentElement.style.fontSize = `${100 * scale}%`
      const source = new DOMParser().parseFromString(html, 'text/html')
      const select = (labelText: string) => {
        const label = [...source.querySelectorAll('label')].find(label => label.textContent?.trim() === labelText)!
        return document.importNode(label.parentElement!, true)
      }
      const notes = document.getElementById('contact-notes')!
      const height = notes.getBoundingClientRect().height
      const fields = [document.getElementById('contact-name')!.parentElement!, select('Digest frequency'), notes.parentElement!,
        document.getElementById('contact-email')!.parentElement!, select('Static account'), select('Account with validation')]
      const grid = document.createElement('div')
      grid.dataset.testid = 'mixed-fields'
      grid.className = 'grid gap-4 sm:grid-cols-3 p-4'
      for (const field of fields) {
        const label = field.querySelector(':scope > label')!
        const id = label.getAttribute('for')!
        field.querySelector(`[id="${id}"]`)!.setAttribute('data-alignment-control', 'true')
        grid.append(field)
      }
      const preview = document.querySelector('#components-form-layouts-panel-preview .fve-components')!
      preview.replaceChildren(grid)
      return height
    }, { html: await response.text(), scale })
    const grid = page.getByTestId('mixed-fields')
    for (const dark of [false, true]) {
      await page.evaluate(dark => document.documentElement.classList.toggle('dark', dark), dark)
      const geometry = await grid.locator('[data-alignment-control]').evaluateAll(controls => controls.map(control => {
        const box = control.getBoundingClientRect()
        const label = document.querySelector(`label[for="${control.id}"]`)!.getBoundingClientRect()
        const descriptionId = control.getAttribute('aria-describedby')?.split(' ').find(id => id.endsWith('-description'))
        const description = descriptionId ? document.getElementById(descriptionId) : null
        return { top: box.top, height: box.height, gap: box.top - label.bottom, textarea: control.tagName === 'TEXTAREA',
          descriptionBelow: !description || description.getBoundingClientRect().top >= box.bottom }
      }))
      expect(geometry).toHaveLength(6)
      for (const field of geometry) {
        expect(field.height).toBe(field.textarea ? textareaHeight : 40 * scale)
        expect(field.gap).toBe(6 * scale)
        expect(field.descriptionBelow).toBeTruthy()
      }
      if (width === 1440) {
        expect(new Set(geometry.slice(0, 3).map(field => field.top)).size).toBe(1)
        expect(new Set(geometry.slice(3).map(field => field.top)).size).toBe(1)
      }
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
      expect((await new AxeBuilder({ page }).include('[data-testid="mixed-fields"]').analyze()).violations).toEqual([])
      await grid.screenshot({ path: testInfo.outputPath(`mixed-fields-${width}-${scale}x-${dark ? 'dark' : 'light'}.png`) })
    }
  })
}

for (const slug of ['file-selection', 'tag-input']) {
  test(`${slug} does not stretch its control in a taller form row`, async ({ page }) => {
    await page.goto(`/components/${slug}`)
    const field = page.locator('[data-docs-example="true"]').first().locator('[data-docs-example-preview="true"] label').first().locator('..')
    const sizes = await field.evaluate(field => {
      const label = field.querySelector(':scope > label')!
      const control = document.getElementById(label.getAttribute('for')!)!
      const measure = () => ({ labelHeight: label.getBoundingClientRect().height, controlHeight: control.getBoundingClientRect().height,
        labelTop: label.getBoundingClientRect().top, controlTop: control.getBoundingClientRect().top })
      const before = measure()
      ;(field as HTMLElement).style.minHeight = '400px'
      return { before, after: measure() }
    })
    expect(sizes.after).toEqual(sizes.before)
  })
}
