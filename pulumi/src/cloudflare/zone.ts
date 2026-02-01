import * as cloudflare from '@pulumi/cloudflare'
import { provider } from './provider'
import * as config from '../config'

export const zone = cloudflare.getZoneOutput({
    filter: {
        account: { id: config.cloudflareConfig.accountId },
        name: 'meiermade.com'
    }
}, { provider })
