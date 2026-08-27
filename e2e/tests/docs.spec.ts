import AxeBuilder from '@axe-core/playwright'
import { expect, test, type Locator, type Page } from '@playwright/test'

const productionOrigin = 'https://fsharpviewengine.meiermade.com'

const routes = [
  { path: '/', heading: 'FSharp.ViewEngine', layout: 'article' },
  { path: '/installation', heading: 'Installation', layout: 'article' },
  { path: '/getting-started/first-view', heading: 'Build your first view', layout: 'article' },
  { path: '/guides/elements-and-attributes', heading: 'Elements and attributes', layout: 'article' },
  { path: '/guides/composition-and-control-flow', heading: 'Composition and control flow', layout: 'article' },
  { path: '/guides/rendering', heading: 'Rendering', layout: 'article' },
  { path: '/guides/encoding-and-trusted-content', heading: 'Encoding and trusted content', layout: 'article' },
  { path: '/guides/accessibility', heading: 'Accessibility', layout: 'article' },
  { path: '/custom', heading: 'Custom Elements & Attributes', layout: 'article' },
  { path: '/usage', heading: 'Giraffe', layout: 'article' },
  { path: '/extensions/alpine', heading: 'Alpine.js', layout: 'article' },
  { path: '/extensions/datastar', heading: 'Datastar', layout: 'article' },
  { path: '/extensions/htmx', heading: 'HTMX', layout: 'article' },
  { path: '/extensions/svg', heading: 'SVG', layout: 'article' },
  { path: '/extensions/tailwind-elements', heading: 'Tailwind Plus Elements', layout: 'article' },
  { path: '/docs', heading: 'FSharp.ViewEngine.Docs', layout: 'article' },
  { path: '/docs/components/layouts', heading: 'Layouts', layout: 'article' },
  { path: '/docs/components/content', heading: 'Content', layout: 'article' },
  { path: '/docs/components/navigation', heading: 'Navigation', layout: 'article' },
  { path: '/docs/components/interactive-examples', heading: 'Interactive examples', layout: 'article' },
  { path: '/docs/components/api-reference', heading: 'API reference components', layout: 'article' },
  { path: '/docs/components/diagrams', heading: 'Diagrams', layout: 'article' },
  { path: '/docs/page-examples/documentation-site', heading: 'Documentation site', layout: 'article' },
  { path: '/docs/page-examples/api-reference', heading: 'API reference page', layout: 'article' },
  { path: '/docs/page-examples/executable-specification', heading: 'Executable specification page', layout: 'article' },
  { path: '/components', heading: 'Components', layout: 'article' },
  { path: '/components/installation', heading: 'Installation', layout: 'article' },
  { path: '/components/button', heading: 'Button', layout: 'article' },
  { path: '/components/icon-button', heading: 'Icon button', layout: 'article' },
  { path: '/components/badge', heading: 'Badge', layout: 'article' },
  { path: '/components/status', heading: 'Status', layout: 'article' },
  { path: '/components/loading-indicator', heading: 'Loading indicator', layout: 'article' },
  { path: '/components/empty-state', heading: 'Empty state', layout: 'article' },
  { path: '/components/table', heading: 'Table', layout: 'article' },
  { path: '/components/select', heading: 'Select', layout: 'article' },
  { path: '/components/combobox', heading: 'Combobox', layout: 'article' },
  { path: '/components/checkbox', heading: 'Checkbox', layout: 'article' },
  { path: '/components/switch', heading: 'Switch', layout: 'article' },
  { path: '/components/toggle-button', heading: 'Toggle button', layout: 'article' },
  { path: '/components/radio-group', heading: 'Radio group', layout: 'article' },
  { path: '/components/dropdown-menu', heading: 'Dropdown menu', layout: 'article' },
  { path: '/components/dialog', heading: 'Dialog', layout: 'article' },
  { path: '/components/collection', heading: 'Collection', layout: 'article' },
  { path: '/components/detail', heading: 'Detail', layout: 'article' },
  { path: '/components/app-shell', heading: 'App shell', layout: 'article' },
  { path: '/components/interaction-and-server-state', heading: 'Interaction and server state', layout: 'article' },
  { path: '/components/accessibility', heading: 'Accessibility', layout: 'article' },
  { path: '/components/theming', heading: 'Theming and density', layout: 'article' },
  { path: '/components/tailwind-css', heading: 'Tailwind CSS setup', layout: 'article' },
  { path: '/components/customization', heading: 'Customization', layout: 'article' },
  { path: '/components/versioning', heading: 'Versioning', layout: 'article' },
  { path: '/benchmarks', heading: 'Benchmarks', layout: 'article' },
  { path: '/changelog', heading: 'Changelog', layout: 'article' },
]

function captureBrowserErrors(page: Page) {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  page.on('console', message => {
    if (message.type() === 'error') errors.push(message.text())
  })
  return errors
}

test.describe('public documentation routes', () => {
  for (const route of routes) {
    test(`GET ${route.path} renders`, async ({ page }) => {
      const browserErrors = captureBrowserErrors(page)
      const response = await page.goto(route.path, { waitUntil: 'domcontentloaded' })

      expect(response?.status(), `${route.path} status`).toBe(200)
      const serverHtml = await response!.text()
      expect(serverHtml, `${route.path} server-rendered main content`).toContain('<main')
      expect(serverHtml, `${route.path} complete HTML document`).toContain('<!DOCTYPE html>')
      await expect(page.getByRole('heading', { level: 1, name: route.heading, exact: true })).toHaveCount(1)
      await expect(page.locator('main.spec-main')).toBeVisible()
      await expect(page.locator(`.docs-${route.layout}-layout`)).toBeVisible()
      const canonicalURL = route.path === '/' ? `${productionOrigin}/` : `${productionOrigin}${route.path}`
      await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', canonicalURL)
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
      expect(browserErrors, `${route.path} browser errors`).toEqual([])
    })
  }
})

test('legacy Docs catalog routes remain aliases with canonical destinations', async ({ page }) => {
  const aliases = [
    ['/docs/components', '/docs/components/layouts'],
    ['/docs-components', '/docs/components/layouts'],
    ['/docs/examples/api-reference', '/docs/page-examples/api-reference'],
    ['/api-reference/render-to-string', '/docs/page-examples/api-reference'],
    ['/docs/examples/executable-specification', '/docs/page-examples/executable-specification'],
    ['/specification/render-a-view', '/docs/page-examples/executable-specification'],
  ] as const

  for (const [alias, canonicalPath] of aliases) {
    const response = await page.goto(alias, { waitUntil: 'domcontentloaded' })
    expect(response?.status(), alias).toBe(200)
    await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', `${productionOrigin}${canonicalPath}`)
  }
})

