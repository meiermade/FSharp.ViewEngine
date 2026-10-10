import AxeBuilder from '@axe-core/playwright'
import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test'

const productionOrigin = 'https://fve.meiermade.com'
const crossBrowser = { tag: '@cross-browser' }

async function resolvedVariableColor(root: Locator, property: 'background-color' | 'color', variable: string) {
  return root.evaluate((element, options) => {
    const probe = document.createElement('span')
    if (options.property === 'background-color') probe.style.backgroundColor = `var(${options.variable})`
    else probe.style.color = `var(${options.variable})`
    element.appendChild(probe)
    const value = getComputedStyle(probe).getPropertyValue(options.property)
    probe.remove()
    return value
  }, { property, variable })
}

async function openComponentGallery(page: Page, path: string, heading: string) {
  await gotoAfterDocsAssetSettlement(page, path)
  await expect(page.getByRole('heading', { level: 1, name: heading, exact: true })).toBeVisible()
  const examples = page.locator('[data-docs-layout="gallery"] [data-docs-example="true"]')
  expect(await examples.count()).toBeGreaterThan(0)
  for (const example of await examples.all()) {
    const preview = example.locator(':scope > [data-docs-example-toolbar="true"]').getByRole('tab', { name: 'Preview', exact: true })
    await expect(preview).toHaveAttribute('aria-selected', 'true')
    await expect(page.locator(`#${await preview.getAttribute('aria-controls')}`)).toBeVisible()
  }
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), path).toBe(true)
  return page.locator('[data-docs-layout="gallery"] [data-docs-example-preview="true"] .fve-components')
}

const representativeRoutes = [
  { path: '/', heading: 'FSharp.ViewEngine' },
  { path: '/getting-started/first-view', heading: 'Build your first view' },
  { path: '/components', heading: 'Components' },
  { path: '/components/button', heading: 'Button' },
  { path: '/components/select', heading: 'Select' },
] as const

async function publicRoutePaths(request: APIRequestContext) {
  const response = await request.get('/sitemap.xml')
  expect(response.status()).toBe(200)
  const xml = await response.text()
  return [...xml.matchAll(/<loc>([^<]+)<\/loc>/g)].map(match => new URL(match[1]).pathname)
}

async function docsCatalogPaths(request: APIRequestContext) {
  const published = await publicRoutePaths(request)
  const representative = ['/components/button', '/components/code-block', '/components/page-top-bar']
  for (const path of representative) expect(published).toContain(path)
  return representative
}

