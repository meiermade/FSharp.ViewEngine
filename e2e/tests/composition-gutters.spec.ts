import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

test('detail statuses belong to fields beneath a Detail section heading @cross-browser', async ({ page }) => {
  for (const width of [1440, 390]) {
    await page.setViewportSize({ width, height: 1000 })
    for (const [route, status] of [
      ['/components/detail', 'Active'],
      ['/components/app-shell?destination=ledger-account-2048', 'Active'],
      ['/components/app-shell?destination=ledger-transaction-201', 'Verified'],
    ]) {
      await page.goto(route)
      const preview = page.locator('.docs-components-preview .fve-components').first()
      const detail = preview.getByRole('region', { name: 'Detail', exact: true })
      await expect(detail.getByRole('heading', { name: 'Detail', exact: true, level: 2 })).toBeVisible()
      await expect(detail.getByRole('term').filter({ hasText: /^Status$/ })).toBeVisible()
      await expect(detail.getByRole('definition').filter({ hasText: status })).toBeVisible()
      await expect(preview.locator('header').filter({ hasText: status })).toHaveCount(0)
      if (status === 'Active') {
        const transactions = preview.getByRole('region', { name: route === '/components/detail' ? 'Recent transactions' : 'Account transactions', exact: true })
        await expect(transactions.locator(':scope > [data-fve-section-header]').getByRole('heading', { name: 'Transactions', exact: true })).toBeVisible()
        await expect(transactions.getByRole('table', { name: 'Transactions', exact: true })).toBeVisible()
      }
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    }
  }
})

test('plain table surfaces match the page while row states remain distinct @cross-browser', async ({ page }) => {
  for (const theme of ['Light', 'Dark']) {
    for (const width of [1440, 390]) {
      await page.setViewportSize({ width, height: 1000 })
      for (const route of ['collection', 'detail']) {
        await page.goto(`/components/${route}`)
        await page.getByRole('button', { name: 'Choose color theme' }).click()
        await page.getByRole('menuitemradio', { name: theme, exact: true }).click()
        await page.mouse.move(0, 0)
        const preview = page.locator('.docs-components-preview .fve-components')
        const pageColor = await preview.evaluate(element => {
          const probe = document.createElement('span')
          probe.style.backgroundColor = 'var(--fve-page)'
          element.append(probe)
          const color = getComputedStyle(probe).backgroundColor
          probe.remove()
          return color
        })
        const region = preview.locator('[data-fve-table]').getByRole('region', { name: route === 'collection' ? 'Accounts' : 'Transactions', exact: true })
        const row = region.locator('tbody tr').first()
        await expect(region).toHaveCSS('background-color', pageColor)
        await expect(region.locator('thead')).toHaveCSS('background-color', pageColor)
        await expect(row).toHaveCSS('background-color', pageColor)
        if (width === 1440) {
          const actions = row.locator('[data-mobile-cell="actions"]')
          await expect(actions).toHaveCSS('background-color', pageColor)
          await row.hover()
          await expect(row).not.toHaveCSS('background-color', pageColor)
          await expect(actions).toHaveCSS('background-color', await row.evaluate(element => getComputedStyle(element).backgroundColor))
        }
        await row.getByRole('checkbox').check()
        await expect(row).toHaveAttribute('data-selected', 'true')
        await expect(row).not.toHaveCSS('background-color', pageColor)
        if (width === 1440) await expect(row.locator('[data-mobile-cell="actions"]')).toHaveCSS('background-color', await row.evaluate(element => getComputedStyle(element).backgroundColor))
      }
    }
  }
})

