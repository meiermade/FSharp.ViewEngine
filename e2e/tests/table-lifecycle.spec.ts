import { test, expect } from '@playwright/test'

test('Table loading rows replace themselves with native rows without rebuilding earlier content @cross-browser', async ({ page }) => {
  const requests: number[] = []
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  page.on('request', request => {
    const url = new URL(request.url())
    if (url.pathname === '/components/table/more') requests.push(Number(url.searchParams.get('offset')))
  })
  await page.goto('/components/table')
  const host = page.locator('#infinite-team-table')
  const table = host.getByRole('table', { name: 'More team members', exact: true })
  await expect(host.locator('tbody tr')).toHaveCount(9)
  const original = await host.locator('#infinite-person-1').elementHandle()
  const header = await table.locator('thead').elementHandle()
  const unrelated = await page.locator('#components-table-panel-preview table').elementHandle()
  await host.scrollIntoViewIfNeeded()
  const scroll = host.getByRole('region', { name: 'More team members', exact: true })
  await scroll.evaluate(element => { element.scrollTop = element.scrollHeight })
  await expect(host.locator('#infinite-person-16')).toBeAttached()
  await expect(host.locator('#people-more-8')).toHaveCount(0)
  await expect(host.locator('#people-more-16')).toHaveCount(1)
  await expect(host.locator('tbody tr')).toHaveCount(17)
  await expect(host.locator('#people-more-16 td')).toHaveAttribute('colspan', '3')
  await expect(host.locator('#people-more-16')).toContainText('Loading more members…')
  await scroll.evaluate(element => { element.scrollTop = element.scrollHeight })
  await expect(host.locator('#infinite-person-24')).toBeAttached()
  await expect(host.locator('tbody tr')).toHaveCount(24)
  await expect(host.locator('[data-on-intersect__once]')).toHaveCount(0)
  expect(requests).toEqual([8, 16])
  expect(await host.locator('#infinite-person-1').evaluate((element, prior) => element === prior, original)).toBe(true)
  expect(await table.locator('thead').evaluate((element, prior) => element === prior, header)).toBe(true)
  expect(await page.locator('#components-table-panel-preview table').evaluate((element, prior) => element === prior, unrelated)).toBe(true)
  expect(await host.locator('tbody').evaluate(body => new Set([...body.children].map(row => row.id)).size)).toBe(24)
  expect(await host.locator('tbody tr').evaluateAll(rows => rows.every(row => row.tagName === 'TR' && [...row.children].every(cell => ['TD', 'TH'].includes(cell.tagName))))).toBe(true)
  expect(errors).toEqual([])
})

