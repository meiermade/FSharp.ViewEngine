import assert from 'node:assert/strict'
import test, { before } from 'node:test'
import * as pulumi from '@pulumi/pulumi'

interface RegisteredResource {
    type: string
    name: string
    inputs: Record<string, any>
}

const resources: RegisteredResource[] = []
const releaseCommit = 'fedcba9876543210fedcba9876543210fedcba98'
const imageRef = `us-east1-docker.pkg.dev/meiermade/fsharpviewengine/fsharpviewengine:${releaseCommit}@sha256:${'b'.repeat(64)}`

process.env.RELEASE_COMMIT = releaseCommit
process.env.CORE_PACKAGE_VERSION = '2026.8.2'
process.env.CLI_PACKAGE_VERSION = '2026.9.0'
process.env.PULUMI_CONFIG = JSON.stringify({
    'docker:registryUri': 'us-east1-docker.pkg.dev/meiermade/fsharpviewengine',
    'docker:registryAccessToken': 'registry-token',
    'k8s:namespace': 'fsharpviewengine',
    'cloudflare:accountId': 'account-id',
    'cloudflare:apiToken': 'api-token',
    'cloudflare:zoneId': 'production-zone-id',
    'cloudflare:zoneName': 'meiermade.com',
    'openTelemetry:endpoint': 'http://otel-collector:4318',
})

pulumi.runtime.setMocks({
    newResource: (args: pulumi.runtime.MockResourceArgs) => {
        resources.push({ type: args.type, name: args.name, inputs: args.inputs })
        return {
            id: `${args.name}-id`,
            state: {
                ...args.inputs,
                ref: args.type === 'docker-build:index:Image' ? imageRef : args.inputs.ref,
            },
        }
    },
    call: (args: pulumi.runtime.MockCallArgs) => {
        if (args.token.includes('getZeroTrustTunnelCloudflaredToken')) {
            return { token: 'mock-tunnel-token' }
        }
        return args.inputs
    },
}, 'fsharp-view-engine', 'prod', false)

before(async () => {
    const stack = await import('../index')
    await Promise.all([
        new Promise<void>(resolve => stack.imageDigest.apply(() => resolve())),
        new Promise<void>(resolve => stack.deploymentName.apply(() => resolve())),
    ])
})

const resource = (type: string, name: string): RegisteredResource => {
    const match = resources.find(candidate => candidate.type === type && candidate.name === name)
    assert.ok(match, `Expected ${type} resource ${name}`)
    return match
}

const resourcesOfType = (type: string): RegisteredResource[] =>
    resources.filter(candidate => candidate.type === type)

test('retains the established production identities and public hostname', () => {
    const deployment = resource('kubernetes:apps/v1:Deployment', 'fsharpviewengine')
    assert.equal(deployment.inputs.metadata.name, 'fsharpviewengine')
    assert.equal(deployment.inputs.metadata.namespace, 'fsharpviewengine')
    assert.deepEqual(deployment.inputs.spec.selector.matchLabels, {
        'app.kubernetes.io/name': 'fsharpviewengine',
    })

    const record = resource('cloudflare:index/dnsRecord:DnsRecord', 'fsharpviewengine-canonical')
    assert.equal(record.inputs.name, 'fve')
    assert.equal(record.inputs.zoneId, 'production-zone-id')

    const legacyRecord = resource('cloudflare:index/dnsRecord:DnsRecord', 'fsharpviewengine')
    assert.equal(legacyRecord.inputs.name, 'fsharpviewengine')
    assert.equal(legacyRecord.inputs.proxied, true)

    const tunnelConfig = resource(
        'cloudflare:index/zeroTrustTunnelCloudflaredConfig:ZeroTrustTunnelCloudflaredConfig',
        'fsharpviewengine',
    )
    assert.equal(tunnelConfig.inputs.config.ingresses[0].hostname, 'fve.meiermade.com')
    assert.equal(tunnelConfig.inputs.config.ingresses[0].originRequest, undefined)
    assert.deepEqual(tunnelConfig.inputs.config.ingresses[1], { service: 'http_status:404' })
})

test('promotes exact release metadata and redirects the legacy hostname at the edge', () => {
    const deployment = resource('kubernetes:apps/v1:Deployment', 'fsharpviewengine')
    const env = deployment.inputs.spec.template.spec.containers[0].env
    const value = (name: string) => env.find((item: any) => item.name === name)?.value
    assert.equal(deployment.inputs.spec.template.spec.containers[0].image, imageRef)
    assert.equal(value('DOCS_PUBLIC_ORIGIN'), 'https://fve.meiermade.com')
    assert.equal(value('RELEASE_COMMIT'), releaseCommit)
    assert.equal(value('CORE_PACKAGE_VERSION'), '2026.8.2')
    assert.equal(value('CLI_PACKAGE_VERSION'), '2026.9.0')
    assert.equal(value('CORE_PACKAGE_TAG'), 'v2026.8.2')
    assert.equal(value('CLI_PACKAGE_TAG'), 'cli/v2026.9.0')

    const redirectScript = resource('cloudflare:index/workersScript:WorkersScript', 'fsharpviewengine-legacy-redirect')
    assert.equal(redirectScript.inputs.scriptName, 'fsharpviewengine-legacy-redirect')
    assert.match(redirectScript.inputs.content, /target\.protocol = 'https:'/)
    assert.match(redirectScript.inputs.content, /target\.hostname = 'fve\.meiermade\.com'/)
    assert.match(redirectScript.inputs.content, /Response\.redirect\(target\.toString\(\), 301\)/)

    const redirectRoute = resource('cloudflare:index/workersRoute:WorkersRoute', 'fsharpviewengine-legacy-redirect-route')
    assert.equal(redirectRoute.inputs.zoneId, 'production-zone-id')
    assert.equal(redirectRoute.inputs.pattern, 'fsharpviewengine.meiermade.com/*')
    assert.equal(redirectRoute.inputs.script, 'fsharpviewengine-legacy-redirect')
})

test('does not add staging Access resources to production', () => {
    assert.equal(resourcesOfType('cloudflare:index/zeroTrustAccessServiceToken:ZeroTrustAccessServiceToken').length, 0)
    assert.equal(resourcesOfType('cloudflare:index/zeroTrustAccessPolicy:ZeroTrustAccessPolicy').length, 0)
    assert.equal(resourcesOfType('cloudflare:index/zeroTrustAccessApplication:ZeroTrustAccessApplication').length, 0)
    assert.equal(resourcesOfType('cloudflare:index/ruleset:Ruleset').length, 2)
})

test('does not create development-named production resources', () => {
    assert.equal(resources.some(candidate => candidate.name.includes('fsharpviewengine-dev')), false)
})
