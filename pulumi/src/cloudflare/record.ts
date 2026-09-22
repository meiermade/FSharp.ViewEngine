import * as cloudflare from '@pulumi/cloudflare'
import * as config from '../config'
import * as tunnel from './tunnel'
import { provider } from './provider'
import { zoneId } from './zone'

export const record = new cloudflare.DnsRecord(config.identifier, {
    zoneId,
    name: config.appConfig.dnsName,
    type: 'CNAME',
    content: tunnel.tunnelHostname,
    proxied: true,
    ttl: 1,
}, {
    provider,
    protect: config.isStaging,
})

export const hostname = config.appConfig.hostname
