import * as k8s from '@pulumi/kubernetes'
import { provider } from './provider'
import * as image from '../docker/image'
import * as tunnel from '../cloudflare/tunnel'
import * as config from '../config'

let appConfigMap = new k8s.core.v1.ConfigMap(config.identifier, {
    metadata: {
        name: config.identifier,
        namespace: config.k8sConfig.namespace
    },
    immutable: true,
    data: {
        SERVER_URL: 'http://0.0.0.0:5000'
    }
}, { provider })

const labels = { 'app.kubernetes.io/name': config.identifier }

const cloudflaredSecret = new k8s.core.v1.Secret(`${config.identifier}-cloudflared`, {
    metadata: {
        name: `${config.identifier}-cloudflared`,
        namespace: config.k8sConfig.namespace
    },
    stringData: {
        TUNNEL_TOKEN: tunnel.tunnelToken,
        TUNNEL_METRICS: '0.0.0.0:2000'
    }
}, { provider })

const podSecurityContext: k8s.types.input.core.v1.PodSecurityContext = {
    runAsNonRoot: true,
    seccompProfile: {
        type: 'RuntimeDefault'
    }
}

const containerSecurityContext: k8s.types.input.core.v1.SecurityContext = {
    allowPrivilegeEscalation: false,
    capabilities: {
        drop: ['ALL']
    }
}

const deployment = new k8s.apps.v1.Deployment(config.identifier, {
    metadata: {
        name: config.identifier,
        namespace: config.k8sConfig.namespace
    },
    spec: {
        replicas: 1,
        selector: { matchLabels: labels },
        template: {
            metadata: { labels: labels },
            spec: {
                securityContext: podSecurityContext,
                containers: [
                    {
                        name: config.identifier,
                        image: image.imageRef,
                        securityContext: containerSecurityContext,
                        imagePullPolicy: 'IfNotPresent',
                        envFrom: [ { configMapRef: { name: appConfigMap.metadata.name } } ],
                        livenessProbe: {
                            httpGet: {
                                path: '/health',
                                port: 5000
                            },
                            initialDelaySeconds: 5
                        },
                        readinessProbe: {
                            httpGet: {
                                path: '/health',
                                port: 5000
                            },
                            initialDelaySeconds: 5
                        }
                    },
                    {
                        name: 'cloudflared',
                        image: `cloudflare/cloudflared:${config.cloudflareConfig.cloudflaredVersion}`,
                        securityContext: containerSecurityContext,
                        args: [
                            'tunnel',
                            '--no-autoupdate',
                            'run'
                        ],
                        envFrom: [{ secretRef: { name: cloudflaredSecret.metadata.name } }],
                        livenessProbe: {
                            httpGet: { path: '/ready', port: 2000 },
                            failureThreshold: 1,
                            initialDelaySeconds: 10,
                            periodSeconds: 10
                        }
                    }
                ]
            }
        }
    }
}, { provider, dependsOn: cloudflaredSecret })

new k8s.core.v1.Service(config.identifier, {
    metadata: {
        name: config.identifier,
        namespace: config.k8sConfig.namespace
    },
    spec: {
        type: 'ClusterIP',
        selector: labels,
        ports: [{
            name: 'http',
            port: 80,
            targetPort: 5000
        }]
    }
}, { provider, dependsOn: deployment })
