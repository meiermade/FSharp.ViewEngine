import assert from 'node:assert/strict'
import test, { before } from 'node:test'
import * as path from 'node:path'
import * as pulumi from '@pulumi/pulumi'

interface RegisteredResource {
    type: string
    name: string
    inputs: Record<string, any>
}

const resources: RegisteredResource[] = []
const releaseCommit = '0123456789abcdef0123456789abcdef01234567'
const imageRef = `us-east1-docker.pkg.dev/meiermade/fsharpviewengine/fsharpviewengine:${releaseCommit}@sha256:${'a'.repeat(64)}`
let stackOutputs: typeof import('../index')
let resolvedImage: string | undefined
let resolvedReady: boolean | undefined

process.env.RELEASE_COMMIT = releaseCommit
process.env.PULUMI_CONFIG = JSON.stringify({
    'fsharpviewengine:origin': 'https://fve.meiermade.net',
    'docker:registryUri': 'us-east1-docker.pkg.dev/meiermade/fsharpviewengine',
    'docker:registryAccessToken': 'registry-token',
    'k8s:namespace': 'fsharpviewengine-dev',
    'cloudflare:accountId': 'account-id',
    'cloudflare:apiToken': 'api-token',
    'cloudflare:zoneId': 'internal-zone-id',
    'cloudflare:zoneName': 'meiermade.net',
    'cloudflare:teamName': 'meiermade',
    'cloudflare:googleAccessIdentityProviderId': 'google-idp-id',
    'cloudflare:allowAdminsAccessPolicyId': 'admin-policy-id',
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
                aud: args.type.includes('AccessApplication') ? `${args.name}-audience` : args.inputs.aud,
                clientId: args.type.includes('AccessServiceToken') ? `${args.name}-client-id` : args.inputs.clientId,
                clientSecret: args.type.includes('AccessServiceToken') ? `${args.name}-client-secret` : args.inputs.clientSecret,
                status: args.type === 'kubernetes:apps/v1:Deployment' ? { readyReplicas: 1 } : args.inputs.status,
            },
        }
    },
    call: (args: pulumi.runtime.MockCallArgs) => {
        if (args.token.includes('getZeroTrustTunnelCloudflaredToken')) {
            return { token: 'mock-tunnel-token' }
        }
        return args.inputs
    },
}, 'fsharp-view-engine', 'dev', false)

before(async () => {
    stackOutputs = await import('../index')
    await Promise.all([
        new Promise<void>(resolve => stackOutputs.imageDigest.apply(value => {
            resolvedImage = value
            resolve()
        })),
        new Promise<void>(resolve => stackOutputs.e2eReady!.apply(value => {
            resolvedReady = value
            resolve()
        })),
    ])
})

const resource = (type: string, name: string): RegisteredResource => {
    const match = resources.find(candidate => candidate.type === type && candidate.name === name)
    assert.ok(match, `Expected ${type} resource ${name}`)
    return match
}

const resourcesOfType = (type: string): RegisteredResource[] =>
    resources.filter(candidate => candidate.type === type)

const envValue = (deployment: RegisteredResource, name: string): unknown =>
    deployment.inputs.spec.template.spec.containers[0].env.find((item: any) => item.name === name)?.value

test('builds one immutable candidate image and publishes staging outputs', async () => {
    const image = resource('docker-build:index:Image', 'fsharpviewengine-dev')
    assert.deepEqual(image.inputs.tags, [
        `us-east1-docker.pkg.dev/meiermade/fsharpviewengine/fsharpviewengine:${releaseCommit}`,
    ])
    assert.equal(image.inputs.context.location, path.dirname(process.cwd()) + '/sln')
    assert.deepEqual(image.inputs.platforms, ['linux/amd64'])
    assert.equal(image.inputs.push, true)
    assert.equal(image.inputs.buildOnPreview, false)
    assert.equal(resolvedImage, imageRef)
    assert.equal(resolvedReady, true)
    assert.equal(stackOutputs.origin, 'https://fve.meiermade.net')
    assert.equal(stackOutputs.hostname, 'fve.meiermade.net')
    assert.equal(await pulumi.isSecret(stackOutputs.ciAccessClientId!), true)
    assert.equal(await pulumi.isSecret(stackOutputs.ciAccessClientSecret!), true)
})

