import { spawnSync } from 'node:child_process'

const listingEnvironment = {
  ...process.env,
  E2E_CROSS_BROWSER_MODE: 'focused',
  E2E_START_LOCAL: '0',
}

for (const name of [
  'CF_ACCESS_CLIENT_ID',
  'CF_ACCESS_CLIENT_SECRET',
  'DOCS_E2E_BASE_URL',
  'DOCS_EXPECTED_COMMIT',
  'DOCS_EXPECTED_ENVIRONMENT',
  'DOCS_EXPECTED_IMAGE',
]) {
  delete listingEnvironment[name]
}

function listSelected(extraArgs = []) {
  const result = spawnSync(
    process.platform === 'win32' ? 'npx.cmd' : 'npx',
    ['playwright', 'test', '--list', ...extraArgs],
    {
      cwd: new URL('..', import.meta.url),
      encoding: 'utf8',
      env: listingEnvironment,
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

const chromium = selected.get('chromium')
const firefox = selected.get('firefox')
const webkit = selected.get('webkit')

for (const [browser, tests, expected] of [
  ['chromium', chromium, 241],
  ['firefox', firefox, 89],
  ['webkit', webkit, 89],
]) {
  if (tests.length !== expected) {
    fail(`${browser} selection changed: expected ${expected}, found ${tests.length}`)
  }
}
if (JSON.stringify(firefox) !== JSON.stringify(webkit)) {
  fail('Firefox and WebKit focused selections differ')
}
for (const [browser, shardCount, completeSelection] of [
  ['chromium', 6, chromium],
  ['firefox', 3, firefox],
  ['webkit', 3, webkit],
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

if (!process.exitCode) {
  console.log(`E2E selection verified: Chromium ${chromium.length}, Firefox ${firefox.length}, WebKit ${webkit.length}`)
}
