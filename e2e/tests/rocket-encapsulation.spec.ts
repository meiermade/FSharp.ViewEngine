import { test, expect } from '@playwright/test'

test('Rocket enhancement keeps caller bindings outside private per-instance state @cross-browser', async ({ page }) => {
  await page.route('**/assets/app.js', route => route.fulfill({ status: 200, contentType: 'text/javascript', body: "export * from '/examples/assets/scripts/datastar-rocket.1.0.4.js';" }))
  await page.goto('/components/card')
  await page.evaluate(async () => {
    const runtime = await import('/assets/app.js' as string)
    runtime.rocket('fve-caller-scope-probe', {
      mode: 'light', scope: false,
      setup: ({ host, $$, effect, cleanup }: any) => {
        $$('open', false)
        effect(() => host.setAttribute('data-open', String($$.open)))
        const click = () => { $$.open = !$$.open }
        host.addEventListener('click', click)
        cleanup(() => host.removeEventListener('click', click))
      },
    })
    const fragment = document.createElement('section')
    fragment.innerHTML = `<fve-caller-scope-probe id="scope-first"><div data-signals="{callerDraft: 'Caller draft'}"><input aria-label="Caller draft" data-bind="callerDraft"><button type="button">First instance</button></div></fve-caller-scope-probe><fve-caller-scope-probe id="scope_first"><button type="button">Second instance</button></fve-caller-scope-probe><output aria-label="Outer draft" data-text="$callerDraft"></output>`
    document.querySelector('main')!.prepend(fragment)
  })
  await expect(page.getByRole('textbox', { name: 'Caller draft', exact: true })).toHaveValue('Caller draft')
  await expect(page.getByLabel('Outer draft', { exact: true })).toHaveText('Caller draft')
  await page.getByRole('button', { name: 'First instance', exact: true }).click()
  await expect(page.locator('#scope-first')).toHaveAttribute('data-open', 'true')
  await expect(page.locator('#scope_first')).toHaveAttribute('data-open', 'false')
  await page.getByRole('textbox', { name: 'Caller draft', exact: true }).fill('Edited outside private state')
  await expect(page.getByLabel('Outer draft', { exact: true })).toHaveText('Edited outside private state')
  await page.locator('#scope-first').evaluate(host => host.dispatchEvent(new Event('datastar-scope-children')))
  await expect(page.getByLabel('Outer draft', { exact: true })).toHaveText('Edited outside private state')
  await expect(page.locator('#scope-first')).toHaveAttribute('data-open', 'true')
})

test('Button and Chart retain application bindings in projected content @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  await page.evaluate(() => {
    const region = document.createElement('section')
    region.innerHTML = ['button', 'chart'].map(kind => `<fve-${kind} id="${kind}-caller-binding" ${kind === 'chart' ? 'aria-label="Bound chart fallback" options="{}" empty="true"' : ''}><span data-signals="{${kind}Draft: 'Caller draft'}"><span data-text="$${kind}Draft"></span></span></fve-${kind}><input aria-label="Projected ${kind} draft" data-bind="${kind}Draft"><output aria-label="Shared ${kind} draft" data-text="$${kind}Draft"></output>`).join('')
    document.querySelector('main')!.prepend(region)
  })
  for (const kind of ['button', 'chart']) {
    const content = page.locator(`#${kind}-caller-binding [data-text]`)
    await expect(content).toHaveText('Caller draft')
    await expect(page.getByLabel(`Shared ${kind} draft`, { exact: true })).toHaveText('Caller draft')
    await page.getByRole('textbox', { name: `Projected ${kind} draft`, exact: true }).fill('Edited by the application')
    await expect(content).toHaveText('Edited by the application')
    await expect(page.getByLabel(`Shared ${kind} draft`, { exact: true })).toHaveText('Edited by the application')
  }
})

test('Checkbox follows authoritative mixed state on the same native node @cross-browser', async ({ page }) => {
  await page.goto('/components/checkbox')
  const checkbox = page.getByRole('checkbox', { name: 'Select all items', exact: true })
  await expect(checkbox).toHaveJSProperty('indeterminate', true)
  await checkbox.evaluate(el => { (window as any).mixedNativeNode = el })
  let markup = ''
  await page.route('**/components/checkbox?mixed-patch=*', route => route.fulfill({
    status: 200, contentType: 'text/event-stream', body: `event: datastar-patch-elements\ndata: elements ${markup}\n\n`,
  }))
  for (const mixed of [false, true, false]) {
    markup = await checkbox.evaluate((el, mixed) => {
      const owner = el.closest('fve-checkbox') || el
      const clone = owner.cloneNode(true) as HTMLElement
      const field = (clone.matches('input') ? clone : clone.querySelector('input')) as HTMLInputElement
      clone.setAttribute('indeterminate', String(mixed))
      field.setAttribute('checked', '')
      field.removeAttribute('data-init')
      if (mixed && clone === field) field.setAttribute('data-init', 'el.indeterminate = true')
      const trigger = document.createElement('button')
      trigger.textContent = 'Patch mixed state'
      trigger.setAttribute('data-on:click', `@get('/components/checkbox?mixed-patch=${mixed}')`)
      document.querySelector('#mixed-state-patch')?.remove()
      trigger.id = 'mixed-state-patch'
      document.body.append(trigger)
      return clone.outerHTML
    }, mixed)
    await page.getByRole('button', { name: 'Patch mixed state', exact: true }).click()
    await expect(checkbox).toHaveJSProperty('indeterminate', mixed)
    await expect(checkbox).toBeChecked()
    expect(await checkbox.evaluate(el => el === (window as any).mixedNativeNode)).toBe(true)
  }
})

