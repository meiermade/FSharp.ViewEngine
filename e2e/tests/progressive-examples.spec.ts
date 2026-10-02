import { test, expect } from '@playwright/test'

test('examples expose only preview and code, copy complete source and preserve edits @cross-browser', async ({ page }) => {
  await page.goto('/components/input')
  const step = page.locator('#components-input-help')
  const field = step.getByRole('textbox', { name: 'Email', exact: true })
  await field.fill('review@meiermade.com')
  await expect(step.getByRole('tab')).toHaveText(['Preview', 'Code'])
  const preview = step.getByRole('tab', { name: 'Preview', exact: true })
  await preview.focus()
  await page.keyboard.press('ArrowRight')
  const full = step.getByRole('tab', { name: 'Code', exact: true })
  await expect(full).toBeFocused()
  await expect(step.locator('code .token').first()).toBeVisible()
  const source = await step.locator('[data-docs-copy-source]').textContent()
  await page.evaluate(() => { navigator.clipboard.writeText = async value => { document.body.dataset.copied = value } })
  await step.getByRole('button', { name: 'Copy With help text code' }).click()
  await expect(page.locator('body')).toHaveAttribute('data-copied', source!)
  expect(source).toContain('Input.withDescription')
  expect(source).not.toContain('+     |>')
  await full.focus()
  await page.keyboard.press('Home')
  await expect(preview).toBeFocused()
  await expect(field).toHaveValue('review@meiermade.com')
  await page.keyboard.press('ArrowLeft')
  await expect(full).toBeFocused()

  // Two-tab examples also work after enhanced navigation, not only direct load.
  await page.getByRole('button', { name: 'Toggle Navigation section', exact: true }).click()
  await page.getByRole('link', { name: 'Breadcrumbs', exact: true }).click()
  const breadcrumbStep = page.locator('#components-breadcrumbs-visible')
  await expect(breadcrumbStep.getByRole('tab')).toHaveText(['Preview', 'Code'])
  await breadcrumbStep.getByRole('tab', { name: 'Code', exact: true }).click()
  await expect(breadcrumbStep.locator('[data-docs-copy-source]')).toContainText('Breadcrumbs.withMaxVisibleItems 4')
  await page.setViewportSize({ width: 320, height: 800 })
  await page.evaluate(() => {
    document.documentElement.style.fontSize = '200%'
    document.documentElement.classList.add('dark')
  })
  for (const name of ['Code', 'Preview']) {
    await breadcrumbStep.getByRole('tab', { name, exact: true }).click()
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  }
})

test('content typography formats unstyled HTML without restyling excluded controls', async ({ page }) => {
  await page.goto('/docs/components/content#typography')
  const preview = page.locator('#docs-typography-example-panel-preview')
  const prose = preview.locator('.prose-neutral')
  const input = preview.getByRole('textbox', { name: 'Document title', exact: true })
  await input.fill('A practical guide')
  const measurements = () => preview.evaluate(el => {
    const prose = el.querySelector('.prose-neutral')!
    const input = el.querySelector('input')!
    const table = el.querySelector('.not-prose table')!
    return {
      headingSize: parseFloat(getComputedStyle(prose.querySelector('h3')!).fontSize),
      bodySize: parseFloat(getComputedStyle(prose.querySelector('p')!).fontSize),
      listStyle: getComputedStyle(prose.querySelector('ul')!).listStyleType,
      input: [getComputedStyle(input).fontSize, getComputedStyle(input).padding, getComputedStyle(input).height],
      table: [getComputedStyle(table).fontSize, getComputedStyle(table.querySelector('th')!).padding],
    }
  })
  const styled = await measurements()
  expect(styled.headingSize).toBeGreaterThan(styled.bodySize)
  expect(styled.listStyle).toBe('disc')
  // Compare the component before/after removing only the prose ancestor.
  await prose.evaluate(el => el.classList.remove('prose'))
  const standalone = await measurements()
  expect(standalone.input).toEqual(styled.input)
  expect(standalone.table).toEqual(styled.table)
  await prose.evaluate(el => el.classList.add('prose'))
  await page.evaluate(() => document.documentElement.classList.add('dark'))
  const colors = await preview.evaluate(el => {
    const heading = el.querySelector('h3')!
    return { heading: getComputedStyle(heading).color, input: getComputedStyle(el.querySelector('input')!).color }
  })
  expect(colors.heading).not.toBe('rgb(0, 0, 0)')
  await page.setViewportSize({ width: 320, height: 800 })
  await page.evaluate(() => document.documentElement.style.fontSize = '200%')
  await expect(input).toHaveValue('A practical guide')
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
})
