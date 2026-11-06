// Product documents own their UI and navigation; the Spec owns review chrome.
const fixtureDocument = new URL(location.href).searchParams.get('fveFixture') === 'product';
const identity = href => {
  const url = new URL(href, location.href);
  url.hash = '';
  for (const key of ['appMode', 'fveAppReturn', 'fveFixture', 'datastar']) url.searchParams.delete(key);
  if (['', 'default'].includes(url.searchParams.get('specState'))) url.searchParams.delete('specState');
  url.searchParams.sort();
  return url.href;
};

if (fixtureDocument && parent !== window) {
  const report = () => {
    const product = document.getElementById('spec-product-document');
    if (!product) return;
    const href = product.dataset.fixtureHref;
    // Validation drafts stay in this document; only the public resulting state is mirrored.
    const url = new URL(href, location.href);
    url.searchParams.set('fveFixture', 'product');
    history.replaceState(history.state, '', url.href);
    parent.postMessage({ type: 'fve-fixture-location', href }, location.origin);
  };
  document.addEventListener('click', event => {
    const link = event.target.closest?.('a[href]');
    if (!link || (link.target && link.target !== '_self')) return;
    const target = new URL(link.href, location.href);
    if (target.origin !== location.origin || !target.pathname.startsWith('/examples/specification/')) link.target = '_parent';
  }, true);
  window.addEventListener('fve-navigation-complete', report);
  window.addEventListener('keydown', event => {
    if (event.key === 'Escape' && !event.defaultPrevented && !document.querySelector('dialog[open], :popover-open')) {
      parent.postMessage({ type: 'fve-fixture-restore' }, location.origin);
    }
  });
  window.fveReportFixture = report;
  report();
} else {
  const sources = new WeakMap();
  let childCommit = null;
  let hostIntent = null;
  let expanded;
  let dockScroll;
  let productScroll = [];
  const frame = () => document.getElementById('spec-product-frame');
  const scrollRoot = () => document.querySelector('[data-fve-page-scroll]');
  const review = () => document.querySelector('[data-review-fixture]');

  const initialize = () => {
    const product = frame();
    const fixture = review();
    if (!product || !fixture) { expanded = undefined; return; }
    const nowExpanded = fixture.dataset.reviewExpanded === 'true';
    if (expanded !== undefined && nowExpanded !== expanded) {
      requestAnimationFrame(() => {
        for (const [element, left, top] of productScroll) if (element.isConnected) element.scrollTo(left, top);
        const control = fixture.querySelector('[data-review-expand]');
        control?.focus({ preventScroll: true });
        if (!nowExpanded && dockScroll) scrollRoot()?.scrollTo(...dockScroll);
      });
    }
    expanded = nowExpanded;
    const expected = new URL(product.dataset.fixtureSource, location.href).href;
    const previous = sources.get(product);
    sources.set(product, expected);
    // A review-only refresh must not replay the child's form, reset its draft or reload its document.
    if (childCommit === location.href) { childCommit = null; return; }
    if (!previous || identity(previous) === identity(expected)) { hostIntent = null; return; }
    try {
      if (identity(product.contentWindow.location.href) !== identity(expected)) product.contentWindow.location.replace(expected);
      else hostIntent = null;
    } catch { /* Only the same-origin product document can receive review state. */ }
  };

  document.addEventListener('click', event => {
    const link = event.target.closest?.('a[href]');
    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || !link || (link.target && link.target !== '_self') || link.hasAttribute('download')) return;
    const target = new URL(link.href, location.href);
    if (target.origin !== location.origin || target.hash) return;
    childCommit = null;
    hostIntent = identity(target.href);
    if (link.hasAttribute('data-review-expand')) {
      const page = scrollRoot();
      if (link.dataset.reviewExpand === 'true') dockScroll = [page?.scrollLeft ?? 0, page?.scrollTop ?? 0];
      try {
        productScroll = [...frame().contentDocument.querySelectorAll('[data-fve-page-scroll], .fve-table-scroll')].map(node => [node, node.scrollLeft, node.scrollTop]);
      } catch { productScroll = []; }
    }
  }, true);

  window.addEventListener('message', event => {
    const product = frame();
    if (event.origin !== location.origin || !product || event.source !== product.contentWindow) return;
    if (event.data?.type === 'fve-fixture-restore') {
      if (review()?.dataset.reviewExpanded === 'true') review().querySelector('[data-review-expand]')?.click();
      return;
    }
    if (event.data?.type !== 'fve-fixture-location' || typeof event.data.href !== 'string') return;
    let url;
    try { url = new URL(event.data.href, location.href); } catch { return; }
    if (url.origin !== location.origin || !url.pathname.startsWith('/examples/specification/') || url.searchParams.has('fveFixture')) return;
    if (hostIntent && identity(url.href) !== hostIntent) return;
    hostIntent = null;
    if (identity(url.href) === identity(location.href)) return;
    if (review()?.dataset.reviewExpanded === 'true') url.searchParams.set('appMode', '1');
    // Child navigation already created a joint-session entry. Never push a duplicate parent entry.
    childCommit = url.href;
    const page = scrollRoot();
    history.replaceState(Object.assign({}, history.state, { fveDocsScroll: [page?.scrollLeft ?? 0, page?.scrollTop ?? 0] }), '', url.href);
    window.dispatchEvent(new CustomEvent('fve-fixture-location', { detail: { href: url.pathname + url.search } }));
  });
  window.addEventListener('fve-navigation-complete', initialize);
  window.addEventListener('keydown', event => {
    if (event.key !== 'Escape' || event.defaultPrevented || document.querySelector('dialog[open], :popover-open') || review()?.dataset.reviewExpanded !== 'true') return;
    const child = frame()?.contentDocument;
    if (!child?.querySelector('dialog[open], :popover-open')) review().querySelector('[data-review-expand]')?.click();
  });
  window.addEventListener('popstate', () => { childCommit = null; hostIntent = identity(location.href); });
  initialize();
  frame()?.addEventListener('load', event => {
    try { event.currentTarget.contentWindow.fveReportFixture?.(); }
    catch { /* External documents cannot participate in the same-origin review bridge. */ }
  });
}
