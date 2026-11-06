import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-side-nav', {
  mode: 'light', scope: false,
  setup: ({ host, cleanup }) => {
    const events = new AbortController(), options = { signal: events.signal };
    const initialized = new WeakMap(), actions = new WeakSet(), observed = new WeakMap();
    const prefix = () => 'fve-side-nav:' + host.getAttribute('persistence-key');
    const nav = () => host.querySelector(':scope > [data-fve-side-nav-content]');
    const saveScroll = () => { const content = nav(); if (content?.clientHeight > 0) try { sessionStorage.setItem(prefix() + ':scroll', String(content.scrollTop)); } catch {} };
    const synchronize = group => group.querySelector('summary')?.setAttribute('aria-expanded', String(group.open));
    const saveDisclosures = () => {
      // Desktop and closed mobile navigation share a key; only the visible owner can flush pending native toggles.
      if (!host.getClientRects().length) return;
      for (const group of host.querySelectorAll('details[data-fve-side-nav-group]:not([data-fve-side-nav-external])')) {
        if (group.closest('fve-side-nav') === host) try { sessionStorage.setItem(prefix() + ':group:' + group.dataset.fveSideNavGroup, String(group.open)); } catch {}
      }
    };
    const intersection = new IntersectionObserver(entries => {
      for (const entry of entries) if (entry.isIntersecting && entry.target.clientHeight > 0) {
        try { const saved = Number(sessionStorage.getItem(prefix() + ':scroll')); if (Number.isFinite(saved) && saved >= 0) entry.target.scrollTop = saved; } catch {}
        intersection.unobserve(entry.target);
      }
    });
    const discover = () => {
      for (const group of host.querySelectorAll('details[data-fve-side-nav-group]')) {
        if (group.closest('fve-side-nav') !== host) continue;
        const key = prefix() + ':group:' + group.dataset.fveSideNavGroup;
        const policy = key + ':' + group.hasAttribute('data-fve-side-nav-external');
        if (initialized.get(group) !== policy) {
          initialized.set(group, policy);
          if (!group.hasAttribute('data-fve-side-nav-external')) try {
            const saved = sessionStorage.getItem(key);
            group.open = saved === 'true' || saved === 'false' ? saved === 'true' : group.dataset.fveSideNavDefaultOpen === 'true';
            sessionStorage.setItem(key, String(group.open));
          } catch {}
        }
        synchronize(group);
      }
      for (const action of host.querySelectorAll('[data-fve-side-nav-action]')) if (action.closest('fve-side-nav') === host && !actions.has(action)) {
        actions.add(action); action.addEventListener('click', event => event.stopPropagation(), options);
      }
      const content = nav();
      if (content && observed.get(content) !== prefix()) { observed.set(content, prefix()); intersection.observe(content); }
    };
    host.addEventListener('toggle', event => {
      const group = event.target;
      if (!group.matches?.('details[data-fve-side-nav-group]') || group.closest('fve-side-nav') !== host) return;
      synchronize(group);
      if (group.hasAttribute('data-fve-side-nav-external')) group.dispatchEvent(new CustomEvent('fve-side-nav-toggle', { detail: { open: group.open } }));
      else try { sessionStorage.setItem(prefix() + ':group:' + group.dataset.fveSideNavGroup, String(group.open)); } catch {}
    }, { ...options, capture: true });
    window.addEventListener('pagehide', saveDisclosures, options);
    host.addEventListener('scroll', event => { if (event.target === nav()) saveScroll(); }, { ...options, capture: true, passive: true });
    host.addEventListener('click', event => { if (nav()?.contains(event.target)) saveScroll(); }, { ...options, capture: true });
    const observer = new MutationObserver(discover);
    observer.observe(host, { childList: true, subtree: true, attributes: true, attributeFilter: ['persistence-key', 'data-fve-side-nav-group', 'data-fve-side-nav-external'] });
    host.addEventListener('datastar-scope-children', discover, options);
    discover();
    cleanup(() => { saveDisclosures(); events.abort(); observer.disconnect(); intersection.disconnect(); });
  },
});
