import * as dockerBuild from '@pulumi/docker-build'
import * as config from '../config'

// Refresh reads historical Image inputs; provider-scoped credentials use the current ESC token.
export const provider = new dockerBuild.Provider('default', {
    registries: [{
        address: config.dockerConfig.registryUri.split('/')[0],
        username: 'oauth2accesstoken',
        password: config.dockerConfig.registryAccessToken,
    }],
})
