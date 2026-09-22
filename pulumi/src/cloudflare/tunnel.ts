import * as pulumi from '@pulumi/pulumi'
import * as cloudflare from '@pulumi/cloudflare'
import * as access from './access'
import * as config from '../config'
import { provider } from './provider'

export const tunnel = new cloudflare.ZeroTrustTunnelCloudflared(config.identifier, {
    accountId: config.cloudflareConfig.accountId,
    name: config.identifier,
    configSrc: 'cloudflare',
}, {
    provider,
    protect: config.isStaging,
})

const accessOriginRequest = access.application
    ? {
        access: {
            required: true,
            audTags: [access.application.aud],
            teamName: config.cloudflareConfig.teamName!,
        },
    }
    : undefined

const applicationIngress = (hostname: string) => ({
    hostname,
    service: 'http://localhost:5000',
    ...(accessOriginRequest ? { originRequest: accessOriginRequest } : {}),
})

const ingresses = [
    applicationIngress(config.appConfig.hostname),
    ...(!config.isStaging && !config.legacyRedirectEnabled
        ? [applicationIngress(config.legacyProductionHostname)]
        : []),
    { service: 'http_status:404' },
]

export const tunnelConfig = new cloudflare.ZeroTrustTunnelCloudflaredConfig(config.identifier, {
    accountId: config.cloudflareConfig.accountId,
    tunnelId: tunnel.id,
    source: 'cloudflare',
    config: { ingresses },
}, { provider })

export const tunnelHostname = tunnel.id.apply(id => `${id}.cfargotunnel.com`)
export const tunnelToken = pulumi.secret(cloudflare.getZeroTrustTunnelCloudflaredTokenOutput({
    accountId: config.cloudflareConfig.accountId,
    tunnelId: tunnel.id,
}, { provider }).token)
