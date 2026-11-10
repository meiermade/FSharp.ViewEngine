import { test, expect } from '../fixture'

test('grouped icon frames render before the button bundle initializes @cross-browser', async ({ page }) => {
  let release!: () => void
  const gate = new Promise<void>(resolve => { release = resolve })
  await page.route('**/assets/app.js', async route => { await gate; await route.continue() })
  try {
    await page.goto('/components/icon', { waitUntil: 'commit' })
    const card = page.locator('[data-icon-entry="AcademicCap"]')
    await expect(card).toBeVisible()
    await expect(card.locator('button')).toHaveCount(0)
    for (const side of ['top', 'right', 'bottom', 'left']) {
      await expect(card).toHaveCSS(`border-${side}-width`, '1px')
      await expect(card).toHaveCSS(`border-${side}-style`, 'solid')
    }
    expect(await card.locator('fve-button').evaluateAll(elements => elements.map(el => el.getClientRects().length))).toEqual([1, 1, 1, 1])
    release()
    await expect(card.getByRole('button')).toHaveCount(4)
    await expect(card.getByRole('button', { name: 'Copy AcademicCap Outline icon', exact: true })).toBeVisible()
  } finally { release() }
})

test('global server search finds names and uses without a family filter @cross-browser', async ({ page }) => {
  await page.goto('/components/icon')
  const browser = page.getByRole('region', { name: 'Icon browser', exact: true })
  const search = browser.getByRole('searchbox', { name: 'Search icons', exact: true })
  const count = browser.locator('#icon-result-count')
  await expect(browser.getByRole('tablist')).toHaveCount(0)
  await expect(count).toHaveText('Showing 24 of 319 icons')
  await expect(browser.locator('[data-icon-entry]')).toHaveCount(24)
  const response = page.waitForResponse(response => response.url().includes('/components/icon?') && new URL(response.url()).searchParams.get('search') === 'settings')
  await search.fill('settings')
  const filtered = await response
  expect(filtered.request().headers()['x-fve-navigation-intent']).toBe('filter')
  expect(filtered.headers()['content-type']).toContain('text/event-stream')
  await expect(count).toHaveText('7 icons')
  await expect(browser.locator('[data-icon-entry]')).toHaveCount(7)
  await expect(browser.locator('svg[data-fve-icon]')).toHaveCount(28)
  expect(await filtered.text()).not.toContain('id="main-content"')
  await expect(browser.getByRole('group', { name: 'Cog6Tooth variants', exact: true })).toBeVisible()
  await expect(browser.locator('[data-icon-entry="AcademicCap"]')).toHaveCount(0)
  await search.fill('COG-6-TOOTH')
  await expect(count).toHaveText('1 icon')
  await expect(browser.locator('[data-icon-entry]')).toHaveCount(1)
  await expect(browser.locator('svg[data-fve-icon]')).toHaveCount(4)
  await search.fill('doesnotexist123')
  await expect(count).toHaveText('0 icons')
  await expect(browser.locator('[data-icon-entry]')).toHaveCount(0)
  await expect(browser.locator('#icon-empty')).toBeVisible()
  await search.fill('')
  await expect(search).toBeFocused()
  await expect(count).toHaveText('Showing 24 of 319 icons')
  await expect(browser.locator('#icon-empty')).toHaveCount(0)
})

test('each visible artwork variant copies its exact F# expression @cross-browser', async ({ page }) => {
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: {
      writeText: async (text: string) => { (window as Window & { copiedIcon?: string }).copiedIcon = text },
    } })
  })
  await page.goto('/components/icon?search=Cog6Tooth')
  const browser = page.getByRole('region', { name: 'Icon browser', exact: true })
  const group = browser.getByRole('group', { name: 'Cog6Tooth variants', exact: true })
  await expect(group.getByRole('button')).toHaveCount(4)
  for (const [family, size] of [['Outline', 24], ['Solid', 24], ['Mini', 20], ['Micro', 16]] as const) {
    const copy = group.getByRole('button', { name: `Copy Cog6Tooth ${family} icon`, exact: true })
    const icon = copy.locator('svg')
    await expect(icon).toHaveAttribute('data-fve-icon-variant', family.toLowerCase())
    await expect(icon).toHaveAttribute('viewBox', `0 0 ${size} ${size}`)
    await expect(icon).toHaveCSS('width', `${size}px`)
    await copy.focus()
    await page.keyboard.press('Enter')
    await expect(browser.locator('#icon-copy-status')).toHaveText(`Copied Cog6Tooth (${family}) F# expression.`)
    expect(await page.evaluate(() => (window as Window & { copiedIcon?: string }).copiedIcon))
      .toBe(`fveIcon { _name IconName.Cog6Tooth; _variant IconVariant.${family} }`)
  }
  await page.evaluate(() => {
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: {
      writeText: async () => { throw new Error('Clipboard denied') },
    } })
  })
  await group.getByRole('button', { name: 'Copy Cog6Tooth Mini icon', exact: true }).click()
  await expect(browser.locator('#icon-copy-status')).toHaveText('Could not copy. Check clipboard access and try again.')
})

