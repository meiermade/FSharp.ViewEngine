import * as path from 'node:path'
import * as pulumi from '@pulumi/pulumi'

export type DeploymentStack = 'dev' | 'prod'

export const stack = pulumi.getStack() as DeploymentStack
if (stack !== 'dev' && stack !== 'prod') {
    throw new Error(`Unsupported stack ${stack}; expected dev or prod`)
}

export const isStaging = stack === 'dev'
export const rootDir = path.dirname(process.cwd())
export const identifier = isStaging ? 'fsharpviewengine-dev' : 'fsharpviewengine'
export const imageRepository = 'fsharpviewengine'
export const releaseCommit = process.env.RELEASE_COMMIT || 'local'
export const deploymentEnvironment = isStaging ? 'staging' : 'production'

const requireStagingValue = (value: string | undefined, key: string): string => {
    if (isStaging && !value) {
        throw new Error(`${key} is required for dev`)
    }
    return value ?? ''
}

const rawAppConfig = new pulumi.Config('fsharpviewengine')
const configuredOrigin = rawAppConfig.get('origin')
const origin = isStaging
    ? requireStagingValue(configuredOrigin, 'fsharpviewengine:origin')
    : configuredOrigin ?? 'https://fve.meiermade.com'

const parsedOrigin = new URL(origin)
if (parsedOrigin.protocol !== 'https:' || parsedOrigin.pathname !== '/' || parsedOrigin.search || parsedOrigin.hash) {
    throw new Error('fsharpviewengine:origin must be an HTTPS origin without a path, query, or fragment')
}

const expectedOrigin = isStaging
    ? 'https://fve.meiermade.net'
    : 'https://fve.meiermade.com'
if (origin !== expectedOrigin) {
    throw new Error(`${stack} must use ${expectedOrigin}`)
}

export const legacyProductionHostname = 'fsharpviewengine.meiermade.com'
export const appConfig = {
    origin,
    hostname: parsedOrigin.hostname,
    dnsName: parsedOrigin.hostname.split('.')[0],
}

const stableVersionPattern = /^\d{4}\.\d{1,2}\.\d+$/
const releaseVersion = (key: string): string => {
    const value = process.env[key]
    if (isStaging) return value || 'unreleased'
    if (!value || !stableVersionPattern.test(value)) {
        throw new Error(`${key} must use YYYY.M.MINOR form for production`)
    }
    return value
}

const coreVersion = releaseVersion('CORE_PACKAGE_VERSION')
const componentsVersion = releaseVersion('COMPONENTS_PACKAGE_VERSION')
export const releaseMetadata = {
    coreVersion,
    coreTag: coreVersion === 'unreleased' ? 'unreleased' : `v${coreVersion}`,
    componentsVersion,
    componentsTag: componentsVersion === 'unreleased' ? 'unreleased' : `components/v${componentsVersion}`,
}

export const legacyRedirectEnabled = !isStaging && process.env.DISABLE_LEGACY_REDIRECT !== 'true'

const rawDockerConfig = new pulumi.Config('docker')
export const dockerConfig = {
    registryUri: rawDockerConfig.require('registryUri'),
    registryAccessToken: rawDockerConfig.requireSecret('registryAccessToken'),
}

const rawCloudflareConfig = new pulumi.Config('cloudflare')
const optionalStagingValue = (key: string): string | undefined => {
    const value = rawCloudflareConfig.get(key)
    return isStaging ? requireStagingValue(value, `cloudflare:${key}`) : value
}

const zoneName = rawCloudflareConfig.require('zoneName')
if (zoneName !== (isStaging ? 'meiermade.net' : 'meiermade.com')) {
    throw new Error(`${stack} has an unexpected Cloudflare zone ${zoneName}`)
}

export const cloudflareConfig = {
    accountId: rawCloudflareConfig.require('accountId'),
    apiToken: rawCloudflareConfig.requireSecret('apiToken'),
    zoneId: rawCloudflareConfig.require('zoneId'),
    zoneName,
    teamName: optionalStagingValue('teamName'),
    googleAccessIdentityProviderId: optionalStagingValue('googleAccessIdentityProviderId'),
    allowAdminsAccessPolicyId: optionalStagingValue('allowAdminsAccessPolicyId'),
    cloudflaredVersion: rawCloudflareConfig.get('cloudflaredVersion') ?? '2026.7.3',
}

const rawK8sConfig = new pulumi.Config('k8s')
const namespace = rawK8sConfig.require('namespace')
const expectedNamespace = isStaging ? 'fsharpviewengine-dev' : 'fsharpviewengine'
if (namespace !== expectedNamespace) {
    throw new Error(`${stack} must use the ${expectedNamespace} Kubernetes namespace`)
}
export const k8sConfig = { namespace }

const rawOpenTelemetryConfig = new pulumi.Config('openTelemetry')
export const openTelemetryConfig = {
    endpoint: rawOpenTelemetryConfig.require('endpoint'),
}

const suppliedImage = process.env.DEPLOY_IMAGE
if (suppliedImage && !/@sha256:[a-f0-9]{64}$/.test(suppliedImage)) {
    throw new Error('DEPLOY_IMAGE must be an immutable sha256 image reference')
}
export const deploymentImage = suppliedImage
