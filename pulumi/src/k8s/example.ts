import * as k8s from '@pulumi/kubernetes'
import * as config from '../config'
import { exampleImageRef } from '../docker/image'
import { provider } from './provider'

const labels = { 'app.kubernetes.io/name': config.exampleName }
const hosts = [
    { name: 'example', port: 5000, servicePort: 80 },
    { name: 'spec', port: 5001, servicePort: 81, assembly: 'spec/Example.Specification.dll' },
    { name: 'docs', port: 5002, servicePort: 82, assembly: 'docs/Example.Documentation.dll' },
    { name: 'presentation', port: 5003, servicePort: 83, assembly: 'presentation/Example.Presentation.dll' },
]

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
                containers: hosts.map(({ name, port, assembly }) => {
                    const probe = { httpGet: { path: '/health', port }, timeoutSeconds: 5 }
                    return {
                        name,
                        image: exampleImageRef,
                        ...(assembly ? { args: [assembly] } : {}),
                        securityContext: { allowPrivilegeEscalation: false, capabilities: { drop: ['ALL'] } },
                        env: [
                            { name: 'ASPNETCORE_URLS', value: `http://0.0.0.0:${port}` },
                            { name: 'EXAMPLE_PUBLIC_ORIGIN', value: config.appConfig.origin },
                            { name: 'EXAMPLE_DOCS_URL', value: '/examples/documentation' },
                            { name: 'RELEASE_COMMIT', value: config.releaseCommit },
                            { name: 'RELEASE_IMAGE', value: exampleImageRef },
                            ...(name === 'spec' ? [{ name: 'EXAMPLE_SPEC_CATALOG_DEMO', value: 'true' }] : []),
                            ...(name === 'presentation' ? [{ name: 'EXAMPLE_APP_URL', value: '/examples/application' }] : []),
                        ],
                        ports: [{ name, containerPort: port }],
                        resources: { requests: { cpu: '100m', memory: '256Mi' }, limits: { memory: '256Mi' } },
                        startupProbe: { ...probe, failureThreshold: 30, periodSeconds: 2 },
                        readinessProbe: probe,
                        livenessProbe: probe,
                    }
                }),
            },
        },
    },
}, { provider, protect: config.isStaging })

export const exampleService = new k8s.core.v1.Service(config.exampleName, {
    metadata: { name: config.exampleName, namespace: config.k8sConfig.namespace },
    spec: { selector: labels, ports: hosts.map(({ name, servicePort, port }) => ({
        name: name === 'example' ? 'http' : name, port: servicePort, targetPort: port,
    })) },
}, { provider, dependsOn: exampleDeployment, protect: config.isStaging })
