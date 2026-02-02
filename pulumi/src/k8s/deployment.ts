import * as k8s from '@pulumi/kubernetes'
import { provider } from './provider'
import * as image from '../docker/image'
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
                containers: [{
                    name: config.identifier,
                    image: image.imageRef,
                    imagePullPolicy: 'IfNotPresent',
                    envFrom: [ { configMapRef: { name: appConfigMap.metadata.name } } ],
                    livenessProbe: {
                        tcpSocket: {
                            port: 5000
                        },
                        initialDelaySeconds: 5
                    },
                    readinessProbe: {
                        tcpSocket: {
                            port: 5000
                        },
                        initialDelaySeconds: 5
                    }
                }]
            }
        }
    }
}, { provider })

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
