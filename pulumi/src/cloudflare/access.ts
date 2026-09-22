import * as cloudflare from '@pulumi/cloudflare'
import * as config from '../config'
import { provider } from './provider'

export const ciServiceToken = config.isStaging
    ? new cloudflare.ZeroTrustAccessServiceToken(`${config.identifier}-ci`, {
        accountId: config.cloudflareConfig.accountId,
        name: `${config.identifier}-ci`,
    }, {
        provider,
        protect: true,
        additionalSecretOutputs: ['clientSecret'],
    })
    : undefined

export const ciPolicy = config.isStaging
    ? new cloudflare.ZeroTrustAccessPolicy(`${config.identifier}-ci`, {
        accountId: config.cloudflareConfig.accountId,
        name: `Allow ${config.identifier} CI`,
        decision: 'non_identity',
        includes: [{ serviceToken: { tokenId: ciServiceToken!.id } }],
    }, {
        provider,
        protect: true,
    })
    : undefined

export const application = config.isStaging
    ? new cloudflare.ZeroTrustAccessApplication(config.identifier, {
        accountId: config.cloudflareConfig.accountId,
        name: 'FSharp.ViewEngine staging',
        domain: config.appConfig.hostname,
        type: 'self_hosted',
        sessionDuration: '24h',
        allowedIdps: [config.cloudflareConfig.googleAccessIdentityProviderId!],
        autoRedirectToIdentity: false,
        httpOnlyCookieAttribute: true,
        policies: [
            { id: config.cloudflareConfig.allowAdminsAccessPolicyId!, precedence: 1 },
            { id: ciPolicy!.id, precedence: 2 },
        ],
    }, {
        provider,
        protect: true,
    })
    : undefined
