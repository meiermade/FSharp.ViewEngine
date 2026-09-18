import AxeBuilder from '@axe-core/playwright'
import { expect, test, type Locator, type Page } from '@playwright/test'

const productionOrigin = 'https://fsharpviewengine.meiermade.com'
const crossBrowser = { tag: '@cross-browser' }

async function openComponentGallery(page: Page, path: string, heading: string) {
  await gotoAfterDocsAssetSettlement(page, path)
  await expect(page.getByRole('heading', { level: 1, name: heading, exact: true })).toBeVisible()
  const examples = page.locator('.docs-gallery-layout [data-docs-example="true"]')
  expect(await examples.count()).toBeGreaterThan(0)
  for (const example of await examples.all()) {
    const preview = example.locator(':scope > .spec-example-toolbar').getByRole('tab', { name: 'Preview', exact: true })
    await expect(preview).toHaveAttribute('aria-selected', 'true')
    await expect(page.locator(`#${await preview.getAttribute('aria-controls')}`)).toBeVisible()
  }
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), path).toBe(true)
  return page.locator('.docs-gallery-layout .spec-example-preview .fve-components')
}

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
  { path: '/docs', heading: 'Documentation', layout: 'article' },
  { path: '/docs/components/layouts', heading: 'Layouts', layout: 'article' },
  { path: '/docs/components/content', heading: 'Content', layout: 'article' },
  { path: '/docs/components/navigation', heading: 'Navigation', layout: 'article' },
  { path: '/docs/components/fixture', heading: 'Fixture', layout: 'article' },
  { path: '/docs/components/api-reference', heading: 'API reference components', layout: 'article' },
  { path: '/docs/components/diagrams', heading: 'Diagrams', layout: 'article' },
  { path: '/docs/page-examples/documentation-site', heading: 'Documentation site', layout: 'gallery' },
  { path: '/docs/page-examples/api-reference', heading: 'API reference page', layout: 'gallery' },
  { path: '/docs/page-examples/executable-specification', heading: 'Executable specification page', layout: 'gallery' },
  { path: '/components', heading: 'Components', layout: 'article' },
  { path: '/components/primitives', heading: 'Primitives', layout: 'article' },
  { path: '/components/application', heading: 'Application', layout: 'article' },
  { path: '/components/installation', heading: 'Installation', layout: 'article' },
  { path: '/components/button', heading: 'Button', layout: 'gallery' },
  { path: '/components/icon-button', heading: 'Icon button', layout: 'gallery' },
  { path: '/components/badge', heading: 'Badge', layout: 'gallery' },
  { path: '/components/status', heading: 'Status', layout: 'gallery' },
  { path: '/components/loading-indicator', heading: 'Loading indicator', layout: 'gallery' },
  { path: '/components/progress', heading: 'Progress', layout: 'gallery' },
  { path: '/components/empty-state', heading: 'Empty state', layout: 'gallery' },
  { path: '/components/action-cluster', heading: 'Action cluster', layout: 'gallery' },
  { path: '/components/row-actions', heading: 'Row actions', layout: 'gallery' },
  { path: '/components/table', heading: 'Table', layout: 'gallery' },
  { path: '/components/description-list', heading: 'Description list', layout: 'gallery' },
  { path: '/components/metric', heading: 'Metric', layout: 'gallery' },
  { path: '/components/pagination', heading: 'Pagination', layout: 'gallery' },
  { path: '/components/avatar', heading: 'Avatar', layout: 'gallery' },
  { path: '/components/copy-reveal', heading: 'Copy and reveal', layout: 'gallery' },
  { path: '/components/input', heading: 'Input', layout: 'gallery' },
  { path: '/components/file-selection', heading: 'File selection', layout: 'gallery' },
  { path: '/components/tag-input', heading: 'Tag input', layout: 'gallery' },
  { path: '/components/form-layouts', heading: 'Form layouts', layout: 'gallery' },
  { path: '/components/textarea', heading: 'Textarea', layout: 'gallery' },
  { path: '/components/error-summary', heading: 'Error summary', layout: 'gallery' },
  { path: '/components/notice', heading: 'Notice', layout: 'gallery' },
  { path: '/components/select', heading: 'Select', layout: 'gallery' },
  { path: '/components/checkbox', heading: 'Checkbox', layout: 'gallery' },
  { path: '/components/switch', heading: 'Switch', layout: 'gallery' },
  { path: '/components/toggle-button', heading: 'Toggle button', layout: 'gallery' },
  { path: '/components/breadcrumbs', heading: 'Breadcrumbs', layout: 'gallery' },
  { path: '/components/side-nav', heading: 'Side nav', layout: 'gallery' },
  { path: '/components/tabs', heading: 'Tabs', layout: 'gallery' },
  { path: '/components/radio-group', heading: 'Radio group', layout: 'gallery' },
  { path: '/components/choice-cards', heading: 'Choice cards', layout: 'gallery' },
  { path: '/components/dropdown-menu', heading: 'Dropdown menu', layout: 'gallery' },
  { path: '/components/dialog', heading: 'Dialog', layout: 'gallery' },
  { path: '/components/confirmation-dialog', heading: 'Confirmation dialog', layout: 'gallery' },
  { path: '/components/drawer', heading: 'Drawer', layout: 'gallery' },
  { path: '/components/page-top-bar', heading: 'Page top bar', layout: 'gallery' },
  { path: '/components/page-header', heading: 'Page header', layout: 'gallery' },
  { path: '/components/section', heading: 'Section', layout: 'gallery' },
  { path: '/components/browser', heading: 'Browser', layout: 'gallery' },
  { path: '/components/phone', heading: 'Phone', layout: 'gallery' },
  { path: '/components/page', heading: 'Page', layout: 'gallery' },
  { path: '/components/collection', heading: 'Collection', layout: 'gallery' },
  { path: '/components/detail', heading: 'Detail', layout: 'gallery' },
  { path: '/components/app-shell', heading: 'App shell', layout: 'gallery' },
  { path: '/components/page-examples/account-management', heading: 'Account management', layout: 'gallery' },
  { path: '/components/bottom-navigation', heading: 'Bottom navigation', layout: 'gallery' },
  { path: '/components/bulk-actions', heading: 'Bulk actions', layout: 'gallery' },
  { path: '/components/upload', heading: 'Upload', layout: 'gallery' },
  { path: '/components/steps', heading: 'Steps', layout: 'gallery' },
  { path: '/components/first-steps', heading: 'First steps', layout: 'gallery' },
  { path: '/components/calendar', heading: 'Calendar', layout: 'gallery' },
  { path: '/components/media-library', heading: 'Media library', layout: 'gallery' },
  { path: '/components/page-examples/dependency-graph', heading: 'Dependency graph', layout: 'gallery' },
  { path: '/components/page-examples/execution-detail', heading: 'Execution detail', layout: 'gallery' },
  { path: '/components/page-examples/operations-dashboard', heading: 'Operations dashboard', layout: 'gallery' },
  { path: '/components/page-examples/scheduling', heading: 'Scheduling', layout: 'gallery' },
  { path: '/components/page-examples/media-management', heading: 'Media management', layout: 'gallery' },
  { path: '/components/page-examples/financial-reporting', heading: 'Financial reporting', layout: 'gallery' },
  { path: '/components/page-examples/messaging', heading: 'Messaging', layout: 'gallery' },
  { path: '/components/interaction-and-server-state', heading: 'Interaction and server state', layout: 'article' },
  { path: '/components/accessibility', heading: 'Accessibility', layout: 'article' },
  { path: '/components/theming', heading: 'Theming and density', layout: 'article' },
  { path: '/components/tailwind-css', heading: 'Tailwind CSS setup', layout: 'article' },
  { path: '/components/customization', heading: 'Customization', layout: 'article' },
  { path: '/components/versioning', heading: 'Versioning', layout: 'article' },
  { path: '/benchmarks', heading: 'Benchmarks', layout: 'article' },
  { path: '/changelog', heading: 'Changelog', layout: 'article' },
]

const componentAccessibilityRouteGroups = [
  {
    name: 'foundations and data display',
    paths: ['/components', '/components/icon-button', '/components/action-cluster', '/components/row-actions', '/components/loading-indicator', '/components/progress', '/components/empty-state', '/components/table', '/components/description-list', '/components/metric', '/components/pagination'],
  },
  {
    name: 'fields and navigation',
    paths: ['/components/avatar', '/components/copy-reveal', '/components/file-selection', '/components/tag-input', '/components/select', '/components/checkbox', '/components/switch', '/components/toggle-button', '/components/breadcrumbs', '/components/side-nav', '/components/bottom-navigation'],
  },
  {
    name: 'choices overlays and page composition',
    paths: ['/components/tabs', '/components/radio-group', '/components/choice-cards', '/components/dropdown-menu', '/components/dialog', '/components/confirmation-dialog', '/components/drawer', '/components/page-top-bar', '/components/page-header', '/components/section', '/components/page'],
  },
  {
    name: 'application workflows',
    paths: ['/components/app-shell', '/components/page-examples/account-management', '/components/bulk-actions', '/components/upload', '/components/steps', '/components/first-steps', '/components/calendar', '/components/media-library'],
  },
  {
    name: 'page examples',
    paths: ['/components/page-examples/dependency-graph', '/components/page-examples/execution-detail', '/components/page-examples/financial-reporting', '/components/page-examples/messaging', '/components/page-examples/operations-dashboard', '/components/page-examples/scheduling', '/components/page-examples/media-management'],
  },
]

function captureBrowserErrors(page: Page) {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  page.on('console', message => {
    if (message.type() === 'error') errors.push(message.text())
  })
  return errors
}

async function waitForDocsCodeSettlement(page: Page) {
  // A commit-only navigation has not necessarily parsed its code blocks yet.
  await page.waitForLoadState('domcontentloaded')
  await page.waitForFunction(() =>
    !document.querySelector('code[class*="language-"]') || Boolean((window as any).fsharpDocsCode?.loading),
  )
  await page.evaluate(() => (window as any).fsharpDocsCode?.loading)
}

// Request routing disables Playwright's HTTP cache, so settle the current document's
// own assets before ordinary full navigation in workflows that mock the CDN runtime.
async function waitForDocsAssetSettlement(page: Page) {
  await waitForDocsCodeSettlement(page)
  await page.evaluate(() => document.fonts?.ready)
}

