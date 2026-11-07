import assert from 'node:assert/strict'
import test, { before } from 'node:test'
import * as pulumi from '@pulumi/pulumi'

const resources: { type: string; name: string; inputs: Record<string, any> }[] = []
const releaseCommit = 'abcdef0123456789abcdef0123456789abcdef01'
const imageRef = `us-east1-docker.pkg.dev/meiermade/fsharpviewengine/fsharpviewengine:${releaseCommit}@sha256:${'c'.repeat(64)}`

process.env.RELEASE_COMMIT = releaseCommit
process.env.DEPLOY_IMAGE = imageRef
process.env.CORE_PACKAGE_VERSION = 'unreleased'
process.env.CLI_PACKAGE_VERSION = 'unreleased'
process.env.ALLOW_UNRELEASED_PACKAGE_SNAPSHOT = 'true'
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

test('promotes the supplied digest without rebuilding and preserves Docs-only snapshot metadata', () => {
    assert.equal(resources.some(resource => resource.type === 'docker-build:index:Image'), false)
    const deployment = resources.find(resource => resource.type === 'kubernetes:apps/v1:Deployment' && resource.name === 'fsharpviewengine')
    assert.ok(deployment)
    const container = deployment.inputs.spec.template.spec.containers[0]
    assert.equal(container.image, imageRef)
    const value = (name: string) => container.env.find((item: any) => item.name === name)?.value
    assert.equal(value('RELEASE_COMMIT'), releaseCommit)
    for (const name of ['CORE_PACKAGE_VERSION', 'CORE_PACKAGE_TAG', 'CLI_PACKAGE_VERSION', 'CLI_PACKAGE_TAG']) {
        assert.equal(value(name), 'unreleased')
    }
})
