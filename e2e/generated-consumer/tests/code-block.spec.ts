import { expect, test } from '@playwright/test'

test('installed CodeBlock owns syntax colors and code surfaces without Docs CSS or a Prism theme', async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()) })
  await page.goto('/code')
  const fsharp = page.locator('code.language-fsharp')
  const sql = page.locator('code.language-sql')
  await expect(fsharp.locator('.token.keyword').first()).toBeVisible()
  await expect(sql.locator('.token.keyword').first()).toBeVisible()
  for (const language of ['python', 'typescript', 'bash']) {
    await expect(page.locator(`code.language-${language} .token.keyword`).first()).toBeVisible()
  }
  await expect(page.locator('code.language-text .token')).toHaveCount(0)
  expect(await page.locator('link[rel="stylesheet"]').evaluateAll(links => links.map(link => link.getAttribute('href')))).toEqual(['/output.css'])

  const colors = () => fsharp.evaluate(element => ({
    text: getComputedStyle(element).color,
    keyword: getComputedStyle(element.querySelector('.token.keyword')!).color,
    string: getComputedStyle(element.querySelector('.token.string')!).color,
    surface: getComputedStyle(element.closest('pre')!).backgroundColor,
    page: getComputedStyle(document.body).backgroundColor,
  }))
  const light = await colors()
  expect(light.keyword).not.toBe(light.text)
  expect(light.string).not.toBe(light.text)
  expect(light.string).not.toBe(light.keyword)
  expect(light.surface).not.toBe(light.page)
  expect(light.surface).not.toBe('rgba(0, 0, 0, 0)')
  expect(await sql.locator('.token.keyword').first().evaluate(element => getComputedStyle(element).color)).toBe(light.keyword)

  await page.locator('html').evaluate(element => element.classList.add('dark'))
  const dark = await colors()
  expect(dark.keyword).not.toBe(light.keyword)
  expect(dark.keyword).not.toBe(dark.text)
  expect(dark.string).not.toBe(dark.text)
  expect(dark.surface).not.toBe(dark.page)
  expect(dark.surface).not.toBe(light.surface)

  // A locally light region must not inherit the surrounding document's dark syntax palette.
  await page.locator('body').evaluate(element => { element.dataset.fveColorMode = 'light' })
  expect(await colors()).toEqual(light)
  await page.locator('body').evaluate(element => { delete element.dataset.fveColorMode })

  // Reinsert the actual component markup, as a consumer patch would, then run its public initializer.
  const original = await fsharp.textContent()
  await fsharp.evaluate(element => {
    const wrapper = element.closest('[data-docs-copyable-code]')!
    const replacement = wrapper.cloneNode(true) as HTMLElement
    const code = replacement.querySelector('code')!
    code.textContent = element.textContent
    wrapper.replaceWith(replacement)
    return (window as any).renderCode(replacement)
  })
  await expect(fsharp.locator('.token.keyword').first()).toBeVisible()
  expect(await colors()).toEqual(dark)
  expect(await fsharp.textContent()).toBe(original)
  expect(errors).toEqual([])
})

test('plain code copies with no Prism scripts or stylesheet', async ({ page, context }) => {
  const prismRequests: string[] = []
  const errors: string[] = []
  page.on('request', request => { if (request.url().includes('prism')) prismRequests.push(request.url()) })
  page.on('pageerror', error => errors.push(error.message))
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()) })
  await context.grantPermissions(['clipboard-read', 'clipboard-write'])
  await page.goto('/code/plain')
  const source = 'Build succeeded.\n0 warnings\n0 errors'
  await expect(page.locator('code.language-text')).toHaveText(source)
  await expect(page.locator('.token')).toHaveCount(0)
  const copy = page.getByRole('button', { name: 'Copy code', exact: true })
  await copy.focus()
  await copy.press('Enter')
  await expect(page.getByRole('status')).toHaveText('Copied')
  expect(await page.evaluate(() => navigator.clipboard.readText())).toBe(source)
  expect(prismRequests).toEqual([])
  expect(errors).toEqual([])
})