const componentAccessibilityRouteGroups = [
  {
    name: 'foundations and data display',
    paths: ['/components/table'],
  },
  {
    name: 'fields and navigation',
    paths: ['/components/select'],
  },
  {
    name: 'choices overlays and page composition',
    paths: ['/components/dialog'],
  },
  {
    name: 'shell structure',
    paths: ['/components/page-top-bar'],
  },
  {
    name: 'financial application',
    paths: ['/examples/application/accounts'],
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

test('representative documentation routes render without browser errors', async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  for (const route of representativeRoutes) {
    const response = await page.goto(route.path, { waitUntil: 'domcontentloaded' })
    expect(response?.status(), `${route.path} status`).toBe(200)
    await expect(page.getByRole('heading', { level: 1, name: route.heading, exact: true })).toHaveCount(1)
    await expect(page.locator('main[data-docs-main="true"]')).toBeVisible()
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  }

  expect(browserErrors).toEqual([])
})

test('legacy Docs catalog routes remain aliases with canonical destinations', async ({ request }) => {
  const aliases = [
    ['/docs/components', '/components'],
    ['/docs-components', '/components'],
    ['/docs/examples/api-reference', '/examples/api-documentation'],
    ['/docs/examples/executable-specification', '/examples/specification'],
  ] as const

  for (const [alias, canonicalPath] of aliases) {
    const response = await request.get(alias)
    expect(response.status(), alias).toBe(200)
    expect(new URL(response.url()).pathname).toBe(canonicalPath)
    expect(await response.text()).toContain(`rel="canonical" href="${productionOrigin}${canonicalPath}"`)
  }
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

test('Representative Components pages provide focused examples, navigation, interaction, themes, and responsive accessibility', async ({ page }, testInfo) => {
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
    ['/components/select', 'Select'],
    ['/components/dropdown-menu', 'Dropdown menu'],
    ['/components/page-top-bar', 'Page top bar'],
    ['/components/month-calendar', 'Month calendar'],
    ['/components/card', 'Card'],
  ] as const

  const openPreview = (path: string, heading: string) => openComponentGallery(page, path, heading)

  await gotoAfterDocsAssetSettlement(page, '/components/select', 'domcontentloaded')
  const formControls = page.getByLabel('Toggle Form controls section', { exact: true })
  await expect(formControls).toHaveAttribute('aria-expanded', 'true')
  await formControls.click()
  await expect(formControls).toHaveAttribute('aria-expanded', 'false')

  for (const [path, heading] of componentRoutes) {
    const surface = await openPreview(path, heading)
    if (path === '/components/month-calendar') {
      await expect(surface.getByRole('combobox', { name: 'Month', exact: true })).toHaveCount(1)
      await expect(surface.getByRole('combobox', { name: 'Year', exact: true })).toHaveCount(1)
    } else if (path === '/components/select') {
      await expect(surface.locator('select')).toHaveCount(1)
      await expect(surface.locator('select')).toBeHidden()
      await expect(surface.locator('select')).toBeDisabled()
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
  await expect(page.getByLabel('Toggle Components section', { exact: true })).toHaveAttribute('aria-expanded', 'true')
  await expect(formControls).toHaveAttribute('aria-expanded', 'false')
  await expect(page.locator('#nav-components-select')).toHaveAttribute('data-selected', 'true')

  const resolvedBackground = (root: Locator, variable: string) =>
    resolvedVariableColor(root.first(), 'background-color', variable)

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
    document.querySelector('[data-docs-layout="gallery"]')!.addEventListener('click', event => {
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
  const docsRoot = page.locator('body')
  const expectDocumentationShellSurfaces = async () => {
    const background = await resolvedBackground(docsRoot, '--fve-background')
    const sideNavigation = page.locator('[data-docs-side-nav="true"]')
    const main = page.locator('main#main-content')
    expect(await sideNavigation.evaluate(element => getComputedStyle(element).backgroundColor)).toBe(background)
    expect(await main.evaluate(element => getComputedStyle(element).backgroundColor)).toBe(background)

    const navigationItem = page.locator('#nav-home')
    const restingBackground = await navigationItem.evaluate(element => getComputedStyle(element).backgroundColor)
    await navigationItem.hover()
    const expectedHover = await resolvedBackground(docsRoot, '--fve-surface-hover')
    await expect.poll(() => navigationItem.evaluate(element => getComputedStyle(element).backgroundColor)).toBe(expectedHover)
    expect(expectedHover).not.toBe(restingBackground)
    await page.locator('main h1').first().hover()
  }

  const lightBackground = await resolvedBackground(buttonSurface, '--fve-background')
  const lightDocsBackground = await resolvedBackground(docsRoot, '--fve-background')
  const lightBrand = await resolvedBackground(buttonSurface, '--fve-brand-solid')
  const lightDocsAccent = await resolvedBackground(docsRoot, '--fve-brand-solid')
  expect(lightBackground).toBe(lightDocsBackground)
  expect(lightBrand).toBe(lightDocsAccent)
  await expectDocumentationShellSurfaces()

  await page.getByRole('button', { name: 'Choose color theme' }).click()
  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  const darkBackground = await resolvedBackground(buttonSurface, '--fve-background')
  const darkDocsBackground = await resolvedBackground(docsRoot, '--fve-background')
  const darkBrand = await resolvedBackground(buttonSurface, '--fve-brand-solid')
  const darkDocsAccent = await resolvedBackground(docsRoot, '--fve-brand-solid')
  expect(darkBackground).toBe(darkDocsBackground)
  expect(darkBackground).not.toBe(lightBackground)
  expect(darkBrand).toBe(darkDocsAccent)
  await expectDocumentationShellSurfaces()

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

  const iconButtonSurface = page.locator('#components-button-icon-primary-panel-preview')
  const addAccount = iconButtonSurface.getByRole('button', { name: 'Add account' })
  await addAccount.focus()
  await expect(addAccount).toBeFocused()
  await expect(addAccount.locator('[aria-hidden="true"]')).toBeVisible()
  for (const control of [addAccount, page.locator('#components-button-icon-variants-panel-preview').getByRole('button', { name: 'Solid refresh' })]) {
    const { hover, active } = await pressedBackgrounds(control)
    expect(active, `${await control.getAttribute('aria-label')} active background`).not.toBe(hover)
  }
  const iconCountBeforeEnabledActivation = await activationCount(iconButtonSurface, 'button-icon-activation-count')
  await addAccount.click()
  await expect.poll(() => activationCount(iconButtonSurface, 'button-icon-activation-count')).toBe(iconCountBeforeEnabledActivation + 1)

  const refreshingAccounts = page.locator('#components-button-icon-pending-panel-preview').getByRole('button', { name: 'Refreshing accounts' })
  const disabledRemoveAccount = page.locator('#components-button-icon-disabled-panel-preview').getByRole('button', { name: 'Remove account' })
  await expect(refreshingAccounts).toBeDisabled()
  await expect(refreshingAccounts).toHaveAttribute('aria-busy', 'true')
  await expect(disabledRemoveAccount).toBeDisabled()
  await expectUnavailableActivationPrevention(refreshingAccounts)
  await expectUnavailableActivationPrevention(disabledRemoveAccount)

  const badgeSurface = await openPreview('/components/badge', 'Badge')
  await expect(badgeSurface.getByText('Internal', { exact: true })).toHaveCount(1)
  await expect(badgeSurface.getByText('Internal', { exact: true })).toBeVisible()

  const loadingSurface = await openPreview('/components/loading-indicator', 'Loading indicator')
  await expect(loadingSurface.getByRole('status')).toHaveCount(2)
  await expect(loadingSurface.getByText('Loading account balances')).toHaveClass(/sr-only/)
  await expect(loadingSurface.getByText('Refreshing transactions')).toBeVisible()

  const emptyStateSurface = await openPreview('/components/empty-state', 'Empty state')
  await expect(emptyStateSurface.getByText('No accounts yet', { exact: true }).first()).toBeVisible()
  await expect(emptyStateSurface.getByRole('link', { name: 'Create account' })).toHaveAttribute('href', '/components/page-examples/account-management?destination=ledger-create-account')

  const simpleTables = await openPreview('/components/table', 'Table')
  await expect(simpleTables.locator('th[aria-sort] a')).toHaveCount(1)
  await expect(simpleTables.getByRole('link', { name: 'Operating checking', exact: true })).toBeVisible()
  await expect(simpleTables.first().getByRole('columnheader')).toHaveText(['Name', 'Email', 'Role'])
  const descriptionListSurface = await openPreview('/components/description-list', 'Description list')
  await expect(descriptionListSurface.locator('dl')).toHaveCount(4)
  await expect(descriptionListSurface.first().locator('dt')).toHaveText(['Account type', 'Currency'])
  await expect(page.locator('#components-description-list-panel-preview dt')).toHaveText(['Type', 'Commodity', 'Parent account', 'Source', 'Status', 'Balance'])
  await expect(descriptionListSurface.locator('dd')).toHaveCount(18)
  await expect(descriptionListSurface.getByText('Includes cleared entries through today.')).toBeVisible()

  const metricSurface = await openPreview('/components/metric', 'Metric')
  await expect(metricSurface.getByText('Available balance', { exact: true }).first()).toBeVisible()
  await expect(metricSurface.getByText('$42,800', { exact: true }).first()).toBeVisible()
  await expect(metricSurface.getByText('Trend: ', { exact: true })).toHaveClass(/sr-only/)
  const availableBalanceLabel = metricSurface.getByText('Available balance', { exact: true }).last()
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

  await openPreview('/components/pagination', 'Pagination')
  const paginationSurface = page.locator('#components-pagination-panel-preview')
  const accountPages = paginationSurface.getByRole('navigation', { name: 'Accounts pages' })
  await expect(accountPages.getByText('Showing 26–50 of 184 accounts')).toBeVisible()
  await expect(accountPages.getByText('2', { exact: true })).toHaveAttribute('aria-current', 'page')
  const nextPage = accountPages.getByText('Next', { exact: true })
  await expect(accountPages.getByRole('link', { name: 'Page 3' })).toHaveAttribute('href', '/components/pagination/page?page=3')
  await expect(nextPage).toHaveAttribute('href', '/components/pagination/page?page=3')
  await page.evaluate(() => { (window as any).__paginationDocumentMarker = true })
  await nextPage.click()
  expect(await page.evaluate(() => (window as any).__paginationDocumentMarker)).toBe(true)
  await expect(page).toHaveURL('/components/pagination')
  await expect(accountPages.getByText('Showing 26–50 of 184 accounts')).toBeVisible()
  await expect(accountPages.getByText('2', { exact: true })).toHaveAttribute('aria-current', 'page')
  await accountPages.getByRole('link', { name: 'Page 8' }).click()
  await expect(page).toHaveURL('/components/pagination')
  await expect(accountPages.getByText('2', { exact: true })).toHaveAttribute('aria-current', 'page')

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
  await expect(compactRows).toHaveAttribute('aria-pressed', 'true')
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
    test(`${group.name} remain accessible`, async ({ page }) => {
      await page.route('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22', route =>
        route.fulfill({ status: 200, contentType: 'text/javascript', body: '' }),
      )
      const browserErrors = captureBrowserErrors(page)

      for (const path of group.paths) {
        const response = await page.goto(path, { waitUntil: 'domcontentloaded' })
        expect(response?.status(), `${path} status`).toBe(200)
        await expect(page.getByRole('main')).toBeVisible()
        const results = await new AxeBuilder({ page })
          .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
          .analyze()
        expect(results.violations, path).toEqual([])
      }

      expect(browserErrors).toEqual([])
    })
  }
})

test('Components examples preserve documentation framing without clipping anchored popups', async ({ page }) => {
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
      const framedSurface = name === 'Preview' ? panel.locator('[data-docs-preview-frame="true"]') : panel
      await expect(framedSurface).toHaveCSS('border-bottom-left-radius', '12px')
      await expect(framedSurface).toHaveCSS('border-bottom-right-radius', '12px')
    }
  }

  await page.goto('/components/dropdown-menu', { waitUntil: 'domcontentloaded' })
  await page.waitForFunction(() => (window as any).fsharpDocsCode?.loading)
  await page.evaluate(() => (window as any).fsharpDocsCode.loading)
  const menuExample = page.locator('#components-dropdown-menu-advanced')
  await menuExample.getByRole('tab', { name: 'Preview' }).click()
  await expect(menuExample).toHaveCSS('overflow', 'visible')
  await menuExample.getByRole('button', { name: 'Actions', exact: true }).click()
  await expect(menuExample.getByRole('menu', { name: 'Actions', exact: true })).toBeVisible()

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

  await openPreview()
  const gallery = page.locator('[data-docs-layout="gallery"]')
  const defaultSurface = page.locator('#components-tabs-panel-preview .fve-components')
  const underlinedSurface = page.locator('#components-tabs-underlined-panel-preview .fve-components')
  const segmented = defaultSurface.getByRole('tablist', { name: 'Example format' })
  const codeTab = segmented.getByRole('tab', { name: 'Code' })
  const previewTab = segmented.getByRole('tab', { name: 'Preview' })
  const codePanel = defaultSurface.getByRole('tabpanel', { name: 'Code' })
  const previewPanel = defaultSurface.getByRole('tabpanel', { name: 'Preview' })
  await expect(codeTab).toHaveAttribute('aria-selected', 'true')
  await expect(codeTab).toHaveAttribute('tabindex', '0')
  await expect(codePanel).toBeVisible()
  await expect(previewPanel).toBeHidden()

  await codeTab.focus()
  await page.keyboard.press('ArrowLeft')
  await expect(previewTab).toBeFocused()
  await expect(previewTab).toHaveAttribute('aria-selected', 'true')
  await expect(previewPanel).toBeVisible()
  await expect(codePanel).toBeHidden()
  await page.keyboard.press('Tab')
  await expect(previewPanel).toBeFocused()
  await previewTab.focus()
  await page.keyboard.press('Home')
  await expect(codeTab).toBeFocused()
  await expect(codeTab).toHaveAttribute('aria-selected', 'true')
  await page.keyboard.press('End')
  await expect(previewTab).toBeFocused()
  await page.keyboard.press('ArrowRight')
  await expect(codeTab).toBeFocused()
  await page.keyboard.press('ArrowRight')
  await expect(previewTab).toBeFocused()
  await previewTab.click()

  const underlined = underlinedSurface.getByRole('tablist', { name: 'Account sections' })
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
  await expect(underlinedSurface.getByRole('tabpanel', { name: 'Activity' })).toBeVisible()
  await expect(previewTab).toHaveAttribute('aria-selected', 'true')

  const refresh = underlinedSurface.getByRole('button', { name: 'Refresh activity' })
  await refresh.click()
  await expect(underlinedSurface.getByRole('status')).toHaveText('Review refreshed')
  await expect(underlinedSurface.getByText('Updated just now.')).toBeVisible()
  await expect(underlined.getByRole('tab', { name: 'Activity' })).toHaveAttribute('aria-selected', 'true')
  await expect(underlinedSurface.getByRole('button', { name: 'Refresh activity' })).toBeFocused()
  await expect(previewTab).toHaveAttribute('aria-selected', 'true')

  const ids = await gallery.locator('[id]').evaluateAll(elements => elements.map(element => element.id))
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
  await openPreview()
  const mobileSegmented = page.locator('#components-tabs-panel-preview .fve-components').getByRole('tablist', { name: 'Example format' })
  const mobileUnderlined = page.locator('#components-tabs-underlined-panel-preview .fve-components').getByRole('tablist', { name: 'Account sections' })
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

  const confirmationSurface = dialogSurface
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
  expect(confirmationAccessibility.violations, 'composed confirmation Dialog').toEqual([])

  let confirmationRequests = 0
  page.on('request', request => {
    if (new URL(request.url()).pathname === '/components/dialogs/confirm') confirmationRequests++
  })
  const confirmationResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/components/dialogs/confirm')
  await confirmDeletion.click()
  await expect(confirmDeletion).toBeDisabled()
  await expect(confirmDeletion).toHaveAttribute('aria-busy', 'true')
  await expect(confirmation.locator('form')).toHaveAttribute('aria-busy', 'true')
  await expect(confirmation.getByRole('status')).toHaveText('Confirmation in progress.')
  await page.keyboard.press('Escape')
  await expect(confirmation).toBeVisible()
  await attachScreenshot('components-dialog-confirmation-desktop-dark-pending')
  await confirmationResponse
  await expect(confirmation.getByRole('alert')).toHaveText('Operating cannot be deleted while posted entries are assigned to its open period.')
  await expect(confirmation.locator('form')).toHaveAttribute('aria-busy', 'false')
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
  await page.keyboard.press('Escape')
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
    ['/scripts/datastar.1.0.4.js', 'Datastar v1.0.4'],
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
  const input = dialog.getByRole('combobox')
  await expect(input).toBeFocused()
  await input.fill('Rendering')
  const visible = dialog.getByRole('option')
  await expect(visible).not.toHaveCount(0)
  await expect(visible.first()).toHaveAttribute('href', /guides\/rendering/)
})

test('getting started shows the product logo and Tailwind Sky accents', async ({ page }) => {
  await page.goto('/', { waitUntil: 'domcontentloaded' })
  const logo = page.locator('.docs-home-logo img')
  await expect(logo).toBeVisible()
  await expect(logo).toHaveAttribute('src', '/logo.svg')
  await expect(page.getByRole('heading', { level: 1, name: 'FSharp.ViewEngine' })).toHaveCount(1)
  const accent = await page.locator('html').evaluate(element => getComputedStyle(element).getPropertyValue('--fve-brand-ring').trim())
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
  await expect(page.locator('[data-docs-nav-link="true"]').first()).toHaveCSS('font-size', '14px')

  const paragraph = page.locator('[data-docs-section-content="true"] > p').first()
  await expect(paragraph).toBeVisible()
  await expect(paragraph).toHaveCSS('font-size', '16px')
  expect(await paragraph.evaluate(element => getComputedStyle(element).fontFamily)).toContain('Noto Sans')

  const code = page.locator('code[data-docs-copy-source="true"]').first()
  await expect(code).toBeVisible()
  await expect(code).toHaveCSS('font-size', '14px')
  expect(await code.evaluate(element => getComputedStyle(element).fontFamily)).toContain('Noto Sans Mono')
  expect(Number.parseFloat(await code.evaluate(element => getComputedStyle(element).lineHeight))).toBeCloseTo(21.7, 1)
  const copy = page.getByRole('button', { name: 'Copy code' }).first()
  await expect(copy).toHaveCSS('font-size', '12px')
  const copyInset = await copy.evaluate(button => {
    const boundary = button.closest('pre') ?? button.parentElement?.querySelector('pre')
    if (!boundary) throw new Error('Copy button has no code boundary')
    const buttonBox = button.getBoundingClientRect()
    const boundaryBox = boundary.getBoundingClientRect()
    return { top: buttonBox.top - boundaryBox.top, right: boundaryBox.right - buttonBox.right }
  })
  expect(copyInset.top).toBeGreaterThanOrEqual(11)
  expect(copyInset.right).toBeGreaterThanOrEqual(11)

  await page.goto('/docs/previews/tables', { waitUntil: 'domcontentloaded' })
  await expect(page.locator('[data-docs-section-content="true"] table th').first()).toHaveCSS('font-size', '12px')
  await expect(page.locator('[data-docs-section-content="true"] table td').first()).toHaveCSS('font-size', '14px')

  await page.goto('/custom', { waitUntil: 'domcontentloaded' })
  await expect(page.locator('[data-docs-toc-rail="true"] > div > div').first()).toHaveCSS('font-size', '12px')
})

test('color mode selector supports persistence, keyboard navigation, and system changes', crossBrowser, async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'light' })
  await page.goto('/components/code-block', { waitUntil: 'domcontentloaded' })
  await page.evaluate(() => localStorage.removeItem('fsharp-viewengine-docs-navigation-color-mode'))
  await page.reload({ waitUntil: 'domcontentloaded' })

  await page.locator('[data-docs-example]').first().getByRole('tab', { name: 'Code' }).click()
  const codeSurface = page.locator('pre:visible:has(code[data-docs-copy-source="true"])').first()
  await expect(codeSurface).toBeVisible()
  const lightCodeBackground = await codeSurface.evaluate(element => getComputedStyle(element).backgroundColor)
  const lightCodeText = await codeSurface.evaluate(element => getComputedStyle(element).color)
  expect(lightCodeBackground).toBe(await resolvedVariableColor(page.locator('body'), 'background-color', '--fve-docs-code-surface'))
  expect(lightCodeBackground).not.toBe(await page.locator('[data-docs-side-nav="true"]').evaluate(element => getComputedStyle(element).backgroundColor))
  expect(lightCodeText).toBe(await resolvedVariableColor(page.locator('body'), 'color', '--fve-text'))

  const trigger = page.getByRole('button', { name: 'Choose color theme' })
  await trigger.click()
  const menu = page.getByRole('menu', { name: 'Choose color theme' })
  await expect(menu).toBeVisible()
  await expect(page.getByRole('menuitemradio', { name: 'System' })).toHaveAttribute('aria-checked', 'true')

  await page.getByRole('menuitemradio', { name: 'Dark' }).click()
  await expect(page.locator('html')).toHaveClass(/dark/)
  const darkCodeBackground = await codeSurface.evaluate(element => getComputedStyle(element).backgroundColor)
  const darkCodeText = await codeSurface.evaluate(element => getComputedStyle(element).color)
  expect(darkCodeBackground).toBe(await resolvedVariableColor(page.locator('body'), 'background-color', '--fve-docs-code-surface'))
  expect(darkCodeBackground).not.toBe(await page.locator('[data-docs-side-nav="true"]').evaluate(element => getComputedStyle(element).backgroundColor))
  expect(darkCodeText).toBe(await resolvedVariableColor(page.locator('body'), 'color', '--fve-text'))
  expect(darkCodeBackground).not.toBe(lightCodeBackground)
  expect(darkCodeText).not.toBe(lightCodeText)
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
  await expect(drawer.getByRole('link', { name: 'Examples', exact: true })).toBeFocused()
  await page.keyboard.press('Tab')
  await expect(close).toBeFocused()

  await page.keyboard.press('Escape')
  await expect(drawer).toBeHidden()
  await expect(opener).toBeFocused()
  await expect(page.locator('#page-content')).not.toHaveAttribute('inert', '')

  await opener.click()
  await page.locator('[data-docs-overlay="true"]').click({ position: { x: 380, y: 400 } })
  await expect(drawer).toBeHidden()
  await expect(opener).toBeFocused()

  await opener.click()
  await close.click()
  await expect(drawer).toBeHidden()
  await expect(opener).toBeFocused()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)

  await opener.click()
  await drawer.getByRole('link', { name: 'Installation', exact: true }).click()
  await expect(page.getByRole('heading', { level: 1, name: 'Installation', exact: true })).toBeVisible()
  await expect(drawer).toBeHidden()
  await expect(page.locator('#page-content')).not.toHaveAttribute('inert', '')
})

