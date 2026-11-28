import Prism from 'prismjs';
import 'prismjs/components/prism-bash.js';
import 'prismjs/components/prism-fsharp.js';
import 'prismjs/components/prism-json.js';
import 'prismjs/components/prism-python.js';
import 'prismjs/components/prism-sql.js';
import 'prismjs/components/prism-typescript.js';

// Code Block handles highlighting itself, including server-rendered updates.
Prism.manual = true;

export function highlight(source, language) {
  if (!Object.hasOwn(Prism.languages, language)) return;
  return Prism.highlight(source, Prism.languages[language], language);
}
