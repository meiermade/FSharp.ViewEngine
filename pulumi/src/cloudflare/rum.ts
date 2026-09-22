import * as cloudflare from '@pulumi/cloudflare'
import * as config from '../config'
import { provider } from './provider'
import { zoneId } from './zone'

export const rumRuleset = config.isStaging
    ? undefined
    : new cloudflare.Ruleset(`${config.identifier}-rum`, {
        zoneId,
        // Preserve the existing Cloudflare entry-point ruleset identity; its name is immutable.
        name: 'Disable Web Analytics RUM for fsharpviewengine.meiermade.com',
        kind: 'zone',
        phase: 'http_config_settings',
        rules: [{
            ref: 'disable_docs_rum',
            description: `Disable unreliable Web Analytics RUM injection for ${config.appConfig.hostname}`,
            enabled: true,
            expression: `(http.host eq "${config.appConfig.hostname}")`,
            action: 'set_config',
            actionParameters: {
                disableRum: true,
            },
        }],
    }, { provider })
