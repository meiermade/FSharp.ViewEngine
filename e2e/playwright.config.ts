import { defineConfig, devices } from '@playwright/test'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const startLocal = process.env.E2E_START_LOCAL !== '0'
const port = process.env.E2E_SERVER_PORT ?? '5054'
const baseURL = process.env.DOCS_E2E_BASE_URL ?? (startLocal ? `http://127.0.0.1:${port}` : 'https://fsharpviewengine.meiermade.com')

export default defineConfig({
  testDir: './tests',
  outputDir: './test-results',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  workers: 1,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL,
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
  ],
  webServer: startLocal
    ? {
        command: 'bash scripts/start-local.sh',
        cwd: __dirname,
        url: `${baseURL}/health`,
        timeout: process.env.CI ? 600_000 : 300_000,
        reuseExistingServer: process.env.E2E_REUSE_EXISTING_SERVER === '1',
        gracefulShutdown: { signal: 'SIGTERM', timeout: 15_000 },
        stdout: 'pipe',
        stderr: 'pipe',
      }
    : undefined,
})