async function gotoAfterDocsAssetSettlement(page: Page, path: string, waitUntil: 'commit' | 'domcontentloaded' = 'commit') {
  if (page.url() !== 'about:blank') await waitForDocsAssetSettlement(page)
  const response = await page.goto(path, { waitUntil })
  await waitForDocsAssetSettlement(page)
  return response
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

test('removed Components routes return not found', async ({ request }) => {
  for (const path of ['/components/contract', '/components/chart', '/components/layouts']) {
    const response = await request.get(path)
    expect(response.status(), path).toBe(404)
  }
})

test('canonical routes expose valid same-origin links, assets, and lazy previews', async ({ page, request }) => {
  test.slow()
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

test.describe('automated accessibility checks', crossBrowser, () => {
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

test('Components pages provide focused examples, navigation, interaction, themes, and responsive accessibility', crossBrowser, async ({ page }, testInfo) => {
  test.slow()
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )
  const browserErrors = captureBrowserErrors(page)
  const attachScreenshot = async (name: string) => {
    if (testInfo.project.name !== 'chromium') return
    await testInfo.attach(name, {
      body: await page.screenshot({ fullPage: true, animations: 'disabled' }),
      contentType: 'image/png',
    })
  }
  const componentRoutes = [
    ['/components/button', 'Button'],
    ['/components/icon-button', 'Icon button'],
    ['/components/badge', 'Badge'],
    ['/components/status', 'Status'],
    ['/components/loading-indicator', 'Loading indicator'],
    ['/components/progress', 'Progress'],
    ['/components/empty-state', 'Empty state'],
    ['/components/action-cluster', 'Action cluster'],
    ['/components/row-actions', 'Row actions'],
    ['/components/table', 'Table'],
    ['/components/description-list', 'Description list'],
    ['/components/metric', 'Metric'],
    ['/components/pagination', 'Pagination'],
    ['/components/avatar', 'Avatar'],
    ['/components/copy-reveal', 'Copy and reveal'],
    ['/components/file-selection', 'File selection'],
    ['/components/tag-input', 'Tag input'],
    ['/components/select', 'Select'],
    ['/components/checkbox', 'Checkbox'],
    ['/components/switch', 'Switch'],
    ['/components/toggle-button', 'Toggle button'],
    ['/components/breadcrumbs', 'Breadcrumbs'],
    ['/components/side-nav', 'Side nav'],
    ['/components/tabs', 'Tabs'],
    ['/components/radio-group', 'Radio group'],
    ['/components/choice-cards', 'Choice cards'],
    ['/components/dropdown-menu', 'Dropdown menu'],
    ['/components/dialog', 'Dialog'],
    ['/components/page-top-bar', 'Page top bar'],
    ['/components/page-header', 'Page header'],
    ['/components/section', 'Section'],
    ['/components/page', 'Page'],
    ['/components/collection', 'Collection'],
    ['/components/detail', 'Detail'],
    ['/components/app-shell', 'App shell'],
    ['/components/page-examples/account-management', 'Account management'],
    ['/components/bottom-navigation', 'Bottom navigation'],
    ['/components/bulk-actions', 'Bulk actions'],
    ['/components/upload', 'Upload'],
    ['/components/steps', 'Steps'],
    ['/components/first-steps', 'First steps'],
    ['/components/calendar', 'Calendar'],
    ['/components/media-library', 'Media library'],
    ['/components/page-examples/dependency-graph', 'Dependency graph'],
    ['/components/page-examples/execution-detail', 'Execution detail'],
    ['/components/page-examples/operations-dashboard', 'Operations dashboard'],
    ['/components/page-examples/scheduling', 'Scheduling'],
    ['/components/page-examples/media-management', 'Media management'],
    ['/components/page-examples/financial-reporting', 'Financial reporting'],
    ['/components/page-examples/messaging', 'Messaging'],
  ] as const

  const openPreview = (path: string, heading: string) => openComponentGallery(page, path, heading)

  for (const [path, heading] of componentRoutes) {
    const surface = await openPreview(path, heading)
    if (path === '/components/collection' || path === '/components/page-examples/account-management') {
      const accountType = surface.getByRole('combobox', { name: 'Filter by account type', exact: true })
      await expect(accountType).toHaveCount(1)
      await expect(surface.locator('select[name="accountType"]')).toHaveCount(path === '/components/collection' ? 1 : 0)
    } else {
      await expect(surface.locator('select')).toHaveCount(0)
    }
    const duplicateIds = await page.locator('[id]').evaluateAll(elements => {
      const ids = elements.map(element => element.id)
      return [...new Set(ids.filter((id, index) => ids.indexOf(id) !== index))]
    })
    expect(duplicateIds, path).toEqual([])
  }

  await gotoAfterDocsAssetSettlement(page, '/components/select', 'domcontentloaded')
  const packageNavOrder = await page.locator('#nav-fsharp-viewengine-components-primitives, #nav-fsharp-viewengine-components-application, #nav-fsharp-viewengine-components-documentation').evaluateAll(elements => elements.map(element => element.id))
  expect(packageNavOrder).toEqual(['nav-fsharp-viewengine-components-primitives', 'nav-fsharp-viewengine-components-application', 'nav-fsharp-viewengine-components-documentation'])
  await expect(page.locator('#nav-fsharp-viewengine-components-primitives')).toHaveAttribute('aria-expanded', 'true')
  await expect(page.locator('#nav-fsharp-viewengine-components-primitives-form-controls')).toHaveAttribute('aria-expanded', 'true')
  await expect(page.locator('#nav-components-select')).toHaveAttribute('data-selected', 'true')

  const resolvedBackground = (root: Locator, variable: string) => root.first().evaluate((element, cssVariable) => {
    const probe = document.createElement('span')
    probe.style.backgroundColor = `var(${cssVariable})`
    element.appendChild(probe)
    const value = getComputedStyle(probe).backgroundColor
    const canvas = document.createElement('canvas')
    canvas.width = canvas.height = 1
    const context = canvas.getContext('2d')!
    context.fillStyle = value
    context.fillRect(0, 0, 1, 1)
    const pixel = Array.from(context.getImageData(0, 0, 1, 1).data).join(',')
    probe.remove()
    return pixel
  }, variable)

  const pressedBackgrounds = async (control: Locator) => {
    const settledBackground = () => control.evaluate(async element => {
      await Promise.all(element.getAnimations().map(animation => animation.finished))
      return getComputedStyle(element).backgroundColor
    })
    await control.hover()
    const hover = await settledBackground()
    await page.mouse.down()
    try {
      const active = await settledBackground()
      expect(active).not.toBe(hover)
      return { hover, active }
    } finally {
      await page.mouse.up()
    }
  }

  const activationCount = async (_surface: Locator, _id: string) =>
    page.evaluate(() => (window as any).__nativeButtonActivations ?? 0)

  const observeActivations = () => page.evaluate(() => {
    ;(window as any).__nativeButtonActivations = 0
    document.querySelector('.docs-gallery-layout')!.addEventListener('click', event => {
      if ((event.target as Element).closest('.fve-components button')) (window as any).__nativeButtonActivations++
    })
  })

  const expectUnavailableActivationPrevention = async (control: Locator) => {
    await control.evaluate(element => {
      element.setAttribute('data-native-activations', '0')
      element.addEventListener('click', () => element.setAttribute('data-native-activations', '1'))
    })
    await control.scrollIntoViewIfNeeded()
    const box = (await control.boundingBox())!
    await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2)
    await control.evaluate(element => (element as HTMLElement).focus())
    await expect(control).not.toBeFocused() // Native disabled controls cannot receive keyboard activation.
    await expect(control).toHaveAttribute('data-native-activations', '0')
  }

  const buttonSurface = await openPreview('/components/button', 'Button')
  const docsRoot = page.locator(':root')
  const lightPage = await resolvedBackground(buttonSurface, '--fve-page')
  const lightDocsPage = await resolvedBackground(docsRoot, '--spec-bg')
  const lightBrand = await resolvedBackground(buttonSurface, '--fve-brand-solid')
  const lightDocsAccent = await resolvedBackground(docsRoot, '--spec-accent-700')
  expect(lightPage).toBe(lightDocsPage)
  expect(lightBrand).toBe(lightDocsAccent)

  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  const darkPage = await resolvedBackground(buttonSurface, '--fve-page')
  const darkDocsPage = await resolvedBackground(docsRoot, '--spec-bg')
  const darkBrand = await resolvedBackground(buttonSurface, '--fve-brand-solid')
  const darkDocsAccent = await resolvedBackground(docsRoot, '--spec-accent-700')
  expect(darkPage).toBe(darkDocsPage)
  expect(darkPage).not.toBe(lightPage)
  expect(darkBrand).toBe(darkDocsAccent)

  await observeActivations()
  for (const name of ['Create account', 'View reports', 'Cancel', 'Delete account']) {
    const { hover, active } = await pressedBackgrounds(buttonSurface.getByRole('button', { name, exact: true }).first())
    expect(active, `${name} active background`).not.toBe(hover)
  }

  const buttonCountBeforeEnabledActivation = await activationCount(buttonSurface, 'button-activation-count')
  const enabledButton = buttonSurface.getByRole('button', { name: 'View reports' }).first()
  await enabledButton.click()
  await enabledButton.press('Enter')
  await enabledButton.press('Space')
  await expect.poll(() => activationCount(buttonSurface, 'button-activation-count')).toBe(buttonCountBeforeEnabledActivation + 3)

  const pendingButton = buttonSurface.getByRole('button', { name: 'Sync accounts' })
  const disabledButton = buttonSurface.getByRole('button', { name: 'Delete account' }).and(page.locator(':disabled'))
  await expect(pendingButton).toBeDisabled()
  await expect(pendingButton).toHaveAttribute('aria-busy', 'true')
  await expect(pendingButton).toContainText('Sync accounts')
  await expect(disabledButton).toBeDisabled()
  await expectUnavailableActivationPrevention(pendingButton)
  await expectUnavailableActivationPrevention(disabledButton)

  const iconButtonSurface = await openPreview('/components/icon-button', 'Icon button')
  await observeActivations()
  const addAccount = iconButtonSurface.getByRole('button', { name: 'Add account' })
  await addAccount.focus()
  await expect(addAccount).toBeFocused()
  await expect(addAccount.locator('[aria-hidden="true"]')).toBeVisible()
  for (const name of ['Add account', 'Refresh accounts']) {
    const { hover, active } = await pressedBackgrounds(iconButtonSurface.getByRole('button', { name }))
    expect(active, `${name} active background`).not.toBe(hover)
  }
  const iconCountBeforeEnabledActivation = await activationCount(iconButtonSurface, 'icon-button-activation-count')
  await addAccount.click()
  await expect.poll(() => activationCount(iconButtonSurface, 'icon-button-activation-count')).toBe(iconCountBeforeEnabledActivation + 1)

  const refreshingAccounts = iconButtonSurface.getByRole('button', { name: 'Refreshing accounts' })
  const disabledRemoveAccount = iconButtonSurface.getByRole('button', { name: 'Remove account' })
  await expect(refreshingAccounts).toBeDisabled()
  await expect(refreshingAccounts).toHaveAttribute('aria-busy', 'true')
  await expect(disabledRemoveAccount).toBeDisabled()
  await expectUnavailableActivationPrevention(refreshingAccounts)
  await expectUnavailableActivationPrevention(disabledRemoveAccount)

  const badgeSurface = await openPreview('/components/badge', 'Badge')
  await expect(badgeSurface.getByText('Internal', { exact: true })).toBeVisible()
  await expect(badgeSurface.getByText('Reconciled', { exact: true })).toBeVisible()

  const loadingSurface = await openPreview('/components/loading-indicator', 'Loading indicator')
  await expect(loadingSurface.getByRole('status')).toHaveCount(2)
  await expect(loadingSurface.getByText('Loading account balances')).toHaveClass(/sr-only/)
  await expect(loadingSurface.getByText('Refreshing transactions')).toBeVisible()

  const emptyStateSurface = await openPreview('/components/empty-state', 'Empty state')
  await expect(emptyStateSurface.getByText('No accounts yet', { exact: true })).toBeVisible()
  await expect(emptyStateSurface.getByRole('link', { name: 'Create account' })).toHaveAttribute('href', '/components/page-examples/account-management?destination=ledger-create-account')

  const simpleTables = await openPreview('/components/table', 'Table')
  await expect(simpleTables.getByRole('table')).toHaveCount(7)
  await expect(simpleTables.locator('th[aria-sort] a')).toHaveCount(1)
  await expect(simpleTables.getByRole('link', { name: 'Operating checking', exact: true })).toBeVisible()
  await expect(simpleTables.first().getByRole('columnheader')).toHaveText(['Name', 'Email', 'Role'])
  // Rich record menus belong to the integrated Collection fixture, not the basic Table gallery.
  const tableSurface = await openPreview('/components/collection', 'Collection')
  const accountTable = tableSurface.getByRole('table', { name: 'Accounts' })
  await expect(accountTable.locator('caption')).toHaveClass('sr-only')
  await expect(accountTable.getByRole('rowheader')).toHaveCount(6)
  const accountTableRegion = tableSurface.locator('[data-fve-table]').getByRole('region', { name: 'Accounts', exact: true })
  await expect(accountTableRegion).toHaveAttribute('tabindex', '0')
  await expect(accountTableRegion).not.toHaveClass(/rounded-\[var\(--fve-radius-panel\)\]/)
  const actionsHeading = accountTable.getByRole('columnheader', { name: 'Actions' })
  await expect(actionsHeading.locator('.sr-only')).toHaveText('Actions')
  const assetsActions = accountTable.getByRole('button', { name: 'More actions for Assets' })
  await expect(assetsActions).toBeVisible()
  await expect(accountTable.getByRole('link', { name: 'View account' })).toHaveCount(0)
  await assetsActions.click()
  const assetsMenu = accountTable.getByRole('menu', { name: 'More actions for Assets' })
  await expect(assetsMenu).toBeVisible()
  await expect(assetsMenu).toHaveCSS('position', 'fixed')
  await expect(assetsMenu).toBeFocused()
  await page.keyboard.press('ArrowDown')
  const viewAccountItem = assetsMenu.getByRole('menuitem', { name: 'View account' })
  await expect(viewAccountItem).toHaveAttribute('href', '/components/page-examples/account-management?destination=ledger-account-101')
  await expect(viewAccountItem).toBeFocused()
  await expect(viewAccountItem).not.toHaveCSS('background-color', 'rgba(0, 0, 0, 0)')
  await expect(viewAccountItem).toHaveCSS('box-shadow', 'none')
  await page.keyboard.press('Escape')
  await expect(assetsActions).toBeFocused()

  const descriptionListSurface = await openPreview('/components/description-list', 'Description list')
  await expect(descriptionListSurface.locator('dl')).toHaveCount(3)
  await expect(descriptionListSurface.first().locator('dt')).toHaveText(['Type', 'Commodity', 'Parent account', 'Source', 'Status', 'Balance'])
  await expect(descriptionListSurface.locator('dd')).toHaveCount(16)
  await expect(descriptionListSurface.getByText('Includes cleared entries through today.')).toBeVisible()

  const metricSurface = await openPreview('/components/metric', 'Metric')
  await expect(metricSurface.getByText('Available balance', { exact: true })).toBeVisible()
  await expect(metricSurface.getByText('$42,800', { exact: true })).toBeVisible()
  await expect(metricSurface.getByText('Trend: ', { exact: true })).toHaveClass(/sr-only/)
  const availableBalanceLabel = metricSurface.getByText('Available balance', { exact: true })
  const currentStatus = metricSurface.getByText('Current', { exact: true })
  const pendingEntriesLabel = metricSurface.getByText('Pending entries', { exact: true })
  await expect(currentStatus).toBeVisible()
  const [availableBalanceBox, currentStatusBox, pendingEntriesBox] = await Promise.all([
    availableBalanceLabel.boundingBox(),
    currentStatus.boundingBox(),
    pendingEntriesLabel.boundingBox(),
  ])
  if (!availableBalanceBox || !currentStatusBox || !pendingEntriesBox) {
    throw new Error('Metric labels and status must have measurable layout boxes')
  }
  expect(currentStatusBox.x - (availableBalanceBox.x + availableBalanceBox.width)).toBeLessThanOrEqual(16)
  expect(pendingEntriesBox.y - (availableBalanceBox.y + availableBalanceBox.height)).toBeGreaterThan(48)

  const paginationSurface = await openPreview('/components/pagination', 'Pagination')
  const accountPages = paginationSurface.getByRole('navigation', { name: 'Accounts pages' })
  await expect(accountPages.getByText('Showing 26–50 of 184 accounts')).toBeVisible()
  await expect(accountPages.getByText('2', { exact: true })).toHaveAttribute('aria-current', 'page')
  const nextPage = accountPages.getByText('Next', { exact: true })
  await expect(accountPages.getByRole('link', { name: 'Page 3' })).toHaveAttribute('href', '/components/pagination/page?page=3')
  await expect(nextPage).toHaveAttribute('href', '/components/pagination/page?page=3')
  await page.evaluate(() => { (window as any).__paginationDocumentMarker = true })
  await nextPage.click()
  expect(await page.evaluate(() => (window as any).__paginationDocumentMarker)).toBe(true)
  await expect(accountPages.getByText('Showing 51–75 of 184 accounts')).toBeVisible()
  await expect(accountPages.getByText('3', { exact: true })).toHaveAttribute('aria-current', 'page')
  await expect(accountPages.getByText('Previous', { exact: true })).toHaveAttribute('href', '/components/pagination/page?page=2')
  await expect(accountPages.getByText('Next', { exact: true })).toHaveAttribute('href', '/components/pagination/page?page=4')
  await accountPages.getByRole('link', { name: 'Page 8' }).click()
  await expect(accountPages.getByText('Showing 176–184 of 184 accounts')).toBeVisible()
  await expect(accountPages.getByText('8', { exact: true })).toHaveAttribute('aria-current', 'page')
  await expect(accountPages.getByText('Next', { exact: true })).toHaveAttribute('aria-disabled', 'true')
  await expect(accountPages.locator('a[href*="ledger.example.test"]')).toHaveCount(0)

  const formEntries = async (form: Locator) =>
    form.evaluate(element => [...new FormData(element as HTMLFormElement).entries()].map(([name, value]) => [name, String(value)]))

  const clonedControlEntries = async (controls: Locator) =>
    controls.evaluateAll(elements => {
      const form = document.createElement('form')
      for (const element of elements) form.appendChild(element.cloneNode(true))
      return [...new FormData(form).entries()].map(([name, value]) => [name, String(value)])
    })

  const selectSurface = await openPreview('/components/select', 'Select')
  const statusSelect = selectSurface.getByRole('combobox', { name: 'Status', exact: true })
  const statusListbox = selectSurface.getByRole('listbox', { name: 'Status' })
  const statusPopup = selectSurface.locator('#fve-select-components_status-popup')
  const selectForm = selectSurface.locator('#components-select-form-region form')
  const selectSubmit = selectSurface.getByRole('button', { name: 'Validate status' })
  const activeStatusOption = async () => statusSelect.getAttribute('aria-activedescendant')
  await expect(statusSelect).toHaveAttribute('aria-required', 'true')
  expect(await formEntries(selectForm)).toEqual([['status', '']])
  await selectSubmit.click()
  await expect(selectForm.getByRole('alert')).toHaveText('Choose an available status.')
  await expect(statusSelect).toHaveAttribute('aria-invalid', 'true')

  await statusSelect.press('Enter')
  await expect(statusListbox).toBeVisible()
  await expect(statusPopup).toHaveAttribute('popover', 'auto')
  expect(await statusPopup.evaluate(popup => popup.matches(':popover-open'))).toBe(true)
  await expect(statusSelect).toBeFocused()
  const selectScrollY = await page.evaluate(() => window.scrollY)
  const selectPopupOffset = await statusPopup.evaluate(popup => {
    const trigger = document.getElementById('fve-select-components_status-trigger')!.getBoundingClientRect()
    const bounds = popup.getBoundingClientRect()
    return { inlineStart: bounds.left - trigger.left, blockStart: bounds.top - trigger.bottom }
  })
  await page.evaluate(() => window.scrollBy(0, 40))
  await expect.poll(() => statusPopup.evaluate(popup => {
    const trigger = document.getElementById('fve-select-components_status-trigger')!.getBoundingClientRect()
    const bounds = popup.getBoundingClientRect()
    return { inlineStart: bounds.left - trigger.left, blockStart: bounds.top - trigger.bottom }
  })).toEqual(selectPopupOffset)
  expect(await statusPopup.evaluate(popup => {
    const bounds = popup.getBoundingClientRect()
    const paintedElement = document.elementFromPoint(bounds.left + bounds.width / 2, bounds.top + bounds.height / 2)
    return paintedElement === popup || (paintedElement !== null && popup.contains(paintedElement))
  })).toBe(true)
  await page.evaluate(scrollY => window.scrollTo(0, scrollY), selectScrollY)
  const openSelectAccessibility = await new AxeBuilder({ page })
    .include('#fve-select-components_status-trigger')
    .include('#fve-select-components_status-options')
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze()
  expect(openSelectAccessibility.violations, 'open Select').toEqual([])
  const activeOption = statusListbox.getByRole('option', { name: 'Active' })
  const pendingOption = statusListbox.getByRole('option', { name: 'Pending' })
  const suspendedOption = statusListbox.getByRole('option', { name: 'Suspended' })
  const scheduledOption = statusListbox.getByRole('option', { name: 'Scheduled' })
  await expect.poll(activeStatusOption).toBe(await activeOption.getAttribute('id'))
  await page.keyboard.press('ArrowUp')
  await expect.poll(activeStatusOption).toBe(await activeOption.getAttribute('id'))
  await page.keyboard.press('PageDown')
  await expect.poll(activeStatusOption).toBe(await scheduledOption.getAttribute('id'))
  await page.keyboard.press('PageUp')
  await expect.poll(activeStatusOption).toBe(await activeOption.getAttribute('id'))
  await page.keyboard.press('ArrowDown')
  await expect.poll(activeStatusOption).toBe(await pendingOption.getAttribute('id'))
  await page.keyboard.press('ArrowDown')
  await expect.poll(activeStatusOption).toBe(await scheduledOption.getAttribute('id'))
  await expect(suspendedOption).toBeDisabled()
  await page.keyboard.press('ArrowUp')
  await expect.poll(activeStatusOption).toBe(await pendingOption.getAttribute('id'))
  await page.keyboard.press('Tab')
  await expect(statusSelect).toContainText('Pending')
  await expect(statusListbox).toBeHidden()
  await expect(statusSelect).not.toBeFocused()
  await expect(selectForm.locator('input[type="hidden"][name="status"]')).toHaveValue('pending')
  expect(await formEntries(selectForm)).toEqual([['status', 'pending']])
  await selectSubmit.focus()
  await selectSubmit.press('Enter')
  await expect(selectSurface.locator('#components-select-result')).toHaveAttribute('role', 'status')
  await expect(selectSurface.locator('#components-select-result')).toHaveText('Accepted status: Pending.')
  await expect(selectForm.getByRole('alert')).toHaveCount(0)
  await expect(statusSelect).toHaveAttribute('aria-invalid', 'false')
  await expect(statusSelect).toContainText('Pending')
  await expect(selectSubmit).toBeFocused()

  await statusSelect.press('Alt+ArrowDown')
  await expect(statusListbox).toBeVisible()
  await page.keyboard.press('End')
  await expect.poll(activeStatusOption).toBe(await scheduledOption.getAttribute('id'))
  await statusSelect.press('Alt+ArrowUp')
  await expect(statusSelect).toContainText('Scheduled')
  await expect(statusSelect).toBeFocused()
  await statusSelect.press('ArrowDown')
  await expect.poll(activeStatusOption).toBe(await scheduledOption.getAttribute('id'))
  await page.keyboard.press('Escape')
  await statusSelect.press('ArrowUp')
  await expect.poll(activeStatusOption).toBe(await activeOption.getAttribute('id'))
  await page.keyboard.press('Escape')
  await statusSelect.press('s')
  await expect.poll(activeStatusOption).toBe(await scheduledOption.getAttribute('id'))
  await page.keyboard.press('Escape')
  await statusSelect.press('Space')
  await expect(statusListbox).toBeVisible()
  await activeOption.click()
  await expect(statusSelect).toContainText('Active')
  await expect(selectForm.locator('input[type="hidden"][name="status"]')).toHaveValue('active')
  await expect(statusSelect).toBeFocused()

  const disabledStatus = selectSurface.getByRole('combobox', { name: 'Disabled status' })
  const pendingStatus = selectSurface.getByRole('combobox', { name: 'Updating status' })
  await expect(disabledStatus).toBeDisabled()
  await expect(disabledStatus).toHaveAttribute('aria-disabled', 'true')
  await expect(pendingStatus).toBeDisabled()
  await expect(pendingStatus).toHaveAttribute('aria-busy', 'true')
  await expect(pendingStatus.locator('[aria-hidden="true"]')).toBeVisible()
  const unavailableStatusValues = selectSurface.locator('input[type="hidden"][name="status"]:disabled')
  await expect(unavailableStatusValues).toHaveCount(2)
  expect(await clonedControlEntries(unavailableStatusValues)).toEqual([])
  await attachScreenshot('components-select-state-matrix-desktop-dark')

  const collectionSurface = await openPreview('/components/collection', 'Collection')
  const accountTypeFilter = collectionSurface.getByRole('combobox', { name: 'Filter by account type' })
  await expect(accountTypeFilter.locator('option')).toHaveText(['All types', 'Asset', 'Liability', 'Equity', 'Revenue', 'Expense'])
  await accountTypeFilter.selectOption('liability')
  await expect(accountTypeFilter).toHaveValue('liability')

  const checkboxSurface = await openPreview('/components/checkbox', 'Checkbox')
  const checkboxForm = checkboxSurface.locator('#components-checkbox-form-region form')
  const confirmReview = checkboxSurface.getByRole('checkbox', { name: 'Confirm archived-account review' })
  const checkboxSubmit = checkboxSurface.getByRole('button', { name: 'Validate confirmation' })
  await expect(confirmReview).toHaveAttribute('required', '')
  await expect(checkboxForm).toHaveAttribute('novalidate', '')
  expect(await confirmReview.evaluate(control => (control as HTMLInputElement).checkValidity())).toBe(false)
  expect(await formEntries(checkboxForm)).toEqual([])
  await checkboxSubmit.click()
  await expect(checkboxSurface.getByRole('alert')).toHaveText('Confirm the archived-account review.')
  await confirmReview.press('Space')
  await expect(confirmReview).toBeChecked()
  expect(await formEntries(checkboxForm)).toEqual([['confirmArchivedReview', 'true']])
  await checkboxSubmit.focus()
  await checkboxSubmit.press('Enter')
  await expect(checkboxSurface.getByRole('status')).toHaveText('Archived-account review confirmed.')
  await expect(confirmReview).toBeChecked()
  await expect(checkboxSurface.getByRole('alert')).toHaveCount(0)
  await expect(checkboxSubmit).toBeFocused()

  const includeArchived = checkboxSurface.getByRole('checkbox', { name: 'Include archived accounts' })
  await expect(includeArchived).toBeChecked()
  await includeArchived.press('Space')
  await expect(includeArchived).not.toBeChecked()
  await includeArchived.press('Space')
  await expect(includeArchived).toBeChecked()
  const pendingReview = checkboxSurface.getByRole('checkbox', { name: 'Saving archived-account review' })
  const disabledReview = checkboxSurface.getByRole('checkbox', { name: 'Archived review unavailable' })
  await expect(pendingReview).toBeDisabled()
  await expect(pendingReview).toHaveAttribute('aria-busy', 'true')
  await expect(disabledReview).toBeDisabled()
  expect(await clonedControlEntries(checkboxSurface.locator('input[name="confirmArchivedReview"]:disabled'))).toEqual([])
  await attachScreenshot('components-checkbox-state-matrix-desktop-dark')

  const switchSurface = await openPreview('/components/switch', 'Switch')
  const switchForm = switchSurface.locator('#components-switch-form-region form')
  const notifications = switchSurface.getByRole('switch', { name: 'Posting notifications' })
  const switchSubmit = switchSurface.getByRole('button', { name: 'Save notifications' })
  expect(await formEntries(switchForm)).toEqual([['postingNotifications', 'true']])
  await notifications.press('Space')
  await expect(notifications).toHaveAttribute('aria-checked', 'false')
  expect(await formEntries(switchForm)).toEqual([])
  await switchSubmit.focus()
  await switchSubmit.press('Enter')
  await expect(switchSurface.getByRole('status')).toHaveText('Posting notifications disabled.')
  await expect(notifications).toHaveAttribute('aria-checked', 'false')
  await expect(switchSubmit).toBeFocused()
  const pendingNotifications = switchSurface.getByRole('switch', { name: 'Saving notifications' })
  await expect(pendingNotifications).toBeDisabled()
  await expect(pendingNotifications).toHaveAttribute('aria-busy', 'true')
  expect(await clonedControlEntries(switchSurface.locator('input[name="postingNotifications"]:disabled'))).toEqual([])
  await expect(switchSurface.getByRole('alert')).toHaveText('Notification preferences could not be saved.')

  const toggleSurface = await openPreview('/components/toggle-button', 'Toggle button')
  const compactRows = toggleSurface.getByRole('button', { name: 'Compact rows', exact: true })
  await compactRows.click()
  await expect(compactRows).toHaveAttribute('aria-pressed', 'false')
  const pendingCompactRows = toggleSurface.getByRole('button', { name: 'Applying compact rows' })
  const disabledCompactRows = toggleSurface.getByRole('button', { name: 'Compact rows unavailable' })
  await expect(pendingCompactRows).toBeDisabled()
  await expect(pendingCompactRows).toHaveAttribute('aria-busy', 'true')
  await expect(pendingCompactRows.locator('[aria-hidden="true"]')).toBeVisible()
  await expect(disabledCompactRows).toBeDisabled()

  const radioSurface = await openPreview('/components/radio-group', 'Radio group')
  const radioForm = radioSurface.locator('#components-radio-form-region form')
  const postingModeGroup = radioSurface.getByRole('radiogroup', { name: 'Posting mode', exact: true })
  const radioSubmit = radioSurface.getByRole('button', { name: 'Validate posting mode' })
  const automaticPosting = postingModeGroup.getByRole('radio', { name: 'Automatic' })
  const manualPosting = postingModeGroup.getByRole('radio', { name: 'Manual review' })
  const scheduledPosting = postingModeGroup.getByRole('radio', { name: 'Scheduled' })
  await expect(postingModeGroup).toHaveAttribute('aria-required', 'true')
  await expect(radioForm).toHaveAttribute('novalidate', '')
  await expect(scheduledPosting).toBeDisabled()
  expect(await automaticPosting.evaluate(control => (control as HTMLInputElement).checkValidity())).toBe(false)
  expect(await formEntries(radioForm)).toEqual([])
  await radioSubmit.click()
  await expect(radioSurface.getByRole('alert')).toHaveText('Choose an available posting mode.')
  await automaticPosting.focus()
  await page.keyboard.press('ArrowRight')
  await expect(manualPosting).toBeChecked()
  expect(await formEntries(radioForm)).toEqual([['postingMode', 'manual']])
  await radioSubmit.focus()
  await radioSubmit.press('Enter')
  await expect(radioSurface.getByRole('status')).toHaveText('Accepted posting mode: Manual review.')
  await expect(manualPosting).toBeChecked()
  await expect(radioSurface.getByRole('alert')).toHaveCount(0)
  await expect(radioSubmit).toBeFocused()
  const pendingMode = radioSurface.getByRole('radiogroup', { name: 'Saving posting mode' })
  const disabledMode = radioSurface.getByRole('radiogroup', { name: 'Posting mode unavailable' })
  await expect(pendingMode).toHaveAttribute('aria-busy', 'true')
  await expect(pendingMode.getByRole('radio')).toHaveCount(3)
  await expect(disabledMode.getByRole('radio')).toHaveCount(3)
  await expect.poll(() => pendingMode.getByRole('radio').evaluateAll(radios => radios.every(radio => (radio as HTMLInputElement).disabled))).toBe(true)
  await expect.poll(() => disabledMode.getByRole('radio').evaluateAll(radios => radios.every(radio => (radio as HTMLInputElement).disabled))).toBe(true)
  expect(await clonedControlEntries(radioSurface.locator('input[name="postingMode"]:disabled'))).toEqual([])
  await attachScreenshot('components-radio-state-matrix-desktop-dark')

  expect(browserErrors).toEqual([])
})

test('DropdownMenu preserves groups, alignment, activation, sibling dismissal, morphs, and responsive behavior', crossBrowser, async ({ page }, testInfo) => {
  test.slow()
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )
  const browserErrors = captureBrowserErrors(page)
  const attachScreenshot = async (name: string) => {
    if (testInfo.project.name !== 'chromium') return
    await testInfo.attach(name, {
      body: await page.screenshot({ fullPage: true, animations: 'disabled' }),
      contentType: 'image/png',
    })
  }
  const openPreview = (path: string, heading: string) => openComponentGallery(page, path, heading)

  const menuSurface = await openPreview('/components/dropdown-menu', 'Dropdown menu')
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  const menuRegion = menuSurface.locator('#components-dropdown-menu-region')
  const actionsTrigger = menuRegion.getByRole('button', { name: 'Actions', exact: true })
  const actionsMenu = menuRegion.getByRole('menu', { name: 'Actions', exact: true })
  const moreActionsTrigger = menuRegion.getByRole('button', { name: 'More actions', exact: true })
  const moreActionsMenu = menuRegion.getByRole('menu', { name: 'More actions', exact: true })
  const menuActionCount = async () => Number((await menuRegion.getByText(/Completed menu actions:/).textContent())?.match(/\d+/)?.[0] ?? -1)

  await actionsTrigger.click()
  await expect(actionsMenu).toHaveAttribute('popover', 'auto')
  expect(await actionsMenu.evaluate(menu => menu.matches(':popover-open'))).toBe(true)
  const [startTriggerBox, startMenuBox] = await Promise.all([actionsTrigger.boundingBox(), actionsMenu.boundingBox()])
  expect(startTriggerBox).toBeTruthy()
  expect(startMenuBox).toBeTruthy()
  expect(Math.abs(startMenuBox!.x - startTriggerBox!.x)).toBeLessThanOrEqual(1)
  await expect(actionsMenu.getByRole('group', { name: 'Account' })).toBeVisible()
  await expect(actionsMenu.getByRole('group', { name: 'Reports' })).toBeVisible()
  await expect(actionsMenu.getByRole('menuitem', { name: 'Dropdown menu guidance' })).toHaveAttribute('href', '/components/dropdown-menu#components-dropdown-menu')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Export statement' })).toBeDisabled()
  await expect(actionsMenu.getByRole('menuitem', { name: 'Export statement' })).toHaveAttribute('aria-disabled', 'true')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Syncing ledger' })).toBeDisabled()
  await expect(actionsMenu.getByRole('menuitem', { name: 'Syncing ledger' })).toHaveAttribute('aria-busy', 'true')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Syncing ledger' }).locator('[aria-hidden="true"]')).toBeVisible()
  await expect(actionsMenu.getByRole('menuitem', { name: 'Record review' }).locator('kbd')).toHaveText('R')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Record review' }).locator('svg')).toBeVisible()
  await page.keyboard.press('Escape')

  await actionsTrigger.focus()
  await actionsTrigger.press('Space')
  await expect(actionsMenu).toBeVisible()
  await expect(actionsMenu.getByRole('menuitem', { name: 'Dropdown menu guidance' })).toBeFocused()
  const openMenuAccessibility = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze()
  expect(openMenuAccessibility.violations, 'open DropdownMenu').toEqual([])
  await page.keyboard.press('ArrowDown')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Record review' })).toBeFocused()
  const countBeforeSpace = await menuActionCount()
  await page.keyboard.press('Space')
  await expect.poll(menuActionCount).toBe(countBeforeSpace + 1)
  await expect(actionsMenu).toBeHidden()
  await expect(actionsTrigger).toBeFocused()

  await actionsTrigger.press('Enter')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Dropdown menu guidance' })).toBeFocused()
  await page.keyboard.press('c')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Create report' })).toBeFocused()
  await page.keyboard.press('c')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Close period' })).toBeFocused()
  const countBeforeEnter = await menuActionCount()
  await page.keyboard.press('Enter')
  await expect.poll(menuActionCount).toBe(countBeforeEnter + 1)
  await expect(actionsTrigger).toBeFocused()

  await actionsTrigger.press('ArrowDown')
  await page.keyboard.press('r')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Record review' })).toBeFocused()
  await page.keyboard.press('r')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Refresh actions' })).toBeFocused()
  await page.keyboard.press('ArrowDown')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Create report' })).toBeFocused()
  await page.keyboard.press('Home')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Dropdown menu guidance' })).toBeFocused()
  await page.keyboard.press('End')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Delete draft' })).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(actionsMenu).toBeHidden()
  await expect(actionsTrigger).toBeFocused()

  await actionsTrigger.click()
  const countBeforePointer = await menuActionCount()
  await actionsMenu.getByRole('menuitem', { name: 'Record review' }).hover()
  await expect(actionsMenu.getByRole('menuitem', { name: 'Record review' })).toBeFocused()
  await actionsMenu.getByRole('menuitem', { name: 'Record review' }).click()
  await expect.poll(menuActionCount).toBe(countBeforePointer + 1)
  await expect(actionsTrigger).toBeFocused()

  await actionsTrigger.click()
  await moreActionsTrigger.click()
  await expect(actionsMenu).toBeHidden()
  await expect(moreActionsMenu).toBeVisible()
  const [endTriggerBox, endMenuBox] = await Promise.all([moreActionsTrigger.boundingBox(), moreActionsMenu.boundingBox()])
  expect(endTriggerBox).toBeTruthy()
  expect(endMenuBox).toBeTruthy()
  expect(Math.abs((endMenuBox!.x + endMenuBox!.width) - (endTriggerBox!.x + endTriggerBox!.width))).toBeLessThanOrEqual(1)
  await expect(moreActionsMenu).toBeFocused()
  await page.keyboard.press('ArrowDown')
  await expect(moreActionsMenu.getByRole('menuitem', { name: 'Read menu guidance' })).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(moreActionsTrigger).toBeFocused()

  await actionsTrigger.click()
  await page.keyboard.press('Tab')
  await expect(actionsMenu).toBeHidden()
  await actionsTrigger.click()
  await page.getByRole('heading', { level: 1, name: 'Dropdown menu' }).click()
  await expect(actionsMenu).toBeHidden()

  await actionsTrigger.click()
  await page.keyboard.press('r')
  await page.keyboard.press('r')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Refresh actions' })).toBeFocused()
  const countBeforePatch = await menuActionCount()
  await page.keyboard.press('Enter')
  await expect(menuRegion.getByRole('status')).toHaveText('Actions refreshed from the server.')
  await expect.poll(menuActionCount).toBe(countBeforePatch)
  await expect(actionsTrigger).toBeFocused()
  await actionsTrigger.press('ArrowDown')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Dropdown menu guidance' })).toBeFocused()
  await page.keyboard.press('End')
  await expect(actionsMenu.getByRole('menuitem', { name: 'Delete draft' })).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(actionsTrigger).toBeFocused()
  await moreActionsTrigger.click()
  await expect(moreActionsMenu).toBeFocused()
  await moreActionsMenu.getByRole('menuitem', { name: 'Read menu guidance' }).hover()
  await expect(moreActionsMenu.getByRole('menuitem', { name: 'Read menu guidance' })).toBeFocused()
  await page.mouse.move(0, 0)
  await expect(moreActionsMenu).toBeFocused()
  await page.keyboard.press('Escape')

  await actionsTrigger.click()
  await attachScreenshot('components-dropdown-menu-desktop-dark-open')
  await page.keyboard.press('Escape')
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Light' }).click()
  await actionsTrigger.click()
  await attachScreenshot('components-dropdown-menu-desktop-light-open')
  await page.keyboard.press('Escape')
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  expect(browserErrors).toEqual([])
})

test.describe('Components route accessibility', () => {
  for (const group of componentAccessibilityRouteGroups) {
    test(`${group.name} remain accessible`, crossBrowser, async ({ page }) => {
      await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
        route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
      )
      const browserErrors = captureBrowserErrors(page)

      for (const path of group.paths) {
        await gotoAfterDocsAssetSettlement(page, path, 'domcontentloaded')
        const results = await new AxeBuilder({ page })
          .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
          .analyze()
        expect(results.violations, path).toEqual([])
      }

      expect(browserErrors).toEqual([])
    })
  }
})

test('Components layouts, catalog, and responsive previews remain coherent', crossBrowser, async ({ page }, testInfo) => {
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )
  const browserErrors = captureBrowserErrors(page)
  const attachScreenshot = async (name: string) => {
    if (testInfo.project.name !== 'chromium') return
    await testInfo.attach(name, {
      body: await page.screenshot({ fullPage: true, animations: 'disabled' }),
      contentType: 'image/png',
    })
  }
  const openPreview = (path: string, heading: string) => openComponentGallery(page, path, heading)

  const buttonSurface = await openPreview('/components/button', 'Button')
  const comfortableControl = buttonSurface.getByRole('button', { name: 'Create account' }).nth(1)
  const comfortableDensity = await comfortableControl.evaluate(element => ({
    height: getComputedStyle(element).height,
    paddingTop: getComputedStyle(element).paddingTop,
  }))
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()

  const dialogSurface = await openPreview('/components/dialog', 'Dialog')
  const dialogTrigger = dialogSurface.getByRole('button', { name: 'Review account' })
  const dialog = dialogSurface.getByRole('dialog', { name: 'Review account' })
  await dialogTrigger.click()
  await expect(dialog).toBeVisible()
  await expect(dialog.getByRole('button', { name: 'Close' })).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(dialog).toBeHidden()
  await expect(dialogTrigger).toBeFocused()

  const appShellSurface = await openPreview('/components/page-examples/account-management', 'Account management')
  const compactControl = appShellSurface.getByRole('link', { name: 'Create', exact: true })
  const compactDensity = await compactControl.evaluate(element => ({
    height: getComputedStyle(element).height,
    paddingTop: getComputedStyle(element).paddingTop,
  }))
  // Application action regions opt into the compact size without changing the default form size.
  expect(parseFloat(comfortableDensity.paddingTop)).toBe(8)
  expect(parseFloat(comfortableDensity.height)).toBe(40)
  expect(parseFloat(compactDensity.paddingTop)).toBe(6)
  expect(parseFloat(compactDensity.height)).toBe(32)
  await expect(appShellSurface.getByRole('navigation', { name: 'Ledger primary navigation' }).locator('[aria-current="page"]')).toHaveCount(1)
  await expect(appShellSurface.getByRole('navigation', { name: 'Breadcrumb' }).locator('[aria-current="page"]')).toHaveCount(1)

  const pageSurface = await openPreview('/components/page', 'Page')
  const transactionTabs = pageSurface.getByRole('tablist', { name: 'Transaction views' })
  const upcomingTab = transactionTabs.getByRole('tab', { name: 'Upcoming' })
  const completedTab = transactionTabs.getByRole('tab', { name: 'Completed' })
  await expect(upcomingTab).toHaveAttribute('aria-selected', 'true')
  await expect(pageSurface.getByRole('tabpanel', { name: 'Upcoming' })).toBeVisible()
  await expect(pageSurface.getByRole('tabpanel', { name: 'Completed' })).toBeHidden()
  await completedTab.click()
  await expect(completedTab).toHaveAttribute('aria-selected', 'true')
  await expect(pageSurface.getByRole('tabpanel', { name: 'Completed' })).toBeVisible()
  await expect(pageSurface.getByRole('tabpanel', { name: 'Upcoming' })).toBeHidden()
  await completedTab.press('ArrowLeft')
  await expect(upcomingTab).toBeFocused()
  await expect(upcomingTab).toHaveAttribute('aria-selected', 'true')

  await gotoAfterDocsAssetSettlement(page, '/components', 'domcontentloaded')
  const catalog = page.locator('.docs-catalog-grid')
  await expect(catalog.getByRole('link')).toHaveCount(3)
  await attachScreenshot('components-catalog-desktop-dark')
  await catalog.getByRole('link', { name: /^Primitives / }).click()
  await expect(page).toHaveURL('/components/primitives')
  for (const [label, slug] of [['Button', 'button'], ['Icon button', 'icon-button'], ['Notification', 'notification'], ['Empty state', 'empty-state'], ['Description list', 'description-list'], ['Calendar', 'calendar'], ['Breadcrumbs', 'breadcrumbs'], ['Side nav', 'side-nav'], ['Pagination', 'pagination'], ['Confirmation dialog', 'confirmation-dialog'], ['Drawer', 'drawer'], ['Floating panel', 'floating-panel'], ['Section', 'section']]) {
    await expect(catalog.getByRole('link', { name: new RegExp(`^${label} `) })).toHaveAttribute('href', `/components/${slug}`)
  }
  await expect(page.locator('a[href="/components/chart"], a[href="/components/layouts"]')).toHaveCount(0)
  await gotoAfterDocsAssetSettlement(page, '/components/application', 'domcontentloaded')
  for (const [label, slug] of [['Page top bar', 'page-top-bar'], ['Page header', 'page-header'], ['Page', 'page'], ['App shell', 'app-shell'], ['Bottom navigation', 'bottom-navigation'], ['Bulk actions', 'bulk-actions'], ['Upload', 'upload'], ['Steps', 'steps'], ['First steps', 'first-steps'], ['Media library', 'media-library']]) {
    await expect(catalog.locator(`a[href="/components/${slug}"]`)).toHaveCount(1)
    await expect(catalog.locator(`a[href="/components/${slug}"]`)).toContainText(label)
  }

  await page.setViewportSize({ width: 390, height: 844 })
  await openPreview('/components/button', 'Button')
  await attachScreenshot('components-button-mobile-dark')
  await openPreview('/components/loading-indicator', 'Loading indicator')
  await attachScreenshot('components-loading-mobile-dark')
  const mobileTableSurface = await openPreview('/components/collection', 'Collection')
  const mobileTableRegion = mobileTableSurface.locator('[data-fve-table]').getByRole('region', { name: 'Accounts', exact: true })
  await expect.poll(() => mobileTableRegion.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true)
  await expect(mobileTableRegion.locator('tbody tr').first()).toHaveCSS('display', 'grid')
  const mobileAssetsActions = mobileTableRegion.getByRole('button', { name: 'More actions for Assets' })
  await expect(mobileAssetsActions).toBeVisible()
  const [mobileRegionBox, mobileActionsBox] = await Promise.all([mobileTableRegion.boundingBox(), mobileAssetsActions.boundingBox()])
  expect(mobileRegionBox).toBeTruthy()
  expect(mobileActionsBox).toBeTruthy()
  expect(mobileActionsBox!.x + mobileActionsBox!.width).toBeLessThanOrEqual(mobileRegionBox!.x + mobileRegionBox!.width)
  await attachScreenshot('components-collection-table-mobile-dark')
  const mobileMenuSurface = await openPreview('/components/dropdown-menu', 'Dropdown menu')
  await mobileMenuSurface.getByRole('button', { name: 'Actions', exact: true }).click()
  const mobileActionsMenu = mobileMenuSurface.getByRole('menu', { name: 'Actions', exact: true })
  await expect(mobileActionsMenu).toBeVisible()
  const mobileMenuBox = await mobileActionsMenu.boundingBox()
  expect(mobileMenuBox).toBeTruthy()
  expect(mobileMenuBox!.x).toBeGreaterThanOrEqual(0)
  expect(mobileMenuBox!.x + mobileMenuBox!.width).toBeLessThanOrEqual(390)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await attachScreenshot('components-dropdown-menu-mobile-dark-open')
  await page.keyboard.press('Escape')

  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Light' }).click()
  await openPreview('/components/icon-button', 'Icon button')
  await attachScreenshot('components-icon-button-mobile-light')
  await openPreview('/components/empty-state', 'Empty state')
  await attachScreenshot('components-empty-state-mobile-light')
  await openPreview('/components/description-list', 'Description list')
  await attachScreenshot('components-description-list-mobile-light')
  await openPreview('/components/pagination', 'Pagination')
  await attachScreenshot('components-pagination-mobile-light')
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  const mobileSelectSurface = await openPreview('/components/select', 'Select')
  await mobileSelectSurface.getByRole('combobox', { name: 'Status', exact: true }).click()
  await attachScreenshot('components-select-mobile-dark')
  await page.keyboard.press('Escape')
  await openPreview('/components/checkbox', 'Checkbox')
  await attachScreenshot('components-checkbox-mobile-dark')
  await openPreview('/components/switch', 'Switch')
  await openPreview('/components/toggle-button', 'Toggle button')
  await openPreview('/components/tabs', 'Tabs')
  await openPreview('/components/radio-group', 'Radio group')
  await attachScreenshot('components-radio-group-mobile-dark')

  await gotoAfterDocsAssetSettlement(page, '/components')
  await expect(page.getByRole('heading', { level: 1, name: 'Components' })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await page.getByRole('button', { name: 'Open navigation' }).click()
  await expect(page.locator('#nav-fsharp-viewengine-components')).toBeVisible()
  await attachScreenshot('components-catalog-mobile-dark')
  expect(browserErrors).toEqual([])
})

test('Composition fixture destinations use Datastar navigation instead of native document reloads', crossBrowser, async ({ page }) => {
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )
  const browserErrors = captureBrowserErrors(page)
  const destinationRequests: { path: string; resourceType: string }[] = []
  page.on('request', request => {
    const url = new URL(request.url())
    if (url.pathname.startsWith('/components/page-examples/account-management')) {
      destinationRequests.push({ path: url.pathname, resourceType: request.resourceType() })
    }
  })

  for (const fixture of [
    { path: '/components/breadcrumbs', link: 'Accounts', destination: 'ledger-accounts' },
    { path: '/components/side-nav', link: 'Reports', destination: 'ledger-reports' },
    { path: '/components/page-header', link: 'View reports', destination: 'ledger-reports' },
    { path: '/components/page', link: 'Home', destination: 'treasury-home' },
  ]) {
    await gotoAfterDocsAssetSettlement(page, fixture.path, 'domcontentloaded')
    const example = page.locator('[data-docs-example="true"]').first()
    await example.locator(':scope > .spec-example-toolbar').getByRole('tab', { name: 'Preview' }).click()
    await page.evaluate(() => { (window as any).__fixtureNavigationSentinel = 'preserved' })
    destinationRequests.length = 0

    await example.getByRole('link', { name: fixture.link, exact: true }).click()
    await expect(page).toHaveURL(`/components/page-examples/account-management?destination=${fixture.destination}`)
    await expect(page.getByRole('heading', { level: 1, name: 'Account management' })).toBeVisible()
    expect(await page.evaluate(() => (window as any).__fixtureNavigationSentinel)).toBe('preserved')
    expect(destinationRequests).toEqual([{ path: '/components/page-examples/account-management', resourceType: 'fetch' }])
  }

  expect(browserErrors).toEqual([])
})

