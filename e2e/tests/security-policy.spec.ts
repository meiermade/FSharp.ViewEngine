import { test, expect } from '../fixture';

function nonce(policy: string | undefined): string {
  expect(policy).toBeTruthy();
  expect(policy).not.toContain("'unsafe-eval'");
  const scripts = policy!.split(';').find(value => value.trim().startsWith('script-src '))!;
  expect(scripts).not.toContain("'unsafe-inline'");
  const value = scripts.match(/'nonce-([^']+)'/)?.[1];
  expect(value).toBeTruthy();
  return value!;
}

function observeScriptPolicy() {
  (window as any).scriptViolations = [];
  document.addEventListener('securitypolicyviolation', event => {
    if (event.effectiveDirective.startsWith('script-src')) (window as any).scriptViolations.push(event.blockedURI);
  });
}

test('full documents issue fresh Datastar nonces and private validation keeps that policy', async ({ request }) => {
  const first = await request.get('/examples/application/accounts/new');
  const firstNonce = nonce(first.headers()['content-security-policy']);
  expect(await first.text()).toContain(`data-nonce="${firstNonce}"`);
  const preview = await request.get('/docs/previews/sections--prose--and-lists');
  expect(await preview.text()).toContain(`data-nonce="${nonce(preview.headers()['content-security-policy'])}"`);
  const second = await request.get('/examples/application/accounts/new');
  expect(nonce(second.headers()['content-security-policy'])).not.toBe(firstNonce);
  const invalid = await request.post('/examples/application/accounts/new', {
    form: { name: '<script>window.untrustedDraftRan=true</script>', accountType: 'Asset', parentType: 'Asset', currency: 'EUR', subtype: 'checking', observedBalance: '0' },
  });
  expect(invalid.status()).toBe(200);
  const invalidNonce = nonce(invalid.headers()['content-security-policy']);
  const body = await invalid.text();
  expect(body).toContain(`data-nonce="${invalidNonce}"`);
  expect(body).toContain('&lt;script&gt;');
  expect(body).not.toContain('<script>window.untrustedDraftRan');
  expect(invalid.headers()['cache-control']).toContain('no-store');
});

test('nonce CSP permits canonical controls but blocks unauthorized scripts @cross-browser', async ({ page }) => {
  await page.addInitScript(observeScriptPolicy);
  const response = await page.goto('/examples/specification/accounts/view-accounts');
  nonce(response!.headers()['content-security-policy']);
  await expect(page.getByRole('heading', { name: 'View accounts', exact: true, level: 1 })).toBeVisible();
  await page.locator('#template-document-theme-trigger').click();
  await page.locator('#template-document-theme-menu').getByRole('menuitemradio', { name: 'Dark', exact: true }).click();
  await expect(page.locator('html')).toHaveClass(/dark/);
  await page.getByRole('tab', { name: 'Type filter added', exact: true }).click();
  await expect(page.getByRole('tabpanel', { name: 'Type filter added', exact: true })).toBeVisible();
  const resize = page.getByRole('separator', { name: 'Resize Documentation navigation width', exact: true });
  const before = Number(await resize.getAttribute('aria-valuenow'));
  await resize.press('ArrowRight');
  await expect(resize).toHaveAttribute('aria-valuenow', String(before + 2));
  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole('button', { name: 'Open navigation', exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'Documentation navigation', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Close navigation', exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'Documentation navigation', exact: true })).not.toBeVisible();
  expect(await page.evaluate(() => (window as any).scriptViolations)).toEqual([]);
  await page.evaluate(() => {
    const script = document.createElement('script');
    script.textContent = 'window.unauthorizedScriptRan = true';
    document.head.append(script);
  });
  await expect.poll(() => page.evaluate(() => (window as any).scriptViolations)).toContain('inline');
  expect(await page.evaluate(() => (window as any).unauthorizedScriptRan)).toBeUndefined();
});

test('Docs lazy assets and enhanced history retain the active nonce document @cross-browser', async ({ page }) => {
  await page.addInitScript(observeScriptPolicy);
  const response = await page.goto('/components/mermaid');
  nonce(response!.headers()['content-security-policy']);
  const identity = await page.evaluate(() => (window as any).cspDocumentIdentity = crypto.randomUUID());
  await expect.poll(() => page.locator('[data-docs-diagram][data-mermaid-state="rendered"] svg').count()).toBeGreaterThan(0);
  await page.getByRole('link', { name: 'Code block', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Code block', exact: true, level: 1 })).toBeVisible();
  expect(await page.evaluate(() => (window as any).cspDocumentIdentity)).toBe(identity);
  await expect.poll(() => page.locator('code.language-fsharp .token.keyword').count()).toBeGreaterThan(0);
  await page.goBack();
  await expect(page.getByRole('heading', { name: 'Mermaid', exact: true, level: 1 })).toBeVisible();
  await expect.poll(() => page.locator('[data-docs-diagram][data-mermaid-state="rendered"] svg').count()).toBeGreaterThan(0);
  expect(await page.evaluate(() => (window as any).cspDocumentIdentity)).toBe(identity);
  expect(await page.evaluate(() => (window as any).scriptViolations)).toEqual([]);
});
