/* eslint-disable no-undef */

(function () {
    // tolerant numeric extractor (handles Page/page/PageNumber etc)
    function _num(obj, ...keys) {
        for (const k of keys) {
            if (!obj) continue;
            const v = obj[k];
            if (v !== undefined && v !== null) {
                const n = Number(v);
                if (!Number.isNaN(n)) return n;
            }
        }
        return undefined;
    }

    // render a Bootstrap-style pagination identical to the Razor view
    function renderPagination(rawPaged, tableSelector, onNavigate) {
        if (!rawPaged) return;
        const containerId = 'shipments-pagination';
        let container = document.getElementById(containerId);
        const table = document.querySelector(tableSelector);
        if (!table) return;

        if (!container) {
            container = document.createElement('div');
            container.id = containerId;
            container.className = 'mt-3';
            table.parentElement.appendChild(container);
        }

        const page = _num(rawPaged, 'Page', 'page') || 1;
        const pageSize = _num(rawPaged, 'PageSize', 'pageSize') || 10;
        const total = _num(rawPaged, 'TotalCount', 'totalCount', 'Total') || 0;
        const totalPages = _num(rawPaged, 'TotalPages', 'totalPages') || Math.max(1, Math.ceil(total / pageSize));

        container.innerHTML = '';

        const ul = document.createElement('ul');
        ul.className = 'pagination justify-content-center';

        function createPageItem(text, targetPage, disabled, active) {
            const li = document.createElement('li');
            li.className = 'page-item' + (disabled ? ' disabled' : '') + (active ? ' active' : '');

            if (active) {
                const span = document.createElement('span');
                span.className = 'page-link';
                span.setAttribute('aria-current', 'page');
                span.textContent = text;
                li.appendChild(span);
            } else {
                const a = document.createElement('a');
                a.className = 'page-link';
                a.href = '#';
                a.textContent = text;
                a.addEventListener('click', function (e) {
                    e.preventDefault();
                    if (disabled) return;
                    if (typeof onNavigate === 'function') onNavigate(targetPage);
                });
                li.appendChild(a);
            }

            return li;
        }

        // Prev
        ul.appendChild(createPageItem('« Previous', Math.max(1, page - 1), page <= 1, false));

        // numeric window (center current page)
        const windowSize = 7;
        const half = Math.floor(windowSize / 2);
        let start = Math.max(1, page - half);
        const end = Math.min(totalPages, start + windowSize - 1);
        if (end - start + 1 < windowSize) start = Math.max(1, end - windowSize + 1);

        for (let p = start; p <= end; p++) {
            ul.appendChild(createPageItem(String(p), p, false, p === page));
        }

        // Next
        ul.appendChild(createPageItem('Next »', Math.min(totalPages, page + 1), page >= totalPages, false));

        container.appendChild(ul);

        const info = document.createElement('div');
        info.className = 'text-center mt-2';
        const shown = rawPaged.Items ? (Array.isArray(rawPaged.Items) ? rawPaged.Items.length : 0) : 0;
        info.innerHTML = `<small class="text-muted">Page ${page} of ${totalPages} — Showing ${shown} of ${total} items</small>`;
        container.appendChild(info);
    }

    // Bind th[data-sort] headers to sort events
    function bindHeaderSorts(tableSelector, currentSort = { sortBy: '', sortDir: 'desc' }, onSort) {
        const table = document.querySelector(tableSelector);
        if (!table) return;
        const headers = table.querySelectorAll('th[data-sort]');
        headers.forEach(h => {
            h.style.cursor = 'pointer';
            const key = h.getAttribute('data-sort');

            if (currentSort.sortBy === key) {
                h.dataset.sortDir = currentSort.sortDir || 'desc';
                h.classList.add('sorted');
            } else {
                h.dataset.sortDir = '';
                h.classList.remove('sorted');
            }

            h.removeEventListener('click', h._sortHandler);
            h._sortHandler = (e) => {
                let dir = (h.dataset.sortDir === 'asc') ? 'desc' : 'asc';
                if (currentSort.sortBy !== key) dir = 'desc'; // default new column to desc
                if (typeof onSort === 'function') onSort(key, dir);
            };
            h.addEventListener('click', h._sortHandler);
        });
    }

    window.ShipmentsPager = {
        renderPagination,
        bindHeaderSorts
    };
})();


