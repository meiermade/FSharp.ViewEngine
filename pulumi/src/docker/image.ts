import * as pulumi from '@pulumi/pulumi'
import * as dockerBuild from '@pulumi/docker-build'
import * as path from 'path'
import { provider } from './provider'
import * as config from '../config'

const registryUri = config.dockerConfig.registryUri
const registryHost = registryUri.split('/')[0]

const isGitHubActions = !!process.env.GITHUB_ACTIONS

export const image = new dockerBuild.Image(config.identifier, {
    tags: [
        pulumi.interpolate`${registryUri}/${config.identifier}`
    ],
    context: {
        location: path.join(config.rootDir, 'sln'),
    },
    platforms: [
        dockerBuild.Platform.Linux_amd64
    ],
    push: true,
    registries: [{
        address: registryHost,
        username: 'oauth2accesstoken',
        password: config.dockerConfig.registryAccessToken,
    }],
    cacheFrom: isGitHubActions ? [{ gha: {} }] : [],
    cacheTo: isGitHubActions ? [{ gha: { mode: dockerBuild.CacheMode.Max, ignoreError: true } }] : [],
}, { provider })

export const imageRef = image.ref
