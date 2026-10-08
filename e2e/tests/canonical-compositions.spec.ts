import { test, expect } from '../fixture'

test('SideNav aligns plain items at every depth and retains guide gutters @cross-browser', async ({ page }) => {
  await page.goto('/components/side-nav')
  const nav = page.locator('#structure-navigation')
  await expect(nav.getByRole('link', { name: 'Button', exact: true })).toBeVisible()
  const geometry = await nav.evaluate(el => {
    const links = [...el.querySelectorAll('a')]
    const documentation = el.querySelector('summary[aria-label="Toggle Documentation section"]')!
    const components = el.querySelector('summary[aria-label="Toggle Components section"]')!
    const edge = (summary: Element) => {
      const svg = summary.querySelector('svg')!, box = svg.querySelector('path')!.getBBox()
      return Math.min(...[[box.x, box.y], [box.x + box.width, box.y], [box.x, box.y + box.height], [box.x + box.width, box.y + box.height]].map(([x, y]) => new DOMPoint(x, y).matrixTransform(svg.getScreenCTM()!).x))
    }
    const labelX = (name: string) => el.querySelector(`a[aria-label="${name}"]`)!.firstElementChild!.getBoundingClientRect().left
    const guides = [...el.querySelectorAll('details[open]')].map(details => {
      const svg = details.querySelector(':scope > summary svg')! as SVGSVGElement
      const ul = details.querySelector(':scope > ul')!, box = ul.getBoundingClientRect(), border = parseFloat(getComputedStyle(ul).borderLeftWidth)
      const center = new DOMPoint(10, 10).matrixTransform(svg.getScreenCTM()!).x
      return { alignment: Math.abs(box.left + border / 2 - center), gutters: [...ul.children].map(li => li.querySelector('summary, a')!).map(row => row.getBoundingClientRect().left - box.left - border) }
    })
    return { insets: links.map(link => getComputedStyle(link).paddingLeft), root: Math.abs(labelX('Home') - edge(documentation)), nested: Math.abs(labelX('Specification') - edge(components)), guides }
  })
  expect(new Set(geometry.insets).size).toBe(1)
  expect(geometry.root).toBeLessThan(0.5)
  expect(geometry.nested).toBeLessThan(0.5)
  for (const guide of geometry.guides) {
    expect(guide.alignment).toBeLessThan(0.5)
    for (const gutter of guide.gutters) expect(gutter).toBe(4)
  }
  const group = nav.getByLabel('Toggle Components section', { exact: true })
  await group.focus()
  await page.keyboard.press('Space')
  await expect(nav.getByRole('link', { name: 'Button', exact: true })).toBeHidden()
  await page.keyboard.press('Enter')
  await expect(nav.getByRole('link', { name: 'Button', exact: true })).toBeVisible()
})

test('joined Select filters submit one value and retain other public controls @cross-browser', async ({ page }) => {
  await page.goto('/examples/application/accounts?filters=accountType&search=checking&sort=balance')
  const group = page.getByRole('group', { name: 'Type filter', exact: true })
  const select = group.getByRole('combobox')
  await expect(select).toBeVisible()
  expect(await select.evaluate(el => new FormData(el.closest('form')!).getAll('accountType'))).toEqual(['all'])
  await select.press('ArrowDown')
  await group.getByRole('option', { name: 'Asset', exact: true }).click()
  await expect(page).toHaveURL(url => url.searchParams.get('accountType') === 'Asset')
  await group.getByRole('button', { name: 'Remove Type filter', exact: true }).press('Enter')
  await expect(group).toHaveCount(0)
  expect(new URL(page.url()).searchParams.get('search')).toBe('checking')
  expect(new URL(page.url()).searchParams.get('sort')).toBe('balance')
})

test('account drawer preserves background context and guards dirty dismissal @cross-browser', async ({ page }) => {
  await page.goto('/examples/application/accounts?search=checking&sort=balance')
  await page.getByRole('button', { name: 'Choose color theme', exact: true }).click()
  await page.getByRole('menuitemradio', { name: 'Dark', exact: true }).click()
  const title = await page.title()
  const crumbs = await page.getByRole('navigation', { name: 'Breadcrumb', exact: true }).innerText()
  await page.getByRole('link', { name: 'Create account', exact: true }).click()
  const drawer = page.getByRole('dialog', { name: 'Create account', exact: true })
  await expect(drawer).toBeVisible()
  expect(await page.title()).toBe(title)
  expect(await page.getByRole('navigation', { name: 'Breadcrumb', exact: true }).innerText()).toBe(crumbs)
  const name = drawer.getByRole('textbox', { name: /^Name/ })
  await expect(name).toBeFocused()
  await name.fill('Unsaved reserve')
  page.once('dialog', async prompt => { expect(prompt.message()).toBe('Discard unsaved changes?'); await prompt.dismiss() })
  await page.keyboard.press('Escape')
  await expect(drawer).toBeVisible()
  await expect(name).toHaveValue('Unsaved reserve')
  page.once('dialog', async prompt => { expect(prompt.message()).toBe('Discard unsaved changes?'); await prompt.accept() })
  await drawer.getByRole('link', { name: 'Cancel', exact: true }).click()
  await expect(drawer).toHaveCount(0)
  expect(new URL(page.url()).searchParams.get('search')).toBe('checking')
  expect(new URL(page.url()).searchParams.get('sort')).toBe('balance')
  await page.getByRole('link', { name: 'Create account', exact: true }).click()
  await drawer.getByRole('textbox', { name: /^Name/ }).fill('Operating checking')
  await drawer.getByRole('button', { name: 'Save', exact: true }).click()
  await expect(drawer.getByRole('textbox', { name: /^Name/ })).toHaveValue('Operating checking')
  expect(await page.title()).toBe(title)
  expect(await page.getByRole('navigation', { name: 'Breadcrumb', exact: true }).innerText()).toBe(crumbs)
  await expect(page.locator('html')).toHaveClass(/dark/)
})
