import { type Page } from '@playwright/test'
import { test, expect } from '../fixture'

async function galleryLink(page: Page, slug: string) {
  const link = page.locator(`#nav-components-${slug}`)
  const parents = await link.evaluate(el => {
    const ids: string[] = []
    for (let node = el.parentElement; node; node = node.parentElement) {
      if (node.tagName === 'DETAILS') ids.unshift(node.querySelector(':scope > summary')!.id)
    }
    return ids
  })
  for (const id of parents) {
    const button = page.locator(`summary[id="${id}"]`)
    if (await button.getAttribute('aria-expanded') === 'false') await button.click()
  }
  return link
}

async function openGallery(page: Page, slug: string) {
  const link = await galleryLink(page, slug)
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
  const galleries = ['select', 'month-calendar', 'dropdown-menu', 'dialog', 'notice']
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
  await expect(page).toHaveURL('/components/dialog')
  await page.goForward()
  await expect(page).toHaveURL('/components/notice')
  expect(await page.evaluate(() => (window as any).__retainedCalendarSession)).toBe('same-document')
  expect(errors).toEqual([])
})

test('Docs navigation preserves exact highlighted and copied source whitespace @cross-browser', async ({ page }) => {
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'clipboard', {
      configurable: true,
      value: { writeText: async (value: string) => { (window as any).__copiedSource = value } },
    })
  })
  const source = async (selector: string) => {
    const code = page.locator(selector).first()
    await expect(code.locator('.token').first()).toHaveCount(1)
    return (await code.textContent())!
  }
  const apiSelector = '#api-reference [data-docs-copy-source="true"]'
  const exampleSelector = '#components-button [data-docs-copy-source="true"]'

  await page.goto('/components/button')
  const canonicalApi = await source(apiSelector)
  const canonicalExample = await source(exampleSelector)
  expect(canonicalApi.match(/\n\n/g)?.length).toBeGreaterThan(1)
  expect(canonicalExample).toContain('\n\n')

  // A same-source morph must preserve highlighting, not only text content.
  await openGallery(page, 'button')
  await expect.poll(() => source(apiSelector)).toBe(canonicalApi)
  await expect.poll(() => source(exampleSelector)).toBe(canonicalExample)

  await openGallery(page, 'button-group')
  await openGallery(page, 'button')

  await expect.poll(() => source(apiSelector)).toBe(canonicalApi)
  await expect.poll(() => source(exampleSelector)).toBe(canonicalExample)
  const apiBlock = page.locator(apiSelector).first().locator('xpath=ancestor::*[@data-docs-copyable-code="true"]')
  await apiBlock.getByRole('button', { name: /^Copy / }).click()
  expect(await page.evaluate(() => (window as any).__copiedSource)).toBe(canonicalApi)

  const example = page.locator('#components-button')
  await example.getByRole('tab', { name: 'Code', exact: true }).click()
  await example.getByRole('button', { name: /^Copy / }).click()
  expect(await page.evaluate(() => (window as any).__copiedSource)).toBe(canonicalExample)

  await page.goBack()
  await expect(page).toHaveURL('/components/button-group')
  await page.goForward()
  await expect(page).toHaveURL('/components/button')
  await expect.poll(() => source(apiSelector)).toBe(canonicalApi)
  await expect.poll(() => source(exampleSelector)).toBe(canonicalExample)
})

test('Forward supersedes a pending Back restore even when the current document already matches @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  await openGallery(page, 'button-group')
  await openGallery(page, 'button')
  let releaseBack: () => void = () => {}
  const gate = new Promise<void>(resolve => { releaseBack = resolve })
  let reachedBack: () => void = () => {}
  const reached = new Promise<void>(resolve => { reachedBack = resolve })
  let finishedBack: () => void = () => {}
  const finished = new Promise<void>(resolve => { finishedBack = resolve })
  await page.route('**/components/button-group?*', async route => {
    const response = await route.fetch()
    reachedBack()
    await gate
    await route.fulfill({ response }).catch(() => {})
    finishedBack()
  })
  try {
    await page.goBack()
    await reached
    await page.goForward()
    await expect(page).toHaveURL('/components/button')
  } finally {
    releaseBack()
  }
  await finished
  await expect(page.locator('#docs-navigation-root')).not.toHaveAttribute('aria-busy', 'true')
  await expect(page.getByRole('heading', { level: 1, name: 'Button', exact: true })).toBeVisible()
})

