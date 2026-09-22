import * as cloudflare from '@pulumi/cloudflare'
import * as config from '../config'
import * as tunnel from './tunnel'
import { provider } from './provider'
import { zoneId } from './zone'

const dnsRecord = (name: string, dnsName: string, protect: boolean) => new cloudflare.DnsRecord(name, {
    zoneId,
    name: dnsName,
    type: 'CNAME',
    content: tunnel.tunnelHostname,
    proxied: true,
    ttl: 1,
}, {
    provider,
    protect,
})

// Preserve the existing production record identity so the legacy hostname stays
// proxied while the canonical hostname is introduced and accepted.
export const legacyRecord = config.isStaging
    ? undefined
    : dnsRecord(config.identifier, 'fsharpviewengine', false)

export const record = config.isStaging
    ? dnsRecord(config.identifier, config.appConfig.dnsName, true)
    : dnsRecord(`${config.identifier}-canonical`, config.appConfig.dnsName, false)

export const hostname = config.appConfig.hostname
