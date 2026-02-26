import * as pulumi from '@pulumi/pulumi'
import * as path from 'path'

export const rootDir = path.dirname(path.dirname(__dirname))

export const identifier = 'fsharpviewengine'

const rawAwsConfig = new pulumi.Config('aws')
const rawCloudflareConfig = new pulumi.Config('cloudflare')
const rawK8sConfig = new pulumi.Config('k8s')

export const awsConfig = {
    accountId: rawAwsConfig.require('platformAccountId'),
    region: rawAwsConfig.require('region'),
    eksNodeManagerArn: rawAwsConfig.require('eksNodeManagerArn')
}

export const cloudflareConfig = {
    accountId: rawCloudflareConfig.require('accountId'),
    apiToken: rawCloudflareConfig.requireSecret('apiToken'),
    zoneName: rawCloudflareConfig.require('zoneName'),
    cloudflaredVersion: '2026.2.0'
}

export const k8sConfig = {
    namespace: rawK8sConfig.require('namespace'),
}
