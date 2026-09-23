import { test, expect } from '@playwright/test'

test('Documentation App mode fixtures expand browser and phone surfaces with independent review states @cross-browser', async ({ page }) => {
  await page.goto('/docs/components/fixture')
  const documentRequests: string[] = []
  page.on('request', request => {
    if (request.resourceType() === 'document') documentRequests.push(request.url())
  })
  await page.evaluate(() => { (window as typeof window & { fixtureDocument?: string }).fixtureDocument = 'retained' })

  await page.evaluate(() => window.fsharpDocsColorMode.set('dark'))
  await expect(page.locator('html')).toHaveClass(/dark/)

  await page.getByRole('button', { name: 'Open Create a view in App mode' }).click()
  const root = page.locator('[data-fve-app-mode-root="true"]')
  const controls = page.locator('[data-fve-app-mode-controls="true"]')
  await expect(root).toHaveAttribute('data-fve-app-mode-surface', 'browser')
  await expect(root.locator('.spec-browser-toolbar')).toHaveCount(0)
  const productSurface = root.locator('.docs-product-screen')
  await expect(productSurface).toBeVisible()
  expect(await productSurface.evaluate((element) => element.getBoundingClientRect().height)).toBeGreaterThanOrEqual(await page.evaluate(() => window.innerHeight))
  await expect(root.getByRole('textbox', { name: 'View name' })).toHaveValue('accountSummary')
  await expect.poll(() => page.evaluate(() => (window as typeof window & { fixtureDocument?: string }).fixtureDocument)).toBe('retained')

  const reviewState = controls.getByRole('combobox', { name: 'Review state' })
  await expect(controls).toHaveCSS('font-size', '12px')
  await expect(reviewState).toHaveCSS('display', 'flex')
  await expect(reviewState).toHaveCSS('min-height', '32px')
  await expect(controls.locator(':scope > span[aria-disabled="true"]')).toHaveCount(0)

  const colorMode = controls.getByRole('button', { name: 'Choose color theme' })
  await expect(colorMode).toBeVisible()
  await expect(colorMode).toHaveCSS('width', '32px')
  await expect(colorMode).toHaveText('')
  await expect(colorMode.locator('svg:visible')).toHaveCount(1)
  await colorMode.click()
  const darkThemeOption = page.getByRole('menuitemradio', { name: 'Dark', exact: true })
  await expect(darkThemeOption).toBeVisible()
  await reviewState.click()
  await expect(darkThemeOption).not.toBeVisible()
  const validationInLightDock = page.getByRole('option', { name: 'Validation', exact: true })
  await expect(validationInLightDock).toBeVisible()
  await validationInLightDock.hover()
  await expect.poll(() => validationInLightDock.evaluate(element => getComputedStyle(element).color)).not.toBe('rgb(255, 255, 255)')
  await reviewState.press('Escape')

  await colorMode.click()
  await page.getByRole('menuitemradio', { name: 'Dark', exact: true }).click()
  await expect(page.locator('html')).toHaveClass(/dark/)
  await expect(productSurface).toHaveCSS('background-color', 'rgb(15, 23, 42)')
  await expect(controls).toHaveAttribute('data-fve-color-mode', 'light')
  await colorMode.click()
  const lightThemeOption = page.getByRole('menuitemradio', { name: 'Light', exact: true })
  await lightThemeOption.hover()
  await expect(lightThemeOption).toBeFocused()
  await expect(lightThemeOption).not.toHaveCSS('background-color', 'rgba(0, 0, 0, 0)')
  await lightThemeOption.click()
  await expect(page.locator('html')).not.toHaveClass(/dark/)
  await expect(controls).toHaveAttribute('data-fve-color-mode', 'dark')

  await reviewState.click()
  const validation = page.getByRole('option', { name: 'Validation', exact: true })
  await expect(validation).toHaveCSS('display', 'flex')
  await validation.hover()
  await expect(validation).not.toHaveCSS('background-color', 'rgba(0, 0, 0, 0)')
  await page.getByRole('option', { name: 'Validation', exact: true }).click()
  await expect(root).toContainText('Enter a view name.')
  await expect(page).toHaveURL(/fixtureState=validation.*fveAppMode=app/)
  await expect.poll(() => page.evaluate(() => (window as typeof window & { fixtureDocument?: string }).fixtureDocument)).toBe('retained')

  await controls.getByRole('link', { name: 'Exit App mode' }).click()
  await expect(page).not.toHaveURL(/fveAppMode=/)
  await expect.poll(() => page.evaluate(() => (window as typeof window & { fixtureDocument?: string }).fixtureDocument)).toBe('retained')
  expect(documentRequests).toEqual([])
  await page.evaluate(() => window.fsharpDocsColorMode.set('dark'))
  await page.getByRole('button', { name: 'Open Create a view on phone in App mode' }).click()
  await expect(root).toHaveAttribute('data-fve-app-mode-surface', 'phone')
  await expect(root).toContainText('Render your first component')
  const phoneScreen = root.locator('.fve-phone-screen')
  const phoneStatus = root.locator('.fve-phone-status')
  const phoneCamera = root.locator('.fve-phone-camera')
  await expect(phoneScreen).toHaveCSS('background-color', 'rgb(23, 23, 23)')
  await expect(phoneStatus).toHaveCSS('background-color', 'rgb(23, 23, 23)')
  expect(await phoneCamera.evaluate((camera) => {
    const cameraBounds = camera.getBoundingClientRect()
    const statusBounds = camera.parentElement!.getBoundingClientRect()
    return Math.abs((cameraBounds.left + cameraBounds.width / 2) - (statusBounds.left + statusBounds.width / 2))
  })).toBeLessThan(0.5)
})