for (const multiple of [false, true]) {
  test(`${multiple ? 'MultiToggleGroup' : 'ToggleGroup'} retains values on reconnect and honors native reset cancellation @cross-browser`, async ({ page }) => {
    await page.goto('/components/toggle-group')
    const group = page.locator(multiple ? '#components-formatting-group' : '#components-alignment-group')
    await group.evaluate(el => {
      const form = document.createElement('form')
      form.id = 'toggle-reset-regression'
      el.before(form)
      form.append(el)
    })
    const form = page.locator('#toggle-reset-regression')
    const values = () => form.evaluate(el => [...new FormData(el as HTMLFormElement).values()])
    await group.getByRole('button', { name: multiple ? 'Italic' : 'Center', exact: true }).click()
    const changed = multiple ? ['bold', 'italic'] : ['center']
    await expect.poll(values).toEqual(changed)
    const chosen = group.getByRole('button', { name: multiple ? 'Italic' : 'Center', exact: true })
    for (let reconnect = 0; reconnect < 2; reconnect++) {
      await group.evaluate(owner => {
        const parent = owner.parentNode!, next = owner.nextSibling
        owner.remove()
        parent.insertBefore(owner, next)
      })
      await expect.poll(values).toEqual(changed)
      await expect(chosen).toHaveAttribute('aria-pressed', 'true')
    }
    await form.evaluate(el => {
      el.addEventListener('reset', event => event.preventDefault(), { once: true })
      ;(el as HTMLFormElement).reset()
    })
    await expect.poll(values).toEqual(changed)
    await form.evaluate(el => (el as HTMLFormElement).reset())
    await expect.poll(values).toEqual(multiple ? ['bold'] : ['left'])
    if (!multiple) await group.evaluate(owner => owner.setAttribute('allow-empty', ''))
    await group.getByRole('button', { name: multiple ? 'Bold' : 'Left', exact: true }).click()
    await expect.poll(values).toEqual([])
    await group.evaluate(owner => {
      const parent = owner.parentNode!, next = owner.nextSibling
      owner.remove()
      parent.insertBefore(owner, next)
    })
    await expect.poll(values).toEqual([])
    await expect(group.getByRole('button', { pressed: true })).toHaveCount(0)
    await form.evaluate(el => (el as HTMLFormElement).reset())
    await expect.poll(values).toEqual(multiple ? ['bold'] : ['left'])
    await expect(group.getByRole('button', { name: multiple ? 'Bold' : 'Left', exact: true })).toHaveAttribute('aria-pressed', 'true')
  })
}

for (const multiple of [false, true]) {
  test(`${multiple ? 'MultiSelect' : 'Select'} retains native values through reconnect and canceled reset @cross-browser`, async ({ page }) => {
    await page.goto('/components/select?fixtures=1')
    const region = page.locator(multiple ? '#components-multiple-select-form-region' : '#components-select-form-region')
    const form = region.locator('form')
    const values = () => form.evaluate(el => [...new FormData(el as HTMLFormElement).values()])
    const trigger = region.getByRole(multiple ? 'button' : 'combobox', { name: multiple ? 'Reviewers' : 'Status', exact: true })
    await trigger.click()
    await region.getByRole('option', { name: multiple ? 'Jamie Lee' : 'Pending', exact: true }).click()
    if (multiple) await trigger.press('Escape')
    await expect.poll(values).toEqual([multiple ? 'jamie' : 'pending'])
    await region.locator('fve-select').evaluate(owner => {
      const parent = owner.parentNode!, next = owner.nextSibling
      owner.remove()
      parent.insertBefore(owner, next)
    })
    await expect.poll(values).toEqual([multiple ? 'jamie' : 'pending'])
    await expect(trigger).toContainText(multiple ? 'Jamie Lee' : 'Pending')
    await form.evaluate(el => {
      el.addEventListener('reset', event => event.preventDefault(), { once: true })
      ;(el as HTMLFormElement).reset()
    })
    await expect.poll(values).toEqual([multiple ? 'jamie' : 'pending'])
    await form.evaluate(el => (el as HTMLFormElement).reset())
    await expect.poll(values).toEqual(multiple ? [] : [''])
  })
}