test('collection and detail share inset content boundaries without a sidebar @cross-browser', async ({ page }) => {
  for (const width of [1440, 390]) {
    await page.setViewportSize({ width, height: 1000 })
    for (const route of ['collection', 'detail']) {
      await page.goto(`/components/${route}`)
      const preview = page.locator('.docs-components-preview .fve-components')
      const heading = preview.getByRole('heading', { name: route === 'collection' ? 'Accounts' : 'Transactions', exact: true })
      const table = preview.locator('[data-fve-table]').getByRole('region', { name: route === 'collection' ? 'Accounts' : 'Transactions', exact: true })
      const bounds = await preview.boundingBox()
      const headingBounds = await heading.boundingBox()
      const tableBounds = await table.boundingBox()
      expect(tableBounds!.x - bounds!.x).toBeGreaterThanOrEqual(16)
      expect(bounds!.x + bounds!.width - tableBounds!.x - tableBounds!.width).toBeGreaterThanOrEqual(16)
      expect(Math.abs(tableBounds!.x - headingBounds!.x)).toBeLessThanOrEqual(1)
      const footer = preview.getByRole('status').filter({ hasText: '0 selected on this page' })
      expect(Math.abs((await footer.boundingBox())!.x - tableBounds!.x)).toBeLessThanOrEqual(1)
      await expect(preview.getByRole('complementary')).toHaveCount(0)
      if (route === 'collection') {
        const controls = await preview.getByRole('region', { name: 'Collection controls' }).boundingBox()
        expect(Math.abs(controls!.x - tableBounds!.x)).toBeLessThanOrEqual(1)
        expect(Math.abs(controls!.width - tableBounds!.width)).toBeLessThanOrEqual(1)
      }
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
      expect((await new AxeBuilder({ page }).include('.docs-components-preview').analyze()).violations).toEqual([])
    }
    for (const destination of ['ledger-accounts', 'ledger-account-2048']) {
      await page.goto(`/components/app-shell?destination=${destination}`)
      const shell = page.locator('#ledger-app-shell')
      const heading = await shell.getByRole('heading', { level: 1 }).boundingBox()
      const table = await shell.locator('[data-fve-table]').getByRole('region', { name: destination === 'ledger-accounts' ? 'Accounts' : 'Transactions', exact: true }).boundingBox()
      expect(Math.abs(heading!.x - table!.x)).toBeLessThanOrEqual(1)
    }
  }
})

test('record menus copy, download, and navigate to matching fixtures @cross-browser', async ({ page }) => {
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: {
      writeText: async (value: string) => { (window as any).copiedValue = value },
    } })
  })
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  for (const [route, record, kind, id] of [
    ['collection', 'Assets', 'account', 101],
    ['detail', 'Northwind payment', 'transaction', 201],
  ] as const) {
    await page.goto(`/components/${route}`)
    const preview = page.locator('.docs-components-preview')
    const trigger = preview.getByRole('button', { name: `More actions for ${record}`, exact: true })
    await trigger.click()
    const menu = page.getByRole('menu', { name: `More actions for ${record}`, exact: true })
    await expect(menu.getByRole('menuitem')).toHaveCount(4)
    await menu.getByRole('menuitem', { name: `Copy ${kind} ID`, exact: true }).click()
    await expect.poll(() => page.evaluate(() => (window as any).copiedValue)).toBe(String(id))
    await expect(preview.getByRole('status').filter({ hasText: `Copied ${kind} ID.` })).toBeVisible()
    await expect(trigger).toBeFocused()
    await trigger.click()
    await menu.getByRole('menuitem', { name: `Copy ${kind} link`, exact: true }).click()
    await expect.poll(() => page.evaluate(() => (window as any).copiedValue)).toBe(`${new URL(page.url()).origin}/components/app-shell?destination=ledger-${kind}-${id}`)
    await trigger.click()
    const downloaded = page.waitForEvent('download')
    await menu.getByRole('menuitem', { name: `Download ${kind}`, exact: true }).click()
    const download = await downloaded
    expect(download.suggestedFilename()).toBe(`${kind}-${id}.json`)
    const stream = await download.createReadStream()
    const chunks: Buffer[] = []
    for await (const chunk of stream!) chunks.push(Buffer.from(chunk))
    const data = JSON.parse(Buffer.concat(chunks).toString('utf8'))
    expect(data.id).toBe(id)
    expect(data.name ?? data.description).toBe(record)
    await trigger.click()
    await menu.getByRole('menuitem', { name: `View ${kind}`, exact: true }).click()
    await expect(page.locator('#ledger-app-shell').getByRole('heading', { level: 1, name: record, exact: true })).toBeVisible()
  }
  await page.goto('/components/detail')
  await page.getByRole('button', { name: 'More actions', exact: true }).click()
  await page.getByRole('menuitem', { name: 'Copy account ID', exact: true }).click()
  await expect.poll(() => page.evaluate(() => (window as any).copiedValue)).toBe('2048')
  await page.goto('/components/collection')
  await page.evaluate(() => Object.defineProperty(navigator, 'clipboard', { value: { writeText: () => Promise.reject(new Error('Denied')) } }))
  await page.getByRole('button', { name: 'More actions for Assets', exact: true }).click()
  await page.getByRole('menuitem', { name: 'Copy account ID', exact: true }).click()
  await expect(page.getByRole('status').filter({ hasText: 'Could not copy account ID. Check clipboard permissions.' })).toBeVisible()
  expect(errors).toEqual([])
})