test('AppShell keeps typed page ownership, responsive navigation, focus, deep links, and morph state coherent', crossBrowser, async ({ page }, testInfo) => {
  test.slow()
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )
  const browserErrors = captureBrowserErrors(page)
  const shellRequests: { path: string; resourceType: string }[] = []
  page.on('request', request => {
    const url = new URL(request.url())
    if (url.pathname.startsWith('/components/page-examples/account-management')) {
      shellRequests.push({ path: url.pathname, resourceType: request.resourceType() })
    }
  })
  const expectFixtureMorph = async () => {
    await expect.poll(() => shellRequests.length).toBe(1)
    expect(shellRequests.shift()).toEqual({ path: '/components/page-examples/account-management/fixture', resourceType: 'fetch' })
    await expect(page.locator('#page-content')).toHaveAttribute('data-fixture-navigation-marker', 'preserved')
  }
  const prepareFixtureMorph = async () => {
    shellRequests.length = 0
    await page.locator('#page-content').evaluate(element => element.setAttribute('data-fixture-navigation-marker', 'preserved'))
  }
  const attachScreenshot = async (name: string) => {
    if (testInfo.project.name !== 'chromium') return
    await testInfo.attach(name, {
      body: await page.screenshot({ fullPage: true, animations: 'disabled' }),
      contentType: 'image/png',
    })
  }
  const openShellPreview = async (path: string, shellId: string) => {
    await gotoAfterDocsAssetSettlement(page, path, 'domcontentloaded')
    const example = page.locator('#components-account-management [data-docs-example="true"]')
    await expect(example).toHaveCount(1)
    await example.getByRole('tab', { name: 'Preview' }).click()
    const shell = page.locator(`#${shellId}`)
    await expect(shell).toBeVisible()
    return shell
  }

  await page.setViewportSize({ width: 1440, height: 800 })
  let shell = await openShellPreview('/components/page-examples/account-management?destination=ledger-account-2048', 'ledger-app-shell')
  await expect(shell.getByRole('heading', { level: 1, name: 'Operating checking', exact: true })).toHaveCount(1)
  await expect(shell.getByRole('navigation', { name: 'Breadcrumb' }).getByText('Operating checking', { exact: true })).toHaveAttribute('aria-current', 'page')
  const ledgerNavigation = shell.getByRole('navigation', { name: 'Ledger primary navigation' })
  await expect(ledgerNavigation).toHaveCount(1)
  await expect(ledgerNavigation.getByRole('link', { name: 'Accounts', exact: true })).toHaveAttribute('aria-current', 'page')
  await expect(shell.getByRole('button', { name: 'Open navigation' })).toBeHidden()
  await expect(shell.locator('[data-fve-action-cluster="true"] > span').getByRole('link', { name: 'View reports' })).toBeVisible()
  const previewRegion = shell.getByRole('region', { name: 'Ledger application preview', exact: true })
  await expect(previewRegion).toHaveCount(1)
  await expect(shell.locator('main')).toHaveCount(0)
  const sideNavigationHeaderBounds = await shell.locator('#ledger-side-navigation > div').first().boundingBox()
  const pageHeaderBounds = await previewRegion.locator('header').first().boundingBox()
  expect(sideNavigationHeaderBounds).toBeTruthy()
  expect(pageHeaderBounds).toBeTruthy()
  expect(sideNavigationHeaderBounds!.y + sideNavigationHeaderBounds!.height).toBe(pageHeaderBounds!.y + pageHeaderBounds!.height)
  expect(await shell.locator('[id]').evaluateAll(elements => new Set(elements.map(element => element.id)).size === elements.length)).toBe(true)
  await attachScreenshot('components-app-shell-ledger-desktop-light')

  await prepareFixtureMorph()
  await shell.getByRole('navigation', { name: 'Breadcrumb' }).getByRole('link', { name: 'Accounts', exact: true }).click()
  await expect(page).toHaveURL('/components/page-examples/account-management?destination=ledger-accounts')
  shell = page.locator('#ledger-app-shell')
  await expect(shell.getByRole('heading', { level: 1, name: 'Accounts', exact: true })).toHaveCount(1)
  await expectFixtureMorph()

  await prepareFixtureMorph()
  await shell.getByRole('navigation', { name: 'Ledger primary navigation' }).getByRole('link', { name: 'Reports', exact: true }).click()
  await expect(page).toHaveURL('/components/page-examples/account-management?destination=ledger-reports')
  shell = page.locator('#ledger-app-shell')
  await expect(shell).toBeVisible()
  await expect(shell.getByRole('heading', { level: 1, name: 'Reports', exact: true })).toHaveCount(1)
  await expect(shell.getByRole('navigation', { name: 'Ledger primary navigation' }).getByRole('link', { name: 'Reports', exact: true })).toHaveAttribute('aria-current', 'page')
  await expect(shell.getByRole('navigation', { name: 'Breadcrumb' }).getByText('Reports', { exact: true })).toHaveAttribute('aria-current', 'page')
  await expectFixtureMorph()

  await prepareFixtureMorph()
  await shell.getByRole('link', { name: 'View accounts', exact: true }).click()
  await expect(page).toHaveURL('/components/page-examples/account-management?destination=ledger-accounts')
  shell = page.locator('#ledger-app-shell')
  await expect(shell.getByRole('heading', { level: 1, name: 'Accounts', exact: true })).toHaveCount(1)
  await expectFixtureMorph()
  await prepareFixtureMorph()
  await shell.getByRole('link', { name: 'Assets', exact: true }).click()
  await expect(page).toHaveURL('/components/page-examples/account-management?destination=ledger-account-101')
  shell = page.locator('#ledger-app-shell')
  await expectFixtureMorph()
  await shell.getByRole('button', { name: 'Refresh balances' }).click()
  await expect(shell.getByRole('status').filter({ hasText: 'Balance refreshes:' })).toHaveText('Balance refreshes: 1')

  await prepareFixtureMorph()
  await shell.getByRole('link', { name: /Andy Meier/ }).click()
  await expect(page).toHaveURL('/components/page-examples/account-management?destination=ledger-settings')
  shell = page.locator('#ledger-app-shell')
  await expect(shell.getByRole('heading', { level: 1, name: 'Settings', exact: true })).toHaveCount(1)
  await expectFixtureMorph()

  shell = await openShellPreview('/components/page-examples/account-management?destination=treasury-transactions', 'treasury-app-shell')
  await expect(shell.getByRole('navigation', { name: 'Treasury primary navigation' })).toHaveCount(1)
  await expect(shell.getByRole('navigation', { name: 'Treasury primary navigation' }).getByRole('link', { name: 'Transactions', exact: true })).toHaveAttribute('aria-current', 'page')
  await expect(shell.getByRole('tablist', { name: 'Transaction views' })).toBeVisible()
  await expect(shell.locator('[data-fve-page-scroll="true"]')).toBeVisible()
  await expect(shell.getByRole('heading', { level: 2, name: 'Manage' })).toHaveCount(0)
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  await attachScreenshot('components-app-shell-treasury-desktop-dark')

  await prepareFixtureMorph()
  await shell.getByRole('navigation', { name: 'Breadcrumb' }).getByRole('link', { name: 'July 2026', exact: true }).click()
  await expect(page).toHaveURL('/components/page-examples/account-management?destination=treasury-period-july')
  shell = page.locator('#treasury-app-shell')
  await expect(shell.getByRole('heading', { level: 1, name: 'Overview', exact: true })).toHaveCount(1)
  await expectFixtureMorph()

  await prepareFixtureMorph()
  await shell.getByRole('navigation', { name: 'Treasury primary navigation' }).getByRole('link', { name: 'Payees', exact: true }).click()
  await expect(page).toHaveURL('/components/page-examples/account-management?destination=treasury-payees')
  shell = page.locator('#treasury-app-shell')
  await expect(shell.getByRole('heading', { level: 1, name: 'Payees', exact: true })).toHaveCount(1)
  await expectFixtureMorph()

  await expect(shell.locator('#treasury-side-navigation').getByText('Workspace', { exact: true })).toBeVisible()
  await expect(shell.locator('#treasury-side-navigation').getByText('Meier Made', { exact: true })).toBeVisible()
  await expect(shell.locator('#treasury-side-navigation').getByText('Andy Meier', { exact: true })).toBeVisible()
  await expect(shell.locator('#treasury-side-navigation').getByText('Cash management', { exact: true })).toHaveCount(0)
  await prepareFixtureMorph()
  await shell.getByRole('navigation', { name: 'Treasury primary navigation' }).getByRole('link', { name: 'Overview', exact: true }).click()
  await expect(page).toHaveURL('/components/page-examples/account-management?destination=treasury-home')
  shell = page.locator('#treasury-app-shell')
  await expect(shell.getByRole('heading', { level: 1, name: 'Overview', exact: true })).toHaveCount(1)
  await expectFixtureMorph()

  await page.setViewportSize({ width: 390, height: 844 })
  shell = await openShellPreview('/components/page-examples/account-management?destination=ledger-account-2048', 'ledger-app-shell')
  const breadcrumbOverflow = shell.getByRole('button', { name: 'Show hidden breadcrumbs' })
  await expect(breadcrumbOverflow).toBeVisible()
  await breadcrumbOverflow.click()
  const breadcrumbMenu = shell.getByRole('menu', { name: 'Show hidden breadcrumbs' })
  await expect(breadcrumbMenu).toBeVisible()
  await expect(breadcrumbMenu).toBeFocused()
  await page.keyboard.press('ArrowDown')
  await expect(breadcrumbMenu.getByRole('menuitem', { name: 'Home' })).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(breadcrumbMenu).toBeHidden()
  await expect(breadcrumbOverflow).toBeFocused()
  const openNavigation = shell.getByRole('button', { name: 'Open navigation' })
  await expect(openNavigation).toBeVisible()
  await openNavigation.click()
  const mobileNavigation = shell.locator('#ledger-side-navigation')
  await expect(mobileNavigation).toBeVisible()
  await expect(mobileNavigation).toHaveAttribute('role', 'dialog')
  await expect(mobileNavigation).toHaveAttribute('aria-label', 'Ledger primary navigation')
  await expect(mobileNavigation).toHaveAttribute('aria-modal', 'true')
  await expect(mobileNavigation.getByRole('link', { name: 'Accounts', exact: true })).toBeFocused()
  await expect(shell.locator('[data-fve-app-shell-content="true"]')).toHaveAttribute('inert', /^(|true)$/)
  const openAccessibility = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze()
  expect(openAccessibility.violations).toEqual([])

  const firstNavigationLink = mobileNavigation.getByRole('link', { name: 'Ledger', exact: true })
  await firstNavigationLink.focus()
  await page.keyboard.press('Shift+Tab')
  await expect(mobileNavigation.getByRole('link', { name: /Andy Meier/ })).toBeFocused()
  await page.keyboard.press('Tab')
  await expect(firstNavigationLink).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(mobileNavigation).toBeHidden()
  await expect(openNavigation).toBeFocused()

  await openNavigation.click()
  const backdrop = shell.locator('[data-fve-app-shell-backdrop="true"]')
  const backdropBox = await backdrop.boundingBox()
  expect(backdropBox).toBeTruthy()
  await backdrop.click({ position: { x: backdropBox!.width - 8, y: 120 } })
  await expect(mobileNavigation).toBeHidden()
  await expect(openNavigation).toBeFocused()

  await page.locator('html').evaluate(element => { element.style.fontSize = '200%' })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await expect(openNavigation).toBeVisible()
  const resizedProductName = shell.locator('[data-fve-app-shell-content="true"]').getByText('Ledger', { exact: true })
  await expect(resizedProductName).toBeVisible()
  expect(await resizedProductName.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true)
  const resizedCurrentBreadcrumb = shell.getByRole('navigation', { name: 'Breadcrumb' }).getByText('Operating checking', { exact: true })
  await expect(resizedCurrentBreadcrumb).toBeVisible()
  expect(await resizedCurrentBreadcrumb.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true)
  const resizedActionCluster = shell.locator('[data-fve-action-cluster="true"]')
  const resizedAction = resizedActionCluster.getByRole('button', { name: 'Refresh balances' })
  await expect(resizedAction).toBeVisible()
  await expect(resizedActionCluster.locator(':scope > span').getByRole('link', { name: 'View reports' })).toBeHidden()
  const resizedOverflow = resizedActionCluster.getByRole('button', { name: 'More actions' })
  await resizedOverflow.click()
  const resizedOverflowMenu = resizedActionCluster.getByRole('menu', { name: 'More actions' })
  await expect(resizedOverflowMenu.getByRole('menuitem', { name: 'View reports' })).toBeVisible()
  await expect(resizedOverflowMenu.getByRole('menuitem', { name: 'Settings' })).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(resizedOverflow).toBeFocused()
  const resizedBounds = await resizedAction.boundingBox()
  const resizedShellBounds = await shell.boundingBox()
  expect(resizedBounds).toBeTruthy()
  expect(resizedShellBounds).toBeTruthy()
  expect(resizedBounds!.x).toBeGreaterThanOrEqual(resizedShellBounds!.x)
  expect(resizedBounds!.x + resizedBounds!.width).toBeLessThanOrEqual(resizedShellBounds!.x + resizedShellBounds!.width)
  await attachScreenshot('components-app-shell-ledger-mobile-dark-200-percent')

  await page.locator('html').evaluate(element => { element.style.fontSize = '100%' })
  await openNavigation.click()
  await page.setViewportSize({ width: 1280, height: 800 })
  await expect(mobileNavigation).toBeVisible()
  await expect(mobileNavigation).not.toHaveAttribute('role', 'dialog')
  expect(await shell.locator('[data-fve-app-shell-content="true"]').evaluate(element => element.hasAttribute('inert'))).toBe(false)
  await page.setViewportSize({ width: 390, height: 844 })
  await expect(mobileNavigation).toBeHidden()

  await breadcrumbOverflow.click()
  await expect(breadcrumbMenu).toBeVisible()
  await prepareFixtureMorph()
  await breadcrumbMenu.getByRole('menuitem', { name: 'Accounts', exact: true }).click()
  await expect(page).toHaveURL('/components/page-examples/account-management?destination=ledger-accounts')
  shell = page.locator('#ledger-app-shell')
  await expect(shell.getByRole('heading', { level: 1, name: 'Accounts', exact: true })).toHaveCount(1)
  await expectFixtureMorph()
  expect(browserErrors).toEqual([])
})