test('the removed Components contract route returns not found', async ({ request }) => {
  const response = await request.get('/components/contract')
  expect(response.status()).toBe(404)
})

test('canonical routes expose valid same-origin links, assets, and lazy previews', async ({ page, request }) => {
  const checked = new Map<string, number>()

  for (const route of routes) {
    await page.goto(route.path, { waitUntil: 'domcontentloaded' })
    const references = await page.locator('a[href], img[src], script[src], link[href], iframe[data-docs-preview-src]').evaluateAll(elements =>
      elements.map(element =>
        element.getAttribute('href') ?? element.getAttribute('src') ?? element.getAttribute('data-docs-preview-src') ?? '',
      ),
    )

    for (const reference of references) {
      if (!reference || reference.startsWith('#') || reference.startsWith('mailto:') || reference.startsWith('tel:')) continue
      const url = new URL(reference, page.url())
      if (url.origin !== new URL(page.url()).origin) continue
      url.hash = ''
      const path = `${url.pathname}${url.search}`
      if (checked.has(path)) continue
      const response = await request.get(path)
      checked.set(path, response.status())
      expect(response.status(), `${route.path} -> ${path}`).toBeLessThan(400)
    }
  }

  expect(checked.size).toBeGreaterThan(routes.length)
})

test.describe('automated accessibility checks', () => {
  const scan = async (page: Page, context: string) => {
    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze()
    expect(results.violations, context).toEqual([])
  }

  test('representative article and catalog routes pass WCAG A/AA scans', async ({ page }) => {
    for (const route of ['/', '/getting-started/first-view', '/docs/components/content', '/components', '/components/select']) {
      await page.goto(route, { waitUntil: 'domcontentloaded' })
      await scan(page, route)
    }
  })

  test('search and mobile navigation open states pass WCAG A/AA scans', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded' })
    await page.getByRole('button', { name: 'Search documentation' }).click()
    await scan(page, 'search dialog')
    await page.keyboard.press('Escape')

    await page.setViewportSize({ width: 390, height: 844 })
    await page.getByRole('button', { name: 'Open navigation' }).click()
    await scan(page, 'mobile navigation')
  })
})

