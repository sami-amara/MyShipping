/* eslint-disable no-undef */




// Simple UI caller that uses ShipmentService + ShipmentsPager
(function () {
    const tableSelector = '#shipments-table';
    const tbodySelector = '#shipments-table-body';

    let current = 1;
    let pageSize = 10;
    let sortBy = 'CreatedDate';
    let sortDir = 'desc';

    function _num(paged, ...keys) {
        for (const k of keys) {
            const v = paged?.[k];
            if (v !== undefined && v !== null) {
                const n = Number(v);
                if (!Number.isNaN(n)) return n;
            }
        }
        return undefined;
    }

    function loadPage(page = 1) {
        current = page;
        const tbody = document.querySelector(tbodySelector);
        if (tbody) tbody.innerHTML = '<tr><td colspan="8" class="text-center">Loading...</td></tr>';

        if (window.ShipmentApiClient && typeof ShipmentApiClient.getPaged === 'function') {
            ShipmentApiClient.getPaged(page, pageSize, sortBy, sortDir)
                .then(paged => {
                    if (!paged) {
                        if (tbody) tbody.innerHTML = '<tr><td colspan="8" class="text-center">No data returned</td></tr>';
                        if (window.ShipmentsPager) window.ShipmentsPager.renderPagination({ Page: page, PageSize: pageSize, TotalPages: 1, TotalCount: 0 }, tableSelector, loadPage);
                        return;
                    }

                    const items = paged.Items || paged.items || paged.data || paged.Data || [];
                    const itemsArr = Array.isArray(items) ? items : (items ? [items] : []);

                    const normalizedPage = _num(paged, 'Page', 'page') ?? page;
                    const normalizedPageSize = _num(paged, 'PageSize', 'pageSize') ?? pageSize;
                    const baseIndex = Math.max(0, (normalizedPage - 1) * normalizedPageSize);

                    // render table using ShipmentService (keeps parity with Razor)
                    if (window.ShipmentService && typeof ShipmentService.renderTable === 'function') {
                        ShipmentService.renderTable(tbodySelector, itemsArr, tableSelector, baseIndex);
                    }

                    // render pager using ShipmentsPager
                    if (window.ShipmentsPager && typeof window.ShipmentsPager.renderPagination === 'function') {
                        window.ShipmentsPager.renderPagination(paged, tableSelector, loadPage);
                        if (typeof window.ShipmentsPager.bindHeaderSorts === 'function') {
                            window.ShipmentsPager.bindHeaderSorts(tableSelector, { sortBy: sortBy, sortDir: sortDir }, (key, dir) => {
                                sortBy = key;
                                sortDir = dir;
                                loadPage(1);
                            });
                        }
                    } else if (window.ShipmentService && typeof ShipmentService._renderPager === 'function') {
                        ShipmentService._renderPager(paged, tableSelector);
                    }
                })
                .catch(err => {
                    console.error('Failed to load paged shipments', err);
                    if (tbody) tbody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Error loading shipments</td></tr>';
                    if (window.ShipmentsPager) window.ShipmentsPager.renderPagination({ Page: 1, PageSize: pageSize, TotalPages: 1, TotalCount: 0 }, tableSelector, loadPage);
                });
        } else if (window.ShipmentService && typeof ShipmentService.initList === 'function') {
            ShipmentService.initList({ page: page, pageSize: pageSize, tableSelector: tableSelector, tableBodySelector: tbodySelector });
        } else {
            if (tbody) tbody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Client not available</td></tr>';
        }
    }

    
    window.ShipmentClientUI = {
        loadPage: loadPage,
        setPageSize: (ps) => { pageSize = ps; loadPage(1); },
        setSort: (sBy, sDir) => { sortBy = sBy; sortDir = sDir; loadPage(1); },
        getCurrentPage: () => current
    };


    document.addEventListener('DOMContentLoaded', function () {
        const pageSizeEl = document.getElementById('pageSizeSelect');
        if (pageSizeEl) {
            pageSizeEl.value = String(pageSize);
            pageSizeEl.addEventListener('change', () => {
                const ps = parseInt(pageSizeEl.value, 10) || 10;
                window.ShipmentClientUI.setPageSize(ps);
            });
        }
        // initial load is triggered by Index.cshtml script that clicks the API button if present.
        // safe fallback: if that button isn't present, start a load here
        const btnApi = document.getElementById('btnLoadApi');
        if (!btnApi) loadPage(1);
    });
})();





























//////// Simple UI caller for /api/Shipments/paged using existing ShipmentApiClient + ShipmentService.renderTable
//////(function () {
//////    const tableSelector = '#shipments-table';
//////    const tbodySelector = '#shipments-table-body';
//////    const pagerContainerId = 'shipments-pagination';

//////    let current = 1;
//////    let pageSize = 10;
//////    let sortBy = 'CreatedDate';
//////    let sortDir = 'desc';