test('Documentation page examples use wide fixtures, relevant building blocks, and App mode', async ({ page }) => {
  for (const item of [
    {
      path: '/docs/page-examples/documentation-site',
      label: 'Documentation site page example',
      expected: 'Getting started',
      builtWith: [
        ['Layouts', '/docs/components/layouts'],
        ['Content', '/docs/components/content'],
        ['Navigation', '/docs/components/navigation'],
      ],
    },
    {
      path: '/docs/page-examples/api-reference',
      label: 'API reference page example',
      expected: 'Render a view',
      builtWith: [
        ['Layouts', '/docs/components/layouts'],
        ['API reference', '/docs/components/api-reference'],
      ],
    },
    {
      path: '/docs/page-examples/executable-specification',
      label: 'Executable specification page example',
      expected: 'Render a view',
      builtWith: [
        ['Layouts', '/docs/components/layouts'],
        ['Browser', '/components/browser'],
        ['Tabs', '/components/tabs'],
        ['Diagrams', '/docs/components/diagrams'],
      ],
    },
  ] as const) {
    await page.goto(item.path)
    const layout = page.locator('#page-content > .spec-page-viewport > .spec-page-layout')
    await expect(layout).toHaveClass(/docs-gallery-layout/)
    await expect(layout.locator(':scope > .spec-toc')).toHaveCount(0)
    await expect(layout.locator('.spec-main-inner')).toHaveCSS('max-width', 'none')

    const example = page.locator('[data-docs-example="true"]').first()
    await expect(example.locator('[data-fve-full-bleed-example="true"]')).toHaveCount(1)
    await expect(example.getByRole('tabpanel', { name: 'Preview' })).toHaveCSS('padding', '0px')
    const builtWith = page.getByRole('navigation', { name: `${item.label.replace(' page example', '')} building blocks` })
    for (const [label, href] of item.builtWith) {
      await expect(builtWith.getByRole('link', { name: label, exact: true })).toHaveAttribute('href', href)
    }

    await example.getByRole('button', { name: `Open ${item.label} in App mode` }).click()
    const root = page.locator('[data-fve-app-mode-root="true"]')
    const controls = page.locator('[data-fve-app-mode-controls="true"]')
    await expect(root).toHaveAttribute('data-fve-app-mode-surface', 'browser')
    await expect(root.locator('.spec-browser-toolbar')).toHaveCount(0)
    await expect(controls.getByRole('button', { name: 'Choose color theme' })).toBeVisible()
    const document = root.locator('iframe').contentFrame()
    await expect(document.getByRole('heading', { name: item.expected, exact: true }).first()).toBeVisible()
    const bounds = await root.locator('iframe').evaluate(frame => frame.getBoundingClientRect().toJSON())
    const viewport = await page.evaluate(() => ({ width: innerWidth, height: innerHeight }))
    expect(bounds.width).toBeGreaterThanOrEqual(viewport.width)
    expect(bounds.height).toBeGreaterThanOrEqual(viewport.height)

    await controls.getByRole('link', { name: 'Exit App mode' }).click()
    await expect(root).toHaveCount(0)
    await expect(example).toBeVisible()
    expect(new URL(page.url()).pathname).toBe(item.path)
    expect(new URL(page.url()).search).toBe('')
  }
})