test('component pages lead with an example, then installation, usage, variants, and API navigation @cross-browser', crossBrowser, async ({ page }) => {
  await page.setViewportSize({ width: 1512, height: 982 })
  await gotoAfterDocsAssetSettlement(page, '/components/button', 'domcontentloaded')

  await expect(page.getByRole('heading', { level: 1, name: 'Button', exact: true })).toBeVisible()
  await expect(page.locator('main h1 + p')).toHaveText('Trigger actions and submit forms with explicit text, icon, color, variant, size, and pending content.')

  const examples = page.locator('[data-docs-example="true"]')
  const lead = examples.first()
  await expect(lead.getByRole('heading', { level: 2, name: 'Default' })).toHaveClass(/sr-only/)
  await expect(lead.getByRole('tab', { name: 'Preview', exact: true })).toHaveAttribute('aria-selected', 'true')
  await expect(page.locator('#installation')).toContainText('dotnet fve add button --config src/Acme.Components/fve.json')
  await expect(page.locator('#usage code[data-docs-copy-source="true"]')).toContainText('Button.create (ButtonContent.Text "Continue")')

  const documentOrder = await page.locator('[data-docs-example="true"], #installation, #usage, #api-reference').evaluateAll(elements =>
    elements.map(element => element.id || element.getAttribute('data-docs-example-title') || element.querySelector('[data-docs-example-title]')?.textContent?.trim()),
  )
  expect(documentOrder.slice(0, 3)).toEqual(['Default', 'installation', 'usage'])
  expect(documentOrder.at(-1)).toBe('api-reference')

  const toc = page.locator('[data-docs-toc-rail="true"]')
  await expect(toc.getByRole('link', { name: 'Installation', exact: true })).toHaveAttribute('href', '#installation')
  await expect(toc.getByRole('link', { name: 'Usage', exact: true })).toHaveAttribute('href', '#usage')
  await expect(toc.getByRole('link', { name: 'API reference', exact: true })).toHaveAttribute('href', '#api-reference')
  await expect(toc.getByRole('link', { name: 'Default', exact: true })).toHaveCount(0)

  const navigationGutters = await page.locator('#nav-components-button').evaluate(element => {
    const item = element.getBoundingClientRect()
    const navigation = element.closest('[data-docs-side-nav="true"]')!.getBoundingClientRect()
    return { left: item.left - navigation.left, right: navigation.right - item.right }
  })
  expect(navigationGutters.left).toBeGreaterThanOrEqual(12)
  expect(navigationGutters.right).toBeGreaterThanOrEqual(12)

  await toc.getByRole('link', { name: 'Usage', exact: true }).click()
  await expect(page).toHaveURL('/components/button#usage')
  await expect(page.locator('#usage')).toBeFocused()

  await page.locator('#nav-components-button-group').click()
  await expect(page).toHaveURL('/components/button-group')
  await expect(page.getByRole('heading', { level: 1, name: 'Button group', exact: true })).toBeVisible()
  await expect(page.locator('[data-docs-toc-rail="true"] a[href="#installation"]')).toBeVisible()
  await expect(page.locator('[data-docs-toc-rail="true"] a[href="#api-reference"]')).toBeVisible()

  await page.setViewportSize({ width: 390, height: 844 })
  await page.locator('html').evaluate(element => { element.style.fontSize = '200%' })
  await expect(page.getByRole('group', { name: 'On this page', exact: true })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})

test('desktop table of contents tracks the visible section and survives Docs navigation', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto('/custom', { waitUntil: 'domcontentloaded' })

  const main = page.locator('[data-docs-main="true"]')
  const target = page.locator('#shoelace-example')
  await main.evaluate(element => element.scrollTo({ top: element.scrollHeight, behavior: 'instant' }))
  await expect(page.locator('[data-docs-toc-rail="true"] a[href="#shoelace-example"]')).toHaveAttribute('aria-current', 'location')

  await page.locator('#nav-accessibility').click()
  await expect(page).toHaveURL('/guides/accessibility')
  await expect(page.locator('[data-docs-toc-rail="true"] a[href="#overview"]')).toHaveAttribute('aria-current', 'location')
})

