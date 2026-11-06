// Native inputs own their values, checked state and form behavior. Rocket owns only the enhancement.
export function enhanceNativeControl({ host, cleanup }, keys = [], mixed = false) {
  const events = new AbortController();
  const options = { signal: events.signal, capture: true };
  const field = () => host.querySelector('input');
  let disposed = false;
  const synchronize = () => {
    const input = field();
    if (input && mixed) input.indeterminate = host.getAttribute('indeterminate') === 'true';
  };
  host.addEventListener('click', event => {
    if (event.target === field() && field().readOnly) event.preventDefault();
  }, options);
  host.addEventListener('keydown', event => {
    if (event.target === field() && field().readOnly && keys.includes(event.key)) event.preventDefault();
  }, options);
  host.addEventListener('datastar-scope-children', synchronize, { signal: events.signal });
  const observer = new MutationObserver(synchronize);
  observer.observe(host, { childList: true, attributes: true, attributeFilter: ['indeterminate'] });
  window.addEventListener('reset', event => {
    if (event.target === field()?.form) queueMicrotask(() => {
      if (!disposed && !event.defaultPrevented && host.isConnected) synchronize();
    });
  }, { signal: events.signal });
  synchronize();
  cleanup(() => { disposed = true; events.abort(); observer.disconnect(); });
}
