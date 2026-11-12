import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

test('installed InputGroup keeps multiple addons operable at narrow enlarged widths', async ({ page, browserName }, testInfo) => {
  await page.goto('/')
  const form = page.locator('#generated-input-group')
  const input = form.getByRole('textbox', { name: 'Search records', exact: true })
  const clear = form.getByRole('button', { name: 'Clear', exact: true })
  const inputBox = (await input.boundingBox())!
  const addons = [form.getByText('In', { exact: true }), form.getByText('Accounts', { exact: true }), clear]
  for (const addon of addons) {
    const box = (await addon.boundingBox())!
    expect(Math.abs(box.y + box.height / 2 - inputBox.y - inputBox.height / 2)).toBeLessThanOrEqual(1)
  }

  await page.setViewportSize({ width: 320, height: 844 })
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%' })
  await expect(input).toBeVisible()
  const frame = (await form.locator('.fve-input-group').boundingBox())!
  const narrow = (await input.boundingBox())!
  const fontSize = await input.evaluate(el => Number.parseFloat(getComputedStyle(el).fontSize))
  expect(narrow.width).toBeGreaterThanOrEqual(4 * fontSize)
  for (const control of [input, ...addons]) {
    const box = (await control.boundingBox())!
    expect(box.x).toBeGreaterThanOrEqual(frame.x)
    expect(box.x + box.width).toBeLessThanOrEqual(frame.x + frame.width + 1)
  }
  expect((await clear.boundingBox())!.y).toBeGreaterThanOrEqual(narrow.y + narrow.height)
  await input.fill('Checking')
  await expect(input).toHaveValue('Checking')
  expect(await form.evaluate(el => [...new FormData(el as HTMLFormElement).entries()])).toEqual([['recordSearch', 'Checking']])
  // macOS WebKit uses Option+Tab to include native buttons in keyboard navigation.
  await input.press(browserName === 'webkit' ? 'Alt+Tab' : 'Tab')
  await expect(clear).toBeFocused()
  await clear.click()
  await expect(input).toHaveValue('')
  const screenshot = testInfo.outputPath('input-group-320-enlarged.png')
  await form.screenshot({ path: screenshot })
  await testInfo.attach('input-group-320-enlarged', { path: screenshot, contentType: 'image/png' })
})

test('installed FileSelection keeps native selection and built-in contextual clearing', async ({ page }) => {
  await page.goto('/')
  const region = page.locator('#generated-file-selection')
  const input = region.getByLabel('Statement file', { exact: true })
  const clear = region.getByRole('button', { name: /^Clear Statement file selection/, includeHidden: true })
  await expect(clear).toBeHidden()
  await input.setInputFiles({ name: 'statement.csv', mimeType: 'text/csv', buffer: Buffer.from('date,amount') })
  await expect(clear).toBeVisible()
  await expect(clear).toHaveAccessibleName('Clear Statement file selection: statement.csv')
  await expect(region.locator('ul')).toHaveCount(0)
  await clear.click()
  await expect(input).toHaveValue('')
  await expect(input).toBeFocused()
  await expect(clear).toBeHidden()
})