test('desktop table of contents follows the final visible section after font loading and text reflow', async ({ page }) => {
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

  const main = page.locator('[data-docs-main="true"]')
  const finalSectionLink = page.locator('[data-docs-toc-rail="true"] a[href="#shoelace-example"]')
  await expect(page.locator('[data-docs-toc-rail="true"] a[aria-current="location"]')).toHaveCount(1)
  const fallbackHeight = await main.evaluate(element => element.scrollHeight)
  await main.evaluate(element => element.scrollTo({ top: element.scrollHeight, behavior: 'instant' }))
  await expect(finalSectionLink).toHaveAttribute('aria-current', 'location')

  releaseFont()
  await page.evaluate(() => document.fonts.ready)
  // Font metrics alone need not change wrapping at every rail width. Exercise a real reflow.
  await page.locator('html').evaluate(element => { element.style.fontSize = '110%' })
  await expect.poll(() => main.evaluate(element => element.scrollHeight)).not.toBe(fallbackHeight)
  await main.evaluate(element => element.scrollTo({ top: element.scrollHeight - element.clientHeight - 70, behavior: 'instant' }))
  await expect(page.locator('#shoelace-example')).toBeInViewport()
  await main.dispatchEvent('scroll')

  await expect(finalSectionLink).toHaveAttribute('aria-current', 'location')
  expect(browserErrors).toEqual([])
})

