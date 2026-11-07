import assert from 'node:assert/strict'
import { spawnSync } from 'node:child_process'
import { readFileSync } from 'node:fs'
import * as path from 'node:path'
import test from 'node:test'
import { runInNewContext } from 'node:vm'
import { parse } from 'yaml'

const pulumiDir = path.resolve(__dirname, '../..')
const workflow = (name: string) => parse(readFileSync(path.join(pulumiDir, `../.github/workflows/${name}.yml`), 'utf8'))
const release = workflow('release')
const docs = workflow('deploy-production-docs')
const preview = docs.jobs.preview
const previewStep = preview.steps.find((step: any) => step.with?.command === 'preview')
const production = docs.jobs.deploy
const deploy = production.steps.find((step: any) => step.id === 'deploy')
const repository = 'meiermade/FSharp.ViewEngine'
const github = (file: string, ref = 'refs/heads/main') => ({ repository, ref, workflow_ref: `${repository}/.github/workflows/${file}.yml@${ref}`, run_id: '4242' })
const format = (template: string, ...values: unknown[]) => template.replace(/\{(\d+)\}/g, (_, index) => String(values[Number(index)]))

// Execute the actual workflow's boolean/property-access expressions, including dashed input names.
const evaluate = (value: unknown, context: Record<string, unknown>): any => typeof value !== 'string' ? value : runInNewContext(
    value.replace(/^\$\{\{([\s\S]*)\}\}$/, '$1').replace(/\.([a-zA-Z_]\w*-[\w-]+)/g, '["$1"]'), { format, ...context },
)
const resolve = (values: Record<string, unknown>, context: Record<string, unknown>) => Object.fromEntries(
    Object.entries(values).map(([key, value]) => [key, typeof value === 'string' && !value.startsWith('${{') ? value : evaluate(value, context)]),
)
const emptyInputs = { 'preview-only': false, commit: '', image: '', 'core-version': '', 'cli-version': '', 'promote-docs': false }
const skipped = { candidate: { result: 'skipped', outputs: {} }, preview: { result: 'skipped' }, acceptance: { result: 'skipped' } }

test('four named entry points separate PR validation, staging, release and production Docs', () => {
    const validation = workflow('validate-pr')
    const staging = workflow('deploy-staging-docs')
    assert.equal(validation.name, 'Validate PR')
    assert.ok(validation.on.pull_request)
    assert.equal(staging.name, 'Deploy staging Docs')
    assert.deepEqual(staging.on.push.branches, ['main'])
    assert.equal(release.name, 'Release')
    assert.equal(docs.name, 'Deploy production Docs')
    assert.ok(Object.hasOwn(docs.on, 'workflow_dispatch'))
    assert.ok(Object.hasOwn(docs.on, 'workflow_call'))
    assert.equal(release.jobs['production-preview'].uses, './.github/workflows/deploy-production-docs.yml')
    assert.equal(release.jobs.production.uses, release.jobs['production-preview'].uses)
    assert.equal(release.jobs['production-preview'].with['preview-only'], true)
    assert.equal(release.jobs.production.with['preview-only'], false)
    assert.equal(release.on.release, undefined, 'separate Core/CLI GitHub Releases must not trigger duplicate deployments')
    const prSteps = validation.jobs.preview.steps
    assert.ok(prSteps.some((step: any) => step.with?.['stack-name'] === 'meiermade/fsharp-view-engine/dev'))
    assert.ok(!prSteps.some((step: any) => step.with?.['stack-name'] === 'meiermade/fsharp-view-engine/prod'))
    const transaction = docs.jobs.candidate.steps.find((step: any) => step.id === 'transaction')
    assert.equal(transaction.env.PACKAGE_SELECTION, 'docs', 'manual production cannot allocate or publish package versions')
    assert.ok(!Object.values(docs.jobs).some((job: any) => job.steps.some((step: any) => step.uses?.startsWith('NuGet/login@'))))
})

test('the shared preview saves the plan consumed by the one production deployment implementation', () => {
    assert.equal(preview.environment, undefined, 'the diff must be available before requesting approval')
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
    assert.equal(workflow('deploy-staging-docs').concurrency.group, release.concurrency.group)
    assert.equal(evaluate(docs.concurrency.group, { github: github('deploy-production-docs'), inputs: emptyInputs }), release.concurrency.group)
    for (const previewOnly of [true, false]) {
        assert.notEqual(evaluate(docs.concurrency.group, { github: github('release'), inputs: { 'preview-only': previewOnly } }), release.concurrency.group, 'a called phase must not deadlock on its caller\'s delivery lock')
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
            assert.equal(evaluate(release.jobs.production.if, { needs, always: () => true }), promoteDocs === 'true' && result === 'success')
        }
    }
})

