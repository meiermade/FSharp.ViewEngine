import '../../Docs.Components/Components/Assets/button.js';
import '../../Docs.Components/Components/Assets/table.js';
import '../../Docs.Components/Components/Assets/tag-input.js';
import '../../Docs.Components/Components/Assets/breadcrumbs.js';

// Chart and ECharts stay separate from the ordinary control bundle.
let chart;
const loadChart = () => {
  if (!chart && document.querySelector('fve-chart')) chart = import('../../Docs.Components/Components/Assets/chart.js');
};
loadChart();
new MutationObserver(loadChart).observe(document.body, { childList: true, subtree: true });
