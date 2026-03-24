import * as cloudflare from '@pulumi/cloudflare'
import { provider } from './provider'
import * as config from '../config'

export const zone = cloudflare.getZoneOutput({
    filter: {
        name: config.cloudflareConfig.zoneName,
        account: {
            id: config.cloudflareConfig.accountId
        }
    }
}, { provider })