test('Components pages provide focused examples, navigation, interaction, themes, and responsive accessibility', async ({ page }, testInfo) => {
  const browserErrors = captureBrowserErrors(page)
  const componentRoutes = [
    ['/components/button', 'Button'],
    ['/components/icon-button', 'Icon button'],
    ['/components/badge', 'Badge'],
    ['/components/status', 'Status'],
    ['/components/loading-indicator', 'Loading indicator'],
    ['/components/empty-state', 'Empty state'],
    ['/components/table', 'Table'],
    ['/components/select', 'Select'],
    ['/components/combobox', 'Combobox'],
    ['/components/checkbox', 'Checkbox'],
    ['/components/switch', 'Switch'],
    ['/components/toggle-button', 'Toggle button'],
    ['/components/radio-group', 'Radio group'],
    ['/components/dropdown-menu', 'Dropdown menu'],
    ['/components/dialog', 'Dialog'],
    ['/components/collection', 'Collection'],
    ['/components/detail', 'Detail'],
    ['/components/app-shell', 'App shell'],
  ] as const

  const openPreview = async (path: string, heading: string) => {
    await page.goto(path, { waitUntil: 'domcontentloaded' })
    await expect(page.getByRole('heading', { level: 1, name: heading, exact: true })).toBeVisible()
    const example = page.locator('[data-docs-example="true"]')
    await expect(example).toHaveCount(1)
    const previewTab = example.getByRole('tab', { name: 'Preview' })
    const panelId = await previewTab.getAttribute('aria-controls')
    expect(panelId).toBeTruthy()
    await previewTab.click()
    const panel = page.locator(`#${panelId}`)
    await expect(panel).toBeVisible()
    await expect(panel.locator('.fve-components')).toHaveCount(1)
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), path).toBe(true)
    return panel.locator('.fve-components')
  }

  for (const [path, heading] of componentRoutes) {
    const surface = await openPreview(path, heading)
    await expect(surface.locator('select')).toHaveCount(0)
    const duplicateIds = await page.locator('[id]').evaluateAll(elements => {
      const ids = elements.map(element => element.id)
      return [...new Set(ids.filter((id, index) => ids.indexOf(id) !== index))]
    })
    expect(duplicateIds, path).toEqual([])
  }

  await page.goto('/components/select', { waitUntil: 'domcontentloaded' })
  const packageNavOrder = await page.locator('#nav-fsharp-viewengine-components, #nav-fsharp-viewengine-docs').evaluateAll(elements => elements.map(element => element.id))
  expect(packageNavOrder).toEqual(['nav-fsharp-viewengine-components', 'nav-fsharp-viewengine-docs'])
  await expect(page.locator('#nav-fsharp-viewengine-components')).toHaveAttribute('aria-expanded', 'true')
  await expect(page.locator('#nav-form-controls')).toHaveAttribute('aria-expanded', 'true')
  await expect(page.locator('#nav-components-select')).toHaveAttribute('data-selected', 'true')
  await page.locator('#nav-components-combobox').click()
  await expect(page).toHaveURL('/components/combobox')
  await expect(page.getByRole('heading', { level: 1, name: 'Combobox' })).toBeVisible()

  const buttonSurface = await openPreview('/components/button', 'Button')
  const lightPage = await buttonSurface.evaluate(element => getComputedStyle(element).getPropertyValue('--fve-page').trim())
  const brandRoles = (surface: Locator) => surface.evaluate(element => {
    const styles = getComputedStyle(element)
    return ['subtle', 'solid', 'hover', 'text', 'ring'].map(role => styles.getPropertyValue(`--fve-brand-${role}`).trim())
  })
  const lightBrandRoles = await brandRoles(buttonSurface)
  const comfortableControl = buttonSurface.getByRole('button', { name: 'Create account' })
  const comfortableDensity = await comfortableControl.evaluate(element => ({
    height: getComputedStyle(element).height,
    paddingTop: getComputedStyle(element).paddingTop,
  }))
  expect(lightPage).toBeTruthy()
  expect(lightBrandRoles.every(Boolean)).toBe(true)

  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  const darkPage = await buttonSurface.evaluate(element => getComputedStyle(element).getPropertyValue('--fve-page').trim())
  const darkBrandRoles = await brandRoles(buttonSurface)
  expect(darkPage).not.toBe(lightPage)
  for (let index = 0; index < darkBrandRoles.length; index += 1) {
    expect(darkBrandRoles[index]).toBeTruthy()
    expect(darkBrandRoles[index]).not.toBe(lightBrandRoles[index])
  }
  const primaryRestingBackground = await comfortableControl.evaluate(element => getComputedStyle(element).backgroundColor)
  await comfortableControl.hover()
  expect(await comfortableControl.evaluate(element => getComputedStyle(element).backgroundColor)).not.toBe(primaryRestingBackground)
  const pendingButton = buttonSurface.getByRole('button', { name: 'Sync accounts' })
  await expect(pendingButton).toBeDisabled()
  await expect(pendingButton).toHaveAttribute('aria-busy', 'true')
  await expect(pendingButton).toContainText('Sync accounts')
  await expect(buttonSurface.getByRole('button', { name: 'Delete account' })).toBeDisabled()

  const iconButtonSurface = await openPreview('/components/icon-button', 'Icon button')
  const addAccount = iconButtonSurface.getByRole('button', { name: 'Add account' })
  await addAccount.focus()
  await expect(addAccount).toBeFocused()
  await expect(addAccount.locator('[aria-hidden="true"]')).toBeVisible()
  const refreshingAccounts = iconButtonSurface.getByRole('button', { name: 'Refresh accounts' })
  await expect(refreshingAccounts).toBeDisabled()
  await expect(refreshingAccounts).toHaveAttribute('aria-busy', 'true')

  const badgeSurface = await openPreview('/components/badge', 'Badge')
  await expect(badgeSurface.getByText('Internal', { exact: true })).toBeVisible()
  await expect(badgeSurface.getByText('Reconciled', { exact: true })).toBeVisible()

  const loadingSurface = await openPreview('/components/loading-indicator', 'Loading indicator')
  await expect(loadingSurface.getByRole('status')).toHaveCount(2)
  await expect(loadingSurface.getByText('Loading account balances')).toHaveClass(/sr-only/)
  await expect(loadingSurface.getByText('Refreshing transactions')).toBeVisible()

  const emptyStateSurface = await openPreview('/components/empty-state', 'Empty state')
  await expect(emptyStateSurface.getByText('No accounts yet', { exact: true })).toBeVisible()
  await expect(emptyStateSurface.getByRole('button', { name: 'Create account' })).toBeEnabled()

  const selectSurface = await openPreview('/components/select', 'Select')
  const statusSelect = selectSurface.getByRole('combobox', { name: 'Status' })
  const statusListbox = selectSurface.getByRole('listbox', { name: 'Status' })
  const activeStatusOption = async () => statusSelect.getAttribute('aria-activedescendant')
  await statusSelect.click()
  await expect(statusListbox).toBeVisible()
  await expect(statusSelect).toBeFocused()
  await expect.poll(activeStatusOption).toBe(await statusListbox.getByRole('option', { name: 'Active' }).getAttribute('id'))
  await statusListbox.getByRole('option', { name: 'Pending' }).click()
  await expect(statusSelect).toContainText('Pending')
  await expect(selectSurface.locator('input[type="hidden"][name="status"]')).toHaveValue('pending')
  await statusSelect.click()
  await page.keyboard.press('End')
  await expect.poll(activeStatusOption).toBe(await statusListbox.getByRole('option', { name: 'Scheduled' }).getAttribute('id'))
  await page.keyboard.press('Enter')
  await expect(statusSelect).toContainText('Scheduled')
  await statusSelect.press('s')
  await statusSelect.press('c')
  await expect.poll(activeStatusOption).toBe(await statusListbox.getByRole('option', { name: 'Scheduled' }).getAttribute('id'))
  await page.keyboard.press('Escape')
  await expect(statusSelect).toBeFocused()

  const collectionSurface = await openPreview('/components/collection', 'Collection')
  const statusFilter = collectionSurface.getByRole('combobox', { name: 'Filter by status' })
  const statusFilterListbox = collectionSurface.getByRole('listbox', { name: 'Filter by status' })
  await statusFilter.press('s')
  await expect.poll(() => statusFilter.getAttribute('aria-activedescendant')).toBe(await statusFilterListbox.getByRole('option', { name: 'Suspended' }).getAttribute('id'))
  await statusFilter.press('s')
  await expect.poll(() => statusFilter.getAttribute('aria-activedescendant')).toBe(await statusFilterListbox.getByRole('option', { name: 'Scheduled' }).getAttribute('id'))
  await statusFilter.press('Escape')

  const comboboxSurface = await openPreview('/components/combobox', 'Combobox')
  const parentAccount = comboboxSurface.getByRole('combobox', { name: 'Parent account' })
  const accountListbox = comboboxSurface.getByRole('listbox', { name: 'Parent account' })
  const activeAccountOption = async () => parentAccount.getAttribute('aria-activedescendant')
  await parentAccount.click()
  await expect(parentAccount).toBeFocused()
  await page.keyboard.press('End')
  await expect.poll(activeAccountOption).toBe(await accountListbox.getByRole('option', { name: 'Tax reserve' }).getAttribute('id'))
  await page.keyboard.press('Enter')
  await expect(comboboxSurface.locator('input[type="hidden"][name="account"]')).toHaveValue('102')
  await parentAccount.fill('oper')
  await expect(accountListbox.getByRole('option', { name: 'Operating' })).toBeVisible()
  await expect(accountListbox.getByRole('option', { name: 'Tax reserve' })).toHaveCount(0)
  await expect(parentAccount).toBeFocused()
  await expect.poll(activeAccountOption).toBe(await accountListbox.getByRole('option', { name: 'Operating' }).getAttribute('id'))
  await page.keyboard.press('Enter')
  await expect(parentAccount).toHaveValue('Operating')
  await expect(comboboxSurface.locator('input[type="hidden"][name="account"]')).toHaveValue('101')
  await parentAccount.fill('missing')
  await expect(accountListbox.getByRole('status')).toHaveText('No matching options')
  await expect(parentAccount).not.toHaveAttribute('aria-activedescendant')
  await page.keyboard.press('Escape')
  await expect(parentAccount).toBeFocused()

  const checkboxSurface = await openPreview('/components/checkbox', 'Checkbox')
  const includeArchived = checkboxSurface.getByRole('checkbox', { name: 'Include archived accounts' })
  await checkboxSurface.getByText('Include archived accounts', { exact: true }).click()
  await expect(includeArchived).toBeChecked()

  const switchSurface = await openPreview('/components/switch', 'Switch')
  const notifications = switchSurface.getByRole('switch', { name: 'Posting notifications' })
  await notifications.press('Space')
  await expect(notifications).toHaveAttribute('aria-checked', 'false')

  const toggleSurface = await openPreview('/components/toggle-button', 'Toggle button')
  const compactRows = toggleSurface.getByRole('button', { name: 'Compact rows' })
  await compactRows.click()
  await expect(compactRows).toHaveAttribute('aria-pressed', 'false')

  const radioSurface = await openPreview('/components/radio-group', 'Radio group')
  const manualPosting = radioSurface.getByRole('radio', { name: 'Manual review' })
  await radioSurface.getByText('Manual review', { exact: true }).click()
  await expect(manualPosting).toBeChecked()

  const menuSurface = await openPreview('/components/dropdown-menu', 'Dropdown menu')
  const actionsTrigger = menuSurface.getByRole('button', { name: 'Actions' })
  const actionsMenu = menuSurface.getByRole('menu', { name: 'Actions' })
  await actionsTrigger.click()
  await expect(actionsMenu.getByRole('menuitem', { name: 'Account settings' })).toBeFocused()
  await page.keyboard.press('End')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Delete account' })).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(actionsTrigger).toBeFocused()

  const dialogSurface = await openPreview('/components/dialog', 'Dialog')
  const dialogTrigger = dialogSurface.getByRole('button', { name: 'Review account' })
  const dialog = dialogSurface.getByRole('dialog', { name: 'Review account' })
  await dialogTrigger.click()
  await expect(dialog).toBeVisible()
  await expect(dialog.getByRole('button', { name: 'Close' })).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(dialog).toBeHidden()
  await expect(dialogTrigger).toBeFocused()

  const appShellSurface = await openPreview('/components/app-shell', 'App shell')
  const compactControl = appShellSurface.getByRole('button', { name: 'Account' })
  const compactDensity = await compactControl.evaluate(element => ({
    height: getComputedStyle(element).height,
    paddingTop: getComputedStyle(element).paddingTop,
  }))
  expect(parseFloat(comfortableDensity.paddingTop)).toBeGreaterThan(parseFloat(compactDensity.paddingTop))
  expect(parseFloat(comfortableDensity.height)).toBeGreaterThan(parseFloat(compactDensity.height))
  await expect(appShellSurface.locator('[aria-current="page"]')).toBeVisible()

  for (const path of ['/components', '/components/icon-button', '/components/loading-indicator', '/components/empty-state', '/components/select', '/components/dialog', '/components/app-shell']) {
    await page.goto(path, { waitUntil: 'domcontentloaded' })
    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze()
    expect(results.violations, path).toEqual([])
  }

  await page.goto('/components', { waitUntil: 'domcontentloaded' })
  const catalog = page.locator('.docs-catalog-grid')
  await expect(catalog.getByRole('link', { name: /ACTIONS Button/ })).toHaveAttribute('href', '/components/button')
  await expect(catalog.getByRole('link', { name: /ACTIONS Icon button/ })).toHaveAttribute('href', '/components/icon-button')
  await expect(catalog.getByRole('link', { name: /FEEDBACK Empty state/ })).toHaveAttribute('href', '/components/empty-state')
  await expect(catalog.getByRole('link', { name: /COMPOSITIONS App shell/ })).toHaveAttribute('href', '/components/app-shell')
  await testInfo.attach('components-catalog-desktop-dark', {
    body: await page.screenshot({ fullPage: true }),
    contentType: 'image/png',
  })

  await page.setViewportSize({ width: 390, height: 844 })
  await expect(page.getByRole('heading', { level: 1, name: 'Components' })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await page.getByRole('button', { name: 'Open navigation' }).click()
  await expect(page.locator('#nav-fsharp-viewengine-components')).toBeVisible()
  await testInfo.attach('components-catalog-mobile-dark', {
    body: await page.screenshot({ fullPage: true }),
    contentType: 'image/png',
  })
  expect(browserErrors).toEqual([])
})

