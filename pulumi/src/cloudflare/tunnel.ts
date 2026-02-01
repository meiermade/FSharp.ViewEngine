import * as cloudflare from '@pulumi/cloudflare'
import * as pulumi from '@pulumi/pulumi'
import { provider } from './provider'
import * as config from '../config'

export const tunnel = new cloudflare.ZeroTrustTunnelCloudflared(config.identifier, {
    accountId: config.cloudflareConfig.accountId,
    name: config.identifier,
    configSrc: 'cloudflare'
}, { provider })

new cloudflare.ZeroTrustTunnelCloudflaredConfig(config.identifier, {
    accountId: config.cloudflareConfig.accountId,
    tunnelId: tunnel.id,
    config: {
        ingresses: [
            {
                hostname: config.domain,
                service: pulumi.interpolate `http://app.${config.k8sConfig.namespace}`
            },
            {
                service: 'http_status:404'
            }
        ],
    }
}, { provider })

export const tunnelToken = cloudflare.getZeroTrustTunnelCloudflaredTokenOutput({
    accountId: config.cloudflareConfig.accountId,
    tunnelId: tunnel.id
}, { provider })
