import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

test('installed InputGroup keeps multiple addons operable at narrow enlarged widths', async ({ page, browserName }, testInfo) => {
  await page.goto('/')
  const form = page.locator('#generated-input-group')
  const input = form.getByRole('searchbox', { name: 'Search records', exact: true })
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

test('packed fve output renders a styled accessible generated consumer', async ({ page }, testInfo) => {
  await page.goto('/')

  await expect(page).toHaveTitle('Generated fve consumer')
  await expect(page.getByRole('heading', { level: 1, name: 'Generated consumer' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Create account' }).first()).toBeVisible()
  const iconButton = page.getByRole('button', { name: 'Refresh accounts', exact: true })
  await expect(iconButton).toBeVisible()
  await expect(iconButton.locator('svg')).toHaveCount(1)
  await expect(page.getByRole('button', { name: 'Refresh balances', exact: true }).locator('svg')).toHaveCount(1)
  await expect(page.getByRole('button', { name: 'Sync now', exact: true }).locator('svg')).toHaveCount(1)
  await expect(page.getByRole('button', { name: 'Custom content', exact: true })).toBeVisible()
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