test('Components examples preserve documentation framing without clipping anchored popups', crossBrowser, async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/components/drawer', { waitUntil: 'domcontentloaded' })
  await page.waitForFunction(() => (window as any).fsharpDocsCode?.loading)
  await page.evaluate(() => (window as any).fsharpDocsCode.loading)

  for (const example of await page.locator('[data-docs-example="true"]').all()) {
    await expect(example).toHaveCSS('overflow', 'visible')
    for (const name of ['Code', 'Preview']) {
      const tab = example.getByRole('tab', { name, exact: true })
      const panelId = await tab.getAttribute('aria-controls')
      expect(panelId).toBeTruthy()
      await tab.click()
      const panel = page.locator(`#${panelId}`)
      await expect(panel).toBeVisible()
      await expect(panel).toHaveCSS('border-bottom-left-radius', '12px')
      await expect(panel).toHaveCSS('border-bottom-right-radius', '12px')
    }
  }

  await page.goto('/components/dropdown-menu', { waitUntil: 'domcontentloaded' })
  await page.waitForFunction(() => (window as any).fsharpDocsCode?.loading)
  await page.evaluate(() => (window as any).fsharpDocsCode.loading)
  const menuExample = page.locator('[data-docs-example="true"]')
  await menuExample.getByRole('tab', { name: 'Preview' }).click()
  await expect(menuExample).toHaveCSS('overflow', 'visible')
  await menuExample.getByRole('button', { name: 'Actions', exact: true }).click()
  await expect(menuExample.getByRole('menu', { name: 'Actions', exact: true })).toBeVisible()

  await page.goto('/components/collection', { waitUntil: 'domcontentloaded' })
  await page.waitForFunction(() => (window as any).fsharpDocsCode?.loading)
  await page.evaluate(() => (window as any).fsharpDocsCode.loading)
  const tableExample = page.locator('[data-docs-example="true"]')
  await tableExample.getByRole('tab', { name: 'Preview' }).click()
  const rowActionTrigger = tableExample.getByRole('button', { name: 'More actions for Assets' })
  const rowActionMenu = tableExample.locator('#account-101-actions-menu')
  await rowActionTrigger.click()
  await expect(rowActionMenu).toHaveAttribute('popover', 'auto')
  expect(await rowActionMenu.evaluate(menu => menu.matches(':popover-open'))).toBe(true)
  expect(await rowActionMenu.evaluate(menu => getComputedStyle(menu).getPropertyValue('position-area'))).toBe('none')
  expect(await rowActionMenu.evaluate(menu => {
    const bounds = menu.getBoundingClientRect()
    const paintedElement = document.elementFromPoint(bounds.right - 8, bounds.top + bounds.height / 2)
    return paintedElement === menu || (paintedElement !== null && menu.contains(paintedElement))
  })).toBe(true)

  const initialOffset = await rowActionMenu.evaluate(menu => {
    const trigger = document.getElementById('account-101-actions-trigger')!.getBoundingClientRect()
    const bounds = menu.getBoundingClientRect()
    return { inlineEnd: trigger.right - bounds.right, blockStart: bounds.top - trigger.bottom }
  })
  await page.locator('#main-content').evaluate(main => main.scrollBy(0, 40))
  await expect.poll(() => rowActionMenu.evaluate(menu => {
    const trigger = document.getElementById('account-101-actions-trigger')!.getBoundingClientRect()
    const bounds = menu.getBoundingClientRect()
    return { inlineEnd: trigger.right - bounds.right, blockStart: bounds.top - trigger.bottom }
  })).toEqual(initialOffset)
  await tableExample.locator('[data-fve-table]').getByRole('region', { name: 'Accounts', exact: true }).evaluate(region => region.scrollBy(80, 0))
  await expect.poll(() => rowActionMenu.evaluate(menu => {
    const trigger = document.getElementById('account-101-actions-trigger')!.getBoundingClientRect()
    const bounds = menu.getBoundingClientRect()
    return { inlineEnd: trigger.right - bounds.right, blockStart: bounds.top - trigger.bottom }
  })).toEqual(initialOffset)

  await page.keyboard.press('Escape')
  expect(await rowActionMenu.evaluate(menu => menu.style.getPropertyValue('position-area'))).toBe('block-end span-inline-start')
  await page.setViewportSize({ width: 390, height: 844 })
  await page.locator('html').evaluate(element => { element.style.fontSize = '200%' })
  await rowActionTrigger.scrollIntoViewIfNeeded()
  await rowActionTrigger.click()
  await expect(rowActionMenu).toBeVisible()
  const resizedMenuBounds = await rowActionMenu.boundingBox()
  expect(resizedMenuBounds).toBeTruthy()
  expect(resizedMenuBounds!.x).toBeGreaterThanOrEqual(0)
  expect(resizedMenuBounds!.x + resizedMenuBounds!.width).toBeLessThanOrEqual(390)
  expect(resizedMenuBounds!.y).toBeGreaterThanOrEqual(0)
  expect(resizedMenuBounds!.y + resizedMenuBounds!.height).toBeLessThanOrEqual(844)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  expect(browserErrors).toEqual([])
})

