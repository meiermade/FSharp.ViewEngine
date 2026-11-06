import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-tooltip', {
  mode: 'light', scope: false,
  setup: ({ host, cleanup }) => {
    const events = new AbortController();
    let timer;
    const tip = () => [...host.querySelectorAll('[role=tooltip]')].find(tip => tip.closest('fve-tooltip') === host);
    const show = () => {
      window.clearTimeout(timer);
      timer = window.setTimeout(() => {
        const content = tip();
        if (host.isConnected && content?.isConnected && !content.matches(':popover-open')) content.showPopover();
      }, Number(host.getAttribute('delay') || 400));
    };
    const hide = () => {
      window.clearTimeout(timer);
      timer = window.setTimeout(() => {
        if (!host.matches(':hover') && !host.contains(document.activeElement)) tip()?.hidePopover();
      }, 150);
    };
    for (const name of ['mouseenter', 'focusin']) host.addEventListener(name, show, { signal: events.signal });
    for (const name of ['mouseleave', 'focusout']) host.addEventListener(name, hide, { signal: events.signal });
    window.addEventListener('keydown', event => {
      if (event.key !== 'Escape' || !tip()?.matches(':popover-open')) return;
      window.clearTimeout(timer); tip().hidePopover(); event.preventDefault(); event.stopPropagation();
    }, { capture: true, signal: events.signal });
    cleanup(() => { events.abort(); window.clearTimeout(timer); });
  },
});