test('mobile table of contents is a compact keyboard-operable disclosure', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/custom', { waitUntil: 'domcontentloaded' })

  const toc = page.getByRole('group', { name: 'On this page', exact: true })
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

  const main = page.locator('[data-docs-main="true"]')
  const target = page.locator('#shoelace-example')
  await page.locator('[data-docs-toc-rail="true"] a[href="#shoelace-example"]').click()

  await expect(page).toHaveURL('/custom#shoelace-example')
  await expect.poll(() => main.evaluate(element => element.scrollTop)).toBeGreaterThan(0)
  await expect.poll(async () => (await target.boundingBox())!.y).toBeLessThan(1000)
})

test('Docs navigation scrolls content to top and highlights morphed code', crossBrowser, async ({ page }) => {
  await page.goto('/components/button', { waitUntil: 'domcontentloaded' })
  const main = page.locator('[data-docs-main="true"]')
  await main.evaluate(element => element.scrollTo({ top: element.scrollHeight }))
  expect(await main.evaluate(element => element.scrollTop)).toBeGreaterThan(0)

  const navigation = page.getByRole('complementary', { name: 'Documentation navigation' })
  await navigation.getByRole('link', { name: 'Button group', exact: true }).click()
  await expect(page).toHaveURL('/components/button-group')
  await expect(page.getByRole('heading', { level: 1, name: 'Button group' })).toBeVisible()
  await expect.poll(() => main.evaluate(element => element.scrollTop)).toBe(0)
  const keyword = page.locator('#api-reference pre:has(code[data-docs-copy-source="true"]) .token.keyword').first()
  await expect(keyword).toBeVisible()
  expect(await keyword.evaluate(element => getComputedStyle(element).color)).toBe(
    await resolvedVariableColor(page.locator('body'), 'color', '--color-red-700'),
  )
})

