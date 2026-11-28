import './spec-fixture.js';
import './balance-chart.js';
import '../../Example.Components/Components/Assets/button.js';
import '../../Example.Components/Components/Assets/masked-value.js';
import '../../Example.Components/Components/Assets/avatar.js';
import '../../Example.Components/Components/Assets/checkbox.js';
import '../../Example.Components/Components/Assets/notification.js';
import '../../Example.Components/Components/Assets/tabs.js';
import '../../Example.Components/Components/Assets/resizable.js';
import '../../Example.Components/Components/Assets/presentation.js';
import '../../Example.Components/Components/Assets/dialog.js';
import '../../Example.Components/Components/Assets/command.js';
import '../../Example.Components/Components/Assets/drawer.js';
import '../../Example.Components/Components/Assets/dropdown-menu.js';
import '../../Example.Components/Components/Assets/select.js';
import '../../Example.Components/Components/Assets/popover.js';
import '../../Example.Components/Components/Assets/time-picker.js';
import '../../Example.Components/Components/Assets/date-picker.js';
import '../../Example.Components/Components/Assets/scroll-region.js';
import '../../Example.Components/Components/Assets/side-nav.js';
import '../../Example.Components/Components/Assets/code-block.js';
import '../../Example.Components/Components/Assets/mermaid.js';
import '../../Example.Components/Components/Assets/table.js';
import '../../Example.Components/Components/Assets/breadcrumbs.js';

// Ordinary links remain native. The mobile drawer lands keyboard users on the destination's main region.
document.addEventListener('click', event => {
  const link = event.target.closest?.('dialog[id$="template-navigation-dialog"][open] a[href]');
  if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || !link || (link.target && link.target !== '_self') || link.hasAttribute('download')) return;
  const url = new URL(link.href);
  if (url.origin === location.origin && !url.hash && url.pathname !== location.pathname) link.hash = 'main-content';
}, true);

// chart.js is a separate build entry so neither it nor ECharts loads until needed.
let chart;
const loadChart = () => {
  if (!chart && document.querySelector('fve-chart')) chart = import('../../Example.Components/Components/Assets/chart.js');
};
loadChart();
const observer = new MutationObserver(loadChart);
observer.observe(document.body, { childList: true, subtree: true });