test('Documentation page examples inherit the resolved host color mode in previews and App mode', async ({ page }) => {
  for (const [path, label] of [
    ['/docs/page-examples/documentation-site', 'Documentation site page example'],
    ['/docs/page-examples/api-reference', 'API reference page example'],
    ['/docs/page-examples/executable-specification', 'Executable specification page example'],
  ] as const) {
    await page.goto(path)
    const example = page.locator('[data-docs-example="true"]').first()
    const frame = example.locator('iframe')
    const preview = frame.contentFrame()

    await page.evaluate(() => window.fsharpDocsColorMode.set('dark'))
    await expect(page.locator('html')).toHaveClass(/dark/)
    await expect(preview.locator('html')).toHaveClass(/dark/)
    await expect(preview.locator('html')).toHaveAttribute('data-color-mode', 'dark')

    await page.evaluate(() => window.fsharpDocsColorMode.set('light'))
    await expect(page.locator('html')).not.toHaveClass(/dark/)
    await expect(preview.locator('html')).not.toHaveClass(/dark/)
    await expect(preview.locator('html')).toHaveAttribute('data-color-mode', 'light')

    await example.getByRole('button', { name: `Open ${label} in App mode` }).click()
    const root = page.locator('[data-fve-app-mode-root="true"]')
    const controls = page.locator('[data-fve-app-mode-controls="true"]')
    const appDocument = root.locator('iframe').contentFrame()
    await expect(appDocument.locator('html')).not.toHaveClass(/dark/)

    const colorMode = controls.getByRole('button', { name: 'Choose color theme' })
    await colorMode.click()
    await page.getByRole('menuitemradio', { name: 'Dark', exact: true }).click()
    await expect(page.locator('html')).toHaveClass(/dark/)
    await expect(appDocument.locator('html')).toHaveClass(/dark/)
    await expect(appDocument.locator('html')).toHaveAttribute('data-color-mode', 'dark')
  }
})

test('Documentation, API, and specification examples keep navigation inside their fixtures @cross-browser', async ({ page }) => {
  await page.setViewportSize({ width: 1800, height: 1000 })
  await page.goto('/docs/page-examples/documentation-site')
  const documentationStates = page.getByRole('navigation', { name: 'Documentation site review state' })
  await expect(documentationStates.getByRole('link', { name: 'Getting started' })).toHaveAttribute('aria-current', 'page')
  await documentationStates.getByRole('link', { name: 'Overview' }).click()
  await expect(page).toHaveURL(/\/docs\/page-examples\/documentation-site\?fixtureState=overview$/)
  let preview = page.locator('[data-docs-example="true"]').first().locator('iframe').contentFrame()
  await expect(preview.getByRole('heading', { name: 'Acme Docs', exact: true })).toBeVisible()
  const gettingStarted = preview.locator('#nav-guide')
  await expect(gettingStarted).toHaveAttribute('href', '/docs/previews/documentation-site-getting-started')
  await gettingStarted.click()
  await expect(preview.getByRole('heading', { name: 'Getting started', exact: true })).toBeVisible()
  await expect(page).toHaveURL(/fixtureState=overview$/)

  await page.goto('/docs/page-examples/api-reference')
  const apiStates = page.getByRole('navigation', { name: 'API reference review state' })
  await expect(apiStates.getByRole('link', { name: 'Render a view' })).toHaveAttribute('aria-current', 'page')
  await apiStates.getByRole('link', { name: 'Overview' }).click()
  await expect(page).toHaveURL(/\/docs\/page-examples\/api-reference\?fixtureState=overview$/)
  preview = page.locator('[data-docs-example="true"]').first().locator('iframe').contentFrame()
  await expect(preview.getByRole('heading', { name: 'Rendering API', exact: true })).toBeVisible()
  await preview.getByRole('link', { name: 'Render a view', exact: true }).last().click()
  await expect(preview.getByRole('heading', { name: 'Render a view', exact: true })).toBeVisible()
  await expect(preview.getByRole('complementary', { name: 'Documentation navigation' })).not.toContainText('Guides')
  await expect(preview.locator('[data-http-method="POST"]')).toContainText('/v1/render')
  await expect(preview.locator('[data-parameter-location]')).toHaveCount(3)
  await expect(preview.locator('.docs-code-panel')).toHaveCount(2)

  await page.goto('/docs/page-examples/executable-specification')
  preview = page.locator('[data-docs-example="true"]').first().locator('iframe').contentFrame()
  const specificationOverview = preview.getByRole('link', { name: 'Overview', exact: true })
  await expect(specificationOverview).toHaveAttribute('href', '/docs/previews/executable-specification-overview')
  await specificationOverview.click()
  await expect(preview.getByRole('heading', { name: 'View rendering', exact: true })).toBeVisible()
  await expect(page).toHaveURL(/\/docs\/page-examples\/executable-specification$/)
  await preview.locator('#nav-render-workflow').click()
  await expect(preview.getByRole('heading', { name: 'Render a view', exact: true })).toBeVisible()
  await expect(page).toHaveURL(/\/docs\/page-examples\/executable-specification$/)

  await page.getByRole('button', { name: 'Open Executable specification page example in App mode' }).click()
  const appPreview = page.locator('[data-fve-app-mode-root="true"] iframe').contentFrame()
  await appPreview.locator('#nav-specification-overview').click()
  await expect(appPreview.getByRole('heading', { name: 'View rendering', exact: true })).toBeVisible()
  await expect(page).toHaveURL(/\/docs\/page-examples\/executable-specification\?fveAppMode=app/)
})

