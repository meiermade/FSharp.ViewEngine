let colors;

// The product owns currency formatting, balance colors and its brush interaction.
document.addEventListener('chart-render', event => {
  const host = event.target;
  if (!(host instanceof HTMLElement) || !host.hasAttribute('data-balance-chart-currency')) return;
  const { chart } = event.detail;
  const currency = host.dataset.balanceChartCurrency;
  const format = currency ? { style: 'currency', currency } : {};
  const number = new Intl.NumberFormat('en-US', { ...format, maximumFractionDigits: 2 });
  const compact = new Intl.NumberFormat('en-US', { ...format, notation: 'compact', maximumFractionDigits: 1 });
  const style = getComputedStyle(host);
  if (!colors) {
    const canvas = document.createElement('canvas');
    canvas.width = canvas.height = 1;
    colors = canvas.getContext('2d', { willReadFrequently: true });
  }
  // ECharts' color parser needs browser-resolved RGB rather than the theme's OKLCH tokens.
  const color = token => {
    colors.clearRect(0, 0, 1, 1);
    colors.fillStyle = style.getPropertyValue(token).trim();
    colors.fillRect(0, 0, 1, 1);
    const [r, g, b, a] = colors.getImageData(0, 0, 1, 1).data;
    return `rgba(${r},${g},${b},${a / 255})`;
  };
  const actual = color('--fve-brand-solid'), scenario = color('--fve-muted-text');
  chart.setOption({
    tooltip: { valueFormatter: value => number.format(value) },
    yAxis: { axisLabel: { formatter: value => compact.format(value) } },
    brush: { brushStyle: { color: color('--fve-brand-subtle'), borderColor: color('--fve-brand-ring') } },
    series: [
      { id: 'actual', lineStyle: { color: actual }, itemStyle: { color: actual } },
      { id: 'scenario', lineStyle: { color: scenario }, itemStyle: { color: scenario } }
    ]
  }, { silent: true });
  chart.dispatchAction({ type: 'brush', areas: [] }, { silent: true });
  chart.dispatchAction({ type: 'takeGlobalCursor', key: 'brush', brushOption: { brushType: 'lineX', brushMode: 'single' } });
});
