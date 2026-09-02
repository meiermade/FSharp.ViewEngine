import * as cloudflare from '@pulumi/cloudflare'
import * as config from '../config'
import { provider } from './provider'
import { zone } from './zone'

const hostname = `${config.identifier}.${config.cloudflareConfig.zoneName}`

new cloudflare.Ruleset(`${config.identifier}-rum`, {
    zoneId: zone.zoneId,
    name: `Disable Web Analytics RUM for ${hostname}`,
    kind: 'zone',
    phase: 'http_config_settings',
    rules: [{
        ref: 'disable_docs_rum',
        description: `Disable unreliable Web Analytics RUM injection for ${hostname}`,
        enabled: true,
        expression: `(http.host eq "${hostname}")`,
        action: 'set_config',
        actionParameters: {
            disableRum: true,
        },
    }],
}, { provider })
