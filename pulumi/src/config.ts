import * as pulumi from '@pulumi/pulumi'
import * as path from 'path'

export const rootDir = path.dirname(path.dirname(__dirname))

export const domain = 'fsharpviewengine.meiermade.com'

export const identifier = 'fsharp-view-engine'

const rawAwsConfig = new pulumi.Config('aws')
const rawK8sConfig = new pulumi.Config('k8s')
export const awsConfig = {
    accountId: rawAwsConfig.require('platformAccountId'),
    region: rawAwsConfig.require('region'),
    eksNodeManagerArn: rawAwsConfig.require('eksNodeManagerArn')
}

export const k8sConfig = {
    namespace: rawK8sConfig.get('namespace'),
}
