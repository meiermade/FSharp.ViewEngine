import { test, expect } from '../fixture'

test('Progress streams native values through completion and can be run again @cross-browser', async ({ page }) => {
  const errors: string[] = []
  const requests: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  page.on('request', request => {
    if (new URL(request.url()).pathname === '/components/progress/run') requests.push(request.method())
  })
  await page.goto('/components/progress')
  const preview = page.locator('#components-progress-active-panel-preview')
  const progress = preview.getByRole('progressbar', { name: 'Example task', exact: true })
  const region = preview.locator('#progress-example-value')
  const trigger = preview.getByRole('button')
  await expect(progress).toHaveAttribute('value', '0')
  await expect(trigger).toHaveAccessibleName('Run example')
  await trigger.evaluate(el => { el.dataset.progressIdentity = 'retained' })

  for (let run = 1; run <= 2; run++) {
    const response = page.waitForResponse(response => new URL(response.url()).pathname === '/components/progress/run' && response.request().method() === 'POST')
    await trigger.click()
    expect((await response).headers()['content-type']).toContain('text/event-stream')
    await expect(trigger).toBeDisabled()
    await expect(trigger).toHaveAttribute('aria-busy', 'true')
    await expect(progress).toHaveAttribute('value', /^(?:[1-9]\d?)$/)
    await expect(region.getByRole('status')).toHaveCount(0)
    await expect(progress).toHaveAttribute('value', '100')
    await expect(region.getByRole('status')).toHaveText('Complete')
    await expect(trigger).toBeEnabled()
    await expect(trigger).toHaveAccessibleName('Run again')
    await expect(trigger).toHaveAttribute('data-progress-identity', 'retained')
    expect(requests).toEqual(Array(run).fill('POST'))
  }
  expect(errors).toEqual([])
})

test('leaving Progress cancels its stream without updating a fresh example @cross-browser', async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', error => errors.push(error.message))
  await page.goto('/components/progress')
  const preview = page.locator('#components-progress-active-panel-preview')
  const progress = preview.getByRole('progressbar', { name: 'Example task', exact: true })
  const requested = page.waitForRequest(request => new URL(request.url()).pathname === '/components/progress/run')
  await preview.getByRole('button', { name: 'Run example', exact: true }).click()
  const request = await requested
  const ended = Promise.race([
    page.waitForEvent('requestfinished', item => item === request),
    page.waitForEvent('requestfailed', item => item === request),
  ])
  await expect(progress).toHaveAttribute('value', /^(?:[1-9]\d?)$/)
  await page.locator('#nav-components-badge').click()
  await expect(page.getByRole('heading', { name: 'Badge', exact: true, level: 1 })).toBeVisible()
  await ended
  await page.locator('#nav-components-progress').click()
  await expect(progress).toHaveAttribute('value', '0')
  await expect(preview.getByRole('button', { name: 'Run example', exact: true })).toBeEnabled()
  await expect(preview.getByRole('status')).toHaveCount(0)
  expect(errors).toEqual([])
})