test('Docs navigation loads Prism dependencies before highlighting a code page', crossBrowser, async ({ page }) => {
  const pageErrors: string[] = []
  page.on('pageerror', error => pageErrors.push(error.message))

  await page.goto('/docs/components/content', { waitUntil: 'domcontentloaded' })
  await page.locator('#nav-home').click()

  await expect(page).toHaveURL('/')
  const keyword = page.locator('pre:has(code[data-docs-copy-source="true"]) .token.keyword').first()
  await expect(keyword).toBeVisible()
  expect(await keyword.evaluate(element => getComputedStyle(element).color)).toBe(
    await resolvedVariableColor(page.locator('body'), 'color', '--color-red-700'),
  )
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

  await expect(page.locator('pre:has(code[data-docs-copy-source="true"]) .token.keyword').first()).toBeVisible()
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
  const block = page.locator('[data-docs-copyable-code="true"]').first()
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
  await expect(example.locator('[data-docs-example-toolbar="true"] button[aria-label^="Copy "]')).toHaveCount(0)
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
  await page.goto('/getting-started/first-view', { waitUntil: 'domcontentloaded' })
  const next = page.getByRole('navigation', { name: 'Page navigation' }).getByRole('link', { name: /Next/ })
  await next.click()
  await expect(page).toHaveURL('/guides/elements-and-attributes')
  await expect(page.getByRole('heading', { level: 1, name: 'Elements and attributes' })).toBeVisible()
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
  expect(await autocompleteSurface.evaluate(element => getComputedStyle(element).backgroundColor)).toBe(
    await resolvedVariableColor(page.locator('body'), 'background-color', '--fve-surface-subtle'),
  )
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
  expect(await securityTab.evaluate(element => getComputedStyle(element).borderBottomColor)).toBe(
    await resolvedVariableColor(page.locator('body'), 'color', '--fve-brand-ring'),
  )

  await page.evaluate(() => document.documentElement.classList.add('dark'))
  expect(await autocompleteSurface.evaluate(element => getComputedStyle(element).backgroundColor)).toBe(
    await resolvedVariableColor(page.locator('body'), 'background-color', '--fve-surface-subtle'),
  )
  expect(await autocomplete.locator('input').evaluate(element => getComputedStyle(element).backgroundColor)).toBe(
    await resolvedVariableColor(page.locator('body'), 'background-color', '--fve-surface'),
  )
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
    let renderMermaid: ((root: Document | Element, pendingOnly?: boolean) => Promise<void>) | undefined
    Object.defineProperty(window, 'renderMermaid', {
      configurable: true,
      get: () => renderMermaid,
      set: value => {
        renderMermaid = (root, pendingOnly) =>
          root instanceof Element && root.matches('.mermaid') ? Promise.resolve() : value(root, pendingOnly)
      },
    })
  })

  await page.goto('/docs/components/diagrams', { waitUntil: 'domcontentloaded' })

  const diagram = page.locator('main [data-docs-diagram="true"]').first()
  await expect(diagram).toHaveAttribute('data-mermaid-state', 'rendered')
  await expect(diagram.locator('svg')).toBeVisible()
  await expectNoRawMermaid(diagram)
})