test('health and pinned application assets are available', async ({ request }) => {
  const health = await request.get('/health')
  expect(health.status()).toBe(200)
  const healthBody = await health.json()
  expect(healthBody).toMatchObject({ status: 'ok' })
  expect(healthBody).not.toHaveProperty('release')
  expect(healthBody.commit).toBeTruthy()

  if (process.env.DOCS_EXPECTED_COMMIT) {
    expect(healthBody.commit).toBe(process.env.DOCS_EXPECTED_COMMIT)
  }

  const css = await request.get('/css/output.css')
  expect(css.status()).toBe(200)
  expect(await css.text()).toContain('tailwindcss v4.3.3')

  const assets = [
    ['/scripts/datastar.1.0.2.js', 'Datastar v1.0.2'],
    ['/scripts/mermaid.11.16.0.min.js', 'mermaid'],
    ['/scripts/prism.1.29.0.min.js', 'Prism'],
    ['/scripts/prism-fsharp.1.29.0.min.js', 'fsharp'],
    ['/css/prism-tomorrow.1.29.0.min.css', 'code[class*=language-]'],
    ['/fonts/noto-sans-latin.woff2', 'wOF2'],
    ['/fonts/noto-sans-mono-latin.woff2', 'wOF2'],
  ] as const

  for (const [path, marker] of assets) {
    const response = await request.get(path)
    expect(response.status(), path).toBe(200)
    expect((await response.text()).toLowerCase(), path).toContain(marker.toLowerCase())
  }
})

test('search filters pages and headings with keyboard access', async ({ page }) => {
  await page.goto('/', { waitUntil: 'domcontentloaded' })
  await page.keyboard.press(process.platform === 'darwin' ? 'Meta+K' : 'Control+K')

  const dialog = page.getByRole('dialog', { name: 'Search documentation' })
  await expect(dialog).toBeVisible()
  const input = dialog.getByRole('searchbox')
  await expect(input).toBeFocused()
  await input.fill('Rendering')
  const visible = dialog.locator('[data-docs-search-entry]:visible')
  await expect(visible).not.toHaveCount(0)
  await expect(visible.first()).toHaveAttribute('href', /guides\/rendering/)
})

test('getting started shows the product logo and Tailwind Sky accents', async ({ page }) => {
  await page.goto('/', { waitUntil: 'domcontentloaded' })
  const logo = page.locator('.docs-home-logo img')
  await expect(logo).toBeVisible()
  await expect(logo).toHaveAttribute('src', '/logo.svg')
  await expect(page.getByRole('heading', { level: 1, name: 'FSharp.ViewEngine' })).toHaveCount(1)
  const accent = await page.locator('html').evaluate(element => getComputedStyle(element).getPropertyValue('--spec-accent-500').trim())
  expect(accent).toBe('#0ea5e9')
})

