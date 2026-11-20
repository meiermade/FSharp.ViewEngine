import * as k8s from '@pulumi/kubernetes'
import * as config from '../config'
import { exampleImageRef } from '../docker/image'
import { provider } from './provider'

const labels = { 'app.kubernetes.io/name': config.exampleName }
const probe = { httpGet: { path: '/health', port: 5000 }, timeoutSeconds: 5 }

export const exampleDeployment = new k8s.apps.v1.Deployment(config.exampleName, {
    metadata: { name: config.exampleName, namespace: config.k8sConfig.namespace },
    spec: {
        replicas: 1,
        progressDeadlineSeconds: 300,
        minReadySeconds: 10,
        revisionHistoryLimit: 3,
        strategy: { type: 'RollingUpdate', rollingUpdate: { maxUnavailable: 0 } },
        selector: { matchLabels: labels },
        template: {
            metadata: { labels },
            spec: {
                automountServiceAccountToken: false,
                securityContext: { runAsNonRoot: true, seccompProfile: { type: 'RuntimeDefault' } },
                containers: [{
                    name: 'example',
                    image: exampleImageRef,
                    securityContext: { allowPrivilegeEscalation: false, capabilities: { drop: ['ALL'] } },
                    env: [
                        { name: 'ASPNETCORE_URLS', value: 'http://0.0.0.0:5000' },
                        { name: 'EXAMPLE_PUBLIC_ORIGIN', value: config.appConfig.origin },
                        { name: 'RELEASE_COMMIT', value: config.releaseCommit },
                        { name: 'RELEASE_IMAGE', value: exampleImageRef },
                    ],
                    ports: [{ name: 'http', containerPort: 5000 }],
                    resources: { requests: { cpu: '100m', memory: '256Mi' }, limits: { memory: '256Mi' } },
                    startupProbe: { ...probe, failureThreshold: 30, periodSeconds: 2 },
                    readinessProbe: probe,
                    livenessProbe: probe,
                }],
            },
        },
    },
}, { provider, protect: config.isStaging })

export const exampleService = new k8s.core.v1.Service(config.exampleName, {
    metadata: { name: config.exampleName, namespace: config.k8sConfig.namespace },
    spec: { selector: labels, ports: [{ name: 'http', port: 80, targetPort: 5000 }] },
}, { provider, dependsOn: exampleDeployment, protect: config.isStaging })