//////    function renderPager(paged) {
//////        let container = document.getElementById(pagerContainerId);
//////        if (!container) {
//////            container = document.createElement('div');
//////            container.id = pagerContainerId;
//////            container.className = 'mt-3 d-flex justify-content-center';
//////            const table = document.querySelector(tableSelector);
//////            if (table && table.parentElement) table.parentElement.appendChild(container);
//////        }
//////        container.innerHTML = '';

//////        const ul = document.createElement('ul');
//////        ul.className = 'pagination';

//////        const prevLi = document.createElement('li');
//////        prevLi.className = 'page-item ' + (paged.Page <= 1 ? 'disabled' : '');
//////        const prevA = document.createElement('a');
//////        prevA.className = 'page-link';
//////        prevA.href = '#';
//////        prevA.textContent = '« Previous';
//////        prevA.addEventListener('click', (e) => { e.preventDefault(); if (paged.Page > 1) loadPage(paged.Page - 1); });
//////        prevLi.appendChild(prevA);
//////        ul.appendChild(prevLi);


//////        const windowSize = 7;
//////        const half = Math.floor(windowSize / 2);
//////        let start = Math.max(1, paged.Page - half);
//////        let end = Math.min(paged.TotalPages || 1, start + windowSize - 1);
//////        if (end - start + 1 < windowSize) start = Math.max(1, end - windowSize + 1);

//////        for (let p = start; p <= end; p++) {
//////            const li = document.createElement('li');
//////            li.className = 'page-item' + (p === paged.Page ? ' active' : '');
//////            const a = document.createElement('a');
//////            a.className = 'page-link';
//////            a.href = '#';
//////            a.textContent = String(p);
//////            a.addEventListener('click', (e) => { e.preventDefault(); loadPage(p); });
//////            li.appendChild(a);
//////            ul.appendChild(li);
//////        }

//////        const nextLi = document.createElement('li');
//////        nextLi.className = 'page-item ' + ((paged.Page >= (paged.TotalPages || 1)) ? 'disabled' : '');
//////        const nextA = document.createElement('a');
//////        nextA.className = 'page-link';
//////        nextA.href = '#';
//////        nextA.textContent = 'Next »';
//////        nextA.addEventListener('click', (e) => { e.preventDefault(); if (paged.Page < (paged.TotalPages || 1)) loadPage(paged.Page + 1); });
//////        nextLi.appendChild(nextA);
//////        ul.appendChild(nextLi);

//////        container.appendChild(ul);

//////        const info = document.createElement('div');
//////        info.className = 'text-center mt-2';
//////        info.innerHTML = `<small class="text-muted">Page ${paged.Page} of ${paged.TotalPages || 1} — ${paged.TotalCount || 0} items</small>`;
//////        container.appendChild(info);
//////    }

//////    function loadPage(page = 1) {
//////        current = page;
//////        // show loading row
//////        const tbody = document.querySelector(tbodySelector);
//////        if (tbody) tbody.innerHTML = '<tr><td colspan="8" class="text-center">Loading...</td></tr>';

//////        ShipmentApiClient.getPaged(page, pageSize, sortBy, sortDir)
//////            .then(paged => {
//////                // paged.Items must be an array
//////                const items = (paged && Array.isArray(paged.Items)) ? paged.Items : [];
//////                ShipmentService.renderTable(tbodySelector, items, tableSelector);
//////                // render pager using the paged metadata
//////                renderPager({
//////                    Page: paged.Page || page,
//////                    PageSize: paged.PageSize || pageSize,
//////                    TotalPages: paged.TotalPages || Math.ceil((paged.TotalCount || items.length) / (paged.PageSize || pageSize)),
//////                    TotalCount: paged.TotalCount || 0
//////                });
//////            })
//////            .catch(err => {
//////                console.error('Failed to load paged shipments', err);
//////                const tbody = document.querySelector(tbodySelector);
//////                if (tbody) tbody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Error loading shipments</td></tr>';
//////            });
//////    }

//////    // expose small API for page controls elsewhere
//////    window.ShipmentClientUI = {
//////        loadPage: loadPage,
//////        setPageSize: (ps) => { pageSize = ps; loadPage(1); },
//////        setSort: (sBy, sDir) => { sortBy = sBy; sortDir = sDir; loadPage(1); }
//////    };

//////    // auto-init when included on the Index page
//////    document.addEventListener('DOMContentLoaded', function () {
//////        const pageSizeEl = document.getElementById('pageSizeSelect');
//////        if (pageSizeEl) {
//////            pageSizeEl.value = String(pageSize);
//////            pageSizeEl.addEventListener('change', () => {
//////                const ps = parseInt(pageSizeEl.value, 10) || 10;
//////                window.ShipmentClientUI.setPageSize(ps);
//////            });
//////        }
//////        loadPage(1);
//////    });
//////})();