test('diagrams render directly without exposing Mermaid source', async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/docs/components/diagrams', { waitUntil: 'domcontentloaded' })

  const diagram = page.locator('main [data-docs-diagram="true"]').first()
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
  const diagram = page.locator('main [data-docs-diagram="true"]').first()
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
  const diagram = page.locator('main [data-docs-diagram="true"]').first()
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
  const diagram = page.locator('main [data-docs-diagram="true"]').first()
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

test('pending diagrams render after Docs navigation through component-local initialization', crossBrowser, async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/components/code-block', { waitUntil: 'domcontentloaded' })
  await page.evaluate(() => {
    const docsWindow = window as typeof window & {
      renderMermaid?: (root: Document | Element, pendingOnly?: boolean) => Promise<void>
      mermaidRenderHosts?: string[]
    }
    const renderMermaid = docsWindow.renderMermaid
    docsWindow.mermaidRenderHosts = []
    docsWindow.renderMermaid = (root, pendingOnly) => {
      const fromDataInit = root instanceof Element && root.matches('.mermaid')
      docsWindow.mermaidRenderHosts?.push(fromDataInit ? 'data-init' : root instanceof Element ? root.id : 'document')
      return renderMermaid?.(root, pendingOnly) ?? Promise.resolve()
    }
  })

  await page.locator('#nav-components-mermaid').click()
  await expect(page).toHaveURL('/components/mermaid')

  const diagram = page.locator('main [data-docs-diagram="true"]').first()
  await expect(diagram).toHaveAttribute('data-mermaid-state', 'rendered')
  await expect(diagram.locator('svg')).toBeVisible()
  await expect.poll(() => page.evaluate(() => (window as typeof window & { mermaidRenderHosts?: string[] }).mermaidRenderHosts)).toContain('data-init')
  expect(await page.evaluate(() => (window as typeof window & { mermaidRenderHosts?: string[] }).mermaidRenderHosts)).not.toContain('page-content')

  await page.evaluate(async () => {
    const diagram = document.querySelector<HTMLElement>('main [data-docs-diagram="true"]')!
    diagram.dataset.mermaidState = 'rendered'
    diagram.dataset.mermaidRenderedSource = 'stale-source'
    diagram.replaceChildren()
    await (window as typeof window & { renderMermaid?: (element: Element, pendingOnly?: boolean) => Promise<void> }).renderMermaid?.(document.getElementById('page-content')!, true)
  })
  await expect(diagram.locator('svg')).toBeVisible()
  await expectNoRawMermaid(diagram)
  expect(browserErrors).toEqual([])
})

test('queued pending rendering discards stale in-flight Mermaid results for a reused host', crossBrowser, async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/docs/components/diagrams', { waitUntil: 'domcontentloaded' })
  const diagram = page.locator('main [data-docs-diagram="true"]').first()
  await expect(diagram.locator('svg')).toBeVisible()

  const expectedSource = await diagram.evaluate(async element => {
    const docsWindow = window as typeof window & {
      mermaid: { render: (id: string, source: string) => Promise<{ svg: string; bindFunctions?: (node: Element) => void }> }
      renderMermaid?: (element: Element, pendingOnly?: boolean) => Promise<void>
    }
    const firstSource = 'flowchart LR\n  Stale --> Result'
    const nextSource = 'flowchart LR\n  Current --> Result'
    const originalRender = docsWindow.mermaid.render
    let releaseFirst: (() => void) | undefined
    let markFirstStarted: (() => void) | undefined
    const firstGate = new Promise<void>(resolve => { releaseFirst = resolve })
    const firstStarted = new Promise<void>(resolve => { markFirstStarted = resolve })
    let first = true

    docsWindow.mermaid.render = async (_id, source) => {
      if (first) {
        first = false
        markFirstStarted?.()
        await firstGate
      }
      return { svg: `<svg xmlns="http://www.w3.org/2000/svg" data-test-mermaid-source="${encodeURIComponent(source)}"></svg>` }
    }

    try {
      const node = element as HTMLElement
      node.dataset.mermaidSource = firstSource
      node.dataset.mermaidState = 'pending'
      const firstRender = docsWindow.renderMermaid?.(node, true) ?? Promise.resolve()
      await firstStarted
      node.dataset.mermaidSource = nextSource
      const nextRender = docsWindow.renderMermaid?.(node, true) ?? Promise.resolve()
      releaseFirst?.()
      await Promise.all([firstRender, nextRender])
      return nextSource
    } finally {
      docsWindow.mermaid.render = originalRender
    }
  })

  await expect(diagram).toHaveAttribute('data-mermaid-state', 'rendered')
  await expect(diagram).toHaveAttribute('data-mermaid-rendered-source', expectedSource)
  await expect(diagram.locator('svg')).toHaveAttribute('data-test-mermaid-source', encodeURIComponent(expectedSource))
  expect(browserErrors).toEqual([])
})

