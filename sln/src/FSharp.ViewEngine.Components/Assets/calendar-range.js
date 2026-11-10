import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-calendar-range', {
  mode: 'light', scope: false,
  setup: ({ host, $$, effect, cleanup }) => {
    const events = new AbortController(), options = { signal: events.signal };
    const read = () => JSON.parse(host.getAttribute('settings'));
    const field = name => host.querySelector(name === 'duration' ? 'fve-select[data-range-field=duration] [data-fve-select-native]' : 'fve-time-picker[data-range-field=start] [data-time-value]');
    const grid = () => host.querySelector('.fve-calendar-events');
    const minute = time => /^\d{2}:\d{2}$/.test(time) ? Number(time.slice(0, 2)) * 60 + Number(time.slice(3)) : null;
    const clock = value => `${String(Math.floor(value / 60)).padStart(2, '0')}:${String(value % 60).padStart(2, '0')}`;
    const text = (element, value) => { if (element && element.textContent !== value) element.textContent = value; };
    let settings = read(), baseline = settings, drag;
    $$('time', field('start').value); $$('minutes', Number(field('duration').value));
    const valid = () => settings.available.some(([start, length]) => start === minute($$.time) && length === $$.minutes);
    const synchronize = () => {
      if (!host.isConnected || !field('start') || !field('duration')) return;
      const start = minute($$.time), within = start != null && start >= settings.minimum && start + $$.minutes <= settings.maximum;
      const available = within && valid();
      const startField = field('start');
      if (startField.value !== $$.time) {
        startField.value = $$.time;
        startField.dispatchEvent(new Event('input', { bubbles: true }));
      }
      const duration = field('duration');
      if (duration.value !== String($$.minutes)) {
        duration.value = String($$.minutes);
        duration.dispatchEvent(new Event('input', { bubbles: true }));
      }
      field('start').max = clock(settings.maximum - $$.minutes);
      host.querySelector('[data-time-input]').setCustomValidity(available ? '' : 'Choose an available start time and duration.');
      const selection = host.querySelector('[data-range-selection]');
      selection.hidden = !within;
      selection.dataset.valid = String(available);
      host.style.setProperty('--fve-selection-start', String((start ?? settings.minimum) - settings.minimum + 1));
      host.style.setProperty('--fve-selection-duration', String($$.minutes));
      text(host.querySelector('[data-range-label]'), within ? `${clock(start)}–${clock(start + $$.minutes)}` : 'Choose a start time');
      text(host.querySelector('[data-range-status]'), available ? 'Selected time is available.' : 'This time is unavailable. Choose another start time or duration.');
      const move = host.querySelector('[data-range-part=move]');
      move.setAttribute('aria-describedby', `${host.id.replace(/-range$/, '')}-range-help ${host.id.replace(/-range$/, '')}-range-status`);
    };
    const notify = () => {
      synchronize();
      const start = minute($$.time);
      host.dispatchEvent(new CustomEvent('fve-calendar-range-change', { bubbles: true, detail: {
        date: settings.date, time: $$.time, end: start == null ? '' : clock(start + $$.minutes), minutes: $$.minutes, valid: valid(),
      } }));
    };
    const apply = (start, length) => {
      $$.minutes = length;
      $$.time = clock(Math.max(settings.minimum, Math.min(settings.maximum - length, start)));
      synchronize();
    };
    const cancel = () => {
      if (!drag) return;
      const previous = drag; drag = undefined;
      $$.time = previous.time; $$.minutes = previous.length;
      if (grid()?.hasPointerCapture(previous.id)) grid().releasePointerCapture(previous.id);
      synchronize();
    };
    const point = event => {
      const box = grid().getBoundingClientRect();
      return Math.round(((event.clientY - box.top) / box.height * (settings.maximum - settings.minimum) + settings.minimum) / settings.step) * settings.step;
    };
    const nearestDuration = value => settings.durations.reduce((best, length) => Math.abs(length - value) < Math.abs(best - value) ? length : best);
    host.addEventListener('change', event => {
      if (event.target === field('start')) $$.time = event.target.value;
      else if (event.target === field('duration')) $$.minutes = Number(event.target.value);
      else return;
      notify();
    }, options);
    host.addEventListener('fve-select-change', event => {
      if (event.target !== field('duration')?.closest('fve-select')) return;
      $$.minutes = Number(event.detail.value); notify();
    }, options);
    host.addEventListener('pointerdown', event => {
      const surface = grid(), part = event.target.closest('[data-range-part]')?.dataset.rangePart;
      if (event.defaultPrevented || event.button !== 0 || !surface.contains(event.target) || event.target.closest('.fve-calendar-event, [data-fve-popover-content]') || (event.pointerType === 'touch' && !part)) return;
      event.preventDefault();
      host.querySelector('[data-range-part=move]').focus({ preventScroll: true });
      // Blank clicks choose the whole highlighted slot; only drag geometry snaps to boundaries.
      const slot = !part && event.target.closest('[data-range-start]');
      const anchor = slot ? Number(slot.dataset.rangeStart) : point(event);
      drag = { id: event.pointerId, point: anchor, y: event.clientY, time: $$.time, start: minute($$.time) ?? settings.start, length: $$.minutes, mode: part || 'create' };
      surface.setPointerCapture(event.pointerId);
      if (drag.mode === 'create') apply(drag.point, drag.length);
    }, options);
    host.addEventListener('pointermove', event => {
      if (!drag || event.pointerId !== drag.id) return;
      const at = point(event), previous = drag;
      let start = previous.start, length = previous.length;
      if (previous.mode === 'move') start += at - previous.point;
      else if (previous.mode === 'create') {
        start = Math.min(previous.point, at);
        if (Math.abs(event.clientY - previous.y) > 5) length = nearestDuration(Math.max(settings.step, Math.abs(at - previous.point)));
      } else {
        length = nearestDuration(previous.mode === 'end' ? at - previous.start : previous.start + previous.length - at);
        if (previous.mode === 'start') start = previous.start + previous.length - length;
      }
      apply(start, length);
    }, options);
    host.addEventListener('pointerup', event => {
      if (!drag || event.pointerId !== drag.id) return;
      drag = undefined;
      if (grid().hasPointerCapture(event.pointerId)) grid().releasePointerCapture(event.pointerId);
      notify();
    }, options);
    host.addEventListener('pointercancel', cancel, options);
    host.addEventListener('lostpointercapture', cancel, options);
    host.addEventListener('click', event => {
      const target = event.target.closest('[data-range-start]');
      if (target && (event.detail === 0 || event.pointerType === 'touch')) { apply(Number(target.dataset.rangeStart), $$.minutes); notify(); }
    }, options);
    host.addEventListener('keydown', event => {
      if (event.key === 'Escape' && drag) { event.preventDefault(); cancel(); return; }
      if (event.defaultPrevented || !event.target.matches('[data-range-part=move]') || !['ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) return;
      event.preventDefault();
      const direction = event.key === 'ArrowUp' ? -1 : 1;
      let start = minute($$.time) ?? settings.start, length = $$.minutes;
      if (event.key === 'Home') start = settings.minimum;
      else if (event.key === 'End') start = settings.maximum - length;
      else if (event.shiftKey) length = settings.durations[Math.max(0, Math.min(settings.durations.length - 1, settings.durations.indexOf(length) + direction))];
      else start += direction * settings.step;
      apply(start, length); notify();
    }, options);
    host.ownerDocument.addEventListener('reset', event => queueMicrotask(() => {
      if (!host.isConnected || event.defaultPrevented || event.target !== field('start')?.form) return;
      cancel(); $$.time = clock(baseline.start); $$.minutes = baseline.minutes; synchronize();
    }), options);
    const reconcile = () => {
      const next = read();
      if (next.start !== settings.start || next.minutes !== settings.minutes || next.date !== settings.date) {
        cancel(); $$.time = clock(next.start); $$.minutes = next.minutes;
      }
      settings = next; baseline = next; synchronize();
    };
    const observer = new MutationObserver(reconcile);
    observer.observe(host, { attributes: true, attributeFilter: ['settings'], childList: true });
    host.addEventListener('datastar-scope-children', reconcile, options);
    effect(synchronize);
    cleanup(() => { cancel(); events.abort(); observer.disconnect(); });
  },
});
