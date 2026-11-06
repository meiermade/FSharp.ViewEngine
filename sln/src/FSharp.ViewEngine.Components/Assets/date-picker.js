import { rocket } from './datastar-rocket.1.0.4.js';
import './popover.js';
import './calendar-selection.js';

rocket('fve-date-picker', {
  mode: 'light', scope: false,
  setup: ({ host, cleanup }) => {
    const events = new AbortController();
    const synchronize = () => {
      const start = host.querySelector('[data-fve-calendar-field=start]')?.value || '';
      const end = host.querySelector('[data-fve-calendar-field=end]')?.value || '';
      const formatter = new Intl.DateTimeFormat(host.lang, { dateStyle: 'short', timeZone: 'UTC' });
      const format = value => formatter.format(new Date(value + 'T00:00:00Z'));
      const range = host.hasAttribute('range');
      const value = start ? format(start) + (range ? end ? ' – ' + format(end) : ' – Choose end date' : '') : range ? 'Choose date range' : 'Choose date';
      const label = host.querySelector('[data-fve-date-picker-value]');
      if (label && label.textContent !== value) label.textContent = value;
    };
    host.addEventListener('fve-calendar-change', synchronize, { signal: events.signal });
    host.addEventListener('datastar-scope-children', synchronize, { signal: events.signal });
    const observer = new MutationObserver(synchronize);
    observer.observe(host, { childList: true, subtree: true, attributes: true, attributeFilter: ['value', 'lang', 'range'] });
    synchronize();
    cleanup(() => { events.abort(); observer.disconnect(); });
  },
});
