import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-calendar-selection', {
  mode: 'light', scope: false,
  setup: ({ host, $$, effect, cleanup }) => {
    const events = new AbortController(), options = { signal: events.signal };
    const read = () => JSON.parse(host.getAttribute('settings'));
    let settings = read(), baseline = settings;
    const field = kind => host.querySelector(`[data-fve-calendar-field=${kind}]`);
    const cells = () => [...host.querySelectorAll('[role=gridcell]')];
    $$('month', settings.month); $$('start', field('start').value); $$('end', field('end')?.value || '');
    const set = (el, key, value) => { if (value == null) el.removeAttribute(key); else if (el.getAttribute(key) !== String(value)) el.setAttribute(key, String(value)); };
    const blocked = value => settings.disabled || !!(settings.minimum && value < settings.minimum) || !!(settings.maximum && value > settings.maximum) || settings.unavailable.includes(value);
    const synchronize = () => {
      if (!host.isConnected) return;
      field('start').value = $$.start; if (field('end')) field('end').value = $$.end;
      const first = new Date($$.month + '-01T00:00:00Z'), offset = (first.getUTCDay() - settings.weekStart + 7) % 7;
      for (const [index, cell] of cells().entries()) {
        const date = new Date(first); date.setUTCDate(index + 1 - offset);
        const value = date.toISOString().slice(0, 10);
        set(cell, 'data-value', value); set(cell, 'aria-current', value === settings.currentDate ? 'date' : null);
        set(cell, 'data-outside', date.getUTCMonth() !== first.getUTCMonth());
        set(cell, 'aria-label', new Intl.DateTimeFormat(settings.locale, { dateStyle: 'full', timeZone: 'UTC' }).format(date));
        set(cell, 'aria-selected', $$.start === value || $$.end === value);
        set(cell, 'data-in-range', !!($$.start && $$.end && value > $$.start && value < $$.end));
        cell.disabled = blocked(value); if (cell.textContent !== String(date.getUTCDate())) cell.textContent = String(date.getUTCDate());
      }
      const caption = host.querySelector('[data-fve-calendar-caption]'), title = new Intl.DateTimeFormat(settings.locale, { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(first);
      if (caption.textContent !== title) caption.textContent = title;
      host.querySelector('[data-fve-calendar-action=previous]').disabled = settings.disabled || !!(settings.minimumMonth && $$.month <= settings.minimumMonth);
      host.querySelector('[data-fve-calendar-action=next]').disabled = settings.disabled || !!(settings.maximumMonth && $$.month >= settings.maximumMonth);
      const month = host.querySelector('[data-fve-calendar-month]'), year = host.querySelector('[data-fve-calendar-year]');
      if (month) {
        month.value = $$.month.slice(5, 7); year.value = $$.month.slice(0, 4);
        for (const option of month.options) {
          const candidate = $$.month.slice(0, 4) + '-' + option.value;
          option.disabled = settings.disabled || !!(settings.minimumMonth && candidate < settings.minimumMonth) || !!(settings.maximumMonth && candidate > settings.maximumMonth);
        }
      }
    };
    const changeMonth = month => { $$.month = settings.minimumMonth && month < settings.minimumMonth ? settings.minimumMonth : settings.maximumMonth && month > settings.maximumMonth ? settings.maximumMonth : month; };
    const moveMonth = offset => { const date = new Date($$.month + '-01T00:00:00Z'); date.setUTCMonth(date.getUTCMonth() + offset); changeMonth(date.toISOString().slice(0, 7)); };
    const focus = cell => { if (cell && !cell.disabled) { for (const item of cells()) item.tabIndex = item === cell ? 0 : -1; cell.focus(); } };
    const close = () => {
      if (!settings.close) return;
      const popup = host.closest('[popover]');
      if (popup) { popup.hidePopover(); [...document.querySelectorAll('[popovertarget]')].find(control => control.getAttribute('popovertarget') === popup.id)?.focus(); }
    };
    const notify = () => host.dispatchEvent(new CustomEvent('fve-calendar-change', { bubbles: true, detail: { start: $$.start, end: $$.end } }));
    const commit = (start, end, complete) => {
      $$.start = start; $$.end = end; synchronize(); notify();
      if (complete) close();
    };
    host.addEventListener('click', event => {
      if (settings.readOnly && event.target.closest('[role=gridcell],[data-fve-calendar-action=clear],[data-fve-calendar-action=today]')) event.preventDefault();
    }, { ...options, capture: true });
    host.addEventListener('click', event => {
      if (event.defaultPrevented || settings.disabled) return;
      const action = event.target.closest('[data-fve-calendar-action]')?.dataset.fveCalendarAction;
      if (action === 'previous' || action === 'next') { moveMonth(action === 'previous' ? -1 : 1); return; }
      if (action === 'clear') { if (!settings.readOnly) commit('', '', false); return; }
      if (action === 'today') { if (!settings.readOnly && !blocked(settings.today)) { changeMonth(settings.today.slice(0, 7)); commit(settings.today, settings.range ? settings.today : '', true); } return; }
      const cell = event.target.closest('[role=gridcell]');
      if (!cell || cell.disabled || settings.readOnly) return;
      const value = cell.dataset.value;
      if (!settings.range) commit(value, '', true);
      else if (!$$.start || $$.end) commit(value, '', false);
      else if (value < $$.start) commit(value, $$.start, true);
      else commit($$.start, value, true);
    }, options);
    host.addEventListener('change', event => {
      if (event.target.matches('[data-fve-calendar-month]')) changeMonth($$.month.slice(0, 4) + '-' + event.target.value);
      else if (event.target.matches('[data-fve-calendar-year]')) changeMonth(event.target.value + '-' + $$.month.slice(5, 7));
    }, options);
    host.addEventListener('keydown', event => {
      if (event.defaultPrevented || !event.target.matches('[role=gridcell]')) return;
      const buttons = cells(), index = buttons.indexOf(event.target), step = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 }[event.key];
      if (step != null) { event.preventDefault(); let target = index + step; while (target >= 0 && target < buttons.length && buttons[target].disabled) target += step; focus(buttons[target]); }
      else if (event.key === 'Home' || event.key === 'End') { event.preventDefault(); const week = buttons.slice(index - index % 7, index - index % 7 + 7).filter(cell => !cell.disabled); focus(event.key === 'Home' ? week[0] : week.at(-1)); }
      else if (event.key === 'PageUp' || event.key === 'PageDown') {
        event.preventDefault(); const day = Number(event.target.dataset.value.slice(8)); moveMonth(event.key === 'PageUp' ? -1 : 1);
        queueMicrotask(() => { if (!host.isConnected) return; const candidates = cells().filter(cell => !cell.disabled && cell.dataset.value.startsWith($$.month)); focus(candidates.reduce((nearest, cell) => !nearest || Math.abs(Number(cell.dataset.value.slice(8)) - day) < Math.abs(Number(nearest.dataset.value.slice(8)) - day) ? cell : nearest, null)); });
      }
    }, options);
    host.ownerDocument.addEventListener('reset', event => queueMicrotask(() => {
      if (host.isConnected && event.target === field('start')?.form && !event.defaultPrevented) { $$.month = baseline.month; $$.start = baseline.start; $$.end = baseline.ending; synchronize(); }
    }), options);
    const reconcile = () => {
      const next = read();
      if (next.month !== settings.month) $$.month = next.month;
      if (next.start !== settings.start) $$.start = next.start;
      if (next.ending !== settings.ending) $$.end = next.ending;
      settings = next; baseline = next; synchronize();
    };
    const observer = new MutationObserver(reconcile);
    observer.observe(host, { attributes: true, attributeFilter: ['settings'], childList: true, subtree: true });
    host.addEventListener('datastar-scope-children', reconcile, options);
    effect(synchronize);
    cleanup(() => { events.abort(); observer.disconnect(); });
  },
});