test('native-fallback Select retains pre-enhancement edits and reconnected values with a separate reset baseline @cross-browser', async ({ page }) => {
  let release!: () => void
  const enhance = new Promise<void>(resolve => { release = resolve })
  await page.route('**/assets/app.js', async route => { await enhance; await route.continue() })
  try {
    await page.goto('/components/select?fixtures=1', { waitUntil: 'commit' })
    const native = page.locator('select[name="nativeFrequency"]')
    await native.selectOption('monthly')
    await native.evaluate(el => {
      const owner = el.closest('fve-select')!
      const form = document.createElement('form')
      form.id = 'native-fallback-reconnect'
      owner.before(form)
      form.append(owner)
    })
    const form = page.locator('#native-fallback-reconnect')
    const values = () => form.evaluate(el => new FormData(el as HTMLFormElement).getAll('nativeFrequency'))
    await expect.poll(values).toEqual(['monthly'])
    release()
    const trigger = page.getByRole('combobox', { name: 'Update frequency', exact: true }).and(page.locator('#nativeFrequency'))
    await expect(trigger).toHaveJSProperty('tagName', 'BUTTON')
    await expect(trigger).toContainText('Monthly')
    await expect.poll(values).toEqual(['monthly'])
    await trigger.click()
    await form.getByRole('option', { name: 'Daily', exact: true }).click()
    await expect.poll(values).toEqual(['daily'])
    for (let reconnect = 0; reconnect < 2; reconnect++) {
      await form.locator('fve-select').evaluate(owner => {
        const parent = owner.parentNode!, next = owner.nextSibling
        owner.remove()
        parent.insertBefore(owner, next)
      })
      await expect.poll(values).toEqual(['daily'])
      await expect(trigger).toContainText('Daily')
      await trigger.click()
      await expect(form.getByRole('option', { name: 'Daily', exact: true })).toHaveAttribute('aria-selected', 'true')
      await trigger.press('Escape')
    }
    await form.evaluate(el => {
      el.addEventListener('reset', event => event.preventDefault(), { once: true })
      ;(el as HTMLFormElement).reset()
    })
    await expect.poll(values).toEqual(['daily'])
    await expect(trigger).toContainText('Daily')
    await form.evaluate(el => (el as HTMLFormElement).reset())
    await expect.poll(values).toEqual(['weekly'])
    await expect(trigger).toContainText('Weekly')
  } finally {
    release()
  }
})

test('nested Examples keep their own tabs and panels @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  const outer = page.locator('fve-example').nth(0)
  const inner = page.locator('fve-example').nth(1)
  const innerId = await inner.locator('[role=tab][aria-controls$="-panel-preview"]').first().getAttribute('id')
  await outer.evaluate(owner => {
    const inner = document.querySelectorAll('fve-example')[1]
    owner.querySelector('[data-docs-preview-frame]')!.append(inner)
  })
  const tabs = outer.locator(':scope > [data-docs-example-toolbar]').getByRole('tablist')
  await tabs.getByRole('tab', { name: 'Code', exact: true }).click()
  await expect(outer.locator(':scope > [data-docs-example-code]')).toBeVisible()
  await expect(page.locator(`[id="${innerId}"]`)).toHaveAttribute('aria-selected', 'true')
  await tabs.getByRole('tab', { name: 'Preview', exact: true }).click()
  await expect(outer.locator(':scope > [data-docs-example-preview]')).toBeVisible()
})

test('nested Resizable owners adjust only their own separator and panels @cross-browser', async ({ page }) => {
  await page.goto('/components/resizable')
  const outer = page.locator('fve-resizable').first()
  const inner = page.locator('fve-resizable').nth(1)
  await inner.evaluate(owner => { owner.setAttribute('data-nested-resize-probe', '') })
  await outer.evaluate(owner => {
    const inner = document.querySelector('[data-nested-resize-probe]')!
    owner.querySelector('[data-fve-resize-leading]')!.append(inner)
  })
  const handle = outer.locator(':scope > [data-fve-resize-handle]')
  const nestedHandle = page.locator('[data-nested-resize-probe] > [data-fve-resize-handle]')
  const before = Number(await handle.getAttribute('aria-valuenow'))
  const nestedBefore = await nestedHandle.getAttribute('aria-valuenow')
  await handle.press(await handle.getAttribute('aria-orientation') === 'vertical' ? 'ArrowRight' : 'ArrowDown')
  await expect(handle).toHaveAttribute('aria-valuenow', String(before + 2))
  await expect(nestedHandle).toHaveAttribute('aria-valuenow', nestedBefore!)
})
