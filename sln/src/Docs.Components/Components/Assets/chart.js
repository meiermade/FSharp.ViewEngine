import { rocket } from './datastar-rocket.1.0.4.js';

let renderer;
const loadRenderer = () => renderer ??= import('./echarts.6.1.0.esm.min.js').catch(error => {
  renderer = undefined;
  throw error;
});
const validOptions = value => value && typeof value === 'object' && !Array.isArray(value);

rocket('fve-chart', {
  renderOnPropChange: false,
  props: ({ json, bool }) => ({ options: json, events: json.default([]), empty: bool.default(false) }),
  manifest: { events: [
    { name: 'chart-render', kind: 'custom-event', bubbles: true, composed: true, description: 'After options and theme are applied. detail.chart exposes the ECharts instance for application-owned callbacks and actions.' },
    { name: 'chart-click', kind: 'custom-event', bubbles: true, composed: true, description: 'An opted-in ECharts click event. Other names supplied through events use the same chart- prefix.' },
    { name: 'chart-error', kind: 'custom-event', bubbles: true, composed: true, description: 'An ECharts rendering error, available as detail.error. Valid updated options can recover the host.' }
  ] },
  render: ({ html }) => html`
    <style>
      :host { display:block; min-width:0; color:var(--fve-text); font:inherit; }
      .plot { width:100%; height:var(--fve-chart-height,18rem); }
      .plot[data-message] { display:grid; place-items:center; color:var(--fve-muted-text); }
      :host([data-ready]) slot { display:none; }
    </style>
    <div class="plot" data-ref:plot><span role="status">Loading chart…</span></div>
    <slot></slot>`,
  onFirstRender: ({ refs, host, props, observeProps, cleanup }) => {
    delete host.dataset.ready;
    let chart;
    let echarts;
    let disposed = false;
    let frame;
    let loading;
    let themeKey;
    let appliedOptions;
    const handlers = new Map();
    const canvas = document.createElement('canvas');
    canvas.width = canvas.height = 1;
    const context = canvas.getContext('2d', { willReadFrequently: true });
    // ECharts' color parser cannot consume OKLCH. Resolve explicit CSS color tokens through the browser.
    const color = (token, style) => {
      const value = style.getPropertyValue(token).trim();
      if (!value || !CSS.supports('color', value)) return token;
      context.clearRect(0, 0, 1, 1);
      context.fillStyle = value;
      context.fillRect(0, 0, 1, 1);
      const [r, g, b, a] = context.getImageData(0, 0, 1, 1).data;
      return `rgba(${r},${g},${b},${a / 255})`;
    };
    const theme = style => {
      const fontSize = parseFloat(style.fontSize);
      const text = color('--fve-text', style), muted = color('--fve-muted-text', style), border = color('--fve-border', style);
      const axis = { axisLine: { lineStyle: { color: border } }, axisLabel: { color: muted, fontSize: fontSize * 12 / 14, hideOverlap: true }, splitLine: { lineStyle: { color: border } } };
      return {
        color: ['--fve-brand-solid', '--fve-positive-text', '--fve-warning-text', '--fve-critical-text', '--fve-muted-text'].map(token => color(token, style)),
        backgroundColor: 'transparent',
        textStyle: { fontFamily: style.fontFamily, fontSize, color: text },
        legend: { textStyle: { color: text, fontSize } },
        sankey: { label: { color: text, fontSize, textBorderWidth: 0 } },
        categoryAxis: axis, valueAxis: axis, timeAxis: axis, logAxis: axis,
        tooltip: { renderMode: 'richText', confine: true, backgroundColor: color('--fve-surface', style), borderColor: border, textStyle: { color: text, fontSize } }
      };
    };
    const release = () => {
      chart?.dispose();
      chart = undefined;
      handlers.clear();
      themeKey = undefined;
      appliedOptions = undefined;
    };
    const message = text => {
      release();
      refs.plot.dataset.message = 'true';
      refs.plot.replaceChildren();
      const status = document.createElement('span');
      status.setAttribute('role', 'status');
      status.textContent = text;
      refs.plot.append(status);
    };
    const update = () => {
      if (disposed) return;
      if (!validOptions(props.options)) {
        message('Chart data is unavailable.');
        delete host.dataset.ready;
        return;
      }
      if (props.empty) {
        message('No chart data.');
        host.dataset.ready = 'true';
        return;
      }
      if (!echarts) {
        if (!loading) loading = loadRenderer().then(module => {
          if (disposed) return;
          echarts = module;
          update();
        }).catch(error => {
          loading = undefined;
          if (disposed) return;
          message('Chart renderer is unavailable.');
          console.error('Unable to load the chart renderer.', error);
        });
        return;
      }
      if (!refs.plot.clientWidth || !refs.plot.clientHeight) return;
      delete refs.plot.dataset.message;
      const style = getComputedStyle(host);
      const currentTheme = theme(style);
      const key = JSON.stringify(currentTheme);
      try {
        if (!chart) {
          refs.plot.replaceChildren();
          chart = echarts.init(refs.plot, currentTheme, { renderer: 'svg' });
        } else if (key !== themeKey) {
          // ECharts 6 setTheme recreates component models; retain settled native interaction state.
          const previous = chart.getOption();
          const interaction = {};
          if (previous.legend?.length) interaction.legend = previous.legend.map(({ id, selected }) => ({ id, selected }));
          if (previous.dataZoom?.length) interaction.dataZoom = previous.dataZoom.map(({ id, start, end, startValue, endValue, rangeMode }) => ({ id, start, end, startValue, endValue, rangeMode }));
          chart.setTheme(currentTheme, { silent: true });
          chart.setOption(interaction, { silent: true });
        }
        themeKey = key;
        const events = new Set(Array.isArray(props.events) ? props.events : []);
        for (const [name, handler] of handlers) if (!events.has(name)) { chart.off(name, handler); handlers.delete(name); }
        for (const name of events) if (!handlers.has(name)) {
          const handler = detail => host.dispatchEvent(new CustomEvent(`chart-${name}`, { bubbles: true, composed: true, detail }));
          chart.on(name, handler);
          handlers.set(name, handler);
        }
        // Replace new option documents, but do not reset native zoom or legend state for a theme/event update.
        if (props.options !== appliedOptions) {
          chart.setOption({ animation: false, aria: { enabled: true, description: host.getAttribute('aria-label') }, ...props.options }, { notMerge: true, silent: true });
          appliedOptions = props.options;
        }
        host.dataset.ready = 'true';
        host.dispatchEvent(new CustomEvent('chart-render', { bubbles: true, composed: true, detail: { chart } }));
      } catch (error) {
        message('Chart options could not be rendered.');
        delete host.dataset.ready;
        host.dispatchEvent(new CustomEvent('chart-error', { bubbles: true, composed: true, detail: { error } }));
      }
    };
    const resize = new ResizeObserver(() => {
      cancelAnimationFrame(frame);
      frame = requestAnimationFrame(() => { if (!chart) update(); chart?.resize(); });
    });
    resize.observe(refs.plot);
    const appearance = new MutationObserver(update);
    for (let element = host; element; element = element.parentElement)
      appearance.observe(element, { attributes: true, attributeFilter: ['class', 'style', 'data-fve-color-mode'] });
    observeProps(update, 'options', 'events', 'empty');
    cleanup(() => { disposed = true; resize.disconnect(); appearance.disconnect(); cancelAnimationFrame(frame); release(); delete host.dataset.ready; });
    update();
  }
});
