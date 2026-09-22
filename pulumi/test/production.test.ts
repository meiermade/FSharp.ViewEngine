import assert from 'node:assert/strict'
import test, { before } from 'node:test'
import * as pulumi from '@pulumi/pulumi'

interface RegisteredResource {
    type: string
    name: string
    inputs: Record<string, any>
}

const resources: RegisteredResource[] = []
const imageRef = `us-east1-docker.pkg.dev/meiermade/fsharpviewengine/fsharpviewengine:local@sha256:${'b'.repeat(64)}`

process.env.PULUMI_CONFIG = JSON.stringify({
    'docker:registryUri': 'us-east1-docker.pkg.dev/meiermade/fsharpviewengine',
    'docker:registryAccessToken': 'registry-token',
    'k8s:namespace': 'fsharpviewengine',
    'cloudflare:accountId': 'account-id',
    'cloudflare:apiToken': 'api-token',
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
        if (args.token.includes('getZone')) {
            return { zoneId: 'production-zone-id', id: 'production-zone-id' }
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

    const record = resource('cloudflare:index/dnsRecord:DnsRecord', 'fsharpviewengine')
    assert.equal(record.inputs.name, 'fsharpviewengine')
    assert.equal(record.inputs.zoneId, 'production-zone-id')

    const tunnelConfig = resource(
        'cloudflare:index/zeroTrustTunnelCloudflaredConfig:ZeroTrustTunnelCloudflaredConfig',
        'fsharpviewengine',
    )
    assert.equal(tunnelConfig.inputs.config.ingresses[0].hostname, 'fsharpviewengine.meiermade.com')
    assert.equal(tunnelConfig.inputs.config.ingresses[0].originRequest, undefined)
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
