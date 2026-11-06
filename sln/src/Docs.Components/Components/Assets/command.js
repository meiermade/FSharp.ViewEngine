import { rocket } from './datastar-rocket.1.0.4.js';
import './dialog.js';

rocket('fve-command', {
  mode: 'light', scope: false,
  setup: ({ host, $$, cleanup }) => {
    const events = new AbortController(), options = { signal: events.signal };
    const words = value => value.toLowerCase().match(/[\p{L}\p{N}]+/gu) || [];
    const input = () => host.querySelector('[data-fve-command-input]');
    const items = () => [...host.querySelectorAll('[data-fve-command-item]')].filter(item => !item.hidden && !item.closest('[data-fve-command-group]').hidden && item.getAttribute('aria-disabled') !== 'true');
    $$('active', '');
    const select = item => {
      const previous = host.querySelector('[data-fve-command-item][aria-selected=true]');
      if (previous && previous !== item) previous.setAttribute('aria-selected', 'false');
      if (item && item.getAttribute('aria-selected') !== 'true') item.setAttribute('aria-selected', 'true');
      $$.active = item?.id || '';
      if (item) {
        if (input().getAttribute('aria-activedescendant') !== item.id) input().setAttribute('aria-activedescendant', item.id);
      } else input()?.removeAttribute('aria-activedescendant');
    };
    const update = (preserve = false) => {
      if (!input()) return;
      const query = input().value.trim().toLowerCase(), terms = words(query);
      for (const group of host.querySelectorAll('[data-fve-command-group]')) {
        const entries = [...group.querySelectorAll('[data-fve-command-item]')];
        const score = item => {
          if (!query) return 0;
          const label = item.dataset.fveCommandLabel.toLowerCase(), labels = words(label);
          const keywords = words(item.dataset.fveCommandKeywords), description = words(item.dataset.fveCommandDescription);
          if (label === query) return 0;
          if (label.startsWith(query)) return 1;
          if (terms.length && terms.every(term => labels.some(word => word.startsWith(term)))) return 2;
          if (terms.length && terms.every(term => [...labels, ...keywords].some(word => word.startsWith(term)))) return 3;
          if (terms.length && terms.every(term => [...labels, ...keywords, ...description].includes(term))) return 4;
          return Infinity;
        };
        const ranked = entries.map(item => ({ item, score: score(item) })).sort((a, b) => a.score - b.score || Number(a.item.dataset.fveCommandOrder) - Number(b.item.dataset.fveCommandOrder));
        let cursor = entries[0] || null;
        for (const entry of ranked) {
          const hidden = !Number.isFinite(entry.score);
          if (entry.item.hidden !== hidden) entry.item.hidden = hidden;
          if (entry.item !== cursor) group.insertBefore(entry.item, cursor);
          cursor = entry.item.nextElementSibling;
        }
        const hidden = !ranked.some(entry => Number.isFinite(entry.score));
        if (group.hidden !== hidden) group.hidden = hidden;
      }
      const available = items();
      select((preserve && available.find(item => item.id === $$.active)) || available[0]);
      const hasResults = [...host.querySelectorAll('[data-fve-command-group]')].some(group => !group.hidden);
      const listbox = host.querySelector('[role=listbox]'), empty = host.querySelector('[data-fve-command-empty]');
      if (listbox.hidden === hasResults) listbox.hidden = !hasResults;
      if (input().getAttribute('aria-expanded') !== String(hasResults)) input().setAttribute('aria-expanded', String(hasResults));
      if (empty.hidden !== hasResults) empty.hidden = hasResults;
    };
    host.addEventListener('input', () => update(), options);
    host.addEventListener('focusin', event => { if (event.target === input()) update(true); }, options);
    host.addEventListener('pointermove', event => {
      const item = event.target.closest('[data-fve-command-item]');
      if (item && item.getAttribute('aria-disabled') !== 'true') select(item);
    }, options);
    host.addEventListener('click', event => {
      const item = event.target.closest('[data-fve-command-item]');
      if (item?.getAttribute('aria-disabled') === 'true') { event.preventDefault(); event.stopPropagation(); }
    }, { ...options, capture: true });
    host.addEventListener('click', event => {
      if (!event.defaultPrevented && !event.ctrlKey && !event.metaKey && !event.altKey && !event.shiftKey && event.target.closest('[data-fve-command-item]:not([aria-disabled=true])')) host.closest('dialog')?.close();
    }, options);
    host.addEventListener('keydown', event => {
      if (event.target !== input() || event.isComposing || event.ctrlKey || event.metaKey || event.altKey || event.defaultPrevented) return;
      if (['ArrowDown', 'ArrowUp', 'Home', 'End', 'Enter'].includes(event.key)) {
        event.preventDefault();
        const available = items(), index = available.findIndex(item => item.id === $$.active);
        if (event.key === 'Enter') available[index]?.click();
        else {
          const next = event.key === 'Home' ? available[0] : event.key === 'End' ? available.at(-1) : available.at((index + (event.key === 'ArrowDown' ? 1 : -1) + available.length) % available.length);
          select(next); next?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
        }
      } else if (event.key === 'Escape' && !host.closest('dialog')) { input().value = ''; update(); }
    }, options);
    host.addEventListener('datastar-scope-children', () => update(true), options);
    update();
    cleanup(() => events.abort());
  },
});