test('repository action and code typography stay compact', async ({ page }) => {
  await page.goto('/', { waitUntil: 'domcontentloaded' })

  const repository = page.getByRole('link', { name: 'View repository on GitHub' })
  await expect(repository).toBeVisible()
  await expect(repository.locator('svg')).toHaveCount(1)
  await expect(repository).not.toContainText('Repository')

  const paragraph = page.locator('.spec-paragraph').first()
  await expect(paragraph).toBeVisible()
  expect(await paragraph.evaluate(element => getComputedStyle(element).fontSize)).toBe('16px')
  expect(await paragraph.evaluate(element => getComputedStyle(element).fontFamily)).toContain('Noto Sans')

  const code = page.locator('.spec-code code').first()
  await expect(code).toBeVisible()
  expect(await code.evaluate(element => getComputedStyle(element).fontSize)).toBe('13px')
  expect(await code.evaluate(element => getComputedStyle(element).fontFamily)).toContain('Noto Sans Mono')
  expect(Number.parseFloat(await code.evaluate(element => getComputedStyle(element).lineHeight))).toBeCloseTo(20.15, 1)
})

test('color mode selector supports persistence, keyboard navigation, and system changes', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'light' })
  await page.goto('/docs/components/layouts', { waitUntil: 'domcontentloaded' })
  await page.evaluate(() => localStorage.removeItem('fsharp-viewengine-docs-navigation-color-mode'))
  await page.reload({ waitUntil: 'domcontentloaded' })

  const codeSurface = page.locator('pre.spec-code').first()
  await expect(codeSurface).toBeVisible()
  await expect(codeSurface).toHaveCSS('background-color', 'rgb(246, 248, 250)')
  await expect(codeSurface).toHaveCSS('color', 'rgb(36, 41, 47)')

  const trigger = page.getByRole('button', { name: 'Choose color theme' })
  await trigger.click()
  const menu = page.getByRole('menu', { name: 'Color theme' })
  await expect(menu).toBeVisible()
  await expect(page.getByRole('menuitemradio', { name: 'System' })).toHaveAttribute('aria-checked', 'true')

  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  await expect(page.locator('html')).toHaveClass(/dark/)
  await expect(codeSurface).toHaveCSS('background-color', 'rgb(13, 17, 23)')
  await expect(codeSurface).toHaveCSS('color', 'rgb(201, 209, 217)')
  expect(await page.evaluate(() => localStorage.getItem('fsharp-viewengine-docs-navigation-color-mode'))).toBe('dark')
  await expect(trigger).toBeFocused()

  await page.locator('#nav-installation').click()
  await expect(page).toHaveURL('/installation')
  await expect(page.getByRole('heading', { level: 1, name: 'Installation' })).toBeVisible()
  await expect(page.locator('html')).toHaveClass(/dark/)

  await page.reload({ waitUntil: 'domcontentloaded' })
  await expect(page.locator('html')).toHaveClass(/dark/)

  await trigger.press('ArrowDown')
  const system = page.getByRole('menuitemradio', { name: 'System' })
  const light = page.getByRole('menuitemradio', { name: 'Light' })
  await expect(system).toBeFocused()
  await system.press('ArrowDown')
  await expect(light).toBeFocused()
  await light.press('Enter')
  await expect(page.locator('html')).not.toHaveClass(/dark/)

  await trigger.click()
  await system.click()
  await page.emulateMedia({ colorScheme: 'dark' })
  await expect(page.locator('html')).toHaveClass(/dark/)
  await page.emulateMedia({ colorScheme: 'light' })
  await expect(page.locator('html')).not.toHaveClass(/dark/)
})

test('mobile navigation manages modal focus and does not overflow', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/', { waitUntil: 'domcontentloaded' })

  const drawer = page.locator('#side-nav')
  const opener = page.getByRole('button', { name: 'Open navigation' })
  const close = page.getByRole('button', { name: 'Close navigation', exact: true })
  await expect(drawer).toBeHidden()

  await opener.click()
  await expect(drawer).toBeVisible()
  await expect(close).toBeFocused()
  await expect(page.locator('#page-content')).toHaveAttribute('inert', '')

  await close.press('Shift+Tab')
  await expect(drawer.locator('#nav-project')).toBeFocused()
  await page.keyboard.press('Tab')
  await expect(close).toBeFocused()

  await page.keyboard.press('Escape')
  await expect(drawer).toBeHidden()
  await expect(opener).toBeFocused()
  await expect(page.locator('#page-content')).not.toHaveAttribute('inert', '')

  await opener.click()
  await page.locator('.spec-overlay').click({ position: { x: 380, y: 400 } })
  await expect(drawer).toBeHidden()
  await expect(opener).toBeFocused()

  await opener.click()
  await close.click()
  await expect(drawer).toBeHidden()
  await expect(opener).toBeFocused()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})

test('desktop table of contents tracks the visible section and survives Docs navigation', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto('/custom', { waitUntil: 'domcontentloaded' })

  const main = page.locator('.spec-main')
  const target = page.locator('#shoelace-example')
  await main.evaluate(element => element.scrollTo({ top: element.scrollHeight, behavior: 'instant' }))
  await expect(page.locator('.spec-toc a[href="#shoelace-example"]')).toHaveAttribute('aria-current', 'location')

  await page.locator('#nav-accessibility').click()
  await expect(page).toHaveURL('/guides/accessibility')
  await expect(page.locator('.spec-toc a[href="#overview"]')).toHaveAttribute('aria-current', 'location')
})

test('mobile table of contents is a compact keyboard-operable disclosure', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/custom', { waitUntil: 'domcontentloaded' })

  const toc = page.getByRole('group', { name: 'On this page' })
  const summary = toc.locator('summary')
  await expect(toc).toBeVisible()
  await expect(toc).not.toHaveAttribute('open', '')
  await summary.press('Enter')
  await expect(toc).toHaveAttribute('open', '')
  await toc.getByRole('link', { name: 'Shoelace Example' }).click()
  await expect(page).toHaveURL('/custom#shoelace-example')
  await expect(toc).not.toHaveAttribute('open', '')
  await expect(page.locator('#shoelace-example')).toBeFocused()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})

test('on-this-page links scroll the nested documentation viewport', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto('/custom', { waitUntil: 'domcontentloaded' })

  const main = page.locator('.spec-main')
  const target = page.locator('#shoelace-example')
  await page.locator('.spec-toc a[href="#shoelace-example"]').click()

  await expect(page).toHaveURL('/custom#shoelace-example')
  await expect.poll(() => main.evaluate(element => element.scrollTop)).toBeGreaterThan(0)
  await expect.poll(async () => (await target.boundingBox())!.y).toBeLessThan(1000)
})

