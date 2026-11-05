import './spec-fixture.js';
import './balance-chart.js';
import './button.js';
import './table.js';
import './breadcrumbs.js';

// Keep the chart and ECharts out of documents that contain no chart.
let chart;
const loadChart = () => {
  if (!chart && document.querySelector('fve-chart')) chart = import('./chart.js');
};
loadChart();
const observer = new MutationObserver(loadChart);
observer.observe(document.body, { childList: true, subtree: true });
