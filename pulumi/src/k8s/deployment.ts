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

const tunnelSecret = new k8s.core.v1.Secret(`${config.identifier}-cloudflared`, {
    metadata: {
        name: `${config.identifier}-cloudflared`,
        namespace: config.k8sConfig.namespace
    },
    stringData: {
        TUNNEL_TOKEN: tunnel.tunnelToken
    }
}, { provider })

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
                containers: [
                    {
                        name: config.identifier,
                        image: image.imageRef,
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
                        args: [
                            'tunnel',
                            '--no-autoupdate',
                            '--metrics', '0.0.0.0:2000',
                            'run',
                            '--token', '$(TUNNEL_TOKEN)'
                        ],
                        env: [{
                            name: 'TUNNEL_TOKEN',
                            valueFrom: {
                                secretKeyRef: {
                                    name: tunnelSecret.metadata.name,
                                    key: 'TUNNEL_TOKEN'
                                }
                            }
                        }],
                        livenessProbe: {
                            httpGet: {
                                path: '/ready',
                                port: 2000
                            },
                            failureThreshold: 1,
                            initialDelaySeconds: 10,
                            periodSeconds: 10
                        },
                        readinessProbe: {
                            httpGet: {
                                path: '/ready',
                                port: 2000
                            },
                            initialDelaySeconds: 10,
                            periodSeconds: 10
                        }
                    }
                ]
            }
        }
    }
}, { provider, dependsOn: tunnelSecret })

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
