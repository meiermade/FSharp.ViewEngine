import * as pulumi from '@pulumi/pulumi'
import * as docker from '@pulumi/docker-build'
import * as path from 'path'
import { provider } from './provider'
import { repo, credentials } from '../aws/repository'
import * as config from '../config'

export const image = new docker.Image(config.identifier, {
    tags: [pulumi.interpolate `${repo.repositoryUrl}:latest`],
    push: true,
    context: {
        location: path.join(config.rootDir, 'sln'),
    },
    platforms: ['linux/arm64'],
    registries: [{
        address: repo.repositoryUrl,
        username: credentials.userName,
        password: credentials.password
    }]
}, { provider })

export const imageRef = image.ref
