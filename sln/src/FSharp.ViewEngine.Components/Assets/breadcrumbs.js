import { rocket } from './datastar-rocket.1.0.4.js';

// Server-owned destinations, client-owned fit. Never clone the navigation model or observe the document.
rocket('fve-breadcrumbs', {
    mode: 'light', scope: false,
    setup: ({ host, cleanup }) => {
        let frame = 0, disposed = false;
        let observed = [];
        const schedule = () => { if (!disposed && !frame) frame = requestAnimationFrame(layout); };
        const resize = new ResizeObserver(schedule);
        const mutation = new MutationObserver(schedule);
        function layout() {
            frame = 0;
            if (disposed || !host.isConnected) return;
            const nav = host.querySelector(':scope > nav');
            const list = nav?.querySelector(':scope > ol');
            if (!list || !list.clientWidth) return;
            const items = [...list.children].filter(item => item.hasAttribute('data-fve-breadcrumb-item'));
            if (!items.length) return;
            const more = list.querySelector('[data-fve-breadcrumb-overflow]');
            const menu = more?.querySelector('[role=menu]');
            const trigger = more?.querySelector('button');
            const menuItems = [...(menu?.querySelectorAll('[role=menuitem]') ?? [])];
            const active = document.activeElement;
            const focusedMenu = menuItems.indexOf(active);
            const focusedItem = items.findIndex(item => item.contains(active));
            const separator = more?.querySelector('[data-fve-breadcrumb-overflow-separator]');
            // Prepare once, read all intrinsic widths together, then commit the computed set.
            for (const item of items) { item.style.display = 'flex'; item.style.flex = 'none'; }
            if (more) more.style.display = 'none';
            if (separator) separator.style.display = '';
            const widths = items.map(item => item.getBoundingClientRect().width);
            const gap = parseFloat(getComputedStyle(list).columnGap) || 0;
            const available = list.getBoundingClientRect().width;
            const hidden = new Set();
            let overflowWidth = 0;
            const required = () => widths.reduce((sum, width, index) => sum + (hidden.has(index) ? 0 : width), 0)
                + (hidden.size ? overflowWidth : 0) + gap * Math.max(0, items.length - hidden.size + (hidden.size ? 1 : 0) - 1);
            if (more && required() > available + 0.5) {
                more.style.display = 'flex';
                overflowWidth = more.getBoundingClientRect().width;
                for (let index = 1; index < items.length - 1 && required() > available + 0.5; index++) hidden.add(index);
                if (required() > available + 0.5 && items.length > 1) {
                    hidden.add(0);
                    separator.style.display = 'none';
                }
            }
            items.forEach((item, index) => { item.style.display = hidden.has(index) ? 'none' : 'flex'; });
            items.at(-1).style.flex = '1 1 0%';
            menuItems.forEach((item, index) => { item.style.display = hidden.has(index) ? '' : 'none'; });
            if (more) more.style.display = hidden.size ? 'flex' : 'none';
            list.style.overflowX = 'hidden';
            const restored = focusedMenu >= 0 && !hidden.has(focusedMenu);
            if (menu?.matches(':popover-open') && (!hidden.size || restored)) {
                menu.hidePopover();
                if (restored) items[focusedMenu].querySelector('a, [tabindex]')?.focus({ preventScroll: true });
                else if (active === trigger) items.at(-1).querySelector('[aria-current]')?.focus({ preventScroll: true });
            } else if (focusedItem >= 0 && hidden.has(focusedItem)) trigger?.focus({ preventScroll: true });
            else if (active === trigger && !hidden.size) items.at(-1).querySelector('[aria-current]')?.focus({ preventScroll: true });
            const targets = [host, nav, ...items.map(item => item.lastElementChild)];
            if (targets.length !== observed.length || targets.some((item, index) => item !== observed[index])) {
                resize.disconnect();
                for (const target of targets) resize.observe(target);
                observed = targets;
            }
        }
        resize.observe(host);
        mutation.observe(host, { childList: true, characterData: true, subtree: true });
        document.fonts?.ready.then(schedule);
        document.fonts?.addEventListener('loadingdone', schedule);
        schedule();
        cleanup(() => {
            disposed = true;
            cancelAnimationFrame(frame);
            resize.disconnect(); mutation.disconnect();
            document.fonts?.removeEventListener('loadingdone', schedule);
        });
    },
});
