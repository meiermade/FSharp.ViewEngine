import { spawnSync } from 'node:child_process'

function listSelected(extraArgs = []) {
  const result = spawnSync(
    process.platform === 'win32' ? 'npx.cmd' : 'npx',
    ['playwright', 'test', '--list', ...extraArgs],
    {
      cwd: new URL('..', import.meta.url),
      encoding: 'utf8',
      env: {
        ...process.env,
        E2E_CROSS_BROWSER_MODE: 'focused',
        E2E_START_LOCAL: '0',
      },
    },
  )

  if (result.status !== 0) {
    process.stderr.write(result.stderr)
    process.exit(result.status ?? 1)
  }

  const selected = new Map([
    ['chromium', []],
    ['firefox', []],
    ['webkit', []],
  ])
  for (const line of result.stdout.split('\n')) {
    const match = line.match(/^  \[(chromium|firefox|webkit)\] › ([^:]+):\d+:\d+ › (.+)$/)
    if (!match) continue
    selected.get(match[1]).push(`${match[2]} › ${match[3]}`)
  }
  return selected
}

const selected = listSelected()

const fail = message => {
  console.error(`E2E selection contract failed: ${message}`)
  process.exitCode = 1
}

for (const [browser, tests] of selected) {
  const unique = new Set(tests)
  if (unique.size !== tests.length) fail(`${browser} contains duplicate selected tests`)
}

const chromium = selected.get('chromium')
const firefox = selected.get('firefox')
const webkit = selected.get('webkit')

if (chromium.length < 200 || chromium.length > 250) {
  fail(`Chromium must retain 200–250 primary checks; found ${chromium.length}`)
}
if (firefox.length < 50 || firefox.length > 100) {
  fail(`Firefox must retain 50–100 focused checks; found ${firefox.length}`)
}
if (webkit.length < 50 || webkit.length > 100) {
  fail(`WebKit must retain 50–100 focused checks; found ${webkit.length}`)
}
if (JSON.stringify(firefox) !== JSON.stringify(webkit)) {
  fail('Firefox and WebKit focused selections differ')
}
if (firefox.some(test => test.startsWith('production-smoke.spec.ts') || test.startsWith('staging-smoke.spec.ts'))) {
  fail('Focused Firefox includes an environment smoke test')
}
if (webkit.some(test => test.startsWith('production-smoke.spec.ts') || test.startsWith('staging-smoke.spec.ts'))) {
  fail('Focused WebKit includes an environment smoke test')
}

for (const [browser, shardCount, completeSelection] of [
  ['chromium', 5, chromium],
  ['firefox', 2, firefox],
  ['webkit', 2, webkit],
]) {
  const shards = Array.from({ length: shardCount }, (_, index) =>
    listSelected([`--project=${browser}`, `--shard=${index + 1}/${shardCount}`]).get(browser),
  )
  const shardedSelection = shards.flat()
  if (new Set(shardedSelection).size !== shardedSelection.length) {
    fail(`${browser} workflow shards overlap`)
  }
  if (JSON.stringify([...shardedSelection].sort()) !== JSON.stringify([...completeSelection].sort())) {
    fail(`${browser} workflow shards do not cover the complete selection`)
  }
}

for (const required of [
  'docs-navigation-session.spec.ts',
  'multiple-choice.spec.ts',
  'popup-focus.spec.ts',
  'workspace-pages.spec.ts',
]) {
  if (!firefox.some(test => test.startsWith(required))) fail(`Firefox is missing ${required}`)
  if (!webkit.some(test => test.startsWith(required))) fail(`WebKit is missing ${required}`)
}

if (!process.exitCode) {
  console.log(`E2E selection verified: Chromium ${chromium.length}, Firefox ${firefox.length}, WebKit ${webkit.length}`)
}
