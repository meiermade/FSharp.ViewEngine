import { rocket } from './datastar-rocket.1.0.4.js';

// Native fields remain authoritative across morphs and reconnects; Rocket owns the lifecycle.
rocket('fve-tag-input', {
    mode: 'light', scope: false,
    setup: ({ host, cleanup }) => {
        let values = [], initial = [], pending = false, disposed = false;
        const events = new AbortController();
        const options = { signal: events.signal };
        const entry = () => host.querySelector('[data-fve-tag-entry]');
        const unavailable = () => host.hasAttribute('disabled') || host.hasAttribute('readonly');
        const readValues = () => {
            const parsed = JSON.parse(host.getAttribute('values') || '[]');
            if (!Array.isArray(parsed) || parsed.some(value => typeof value !== 'string' || !value.trim()) || new Set(parsed).size !== parsed.length)
                throw new TypeError('TagInput values must be unique non-empty strings.');
            initial = parsed;
            values = [...parsed];
        };
        const status = message => {
            const output = host.querySelector('[data-fve-tag-status]');
            if (output && output.textContent !== message) output.textContent = message;
        };
        const observe = () => observer.observe(host, { childList: true, subtree: true, attributes: true,
            attributeFilter: ['data-fve-tag-value', 'values', 'name', 'disabled', 'readonly'] });
        const schedule = () => {
            if (pending || disposed) return;
            pending = true;
            queueMicrotask(() => { pending = false; if (!disposed && host.isConnected) sync(); });
        };
        const observer = new MutationObserver(records => {
            if (records.some(record => record.target === host && record.attributeName === 'values')) readValues();
            else {
                const list = host.querySelector('[data-fve-tag-list]');
                // A server-rendered list wins even if its values attribute has not changed.
                if (list && records.some(record => record.type === 'childList' && (record.target === host || record.target === list || list.contains(record.target)) || record.attributeName === 'data-fve-tag-value'))
                    values = [...list.children].map(node => node.dataset.fveTagValue);
            }
            schedule();
        });
        function sync() {
            const list = host.querySelector('[data-fve-tag-list]');
            const input = entry();
            if (!list || !input) return;
            observer.disconnect();
            const disabled = host.hasAttribute('disabled');
            input.disabled = disabled;
            input.readOnly = host.hasAttribute('readonly');
            const add = host.querySelector('[data-fve-tag-add]');
            if (add) add.disabled = unavailable();
            const existing = new Map([...list.children].map(node => [node.dataset.fveTagValue, node]));
            values.forEach((value, index) => {
                let node = existing.get(value);
                if (!node) {
                    node = document.createElement('li');
                    node.dataset.fveTagValue = value;
                    node.className = 'inline-flex min-h-8 max-w-full items-center gap-1 rounded-full bg-[var(--fve-neutral-subtle)] py-1 pr-1 pl-3 text-sm leading-5 text-[var(--fve-text)]';
                    const label = document.createElement('span');
                    label.dataset.fveTagLabel = '';
                    label.className = 'min-w-0 [overflow-wrap:anywhere]';
                    label.textContent = value;
                    const field = document.createElement('input');
                    field.type = 'hidden';
                    const remove = document.createElement('button');
                    remove.type = 'button';
                    remove.dataset.fveTagRemove = '';
                    remove.textContent = '×';
                    remove.className = 'inline-grid size-6 shrink-0 cursor-pointer place-items-center rounded-full border-0 bg-transparent p-0 text-base leading-none text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-50';
                    node.append(label, field, remove);
                }
                const field = node.querySelector('input');
                field.name = host.getAttribute('name');
                field.value = value;
                field.disabled = disabled;
                const remove = node.querySelector('[data-fve-tag-remove]');
                remove.setAttribute('aria-label', `Remove ${value}`);
                remove.disabled = unavailable();
                if (list.children[index] !== node) list.insertBefore(node, list.children[index] || null);
                existing.delete(value);
            });
            for (const node of existing.values()) node.remove();
            observe();
        }
        const changed = message => {
            sync(); status(message);
            entry()?.focus();
            entry()?.dispatchEvent(new Event('input', { bubbles: true }));
            entry()?.dispatchEvent(new Event('change', { bubbles: true }));
        };
        const add = text => {
            const candidates = text.split(/[,\n]+/).map(value => value.trim()).filter(Boolean);
            const additions = [...new Set(candidates)].filter(value => !values.includes(value));
            if (!additions.length) {
                entry().setAttribute('aria-invalid', 'true');
                status(candidates.length ? 'That tag has already been added.' : 'Enter a tag before adding it.');
                return;
            }
            values.push(...additions);
            entry().value = '';
            entry().setAttribute('aria-invalid', 'false');
            changed(`${additions.join(', ')} added.`);
        };
        host.addEventListener('click', event => {
            if (unavailable()) return;
            const remove = event.target.closest('[data-fve-tag-remove]');
            if (remove) {
                const value = remove.closest('[data-fve-tag-value]').dataset.fveTagValue;
                values = values.filter(item => item !== value);
                changed(`${value} removed.`);
            } else if (event.target.closest('[data-fve-tag-add]')) add(entry().value);
        }, options);
        host.addEventListener('keydown', event => {
            if (event.target === entry() && !event.isComposing && !unavailable() && ['Enter', ','].includes(event.key)) {
                event.preventDefault(); add(entry().value);
            }
        }, options);
        host.addEventListener('paste', event => {
            if (event.target !== entry() || unavailable()) return;
            const text = event.clipboardData?.getData('text') || '';
            if (/[,\n]/.test(text)) { event.preventDefault(); add(text); }
        }, options);
        host.closest('form')?.addEventListener('reset', event => queueMicrotask(() => {
            if (event.defaultPrevented || disposed || !host.isConnected) return;
            values = [...initial]; sync(); status('');
        }), options);
        readValues();
        const list = host.querySelector('[data-fve-tag-list]');
        if (list) values = [...list.children].map(node => node.dataset.fveTagValue);
        observe();
        sync();
        cleanup(() => { disposed = true; events.abort(); observer.disconnect(); });
    },
});