test('Table reconciles server-supplied parts and remains interactive after repeated morphs and reconnect @cross-browser', async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/table')
  const preview = page.locator('#components-table-panel-preview')
  await preview.locator('fve-table table').waitFor()
  await preview.locator('fve-table').evaluate(host => host.id = 'table-morph-host')
  let version = 0
  await page.route('**/components/table/more?morph=1*', async route => {
    version++
    const source = `<fve-table id="table-morph-host" data-fve-table="true" aria-label="Updated members" density="comfortable"><template data-fve-table-part="head"><tr class="fve-table-row"><th class="fve-table-cell" scope="col">Action</th></tr></template><template data-fve-table-part="body" id="updated-member-rows"><tr class="fve-table-row"><td class="fve-table-cell"><button id="table-morph-action" type="button" data-on:click="@get('/components/table/more?morph=1')">Revision ${version}</button></td></tr></template></fve-table>`
    await route.fulfill({ status: 200, contentType: 'text/event-stream', body: `event: datastar-patch-elements\ndata: elements ${source}\n\n` })
  })
  // A normal application-owned control initiates the first real Datastar patch.
  await preview.locator('tbody tr').first().locator('td').first().evaluate(cell => {
    const button = document.createElement('button')
    button.id = 'table-morph-action'
    button.textContent = 'Update table'
    button.setAttribute('data-on:click', "@get('/components/table/more?morph=1')")
    cell.append(button)
  })
  const initialAction = page.getByRole('button', { name: 'Update table', exact: true })
  await initialAction.focus()
  await initialAction.press('Enter')
  const host = page.locator('#table-morph-host')
  await expect(host.getByRole('table', { name: 'Updated members', exact: true })).toHaveCount(1)
  await expect(host.locator('tbody')).toHaveAttribute('id', 'updated-member-rows')
  const action = host.getByRole('button', { name: 'Revision 1', exact: true })
  await expect(action).toBeFocused()
  await action.click()
  await expect(host.getByRole('button', { name: 'Revision 2', exact: true })).toBeVisible()
  await host.evaluate(element => { const parent = element.parentElement!; const next = element.nextSibling; element.remove(); parent.insertBefore(element, next) })
  await host.getByRole('button', { name: 'Revision 2', exact: true }).click()
  await expect(host.getByRole('button', { name: 'Revision 3', exact: true })).toBeVisible()
  await expect(host.locator('table')).toHaveCount(1)
  await expect(host.locator('template')).toHaveCount(0)
  await expect(host.locator('tbody tr')).toHaveCount(1)
  const currentAction = host.getByRole('button', { name: 'Revision 3', exact: true })
  await currentAction.focus()
  await currentAction.evaluate(element => element.blur())
  await host.evaluate(element => { element.classList.add('min-h-0'); element.setAttribute('scroll-rows', 'true') })
  await expect(host).toHaveCSS('display', 'flex')
  await expect(currentAction).not.toBeFocused()
  await host.evaluate(element => element.setAttribute('scroll-rows', 'false'))
  await expect(host).toHaveCSS('display', 'block')
  await expect(host).toHaveClass(/(?:^|\s)min-h-0(?:\s|$)/)
  expect(errors).toEqual([])
})

test('Table batches host updates and leaves existing rows untouched when adding rows @cross-browser', async ({ page }) => {
  await page.goto('/components/table')
  const host = page.locator('#components-table-panel-preview fve-table')
  await expect(host.locator('tbody tr').first()).toHaveAttribute('role', 'row')
  const changes = await host.evaluate(async host => {
    const table = host.querySelector('table')!
    const body = table.tBodies[0]
    const rows = new Set([...body.rows])
    let roleWrites = 0
    let traversals = 0
    const query = Element.prototype.querySelectorAll
    Element.prototype.querySelectorAll = function (selector: string) {
      if (this === table && selector === 'tr') traversals++
      return query.call(this, selector)
    }
    const observer = new MutationObserver(records => {
      roleWrites += records.filter(record => record.attributeName === 'role' &&
        (rows.has(record.target as HTMLTableRowElement) || rows.has(record.target.parentElement as HTMLTableRowElement))).length
    })
    observer.observe(table, { subtree: true, attributes: true, attributeFilter: ['role'] })
    const settle = () => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))
    try {
      host.setAttribute('density', 'comfortable')
      host.setAttribute('surface', 'panel')
      host.setAttribute('scroll-rows', 'true')
      await settle()
      const hostUpdates = { traversals, roleWrites }
      traversals = 0
      roleWrites = 0
      const row = body.insertRow()
      row.innerHTML = '<th scope="row">New member</th><td>Editor</td><td>Active</td>'
      await settle()
      return { hostUpdates, rowUpdates: { traversals, roleWrites }, newRoles: [row.getAttribute('role'), ...[...row.cells].map(cell => cell.getAttribute('role'))] }
    } finally {
      observer.disconnect()
      Element.prototype.querySelectorAll = query
    }
  })
  expect(changes).toEqual({ hostUpdates: { traversals: 1, roleWrites: 0 }, rowUpdates: { traversals: 1, roleWrites: 0 }, newRoles: ['row', 'rowheader', 'cell', 'cell'] })
})
