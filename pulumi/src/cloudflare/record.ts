import * as cloudflare from '@pulumi/cloudflare'
import * as config from '../config'
import * as tunnel from './tunnel'
import { provider } from './provider'
import { zone } from './zone'

new cloudflare.DnsRecord(config.identifier, {
    zoneId: zone.zoneId,
    name: config.identifier,
    type: 'CNAME',
    content: tunnel.tunnelHostname,
    proxied: true,
    ttl: 1
}, { provider, deleteBeforeReplace: true })