test.skip('retired editable Combobox gallery workflow', crossBrowser, async ({ page }, testInfo) => {
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )
  const browserErrors = captureBrowserErrors(page)
  const openPreview = () => openComponentGallery(page, '/components/select', 'Select')
  const attachScreenshot = async (name: string) => {
    if (testInfo.project.name !== 'chromium') return
    await testInfo.attach(name, {
      body: await page.screenshot({ fullPage: true, animations: 'disabled' }),
      contentType: 'image/png',
    })
  }
  const clonedControlEntries = async (controls: Locator) =>
    controls.evaluateAll(elements => {
      const form = document.createElement('form')
      for (const element of elements) form.appendChild(element.cloneNode(true))
      return [...new FormData(form).entries()].map(([name, value]) => [name, String(value)])
    })

  const comboboxSurface = await openPreview()
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()

  const staticAccount = comboboxSurface.getByRole('combobox', { name: 'Static account' })
  const staticListbox = comboboxSurface.getByRole('listbox', { name: 'Static account' })
  const staticValue = comboboxSurface.locator('input[type="hidden"][name="staticAccount"]')
  await staticAccount.fill('tax')
  await expect(staticListbox.getByRole('option', { name: 'Tax reserve' })).toBeVisible()
  await expect(staticListbox.getByRole('option', { name: 'Payroll clearing' })).toBeHidden()
  await expect(staticAccount).toBeFocused()
  await page.keyboard.press('Enter')
  await expect(staticAccount).toHaveValue('Tax reserve')
  await expect(staticValue).toHaveValue('102')
  await comboboxSurface.getByRole('button', { name: 'Clear Static account' }).click()
  await expect(staticAccount).toBeFocused()
  await expect(staticAccount).toHaveValue('')
  await expect(staticValue).toHaveValue('')

  const parentAccount = comboboxSurface.getByRole('combobox', { name: 'Parent account' })
  const accountPopup = comboboxSurface.locator('#fve-select-account-popup')
  const accountListbox = comboboxSurface.getByRole('listbox', { name: 'Parent account' })
  const accountValue = comboboxSurface.locator('input[type="hidden"][name="account"]')
  const activeAccountOption = async () => parentAccount.getAttribute('aria-activedescendant')
  const remoteQuery = (url: string) => {
    const signals = new URL(url).searchParams.get('datastar')
    return signals ? String(JSON.parse(signals).account_query ?? '') : null
  }

  await parentAccount.click()
  await expect(parentAccount).toBeFocused()
  await expect(accountPopup).toHaveAttribute('popover', 'auto')
  expect(await accountPopup.evaluate(popup => popup.matches(':popover-open'))).toBe(true)
  const comboboxScrollY = await page.evaluate(() => window.scrollY)
  const comboboxPopupOffset = await accountPopup.evaluate(popup => {
    const input = document.getElementById('fve-select-account')!.getBoundingClientRect()
    const bounds = popup.getBoundingClientRect()
    return { inlineStart: bounds.left - input.left, blockStart: bounds.top - input.bottom }
  })
  await page.evaluate(() => window.scrollBy(0, 40))
  await expect.poll(() => accountPopup.evaluate(popup => {
    const input = document.getElementById('fve-select-account')!.getBoundingClientRect()
    const bounds = popup.getBoundingClientRect()
    return { inlineStart: bounds.left - input.left, blockStart: bounds.top - input.bottom }
  })).toEqual(comboboxPopupOffset)
  expect(await accountPopup.evaluate(popup => {
    const bounds = popup.getBoundingClientRect()
    const paintedElement = document.elementFromPoint(bounds.left + bounds.width / 2, bounds.top + bounds.height / 2)
    return paintedElement === popup || (paintedElement !== null && popup.contains(paintedElement))
  })).toBe(true)
  await page.evaluate(scrollY => window.scrollTo(0, scrollY), comboboxScrollY)
  await page.keyboard.press('End')
  await expect.poll(activeAccountOption).toBe(await accountListbox.getByRole('option', { name: 'Tax reserve' }).getAttribute('id'))
  await expect(accountListbox.getByRole('option', { name: 'Payroll clearing' })).toBeDisabled()
  await page.keyboard.press('Enter')
  await expect(accountValue).toHaveValue('102')

  const olderRequestStarted = page.waitForRequest(request => remoteQuery(request.url()) === 'oper')
  await parentAccount.fill('oper')
  await olderRequestStarted
  await expect(parentAccount).toHaveAttribute('aria-busy', 'true')
  await expect(accountPopup.getByRole('status')).toHaveText('Loading accounts')
  await expect(parentAccount).not.toHaveAttribute('aria-activedescendant')
  await parentAccount.fill('tax')
  await expect(accountListbox.getByRole('option', { name: 'Tax reserve' })).toBeVisible()
  await expect(accountListbox.getByRole('option', { name: 'Operating' })).toHaveCount(0)
  const staleRequestWindow = await page.request.get('/components/accounts/search/settled')
  expect(staleRequestWindow.status()).toBe(204)
  await expect(accountListbox.getByRole('option', { name: 'Tax reserve' })).toBeVisible()
  await expect(accountListbox.getByRole('option', { name: 'Operating' })).toHaveCount(0)
  await expect(parentAccount).toBeFocused()
  await expect.poll(activeAccountOption).toBe(await accountListbox.getByRole('option', { name: 'Tax reserve' }).getAttribute('id'))
  await page.keyboard.press('Enter')
  await expect(parentAccount).toHaveValue('Tax reserve')
  await expect(accountValue).toHaveValue('102')

  await parentAccount.fill('oper')
  await expect(accountListbox.getByRole('option', { name: 'Operating' })).toBeVisible()
  await expect(accountListbox.getByRole('option', { name: 'Tax reserve' })).toHaveCount(0)
  await expect.poll(activeAccountOption).toBe(await accountListbox.getByRole('option', { name: 'Operating' }).getAttribute('id'))
  await page.keyboard.press('Enter')
  await expect(parentAccount).toHaveValue('Operating')
  await expect(accountValue).toHaveValue('101')

  const clearedResults = page.waitForResponse(response => remoteQuery(response.url()) === '')
  await comboboxSurface.getByRole('button', { name: 'Clear Parent account' }).click()
  await clearedResults
  await expect(parentAccount).toBeFocused()
  await expect(parentAccount).toHaveValue('')
  await expect(accountValue).toHaveValue('')

  await parentAccount.fill('missing')
  await expect(accountPopup.getByRole('status')).toHaveText('No matching accounts')
  await expect(parentAccount).not.toHaveAttribute('aria-activedescendant')
  await parentAccount.fill('error')
  await expect(accountPopup.getByRole('alert')).toHaveText('Accounts could not be loaded.')
  await expect(parentAccount).toBeFocused()
  await expect(parentAccount).not.toHaveAttribute('aria-activedescendant')
  await accountPopup.getByRole('button', { name: 'Retry' }).click()
  await expect(accountPopup.getByRole('status')).toHaveText('No matching accounts')
  await expect(parentAccount).toBeFocused()
  await page.keyboard.press('Escape')
  await expect(parentAccount).toBeFocused()

  const loadingAccount = comboboxSurface.getByRole('combobox', { name: 'Loading account' })
  await expect(loadingAccount).toHaveAttribute('aria-busy', 'true')
  await loadingAccount.click()
  await expect(comboboxSurface.locator('#fve-select-components_loading_account-popup').getByRole('status')).toHaveText('Loading accounts')
  const validationAccount = comboboxSurface.getByRole('combobox', { name: 'Account with validation' })
  await expect(validationAccount).toHaveAttribute('aria-invalid', 'true')
  await expect(validationAccount).toHaveAttribute('aria-describedby', 'fve-select-components_validated_account-description fve-select-components_validated_account-validation')
  const disabledAccount = comboboxSurface.getByRole('combobox', { name: 'Disabled account' })
  const pendingAccount = comboboxSurface.getByRole('combobox', { name: 'Updating account' })
  await expect(disabledAccount).toBeDisabled()
  await expect(disabledAccount).toHaveAttribute('aria-disabled', 'true')
  await expect(pendingAccount).toBeDisabled()
  await expect(pendingAccount).toHaveAttribute('aria-busy', 'true')
  const unavailableAccountValues = comboboxSurface.locator('input[type="hidden"][name="disabledAccount"], input[type="hidden"][name="pendingAccount"]')
  await expect(unavailableAccountValues).toHaveCount(2)
  expect(await clonedControlEntries(unavailableAccountValues)).toEqual([])
  await attachScreenshot('components-select-search-state-matrix-desktop-dark')

  await page.setViewportSize({ width: 390, height: 844 })
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Light' }).click()
  const mobileSearchableSelectSurface = await openPreview()
  const mobileParentAccount = mobileSearchableSelectSurface.getByRole('button', { name: 'Parent account' })
  await mobileParentAccount.fill('error')
  await expect(mobileSearchableSelectSurface.locator('#fve-select-account-popup').getByRole('alert')).toHaveText('Accounts could not be loaded.')
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await attachScreenshot('components-select-search-mobile-light-error')
  expect(browserErrors).toEqual([])
})