test('Docs navigation scrolls content to top and highlights morphed code', async ({ page }) => {
  await page.goto('/extensions/svg', { waitUntil: 'domcontentloaded' })
  const main = page.locator('.spec-main')
  await main.evaluate(element => element.scrollTo({ top: element.scrollHeight }))
  expect(await main.evaluate(element => element.scrollTop)).toBeGreaterThan(0)

  await page.locator('#nav-core-concepts').click()
  await page.locator('#nav-custom').click()
  await expect(page).toHaveURL('/custom')
  await expect(page.getByRole('heading', { level: 1, name: 'Custom Elements & Attributes' })).toBeVisible()
  await expect.poll(() => main.evaluate(element => element.scrollTop)).toBe(0)
  await expect(page.locator('.spec-code .token.keyword').first()).toBeVisible()
})

test('Docs navigation loads Prism dependencies before highlighting a code page', async ({ page }) => {
  const pageErrors: string[] = []
  page.on('pageerror', error => pageErrors.push(error.message))

  await page.goto('/docs/components/content', { waitUntil: 'domcontentloaded' })
  await page.locator('#nav-home').click()

  await expect(page).toHaveURL('/')
  const keyword = page.locator('.spec-code .token.keyword').first()
  await expect(keyword).toBeVisible()
  await expect(keyword).toHaveCSS('color', 'rgb(207, 34, 46)')
  expect(pageErrors.filter(error => error.includes('Prism is not defined'))).toEqual([])
})

test('code blocks copy their literal source', async ({ page }) => {
  await page.goto('/getting-started/first-view', { waitUntil: 'domcontentloaded' })
  await page.evaluate(() => {
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: async (value: string) => { (window as any).__copiedSource = value } } })
  })
  const block = page.locator('.docs-copyable-code').first()
  await block.getByRole('button', { name: 'Copy code' }).click()
  await expect(block.getByRole('button', { name: 'Copy code' })).toContainText('Copied')
  expect(await page.evaluate(() => (window as any).__copiedSource)).toContain('let greeting')
})

test('code and preview examples support pointer and keyboard tabs', async ({ page }) => {
  await page.goto('/extensions/svg', { waitUntil: 'domcontentloaded' })
  const example = page.locator('[data-docs-example="true"]').first()
  const preview = example.getByRole('tab', { name: 'Preview' })
  const code = example.getByRole('tab', { name: 'Code' })

  await expect(code).toHaveAttribute('aria-selected', 'true')
  expect((await code.boundingBox())!.x).toBeLessThan((await preview.boundingBox())!.x)
  await expect(example.getByRole('tabpanel', { name: 'Code' })).toBeVisible()
  await expect(example.locator('.token.keyword').first()).toBeVisible()
  await preview.click()
  await expect(example.getByRole('tabpanel', { name: 'Preview' })).toBeVisible()
  await preview.press('ArrowLeft')
  await expect(code).toBeFocused()
  await expect(code).toHaveAttribute('aria-selected', 'true')
})

test('inline prose links are visually identifiable and article pagers continue the learning path', async ({ page }) => {
  await page.goto('/', { waitUntil: 'domcontentloaded' })
  const installation = page.getByRole('link', { name: 'Installation', exact: true }).last()
  await expect(installation).toHaveCSS('text-decoration-line', 'underline')
  await expect(installation).toHaveCSS('font-weight', '600')

  await page.goto('/docs', { waitUntil: 'domcontentloaded' })
  await expect(page.getByText('This documentation site is built with FSharp.ViewEngine.Docs', { exact: false })).toBeVisible()
  const pager = page.getByRole('navigation', { name: 'Page navigation' })
  await expect(page.getByRole('link', { name: 'Browse components' })).toHaveAttribute('href', '/docs/components/layouts')
  await expect(page.getByRole('link', { name: 'Browse page examples' })).toHaveAttribute('href', '/docs/page-examples/documentation-site')
  await expect(pager.getByRole('link', { name: /Previous Versioning/ })).toHaveAttribute('href', '/components/versioning')
  const next = pager.getByRole('link', { name: /Next Layouts/ })
  await expect(next).toBeVisible()
  await next.click()
  await expect(page).toHaveURL('/docs/components/layouts')
  await expect(page.getByRole('heading', { level: 1, name: 'Layouts' })).toBeVisible()
})

test('Tailwind Plus Elements previews render and operate the actual custom elements', async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/extensions/svg', { waitUntil: 'domcontentloaded' })
  await page.locator('#nav-tailwind-elements').click()
  await expect(page).toHaveURL('/extensions/tailwind-elements')
  await page.waitForFunction(() => Boolean(customElements.get('el-autocomplete')))

  const ids = ['autocomplete', 'command-palette', 'copy-button', 'dialog', 'disclosure', 'dropdown-menu', 'popover', 'select', 'tabs']
  for (const id of ids) {
    const example = page.locator(`[data-docs-example="true"]:has(#tailwind-elements-${id}-tab-preview)`)
    await example.getByRole('tab', { name: 'Preview' }).click()
    await expect(example.getByRole('tabpanel', { name: 'Preview' })).toBeVisible()
  }

  const autocomplete = page.locator('[data-docs-example="true"]:has(#tailwind-elements-autocomplete-tab-preview)')
  const autocompleteSurface = autocomplete.locator('[data-example-surface="true"]')
  const options = autocomplete.locator('el-options')
  await expect(autocompleteSurface).toHaveCSS('background-color', 'rgb(250, 250, 250)')
  await expect(options).toBeHidden()
  await autocomplete.getByRole('button', { name: 'Show people' }).click()
  await expect(options).toBeVisible()
  await expect(options).toHaveAttribute('role', 'listbox')

  const disclosure = page.locator('[data-docs-example="true"]:has(#tailwind-elements-disclosure-tab-preview)')
  const disclosureButton = disclosure.getByRole('button', { name: 'What does the answer mean?' })
  await disclosureButton.click()
  await expect(disclosure.locator('el-disclosure')).toBeVisible()
  await expect(disclosureButton.locator('svg')).toHaveCSS('transform', 'matrix(-1, 0, 0, -1, 0, 0)')

  const popover = page.locator('[data-docs-example="true"]:has(#tailwind-elements-popover-tab-preview)')
  const popoverButton = popover.getByRole('button', { name: 'Account' })
  await popoverButton.click()
  const popoverPanel = popover.locator('el-popover')
  await expect(popoverPanel).toBeVisible()
  const popoverButtonBox = await popoverButton.boundingBox()
  const popoverPanelBox = await popoverPanel.boundingBox()
  expect(popoverButtonBox).not.toBeNull()
  expect(popoverPanelBox).not.toBeNull()
  expect(popoverPanelBox!.x).toBeGreaterThanOrEqual(popoverButtonBox!.x - 1)
  expect(popoverPanelBox!.y).toBeGreaterThan(popoverButtonBox!.y)
  await page.keyboard.press('Escape')

  const dialogExample = page.locator('[data-docs-example="true"]:has(#tailwind-elements-dialog-tab-preview)')
  await dialogExample.getByRole('button', { name: 'Delete profile' }).click()
  const dialogPanel = page.locator('#preview-delete-profile el-dialog-panel')
  await expect(dialogPanel).toBeVisible()
  await expect(dialogPanel).not.toHaveAttribute('data-closed', '')
  await expect(dialogPanel).toHaveCSS('opacity', '1')
  const dialogPanelBox = await dialogPanel.boundingBox()
  expect(dialogPanelBox).not.toBeNull()
  expect(dialogPanelBox!.x).toBeGreaterThan(100)
  expect(dialogPanelBox!.y).toBeGreaterThan(100)
  await page.keyboard.press('Escape')

  const tabs = page.locator('[data-docs-example="true"]:has(#tailwind-elements-tabs-tab-preview)')
  const accountTab = tabs.getByRole('tab', { name: 'Account' })
  const securityTab = tabs.getByRole('tab', { name: 'Security' })
  await securityTab.click()
  await expect(tabs.getByRole('tabpanel', { name: 'Security' })).toBeVisible()
  await expect(accountTab).toHaveCSS('border-bottom-color', 'rgba(0, 0, 0, 0)')
  await expect(securityTab).toHaveCSS('border-bottom-color', 'rgb(14, 165, 233)')

  await page.evaluate(() => document.documentElement.classList.add('dark'))
  await expect(autocompleteSurface).toHaveCSS('background-color', 'rgb(17, 17, 17)')
  await expect(autocomplete.locator('input')).toHaveCSS('background-color', 'rgb(31, 31, 31)')
  expect(browserErrors).toEqual([])
})

