import * as cloudflare from '@pulumi/cloudflare'
import { provider } from './provider'
import * as config from '../config'

export const tunnel = new cloudflare.ZeroTrustTunnelCloudflared(config.identifier, {
    accountId: config.cloudflareConfig.accountId,
    name: config.identifier,
    configSrc: 'cloudflare'
}, { provider, deleteBeforeReplace: true })

new cloudflare.ZeroTrustTunnelCloudflaredConfig(config.identifier, {
    accountId: config.cloudflareConfig.accountId,
    tunnelId: tunnel.id,
    source: 'cloudflare',
    config: {
        ingresses: [
            {
                hostname: `${config.identifier}.${config.cloudflareConfig.zoneName}`,
                service: 'http://localhost:80'
            },
            {
                service: 'http_status:404'
            }
        ]
    }
}, { provider })

export const tunnelHostname = tunnel.id.apply(id => `${id}.cfargotunnel.com`)

const tunnelTokenRes = cloudflare.getZeroTrustTunnelCloudflaredTokenOutput({
    accountId: config.cloudflareConfig.accountId,
    tunnelId: tunnel.id
}, { provider })

export const tunnelToken = tunnelTokenRes.token
