// Regenerate pinned Heroicons and supplementary MIT brand artwork; no build-time network dependency.
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { mkdirSync, mkdtempSync, readFileSync, readdirSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const version = '2.2.0';
const brandVersion = '1.13.1';
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const temporary = mkdtempSync(join(tmpdir(), 'fve-icons-'));
const brands = [
  {name:'GitHub', slug:'github', file:'github.svg'},
  {name:'X', slug:'x', file:'twitter-x.svg'},
  {name:'LinkedIn', slug:'linkedin', file:'linkedin.svg'},
];
async function pinnedPackage(name, version) {
  const response = await fetch(`https://registry.npmjs.org/${name}/${version}`);
  if (!response.ok) throw Error(`${name} metadata: ${response.status}`);
  const {dist} = await response.json();
  const archiveResponse = await fetch(dist.tarball);
  if (!archiveResponse.ok) throw Error(`${name} archive: ${archiveResponse.status}`);
  const archive = Buffer.from(await archiveResponse.arrayBuffer());
  const [algorithm, expected] = dist.integrity.split('-');
  if (createHash(algorithm).update(archive).digest('base64') !== expected) throw Error(`${name} integrity mismatch`);
  const directory = join(temporary, name);
  mkdirSync(directory);
  writeFileSync(join(directory, 'icons.tgz'), archive);
  execFileSync('tar', ['-xzf', join(directory, 'icons.tgz'), '-C', directory]);
  return {source:join(directory, 'package'), dist};
}
function artwork(path, size) {
  const svg = readFileSync(path, 'utf8');
  if (!svg.includes(`viewBox="0 0 ${size} ${size}"`)) throw Error(`Unexpected viewBox: ${path}`);
  const inner = svg.replace(/^<svg\b[^>]*>\s*/, '').replace(/\s*<\/svg>\s*$/, '').trim();
  if (!inner || inner.includes('<script') || inner.includes('<svg')) throw Error(`Unexpected SVG: ${path}`);
  return `        """${inner.replace(/\s+/g, ' ')}"""\n`;
}
try {
  const {source, dist} = await pinnedPackage('heroicons', version);
  const brandPackage = await pinnedPackage('bootstrap-icons', brandVersion);
  const families = [['outline', '24/outline', 24], ['solid', '24/solid', 24], ['mini', '20/solid', 20], ['micro', '16/solid', 16]];
  const catalogues = families.map(([, folder]) => readdirSync(join(source, folder)).filter(name => name.endsWith('.svg')));
  // The larger families contain eight retired aliases absent from Micro. Export canonical shared names.
  const names = catalogues[0].filter(name => catalogues.every(catalogue => catalogue.includes(name))).sort();
  const header = `namespace FSharp.ViewEngine.Components\n\n// Generated from Heroicons ${version} and Bootstrap Icons ${brandVersion}; see Assets/*provenance.json.\n`;
  const pascal = name => name.slice(0, -4).split('-').map(part => part[0].toUpperCase() + part.slice(1)).join('');
  // Append brands so existing Heroicons enum values remain stable.
  const typedNames = [...names.map(pascal), ...brands.map(brand => brand.name)];
  writeFileSync(join(root, 'IconNames.fs'), `${header}\n/// <summary>Typed Heroicons glyph names and supplementary brand logos.</summary>\n/// <category>icon</category>\ntype IconName =\n${typedNames.map((name, index) => `    | ${name} = ${index}`).join('\n')}\n`);
  let data = `${header}\nmodule internal IconData =\n    let names = [| ${[...names.map(name => name.slice(0, -4)), ...brands.map(brand => brand.slug)].map(name => JSON.stringify(name)).join('; ')} |]\n`;
  for (const [family, folder, size] of families) {
    data += `\n    let ${family} = [|\n`;
    for (const name of names) data += artwork(join(source, folder, name), size);
    data += '    |]\n';
  }
  data += '\n    let brand = [|\n';
  for (const brand of brands) data += artwork(join(brandPackage.source, 'icons', brand.file), 16);
  data += '    |]\n';
  writeFileSync(join(root, 'IconData.fs'), data);
  writeFileSync(join(root, 'Assets/heroicons-LICENSE.txt'), readFileSync(join(source, 'LICENSE')));
  writeFileSync(join(root, `Assets/heroicons.${version}.provenance.json`), JSON.stringify({name:'heroicons',version,source:'https://github.com/tailwindlabs/heroicons',tarball:dist.tarball,integrity:dist.integrity,license:'MIT',glyphs:names.length,families:families.map(([name,folder])=>({name,folder})),generator:'Documentation/update-heroicons.mjs',transform:'Unmodified inner SVG markup, whitespace-normalized; matching native viewBox and paint attributes supplied by Icon.fs.'},null,2)+'\n');
  writeFileSync(join(root, 'Assets/bootstrap-icons-LICENSE.txt'), readFileSync(join(brandPackage.source, 'LICENSE')));
  writeFileSync(join(root, `Assets/bootstrap-icons.${brandVersion}.provenance.json`), JSON.stringify({name:'bootstrap-icons',version:brandVersion,source:'https://github.com/twbs/icons',tarball:brandPackage.dist.tarball,integrity:brandPackage.dist.integrity,license:'MIT',glyphs:brands,nativeViewBox:'0 0 16 16',generator:'Documentation/update-heroicons.mjs',transform:'Unmodified inner SVG markup, whitespace-normalized; currentColor paint and native viewBox supplied by Icon.fs.'},null,2)+'\n');
  console.log(`Generated ${names.length} Heroicons in four families and ${brands.length} brand logos.`);
} finally { rmSync(temporary, {recursive:true, force:true}); }
