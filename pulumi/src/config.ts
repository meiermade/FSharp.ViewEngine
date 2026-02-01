import * as pulumi from '@pulumi/pulumi'
import * as random from '@pulumi/random'
import * as path from 'path'

const randomProvider = new random.Provider('default')

const awsConfig_ = new pulumi.Config('aws')
const k8sConfig = new pulumi.Config('k8s')
const cloudflareConfig_ = new pulumi.Config('cloudflare')

export const env = pulumi.getStack()

export const rootDir = path.dirname(path.dirname(__dirname))

export const eksNodeManagerArn = awsConfig_.require('eksNodeManagerArn')
export const fsharpViewEngineNamespace = k8sConfig.require('namespace')
export const awsRegion = awsConfig_.require('region')
export const awsAccountId = awsConfig_.require('platformAccountId')

export const domain = 'fsharpviewengine.meiermade.com'

export const identifier = `fsharp-view-engine-${env}`

export const awsConfig = {
    accountId: awsAccountId,
    region: awsRegion
}

const tunnelRandomPassword = new random.RandomPassword(`${identifier}-tunnel`, {
    length: 32,
    special: false
}, { provider: randomProvider })

export const cloudflareConfig = {
    accountId: cloudflareConfig_.require('accountId'),
    apiToken: cloudflareConfig_.requireSecret('apiToken'),
    tunnelSecret: pulumi.secret(tunnelRandomPassword.result),
    cloudflaredVersion: '2024.9.1'
}
