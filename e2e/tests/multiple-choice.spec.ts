import { expect, test, type Locator } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const crossBrowser = { tag: '@cross-browser' }

async function values(root: Locator, name: string) {
  return root.evaluate((element, name) => {
    const form = document.createElement('form')
    for (const input of element.querySelectorAll('input[type="hidden"]')) form.append(input.cloneNode(true))
    return new FormData(form).getAll(name)
  }, name)
}

test('multiple Select separates active and selected options and submits ordered repeated values', crossBrowser, async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/select')
  const example = page.locator('#components-select-multiple')
  const trigger = example.getByRole('button', { name: 'Team members', exact: true })
  const list = example.getByRole('listbox', { name: 'Team members', exact: true })
  await trigger.click()
  const selectAll = example.getByRole('button', { name: 'Select all Team members', exact: true })
  const clear = example.getByRole('button', { name: 'Clear Team members', exact: true })
  await expect(clear).toBeDisabled()
  await expect(selectAll).toBeEnabled()
  await selectAll.click()
  await expect.poll(() => values(example, 'memberIds')).toEqual(['alex', 'jamie', 'riley', 'taylor'])
  await expect(selectAll).toBeDisabled()
  await clear.click()
  await expect(list).toBeFocused()
  await expect(list).toHaveAttribute('aria-multiselectable', 'true')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Space')
  await expect(list.getByRole('option', { name: 'Alex Morgan' })).toHaveAttribute('aria-selected', 'true')
  await page.keyboard.press('ArrowDown')
  await expect(list.getByRole('option', { name: 'Jamie Lee' })).toHaveAttribute('data-active', 'true')
  await expect(list.getByRole('option', { name: 'Jamie Lee' })).toHaveAttribute('aria-selected', 'false')
  await expect.poll(() => values(example, 'memberIds')).toEqual(['alex'])
  await page.keyboard.press('Space')
  await expect(list).toBeVisible()
  await page.keyboard.press('End')
  await expect(list.getByRole('option', { name: 'Taylor Brooks' })).toHaveAttribute('data-active', 'true')
  await expect(list.getByRole('option', { name: 'Sam Rivera' })).toBeDisabled()
  await page.keyboard.press('Home')
  await expect(list.getByRole('option', { name: 'Alex Morgan' })).toHaveAttribute('data-active', 'true')
  await page.keyboard.press('j')
  await expect(list.getByRole('option', { name: 'Jamie Lee' })).toHaveAttribute('data-active', 'true')
  await page.keyboard.press('Space')
  await expect.poll(() => values(example, 'memberIds')).toEqual(['alex'])
  await page.keyboard.press('Escape')
  await expect(trigger).toBeFocused()
  await expect(list).toBeHidden()
  await trigger.press('Enter')
  await expect(list).toBeFocused()
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Tab')
  await expect(list).toBeHidden()
  await expect.poll(() => values(example, 'memberIds')).toEqual(['alex'])
  await trigger.click()
  await expect(clear).toBeEnabled()
  await clear.click()
  await expect(list).toBeFocused()
  await expect.poll(() => values(example, 'memberIds')).toEqual([])
  const selected = page.locator('#components-select-multiple-selected')
  await expect(selected.getByRole('button', { name: 'Assigned members', exact: true })).toContainText('Alex Morgan, Jamie Lee + 1 more')
  await expect.poll(() => values(selected, 'assignedIds')).toEqual(['alex', 'jamie', 'riley'])
  expect((await new AxeBuilder({ page }).include('#components-select-multiple').analyze()).violations).toEqual([])
  expect(errors).toEqual([])
})

