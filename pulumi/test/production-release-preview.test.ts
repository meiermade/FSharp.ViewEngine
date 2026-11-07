import assert from 'node:assert/strict'
import { spawnSync } from 'node:child_process'
import { readFileSync } from 'node:fs'
import * as path from 'node:path'
import test from 'node:test'
import { runInNewContext } from 'node:vm'
import { parse } from 'yaml'

const pulumiDir = path.resolve(__dirname, '../..')
const workflow = (name: string) => parse(readFileSync(path.join(pulumiDir, `../.github/workflows/${name}.yml`), 'utf8'))
const release = workflow('publish')
const preview = release.jobs['production-preview']
const previewStep = preview.steps.find((step: any) => step.with?.command === 'preview')
const production = release.jobs.production
const deploy = production.steps.find((step: any) => step.id === 'deploy')

// These workflow expressions use the common boolean/property-access subset of GitHub expressions.
// Normalize dashed property names for JavaScript; execute the actual condition, not a copied policy.
const evaluate = (expression: string, context: Record<string, unknown>) => runInNewContext(
    expression.replace(/^\$\{\{([\s\S]*)\}\}$/, '$1').replace(/\.([a-zA-Z_][\w-]*)/g, '["$1"]'), context,
)

test('PR previews are staging-only; release preview saves the plan consumed after approval', () => {
    const prSteps = workflow('preview').jobs.preview.steps
    assert.ok(prSteps.some((step: any) => step.with?.['stack-name'] === 'meiermade/fsharp-view-engine/dev'))
    assert.ok(!prSteps.some((step: any) => step.with?.['stack-name'] === 'meiermade/fsharp-view-engine/prod'))
    assert.equal(preview.needs, 'candidate')
    assert.equal(preview.environment, undefined, 'the preview itself must not wait for release approval')
    assert.equal(previewStep.with['stack-name'], 'meiermade/fsharp-view-engine/prod')
    assert.equal(previewStep.with['comment-on-summary'], true)
    assert.equal(preview.steps[0].with.ref, production.steps[0].with.ref, 'infrastructure source is pinned identically')
    assert.ok(!preview.steps.some((step: any) => step.with?.command === 'up'))
    const upload = preview.steps.find((step: any) => step.with?.name?.startsWith('production-plan-'))
    const download = production.steps.find((step: any) => step.with?.name?.startsWith('production-plan-'))
    assert.equal(upload.with.name, download.with.name)
    assert.equal(upload.with.path, previewStep.with.plan)
    assert.equal(upload.with['if-no-files-found'], 'error')
    assert.equal(deploy.with.plan, `${download.with.path}/${path.posix.basename(upload.with.path)}`)
    assert.ok(preview.steps.indexOf(upload) > preview.steps.indexOf(previewStep))
    assert.equal(upload.if, previewStep.if)
    for (const name of ['deploy', 'deploy-infrastructure']) {
        assert.equal(workflow(name).concurrency.group, release.concurrency.group, 'delivery cannot race a pending approval')
    }
})

test('publication requires preview success even for bundle retries; rejected approval cannot promote', () => {
    const publish = release.jobs.publish
    assert.ok(publish.needs.includes('production-preview'))
    assert.equal(publish.environment, 'release')
    for (const bundle of ['true', 'false']) {
        for (const result of ['success', 'failure', 'skipped', 'cancelled']) {
            const needs = {
                candidate: { result: 'success', outputs: { bundle } },
                selection: { result: 'success' },
                package: { result: bundle === 'true' ? 'skipped' : 'success' },
                'production-preview': { result },
            }
            assert.equal(evaluate(publish.if, { needs, cancelled: () => false }), result === 'success', `${bundle}/${result}`)
            assert.equal(evaluate(publish.if, { needs, cancelled: () => true }), false, 'cancellation never publishes')
        }
    }
    for (const promoteDocs of ['true', 'false']) {
        for (const result of ['success', 'failure', 'cancelled', 'skipped']) {
            const needs = { candidate: { result: 'success', outputs: { promoteDocs } }, publish: { result } }
            assert.equal(evaluate(previewStep.if, { needs }), promoteDocs === 'true', 'package-only releases skip the infrastructure preview')
            assert.equal(evaluate(production.if, { needs, always: () => true }), promoteDocs === 'true' && result === 'success')
        }
    }
})

for (const [coreVersion, cliVersion] of [
    ['2026.8.2', '2026.10.2'],
    ['unreleased', '2026.10.2'],
    ['2026.8.2', 'unreleased'],
    ['unreleased', 'unreleased'],
]) {
    test(`release preview and promotion preserve pinned Core ${coreVersion} and CLI ${cliVersion}`, () => {
        const image = `registry/image@sha256:${'a'.repeat(64)}`
        const commit = 'b'.repeat(40)
        const context = { needs: { candidate: { outputs: { image, commit, docsCoreVersion: coreVersion, docsCliVersion: cliVersion, promoteDocs: 'true' } } } }
        const env = { ...process.env }
        for (const key of ['CORE_PACKAGE_VERSION', 'CLI_PACKAGE_VERSION', 'ALLOW_UNRELEASED_PACKAGE_SNAPSHOT', 'DEPLOY_IMAGE', 'RELEASE_COMMIT']) delete env[key]
        for (const [key, expression] of Object.entries(preview.env as Record<string, string>)) {
            env[key] = String(evaluate(expression, context))
        }
        for (const [key, expression] of Object.entries(deploy.env as Record<string, string>)) {
            assert.equal(env[key], String(evaluate(expression, context)), `${key} is identical before and after approval`)
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
        assert.equal(actual.image, image)
        assert.equal(actual.commit, commit)
    })
}