test('visual SVG examples provide rendered previews', async ({ page }) => {
  await page.goto('/extensions/svg', { waitUntil: 'domcontentloaded' })
  await expect(page.locator('[data-docs-example="true"]')).toHaveCount(3)

  for (const id of ['svg-icon-example', 'svg-chart-example', 'svg-resources-example']) {
    const example = page.locator(`[data-docs-example="true"]:has(#${id}-tab-preview)`)
    await example.getByRole('tab', { name: 'Preview' }).click()
    await expect(example.getByRole('tabpanel', { name: 'Preview' }).locator('svg')).toBeVisible()
  }
})

test('diagram previews rerender after their hidden panel becomes visible', async ({ page }) => {
  await page.goto('/docs/components/diagrams', { waitUntil: 'domcontentloaded' })
  const example = page.locator('[data-docs-example="true"]:has(#docs-mermaid-example-tab-preview)')
  await example.getByRole('tab', { name: 'Preview' }).click()
  const preview = example.locator('iframe').contentFrame()
  const diagram = preview.locator('.mermaid svg')
  await expect(diagram).toBeVisible()
  await expect.poll(() => diagram.getAttribute('viewBox')).not.toBe('-8 -8 16 16')
  expect((await diagram.boundingBox())!.width).toBeGreaterThan(200)
})

test('documentation remains readable when text is resized to 200 percent', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 800 })
  await page.goto('/docs', { waitUntil: 'domcontentloaded' })
  await page.locator('html').evaluate(element => { element.style.fontSize = '200%' })

  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  const bounds = await page.evaluate(() => {
    const main = document.querySelector('main')!.getBoundingClientRect()
    const heading = document.querySelector('h1')!.getBoundingClientRect()
    return { mainLeft: main.left, mainRight: main.right, headingLeft: heading.left, headingRight: heading.right }
  })
  expect(bounds.headingLeft).toBeGreaterThanOrEqual(bounds.mainLeft)
  expect(bounds.headingRight).toBeLessThanOrEqual(bounds.mainRight)
})

test('Docs catalog navigation updates articles without a full-page browser error', async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/docs/page-examples/api-reference', { waitUntil: 'domcontentloaded' })
  await expect(page.locator('#nav-fsharp-viewengine-docs')).toHaveAttribute('aria-expanded', 'true')
  await expect(page.locator('#nav-children-fsharp-viewengine-docs > li > #nav-components')).toBeVisible()
  await expect(page.locator('#nav-children-fsharp-viewengine-docs > li > #nav-page-examples')).toBeVisible()
  await page.locator('#nav-components').click()
  await page.locator('#nav-docs-layouts').click()
  await expect(page).toHaveURL('/docs/components/layouts')
  await expect(page.getByRole('heading', { level: 1, name: 'Layouts' })).toBeVisible()
  await expect(page.locator('.docs-article-layout')).toBeVisible()
  expect(browserErrors).toEqual([])
})

test('Docs catalog host documents retain one page heading and unique IDs', async ({ page }) => {
  const catalogRoutes = routes.filter(route => route.path.startsWith('/docs/components/') || route.path.startsWith('/docs/page-examples/'))

  for (const route of catalogRoutes) {
    await page.goto(route.path, { waitUntil: 'domcontentloaded' })
    await expect(page.locator('#page-content h1')).toHaveCount(1)
    const duplicateIds = await page.evaluate(() => {
      const counts = new Map<string, number>()
      for (const element of document.querySelectorAll<HTMLElement>('[id]')) {
        counts.set(element.id, (counts.get(element.id) ?? 0) + 1)
      }
      return Array.from(counts.entries()).filter(([, count]) => count > 1)
    })
    expect(duplicateIds, route.path).toEqual([])
  }
})

test('Docs component and page-example catalogs use source-first code and complete styled previews', async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  const catalogRoutes = routes.filter(route => route.path.startsWith('/docs/components/') || route.path.startsWith('/docs/page-examples/'))
  let reviewedPreviews = 0

  for (const route of catalogRoutes) {
    await page.goto(route.path, { waitUntil: 'domcontentloaded' })
    reviewedPreviews += await page.locator('button[role="tab"][id^="docs-"][id$="-example-tab-preview"]:visible').count()
    const examples = page.locator('[data-docs-example="true"]')
    expect(await examples.count(), route.path).toBeGreaterThan(0)
    for (const example of await examples.all()) {
      await expect(example.getByRole('tab', { name: 'Code' })).toHaveAttribute('aria-selected', 'true')
      await example.getByRole('tab', { name: 'Preview' }).click()
      const preview = example.getByRole('tabpanel', { name: 'Preview' })
      await expect(preview).toBeVisible()
      const iframe = preview.locator('iframe')
      if (await iframe.count()) {
        const frame = iframe.contentFrame()
        await expect(frame.locator('body')).toHaveClass(/spec-document/)
        expect(await frame.locator('style, link[rel="stylesheet"]').count(), route.path).toBeGreaterThan(0)
        expect(await frame.locator('html').evaluate(element => element.scrollWidth <= element.clientWidth), route.path).toBe(true)
      } else {
        expect(await preview.evaluate(element => element.scrollWidth <= element.clientWidth), route.path).toBe(true)
      }
    }
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), route.path).toBe(true)
  }
  expect(reviewedPreviews).toBe(23)
  expect(browserErrors).toEqual([])
})