test('multiple searchable Select keeps search separate, toggles with Enter, and clears from its popup', crossBrowser, async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/select')
  const example = page.locator('#components-select-search-multiple')
  const trigger = example.getByRole('button', { name: 'Search members', exact: true })
  const list = example.getByRole('listbox', { name: 'Search members', exact: true })
  await expect.poll(() => values(example, 'searchMemberIds')).toEqual(['alex', 'jamie'])
  await trigger.click()
  const input = example.getByRole('searchbox', { name: 'Search Search members', exact: true })
  const selectAll = example.getByRole('button', { name: 'Select all Search members', exact: true })
  await expect(input).toBeFocused()
  await input.press('Tab')
  await expect(selectAll).toBeFocused()
  await page.keyboard.press('Shift+Tab')
  await expect(input).toBeFocused()
  await expect(input).toHaveValue('')
  await input.fill('Riley')
  await expect(list.getByRole('option', { name: 'Riley Chen' })).toBeVisible()
  await expect(list).toHaveAttribute('aria-multiselectable', 'true')
  await input.press('ArrowDown')
  await input.press('Enter')
  await expect(input).toHaveValue('Riley')
  await expect(input).toBeFocused()
  await expect(list).toBeVisible()
  await expect.poll(() => values(example, 'searchMemberIds')).toEqual(['alex', 'jamie', 'riley'])
  await input.press('Space')
  await expect(input).toHaveValue('Riley ')
  await expect.poll(() => values(example, 'searchMemberIds')).toEqual(['alex', 'jamie', 'riley'])
  await input.press('ArrowDown')
  await input.press('Enter')
  await expect.poll(() => values(example, 'searchMemberIds')).toEqual(['alex', 'jamie'])
  await example.getByRole('button', { name: 'Clear Search Search members', exact: true }).click()
  await expect(input).toHaveValue('')
  await input.press('Backspace')
  await expect.poll(() => values(example, 'searchMemberIds')).toEqual(['alex', 'jamie'])
  await example.getByRole('option', { name: 'Alex Morgan' }).click()
  await expect.poll(() => values(example, 'searchMemberIds')).toEqual(['jamie'])
  await example.getByRole('button', { name: 'Clear Search members', exact: true }).click()
  await expect.poll(() => values(example, 'searchMemberIds')).toEqual([])
  expect((await new AxeBuilder({ page }).include('#components-select-search-multiple').analyze()).violations).toEqual([])
  expect(errors).toEqual([])
})

for (const kind of ['select']) {
  test(`multiple ${kind} validates native POST values and preserves edits through form morphs`, crossBrowser, async ({ page }) => {
    await page.goto(`/components/${kind}`)
    const form = page.getByRole('form', { name: kind === 'select' ? 'Project reviewers' : 'Project assignees', exact: true })
    const control = kind === 'select'
      ? form.getByRole('button', { name: 'Reviewers', exact: true })
      : form.getByRole('combobox', { name: 'Assignees', exact: true })
    const submit = form.getByRole('button', { name: kind === 'select' ? 'Validate reviewers' : 'Validate assignees' })
    await submit.click()
    await expect(form.getByRole('alert')).toHaveText('Choose one to three available members.')
    await expect(control).toHaveAttribute('aria-invalid', 'true')
    await control.click()
    await form.getByRole('option', { name: 'Alex Morgan' }).click()
    await form.getByRole('option', { name: 'Jamie Lee' }).click()
    await control.press('Escape')
    const response = page.waitForResponse(r => new URL(r.url()).pathname === `/components/choices/multiple-${kind}`)
    await submit.click()
    const result = await response
    expect(result.status()).toBe(200)
    const body = result.request().postData()!
    expect(body).toContain('alex')
    expect(body).toContain('jamie')
    const name = kind === 'select' ? 'reviewerIds' : 'assigneeIds'
    expect(result.request().headers()['content-type']).toContain('application/x-www-form-urlencoded')
    expect(new URLSearchParams(body).getAll(name)).toEqual(['alex', 'jamie'])
    await expect(page.locator(`#components-${kind === 'select' ? 'reviewers' : 'assignees'}-result`)).toHaveText('Validated 2 members. This example does not save your data.')
    await expect(form.getByRole('alert')).toHaveCount(0)
    await expect.poll(() => values(form, name)).toEqual(['alex', 'jamie'])
    await control.click()
    await form.getByRole('option', { name: 'Riley Chen' }).click()
    await form.getByRole('option', { name: 'Taylor Brooks' }).click()
    await control.press('Escape')
    await submit.click()
    await expect(form.getByRole('alert')).toHaveText('Choose one to three available members.')
    await expect.poll(() => values(form, name)).toEqual(['alex', 'jamie', 'riley', 'taylor'])
    expect((await new AxeBuilder({ page }).include(`#components-multiple-${kind}-form-region`).analyze()).violations).toEqual([])
  })
}

