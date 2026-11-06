import { rocket } from './datastar-rocket.1.0.4.js';

// Rocket owns lifecycle; native rows and caller-owned state remain intact.
// Rows/cells remain native HTML and new rows inherit these descendant styles.
const layout = [
  '@container/fve-table block w-full min-w-0 max-w-full [--fve-table-background:var(--fve-background)] data-[surface=panel]:[--fve-table-background:var(--fve-surface)] data-[scroll-rows=true]:flex data-[scroll-rows=true]:min-h-0 data-[scroll-rows=true]:flex-1 data-[scroll-rows=true]:flex-col',
  '[&_.fve-table-grid]:w-full [&_.fve-table-grid]:border-collapse [&_.fve-table-grid_thead]:bg-[var(--fve-table-background)] [&_.fve-table-grid_tr]:border-b [&_.fve-table-grid_tr]:border-[var(--fve-border)]',
  '[&_.fve-table-cell]:whitespace-nowrap [&_.fve-table-cell]:px-[var(--fve-table-padding-inline,0.75rem)] [&_.fve-table-cell]:py-[var(--fve-table-padding-block-compact,0.25rem)] data-[density=comfortable]:[&_.fve-table-cell]:py-[var(--fve-table-padding-block-comfortable,0.75rem)]',
  '[&_.fve-table-row]:[--fve-row-background:var(--fve-table-background)] [&_.fve-table-row]:bg-[var(--fve-row-background)] [&_.fve-table-row:hover]:[--fve-row-background:var(--fve-surface-hover)] [&_.fve-table-row[data-selected=true]]:[--fve-row-background:var(--fve-brand-subtle)]',
  '[&_.fve-table-sort-control]:inline-flex [&_.fve-table-sort-control]:items-center [&_.fve-table-sort-control]:gap-1 [&_.fve-table-sort-control]:rounded-[var(--fve-radius-control)] [&_.fve-table-sort-control]:text-inherit [&_.fve-table-sort-control]:no-underline [&_.fve-table-sort-control:hover]:underline [&_.fve-table-sort-control:focus-visible]:outline-2 [&_.fve-table-sort-control:focus-visible]:outline-offset-2 [&_.fve-table-sort-control:focus-visible]:outline-[var(--fve-brand-ring)]',
  '[&_[data-fve-sticky-cell=true]]:sticky [&_[data-fve-sticky-cell=true]]:right-0 [&_[data-fve-sticky-cell=true]]:z-10 [&_[data-fve-sticky-cell=true]]:bg-[var(--fve-row-background,var(--fve-table-background))]',
  '[&_.fve-table-checkbox]:size-4 [&_.fve-table-checkbox]:accent-[var(--fve-brand-solid)] [&_.fve-table-checkbox]:align-middle [&_.fve-table-checkbox]:focus-visible:outline-2 [&_.fve-table-checkbox]:focus-visible:outline-offset-2 [&_.fve-table-checkbox]:focus-visible:outline-[var(--fve-brand-ring)]',
  '@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-scroll]:overflow-x-visible',
  '@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-sort]:flex @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-sort]:flex-wrap @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-sort]:gap-4 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-sort]:p-3',
  '@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-grid]:block @max-[40rem]/fve-table:[&.fve-table-records_tbody]:block @max-[40rem]/fve-table:[&.fve-table-records_thead]:block @max-[40rem]/fve-table:[&.fve-table-records_thead_tr]:block',
  '@max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:absolute @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:size-px @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:overflow-hidden @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:p-0 @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:[clip-path:inset(50%)]',
  '@max-[40rem]/fve-table:[&.fve-table-records_thead_.fve-table-selection]:flex @max-[40rem]/fve-table:[&.fve-table-records_thead_.fve-table-selection]:w-auto @max-[40rem]/fve-table:[&.fve-table-records_thead_.fve-table-selection]:items-center @max-[40rem]/fve-table:[&.fve-table-records_thead_.fve-table-selection]:gap-2 @max-[40rem]/fve-table:[&.fve-table-records_thead_.fve-table-selection]:px-3',
  '@max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-row]:grid @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-row]:grid-cols-[auto_minmax(0,1fr)_auto] @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-row]:gap-x-2 @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-row]:gap-y-1 @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-row]:p-3',
  '@max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-cell]:min-w-0 @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-cell]:p-0 @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-cell]:text-left @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-cell]:whitespace-normal @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-cell]:[overflow-wrap:anywhere]',
  '@max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-selection]:col-start-1 @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-selection]:row-start-1 @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-selection]:col-span-1 @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-selection]:w-auto @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-selection]:self-center @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-selection]:block',
  '@max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=primary]]:col-start-2 @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=primary]]:row-start-1 @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=primary]]:self-center @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=summary]]:col-start-2 @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=summary]]:row-start-2',
  '@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row:not(:has(.fve-table-selection))_[data-mobile-cell=primary]]:col-span-2 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row:not(:has(.fve-table-selection))_[data-mobile-cell=primary]]:col-start-1 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row:not(:has(.fve-table-selection))_[data-mobile-cell=summary]]:col-span-2 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row:not(:has(.fve-table-selection))_[data-mobile-cell=summary]]:col-start-1',
  '@max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=actions]]:col-start-3 @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=actions]]:row-start-1 @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=actions]]:static',
  '@max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:col-span-full @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:grid @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:grid-cols-[minmax(6rem,35%)_minmax(0,1fr)] @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:gap-2',
  '@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-label]:inline @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-label]:font-normal @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-label]:text-[var(--fve-muted-text)]',
  '@max-[16rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:grid-cols-1',
].join(' ');
const layoutClasses = layout.split(' ');
const setAttributeIfChanged = (element, name, value) => {
  if (element.getAttribute(name) === value) return;
  if (value === null) element.removeAttribute(name);
  else element.setAttribute(name, value);
};

