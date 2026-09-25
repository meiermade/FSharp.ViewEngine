import assert from 'node:assert/strict'
import test, { before } from 'node:test'
import * as pulumi from '@pulumi/pulumi'

interface RegisteredResource {
    type: string
    name: string
    inputs: Record<string, any>
}

const resources: RegisteredResource[] = []
const releaseCommit = 'abcdef0123456789abcdef0123456789abcdef01'
const imageRef = `us-east1-docker.pkg.dev/meiermade/fsharpviewengine/fsharpviewengine:${releaseCommit}@sha256:${'c'.repeat(64)}`

process.env.RELEASE_COMMIT = releaseCommit
process.env.DEPLOY_IMAGE = imageRef
process.env.CORE_PACKAGE_VERSION = '2026.8.2'
process.env.CLI_PACKAGE_VERSION = 'unreleased'
process.env.ALLOW_UNRELEASED_PACKAGE_SNAPSHOT = 'true'
process.env.DISABLE_LEGACY_REDIRECT = 'true'
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
        return { id: `${args.name}-id`, state: args.inputs }
    },
    call: (args: pulumi.runtime.MockCallArgs) => {
        if (args.token.includes('getZeroTrustTunnelCloudflaredToken')) return { token: 'mock-tunnel-token' }
        return args.inputs
    },
}, 'fsharp-view-engine', 'prod', false)

before(async () => {
    const stack = await import('../index')
    await new Promise<void>(resolve => stack.deploymentName.apply(() => resolve()))
})

const resource = (type: string, name: string): RegisteredResource => {
    const match = resources.find(candidate => candidate.type === type && candidate.name === name)
    assert.ok(match, `Expected ${type} resource ${name}`)
    return match
}

test('keeps the legacy hostname serving while canonical production is accepted', () => {
    const tunnelConfig = resource(
        'cloudflare:index/zeroTrustTunnelCloudflaredConfig:ZeroTrustTunnelCloudflaredConfig',
        'fsharpviewengine',
    )
    assert.equal(tunnelConfig.inputs.config.ingresses[0].hostname, 'fve.meiermade.com')
    assert.equal(tunnelConfig.inputs.config.ingresses[1].hostname, 'fsharpviewengine.meiermade.com')
    assert.deepEqual(tunnelConfig.inputs.config.ingresses[2], { service: 'http_status:404' })
    assert.equal(
        resources.some(candidate => candidate.name === 'fsharpviewengine-legacy-redirect'),
        false,
    )
})

test('represents the explicit pre-CLI recovery snapshot without candidate metadata', () => {
    const deployment = resource('kubernetes:apps/v1:Deployment', 'fsharpviewengine')
    const env = deployment.inputs.spec.template.spec.containers[0].env
    const value = (name: string) => env.find((item: any) => item.name === name)?.value
    assert.equal(value('CORE_PACKAGE_VERSION'), '2026.8.2')
    assert.equal(value('CLI_PACKAGE_VERSION'), 'unreleased')
    assert.equal(value('CLI_PACKAGE_TAG'), 'unreleased')
})