test('brand aliases are found by the same search and have only their original logo @cross-browser', async ({ page }) => {
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: {
      writeText: async (text: string) => { (window as Window & { copiedIcon?: string }).copiedIcon = text },
    } })
  })
  // Previously shared family URLs must not restrict the new global search.
  await page.goto('/components/icon?variant=outline&search=git')
  const browser = page.getByRole('region', { name: 'Icon browser', exact: true })
  const search = browser.getByRole('searchbox', { name: 'Search icons', exact: true })
  for (const [query, name] of [['git', 'GitHub'], ['twitter', 'X'], ['linkedin', 'LinkedIn']] as const) {
    if (query !== 'git') await search.fill(query)
    await expect(browser.locator('#icon-result-count')).toHaveText('1 icon')
    const group = browser.getByRole('group', { name: `${name} variants`, exact: true })
    await expect(group.getByRole('button')).toHaveCount(1)
    const copy = group.getByRole('button', { name: `Copy ${name} Solid icon`, exact: true })
    await expect(copy).toHaveText('Solid')
    await expect(copy.locator('svg')).toHaveAttribute('data-fve-icon-variant', 'solid')
    await expect(copy.locator('svg')).toHaveAttribute('viewBox', '0 0 16 16')
    await expect(copy.locator('svg')).toHaveCSS('width', '24px')
    await copy.click()
    expect(await page.evaluate(() => (window as Window & { copiedIcon?: string }).copiedIcon))
      .toBe(`fveIcon { _name IconName.${name} }`)
  }
})

test('showing another batch preserves cards and search resets the batch @cross-browser', async ({ page }) => {
  await page.goto('/components/icon')
  const browser = page.getByRole('region', { name: 'Icon browser', exact: true })
  await expect(browser.locator('[data-icon-entry]')).toHaveCount(24)
  const card = await browser.locator('[data-icon-entry="AcademicCap"]').elementHandle()
  const gallery = await browser.locator('#icon-gallery').elementHandle()
  const more = browser.getByRole('link', { name: 'Show more icons', exact: true })
  await more.scrollIntoViewIfNeeded()
  const scrollTop = await browser.locator('#icon-grid').evaluate(el => el.scrollTop)
  await more.click()
  await expect(browser.locator('[data-icon-entry]')).toHaveCount(48)
  await expect(browser.locator('#icon-grid')).toHaveJSProperty('scrollTop', scrollTop)
  await expect(browser.locator('#icon-result-count')).toHaveText('Showing 48 of 319 icons')
  await expect(page).toHaveURL(url => url.searchParams.get('count') === '48')
  expect(await card!.evaluate(el => el === document.getElementById('icon-tile-AcademicCap'))).toBe(true)
  expect(await gallery!.evaluate(el => el === document.getElementById('icon-gallery'))).toBe(true)
  await browser.getByRole('searchbox', { name: 'Search icons', exact: true }).fill('git')
  await expect(browser.locator('#icon-result-count')).toHaveText('1 icon')
  await expect(page).toHaveURL(url => url.searchParams.get('search') === 'git' && !url.searchParams.has('count'))
  await expect(browser.locator('#icon-grid')).toHaveJSProperty('scrollTop', 0)
})

test('new searches keep accepted results visible and cancel stale replies', async ({ page }) => {
  await page.goto('/components/icon')
  const browser = page.getByRole('region', { name: 'Icon browser', exact: true })
  const search = browser.getByRole('searchbox', { name: 'Search icons', exact: true })
  let release!: () => void, reached!: () => void, delivered!: () => void
  const gate = new Promise<void>(resolve => { release = resolve })
  const requested = new Promise<void>(resolve => { reached = resolve })
  const delivery = new Promise<void>(resolve => { delivered = resolve })
  await page.route('**/components/icon?**', async route => {
    if (new URL(route.request().url()).searchParams.get('search') !== 'settings') return route.continue()
    const response = await route.fetch()
    reached()
    await gate
    await route.fulfill({ response })
    delivered()
  })
  try {
    await search.fill('settings')
    await requested
    await expect(browser.locator('#icon-result-count')).toHaveText('Showing 24 of 319 icons')
    await expect(browser.locator('[data-icon-entry]')).toHaveCount(24)
    await search.fill('git')
    await expect(browser.getByRole('group', { name: 'GitHub variants', exact: true })).toBeVisible()
    release()
    await delivery
    await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))))
    await expect(browser.locator('[data-icon-entry]')).toHaveCount(1)
    await expect(page).toHaveURL(url => url.searchParams.get('search') === 'git')
    await expect(search).toBeFocused()
  } finally { release() }
})

test('Tabs documentation retains its independent server-owned selection example', async ({ page }) => {
  await page.goto('/components/tabs')
  const tabs = page.locator('#components-server-tabs')
  await tabs.getByRole('tab', { name: 'Activity', exact: true }).click()
  await expect(tabs.getByRole('tabpanel')).toHaveText('The server selected recent activity.')
  await tabs.getByRole('tab', { name: 'Activity', exact: true }).focus()
  await page.keyboard.press('Home')
  await expect(tabs.getByRole('tab', { name: 'Summary', exact: true })).toHaveAttribute('aria-selected', 'true')
  await page.keyboard.press('End')
  await expect(tabs.getByRole('tab', { name: 'Activity', exact: true })).toHaveAttribute('aria-selected', 'true')
})

test.describe('native icon browsing', () => {
  test.use({ javaScriptEnabled: false })
  test('search and further batches work through real GET destinations', async ({ page }) => {
    await page.goto('/components/icon')
    const browser = page.getByRole('region', { name: 'Icon browser', exact: true })
    await expect(browser.locator('[data-icon-entry]')).toHaveCount(24)
    await browser.getByRole('link', { name: 'Show more icons', exact: true }).click()
    await expect(browser.locator('[data-icon-entry]')).toHaveCount(48)
    const search = browser.getByRole('searchbox', { name: 'Search icons', exact: true })
    await search.fill('git')
    await search.press('Enter')
    await expect(page).toHaveURL(url => url.searchParams.get('search') === 'git' && !url.searchParams.has('count'))
    await expect(browser.locator('[data-icon-entry]')).toHaveCount(1)
    await expect(browser.locator('[data-icon-entry="GitHub"] svg')).toHaveAttribute('data-fve-icon-variant', 'solid')
  })
})