for (const remote of [false, true]) {
  test(`${remote ? 'remote' : 'static'} Select all reacts to clearing and changing search without changing selection`, crossBrowser, async ({ page }) => {
    await page.goto('/components/select')
    const example = page.locator(remote ? '#components-select-search-multiple-remote' : '#components-select-search-multiple')
    const label = remote ? 'Remote members' : 'Search members'
    await example.getByRole('button', { name: label, exact: true }).click()
    const input = example.getByRole('searchbox')
    const all = example.getByRole('button', { name: `${remote ? 'Select all results' : 'Select all'} ${label}`, exact: true })
    await example.getByRole('button', { name: `Clear ${label}`, exact: true }).click()
    await input.fill('Jamie')
    const jamie = example.getByRole('option', { name: 'Jamie Lee', exact: true })
    await expect(jamie).toBeVisible()
    await jamie.click()
    await expect(all).toBeDisabled()
    await example.getByRole('button', { name: `Clear Search ${label}`, exact: true }).click()
    await expect(example.getByRole('option', { name: 'Alex Morgan', exact: true })).toBeVisible()
    await expect(all).toBeEnabled()
    await input.fill('Nobody')
    await expect(example.getByRole('status').filter({ hasText: 'No matching options' })).toBeVisible()
    await expect(all).toBeDisabled()
    await input.fill('Riley')
    await expect(example.getByRole('option', { name: 'Riley Chen', exact: true })).toBeVisible()
    await expect(all).toBeEnabled()
    await all.click()
    await expect.poll(() => values(example, remote ? 'remoteMemberIds' : 'searchMemberIds')).toEqual(['jamie', 'riley'])
    await expect(all).toBeDisabled()
    await example.getByRole('button', { name: `Clear Search ${label}`, exact: true }).click()
    await expect(example.getByRole('option', { name: 'Alex Morgan', exact: true })).toBeVisible()
    await expect(all).toBeEnabled()
    await all.click()
    await expect.poll(() => values(example, remote ? 'remoteMemberIds' : 'searchMemberIds')).toEqual(['jamie', 'riley', 'alex', 'taylor'])
    await expect(all).toBeDisabled()
  })
}

test('remote multiple searchable Select retains selected labels across result, empty, error, and retry morphs', crossBrowser, async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/select')
  const example = page.locator('#components-select-search-multiple-remote')
  const trigger = example.getByRole('button', { name: 'Remote members', exact: true })
  await trigger.click()
  const input = example.getByRole('searchbox', { name: 'Search Remote members', exact: true })
  const selectAll = example.getByRole('button', { name: 'Select all results Remote members', exact: true })
  await input.fill('Jamie')
  await expect(example.getByRole('option', { name: 'Jamie Lee' })).toBeVisible()
  await selectAll.click()
  await expect.poll(() => values(example, 'remoteMemberIds')).toEqual(['alex', 'jamie'])
  await expect(example.getByRole('button', { name: 'Clear Remote members', exact: true })).toBeEnabled()
  await input.fill('Nobody')
  await expect(example.getByRole('status').filter({ hasText: 'No matching options' })).toBeVisible()
  await expect.poll(() => values(example, 'remoteMemberIds')).toEqual(['alex', 'jamie'])
  await input.fill('error')
  await expect(example.getByRole('alert')).toHaveText('Members could not be loaded.')
  await example.getByRole('button', { name: 'Retry', exact: true }).click()
  await expect(example.getByRole('option', { name: 'Riley Chen' })).toBeVisible()
  await expect(example.getByRole('status').filter({ hasText: 'No matching options' })).toBeHidden()
  await expect(input).toBeFocused()
  await example.getByRole('option', { name: 'Riley Chen' }).click()
  await expect.poll(() => values(example, 'remoteMemberIds')).toEqual(['alex', 'jamie', 'riley'])
  expect(errors).toEqual([])
})

test('remote multiple searchable Select cancels stale results immediately and preserves current selection', crossBrowser, async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  let release!: () => void
  const gate = new Promise<void>(resolve => { release = resolve })
  let received!: () => void
  const held = new Promise<void>(resolve => { received = resolve })
  let finished!: () => void
  const delivered = new Promise<void>(resolve => { finished = resolve })
  const query = (url: string) => JSON.parse(new URL(url).searchParams.get('datastar') || '{}').components_remote_members_query
  await page.route('**/components/members/search?*', async route => {
    if (query(route.request().url()) !== 'Jamie') return route.continue()
    const response = await route.fetch()
    received()
    await gate
    await route.fulfill({ response })
    finished()
  })
  await page.goto('/components/select')
  const example = page.locator('#components-select-search-multiple-remote')
  const trigger = example.getByRole('button', { name: 'Remote members', exact: true })
  await trigger.click()
  const input = example.getByRole('searchbox', { name: 'Search Remote members', exact: true })
  await input.fill('Jamie')
  await held
  await expect(input).toHaveAttribute('aria-busy', 'true')
  const aborted = page.waitForEvent('requestfailed', { predicate: request => query(request.url()) === 'Jamie' })
  await input.fill('Riley')
  await aborted
  await example.getByRole('option', { name: 'Riley Chen' }).click()
  release()
  await delivered
  await expect(input).toHaveValue('Riley')
  await expect(input).toBeFocused()
  await expect(example.getByRole('option', { name: 'Jamie Lee' })).toHaveCount(0)
  await expect.poll(() => values(example, 'remoteMemberIds')).toEqual(['alex', 'riley'])
  expect(errors).toEqual([])
})

