// Dialog and Drawer share native modality, opener/focus and contained-background lifecycle.
export function enhanceOverlay({ host, cleanup }) {
  const events = new AbortController();
  const options = { signal: events.signal };
  const dialog = () => host.querySelector('dialog');
  const background = () => host.querySelector('[data-fve-overlay-background]')?.dataset.fveOverlayBackground;
  let opener;
  const synchronize = () => {
    const id = background();
    if (!id) return;
    const target = host.ownerDocument.getElementById(id);
    if (target) target.inert = [...host.ownerDocument.querySelectorAll('[data-fve-overlay-background]')].some(layer => layer.dataset.fveOverlayBackground === id && layer.querySelector('dialog[open]'));
  };
  const restore = () => {
    const target = opener?.isConnected ? opener : host.ownerDocument.getElementById(dialog()?.id + '-trigger');
    target?.focus(); opener = undefined;
  };
  const cancel = () => {
    const current = dialog();
    if (current?.open && current.dispatchEvent(new Event('cancel', { cancelable: true }))) current.close();
  };
  host.ownerDocument.addEventListener('click', event => {
    const control = event.target.closest('[data-fve-overlay-target]');
    const current = dialog();
    if (!current || control?.dataset.fveOverlayTarget !== current.id || event.defaultPrevented || control.disabled || control.getAttribute('aria-disabled') === 'true') return;
    if (control.dataset.fveOverlayAction === 'close') { current.close(); return; }
    if (current.open) return;
    opener = control;
    current.hasAttribute('data-fve-contained') ? current.show() : current.showModal();
    const focus = control.dataset.fveOverlayFocus || current.dataset.fveInitialFocus;
    if (focus) queueMicrotask(() => { if (current.isConnected && current.open) current.ownerDocument.getElementById(focus)?.focus(); });
    synchronize();
  }, options);
  host.addEventListener('close', event => {
    if (event.target !== dialog()) return;
    synchronize(); restore();
  }, { ...options, capture: true });
  host.addEventListener('click', event => {
    const current = dialog();
    if (event.defaultPrevented || !current?.open || !host.hasAttribute('dismiss-on-backdrop') || event.target.closest('fve-dialog,fve-drawer') !== host) return;
    if (event.target === current || event.target.closest('[data-fve-overlay-backdrop]')) cancel();
  }, options);
  window.addEventListener('keydown', event => {
    const current = dialog();
    if (event.key === 'Escape' && !event.defaultPrevented && current?.open && current.hasAttribute('data-fve-contained') && !host.ownerDocument.querySelector(':popover-open, dialog:modal')) { event.preventDefault(); cancel(); }
  }, options);
  const observer = new MutationObserver(synchronize);
  observer.observe(host, { childList: true, subtree: true, attributes: true, attributeFilter: ['open'] });
  host.addEventListener('datastar-scope-children', () => queueMicrotask(synchronize), options);
  synchronize();
  cleanup(() => { events.abort(); observer.disconnect(); synchronize(); });
}
