import { test, expect } from '@playwright/test'

const avatarSelector = 'fve-avatar[aria-label="FSharp.ViewEngine"]'

test('Avatar owns image loading, fallback, request policy and morph recovery @cross-browser', async ({ page }) => {
  const imageRequest = page.waitForRequest(request => new URL(request.url()).pathname === '/android-chrome-512x512.png')
  await page.goto('/components/avatar')
  const avatar = page.locator(avatarSelector)
  const image = avatar.locator('img')
  const fallback = avatar.locator(':scope > span')
  await expect(avatar).toHaveAttribute('role', 'img')
  await expect(image).toBeVisible()
  await expect(fallback).toHaveText('FV')
  await expect(fallback).toBeHidden()
  expect((await imageRequest).headers().referer ?? '').toBe('')
  await expect(image).toHaveAttribute('referrerpolicy', 'no-referrer')
  expect(await avatar.getAttribute('referrerpolicy')).toBeNull()
  const originalSize = await avatar.boundingBox()

  let release!: () => void
  let requested!: () => void
  const held = new Promise<void>(resolve => { release = resolve })
  const started = new Promise<void>(resolve => { requested = resolve })
  await page.route('**/android-chrome-512x512.png?avatar=slow', async route => {
    requested()
    await held
    await route.continue()
  })
  try {
    await image.evaluate(el => { el.setAttribute('src', '/android-chrome-512x512.png?avatar=slow') })
    await started
    await expect(image).toBeHidden()
    await expect(fallback).toBeVisible()
  } finally { release() }
  await expect(image).toBeVisible()

  await page.route('**/android-chrome-512x512.png?avatar=failed', route => route.abort())
  await image.evaluate(el => { el.setAttribute('src', '/android-chrome-512x512.png?avatar=failed') })
  await expect(image).toBeHidden()
  await expect(fallback).toBeVisible()
  await image.evaluate(el => { el.setAttribute('src', '/android-chrome-512x512.png') })
  await expect(image).toBeVisible()
  // A server morph may restore the authored hidden attribute without changing the source.
  await image.evaluate(el => { (el as HTMLImageElement).hidden = true })
  await expect(image).toBeVisible()
  await avatar.evaluate(el => el.replaceWith(el.cloneNode(true)))
  await expect(image).toBeVisible()
  const currentSize = await avatar.boundingBox()
  expect(currentSize!.width).toBe(originalSize!.width)
  expect(currentSize!.height).toBe(originalSize!.height)
  const failed = page.locator('fve-avatar[aria-label="Unavailable profile image"]')
  await expect(failed.locator('img')).toBeHidden()
  await expect(failed.locator(':scope > span')).toHaveText('AM')
})

for (const success of [true, false]) test(`Avatar handles image ${success ? 'success' : 'failure'} before application enhancement @cross-browser`, async ({ page }) => {
  let release!: () => void
  const held = new Promise<void>(resolve => { release = resolve })
  await page.route('**/assets/app.js', async route => { await held; await route.continue() })
  if (!success) await page.route('**/android-chrome-512x512.png', route => route.abort())
  try {
    await page.goto('/components/avatar', { waitUntil: 'commit' })
    const avatar = page.locator(avatarSelector)
    await expect(avatar.locator(':scope > span')).toHaveText('FV')
    await page.waitForFunction(({ selector, success }) => {
      const image = document.querySelector<HTMLImageElement>(selector + ' img')
      return image?.complete && (image.naturalWidth > 0) === success
    }, { selector: avatarSelector, success })
    await expect(avatar.locator('img')).toBeHidden()
    release()
    await page.waitForFunction(() => customElements.get('fve-avatar') !== undefined)
    if (success) {
      await expect(avatar.locator('img')).toBeVisible()
      await expect(avatar.locator(':scope > span')).toBeHidden()
    } else {
      await expect(avatar.locator('img')).toBeHidden()
      await expect(avatar.locator(':scope > span')).toBeVisible()
    }
  } finally { release() }
})

test('Avatar keeps a named native fallback without JavaScript', async ({ browser, baseURL }) => {
  const context = await browser.newContext({ baseURL, javaScriptEnabled: false })
  try {
    const page = await context.newPage()
    await page.goto('/components/avatar')
    for (const name of ['FSharp.ViewEngine', 'Unavailable profile image']) {
      const avatar = page.locator(`fve-avatar[aria-label="${name}"]`)
      await expect(avatar).toHaveAttribute('role', 'img')
      await expect(avatar.locator(':scope > span')).toBeVisible()
      await expect(avatar.locator('img')).toBeHidden()
    }
  } finally { await context.close() }
})
