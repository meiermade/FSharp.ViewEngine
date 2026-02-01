import * as k8s from '@pulumi/kubernetes'
import { provider } from './provider'
import * as image from '../docker/image'
import * as config from '../config'

let appSecret = new k8s.core.v1.Secret('app', {
    metadata: {
        name: 'app',
        namespace: config.fsharpViewEngineNamespace
    },
    immutable: true,
    stringData: {
        SERVER_URL: 'http://0.0.0.0:5000'
    }
}, { provider })

const labels = { 'app.kubernetes.io/name': 'app' }

const deployment = new k8s.apps.v1.Deployment('app', {
    metadata: {
        name: 'app',
        namespace: config.fsharpViewEngineNamespace
    },
    spec: {
        replicas: 1,
        selector: { matchLabels: labels },
        template: {
            metadata: { labels: labels },
            spec: {
                containers: [{
                    name: 'app',
                    image: image.imageName,
                    imagePullPolicy: 'IfNotPresent',
                    envFrom: [ { secretRef: { name: appSecret.metadata.name } } ],
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

new k8s.core.v1.Service('app', {
    metadata: {
        name: 'app',
        namespace: config.fsharpViewEngineNamespace
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
