import * as pulumi from '@pulumi/pulumi'
import * as cloudflare from '@pulumi/cloudflare'
import * as config from '../config'
import { provider } from './provider'

const discoveredZone = config.cloudflareConfig.zoneId
    ? undefined
    : cloudflare.getZoneOutput({
        filter: {
            name: config.cloudflareConfig.zoneName,
            account: {
                id: config.cloudflareConfig.accountId,
            },
        },
    }, { provider })

export const zoneId: pulumi.Input<string> = config.cloudflareConfig.zoneId ?? discoveredZone!.zoneId
