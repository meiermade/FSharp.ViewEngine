import { test, expect } from '@playwright/test'

test('Rocket Button keeps native validation, submitter overrides, reset and disabled fieldsets @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  await page.evaluate(() => {
    const fixture = document.createElement('section')
    fixture.id = 'button-native-contract'
    fixture.innerHTML = `<form id="button-contract-form" action="/components/button" method="get"><input name="account" aria-label="Contract account" required></form>
      <fve-button type="submit" name="action" value="save" form="button-contract-form" formaction="/components/button" formmethod="get">Contract save</fve-button>
      <fve-button type="reset" form="button-contract-form">Contract reset</fve-button>
      <fieldset disabled><fve-button type="submit" form="button-contract-form">Contract disabled</fve-button></fieldset>`
    fixture.querySelector('form')!.addEventListener('submit', event => {
      event.preventDefault()
      const submitter = (event as SubmitEvent).submitter as HTMLButtonElement
      fixture.dataset.submission = JSON.stringify({
        tag: submitter.tagName,
        action: new URL(submitter.formAction).pathname,
        method: submitter.formMethod,
        values: [...new FormData(submitter.form!, submitter)],
      })
    })
    document.body.append(fixture)
  })
  const fixture = page.locator('#button-native-contract')
  const save = fixture.getByRole('button', { name: 'Contract save', exact: true })
  const account = fixture.getByRole('textbox', { name: 'Contract account' })
  await save.click()
  await expect(account).toBeFocused()
  await expect(fixture).not.toHaveAttribute('data-submission')
  await account.fill('Cash')
  await save.focus()
  await page.keyboard.press('Enter')
  await expect(fixture).toHaveAttribute('data-submission', JSON.stringify({ tag: 'BUTTON', action: '/components/button', method: 'get', values: [['account', 'Cash'], ['action', 'save']] }))
  await fixture.getByRole('button', { name: 'Contract reset', exact: true }).click()
  await expect(account).toHaveValue('')
  await expect(fixture.getByRole('button', { name: 'Contract disabled', exact: true })).toBeDisabled()
  await save.evaluate(el => el.parentElement!.setAttribute('disabled', ''))
  await expect(save).toBeDisabled()
  await save.evaluate(el => el.parentElement!.removeAttribute('disabled'))
  await expect(save).toBeEnabled()
  await save.evaluate(el => el.parentElement!.setAttribute('hidden', ''))
  await expect(save).toBeHidden()
})

test('Rocket Button rebuilds server-morphed content and remains interactive without stale props or handlers @cross-browser', async ({ page }) => {
  const raw = await (await page.request.get('/components/button')).text()
  await page.goto('/components/button')
  const region = page.locator('#components-button-basic-panel-preview')
  const button = region.getByRole('button')
  const replacement = await page.evaluate(raw => {
    const doc = new DOMParser().parseFromString(raw, 'text/html')
    const host = doc.querySelector('#components-button-basic-panel-preview fve-button')!
    host.setAttribute('color', 'error')
    host.setAttribute('variant', 'soft')
    host.setAttribute('size', 'large')
    host.innerHTML = '<strong>Synced</strong><span aria-hidden="true"> ✓</span>'
    return host.outerHTML
  }, raw)
  let requests = 0
  await page.route('**/components/button/settled*', async route => {
    requests++
    await route.fulfill({ status: 200, contentType: 'text/event-stream', body: `event: datastar-patch-elements\ndata: selector #components-button-basic-panel-preview fve-button\ndata: elements ${replacement}\n\n` })
  })
  await button.focus()
  await page.keyboard.press('Space')
  await expect(button).toHaveAccessibleName('Synced')
  await expect(button).toBeEnabled()
  await expect(button).toBeFocused()
  await expect(button).toHaveCSS('height', '36px')
  await expect(button.locator('strong')).toHaveText('Synced')
  await expect(region.locator('fve-button')).toHaveCSS('display', 'contents')
  await expect(button.locator('button')).toHaveCount(0)
  await button.click()
  await expect.poll(() => requests).toBe(2)
  await expect(button).toBeEnabled()
  await expect(region.getByRole('button')).toHaveCount(1)
  // A disconnected/reconnected instance must retain the latest server-owned content, not its initial children.
  await button.evaluate(el => { const host = el.parentElement!; const parent = host.parentElement!; host.remove(); parent.append(host) })
  await expect(button).toHaveAccessibleName('Synced')
  await button.click()
  await expect.poll(() => requests).toBe(3)
  await expect(button).toBeEnabled()
})

test('new Rocket buttons keep their settled border during Dialog/Drawer navigation @cross-browser', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark', reducedMotion: 'no-preference' })
  await page.goto('/components/drawer')
  const drawer = page.getByRole('button', { name: 'Open drawer', exact: true })
  await expect(drawer).toBeVisible()
  await expect.poll(() => drawer.evaluate(button => button.getAnimations().length)).toBe(0)
  const border = await drawer.evaluate(button => getComputedStyle(button).borderTopColor)
  await page.evaluate(() => {
    const state = window as Window & { buttonEntryFrames: { id: string; border: string }[]; buttonEntrySampling: boolean }
    state.buttonEntryFrames = []
    state.buttonEntrySampling = true
    const sample = () => {
      for (const id of ['default-dialog-trigger', 'default-drawer-trigger']) {
        const button = document.getElementById(id)
        if (button) state.buttonEntryFrames.push({ id, border: getComputedStyle(button).borderTopColor })
      }
      if (state.buttonEntrySampling) requestAnimationFrame(sample)
    }
    requestAnimationFrame(sample)
  })
  for (const component of ['dialog', 'drawer', 'dialog', 'drawer']) {
    const root = page.locator('[data-fve-navigation-root]')
    const previousDocument = await root.getAttribute('data-navigation-document')
    await page.locator(`#nav-components-${component}`).click()
    await expect.poll(() => root.getAttribute('data-navigation-document')).not.toBe(previousDocument)
    await expect(page.getByRole('button', { name: `Open ${component}`, exact: true })).toBeVisible()
    await page.evaluate(() => new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve()))))
  }
  const frames = await page.evaluate(() => {
    const state = window as Window & { buttonEntryFrames: { id: string; border: string }[]; buttonEntrySampling: boolean }
    state.buttonEntrySampling = false
    return state.buttonEntryFrames
  })
  expect(new Set(frames.map(frame => frame.id))).toEqual(new Set(['default-dialog-trigger', 'default-drawer-trigger']))
  expect(frames.filter(frame => frame.border !== border)).toEqual([])
  await drawer.evaluate(button => button.addEventListener('transitionrun', event => {
    if ((event as TransitionEvent).propertyName === 'background-color') button.setAttribute('data-hover-transition', 'true')
  }))
  await drawer.hover()
  await expect(drawer).toHaveAttribute('data-hover-transition', 'true')
})
