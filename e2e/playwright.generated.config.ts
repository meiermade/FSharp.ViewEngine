import { defineConfig, devices } from '@playwright/test'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const generatedRoot = process.env.GENERATED_CONSUMER_ROOT ?? '/tmp/fsharp-viewengine-generated-consumer'
const port = process.env.GENERATED_CONSUMER_PORT ?? '5064'
const baseURL = `http://127.0.0.1:${port}`

export default defineConfig({
  testDir: './generated-consumer/tests',
  outputDir: './test-results/generated-consumer',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  preserveOutput: 'always',
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    ...devices['Desktop Chrome'],
    baseURL,
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  webServer: {
    command: `dotnet run --project App/GeneratedConsumer.fsproj --configuration Release --no-build -- --urls ${baseURL}`,
    cwd: path.join(generatedRoot, 'net10.0'),
    env: { ...process.env, ASPNETCORE_URLS: baseURL },
    url: `${baseURL}/health`,
    timeout: 120_000,
    reuseExistingServer: false,
    gracefulShutdown: { signal: 'SIGTERM', timeout: 10_000 },
    stdout: 'pipe',
    stderr: 'pipe',
  },
})
