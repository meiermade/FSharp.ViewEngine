import { test, expect, type Page } from '@playwright/test'

async function openGallery(page: Page, slug: string) {
  const link = page.locator(`#nav-components-${slug}`)
  const parents = await link.evaluate(el => {
    const ids: string[] = []
    for (let node = el.parentElement; node; node = node.parentElement) {
      if (node.id.startsWith('nav-children-')) ids.unshift(node.id)
    }
    return ids
  })
  for (const id of parents) {
    const button = page.locator(`button[aria-controls="${id}"]`)
    if (await button.getAttribute('aria-expanded') === 'false') await button.click()
  }
  const response = page.waitForResponse(response => new URL(response.url()).pathname === `/components/${slug}` && response.request().headers()['datastar-request'] === 'true')
  await link.click()
  const result = await response
  expect(result.status(), `navigation to ${slug}`).toBe(200)
  expect(result.url().length, `bounded URL for ${slug}`).toBeLessThan(2000)
  expect(new URL(result.url()).searchParams.get('datastar'), `navigation excludes demo state for ${slug}`).toBe('{}')
  await expect(page).toHaveURL(`/components/${slug}`)
}

test('long-lived Docs navigation excludes demo signals and preserves Preview Code Copy after morphs @cross-browser', async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/button')
  await page.evaluate(() => { (window as any).__retainedCalendarSession = 'same-document' })
  await page.evaluate(() => {
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: async (value: string) => { (window as any).__copiedExample = value } } })
  })
  const galleries = ['select', 'calendar', 'dropdown-menu', 'dialog', 'app-shell', 'form-layouts', 'media-library', 'notice']
  for (const slug of galleries) {
    await openGallery(page, slug)
    const example = page.locator('[data-docs-example="true"]').first()
    const toolbar = example.locator(':scope > [data-docs-example-toolbar="true"]')
    await toolbar.getByRole('tab', { name: 'Code', exact: true }).click()
    const panel = example.getByRole('tabpanel', { name: 'Code', exact: true })
    await expect(panel).toBeVisible()
    await expect.poll(async () => (await panel.innerText()).trim().length).toBeGreaterThan(20)
    const previewTab = toolbar.getByRole('tab', { name: 'Preview', exact: true })
    const previewId = await previewTab.getAttribute('aria-controls')
    expect(previewId).toBeTruthy()
    await previewTab.click()
    await expect(page.locator(`#${previewId}`)).toBeVisible()
    expect(await page.evaluate(() => (window as any).__retainedCalendarSession)).toBe('same-document')
  }
  // All Notice examples must still be independently operable after the accumulated session.
  for (const example of await page.locator('[data-docs-example="true"]').all()) {
    const toolbar = example.locator(':scope > [data-docs-example-toolbar="true"]')
    await toolbar.getByRole('tab', { name: 'Code', exact: true }).click()
    const panel = example.getByRole('tabpanel', { name: 'Code', exact: true })
    await panel.getByRole('button', { name: /^Copy / }).click()
    await expect(panel.locator('[data-copied="true"]')).toHaveCount(1)
    expect(await page.evaluate(() => (window as any).__copiedExample)).toContain('Notice.create')
    const previewTab = toolbar.getByRole('tab', { name: 'Preview', exact: true })
    const previewId = await previewTab.getAttribute('aria-controls')
    expect(previewId).toBeTruthy()
    await previewTab.click()
    await expect(page.locator(`#${previewId}`)).toBeVisible()
  }
  await page.goBack()
  await expect(page).toHaveURL('/components/media-library')
  await page.goForward()
  await expect(page).toHaveURL('/components/notice')
  expect(await page.evaluate(() => (window as any).__retainedCalendarSession)).toBe('same-document')
  expect(errors).toEqual([])
})
