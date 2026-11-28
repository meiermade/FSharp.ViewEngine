import "../../Example.UI/Components/Assets/code-block.js";
import "../../Example.UI/Components/Assets/button.js";
import "../../Example.UI/Components/Assets/link.js";
import "../../Example.UI/Components/Assets/tabs.js";
import "../../Example.UI/Components/Assets/side-nav.js";
import "../../Example.UI/Components/Assets/dialog.js";
import "../../Example.UI/Components/Assets/drawer.js";
import "../../Example.UI/Components/Assets/dropdown-menu.js";
import "../../Example.UI/Components/Assets/command.js";
// Ordinary links remain native. The mobile drawer lands keyboard users on the destination's main region.
document.addEventListener('click', event => {
  const link = event.target.closest?.('dialog[id$="template-navigation-dialog"][open] a[href]');
  if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || !link || (link.target && link.target !== '_self') || link.hasAttribute('download')) return;
  const url = new URL(link.href);
  if (!link.hasAttribute('data-fve-link-navigation') && url.origin === location.origin && !url.hash && url.pathname !== location.pathname) link.hash = 'main-content';
}, true);
