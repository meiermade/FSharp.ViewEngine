import assert from 'node:assert/strict'
import { spawnSync } from 'node:child_process'
import { mkdtempSync, mkdirSync, copyFileSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs'
import { tmpdir } from 'node:os'
import path from 'node:path'
import test from 'node:test'

function runSmoke(changes = {}, dockerStatus = 0) {
  const root = mkdtempSync(path.join(tmpdir(), 'fve-release-smoke-'))
  try {
    for (const directory of ['scripts', 'bin', '.auth']) mkdirSync(path.join(root, directory))
    for (const script of ['test-release-candidate.sh', 'test-published-ci.sh']) {
      copyFileSync(new URL(script, import.meta.url), path.join(root, 'scripts', script))
    }
    copyFileSync(new URL('../playwright-image.txt', import.meta.url), path.join(root, 'playwright-image.txt'))
    writeFileSync(path.join(root, '.auth/access.json'), 'temporary auth state')
    writeFileSync(path.join(root, 'bin/docker'), `#!/usr/bin/env node
require('fs').writeFileSync(process.env.ARGUMENTS, JSON.stringify(process.argv.slice(2))); process.exit(${dockerStatus})
`, { mode: 0o755 })
    const result = spawnSync('bash', ['scripts/test-release-candidate.sh'], {
      cwd: root, encoding: 'utf8', timeout: 10_000,
      env: {
        PATH: `${root}/bin:${process.env.PATH}`, ARGUMENTS: `${root}/arguments.json`,
        EXPECTED_COMMIT: 'accepted-source', DOCS_EXPECTED_COMMIT: 'accepted-source',
        EXPECTED_IMAGE: 'accepted-image', DOCS_EXPECTED_IMAGE: 'accepted-image',
        DOCS_E2E_BASE_URL: 'https://fve.meiermade.net',
        E2E_BROWSER: 'webkit', E2E_CROSS_BROWSER_MODE: 'full', E2E_SHARD: '1/3',
        ...changes,
      },
    })
    return {
      status: result.status,
      arguments: existsSync(`${root}/arguments.json`) ? JSON.parse(readFileSync(`${root}/arguments.json`)) : null,
      authRemoved: !existsSync(`${root}/.auth/access.json`),
    }
  } finally {
    rmSync(root, { recursive: true, force: true })
  }
}

test('release runner invokes only complete three-engine smoke, zero retries, and removes temporary auth', () => {
  const result = runSmoke()
  assert.equal(result.status, 0)
  const args = result.arguments
  assert.deepEqual(args.slice(args.indexOf('playwright') + 2), [
    'tests/staging-smoke.spec.ts', '--project=chromium', '--project=firefox', '--project=webkit', '--retries=0',
  ])
  assert.ok(args.includes('E2E_CROSS_BROWSER_MODE=focused'))
  assert.ok(args.includes('CI=true'))
  assert.equal(result.authRemoved, true)
})

test('failed smoke propagates failure and still removes temporary auth', () => {
  const result = runSmoke({}, 17)
  assert.equal(result.status, 17)
  assert.equal(result.authRemoved, true)
})

test('wrong staging identity or origin stops before browser invocation', () => {
  for (const changes of [
    { DOCS_EXPECTED_COMMIT: 'wrong-source' },
    { DOCS_EXPECTED_IMAGE: 'wrong-image' },
    { DOCS_E2E_BASE_URL: 'https://fve.meiermade.com' },
  ]) {
    const result = runSmoke(changes)
    assert.notEqual(result.status, 0)
    assert.equal(result.arguments, null)
  }
})