test('searchable Select keeps a standard trigger and popup search for static and remote results', crossBrowser, async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/components/select')

  const staticExample = page.locator('#components-select-search')
  const staticTrigger = staticExample.getByRole('button', { name: 'Static account', exact: true })
  await staticTrigger.click()
  const staticSearch = staticExample.locator('#fve-select-components_static_account-search')
  await expect(staticSearch).toBeFocused()
  await staticSearch.fill('tax')
  const staticListbox = staticExample.getByRole('listbox', { name: 'Static account' })
  await expect(staticListbox.getByRole('option', { name: 'Tax reserve' })).toBeVisible()
  await expect(staticListbox.getByRole('option', { name: 'Payroll clearing' })).toBeHidden()
  await staticSearch.press('ArrowDown')
  await staticSearch.press('Enter')
  await expect(staticTrigger).toHaveText('Tax reserve')
  await expect(staticExample.locator('input[type="hidden"][name="staticAccount"]')).toHaveValue('102')

  const remoteExample = page.locator('#components-select-search-remote')
  const remoteTrigger = remoteExample.getByRole('button', { name: 'Parent account', exact: true })
  await remoteTrigger.click()
  const remoteSearch = remoteExample.locator('#fve-select-account-search')
  await expect(remoteSearch).toBeFocused()
  await remoteSearch.fill('tax')
  const remotePopup = remoteExample.locator('#fve-select-account-popup')
  await expect(remotePopup.getByRole('option', { name: 'Tax reserve' })).toBeVisible()
  await expect(remotePopup.getByRole('option', { name: 'Operating' })).toHaveCount(0)
  await remoteSearch.press('ArrowDown')
  await remoteSearch.press('Enter')
  await expect(remoteTrigger).toHaveText('Tax reserve')
  await expect(remoteExample.locator('input[type="hidden"][name="account"]')).toHaveValue('102')
  await remoteTrigger.click()
  await remoteSearch.fill('error')
  await expect(remotePopup.getByRole('alert')).toHaveText('Accounts could not be loaded.')
  await remotePopup.getByRole('button', { name: 'Retry' }).click()
  await expect(remotePopup.getByRole('status')).toHaveText('No matching accounts')
  await remoteSearch.press('Escape')
  await expect(remoteTrigger).toBeFocused()

  await expect(page.locator('#components-select-search-disabled').getByRole('button', { name: 'Disabled account' })).toBeDisabled()
  await expect(page.locator('#components-select-search-pending').getByRole('button', { name: 'Updating account' })).toBeDisabled()
  await page.setViewportSize({ width: 390, height: 844 })
  await remoteTrigger.click()
  await expect(remoteSearch).toBeFocused()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  expect(browserErrors).toEqual([])
})

test('Tabs preserve variants, automatic keyboard selection, instances, morphs, and responsive accessibility', crossBrowser, async ({ page }, testInfo) => {
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )
  const browserErrors = captureBrowserErrors(page)
  const attachScreenshot = async (name: string) => {
    if (testInfo.project.name !== 'chromium') return
    await testInfo.attach(name, {
      body: await page.screenshot({ fullPage: true, animations: 'disabled' }),
      contentType: 'image/png',
    })
  }
  const openPreview = () => openComponentGallery(page, '/components/tabs', 'Tabs')

  const surface = await openPreview()
  const segmented = surface.getByRole('tablist', { name: 'Example format' })
  const codeTab = segmented.getByRole('tab', { name: 'Code' })
  const previewTab = segmented.getByRole('tab', { name: 'Preview' })
  const codePanel = surface.getByRole('tabpanel', { name: 'Code' })
  const previewPanel = surface.getByRole('tabpanel', { name: 'Preview' })
  await expect(previewTab).toHaveAttribute('aria-selected', 'true')
  await expect(previewTab).toHaveAttribute('tabindex', '0')
  await expect(previewPanel).toBeVisible()
  await expect(codePanel).toBeHidden()

  await previewTab.focus()
  await page.keyboard.press('ArrowLeft')
  await expect(codeTab).toBeFocused()
  await expect(codeTab).toHaveAttribute('aria-selected', 'true')
  await expect(codePanel).toBeVisible()
  await expect(previewPanel).toBeHidden()
  await page.keyboard.press('Tab')
  await expect(codePanel).toBeFocused()
  await codeTab.focus()
  await page.keyboard.press('End')
  await expect(previewTab).toBeFocused()
  await expect(previewTab).toHaveAttribute('aria-selected', 'true')
  await page.keyboard.press('Home')
  await expect(codeTab).toBeFocused()
  await page.keyboard.press('ArrowRight')
  await expect(previewTab).toBeFocused()
  await page.keyboard.press('ArrowRight')
  await expect(codeTab).toBeFocused()
  await previewTab.click()

  const underlined = surface.getByRole('tablist', { name: 'Account sections' })
  const overviewTab = underlined.getByRole('tab', { name: 'Overview' })
  const activityTab = underlined.getByRole('tab', { name: 'Activity' })
  const settingsTab = underlined.getByRole('tab', { name: 'Settings' })
  await expect(overviewTab).toHaveAttribute('aria-selected', 'true')
  await overviewTab.focus()
  await page.keyboard.press('End')
  await expect(settingsTab).toBeFocused()
  await expect(settingsTab).toHaveAttribute('aria-selected', 'true')
  await page.keyboard.press('ArrowRight')
  await expect(overviewTab).toBeFocused()
  await activityTab.click()
  await expect(activityTab).toHaveAttribute('aria-selected', 'true')
  await expect(surface.getByRole('tabpanel', { name: 'Activity' })).toBeVisible()
  await expect(previewTab).toHaveAttribute('aria-selected', 'true')

  const refresh = surface.getByRole('button', { name: 'Refresh activity' })
  await refresh.click()
  await expect(surface.getByRole('status')).toHaveText('Review refreshed')
  await expect(surface.getByText('Updated just now.')).toBeVisible()
  await expect(underlined.getByRole('tab', { name: 'Activity' })).toHaveAttribute('aria-selected', 'true')
  await expect(surface.getByRole('button', { name: 'Refresh activity' })).toBeFocused()
  await expect(previewTab).toHaveAttribute('aria-selected', 'true')

  const ids = await surface.locator('[id]').evaluateAll(elements => elements.map(element => element.id))
  expect(new Set(ids).size).toBe(ids.length)
  const accessibility = await new AxeBuilder({ page })
    .include('#components-tabs-panel-preview')
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze()
  expect(accessibility.violations, 'Tabs preview').toEqual([])

  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  await underlined.getByRole('tab', { name: 'Activity' }).focus()
  await page.keyboard.press('ArrowLeft')
  await page.keyboard.press('ArrowRight')
  await expect(underlined.getByRole('tab', { name: 'Activity' })).toBeFocused()
  await attachScreenshot('components-tabs-desktop-dark-variants')

  await page.setViewportSize({ width: 390, height: 844 })
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Light' }).click()
  const mobileSurface = await openPreview()
  const mobileSegmented = mobileSurface.getByRole('tablist', { name: 'Example format' })
  const mobileUnderlined = mobileSurface.getByRole('tablist', { name: 'Account sections' })
  await mobileSegmented.getByRole('tab', { name: 'Code' }).focus()
  await page.keyboard.press('ArrowRight')
  await page.keyboard.press('ArrowLeft')
  await expect(mobileSegmented.getByRole('tab', { name: 'Code' })).toBeFocused()
  await expect(mobileUnderlined).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await attachScreenshot('components-tabs-mobile-light-variants')
  expect(browserErrors).toEqual([])
})

