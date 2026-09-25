const replacementPackage = 'FSharp.ViewEngine.Cli'
const retiredPackages = ['FSharp.ViewEngine.Components', 'FSharp.ViewEngine.Docs']

const getJson = async url => {
    const response = await fetch(url, { headers: { accept: 'application/json' } })
    if (!response.ok) throw new Error(`${url} returned ${response.status}`)
    return response.json()
}

const verifyRetirement = async retiredPackage => {
    const packageKey = retiredPackage.toLowerCase()
    const registrationUrl = `https://api.nuget.org/v3/registration5-gz-semver2/${packageKey}/index.json`
    const versionsUrl = `https://api.nuget.org/v3-flatcontainer/${packageKey}/index.json`
    const index = await getJson(registrationUrl)
    const pages = await Promise.all(index.items.map(async page => page.items ?? (await getJson(page['@id'])).items))
    const entries = pages.flat().map(item => item.catalogEntry)
    const availableVersions = (await getJson(versionsUrl)).versions
    const entriesByVersion = new Map(entries.map(entry => [entry.version.toLowerCase(), entry]))

    if (availableVersions.length === 0) throw new Error(`${retiredPackage} has no published versions`)

    for (const version of availableVersions) {
        const entry = entriesByVersion.get(version.toLowerCase())
        if (!entry) throw new Error(`${retiredPackage} ${version} is missing registration metadata`)

        const deprecation = entry.deprecation
        if (!deprecation?.reasons?.includes('Legacy')) {
            throw new Error(`${retiredPackage} ${version} is not deprecated as Legacy`)
        }
        if (deprecation.alternatePackage?.id?.toLowerCase() !== replacementPackage.toLowerCase()) {
            throw new Error(`${retiredPackage} ${version} does not name ${replacementPackage} as its replacement`)
        }

        const packageResponse = await fetch(entry.packageContent, { headers: { range: 'bytes=0-0' } })
        if (!packageResponse.ok) {
            throw new Error(`${retiredPackage} ${version} is no longer downloadable (${packageResponse.status})`)
        }
        await packageResponse.body?.cancel()
    }

    console.log(
        `Verified ${availableVersions.length} ${retiredPackage} versions are downloadable, deprecated as Legacy, and replaced by ${replacementPackage}: ${availableVersions.join(', ')}`,
    )
}

for (const retiredPackage of retiredPackages) await verifyRetirement(retiredPackage)