test('manual Docs require accepted source, preview, smoke and release approval; release calls add no second gate', () => {
    const manual = { github: github('deploy-production-docs'), inputs: emptyInputs, always: () => true, cancelled: () => false }
    assert.equal(evaluate(docs.jobs.candidate.if, manual), true)
    assert.equal(evaluate(production.environment.name, manual), 'release')
    for (const dependency of ['candidate', 'preview', 'acceptance']) {
        for (const result of ['success', 'failure', 'skipped', 'cancelled']) {
            const needs: Record<string, unknown> = { candidate: { result: 'success' }, preview: { result: 'success' }, acceptance: { result: 'success' } }
            needs[dependency] = { result }
            assert.equal(evaluate(production.if, { ...manual, needs }), result === 'success', `${dependency}/${result}`)
        }
    }
    assert.equal(evaluate(preview.if, { ...manual, needs: { candidate: { result: 'success' } } }), true)
    assert.equal(evaluate(preview.if, { ...manual, needs: { candidate: { result: 'success' } }, cancelled: () => true }), false)
    for (const previewOnly of [true, false]) {
        const called = { github: github('release'), inputs: { 'preview-only': previewOnly, 'promote-docs': true }, needs: skipped, always: () => true, cancelled: () => false }
        assert.equal(evaluate(docs.jobs.candidate.if, called), false, 'release calls never repick staging')
        assert.equal(evaluate(preview.if, called), previewOnly)
        assert.equal(evaluate(production.if, called), !previewOnly, 'preview-only calls cannot deploy before approval')
        assert.equal(evaluate(production.environment.name, called), 'production', 'the caller already passed release approval')
    }
    const otherCaller = { github: github('validate-pr'), inputs: { 'preview-only': false, 'promote-docs': true }, needs: skipped, always: () => true }
    assert.equal(evaluate(production.if, otherCaller), false, 'another workflow cannot claim the release deployment path')
    const foreign = { ...otherCaller, github: { ...github('release'), repository: 'other/FSharp.ViewEngine', workflow_ref: 'other/FSharp.ViewEngine/.github/workflows/release.yml@refs/heads/main' } }
    assert.equal(evaluate(docs.jobs.candidate.if, foreign), false)
    assert.equal(evaluate(preview.if, { ...foreign, inputs: { 'preview-only': true }, cancelled: () => false }), false, 'foreign callers cannot obtain production preview credentials')
    assert.equal(evaluate(production.if, foreign), false)
    assert.equal(evaluate(production.if, { ...manual, github: github('deploy-production-docs', 'refs/heads/feature'), needs: { candidate: { result: 'success' }, preview: { result: 'success' }, acceptance: { result: 'success' } } }), false)
})

for (const [coreVersion, cliVersion] of [
    ['2026.8.2', '2026.10.2'],
    ['unreleased', '2026.10.2'],
    ['2026.8.2', 'unreleased'],
    ['unreleased', 'unreleased'],
]) {
    test(`manual and release deployment preserve pinned Core ${coreVersion} and CLI ${cliVersion}`, () => {
        const image = `registry/image@sha256:${'a'.repeat(64)}`
        const commit = 'b'.repeat(40)
        const outputs = { image, commit, docsCoreVersion: coreVersion, docsCliVersion: cliVersion, promoteDocs: 'true' }
        const caller = { needs: { candidate: { outputs } } }
        const previewInputs = resolve(release.jobs['production-preview'].with, caller)
        const deployInputs = resolve(release.jobs.production.with, caller)
        const { 'preview-only': _, ...reviewedIdentity } = previewInputs
        const { 'preview-only': __, ...deployedIdentity } = deployInputs
        assert.deepEqual(deployedIdentity, reviewedIdentity)
        const previewEnv = resolve(preview.env, { inputs: previewInputs, needs: skipped })
        const deployEnv = resolve(deploy.env, { inputs: deployInputs, needs: skipped })
        const manualContext = { inputs: emptyInputs, needs: { candidate: { outputs } } }
        assert.deepEqual(resolve(deploy.env, manualContext), deployEnv)
        assert.deepEqual(resolve(preview.env, manualContext), previewEnv)
        for (const [key, value] of Object.entries(deployEnv)) assert.equal(previewEnv[key], value, `${key} is identical before and after approval`)
        for (const promoteDocs of [true, false]) {
            const env = resolve(preview.env, { inputs: { ...previewInputs, 'promote-docs': promoteDocs }, needs: skipped })
            assert.equal(evaluate(previewStep.if, { env }), promoteDocs, 'package-only releases skip the infrastructure preview')
        }
        const env = { ...process.env }
        for (const [key, value] of Object.entries(previewEnv)) env[key] = String(value)
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
