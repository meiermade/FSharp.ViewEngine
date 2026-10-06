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

for (const [browser, tests] of [['chromium', chromium], ['firefox', firefox], ['webkit', webkit]]) {
  if (tests.length === 0) fail(`${browser} has no selected tests`)
}
if (firefox.length >= chromium.length || firefox.some(name => !chromium.includes(name))) {
  fail('Focused compatibility projects must select a subset of Chromium contracts, not the full suite')
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

const releaseSmoke = listSelected(['tests/staging-smoke.spec.ts'])
for (const [browser, count] of [['chromium', 5], ['firefox', 1], ['webkit', 1]]) {
  if (releaseSmoke.get(browser).length !== count) fail(`${browser} release smoke must select ${count} intentional checks`)
}
if (JSON.stringify(releaseSmoke.get('firefox')) !== JSON.stringify(releaseSmoke.get('webkit'))) {
  fail('Release compatibility smoke selections differ')
}

if (!process.exitCode) {
  console.log(`PR E2E selection verified: Chromium ${chromium.length}, Firefox ${firefox.length}, WebKit ${webkit.length}`)
  console.log('Release smoke selection verified: Chromium 5, Firefox 1, WebKit 1')
}