test('sidebar preserves expanded groups and scroll during navigation morphs @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 1512, height: 825 })
  await page.goto('/components/browser')
  const sidebar = page.getByRole('complementary', { name: 'Documentation navigation' })
  const nav = sidebar.getByRole('navigation', { name: 'Documentation', exact: true })
  const expandedGroups = ['Guides', 'Actions', 'Feedback', 'Form controls', 'Navigation', 'Overlays']
  for (const name of expandedGroups) {
    await sidebar.getByLabel(`Toggle ${name} section`, { exact: true }).click()
  }

  for (const slug of ['phone', 'dialog', 'select', 'browser']) {
    const link = await galleryLink(page, slug)
    await link.scrollIntoViewIfNeeded()
    const scrollBefore = await nav.evaluate(el => el.scrollTop)
    expect(scrollBefore).toBeGreaterThan(300)
    await openGallery(page, slug)
    await expect(link).toHaveAttribute('aria-current', 'page')
    await expect.poll(async () => Math.abs(await nav.evaluate(el => el.scrollTop) - scrollBefore)).toBeLessThan(2)
    for (const name of expandedGroups) {
      await expect(sidebar.getByLabel(`Toggle ${name} section`, { exact: true })).toHaveAttribute('aria-expanded', 'true')
    }
    await expect(sidebar.getByLabel('Toggle Data display section', { exact: true })).toHaveAttribute('aria-expanded', 'false')
  }

  const scrollBeforeHistory = await nav.evaluate(el => el.scrollTop)
  await page.goBack()
  await expect(page).toHaveURL('/components/select')
  await expect.poll(async () => Math.abs(await nav.evaluate(el => el.scrollTop) - scrollBeforeHistory)).toBeLessThan(2)
  await page.goForward()
  await expect(page).toHaveURL('/components/browser')
  await expect.poll(async () => Math.abs(await nav.evaluate(el => el.scrollTop) - scrollBeforeHistory)).toBeLessThan(2)
})

test('rapid navigation keeps the latest server-rendered destination', async ({ page }) => {
  await page.goto('/components/button')
  let releaseSelect: () => void = () => {}
  const selectCanFinish = new Promise<void>(resolve => { releaseSelect = resolve })
  let selectReachedServer: () => void = () => {}
  const selectResponseReady = new Promise<void>(resolve => { selectReachedServer = resolve })

  await page.route('**/components/select?*', async route => {
    const response = await route.fetch()
    selectReachedServer()
    await selectCanFinish
    await route.fulfill({ response }).catch(() => {})
  })

  await (await galleryLink(page, 'select')).click()
  await selectResponseReady
  await (await galleryLink(page, 'month-calendar')).click()
  await expect(page).toHaveURL('/components/month-calendar')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Month calendar')

  releaseSelect()
  await expect(page).toHaveURL('/components/month-calendar')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Month calendar')
})

test('failed enhanced navigation keeps visible content and supports retry', async ({ page }) => {
  await page.goto('/components/button')
  await page.route('**/components/select?*', route => route.fulfill({ status: 503, contentType: 'text/plain', body: 'Temporarily unavailable' }))

  await (await galleryLink(page, 'select')).click()
  await expect(page.locator('#docs-navigation-status')).toHaveText('The page could not be loaded. Try the link again or refresh.')
  await expect(page).toHaveURL('/components/button')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Button')

  await page.unroute('**/components/select?*')
  await openGallery(page, 'select')
  await expect(page.locator('#docs-navigation-status')).toBeHidden()
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Select')
})