rocket('fve-table', {
  mode: 'light', scope: false,
  setup: ({ host, cleanup }) => {
    let syncing = false, pending = false, disposed = false, focusId;
    const attributes = ['density', 'surface', 'mobile-layout', 'scroll-rows', 'aria-label', 'aria-labelledby', 'aria-describedby', 'class'];
    const observer = new MutationObserver(records => {
      if (records.some(record => record.type === 'childList' || record.target === host)) schedule();
    });
    const observe = () => observer.observe(host, { childList: true, subtree: true, attributes: true, attributeFilter: attributes });
    const schedule = () => {
      if (disposed || syncing || pending || !host.isConnected) return;
      pending = true;
      queueMicrotask(() => { pending = false; sync(); });
    };
    const events = new AbortController();
    const options = { signal: events.signal };
    host.addEventListener('focusin', event => { focusId = event.target.id || undefined; }, options);
    host.addEventListener('focusout', event => {
      if (event.relatedTarget) focusId = undefined;
      else {
        const previous = event.target;
        queueMicrotask(() => { if (host.contains(previous) && !previous.disabled && document.activeElement !== previous) focusId = undefined; });
      }
    }, options);
    const outside = event => { if (!host.contains(event.target)) focusId = undefined; };
    document.addEventListener('focusin', outside, { ...options, capture: true });
    document.addEventListener('pointerdown', outside, { ...options, capture: true });
    window.addEventListener('blur', () => { focusId = undefined; }, options);
    function sync() {
    if (disposed || syncing || !host.isConnected) return;
    syncing = true;
    observer.disconnect();
    try {
      if (layoutClasses.some(name => !host.classList.contains(name))) host.classList.add(...layoutClasses);
      setAttributeIfChanged(host, 'data-density', host.getAttribute('density') ?? 'compact');
      setAttributeIfChanged(host, 'data-surface', host.getAttribute('surface') ?? 'plain');
      host.classList.toggle('fve-table-records', host.getAttribute('mobile-layout') === 'records');
      const bounded = host.getAttribute('scroll-rows') === 'true';
      setAttributeIfChanged(host, 'data-scroll-rows', String(bounded));
      let scroll = host.querySelector(':scope > .fve-table-scroll');
      if (!scroll) {
        scroll = document.createElement('div');
        scroll.className = 'fve-table-scroll relative bg-[var(--fve-table-background)]';
        scroll.setAttribute('role', 'region');
        scroll.tabIndex = 0;
        host.append(scroll);
      }
      setAttributeIfChanged(scroll, 'aria-label', host.getAttribute('aria-label') ?? 'Table');
      for (const name of ['min-h-0', 'flex-1', 'overflow-auto']) scroll.classList.toggle(name, bounded);
      scroll.classList.toggle('overflow-x-auto', !bounded);
      for (const name of ['rounded-[var(--fve-radius-panel)]', 'ring-1', 'ring-[var(--fve-border)]']) scroll.classList.toggle(name, host.dataset.surface === 'panel');
      let table = scroll.querySelector(':scope > table');
      if (!table) {
        table = document.createElement('table');
        table.className = 'fve-table-grid min-w-full border-collapse text-left text-sm text-[var(--fve-text)]';
        table.setAttribute('role', 'table');
        scroll.append(table);
      }
      for (const name of ['aria-label', 'aria-labelledby', 'aria-describedby']) {
        setAttributeIfChanged(table, name, host.getAttribute(name));
      }
      let headingsChanged = false;
      for (const source of host.querySelectorAll(':scope > template[data-fve-table-part]')) {
        const part = source.getAttribute('data-fve-table-part');
        const tag = { head: 'thead', body: 'tbody', foot: 'tfoot', caption: 'caption', empty: 'div' }[part];
        if (!tag) continue;
        const section = document.createElement(tag);
        for (const { name, value } of source.attributes) if (name !== 'data-fve-table-part') section.setAttribute(name, value);
        section.append(source.content);
        if (part === 'empty') {
          section.dataset.fveTableEmpty = '';
          section.classList.add('p-6', 'text-center', 'text-sm', 'text-[var(--fve-muted-text)]');
          const old = scroll.querySelector(':scope > [data-fve-table-empty]');
          if (old) old.replaceWith(section); else scroll.append(section);
        } else {
          if (part === 'head') headingsChanged = true;
          if (['head', 'body', 'foot'].includes(part)) section.setAttribute('role', 'rowgroup');
          const old = table.querySelector(`:scope > ${tag}`);
          if (old) old.replaceWith(section); else table.append(section);
        }
        source.remove();
      }
      if (!table.caption) {
        const caption = table.createCaption();
        caption.className = 'sr-only';
        caption.textContent = host.getAttribute('aria-label') ?? '';
        caption.dataset.fveGenerated = '';
      } else if (table.caption.hasAttribute('data-fve-generated') && table.caption.textContent !== (host.getAttribute('aria-label') ?? '')) {
        table.caption.textContent = host.getAttribute('aria-label') ?? '';
      }
      const head = table.tHead;
      if (head) {
        head.classList.add('text-xs', 'font-semibold', 'text-[var(--fve-muted-text)]');
        for (const name of ['sticky', 'top-0', 'z-20']) head.classList.toggle(name, bounded);
      }
      for (const row of table.querySelectorAll('tr')) {
        setAttributeIfChanged(row, 'role', 'row');
        for (const cell of row.cells) {
          setAttributeIfChanged(cell, 'role', cell.tagName === 'TH' ? (cell.scope === 'row' ? 'rowheader' : 'columnheader') : 'cell');
          if (row.parentElement.tagName === 'TBODY' && !cell.hasAttribute('data-mobile-cell') && !cell.classList.contains('fve-table-selection')) cell.dataset.mobileCell = 'field';
        }
      }
      let mobileSort = scroll.querySelector(':scope > .fve-table-mobile-sort');
      if (!mobileSort || headingsChanged) {
        mobileSort?.remove();
        const links = [...(head?.querySelectorAll('a[href]') ?? [])];
        if (links.length) {
          mobileSort = document.createElement('div');
          mobileSort.className = 'fve-table-mobile-sort hidden';
          mobileSort.setAttribute('role', 'group');
          mobileSort.setAttribute('aria-label', `Sort ${host.getAttribute('aria-label')}`);
          mobileSort.append(...links.map(link => { const clone = link.cloneNode(true); clone.removeAttribute('id'); return clone; }));
          scroll.prepend(mobileSort);
        }
      }
      const empty = !table.tBodies.length || [...table.tBodies].every(body => body.rows.length === 0);
      table.hidden = empty;
      const emptyState = scroll.querySelector(':scope > [data-fve-table-empty]');
      if (emptyState) emptyState.hidden = !empty;
      if (focusId && document.activeElement === document.body && document.hasFocus()) {
        const target = document.getElementById(focusId);
        if (target && host.contains(target)) target.focus({ preventScroll: true });
      }
    } finally {
      syncing = false;
      observe();
    }
  }
    sync();
    cleanup(() => { disposed = true; observer.disconnect(); events.abort(); });
  },
});
