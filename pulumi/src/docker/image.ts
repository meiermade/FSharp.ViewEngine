import * as path from 'node:path'
import * as pulumi from '@pulumi/pulumi'
import * as dockerBuild from '@pulumi/docker-build'
import * as config from '../config'
import { provider } from './provider'

const registryUri = config.dockerConfig.registryUri
const registryHost = registryUri.split('/')[0]
const isGitHubActions = !!process.env.GITHUB_ACTIONS

export const image = config.deploymentImage
    ? undefined
    : new dockerBuild.Image(config.identifier, {
        tags: [
            `${registryUri}/${config.imageRepository}:${config.releaseCommit}`,
        ],
        context: {
            location: path.join(config.rootDir, 'sln'),
        },
        platforms: [
            dockerBuild.Platform.Linux_amd64,
        ],
        push: true,
        buildOnPreview: false,
        registries: [{
            address: registryHost,
            username: 'oauth2accesstoken',
            password: config.dockerConfig.registryAccessToken,
        }],
        cacheFrom: isGitHubActions ? [{ gha: {} }] : [],
        cacheTo: isGitHubActions ? [{ gha: { mode: dockerBuild.CacheMode.Max, ignoreError: true } }] : [],
    }, {
        provider,
        retainOnDelete: true,
    })

export const imageRef: pulumi.Output<string> = config.deploymentImage
    ? pulumi.output(config.deploymentImage)
    : image!.ref