test('Fixture keeps its exact compiled source beside preview and copy controls', async ({ page }) => {
  await page.goto('/docs/components/fixture')
  await page.evaluate(() => {
    Object.defineProperty(navigator, 'clipboard', {
      configurable: true,
      value: { writeText: async (value: string) => { (window as typeof window & { copiedFixtureSource?: string }).copiedFixtureSource = value } },
    })
  })

  const example = page.locator('[data-docs-example="true"]').filter({ has: page.locator('button[aria-label="Copy Workflow fixture code"]') })
  await example.getByRole('tab', { name: 'Code' }).click()
  const copy = example.getByRole('button', { name: 'Copy Workflow fixture code' })
  await copy.click()
  await expect(copy).toHaveAttribute('data-copied', 'true')
  await expect.poll(() => page.evaluate(() => (window as typeof window & { copiedFixtureSource?: string }).copiedFixtureSource)).toContain('Fixture.create "checkout-shipping"')
})

test('Browser and Phone primitives render independently while Fixture adds review context', async ({ page }) => {
  const root = page.locator('[data-fve-app-mode-root="true"]')

  await page.goto('/components/browser')
  await expect(page.locator('.spec-browser-address')).toContainText('shop.example.test/checkout/shipping')
  await page.getByRole('button', { name: 'Open Shipping address in App mode' }).click()
  await expect(root).toHaveAttribute('data-fve-app-mode-surface', 'browser')
  await expect(root.locator('.spec-browser-toolbar')).toHaveCount(0)
  await page.locator('[data-fve-app-mode-controls="true"]').getByRole('link', { name: 'Exit App mode' }).click()

  await page.goto('/components/phone')
  await expect(page.locator('[data-fve-phone="true"]')).toBeVisible()
  await page.getByRole('button', { name: 'Open Saved offers in App mode' }).click()
  await expect(root).toHaveAttribute('data-fve-app-mode-surface', 'phone')
  await page.locator('[data-fve-app-mode-controls="true"]').getByRole('link', { name: 'Exit App mode' }).click()

  await page.goto('/docs/components/fixture')
  await page.getByRole('button', { name: 'Open Shipping address in App mode' }).click()
  const controls = page.locator('[data-fve-app-mode-controls="true"]')
  await expect(controls.getByRole('combobox', { name: 'Review state' })).toBeVisible()
  await expect(controls.getByRole('link', { name: 'Previous: Cart' })).toBeVisible()
  await expect(controls.getByRole('link', { name: 'Next: Payment' })).toBeVisible()
})