test('packed fve output renders a styled accessible generated consumer', async ({ page }, testInfo) => {
  await page.goto('/')

  await expect(page).toHaveTitle('Generated fve consumer')
  await expect(page.getByRole('heading', { level: 1, name: 'Generated consumer' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Create account' }).first()).toBeVisible()
  const iconButton = page.getByRole('button', { name: 'Refresh accounts', exact: true })
  await expect(iconButton).toBeVisible()
  await expect(iconButton.locator('svg')).toHaveCount(1)
  await expect(iconButton).toHaveCSS('width', '32px')
  await expect(iconButton).toHaveCSS('height', '28px')
  for (const [label, pixels, inline, block] of [['XS', 24, 7, 3], ['S', 28, 7, 3], ['M', 32, 9, 5], ['L', 36, 11, 7], ['XL', 40, 13, 9]] as const) {
    const control = page.locator('#generated-button-sizes').getByRole('button', { name: `Size ${label}`, exact: true })
    await expect(control).toHaveCSS('height', `${pixels}px`)
    await expect(control).toHaveCSS('padding-left', `${inline}px`)
    await expect(control).toHaveCSS('padding-top', `${block}px`)
  }
  await expect(page.getByRole('button', { name: 'Refresh balances', exact: true }).locator('svg')).toHaveCount(1)
  await expect(page.getByRole('button', { name: 'Sync now', exact: true }).locator('svg')).toHaveCount(1)
  await expect(page.getByRole('button', { name: 'Custom content', exact: true })).toBeVisible()
  const popover = page.locator('#generated-popover-content')
  await expect(popover).toBeHidden()
  const popoverTrigger = page.getByRole('button', { name: 'Open details', exact: true })
  for (let repeat = 0; repeat < 2; repeat++) {
    await popoverTrigger.click()
    await expect(popover).toBeVisible()
    await expect(popover).toHaveCSS('display', 'grid')
    await expect(popover).toHaveCSS('row-gap', '12px')
    await popoverTrigger.click()
    await expect(popover).toBeHidden()
  }
  const dateTrigger = page.getByRole('button', { name: 'Generated date', exact: true })
  await dateTrigger.click()
  const datePanel = page.getByRole('dialog', { name: 'Generated date', exact: true })
  await expect(datePanel).toHaveCSS('padding', '0px')
  await expect(datePanel.locator('[data-fve-month-calendar]')).toHaveCSS('padding', '12px')
  await datePanel.getByRole('button', { name: /^Select today,/ }).click()
  await expect(datePanel).toBeHidden()
  await expect(dateTrigger).toHaveText('11/5/26')
  await expect(page.locator('input[name="generatedDate"]')).toHaveValue('2026-11-05')
  await dateTrigger.click()
  const currentDay = datePanel.getByRole('gridcell', { name: 'Thursday, November 5, 2026', exact: true })
  await expect(currentDay).toHaveAttribute('aria-current', 'date')
  const selectedDateColor = await currentDay.evaluate(e => getComputedStyle(e).backgroundColor)
  await datePanel.getByRole('button', { name: 'Clear', exact: true }).click()
  await expect(currentDay).toHaveAttribute('aria-selected', 'false')
  const currentDayColor = await currentDay.evaluate(e => getComputedStyle(e).backgroundColor)
  expect(currentDayColor).not.toBe(selectedDateColor)
  expect(currentDayColor).not.toBe(await datePanel.getByRole('gridcell', { name: 'Wednesday, November 4, 2026', exact: true }).evaluate(e => getComputedStyle(e).backgroundColor))
  await page.keyboard.press('Escape')
  for (const selector of ['#generated-breadcrumbs-host', '#generated-table', 'fve-tag-input']) {
    expect(await page.locator(selector).evaluate((el: any) => typeof el.rocketInstanceId)).toBe('string')
  }
  const crumbs = page.getByRole('navigation', { name: 'Generated breadcrumb', exact: true })
  await expect(crumbs.getByRole('link', { name: 'Plain code', exact: true })).toBeVisible()
  await crumbs.evaluate(el => { el.style.width = '180px' })
  const more = crumbs.getByRole('button', { name: 'Show hidden breadcrumbs', exact: true })
  await more.click()
  await expect(crumbs.getByRole('menuitem', { name: 'Plain code', exact: true })).toHaveAttribute('href', '/code/plain')
  const codeLink = crumbs.getByRole('menuitem', { name: 'Code', exact: true, includeHidden: true })
  await expect(codeLink).toHaveAttribute('target', '_blank')
  await expect(codeLink).toHaveAttribute('rel', 'noopener')
  await expect(page.locator('#generated-code-link')).toHaveCount(1)
  const opened = page.waitForEvent('popup')
  await codeLink.click()
  const codePage = await opened
  await expect(codePage).toHaveURL(/\/code$/)
  await codePage.close()
  await expect(codeLink).toHaveAttribute('data-activated', 'true')
  await page.keyboard.press('Escape')
  await crumbs.evaluate(el => { el.style.removeProperty('width') })
  await expect(more).toBeHidden()
  const tagEntry = page.getByRole('textbox', { name: 'Generated tags', exact: true })
  await tagEntry.fill('packaged')
  await tagEntry.press('Enter')
  await expect(page.locator('input[name="generatedTags"][value="packaged"]')).toHaveCount(1)
  const binding = page.getByRole('textbox', { name: 'Table binding', exact: true })
  await expect(binding).toHaveValue('before')
  await binding.fill('changed')
  await expect(page.locator('#generated-table-echo')).toHaveText('changed')
  await expect(page.getByRole('combobox', { name: 'Status' })).toHaveText('Active')
  await expect(page.locator('[data-browser-frame="true"]')).toHaveAttribute('data-browser-url', '/')
  await expect(page.locator('[data-fve-phone="true"]')).toBeVisible()
  const notice = page.locator('[data-fve-notice="true"]')
  await expect(notice).toContainText('Generated source is active')
  await expect(notice).toHaveCSS('background-color', 'rgba(0, 0, 0, 0)')
  expect(await notice.evaluate(el => getComputedStyle(el).boxShadow)).toContain('inset')
  await expect(page.locator('[data-fve-notification="true"]')).toContainText('Ready for review')
  const skeleton = page.locator('#generated-skeleton')
  await expect(skeleton).toHaveAttribute('aria-busy', 'true')
  await expect(skeleton.locator('[aria-hidden="true"]')).toHaveCount(2)
  expect((await skeleton.locator('[aria-hidden="true"]').first().boundingBox())!.width).toBeGreaterThan(100)

  const button = page.getByRole('button', { name: 'Create account' }).first()
  const buttonStyle = await button.evaluate((element) => {
    const style = getComputedStyle(element)
    return { backgroundColor: style.backgroundColor, minHeight: style.minHeight }
  })
  expect(buttonStyle.backgroundColor).not.toBe('rgba(0, 0, 0, 0)')
  expect(Number.parseFloat(buttonStyle.minHeight)).toBeGreaterThanOrEqual(24)

  const browserRadius = await page.locator('[data-browser-frame="true"]').evaluate((element) => getComputedStyle(element).borderTopLeftRadius)
  expect(Number.parseFloat(browserRadius)).toBeGreaterThan(0)

  const phoneBox = await page.locator('[data-fve-phone="true"]').boundingBox()
  expect(phoneBox).not.toBeNull()
  expect(phoneBox!.width).toBeGreaterThan(280)
  expect(phoneBox!.width).toBeLessThanOrEqual(310)
  expect(phoneBox!.height).toBeGreaterThan(600)

  const accessibility = await new AxeBuilder({ page }).analyze()
  expect(accessibility.violations).toEqual([])

  const root = page.locator('[data-generated-consumer="true"]')
  const lightBackground = await root.evaluate((element) => getComputedStyle(element).backgroundColor)
  const lightScreenshot = testInfo.outputPath('generated-consumer-light-desktop.png')
  await page.screenshot({ path: lightScreenshot, fullPage: true, animations: 'disabled' })
  await testInfo.attach('generated-consumer-light-desktop', { path: lightScreenshot, contentType: 'image/png' })

  await page.locator('html').evaluate((element) => element.classList.add('dark'))
  const darkBackground = await root.evaluate((element) => getComputedStyle(element).backgroundColor)
  expect(darkBackground).not.toBe(lightBackground)

  await page.setViewportSize({ width: 390, height: 844 })
  await expect(page.locator('main')).toBeVisible()
  const phoneWidth = await page.locator('[data-fve-phone="true"]').evaluate((element) => element.getBoundingClientRect().width)
  expect(phoneWidth).toBeLessThanOrEqual(342)
  const darkMobileScreenshot = testInfo.outputPath('generated-consumer-dark-mobile.png')
  await page.screenshot({ path: darkMobileScreenshot, fullPage: true, animations: 'disabled' })
  await testInfo.attach('generated-consumer-dark-mobile', { path: darkMobileScreenshot, contentType: 'image/png' })
})
