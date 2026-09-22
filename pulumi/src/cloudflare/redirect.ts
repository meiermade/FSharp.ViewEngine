import * as cloudflare from '@pulumi/cloudflare'
import * as config from '../config'
import { provider } from './provider'

const scriptName = `${config.identifier}-legacy-redirect`
const script = config.legacyRedirectEnabled
    ? new cloudflare.WorkersScript(scriptName, {
        accountId: config.cloudflareConfig.accountId,
        scriptName,
        compatibilityDate: '2026-09-22',
        content: `addEventListener('fetch', event => {
    const target = new URL(event.request.url)
    target.protocol = 'https:'
    target.hostname = '${config.appConfig.hostname}'
    event.respondWith(Response.redirect(target.toString(), 301))
})`,
    }, { provider })
    : undefined

export const legacyRedirect = script
    ? new cloudflare.WorkersRoute(`${config.identifier}-legacy-redirect-route`, {
        zoneId: config.cloudflareConfig.zoneId,
        pattern: `${config.legacyProductionHostname}/*`,
        script: script.scriptName,
    }, { provider })
    : undefined
