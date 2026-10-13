import assert from 'node:assert/strict'
import { spawnSync } from 'node:child_process'
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import * as path from 'node:path'
import test from 'node:test'
import { runInNewContext } from 'node:vm'
import { parse } from 'yaml'

const pulumiDir = path.resolve(__dirname, '../..')
const workflow = parse(readFileSync(path.join(pulumiDir, '../.github/workflows/preview.yml'), 'utf8'))
const steps = workflow.jobs.preview.steps as { id?: string; run?: string; env?: Record<string, string> }[]
const preview = steps.find(step => step.env?.CORE_PACKAGE_VERSION)!
const readerId = preview.env!.RELEASE_COMMIT.match(/steps\.([^.]+)\.outputs\./)![1]
const reader = steps.find(step => step.id === readerId)!
const retained = {
    imageDigest: `registry/image@sha256:${'a'.repeat(64)}`,
    releaseCommit: 'a'.repeat(40),
    coreVersion: '2026.8.2',
    cliVersion: '2026.10.2',
    origin: 'https://fve.meiermade.com',
}

function readRetained(overrides: Partial<typeof retained>, consume: (outputs: Record<string, string>) => void) {
    const directory = mkdtempSync(path.join(tmpdir(), 'fve-retained-preview-'))
    try {
        const input = path.join(directory, 'stack.json')
        const output = path.join(directory, 'output')
        writeFileSync(input, JSON.stringify({ ...retained, ...overrides }))
        writeFileSync(output, '')
        // Only the provider is fake; execute the workflow's actual validation and output serialization.
        writeFileSync(path.join(directory, 'pulumi'), '#!/bin/sh\ncat "$RETAINED_TEST_INPUT"\n', { mode: 0o755 })
        const result = spawnSync('bash', ['-e', '-o', 'pipefail', '-c', reader.run!], {
            env: { ...process.env, PATH: `${directory}:${process.env.PATH}`, GITHUB_OUTPUT: output, RETAINED_TEST_INPUT: input },
            encoding: 'utf8', timeout: 15_000,
        })
        const content = readFileSync(output, 'utf8')
        if (result.status !== 0) return { status: result.status, content, error: result.stderr }
        consume(Object.fromEntries(content.trim().split('\n').map(line => {
            const separator = line.indexOf('=')
            return [line.slice(0, separator), line.slice(separator + 1)]
        })))
        return { status: result.status, content, error: result.stderr }
    } finally {
        rmSync(directory, { recursive: true, force: true })
    }
}

for (const [coreVersion, cliVersion] of [
    ['2026.8.2', '2026.10.2'],
    ['unreleased', '2026.10.2'],
    ['2026.8.2', 'unreleased'],
    ['unreleased', 'unreleased'],
]) {
    test(`retained production preview preserves Core ${coreVersion} and CLI ${cliVersion}`, () => {
        const result = readRetained({ coreVersion, cliVersion }, outputs => {
            const env = { ...process.env }
            for (const key of ['CORE_PACKAGE_VERSION', 'CLI_PACKAGE_VERSION', 'ALLOW_UNRELEASED_PACKAGE_SNAPSHOT', 'DEPLOY_IMAGE', 'RELEASE_COMMIT']) delete env[key]
            for (const [key, expression] of Object.entries(preview.env!)) {
                const match = expression.match(/^\$\{\{([\s\S]*)\}\}$/)!
                env[key] = String(runInNewContext(match[1], { steps: { [reader.id!]: { outputs } } }))
            }
            env.PULUMI_CONFIG = JSON.stringify({
                'docker:registryUri': 'registry', 'docker:registryAccessToken': 'test-token',
                'cloudflare:accountId': 'test-account', 'cloudflare:apiToken': 'test-token',
                'cloudflare:zoneId': 'test-zone', 'cloudflare:zoneName': 'meiermade.com',
                'k8s:namespace': 'fsharpviewengine', 'openTelemetry:endpoint': 'http://otel-collector:4318',
            })
            const config = spawnSync(process.execPath, ['-e', `
                const pulumi = require('@pulumi/pulumi');
                pulumi.runtime.setMocks({newResource: args => ({id: args.name, state: args.inputs}), call: args => args.inputs}, 'fsharp-view-engine', 'prod', false);
                const config = require('./bin/src/config');
                console.log(JSON.stringify({metadata: config.releaseMetadata, image: config.deploymentImage, commit: config.releaseCommit}));
            `], { cwd: pulumiDir, env, encoding: 'utf8', timeout: 15_000 })
            assert.equal(config.status, 0, config.stderr)
            const actual = JSON.parse(config.stdout)
            assert.deepEqual(actual.metadata, {
                coreVersion, coreTag: coreVersion === 'unreleased' ? 'unreleased' : `v${coreVersion}`,
                cliVersion, cliTag: cliVersion === 'unreleased' ? 'unreleased' : `cli/v${cliVersion}`,
            })
            assert.equal(actual.image, retained.imageDigest)
            assert.equal(actual.commit, retained.releaseCommit)
        })
        assert.equal(result.status, 0, result.error)
    })
}

test('retained production preview rejects malformed metadata without exporting it', () => {
    for (const invalid of [
        { coreVersion: 'latest' }, { cliVersion: 'latest' },
        { coreVersion: '' }, { cliVersion: 'UNRELEASED' },
        { imageDigest: 'registry/image:latest' }, { releaseCommit: 'missing' },
        { origin: 'https://fve.meiermade.net' },
    ]) {
        const result = readRetained(invalid, () => assert.fail('Invalid production metadata reached Pulumi'))
        assert.notEqual(result.status, 0, JSON.stringify(invalid))
        assert.equal(result.content, '')
    }
})