test('remote whole-field morph preserves query, focus, and selections absent from results', crossBrowser, async ({ page }) => {
  let release!: () => void
  const gate = new Promise<void>(resolve => { release = resolve })
  let received!: () => void
  const held = new Promise<void>(resolve => { received = resolve })
  await page.route('**/components/members/field?*', async route => {
    const response = await route.fetch()
    received()
    await gate
    await route.fulfill({ response })
  })
  await page.goto('/components/select')
  const example = page.locator('#components-select-search-multiple-remote')
  const trigger = example.getByRole('button', { name: 'Remote members', exact: true })
  await trigger.click()
  const input = example.getByRole('searchbox', { name: 'Search Remote members', exact: true })
  await input.fill('Jamie')
  await example.getByRole('option', { name: 'Jamie Lee' }).click()
  await example.getByRole('button', { name: 'Refresh members' }).click()
  await held
  const response = page.waitForResponse(r => new URL(r.url()).pathname === '/components/members/field')
  release()
  await response
  await example.getByRole('button', { name: 'Remote members', exact: true }).click()
  const refreshedInput = example.getByRole('searchbox', { name: 'Search Remote members', exact: true })
  await expect(refreshedInput).toBeFocused()
  await expect(refreshedInput).toHaveValue('Jamie')
  await expect.poll(() => values(example, 'remoteMemberIds')).toEqual(['alex', 'jamie'])
  await expect(example.getByRole('button', { name: 'Clear Remote members', exact: true })).toBeEnabled()
  await input.click()
  await expect(example.getByRole('option', { name: 'Jamie Lee' })).toHaveAttribute('aria-selected', 'true')
  await expect(example.getByRole('option', { name: 'Alex Morgan' })).toHaveCount(0)
})

test('remote requests cancel on same-document navigation without resurrecting removed fields', crossBrowser, async ({ page }) => {
  let release!: () => void
  const gate = new Promise<void>(resolve => { release = resolve })
  let received!: () => void
  const held = new Promise<void>(resolve => { received = resolve })
  let finished!: () => void
  const delivered = new Promise<void>(resolve => { finished = resolve })
  await page.route('**/components/members/search?*', async route => {
    const response = await route.fetch()
    received()
    await gate
    await route.fulfill({ response })
    finished()
  })
  await page.goto('/components/select')
  await page.evaluate(() => { (window as any).multipleChoiceSession = 'retained' })
  const trigger = page.getByRole('button', { name: 'Remote members', exact: true })
  await trigger.click()
  const input = page.getByRole('searchbox', { name: 'Search Remote members', exact: true })
  await input.fill('Jamie')
  await held
  const aborted = page.waitForEvent('requestfailed', { predicate: request => new URL(request.url()).pathname === '/components/members/search' })
  await page.locator('a[href="/components/select"]').first().click()
  await expect(page).toHaveURL(/\/components\/select$/)
  await aborted
  release()
  await delivered
  expect(await page.evaluate(() => (window as any).multipleChoiceSession)).toBe('retained')
  await expect(page.getByRole('searchbox', { name: 'Search Remote members', exact: true })).toHaveCount(0)
})

for (const [width, scale] of [[1440, 1], [390, 1], [320, 2]]) {
  test(`multiple Select modes remain accessible without overflow at ${width}px ${scale}x`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 1000 })
    await page.goto('/components/select')
    for (const [id, label] of [['components-select-multiple', 'Team members'], ['components-select-search-multiple', 'Search members']] as const) {
      const example = page.locator(`#${id}`)
      for (const dark of [false, true]) {
        await page.evaluate(({ dark, scale }) => {
          document.documentElement.classList.toggle('dark', dark)
          document.documentElement.style.fontSize = `${100 * scale}%`
        }, { dark, scale })
        const control = example.getByRole('button', { name: label, exact: true })
        await control.click()
        await expect(example.getByRole('listbox')).toBeVisible()
        await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
        const box = (await example.getByRole('listbox').boundingBox())!
        expect(box.x).toBeGreaterThanOrEqual(0)
        expect(box.x + box.width).toBeLessThanOrEqual(width + 1)
        expect((await new AxeBuilder({ page }).include(`#${id}`).analyze()).violations).toEqual([])
        await control.press('Escape')
        await example.scrollIntoViewIfNeeded()
        await page.screenshot({ path: testInfo.outputPath(`${id}-${dark ? 'dark' : 'light'}-${width}-${scale}x.png`) })
      }
    }
  })
}
