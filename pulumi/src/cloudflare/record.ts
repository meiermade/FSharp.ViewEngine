import * as cloudflare from '@pulumi/cloudflare'
import * as config from '../config'
import * as tunnel from './tunnel'
import { provider } from './provider'

const zone = cloudflare.getZoneOutput({
    filter: {
        name: config.cloudflareConfig.zoneName,
        account: {
            id: config.cloudflareConfig.accountId
        }
    }
}, { provider })

new cloudflare.DnsRecord(config.identifier, {
    zoneId: zone.zoneId,
    name: config.identifier,
    type: 'CNAME',
    content: tunnel.tunnelHostname,
    proxied: true,
    ttl: 1
}, { provider, deleteBeforeReplace: true })
