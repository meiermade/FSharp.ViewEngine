import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-avatar', {
  mode: 'light', scope: false,
  setup: ({ host, cleanup }) => {
    // Cached images may finish before enhancement. Native fallback content is always present.
    const synchronize = () => {
      const image = host.querySelector(':scope > img[data-fve-avatar-image]');
      const fallback = host.querySelector(':scope > span');
      const hidden = !image || !image.complete || image.naturalWidth === 0;
      if (image && image.hidden !== hidden) image.hidden = hidden;
      // Transparent pictures must not reveal initials underneath their successful image.
      if (fallback && fallback.hidden === hidden) fallback.hidden = !hidden;
    };
    const events = new AbortController();
    for (const name of ['load', 'error']) host.addEventListener(name, synchronize, { capture: true, signal: events.signal });
    const observer = new MutationObserver(synchronize);
    observer.observe(host, { childList: true, subtree: true, attributes: true, attributeFilter: ['src', 'srcset', 'hidden'] });
    synchronize();
    cleanup(() => { events.abort(); observer.disconnect(); });
  },
});
