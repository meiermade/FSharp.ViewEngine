import * as pulumi from '@pulumi/pulumi'
import * as config from './src/config'
import * as access from './src/cloudflare/access'
import * as record from './src/cloudflare/record'
import './src/cloudflare'
import * as image from './src/docker/image'
import './src/docker'
import * as workload from './src/k8s/deployment'
import './src/k8s'

export const origin = config.appConfig.origin
export const releaseCommit = config.releaseCommit
export const imageDigest = image.imageRef
export const hostname = record.hostname
export const accessAudience = access.application?.aud ?? ''
export const ciAccessClientId = pulumi.secret(access.ciServiceToken?.clientId ?? '')
export const ciAccessClientSecret = pulumi.secret(access.ciServiceToken?.clientSecret ?? '')
export const deploymentName = workload.deployment.metadata.name
export const serviceName = workload.service.metadata.name
export const e2eReady: pulumi.Output<boolean> = config.isStaging
    ? pulumi.all([
        image.imageRef,
        access.application!.aud,
        access.ciServiceToken!.clientId,
        workload.deployment.status,
    ]).apply(() => true)
    : pulumi.output(false)
