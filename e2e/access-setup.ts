import { mkdir } from 'node:fs/promises'
import path from 'node:path'
import { request, type FullConfig } from '@playwright/test'

const stagingOrigin = 'https://fve.meiermade.net'

export default async function accessSetup(config: FullConfig) {
  const clientId = process.env.CF_ACCESS_CLIENT_ID ?? ''
  const clientSecret = process.env.CF_ACCESS_CLIENT_SECRET ?? ''
  if (!clientId || !clientSecret) return

  const baseURL = config.projects[0]?.use.baseURL
  if (typeof baseURL !== 'string' || new URL(baseURL).origin !== stagingOrigin) {
    throw new Error(`Cloudflare Access credentials may only target ${stagingOrigin}`)
  }

  const accessRequest = await request.newContext({
    baseURL: stagingOrigin,
    extraHTTPHeaders: {
      'CF-Access-Client-Id': clientId,
      'CF-Access-Client-Secret': clientSecret,
    },
  })

  try {
    const response = await accessRequest.get('/health')
    if (!response.ok()) {
      throw new Error(`Cloudflare Access bootstrap failed with HTTP ${response.status()}`)
    }
    const storagePath = path.join(process.cwd(), '.auth', 'access.json')
    await mkdir(path.dirname(storagePath), { recursive: true })
    await accessRequest.storageState({ path: storagePath })
  } finally {
    await accessRequest.dispose()
  }
}
