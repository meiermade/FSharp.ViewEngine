import * as docker from '@pulumi/docker-build'

export const provider = new docker.Provider('default')
