import * as fs from 'node:fs'
import * as k8s from '@pulumi/kubernetes'

const kubeconfigPath = process.env.KUBECONFIG

if (!kubeconfigPath) {
    throw new Error('KUBECONFIG is required')
}

export const provider = new k8s.Provider('default', {
    kubeconfig: fs.readFileSync(kubeconfigPath, 'utf8'),
})
