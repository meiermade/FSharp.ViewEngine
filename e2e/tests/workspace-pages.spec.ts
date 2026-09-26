import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

const crossBrowser = { tag: '@cross-browser' }

test.describe('Reference-backed pages', () => {
const root = '/components/page-examples/';
const pages = ['dependency-graph', 'execution-detail', 'financial-reporting', 'messaging', 'operations-dashboard', 'scheduling', 'media-management'];
const productNavigation: Record<string, string> = {
  'dependency-graph': 'Relay navigation',
  'execution-detail': 'Relay navigation',
  'financial-reporting': 'Ledger navigation',
  messaging: 'Conversations',
  'operations-dashboard': 'Fieldwork navigation',
  scheduling: 'Fieldwork navigation',
  'media-management': 'Fieldwork navigation',
};
const frame = (page: import('@playwright/test').Page) => page.locator('[data-fve-fixture-id="page-workspace"]');
test('dependency selection opens the matching execution and span cross-browser', crossBrowser, async ({ page }) => {
  await page.goto(root + 'dependency-graph');
  await page.evaluate(() => (window as any).__workspaceDocument = 'retained');
  await frame(page).getByRole('link', { name: 'warehouse.orders, Failed', exact: true }).click();
  await expect(page).toHaveURL(/item=order-load/);
  await expect(frame(page).getByRole('link', { name: 'warehouse.orders, Failed', exact: true })).toHaveAttribute('aria-current', 'true');
  await frame(page).getByRole('link', { name: 'Inspect execution', exact: true }).first().click();
  await expect(page).toHaveURL(/execution-detail.*item=run-2408/);
  await expect(frame(page).getByRole('heading', { name: 'run-2408', exact: true })).toBeVisible();
  await frame(page).getByRole('link', { name: 'INSERT order_facts', exact: true }).click();
  await expect(frame(page).getByRole('heading', { name: 'Span · INSERT order_facts', exact: true })).toBeVisible();
  await expect(frame(page)).toContainText('Duplicate order key');
  expect(await page.evaluate(() => (window as any).__workspaceDocument)).toBe('retained');
  await page.goBack();
  await expect(page).not.toHaveURL(/view=write/);
});

test('graph search, zoom, and recovery operate on the workspace cross-browser', async ({ page }) => {
  await page.goto(root + 'dependency-graph');
  await frame(page).getByRole('button', { name: 'Zoom in', exact: true }).click();
  await expect(frame(page).getByLabel('Graph zoom')).toHaveText('125%');
  await frame(page).getByRole('searchbox', { name: 'Search dependencies' }).fill('no-such-node');
  await expect(frame(page).getByText('No dependencies match your search.', { exact: true })).toBeVisible();
  await frame(page).getByRole('button', { name: 'Reset view', exact: true }).click();
  await expect(frame(page).getByLabel('Graph zoom')).toHaveText('100%');
  await page.goto(root + 'dependency-graph?state=error&item=orders');
  await frame(page).getByRole('link', { name: 'Try again' }).click();
  await expect(page).toHaveURL(/state=ready&item=orders/);
  await expect(frame(page).getByRole('link', { name: 'source.orders, Succeeded', exact: true })).toHaveAttribute('aria-current', 'true');
});

test('financial periods have matching axes and accessible values cross-browser', async ({ page }) => {
  await page.goto(root + 'financial-reporting');
  await frame(page).getByRole('link', { name: '3 months', exact: true }).click();
  await expect(page).toHaveURL(/range=3m/);
  await frame(page).getByText('View balance data', { exact: true }).click();
  const table = frame(page).getByRole('table', { name: 'Monthly closing balances (USD)' });
  await expect(table.locator('tbody tr')).toHaveCount(3);
  await expect(table).toContainText('$38,442.11');
  await expect(table).not.toContainText('Apr');
  await frame(page).getByRole('link', { name: 'Northwind payment', exact: true }).click();
  await expect(page).toHaveURL(/ledger-transaction-201/);
  await expect(page.locator('#ledger-app-shell')).toContainText('Northwind payment');
});

test('messages use deterministic URL-backed outcomes without retaining submitted text cross-browser', async ({ page }) => {
  await page.goto(root + 'messaging');
  await frame(page).getByRole('textbox', { name: 'Message', exact: true }).fill('   ');
  await frame(page).getByRole('button', { name: 'Send message', exact: true }).click();
  await expect(page).toHaveURL(/view=message-empty/);
  await expect(frame(page)).toContainText('Enter a message before sending.');

  const submitted = 'Private submitted message';
  await frame(page).getByRole('textbox', { name: 'Message', exact: true }).fill(submitted);
  await frame(page).getByRole('button', { name: 'Send message', exact: true }).click();
  await expect(page).toHaveURL(/view=sent/);
  const deterministic = 'Meet at the east entrance 15 minutes before we leave.';
  await expect(frame(page).getByRole('log').getByText(deterministic, { exact: true })).toBeVisible();
  await expect(frame(page).getByRole('log')).not.toContainText(submitted);
  await page.reload();
  await expect(frame(page).getByRole('log')).toContainText(deterministic);
  await frame(page).getByRole('link', { name: /Studio team/ }).click();
  await expect(page).not.toHaveURL(/view=sent/);
  await expect(frame(page).getByRole('log')).not.toContainText(deterministic);
});

test('schedule records and photographs retain matching destinations cross-browser', async ({ page }) => {
  await page.goto(root + 'operations-dashboard');
  await frame(page).getByRole('link', { name: 'Coastal trail lesson', exact: true }).click();
  await expect(page).toHaveURL(/item=lesson-201/);
  await expect(frame(page)).toContainText('Maya and Sam');
  await frame(page).getByRole('link', { name: 'View matching photograph' }).click();
  await expect(page).toHaveURL(/item=photo-303/);
  await expect(frame(page).getByRole('textbox', { name: 'Photo name' })).toHaveValue('Before the lesson');
  await page.goto(root + 'scheduling');
  await frame(page).getByRole('link', { name: 'Day', exact: true }).click();
  await expect(page).toHaveURL(/view=day/);
  await expect(frame(page).getByRole('link', { name: /Coastal trail lesson/ })).toBeVisible();
  await frame(page).getByRole('link', { name: 'Next', exact: true }).click();
  await expect(page).toHaveURL(/view=day.*range=1|range=1.*view=day/);
  await expect(frame(page)).toContainText('Cornering fundamentals');
  await expect(frame(page)).not.toContainText('Coastal trail lesson');
});

test('media selection and drawer use deterministic fixtures without transmitting files cross-browser', crossBrowser, async ({ page }) => {
  await page.goto(root + 'media-management');
  const library = frame(page).locator('#fieldwork-photos');
  await library.getByRole('checkbox', { name: 'Select Before the lesson', exact: true }).check();
  await expect(library.getByRole('status')).toHaveText('1 selected');
  await expect(frame(page).getByRole('button', { name: 'Use selected as cover' })).toHaveCount(0);
  await frame(page).getByRole('link', { name: 'Before the lesson', exact: true }).click();
  await frame(page).getByRole('textbox', { name: 'Image description' }).fill('A private submitted description.');
  await frame(page).getByRole('button', { name: 'Save changes' }).click();
  await expect(page).toHaveURL(/view=saved/);
  await expect(frame(page).getByRole('img', { name: 'Solid amber background' })).toHaveAttribute('src', '/images/page-examples/amber.png');
  await expect(frame(page)).toContainText('continues to use seeded metadata');
  await expect(frame(page)).not.toContainText('A private submitted description.');
  await frame(page).getByRole('link', { name: 'Back to photos' }).click();

  const uploadAction = frame(page).locator('#media-management-actions').getByRole('button', { name: 'Upload', exact: true });
  await uploadAction.click();
  const drawer = frame(page).getByRole('dialog', { name: 'Upload photograph' });
  await expect(drawer).toBeVisible();
  await expect(drawer.getByRole('textbox', { name: 'Photo name' })).toBeFocused();
  await page.keyboard.press('Escape');
  await expect(drawer).toBeHidden();
  await expect(uploadAction).toBeFocused();
  await uploadAction.click();
  await drawer.getByRole('textbox', { name: 'Photo name' }).fill('Private upload name');
  await drawer.getByRole('textbox', { name: 'Image description' }).fill('Private upload description');
  const fileMarker = Buffer.from('private-file-contents-must-not-leave-the-browser');
  await drawer.locator('input[type=file]').setInputFiles({ name: 'private-marker.png', mimeType: 'image/png', buffer: fileMarker });
  const requestPromise = page.waitForRequest(request => request.url().includes('/media-management/upload'));
  await drawer.getByRole('button', { name: 'Upload', exact: true }).click();
  const request = await requestPromise;
  expect(await request.headerValue('content-type')).toContain('application/x-www-form-urlencoded');
  expect(request.postData() ?? '').not.toContain('private-marker.png');
  expect(request.postDataBuffer()?.includes(fileMarker) ?? false).toBe(false);
  await expect(page).toHaveURL(/item=photo-uploaded.*view=uploaded|view=uploaded.*item=photo-uploaded/);
  await expect(frame(page).getByRole('heading', { name: 'Uploaded fixture preview', exact: true })).toBeVisible();
  await expect(frame(page).getByRole('img', { name: 'Solid violet background used for the upload success fixture' })).toHaveAttribute('src', '/images/page-examples/violet.png');
  await expect(frame(page)).toContainText('selected local file was not submitted');
  await expect(frame(page)).not.toContainText('Private upload name');
});

test('demo form boundary rejects multipart data over direct HTTP', async ({ request }) => {
  const response = await request.post(root + 'messaging/send?item=beach', {
    headers: { 'Content-Type': 'multipart/form-data' },
    data: 'missing boundary',
  })
  expect(response.status()).toBe(415)
})

test('account create, filters and settings use deterministic stateless outcomes cross-browser', crossBrowser, async ({ page }) => {
  await page.goto(root + 'account-management?destination=ledger-create-account');
  const navigation = page.locator('#ledger-side-navigation');
  await expect(navigation.getByText('Workspace', { exact: true })).toBeVisible();
  await expect(navigation.getByText('Meier Made', { exact: true })).toBeVisible();
  await expect(navigation.getByRole('link', { name: 'Andy Meier', exact: true })).toBeVisible();
  await expect(navigation.getByText('Account settings', { exact: true })).toHaveCount(0);
  const commodity = page.locator('#ledger-app-shell dl').filter({ has: page.getByText('Commodity', { exact: true }) });
  await expect(commodity.getByRole('definition')).toHaveText('USD');
  await expect(page.locator('#ledger-app-shell [name="commodity"]')).toHaveCount(0);
  await page.getByRole('textbox', { name: 'Account name', exact: true }).fill('Private account value');
  await page.getByRole('button', { name: 'Create account', exact: true }).click();
  await expect(page).toHaveURL(/view=account-created/);
  await expect(page.locator('#ledger-app-shell')).toContainText('does not create or retain records');
  await expect(page.locator('#ledger-app-shell')).not.toContainText('Private account value');

  await page.goto(root + 'account-management');
  await page.getByRole('searchbox', { name: 'Search accounts', exact: true }).fill('Assets');
  await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
  await expect(page.locator('#ledger-app-shell').getByRole('table').locator('tbody tr')).toHaveCount(1);
  await expect(page.locator('#ledger-app-shell')).toContainText('Assets');

  await page.goto(root + 'account-management?destination=ledger-settings');
  const currency = page.locator('#ledger-app-shell dl').filter({ has: page.getByText('Reporting currency', { exact: true }) });
  await expect(currency.getByRole('definition')).toContainText('USD');
  await expect(page.locator('#ledger-app-shell [name="currency"]')).toHaveCount(0);
  await page.getByRole('textbox', { name: 'Workspace name', exact: true }).fill('Private workspace value');
  await page.getByRole('button', { name: 'Save settings', exact: true }).click();
  await expect(page).toHaveURL(/view=settings-saved/);
  await expect(page.locator('#ledger-app-shell')).toContainText('does not retain submitted values');
  await expect(page.getByRole('textbox', { name: 'Workspace name', exact: true })).toHaveValue('Meier Made');
  await page.reload();
  await expect(page.locator('#ledger-app-shell')).toContainText('does not retain submitted values');
  await expect(page.getByRole('textbox', { name: 'Workspace name', exact: true })).toHaveValue('Meier Made');
});

test('page-example sidebars put workspace context first and keep the profile concise', async ({ page }) => {
  for (const slug of pages) {
    await page.goto(root + slug);
    const shell = frame(page);
    const navigation = shell.getByRole('navigation', { name: productNavigation[slug], exact: true });
    const sideNav = navigation.locator('..');
    const workspace = sideNav.getByText('Workspace', { exact: true });
    const profile = sideNav.getByText('Andy Meier', { exact: true });
    await expect(workspace).toBeVisible();
    await expect(profile).toBeVisible();
    expect((await workspace.boundingBox())!.y).toBeLessThan((await navigation.boundingBox())!.y);
    expect((await profile.boundingBox())!.y).toBeGreaterThan((await navigation.boundingBox())!.y);
    await expect(sideNav.getByText('Northwind workspace', { exact: true })).toHaveCount(0);
  }
});

test('media fixtures use repository-owned color backgrounds instead of photographs', async ({ page }) => {
  await page.goto(root + 'media-management');
  const images = frame(page).locator('#fieldwork-photos img');
  await expect(images).toHaveCount(6);
  const fixtures = await images.evaluateAll(elements => elements.map(element => ({ src: (element as HTMLImageElement).getAttribute('src'), alt: (element as HTMLImageElement).alt })));
  expect(fixtures).toEqual([
    { src: '/images/page-examples/blue.png', alt: 'Solid blue background' },
    { src: '/images/page-examples/teal.png', alt: 'Solid teal background' },
    { src: '/images/page-examples/amber.png', alt: 'Solid amber background' },
    { src: '/images/page-examples/green.png', alt: 'Solid green background' },
    { src: '/images/page-examples/coral.png', alt: 'Solid coral background' },
    { src: '/images/page-examples/violet.png', alt: 'Solid violet background' },
  ]);
});

for (const slug of pages) {
  test(`${slug} keeps review scenarios outside the product`, async ({ page }) => {
    await page.goto(root + slug + '?range=3m');
    await expect(frame(page).getByText('Andy Meier', { exact: true }).first()).toBeVisible();
    const review = page.getByRole('navigation', { name: 'Example review state' });
    await expect(review.getByRole('link', { name: 'Populated', exact: true })).toHaveAttribute('aria-current', 'page');
    if (slug === 'operations-dashboard') {
      await expect(frame(page).locator('#fieldwork-first-steps')).toHaveCount(0);
      await review.getByRole('link', { name: 'Setup', exact: true }).click();
      await expect(page).toHaveURL(/state=setup/);
      await expect(frame(page).locator('#fieldwork-first-steps')).toBeVisible();
      await review.getByRole('link', { name: 'Populated', exact: true }).click();
      await expect(frame(page).locator('#fieldwork-first-steps')).toHaveCount(0);
    } else {
      await expect(review.getByRole('link', { name: 'Setup', exact: true })).toHaveCount(0);
    }
    for (const [label, expected] of [['Loading', 'Loading '], ['Empty', 'Nothing to show yet'], ['Error', 'This view could not be loaded']]) {
      await review.getByRole('link', { name: label, exact: true }).click();
      await expect(review.getByRole('link', { name: label, exact: true })).toHaveAttribute('aria-current', 'page');
      await expect(frame(page)).toContainText(expected);
      await expect(frame(page).getByRole('button', { name: /Simulate/ })).toHaveCount(0);
    }
    await frame(page).getByRole('link', { name: 'Try again', exact: true }).click();
    await expect(page).toHaveURL(/range=3m/);
    await expect(frame(page).getByText('This view could not be loaded', { exact: true })).toHaveCount(0);
  });

  test(`${slug} supports mobile App mode and keyboard navigation`, slug === 'media-management' ? crossBrowser : {}, async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(root + slug + '?fveAppMode=app&fveAppFrame=page-workspace');
    const app = page.locator('[data-fve-app-mode-root="true"]');
    await expect(app).toBeVisible();
    await expect(page.locator('[data-docs-shell="true"]')).not.toBeVisible();
    const open = app.getByRole('button', { name: 'Open navigation', exact: true });
    await open.click();
    await expect(app.getByRole('button', { name: 'Close navigation', exact: true })).toBeVisible();
    const mobileNavigation = app.getByRole('navigation', { name: productNavigation[slug], exact: true });
    const mobileSideNav = mobileNavigation.locator('..');
    await expect(mobileSideNav.getByText('Workspace', { exact: true })).toBeVisible();
    await expect(mobileSideNav.getByText('Andy Meier', { exact: true })).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(open).toBeFocused();
    expect((await new AxeBuilder({ page }).include('[data-fve-app-mode-root="true"]').analyze()).violations).toEqual([]);
    if (slug === 'media-management') {
      await app.getByRole('link', { name: 'Before the lesson', exact: true }).click();
      await app.getByRole('textbox', { name: 'Image description' }).fill('Helmets checked before the lesson.');
      await app.getByRole('button', { name: 'Save changes', exact: true }).click();
      await expect(page).toHaveURL(/fveAppMode=app/);
      await expect(page).toHaveURL(/view=saved/);
      await expect(app).toContainText('continues to use seeded metadata');
      await expect(app.getByRole('textbox', { name: 'Image description' })).toHaveValue('Solid amber background');
    }
  });

  test(`${slug} has usable light desktop and dark narrow layouts cross-browser`, async ({ page }, testInfo) => {
    const errors: string[] = [];
    page.on('pageerror', error => errors.push(error.message));
    for (const [width, dark, scale] of [[1440, false, 1], [390, true, 1], [320, true, 2]] as const) {
      await page.setViewportSize({ width, height: 1000 });
      await page.goto(root + slug);
      await page.evaluate(({ dark, scale }) => {
        document.documentElement.classList.toggle('dark', dark);
        document.documentElement.style.fontSize = `${16 * scale}px`;
      }, { dark, scale });
      await expect(frame(page)).toBeVisible();
      // Theme switching transitions foreground/background colors; inspect their settled values.
      await page.waitForFunction(() => !document.getAnimations().some(animation => {
        const target = (animation.effect as KeyframeEffect | null)?.target;
        return animation.playState === 'running' && Number.isFinite(animation.effect?.getComputedTiming().endTime)
          && target instanceof Element && target.checkVisibility() && target.closest('[data-fve-fixture-id="page-workspace"]');
      }));
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true);
      const violations = (await new AxeBuilder({ page }).include('[data-fve-fixture-id="page-workspace"]').analyze()).violations;
      expect(violations).toEqual([]);
      if (testInfo.project.name === 'chromium') {
        await testInfo.attach(`${slug}-${width}-${scale}`, { body: await frame(page).screenshot(), contentType: 'image/png' });
      }
    }
    expect(errors).toEqual([]);
  });
}
});
