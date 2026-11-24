import { test, expect } from '@playwright/test'

test('Rocket discovery avoids repeatedly rescanning search groups @cross-browser', async ({ page }) => {
  await page.goto('/components/button')
  await page.getByRole('button', { name: 'Search documentation', exact: true }).click()
  const input = page.locator('#docs-search-input')
  await expect(input).toBeFocused()
  await page.evaluate(() => {
    const query = Element.prototype.querySelectorAll
    const scans = new Map<Element, number>()
    ;(window as any).discoveryScans = scans
    Element.prototype.querySelectorAll = function (selector: string) {
      if (selector === '*' && this.matches('[data-fve-command-group]')) scans.set(this, (scans.get(this) ?? 0) + 1)
      return query.call(this, selector)
    }
    ;(window as any).restoreDiscoveryQueries = () => { Element.prototype.querySelectorAll = query }
  })
  try {
    for (const query of ['b', 'bu', 'but', 'button']) {
      await input.fill(query)
      await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))))
    }
    const scans = await page.evaluate(() => [...(window as any).discoveryScans.values()] as number[])
    // Upstream now scans inserted subtrees rather than rescanning the whole group.
    // Zero group traversals is valid; still guard against one traversal per moved result.
    expect(Math.max(0, ...scans)).toBeLessThanOrEqual(8)
    await expect(page.locator('#docs-search').getByRole('option', { name: 'Button', exact: true })).toBeVisible()
  } finally {
    await page.evaluate(() => (window as any).restoreDiscoveryQueries())
  }
})

test('Rocket releases light and shadow roots, bindings and removed DOM', async ({ page, context }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  // Reuse the Example's existing module, rather than loading another Datastar runtime.
  await page.goto('/examples/application/accounts')
  await page.waitForFunction(() => customElements.get('fve-button'))
  const lifecycle = await page.evaluate(async () => {
    const { rocket, mergePatch, getPath } = await import('/examples/js/datastar-rocket.1.0.5.js')
    mergePatch({ runtimeRootProbe: 0 })
    for (const mode of ['light', 'open'] as const) {
      rocket(`runtime-probe-${mode}`, {
        mode,
        setup: ({ $$ }: any) => { $$('count', 0) },
        render: ({ html }: any) => html`<button data-on:click="$$count++"><span data-text="$$count"></span></button>`,
      })
    }
    const settle = () => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))
    const host = document.createElement('runtime-probe-open')
    document.body.append(host)
    await settle()
    const button = host.shadowRoot!.querySelector('button')!
    button.click()
    await settle()
    const clicked = button.textContent
    // Third-party content in a shadow root still receives Datastar activation.
    const added = document.createElement('button')
    added.setAttribute('data-on:click', '$runtimeRootProbe++')
    host.shadowRoot!.append(added)
    await settle()
    added.click()
    const connectedProbe = getPath('runtimeRootProbe')
    host.remove()
    await settle()
    added.setAttribute('data-on:click', '$runtimeRootProbe += 10')
    await settle()
    added.click()
    const detachedProbe = getPath('runtimeRootProbe')
    document.body.append(host)
    await settle()
    const reconnected = host.shadowRoot!.querySelector('button')!
    const reset = reconnected.textContent
    reconnected.click()
    await settle()
    const reconnectedClick = reconnected.textContent
    host.remove()
    await settle()
    const weak: WeakRef<Node>[] = []
    for (let i = 0; i < 12; i++) {
      for (const mode of ['light', 'open']) {
        const item = document.createElement(`runtime-probe-${mode}`)
        document.body.append(item)
        weak.push(new WeakRef(item))
        if (item.shadowRoot) weak.push(new WeakRef(item.shadowRoot))
        item.remove()
      }
    }
    ;(window as any).removedRuntimeNodes = weak
    await settle()
    return { clicked, connectedProbe, detachedProbe, reset, reconnectedClick }
  })
  expect(lifecycle).toEqual({ clicked: '1', connectedProbe: 1, detachedProbe: 1, reset: '0', reconnectedClick: '1' })
  const cdp = await context.newCDPSession(page)
  await cdp.send('HeapProfiler.collectGarbage')
  expect(await page.evaluate(() => (window as any).removedRuntimeNodes.filter((ref: WeakRef<Node>) => ref.deref()).length)).toBe(0)
  expect(errors).toEqual([])
})
