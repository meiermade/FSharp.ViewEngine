import * as k8s from '@pulumi/kubernetes'
import * as config from '../config'
import * as tunnel from '../cloudflare/tunnel'
import * as image from '../docker/image'
import { provider } from './provider'

const labels: Record<string, string> = config.isStaging
    ? {
        'app.kubernetes.io/name': config.identifier,
        'app.kubernetes.io/environment': config.deploymentEnvironment,
    }
    : {
        'app.kubernetes.io/name': config.identifier,
    }

export const cloudflaredSecret = new k8s.core.v1.Secret(`${config.identifier}-cloudflared`, {
    metadata: {
        name: `${config.identifier}-cloudflared`,
        namespace: config.k8sConfig.namespace,
    },
    stringData: {
        TUNNEL_TOKEN: tunnel.tunnelToken,
        TUNNEL_METRICS: '0.0.0.0:2000',
    },
}, {
    provider,
    protect: config.isStaging,
})

const podSecurityContext: k8s.types.input.core.v1.PodSecurityContext = {
    runAsNonRoot: true,
    seccompProfile: {
        type: 'RuntimeDefault',
    },
}

const containerSecurityContext: k8s.types.input.core.v1.SecurityContext = {
    allowPrivilegeEscalation: false,
    capabilities: {
        drop: ['ALL'],
    },
}

export const deployment = new k8s.apps.v1.Deployment(config.identifier, {
    metadata: {
        name: config.identifier,
        namespace: config.k8sConfig.namespace,
    },
    spec: {
        replicas: 1,
        revisionHistoryLimit: 3,
        minReadySeconds: 10,
        progressDeadlineSeconds: 300,
        strategy: {
            type: 'RollingUpdate',
            rollingUpdate: {
                maxUnavailable: 0,
                maxSurge: 1,
            },
        },
        selector: { matchLabels: labels },
        template: {
            metadata: {
                labels,
                annotations: {
                    'fsharpviewengine.meiermade.com/release-commit': config.releaseCommit,
                    'fsharpviewengine.meiermade.com/release-image': image.imageRef,
                },
            },
            spec: {
                automountServiceAccountToken: false,
                terminationGracePeriodSeconds: 30,
                securityContext: podSecurityContext,
                containers: [
                    {
                        name: config.identifier,
                        image: image.imageRef,
                        securityContext: containerSecurityContext,
                        imagePullPolicy: 'IfNotPresent',
                        env: [
                            { name: 'DOCS_SERVER_URL', value: 'http://0.0.0.0:5000' },
                            { name: 'OTEL_EXPORTER_OTLP_ENDPOINT', value: config.openTelemetryConfig.endpoint },
                            { name: 'DEPLOYMENT_ENVIRONMENT', value: config.deploymentEnvironment },
                            { name: 'RELEASE_COMMIT', value: config.releaseCommit },
                            { name: 'RELEASE_IMAGE', value: image.imageRef },
                        ],
                        resources: {
                            requests: { cpu: '25m', memory: '64Mi' },
                            limits: { cpu: '250m', memory: '256Mi' },
                        },
                        startupProbe: {
                            httpGet: { path: '/health', port: 5000 },
                            failureThreshold: 30,
                            periodSeconds: 2,
                        },
                        readinessProbe: {
                            httpGet: { path: '/health', port: 5000 },
                            failureThreshold: 3,
                            periodSeconds: 5,
                        },
                        livenessProbe: {
                            httpGet: { path: '/health', port: 5000 },
                            failureThreshold: 3,
                            periodSeconds: 10,
                        },
                    },
                    {
                        name: 'cloudflared',
                        image: `cloudflare/cloudflared:${config.cloudflareConfig.cloudflaredVersion}`,
                        securityContext: containerSecurityContext,
                        args: [
                            'tunnel',
                            '--no-autoupdate',
                            'run',
                        ],
                        envFrom: [{ secretRef: { name: cloudflaredSecret.metadata.name } }],
                        resources: {
                            requests: { cpu: '10m', memory: '32Mi' },
                            limits: { cpu: '100m', memory: '128Mi' },
                        },
                        startupProbe: {
                            httpGet: { path: '/ready', port: 2000 },
                            failureThreshold: 30,
                            periodSeconds: 2,
                        },
                        readinessProbe: {
                            httpGet: { path: '/ready', port: 2000 },
                            failureThreshold: 3,
                            periodSeconds: 5,
                        },
                        livenessProbe: {
                            httpGet: { path: '/ready', port: 2000 },
                            failureThreshold: 3,
                            periodSeconds: 10,
                        },
                    },
                ],
            },
        },
    },
}, {
    provider,
    dependsOn: cloudflaredSecret,
    protect: config.isStaging,
})

export const service = new k8s.core.v1.Service(config.identifier, {
    metadata: {
        name: config.identifier,
        namespace: config.k8sConfig.namespace,
    },
    spec: {
        type: 'ClusterIP',
        selector: labels,
        ports: [{
            name: 'http',
            port: 80,
            targetPort: 5000,
        }],
    },
}, {
    provider,
    dependsOn: deployment,
    protect: config.isStaging,
})
