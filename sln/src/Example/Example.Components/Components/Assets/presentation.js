import { rocket } from './datastar-rocket.1.0.4.js';
import './button.js';

rocket('fve-presentation', {
  mode: 'light', scope: false,
  setup: ({ host, $$, effect, cleanup }) => {
    const events = new AbortController(), options = { signal: events.signal };
    const stage = () => host.querySelector(':scope > [data-fve-presentation-stage]');
    const slides = () => [...(stage()?.children || [])].filter(el => el.hasAttribute('data-fve-slide-viewport'));
    const button = action => host.querySelector(`[data-fve-presentation-action="${action}"] button`);
    $$('index', 0); $$('expanded', false);
    let background = [], scrolling = [], printView;
    const visibleIndex = () => slides().map((el, index) => {
      const r = el.getBoundingClientRect();
      return { index, height: Math.max(0, Math.min(innerHeight, r.bottom) - Math.max(0, r.top)) };
    }).reduce((best, item) => item.height > best.height ? item : best, { index: $$.index, height: 0 }).index;

    const fit = () => {
      if (!$$.expanded) return;
      const box = stage().getBoundingClientRect();
      if (!box.width || !box.height) return;
      const controls = host.querySelector(':scope > [data-fve-presentation-controls]');
      const controlHeight = controls.getBoundingClientRect().height;
      const gap = parseFloat(getComputedStyle(host).rowGap) || 0;
      const scale = Math.max(0, Math.min(box.width / 1280, (box.height - controlHeight - gap) / 720));
      const width = 1280 * scale, height = 720 * scale;
      const top = Math.max(0, (box.height - controlHeight - gap - height) / 2);
      const bounds = host.getBoundingClientRect();
      // Fit the controls and canvas as one group; align to the canvas, not the outer viewport.
      host.style.setProperty('--fve-internal-presentation-scale', String(scale));
      host.style.setProperty('--fve-internal-presentation-canvas-center', `${top + controlHeight + gap + height / 2}px`);
      host.style.setProperty('--fve-internal-presentation-controls-top', `${box.top - bounds.top + top}px`);
      host.style.setProperty('--fve-internal-presentation-controls-right', `${bounds.right - box.right + (box.width - width) / 2}px`);
      host.style.setProperty('--fve-internal-presentation-controls-width', `${(box.width + width) / 2}px`);
    };
    const synchronize = () => {
      const items = slides();
      // Keep this effect read-only: Rocket clears its signal scope before disposing effects.
      const index = Math.max(0, Math.min($$.index || 0, items.length - 1));
      host.dataset.expanded = String($$.expanded);
      host.querySelector('[data-fve-presentation-controls]').hidden = false;
      host.querySelector('[data-fve-presentation-navigation]').hidden = !$$.expanded;
      host.querySelector('[data-fve-presentation-expand]').hidden = $$.expanded;
      host.querySelector('[data-fve-presentation-restore]').hidden = !$$.expanded;
      const position = host.querySelector('[data-fve-presentation-position]'), text = `${index + 1} of ${items.length}`;
      if (position.textContent !== text) position.textContent = text;
      const focusedSlide = document.activeElement?.closest('[data-fve-slide-viewport]');
      if ($$.expanded && focusedSlide && items.includes(focusedSlide) && focusedSlide !== items[index]) button('restore')?.focus({ preventScroll: true });
      for (const [action, disabled] of [['previous', index === 0], ['next', index === items.length - 1]]) {
        if ($$.expanded && disabled && document.activeElement === button(action)) button('restore')?.focus({ preventScroll: true });
        host.querySelector(`[data-fve-presentation-action="${action}"]`).toggleAttribute('disabled', disabled);
        if (button(action)) button(action).disabled = disabled;
      }
      items.forEach((slide, position) => { slide.hidden = $$.expanded && position !== index; });
      fit();
    };
    const releaseBackground = () => {
      for (const [el, inert] of background) el.inert = inert;
      for (const [el, overflow] of scrolling) el.style.overflow = overflow;
      background = []; scrolling = [];
    };
    const restore = (returnFocus = true) => {
      const current = slides()[$$.index];
      $$.expanded = false;
      if (host.matches(':popover-open')) host.hidePopover();
      host.removeAttribute('popover'); host.removeAttribute('aria-modal'); host.setAttribute('role', 'group');
      releaseBackground(); synchronize();
      if (returnFocus) requestAnimationFrame(() => {
        if (!host.isConnected || $$.expanded) return;
        current?.scrollIntoView({ block: 'start' });
        button('expand')?.focus({ preventScroll: true });
      });
    };
    const expand = (index = visibleIndex()) => {
      // Top-layer promotion preserves slide DOM and nested Rocket instances, even inside clipped previews.
      $$.index = index;
      for (let child = host, parent = host.parentElement; parent; child = parent, parent = parent.parentElement) {
        for (const sibling of parent.children) if (sibling !== child && sibling instanceof HTMLElement) {
          background.push([sibling, sibling.inert]); sibling.inert = true;
        }
        scrolling.push([parent, parent.style.overflow]); parent.style.overflow = 'hidden';
      }
      host.setAttribute('popover', 'manual'); host.setAttribute('role', 'dialog'); host.setAttribute('aria-modal', 'true');
      $$.expanded = true; synchronize(); host.showPopover(); fit();
      button('restore')?.focus({ preventScroll: true });
    };
    // Printing needs ordinary paginated flow rather than the viewer's top layer.
    window.addEventListener('beforeprint', () => {
      if (!$$.expanded) return;
      printView = { index: $$.index, focus: document.activeElement };
      restore(false);
    }, options);
    window.addEventListener('afterprint', () => {
      if (!printView) return;
      const previous = printView; printView = undefined;
      if (!host.isConnected) return;
      expand(previous.index);
      if (previous.focus?.isConnected) previous.focus.focus({ preventScroll: true });
    }, options);
    const go = index => {
      if (!$$.expanded) return;
      $$.index = Math.max(0, Math.min(slides().length - 1, index));
      synchronize();
    };
    host.addEventListener('click', event => {
      const control = event.target.closest('[data-fve-presentation-action]');
      if (event.defaultPrevented || control?.closest('fve-presentation') !== host || control.hasAttribute('disabled')) return;
      const action = control.dataset.fvePresentationAction;
      switch (action) {
        case 'previous':
        case 'next':
          go($$.index + (action === 'previous' ? -1 : 1));
          // Safari pointer clicks do not focus buttons; keep subsequent keys inside the viewer.
          (button(action)?.disabled ? button('restore') : button(action))?.focus({ preventScroll: true });
          break;
        case 'expand': expand(); break;
        case 'restore': restore(); break;
      }
    }, options);
    host.addEventListener('keydown', event => {
      if (!$$.expanded || event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey && event.key !== 'Tab') return;
      if ([...host.querySelectorAll(':popover-open, dialog:modal')].length) return;
      if (event.key === 'Escape') { event.preventDefault(); restore(); return; }
      if (event.key === 'Tab') {
        const targets = [...host.querySelectorAll('button,a[href],input,select,textarea,[tabindex]')].filter(el => el.tabIndex >= 0 && !el.matches(':disabled') && !el.closest('[inert]') && el.getClientRects().length);
        const first = targets[0], last = targets.at(-1);
        if (event.shiftKey && (document.activeElement === first || !targets.includes(document.activeElement))) { event.preventDefault(); last?.focus(); }
        else if (!event.shiftKey && (document.activeElement === last || !targets.includes(document.activeElement))) { event.preventDefault(); first?.focus(); }
        return;
      }
      if (event.target.closest('input,textarea,select,[contenteditable]:not([contenteditable="false"]),[role="slider"],[role="combobox"]')) return;
      if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
      event.preventDefault();
      go(event.key === 'Home' ? 0 : event.key === 'End' ? slides().length - 1 : $$.index + (event.key === 'ArrowLeft' ? -1 : 1));
    }, options);
    // A queued close from beforeprint can arrive after afterprint has reopened the viewer.
    host.addEventListener('toggle', event => { if (event.target === host && event.newState === 'closed' && $$.expanded && !host.matches(':popover-open')) restore(); }, options);
    const resize = new ResizeObserver(fit);
    const observeSize = () => { resize.observe(stage()); resize.observe(host.querySelector(':scope > [data-fve-presentation-controls]')); };
    observeSize();
    const mutations = new MutationObserver(synchronize);
    mutations.observe(stage(), { childList: true });
    host.addEventListener('datastar-scope-children', event => {
      if (event.target !== host) return;
      if ($$.expanded) restore(false);
      resize.disconnect(); mutations.disconnect();
      observeSize(); mutations.observe(stage(), { childList: true });
      synchronize();
    }, options);
    effect(synchronize);
    cleanup(() => {
      events.abort(); resize.disconnect(); mutations.disconnect(); releaseBackground();
      if (host.matches(':popover-open')) host.hidePopover();
      host.removeAttribute('popover'); host.removeAttribute('data-expanded'); host.removeAttribute('aria-modal');
      host.setAttribute('role', 'group');
      for (const slide of slides()) slide.hidden = false;
      const controls = host.querySelector('[data-fve-presentation-controls]');
      if (controls) controls.hidden = true;
    });
  },
});