test('creates isolated Access, DNS, and tunnel resources for staging', () => {
    const token = resource(
        'cloudflare:index/zeroTrustAccessServiceToken:ZeroTrustAccessServiceToken',
        'fsharpviewengine-dev-ci',
    )
    const policy = resource(
        'cloudflare:index/zeroTrustAccessPolicy:ZeroTrustAccessPolicy',
        'fsharpviewengine-dev-ci',
    )
    assert.equal(policy.inputs.decision, 'non_identity')
    assert.equal(policy.inputs.includes[0].serviceToken.tokenId, `${token.name}-id`)

    const application = resource(
        'cloudflare:index/zeroTrustAccessApplication:ZeroTrustAccessApplication',
        'fsharpviewengine-dev',
    )
    assert.equal(application.inputs.domain, 'fve.meiermade.net')
    assert.deepEqual(application.inputs.allowedIdps, ['google-idp-id'])
    assert.deepEqual(application.inputs.policies, [
        { id: 'admin-policy-id', precedence: 1 },
        { id: 'fsharpviewengine-dev-ci-id', precedence: 2 },
    ])

    const record = resource('cloudflare:index/dnsRecord:DnsRecord', 'fsharpviewengine-dev')
    assert.equal(record.inputs.zoneId, 'internal-zone-id')
    assert.equal(record.inputs.name, 'fve')
    assert.equal(record.inputs.proxied, true)

    const tunnelConfig = resource(
        'cloudflare:index/zeroTrustTunnelCloudflaredConfig:ZeroTrustTunnelCloudflaredConfig',
        'fsharpviewengine-dev',
    )
    assert.equal(tunnelConfig.inputs.config.ingresses[0].hostname, 'fve.meiermade.net')
    assert.equal(tunnelConfig.inputs.config.ingresses[0].service, 'http://localhost:5000')
    assert.equal(tunnelConfig.inputs.config.ingresses[0].originRequest.access.required, true)
    assert.deepEqual(tunnelConfig.inputs.config.ingresses[0].originRequest.access.audTags, [
        'fsharpviewengine-dev-audience',
    ])
    assert.deepEqual(tunnelConfig.inputs.config.ingresses[1], { service: 'http_status:404' })

    assert.equal(resourcesOfType('cloudflare:index/ruleset:Ruleset').length, 0)
})

test('deploys staging in place with a zero-unavailable rollout and exact identity', () => {
    const deployment = resource('kubernetes:apps/v1:Deployment', 'fsharpviewengine-dev')
    assert.equal(deployment.inputs.metadata.namespace, 'fsharpviewengine-dev')
    assert.equal(deployment.inputs.spec.strategy.type, 'RollingUpdate')
    assert.equal(deployment.inputs.spec.strategy.rollingUpdate.maxUnavailable, 0)
    assert.equal(deployment.inputs.spec.strategy.rollingUpdate.maxSurge, 1)
    assert.equal(deployment.inputs.spec.replicas, 1)
    assert.equal(deployment.inputs.spec.minReadySeconds, 10)

    const pod = deployment.inputs.spec.template.spec
    assert.equal(pod.automountServiceAccountToken, false)
    assert.equal(pod.containers.length, 2)
    assert.equal(pod.containers[0].image, imageRef)
    assert.equal(envValue(deployment, 'DEPLOYMENT_ENVIRONMENT'), 'staging')
    assert.equal(envValue(deployment, 'RELEASE_COMMIT'), releaseCommit)
    assert.equal(envValue(deployment, 'RELEASE_IMAGE'), imageRef)
    assert.equal(pod.containers[0].startupProbe.httpGet.path, '/health')
    assert.equal(pod.containers[0].readinessProbe.httpGet.path, '/health')
    assert.equal(pod.containers[1].readinessProbe.httpGet.path, '/ready')

    const service = resource('kubernetes:core/v1:Service', 'fsharpviewengine-dev')
    assert.equal(service.inputs.metadata.namespace, 'fsharpviewengine-dev')
    assert.equal(service.inputs.spec.type, 'ClusterIP')
    assert.equal(service.inputs.spec.ports[0].targetPort, 5000)
})

test('does not create production-named staging resources', () => {
    const managedNames = resources
        .filter(candidate => !candidate.type.startsWith('pulumi:providers:'))
        .map(candidate => candidate.name)
    assert.equal(managedNames.includes('fsharpviewengine'), false)
})