test('Dialogs and drawers preserve modal focus, safe confirmation, morphs, instances, and responsive behavior', crossBrowser, async ({ page }, testInfo) => {
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )
  const browserErrors = captureBrowserErrors(page)
  const openPreview = (path: string, heading: string) => openComponentGallery(page, path, heading)
  const attachScreenshot = async (name: string) => {
    if (testInfo.project.name !== 'chromium') return
    await testInfo.attach(name, {
      body: await page.screenshot({ fullPage: true, animations: 'disabled' }),
      contentType: 'image/png',
    })
  }
  const expectOutsideFocusBlocked = async (modal: Locator, outside: Locator) => {
    await outside.evaluate(element => (element as HTMLElement).focus())
    await expect(outside).not.toBeFocused()
    expect(await modal.evaluate(element => element.contains(document.activeElement) || document.activeElement === document.body)).toBe(true)
  }

  const dialogSurface = await openPreview('/components/dialog', 'Dialog')
  const dialogTrigger = dialogSurface.getByRole('button', { name: 'Review account' })
  const dialog = dialogSurface.getByRole('dialog', { name: 'Review account' })
  await dialogTrigger.click()
  await expect(dialog).toBeVisible()
  await expect(dialog.getByRole('button', { name: 'Close' })).toBeFocused()
  await page.keyboard.press('Tab')
  await expectOutsideFocusBlocked(dialog, dialogTrigger)
  await page.mouse.click(10, 400)
  await expect(dialog).toBeHidden()
  await expect(dialogTrigger).toBeFocused()

  const confirmationSurface = await openPreview('/components/confirmation-dialog', 'Confirmation dialog')
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  const confirmationTrigger = confirmationSurface.locator('#delete-account-confirmation-trigger')
  const confirmation = confirmationSurface.getByRole('alertdialog', { name: 'Delete account?' })
  await confirmationTrigger.click()
  await expect(confirmation).toBeVisible()
  const cancelConfirmation = confirmation.getByRole('button', { name: 'Keep account' })
  const confirmDeletion = confirmation.getByRole('button', { name: 'Delete account' })
  await expect(cancelConfirmation).toBeFocused()
  await page.keyboard.press('Tab')
  await expectOutsideFocusBlocked(confirmation, confirmationTrigger)
  await confirmDeletion.focus()
  await expect(confirmDeletion).toBeFocused()
  const confirmationAccessibility = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze()
  expect(confirmationAccessibility.violations, 'open ConfirmationDialog').toEqual([])

  let confirmationRequests = 0
  page.on('request', request => {
    if (new URL(request.url()).pathname === '/components/dialogs/confirm') confirmationRequests++
  })
  const confirmationResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/components/dialogs/confirm')
  await confirmDeletion.click()
  await expect(confirmDeletion).toBeDisabled()
  await expect(confirmDeletion).toHaveAttribute('aria-busy', 'true')
  await expect(confirmation.getByRole('status')).toHaveText('Confirmation in progress.')
  await page.keyboard.press('Escape')
  await expect(confirmation).toBeVisible()
  await attachScreenshot('components-confirmation-dialog-desktop-dark-pending')
  await confirmationResponse
  await expect(confirmation.getByRole('alert')).toHaveText('Operating cannot be deleted while posted entries are assigned to its open period.')
  await expect(confirmation.getByRole('button', { name: 'Delete account' })).toBeFocused()
  expect(confirmationRequests).toBe(1)
  await confirmation.getByRole('button', { name: 'Keep account' }).click()
  await expect(confirmation).toBeHidden()
  await expect(confirmationTrigger).toBeFocused()

  const drawerSurface = await openPreview('/components/drawer', 'Drawer')
  const accountDrawerTrigger = drawerSurface.getByRole('button', { name: 'Open account details' })
  const editorDrawerTrigger = drawerSurface.getByRole('button', { name: 'Edit account', exact: true })
  const filterDrawerTrigger = drawerSurface.getByRole('button', { name: 'Open filters' })
  const accountDrawer = drawerSurface.getByRole('dialog', { name: 'Account details' })
  const editorDrawer = drawerSurface.getByRole('dialog', { name: 'Edit account' })
  const filterDrawer = drawerSurface.getByRole('dialog', { name: 'Account filters' })
  await accountDrawerTrigger.click()
  await expect(accountDrawer).toBeVisible()
  await expect(accountDrawer.getByRole('button', { name: 'Refresh details' })).toBeFocused()
  await expect(accountDrawer).toContainText('Operating checking')
  const accountDrawerBox = await accountDrawer.boundingBox()
  expect(accountDrawerBox).toBeTruthy()
  expect(accountDrawerBox!.x + accountDrawerBox!.width).toBeCloseTo(await page.evaluate(() => window.innerWidth), 0)
  expect(accountDrawerBox!.width).toBeLessThanOrEqual(384)
  await attachScreenshot('components-drawer-desktop-dark-end')
  await page.mouse.click(10, 400)
  await expect(accountDrawer).toBeHidden()
  await expect(accountDrawerTrigger).toBeFocused()

  await filterDrawerTrigger.click()
  await expect(filterDrawer).toBeVisible()
  const filterDrawerBox = await filterDrawer.boundingBox()
  expect(filterDrawerBox).toBeTruthy()
  expect(filterDrawerBox!.x).toBeCloseTo(0, 0)
  await page.keyboard.press('Escape')
  await expect(filterDrawer).toBeHidden()
  await expect(filterDrawerTrigger).toBeFocused()

  await accountDrawerTrigger.click()
  const refreshPanel = accountDrawer.getByRole('button', { name: 'Refresh details' })
  await refreshPanel.click()
  await expect(accountDrawer.getByRole('status')).toHaveText('Account details refreshed from the server.')
  await expect(accountDrawer.getByRole('button', { name: 'Refresh details' })).toBeFocused()
  await expect(accountDrawer).toBeVisible()
  await page.keyboard.press('Tab')
  await expectOutsideFocusBlocked(accountDrawer, accountDrawerTrigger)
  await page.keyboard.press('Escape')
  await expect(accountDrawerTrigger).toBeFocused()

  await page.setViewportSize({ width: 1100, height: 600 })
  await editorDrawerTrigger.click()
  await expect(editorDrawer.getByLabel('Account name', { exact: true })).toBeFocused()
  const editorBox = await editorDrawer.boundingBox()
  expect(editorBox).toBeTruthy()
  expect(editorBox!.width).toBeGreaterThan(384)
  expect(editorBox!.width).toBeLessThanOrEqual(672)
  const editorBody = editorDrawer.locator('#account-editor-form').locator('..')
  const editorFooter = editorDrawer.getByRole('button', { name: 'Save changes', exact: true }).locator('../..')
  await expect.poll(() => editorBody.evaluate(element => element.scrollHeight - element.clientHeight)).toBeGreaterThan(0)
  const footerBeforeScroll = await editorFooter.boundingBox()
  await editorDrawer.getByLabel('Account name', { exact: true }).fill('Operating account')
  await editorBody.evaluate(element => { element.scrollTop = element.scrollHeight })
  const footerAfterScroll = await editorFooter.boundingBox()
  expect(footerBeforeScroll).toBeTruthy()
  expect(footerAfterScroll).toBeTruthy()
  expect(footerAfterScroll!.y).toBeCloseTo(footerBeforeScroll!.y, 0)
  await expect(editorDrawer.getByRole('button', { name: 'Save changes', exact: true })).toHaveAttribute('form', 'account-editor-form')
  await page.keyboard.press('Escape')
  await expect(editorDrawerTrigger).toBeFocused()
  await editorDrawerTrigger.click()
  await expect(editorDrawer.getByLabel('Account name', { exact: true })).toHaveValue('Operating account')
  await page.keyboard.press('Escape')

  await page.setViewportSize({ width: 390, height: 844 })
  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Light' }).click()
  const mobileDrawerSurface = await openPreview('/components/drawer', 'Drawer')
  const mobileDrawerTrigger = mobileDrawerSurface.getByRole('button', { name: 'Open account details' })
  const mobileDrawer = mobileDrawerSurface.getByRole('dialog', { name: 'Account details' })
  await mobileDrawerTrigger.click()
  await expect(mobileDrawer.getByRole('button', { name: 'Refresh details' })).toBeFocused()
  const mobileDrawerBox = await mobileDrawer.boundingBox()
  expect(mobileDrawerBox).toBeTruthy()
  expect(mobileDrawerBox!.x).toBeGreaterThanOrEqual(0)
  expect(mobileDrawerBox!.x + mobileDrawerBox!.width).toBeLessThanOrEqual(390)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await attachScreenshot('components-drawer-mobile-light-end')
  await page.mouse.click(10, 400)
  await expect(mobileDrawer).toBeHidden()
  await expect(mobileDrawerTrigger).toBeFocused()
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
  expect(await css.text()).toContain('tailwindcss v4.2.2')

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

test('search filters pages and headings with keyboard access', crossBrowser, async ({ page }) => {
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

test('Docs typography uses semantic ancillary, UI, reading, and code roles', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto('/', { waitUntil: 'domcontentloaded' })

  const repository = page.getByRole('link', { name: 'View repository on GitHub' })
  await expect(repository).toBeVisible()
  await expect(repository.locator('svg')).toHaveCount(1)
  await expect(repository).not.toContainText('Repository')
  await expect(page.getByRole('button', { name: 'Search documentation' })).toHaveCSS('font-size', '14px')
  await expect(page.locator('.spec-nav-link').first()).toHaveCSS('font-size', '14px')

  const paragraph = page.locator('.spec-paragraph').first()
  await expect(paragraph).toBeVisible()
  await expect(paragraph).toHaveCSS('font-size', '16px')
  expect(await paragraph.evaluate(element => getComputedStyle(element).fontFamily)).toContain('Noto Sans')

  const code = page.locator('.spec-code code').first()
  await expect(code).toBeVisible()
  await expect(code).toHaveCSS('font-size', '14px')
  expect(await code.evaluate(element => getComputedStyle(element).fontFamily)).toContain('Noto Sans Mono')
  expect(Number.parseFloat(await code.evaluate(element => getComputedStyle(element).lineHeight))).toBeCloseTo(21.7, 1)
  await expect(page.getByRole('button', { name: 'Copy code' }).first()).toHaveCSS('font-size', '12px')

  await page.goto('/docs/previews/tables', { waitUntil: 'domcontentloaded' })
  await expect(page.locator('.spec-table th').first()).toHaveCSS('font-size', '12px')
  await expect(page.locator('.spec-table td').first()).toHaveCSS('font-size', '14px')

  await page.goto('/custom', { waitUntil: 'domcontentloaded' })
  await expect(page.locator('.spec-toc-title')).toHaveCSS('font-size', '12px')
})

test('color mode selector supports persistence, keyboard navigation, and system changes', crossBrowser, async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'light' })
  await page.goto('/docs/components/layouts', { waitUntil: 'domcontentloaded' })
  await page.evaluate(() => localStorage.removeItem('fsharp-viewengine-docs-navigation-color-mode'))
  await page.reload({ waitUntil: 'domcontentloaded' })

  await page.locator('[data-docs-example]').first().getByRole('tab', { name: 'Code' }).click()
  const codeSurface = page.locator('pre.spec-code').first()
  await expect(codeSurface).toBeVisible()
  await expect(codeSurface).toHaveCSS('background-color', 'rgb(246, 248, 250)')
  await expect(codeSurface).toHaveCSS('color', 'rgb(36, 41, 47)')

  const trigger = page.getByRole('button', { name: 'Choose color theme' })
  await trigger.click()
  const menu = page.getByRole('menu', { name: 'Choose color theme' })
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

test('mobile navigation manages modal focus and does not overflow', crossBrowser, async ({ page }) => {
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

test('desktop table of contents follows the final visible section after preferred-font reflow', crossBrowser, async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )

  let releaseFont = () => {}
  const fontGate = new Promise<void>(resolve => { releaseFont = resolve })
  await page.route('**/fonts/noto-sans-latin.woff2', async route => {
    await fontGate
    await route.continue().catch(() => {})
  })

  const browserErrors = captureBrowserErrors(page)
  await page.goto('/custom', { waitUntil: 'domcontentloaded' })
  await page.waitForFunction(() => document.fonts.status === 'loading')

  const main = page.locator('.spec-main')
  const finalSectionLink = page.locator('.spec-toc a[href="#shoelace-example"]')
  await expect(page.locator('.spec-toc a[aria-current="location"]')).toHaveCount(1)
  const fallbackHeight = await main.evaluate(element => element.scrollHeight)
  await main.evaluate(element => element.scrollTo({ top: element.scrollHeight, behavior: 'instant' }))
  await expect(finalSectionLink).toHaveAttribute('aria-current', 'location')

  releaseFont()
  await page.evaluate(() => document.fonts.ready)
  await expect.poll(() => main.evaluate(element => element.scrollHeight)).toBeGreaterThan(fallbackHeight)
  await main.evaluate(element => element.scrollTo({ top: element.scrollHeight - element.clientHeight - 70, behavior: 'instant' }))
  await expect(page.locator('#shoelace-example')).toBeInViewport()
  await main.dispatchEvent('scroll')

  await expect(finalSectionLink).toHaveAttribute('aria-current', 'location')
  expect(browserErrors).toEqual([])
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

test('Docs navigation scrolls content to top and highlights morphed code', crossBrowser, async ({ page }) => {
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

test('Docs navigation loads Prism dependencies before highlighting a code page', crossBrowser, async ({ page }) => {
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

test('rapid full-document navigation cancels old Prism loading without browser errors', crossBrowser, async ({ page }) => {
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )

  let interceptedPrism: import('@playwright/test').Route | undefined
  let releaseIntercept = () => {}
  const interceptReleased = new Promise<void>(resolve => { releaseIntercept = resolve })
  let markIntercepted = () => {}
  const prismIntercepted = new Promise<void>(resolve => { markIntercepted = resolve })
  await page.route('**/scripts/prism*.js', async route => {
    if (!interceptedPrism) {
      interceptedPrism = route
      markIntercepted()
      await interceptReleased
      return
    }
    await route.continue()
  })

  const browserErrors = captureBrowserErrors(page)
  await page.goto('/custom', { waitUntil: 'domcontentloaded' })
  await page.waitForFunction(() => Boolean((window as any).fsharpDocsCode?.loading))
  await prismIntercepted
  await page.waitForFunction(() => document.fonts?.status === 'loaded')

  const navigationRequest = page.waitForRequest(request => request.isNavigationRequest() && new URL(request.url()).pathname === '/getting-started/first-view')
  const navigation = page.goto('/getting-started/first-view', { waitUntil: 'domcontentloaded' })
  await navigationRequest
  await interceptedPrism!.abort('aborted').catch(() => {})
  releaseIntercept()
  await navigation
  await page.waitForFunction(() => Boolean((window as any).fsharpDocsCode?.loading))
  await page.evaluate(() => (window as any).fsharpDocsCode.loading)

  await expect(page.locator('.spec-code .token.keyword').first()).toBeVisible()
  expect(browserErrors).toEqual([])
})

test('active-document Prism failures remain observable', async ({ page }) => {
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )
  await page.route('**/scripts/prism.1.29.0.min.js', route => route.abort('failed'))
  const pageErrors: string[] = []
  page.on('pageerror', error => pageErrors.push(error.message))

  await page.goto('/custom', { waitUntil: 'domcontentloaded' })

  await expect(page.getByRole('heading', { level: 1, name: 'Custom Elements & Attributes' })).toBeVisible()
  await expect.poll(() => pageErrors.some(error => error.includes('Unable to load Prism asset: /scripts/prism.1.29.0.min.js'))).toBe(true)
})

test('hidden active-document Prism failures remain observable', crossBrowser, async ({ page }) => {
  await page.addInitScript(() => {
    Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'hidden' })
  })
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
    route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
  )
  await page.route('**/scripts/prism.1.29.0.min.js', route => route.abort('failed'))
  const pageErrors: string[] = []
  page.on('pageerror', error => pageErrors.push(error.message))

  await page.goto('/custom', { waitUntil: 'domcontentloaded' })

  expect(await page.evaluate(() => ({ visibility: document.visibilityState, unloading: (window as any).fsharpDocsCode?.unloading }))).toEqual({ visibility: 'hidden', unloading: false })
  await expect.poll(() => pageErrors.some(error => error.includes('Unable to load Prism asset: /scripts/prism.1.29.0.min.js'))).toBe(true)
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

test('code and preview examples support pointer and keyboard tabs', crossBrowser, async ({ page }) => {
  await page.goto('/extensions/svg', { waitUntil: 'domcontentloaded' })
  const example = page.locator('[data-docs-example="true"]').first()
  const preview = example.getByRole('tab', { name: 'Preview' })
  const code = example.getByRole('tab', { name: 'Code' })

  await expect(code).toHaveAttribute('aria-selected', 'true')
  expect((await code.boundingBox())!.x).toBeLessThan((await preview.boundingBox())!.x)
  await expect(example.locator('.spec-example-toolbar .docs-copy-code')).toHaveCount(0)
  const codePanel = example.getByRole('tabpanel', { name: 'Code' })
  await expect(codePanel).toBeVisible()
  await expect(codePanel.getByRole('button', { name: /Copy .* code/ })).toBeVisible()
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
  await expect(page.getByText('This documentation site is built with FSharp.ViewEngine.Components.Documentation', { exact: false })).toBeVisible()
  const pager = page.getByRole('navigation', { name: 'Page navigation' })
  await expect(page.getByRole('link', { name: 'Browse components' })).toHaveAttribute('href', '/docs/components/layouts')
  await expect(page.getByRole('link', { name: 'Browse page examples' })).toHaveAttribute('href', '/docs/page-examples/documentation-site')
  await expect(pager.getByRole('link', { name: /Previous Media management/ })).toHaveAttribute('href', '/components/page-examples/media-management')
  const next = pager.getByRole('link', { name: /Next Layouts/ })
  await expect(next).toBeVisible()
  await next.click()
  await expect(page).toHaveURL('/docs/components/layouts')
  await expect(page.getByRole('heading', { level: 1, name: 'Layouts' })).toBeVisible()
})

test('Tailwind Plus Elements previews render and operate the actual custom elements', crossBrowser, async ({ page }) => {
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

const expectNoRawMermaid = async (diagram: Locator) => {
  expect(await diagram.innerText()).not.toContain('flowchart LR')
  await expect(diagram.locator('.error-icon, .error-text')).toHaveCount(0)
}

test('initial document readiness renders pending diagrams independently of data-init timing', crossBrowser, async ({ page }) => {
  await page.addInitScript(() => {
    let renderMermaid: ((element: Element, pendingOnly?: boolean) => Promise<void>) | undefined
    Object.defineProperty(window, 'renderMermaid', {
      configurable: true,
      get: () => renderMermaid,
      set: value => {
        renderMermaid = (element, pendingOnly) =>
          element.matches('.mermaid') ? Promise.resolve() : value(element, pendingOnly)
      },
    })
  })

  await page.goto('/docs/components/diagrams', { waitUntil: 'domcontentloaded' })

  const diagram = page.locator('main .mermaid.spec-diagram').first()
  await expect(diagram).toHaveAttribute('data-mermaid-state', 'rendered')
  await expect(diagram.locator('svg')).toBeVisible()
  await expectNoRawMermaid(diagram)
})

test('diagrams render directly without exposing Mermaid source', async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/docs/components/diagrams', { waitUntil: 'domcontentloaded' })

  const diagram = page.locator('main .mermaid.spec-diagram').first()
  await expect(diagram).toHaveAttribute('data-mermaid-state', 'rendered')
  await expect(diagram.locator('svg')).toBeVisible()
  await expect(diagram).not.toHaveAttribute('aria-busy', 'true')
  await expectNoRawMermaid(diagram)
  expect(browserErrors).toEqual([])
})

test('delayed Mermaid loading shows accessible pending content and never raw source', async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  let releaseAsset: (() => void) | undefined
  const assetGate = new Promise<void>(resolve => { releaseAsset = resolve })
  let intercepted = false
  await page.route('**/scripts/mermaid.11.16.0.min.js', async route => {
    intercepted = true
    await assetGate
    await route.continue()
  })

  await page.goto('/docs/components/diagrams', { waitUntil: 'domcontentloaded' })
  await expect.poll(() => intercepted).toBe(true)
  const diagram = page.locator('main .mermaid.spec-diagram').first()
  await expect(diagram).toHaveAttribute('data-mermaid-state', 'pending')
  await expect(diagram).toHaveAttribute('aria-busy', 'true')
  await expect(diagram.getByRole('status')).toHaveText('Rendering diagram…')
  await expect(diagram.locator('svg')).toHaveCount(0)
  await expectNoRawMermaid(diagram)

  releaseAsset!()
  await expect(diagram).toHaveAttribute('data-mermaid-state', 'rendered')
  await expect(diagram.locator('svg')).toBeVisible()
  await expectNoRawMermaid(diagram)
  expect(browserErrors).toEqual([])
})

test('an unavailable Mermaid asset shows the accessible deterministic failure state', async ({ page }) => {
  const pageErrors: string[] = []
  page.on('pageerror', error => pageErrors.push(error.message))
  await page.route('**/scripts/mermaid.11.16.0.min.js', route => route.abort('failed'))

  await page.goto('/docs/components/diagrams', { waitUntil: 'domcontentloaded' })
  const diagram = page.locator('main .mermaid.spec-diagram').first()
  await expect(diagram).toHaveAttribute('data-mermaid-state', 'failed')
  await expect(diagram.getByRole('alert')).toHaveText('Diagram unavailable.')
  await expect(diagram).not.toHaveAttribute('aria-busy', 'true')
  await expect(diagram.locator('svg')).toHaveCount(0)
  await expectNoRawMermaid(diagram)
  expect(pageErrors).toEqual([])
})

test('a Mermaid render rejection shows the accessible failure state without an error SVG', async ({ page }) => {
  const pageErrors: string[] = []
  page.on('pageerror', error => pageErrors.push(error.message))
  await page.goto('/docs/components/diagrams', { waitUntil: 'domcontentloaded' })
  const diagram = page.locator('main .mermaid.spec-diagram').first()
  await expect(diagram.locator('svg')).toBeVisible()

  await diagram.evaluate(async element => {
    element.setAttribute('data-mermaid-source', 'not-a-valid-mermaid-diagram')
    await (window as typeof window & { renderMermaid?: (element: Element) => Promise<void> }).renderMermaid?.(element)
  })

  await expect(diagram).toHaveAttribute('data-mermaid-state', 'failed')
  await expect(diagram.getByRole('alert')).toHaveText('Diagram unavailable.')
  await expect(diagram.locator('svg')).toHaveCount(0)
  await expect(page.locator('.error-icon, .error-text')).toHaveCount(0)
  await expectNoRawMermaid(diagram)
  expect(pageErrors).toEqual([])
})

test('pending diagrams render after Docs navigation without relying on repeated data-init', crossBrowser, async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/docs/components/content', { waitUntil: 'domcontentloaded' })
  await page.evaluate(() => {
    const docsWindow = window as typeof window & {
      renderMermaid?: (element: Element, pendingOnly?: boolean) => Promise<void>
      mermaidRenderHosts?: string[]
    }
    const renderMermaid = docsWindow.renderMermaid
    docsWindow.mermaidRenderHosts = []
    docsWindow.renderMermaid = (element, pendingOnly) => {
      const fromDataInit = element.matches('.mermaid')
      docsWindow.mermaidRenderHosts?.push(fromDataInit ? 'data-init' : element.id)
      return fromDataInit ? Promise.resolve() : renderMermaid?.(element, pendingOnly) ?? Promise.resolve()
    }
  })

  await page.locator('#nav-docs-diagrams').click()
  await expect(page).toHaveURL('/docs/components/diagrams')

  const diagram = page.locator('main .mermaid.spec-diagram').first()
  await expect(diagram).toHaveAttribute('data-mermaid-state', 'rendered')
  await expect(diagram.locator('svg')).toBeVisible()
  await expect.poll(() => page.evaluate(() => (window as typeof window & { mermaidRenderHosts?: string[] }).mermaidRenderHosts)).toContain('page-content')
  expect(await page.evaluate(() => (window as typeof window & { mermaidRenderHosts?: string[] }).mermaidRenderHosts)).toContain('data-init')

  await page.evaluate(async () => {
    const diagram = document.querySelector<HTMLElement>('main .mermaid.spec-diagram')!
    diagram.dataset.mermaidState = 'rendered'
    diagram.dataset.mermaidRenderedSource = 'stale-source'
    diagram.replaceChildren()
    await (window as typeof window & { renderMermaid?: (element: Element, pendingOnly?: boolean) => Promise<void> }).renderMermaid?.(document.getElementById('page-content')!, true)
  })
  await expect(diagram.locator('svg')).toBeVisible()
  await expectNoRawMermaid(diagram)
  expect(browserErrors).toEqual([])
})

test('diagrams render after Docs navigation and light-dark rerenders', crossBrowser, async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/docs/components/content', { waitUntil: 'domcontentloaded' })
  await page.locator('#nav-docs-diagrams').click()
  await expect(page).toHaveURL('/docs/components/diagrams')

  const diagram = page.locator('main .mermaid.spec-diagram').first()
  await expect(diagram).toHaveAttribute('data-mermaid-state', 'rendered')
  const lightSvg = await diagram.locator('svg').evaluate(element => element.outerHTML)
  await expectNoRawMermaid(diagram)

  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  await expect(page.locator('html')).toHaveClass(/dark/)
  await expect.poll(() => diagram.locator('svg').evaluate(element => element.outerHTML)).not.toBe(lightSvg)
  const darkSvg = await diagram.locator('svg').evaluate(element => element.outerHTML)
  await expectNoRawMermaid(diagram)

  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Light' }).click()
  await expect(page.locator('html')).not.toHaveClass(/dark/)
  await expect.poll(() => diagram.locator('svg').evaluate(element => element.outerHTML)).not.toBe(darkSvg)
  await expectNoRawMermaid(diagram)
  expect(browserErrors).toEqual([])
})

test('diagram previews rerender after their hidden panel becomes visible', crossBrowser, async ({ page }) => {
  await page.goto('/docs/components/diagrams', { waitUntil: 'domcontentloaded' })
  const example = page.locator('[data-docs-example="true"]:has(#docs-mermaid-example-tab-preview)')
  await example.getByRole('tab', { name: 'Preview' }).click()
  const preview = example.locator('iframe').contentFrame()
  const diagram = preview.locator('.mermaid svg')
  await expect(diagram).toBeVisible()
  await expect.poll(() => diagram.getAttribute('viewBox')).not.toBe('-8 -8 16 16')
  expect((await diagram.boundingBox())!.width).toBeGreaterThan(200)
})

test('documentation remains readable when text is resized to 200 percent', crossBrowser, async ({ page }) => {
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

test('code-free catalog indexes settle without starting Prism', crossBrowser, async ({ page }) => {
  const prismRequests: string[] = []
  page.on('request', request => {
    if (/\/scripts\/prism|\/css\/prism/.test(new URL(request.url()).pathname)) prismRequests.push(request.url())
  })
  for (const path of ['/components/application', '/components/primitives']) {
    await gotoAfterDocsAssetSettlement(page, path)
    await expect(page.locator('code[class*="language-"]')).toHaveCount(0)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
  }
  expect(prismRequests).toEqual([])
})

test('Documentation namespaces wrap within narrow and enlarged reading columns', crossBrowser, async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 844 })
  await page.goto('/docs', { waitUntil: 'domcontentloaded' })
  const paragraph = page.getByText('This documentation site is built with FSharp.ViewEngine.Components.Documentation.', { exact: false })
  for (const dark of [false, true]) {
    for (const fontSize of ['100%', '200%']) {
      await page.evaluate(({ dark, fontSize }) => {
        document.documentElement.classList.toggle('dark', dark)
        document.documentElement.style.fontSize = fontSize
      }, { dark, fontSize })
      await expect(paragraph).toBeVisible()
      await expect.poll(() => paragraph.evaluate(element => element.scrollWidth - element.clientWidth)).toBeLessThanOrEqual(1)
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
    }
  }
})

test('Docs catalog navigation updates articles without a full-page browser error', crossBrowser, async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/docs/page-examples/api-reference', { waitUntil: 'domcontentloaded' })
  await expect(page.getByRole('button', { name: 'Toggle Documentation section', exact: true })).toHaveAttribute('aria-expanded', 'true')
  await expect(page.locator('#nav-fsharp-viewengine-components-documentation-components')).toBeVisible()
  await expect(page.locator('#nav-fsharp-viewengine-components-documentation-page-examples')).toBeVisible()
  await page.locator('#nav-fsharp-viewengine-components-documentation-components').click()
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

test('Docs component and page-example catalogs default to complete styled previews', async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  const catalogRoutes = routes.filter(route => route.path.startsWith('/docs/components/') || route.path.startsWith('/docs/page-examples/'))
  let reviewedPreviews = 0

  for (const route of catalogRoutes) {
    await page.goto(route.path, { waitUntil: 'domcontentloaded' })
    reviewedPreviews += await page.locator('button[role="tab"][id^="docs-"][id$="-example-tab-preview"]:visible').count()
    const examples = page.locator('[data-docs-example="true"]')
    expect(await examples.count(), route.path).toBeGreaterThan(0)
    for (const example of await examples.all()) {
      const previewTab = example.locator(':scope > .spec-example-toolbar').getByRole('tab', { name: 'Preview' })
      const panelId = await previewTab.getAttribute('aria-controls')
      expect(panelId).toBeTruthy()
      await expect(previewTab).toHaveAttribute('aria-selected', 'true')
      const preview = page.locator(`#${panelId}`)
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
  expect(reviewedPreviews).toBe(25)
  expect(browserErrors).toEqual([])
})

test('catalog code is extracted from the same compiled definition as its preview', async ({ page }) => {
  await page.goto('/docs/components/fixture', { waitUntil: 'domcontentloaded' })
  const example = page.locator('[data-docs-example="true"]:has(#docs-state-tabs-example-tab-preview)')
  await example.getByRole('tab', { name: 'Code' }).click()
  const source = await example.getByRole('tabpanel', { name: 'Code' }).innerText()
  expect(source).toContain('Tabs.create "component-workflow-states" "Workflow states"')
  expect(source).toContain('Tabs.withVariant TabsVariant.Underlined')
  expect(source).toContain('productScreen "ready"')
  expect(source).not.toContain('docsStateTabs')

  await example.getByRole('tab', { name: 'Preview' }).click()
  const preview = example.getByRole('tabpanel', { name: 'Preview' })
  await expect(preview.locator('iframe')).toHaveCount(0)
  const workflowTabs = preview.locator('#component-workflow-states')
  await expect(workflowTabs.getByRole('tab', { name: 'Ready' })).toBeVisible()
  await expect(workflowTabs.getByRole('tabpanel')).toContainText('Render your first component')
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
  await expect(preview.getByText('Rendered HTML and response media type.')).toBeVisible()
  await expect(preview.locator('.docs-code-panel')).toHaveCount(2)

  await page.setViewportSize({ width: 390, height: 844 })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})

test('specification page example state tabs work after parsing while the optional CDN module is pending', crossBrowser, async ({ page }) => {
  let releaseTailwind = () => {}
  const tailwindReleased = new Promise<void>(resolve => { releaseTailwind = resolve })
  let markTailwindIntercepted = () => {}
  const tailwindIntercepted = new Promise<void>(resolve => { markTailwindIntercepted = resolve })
  await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', async route => {
    markTailwindIntercepted()
    await tailwindReleased
    await route.fulfill({ status: 200, contentType: 'text/javascript', body: '' })
  })

  const browserErrors = captureBrowserErrors(page)
  const navigation = page.goto('/docs/page-examples/executable-specification', { waitUntil: 'commit' })
  await tailwindIntercepted

  try {
    await navigation
    await expect.poll(() => page.evaluate(() => (window as any).fsharpDocsTailwindElements?.startedAt)).toBe('interactive')
    await waitForDocsCodeSettlement(page)
    const example = page.locator('[data-docs-example="true"]').first()
    await example.getByRole('tab', { name: 'Preview' }).click()
    const preview = example.locator('iframe').contentFrame()
    const ready = preview.getByRole('tab', { name: 'Ready' })
    const validation = preview.getByRole('tab', { name: 'Validation' })

    await expect(preview.locator('body')).toHaveClass(/fve-components/)
    await expect(preview.locator('body')).toHaveClass(/fve-theme-sky/)
    await expect(ready).toBeVisible()
    const restingColor = await validation.evaluate(element => getComputedStyle(element).color)
    await validation.hover()
    await expect.poll(() => validation.evaluate(element => getComputedStyle(element).color)).not.toBe(restingColor)
    await validation.click()
    await expect(validation).toHaveAttribute('aria-selected', 'true')
    await expect(preview.getByRole('tabpanel', { name: 'Validation' })).toBeVisible()

    await validation.press('ArrowLeft')
    await expect(ready).toBeFocused()
    await expect(ready).toHaveAttribute('aria-selected', 'true')

    releaseTailwind()
    await page.evaluate(() => (window as any).fsharpDocsTailwindElements.loading)
    expect(browserErrors).toEqual([])
  } finally {
    releaseTailwind()
  }
})

test('benchmark tables remain readable without page overflow on mobile', crossBrowser, async ({ page }) => {
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