test('diagrams render after Docs navigation and light-dark rerenders', crossBrowser, async ({ page }) => {
  const browserErrors = captureBrowserErrors(page)
  await page.goto('/components/code-block', { waitUntil: 'domcontentloaded' })
  await page.locator('#nav-components-mermaid').click()
  await expect(page).toHaveURL('/components/mermaid')

  const diagram = page.locator('main [data-docs-diagram="true"]').first()
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
  await page.goto('/components/mermaid', { waitUntil: 'domcontentloaded' })
  const example = page.locator('[data-docs-example="true"]').first()
  const diagram = example.locator('.mermaid svg')
  await expect(diagram).toBeVisible()
  await example.getByRole('tab', { name: 'Code', exact: true }).click()
  await expect(diagram).toBeHidden()
  await example.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(diagram).toBeVisible()
  await expect.poll(() => diagram.getAttribute('viewBox')).not.toBe('-8 -8 16 16')
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

test('code-free Examples index settles without starting Prism', crossBrowser, async ({ page }) => {
  const prismRequests: string[] = []
  page.on('request', request => {
    if (/\/scripts\/prism|\/css\/prism/.test(new URL(request.url()).pathname)) prismRequests.push(request.url())
  })
  for (const path of ['/examples']) {
    await gotoAfterDocsAssetSettlement(page, path)
    await expect(page.locator('code[class*="language-"]')).toHaveCount(0)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
  }
  expect(prismRequests.filter(url => /\/scripts\/prism/.test(new URL(url).pathname))).toEqual([])
  expect(prismRequests.some(url => new URL(url).pathname === '/css/prism-tomorrow.1.29.0.min.css')).toBe(true)
})

test('representative component documents retain one page heading and unique IDs', async ({ page, request }) => {
  for (const path of await docsCatalogPaths(request)) {
    await page.goto(path, { waitUntil: 'domcontentloaded' })
    await expect(page.locator('#page-content h1')).toHaveCount(1)
    const duplicateIds = await page.evaluate(() => {
      const counts = new Map<string, number>()
      for (const element of document.querySelectorAll<HTMLElement>('[id]')) {
        counts.set(element.id, (counts.get(element.id) ?? 0) + 1)
      }
      return Array.from(counts.entries()).filter(([, count]) => count > 1)
    })
    expect(duplicateIds, path).toEqual([])
  }
})

test('representative component examples default to complete styled previews', async ({ page, request }) => {
  const browserErrors = captureBrowserErrors(page)
  let reviewedPreviews = 0

  for (const path of await docsCatalogPaths(request)) {
    await page.goto(path, { waitUntil: 'domcontentloaded' })
    reviewedPreviews += await page.locator('[data-docs-example="true"] [role="tab"][aria-selected="true"]').count()
    const examples = page.locator('[data-docs-example="true"]')
    expect(await examples.count(), path).toBeGreaterThan(0)
    for (const example of await examples.all()) {
      const previewTab = example.locator(':scope > [data-docs-example-toolbar="true"]').getByRole('tab', { name: 'Preview' })
      const panelId = await previewTab.getAttribute('aria-controls')
      expect(panelId).toBeTruthy()
      await expect(previewTab).toHaveAttribute('aria-selected', 'true')
      const preview = page.locator(`#${panelId}`)
      await expect(preview).toBeVisible()
      const iframe = preview.locator('iframe')
      if (await iframe.count()) {
        const frame = iframe.contentFrame()
        await expect(frame.locator('body')).toHaveClass(/fve-components/)
        expect(await frame.locator('style, link[rel="stylesheet"]').count(), path).toBeGreaterThan(0)
        expect(await frame.locator('html').evaluate(element => element.scrollWidth <= element.clientWidth), path).toBe(true)
      } else {
        expect(await preview.evaluate(element => element.scrollWidth <= element.clientWidth), path).toBe(true)
      }
    }
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), path).toBe(true)
  }
  expect(reviewedPreviews).toBeGreaterThan(0)
  expect(browserErrors).toEqual([])
})

test('benchmark comparison remains legible in light and dark themes', async ({ page }) => {
  for (const theme of ['light', 'dark'] as const) {
    await page.addInitScript(selected => localStorage.setItem('fsharp-viewengine-docs-navigation-color-mode', selected), theme)
    await page.goto('/benchmarks', { waitUntil: 'domcontentloaded' })
    const chart = page.locator('.docs-comparison-chart')
    await expect(chart).toBeVisible()
    const colors = await chart.evaluate(element => {
      const style = getComputedStyle(element)
      const text = element.querySelector('.docs-comparison-labels strong')!
      return { background: style.backgroundColor, text: getComputedStyle(text).color }
    })
    expect(colors.background).not.toBe(colors.text)
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  }
})

test('benchmark tables remain readable without page overflow on mobile', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/benchmarks', { waitUntil: 'domcontentloaded' })

  const comparison = page.getByRole('figure', { name: 'Build and render comparison' })
  await expect(comparison).toBeVisible()
  await expect(comparison).toContainText('FSharp.ViewEngine')
  await expect(page.getByRole('table').first()).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})

test('sitemap, robots, and social metadata expose canonical public discovery', async ({ request, page }) => {
  const sitemap = await request.get('/sitemap.xml')
  expect(sitemap.status()).toBe(200)
  expect(sitemap.headers()['content-type']).toContain('application/xml')
  const sitemapXml = await sitemap.text()
  const paths = await publicRoutePaths(request)
  expect(paths).toContain('/')
  expect(paths).toContain('/components')
  expect(paths).toContain('/examples')
  expect(paths).not.toContain('/docs')
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