test('catalog code is extracted from the same compiled definition as its preview', async ({ page }) => {
  await page.goto('/docs/components/interactive-examples', { waitUntil: 'domcontentloaded' })
  const example = page.locator('[data-docs-example="true"]:has(#docs-state-tabs-example-tab-preview)')
  const source = await example.getByRole('tabpanel', { name: 'Code' }).innerText()
  expect(source).toContain('docsStateTabs "component-workflow-states"')
  expect(source).toContain('productScreen "ready"')
  expect(source).not.toContain('docsStateTabs "workflow-states"')

  await example.getByRole('tab', { name: 'Preview' }).click()
  const preview = example.getByRole('tabpanel', { name: 'Preview' })
  await expect(preview.locator('iframe')).toHaveCount(0)
  await expect(preview.locator('#component-workflow-states-tab-ready')).toBeVisible()
  await expect(preview.locator('input[value="accountSummary"]')).toBeVisible()

  const browserFrame = page.locator('[data-docs-example="true"]:has(#docs-browser-frame-example-tab-preview)')
  await browserFrame.getByRole('tab', { name: 'Preview' }).click()
  const browserPreview = browserFrame.getByRole('tabpanel', { name: 'Preview' })
  await expect(browserPreview.locator('iframe')).toHaveCount(0)
  await expect(browserPreview.locator('.spec-browser-frame')).toBeVisible()
})

test('benchmark comparison remains legible in light and dark themes', async ({ page }) => {
  for (const theme of ['light', 'dark'] as const) {
    await page.addInitScript(selected => localStorage.setItem('fsharp-viewengine-docs-navigation-color-mode', selected), theme)
    await page.goto('/benchmarks', { waitUntil: 'domcontentloaded' })
    const chart = page.locator('.docs-comparison-chart')
    await expect(chart).toBeVisible()
    await expect(chart.getByText('Mean duration · Lower is better')).toBeVisible()
    const colors = await chart.evaluate(element => {
      const style = getComputedStyle(element)
      const text = element.querySelector('.docs-comparison-labels strong')!
      return { background: style.backgroundColor, text: getComputedStyle(text).color }
    })
    expect(colors.background).not.toBe(colors.text)
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  }
})

test('API reference page example renders endpoint and request-response composition', async ({ page }) => {
  await page.goto('/docs/page-examples/api-reference', { waitUntil: 'domcontentloaded' })
  const example = page.locator('[data-docs-example="true"]').first()
  await example.getByRole('tab', { name: 'Preview' }).click()
  const preview = example.locator('iframe').contentFrame()
  await expect(preview.locator('[data-http-method="POST"]')).toBeVisible()
  await expect(preview.locator('.docs-code-panel')).toHaveCount(2)

  await page.setViewportSize({ width: 390, height: 844 })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})

test('specification page example state tabs support pointer and keyboard navigation', async ({ page }) => {
  await page.goto('/docs/page-examples/executable-specification', { waitUntil: 'domcontentloaded' })
  const example = page.locator('[data-docs-example="true"]').first()
  await example.getByRole('tab', { name: 'Preview' }).click()
  const preview = example.locator('iframe').contentFrame()
  const ready = preview.getByRole('tab', { name: 'Ready' })
  const validation = preview.getByRole('tab', { name: 'Validation' })

  await validation.click()
  await expect(validation).toHaveAttribute('aria-selected', 'true')
  await expect(preview.getByRole('tabpanel', { name: 'Validation' })).toBeVisible()

  await validation.press('ArrowLeft')
  await expect(ready).toBeFocused()
  await expect(ready).toHaveAttribute('aria-selected', 'true')
})

test('benchmark tables remain readable without page overflow on mobile', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/benchmarks', { waitUntil: 'domcontentloaded' })

  const comparison = page.getByRole('figure', { name: 'Build and render comparison' })
  await expect(comparison).toBeVisible()
  await expect(comparison).toContainText('FSharp.ViewEngine')
  await expect(comparison).toContainText('1.35× as long')
  await expect(comparison.locator('.docs-comparison-bar')).toHaveCount(4)
  await expect(page.getByRole('table')).toHaveCount(7)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})

test('sitemap, robots, and social metadata expose canonical public discovery', async ({ request, page }) => {
  const sitemap = await request.get('/sitemap.xml')
  expect(sitemap.status()).toBe(200)
  expect(sitemap.headers()['content-type']).toContain('application/xml')
  const sitemapXml = await sitemap.text()
  for (const route of routes) {
    const canonicalURL = route.path === '/' ? `${productionOrigin}/` : `${productionOrigin}${route.path}`
    expect(sitemapXml, route.path).toContain(`<loc>${canonicalURL}</loc>`)
  }
  expect(sitemapXml).not.toContain('/docs/components</loc>')
  expect(sitemapXml).not.toContain('/docs/previews/')

  const robots = await request.get('/robots.txt')
  expect(robots.status()).toBe(200)
  expect(await robots.text()).toBe(`User-agent: *\nAllow: /\nSitemap: ${productionOrigin}/sitemap.xml\n`)

  await page.goto('/getting-started/first-view', { waitUntil: 'domcontentloaded' })
  await expect(page.locator('meta[property="og:url"]')).toHaveAttribute('content', `${productionOrigin}/getting-started/first-view`)
  await expect(page.locator('meta[property="og:type"]')).toHaveAttribute('content', 'website')
  await expect(page.locator('meta[property="og:image"]')).toHaveAttribute('content', `${productionOrigin}/social-card.png`)
  await expect(page.locator('meta[property="og:image:alt"]')).toHaveAttribute('content', 'Build your first view')
  await expect(page.locator('meta[name="twitter:card"]')).toHaveAttribute('content', 'summary_large_image')
  const image = await request.get('/social-card.png')
  expect(image.status()).toBe(200)
  expect(image.headers()['content-type']).toContain('image/png')
})

test('removed Tailwind documentation route returns 404', async ({ request }) => {
  const response = await request.get('/extensions/tailwind')
  expect(response.status()).toBe(404)
})
