import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-time-picker', {
  mode: 'light', scope: false,
  setup: ({ host, cleanup }) => {
    const events = new AbortController(), options = { signal: events.signal };
    const read = () => JSON.parse(host.getAttribute('settings'));
    const native = () => host.querySelector('[data-time-value]');
    const input = () => host.querySelector('[data-time-input]');
    const panel = () => host.querySelector('[data-fve-popover-content]');
    const choices = () => [...host.querySelectorAll('[data-time-option]')];
    const minute = value => {
      const match = /^(\d{1,2}):(\d{2})(?:\s*([ap]m))?$/i.exec(value.trim());
      if (!match) return null;
      let hour = Number(match[1]); const minutes = Number(match[2]);
      if (minutes > 59 || (match[3] ? hour < 1 || hour > 12 : hour > 23)) return null;
      if (match[3]) hour = hour % 12 + (match[3].toLowerCase() === 'pm' ? 12 : 0);
      return hour * 60 + minutes;
    };
    const clock = value => `${String(Math.floor(value / 60)).padStart(2, '0')}:${String(value % 60).padStart(2, '0')}`;
    let settings = read(), baseline = settings.initial, ownError = '', lastCommitted = native().value, active = '';
    const format = value => {
      if (!value) return '';
      const at = minute(value); if (at == null) return value;
      return settings.format === '24' ? clock(at) : `${Math.floor(at / 60) % 12 || 12}:${String(at % 60).padStart(2, '0')} ${at < 720 ? 'AM' : 'PM'}`;
    };
    let draft = format(native().value);
    const allowed = at => at != null && at >= minute(native().min || '00:00') && at <= minute(native().max || '23:59') && (at - minute(native().min || '00:00')) % (Number(native().step || 1800) / 60) === 0;
    const available = () => choices().filter(choice => !choice.hidden);
    const promote = () => {
      const id = host.id.slice(0, -6);
      native().id = id + '-value'; native().type = 'hidden';
      for (const name of ['aria-label', 'aria-labelledby']) {
        if (native().hasAttribute(name)) { input().setAttribute(name, native().getAttribute(name)); native().removeAttribute(name); }
      }
      input().id = id; input().disabled = settings.disabled;
      host.querySelector('[data-time-enhanced]').hidden = false;
    };
    const synchronizeValue = () => {
      if (!host.isConnected) return;
      promote();
      const at = draft ? minute(draft) : null;
      const error = !draft ? '' : at == null ? 'Enter a time such as 9:30 AM or 14:30.' : !allowed(at) ? 'Choose a time within the allowed hours and interval.' : '';
      native().value = draft && !error ? clock(at) : '';
      if (input().value !== draft) input().value = draft;
      // Do not erase a composed control's availability error during a structural update.
      if (error || !input().validity.customError || input().validationMessage === ownError) input().setCustomValidity(error);
      ownError = error;
    };
    const synchronizeOptions = () => {
      if (!host.isConnected) return;
      const open = panel()?.matches(':popover-open');
      input().setAttribute('aria-expanded', String(!!open));
      if (open && active) input().setAttribute('aria-activedescendant', active);
      else input().removeAttribute('aria-activedescendant');
      for (const choice of choices()) {
        choice.hidden = !allowed(minute(choice.dataset.timeOption));
        choice.setAttribute('aria-selected', String(choice.dataset.timeOption === native().value));
        choice.tabIndex = open && choice.id === active ? 0 : -1;
        if (choice.id === active) choice.dataset.active = 'true'; else delete choice.dataset.active;
      }
    };
    const close = () => { if (panel()?.matches(':popover-open')) panel().hidePopover(); active = ''; synchronizeOptions(); };
    const commit = () => {
      synchronizeValue();
      if (!ownError && native().value) { draft = format(native().value); synchronizeValue(); }
      if (native().value !== lastCommitted) {
        lastCommitted = native().value;
        native().dispatchEvent(new Event('change', { bubbles: true }));
      }
      synchronizeOptions();
    };
    const activate = choice => {
      const moveFocus = document.activeElement?.matches('[data-time-option]') && host.contains(document.activeElement);
      active = choice?.id || ''; synchronizeOptions();
      if (choice && moveFocus) choice.focus({ preventScroll: true });
      if (choice) queueMicrotask(() => { if (host.isConnected && panel().matches(':popover-open')) choice.scrollIntoView({ block: 'nearest' }); });
    };
    // Keep Popover's native anchoring; bound the list to the larger available side.
    const constrainPanel = () => {
      const viewport = window.visualViewport, rect = host.getBoundingClientRect();
      const top = viewport?.offsetTop || 0, bottom = top + (viewport?.height || window.innerHeight);
      const margin = parseFloat(getComputedStyle(document.documentElement).fontSize);
      const space = Math.max(rect.top - top, bottom - rect.bottom) - margin;
      panel().style.maxHeight = `min(18rem, ${Math.max(0, Math.floor(space))}px)`;
    };
    const constrainOpenPanel = () => { if (panel()?.matches(':popover-open')) constrainPanel(); };
    window.addEventListener('resize', constrainOpenPanel, options);
    window.addEventListener('scroll', constrainOpenPanel, { ...options, capture: true });
    window.visualViewport?.addEventListener('resize', constrainOpenPanel, options);
    window.visualViewport?.addEventListener('scroll', constrainOpenPanel, options);
    host.addEventListener('beforetoggle', event => {
      if (event.target === panel() && event.newState === 'open') { active = ''; constrainPanel(); }
    }, { ...options, capture: true });
    const open = () => {
      if (settings.disabled || settings.readOnly) return;
      panel().showPopover({ source: host.querySelector('[data-time-toggle]') });
      synchronizeOptions();
      activate(available().find(choice => choice.dataset.timeOption === native().value) || available()[0]);
      input().focus({ preventScroll: true });
    };
    host.addEventListener('input', event => {
      if (event.target === native()) { draft = format(native().value); lastCommitted = native().value; }
      else if (event.target === input()) draft = input().value;
      else return;
      synchronizeValue(); synchronizeOptions();
    }, options);
    host.addEventListener('change', event => {
      if (event.target !== input()) return;
      // Expose one canonical change, not a second change from the display-only text field.
      event.stopPropagation(); commit();
    }, { ...options, capture: true });
    host.addEventListener('mousedown', event => { if (event.target.closest('[data-time-option]')) event.preventDefault(); }, options);
    host.addEventListener('click', event => {
      const choice = event.target.closest('[data-time-option]');
      if (!choice || choice.hidden || settings.disabled || settings.readOnly) return;
      draft = format(choice.dataset.timeOption); commit(); close(); input().focus();
    }, options);
    host.addEventListener('keydown', event => {
      if (event.defaultPrevented || event.isComposing || settings.disabled || settings.readOnly || (event.target !== input() && !event.target.closest('[data-time-option]'))) return;
      const expanded = panel().matches(':popover-open'), values = available();
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        if (!expanded) { open(); return; }
        const index = values.findIndex(choice => choice.id === active);
        activate(values[Math.max(0, Math.min(values.length - 1, index + (event.key === 'ArrowDown' ? 1 : -1)))]);
      } else if (expanded && (event.key === 'Home' || event.key === 'End')) {
        event.preventDefault(); activate(event.key === 'Home' ? values[0] : values.at(-1));
      } else if (event.key === 'Enter') {
        event.preventDefault();
        const choice = expanded && values.find(choice => choice.id === active);
        if (choice) draft = format(choice.dataset.timeOption);
        commit(); close();
      } else if (event.key === 'Escape' && expanded) {
        event.preventDefault(); close(); input().focus();
      } else if (event.key === 'Tab' && expanded) close();
    }, { ...options, capture: true });
    host.addEventListener('toggle', event => {
      if (event.target !== panel()) return;
      synchronizeOptions();
      if (event.newState === 'open') {
        // Native toggle is queued: do not overwrite a keyboard move made since opening.
        if (!active) activate(available().find(choice => choice.dataset.timeOption === native().value) || available()[0]);
        input().focus({ preventScroll: true });
      } else if (!panel().matches(':popover-open')) active = '';
    }, { ...options, capture: true });
    const reconcile = () => {
      const next = read(), changed = next.initial !== settings.initial || next.format !== settings.format;
      settings = next; baseline = next.initial;
      if (changed) { input().setCustomValidity(''); ownError = ''; draft = format(next.initial); lastCommitted = next.initial; }
      synchronizeValue(); synchronizeOptions(); constrainOpenPanel();
    };
    const observer = new MutationObserver(reconcile);
    observer.observe(host, { attributes: true, attributeFilter: ['settings'], childList: true });
    const bounds = new MutationObserver(synchronizeOptions);
    bounds.observe(native(), { attributes: true, attributeFilter: ['min', 'max', 'step'] });
    host.addEventListener('datastar-scope-children', reconcile, options);
    native().form?.addEventListener('reset', event => queueMicrotask(() => {
      if (event.defaultPrevented || !host.isConnected) return;
      input().setCustomValidity(''); ownError = ''; draft = format(baseline); lastCommitted = baseline;
      synchronizeValue(); close();
    }), options);
    synchronizeValue(); synchronizeOptions();
    cleanup(() => { events.abort(); observer.disconnect(); bounds.disconnect(); });
  },
});
