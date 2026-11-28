import { expect, test } from '@playwright/test'

test('server-driven page navigation preserves the scrolled sidebar through Back and Forward @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 600 })
  await page.goto('/components/button')
  const sidebar = page.locator('#side-nav [data-fve-side-nav-content]')
  const node = (await sidebar.elementHandle())!
  const target = sidebar.locator('a[href="/components/input"]')
  await target.evaluate(link => {
    for (let parent = link.parentElement; parent; parent = parent.parentElement) {
      if (parent instanceof HTMLDetailsElement) parent.open = true
    }
    link.scrollIntoView({ block: 'center' })
  })
  await expect(target).toBeVisible()
  const scroll = await sidebar.evaluate(el => el.scrollTop)
  expect(scroll).toBeGreaterThan(0)
  const documentRequests: string[] = []
  const pageRequests: string[] = []
  page.on('request', request => {
    if (request.isNavigationRequest() && request.frame() === page.mainFrame()) documentRequests.push(request.url())
    if (request.headers()['datastar-request'] === 'true') pageRequests.push(new URL(request.url()).pathname)
  })
  const stable = async (title: string, path: string) => {
    await expect(page).toHaveURL(path)
    await expect(page).toHaveTitle(title)
    await expect(sidebar.getByRole('link', { name: title, exact: true })).toHaveAttribute('aria-current', 'page')
    expect(await node.evaluate(el => el === document.querySelector('#side-nav [data-fve-side-nav-content]'))).toBe(true)
    await expect.poll(() => sidebar.evaluate(el => el.scrollTop)).toBe(scroll)
  }
  await target.click()
  await stable('Input', '/components/input')
  await page.evaluate(() => history.back())
  await stable('Button', '/components/button')
  await page.evaluate(() => history.forward())
  await stable('Input', '/components/input')
  expect(documentRequests).toEqual([])
  expect(pageRequests).toEqual(['/components/input', '/components/button', '/components/input'])
})

test('Link applies an HTML fragment without history and the server can push an SSE destination', async ({ page }) => {
  await page.goto('/components/link')
  const requests: string[] = []
  for (const [path, contentType, body] of [
    ['/components/input', 'text/html', '<h1 id="link-nav-probe">Input fragment</h1>'],
    ['/components/button', 'text/event-stream', 'event: datastar-patch-elements\ndata: elements <h1 id="link-nav-probe">Button stream</h1>\n\nevent: datastar-patch-elements\ndata: selector body\ndata: mode append\ndata: elements <script>history.pushState(null, "", "/components/button")</script>\n\n'],
  ]) {
    await page.route(url => url.pathname === path, async route => {
      requests.push(route.request().url())
      expect(route.request().headers()['datastar-request']).toBe('true')
      expect(route.request().headers()['x-fve-navigation']).toBeUndefined()
      await route.fulfill({ contentType, body })
    })
  }
  await page.locator('main').evaluate(main => { main.insertAdjacentHTML('afterbegin', '<h1 id="link-nav-probe">Initial</h1>') })
  const modes = page.locator('#components-link-navigation')
  const initialLength = await page.evaluate(() => history.length)
  await modes.getByRole('link', { name: 'Navigate in place' }).click()
  await expect(page.locator('#link-nav-probe')).toHaveText('Input fragment')
  await expect(page).toHaveURL('/components/link')
  await modes.getByRole('link', { name: 'Open Button docs' }).click()
  await expect(page.locator('#link-nav-probe')).toHaveText('Button stream')
  await expect(page).toHaveURL('/components/button')
  expect(requests).toHaveLength(2)
  expect(await page.evaluate(() => history.length)).toBe(initialLength + 1)
  await page.goBack()
  await expect(page).toHaveURL('/components/link')
  await expect(page).toHaveTitle('Link')
})

test('Link leaves document links and opted-out links native', async ({ page }) => {
  await page.goto('/components/link')
  const modes = page.locator('#components-link-navigation')
  const plain = page.getByRole('link', { name: 'Plain native proof', exact: true })
  await modes.evaluate(element => { element.insertAdjacentHTML('afterbegin', '<a href="/components/input">Plain native proof</a>') })
  const native = async (link: ReturnType<typeof page.getByRole>, path: string) => {
    const response = page.waitForResponse(response => response.request().isNavigationRequest() && new URL(response.url()).pathname === path)
    await link.click()
    expect((await response).request().headers()['datastar-request']).toBeUndefined()
    await expect(page).toHaveURL(path)
  }
  await native(plain, '/components/input')
  await page.goto('/components/link')
  await native(modes.getByRole('link', { name: 'Load a new page' }), '/components')
})

test('Link discards an older pending response and never retries a failed navigation', async ({ page }) => {
  await page.goto('/components/link')
  let signalRequest!: () => void
  let release!: () => void
  const requested = new Promise<void>(resolve => { signalRequest = resolve })
  const gate = new Promise<void>(resolve => { release = resolve })
  await page.route(url => url.pathname === '/components/input', async route => {
    signalRequest()
    await gate
    await route.fulfill({ contentType: 'text/html', body: '<div id="link-nav-probe">Stale</div>' }).catch(() => {})
  })
  let failed = 0
  await page.route(url => url.pathname === '/components/button', async route => { failed++; await route.fulfill({ status: 503, body: 'Unavailable' }) })
  await page.locator('main').evaluate(main => { main.insertAdjacentHTML('afterbegin', '<div id="link-nav-probe">Initial</div>') })
  const modes = page.locator('#components-link-navigation')
  await modes.getByRole('link', { name: 'Navigate in place' }).click()
  await requested
  await page.evaluate(() => document.addEventListener('datastar-fetch', event => {
    if ((event as CustomEvent).detail.type === 'error') document.documentElement.dataset.navigationFailed = 'true'
  }))
  const cancelled = page.waitForEvent('requestfailed', request => new URL(request.url()).pathname === '/components/input')
  await modes.getByRole('link', { name: 'Open Button docs' }).click()
  await cancelled
  await expect(page.locator('html')).toHaveAttribute('data-navigation-failed', 'true')
  release()
  await expect(page).toHaveURL('/components/link')
  await expect(page.locator('#link-nav-probe')).toHaveText('Initial')
  expect(failed).toBe(1)
})
