import * as cloudflare from '@pulumi/cloudflare'
import * as pulumi from '@pulumi/pulumi'
import { tunnel } from './tunnel'
import { zone } from './zone'
import { provider } from './provider'
import * as config from '../config'

export const record = new cloudflare.DnsRecord(config.domain, {
    name: 'fsharpviewengine',
    zoneId: zone.id,
    type: 'CNAME',
    content: pulumi.interpolate `${tunnel.id}.cfargotunnel.com`,
    proxied: true,
    ttl: 1
}, { provider })
