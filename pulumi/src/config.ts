import * as pulumi from '@pulumi/pulumi'
import * as path from 'path'

export const rootDir = path.dirname(path.dirname(__dirname))

export const identifier = 'fsharpviewengine'

const rawDockerConfig = new pulumi.Config('docker')

export const dockerConfig = {
    registryUri: rawDockerConfig.require('registryUri'),
    registryAccessToken: rawDockerConfig.requireSecret('registryAccessToken'),
}

const rawCloudflareConfig = new pulumi.Config('cloudflare')

export const cloudflareConfig = {
    accountId: rawCloudflareConfig.require('accountId'),
    apiToken: rawCloudflareConfig.requireSecret('apiToken'),
    zoneName: rawCloudflareConfig.require('zoneName'),
    cloudflaredVersion: '2026.2.0'
}

const rawK8sConfig = new pulumi.Config('k8s')

export const k8sConfig = {
    namespace: rawK8sConfig.require('namespace'),
}
