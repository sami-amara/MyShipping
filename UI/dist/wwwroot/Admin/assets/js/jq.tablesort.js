/*
 * jq.TableSort -- jQuery Table sorter Plug-in.
 *
 * Version 1.0.0.
 *
 * Copyright (c) 2017 Dmitry Zavodnikov.
 *
 * Licensed under the MIT License.
 */
(function ($) {
    'use strict';
    const SORT = 'sort';
    const ASC = 'asc';
    const DESC = 'desc';
    const UNSORT = 'unsort';
    const config = {
        defaultColumn: 0,
        defaultOrder: 'asc',
        styles: {
            'sort': 'sortStyle',
            'asc': 'ascStyle',
            'desc': 'descStyle',
            'unsort': 'unsortStyle'
        },
        selector: function (tableBody, column) {
            const groups = [];
            const tableRows = $(tableBody).find('tr');
            for (let i = 0; i < tableRows.length; i++) {
                const td = $(tableRows[i]).find('td')[column];
                groups.push({
                    'values': [tableRows[i]],
                    'key': $(td).text()
                });
            }
            return groups;
        },
        comparator: function (group1, group2) {
            return group1.key.localeCompare(group2.key);
        }
    };
    function getTableHeaders(table) {
        return $(table).find('thead > tr > th');
    }
    function getSortableTableHeaders(table) {
        return getTableHeaders(table).filter(function (index) {
            return $(this).hasClass(config.styles[SORT]);
        });
    }
    function changeOrder(table, column) {
        const sortedHeader = getTableHeaders(table).filter(function (index) {
            return $(this).hasClass(config.styles[ASC]) || $(this).hasClass(config.styles[DESC]);
        });
        let sordOrder = config.defaultOrder;
        if (sortedHeader.hasClass(config.styles[ASC])) {
            sordOrder = ASC;
        }
        if (sortedHeader.hasClass(config.styles[DESC])) {
            sordOrder = DESC;
        }
        const th = getTableHeaders(table)[column];
        if (th === sortedHeader[0]) {
            if (sordOrder === ASC) {
                sordOrder = DESC;
            }
            else {
                sordOrder = ASC;
            }
        }
        const headers = getSortableTableHeaders(table);
        headers.removeClass(config.styles[ASC]);
        headers.removeClass(config.styles[DESC]);
        headers.addClass(config.styles[UNSORT]);
        $(th).removeClass(config.styles[UNSORT]);
        $(th).addClass(config.styles[sordOrder]);
        const tbody = $(table).find('tbody')[0];
        const groups = config.selector(tbody, column);
        // Sorting.
        groups.sort(function (a, b) {
            const res = config.comparator(a, b);
            return sordOrder === ASC ? res : -1 * res;
        });
        for (let i = 0; i < groups.length; i++) {
            const trList = groups[i];
            const trListValues = trList.values;
            for (let j = 0; j < trListValues.length; j++) {
                tbody.append(trListValues[j]);
            }
        }
    }
    $.fn.tablesort = function (userConfig) {
        // Create and save table sort configuration.
        $.extend(config, userConfig);
        // Process all selected tables.
        const selectedTables = this;
        for (let i = 0; i < selectedTables.length; i++) {
            var table = selectedTables[i];
            const tableHeader = getSortableTableHeaders(table);
            for (let j = 0; j < tableHeader.length; j++) {
                const th = tableHeader[j];
                $(th).on('click', function (event) {
                    const clickColumn = $.inArray(event.currentTarget, getTableHeaders(table));
                    changeOrder(table, clickColumn);
                });
            }
        }
        return this;
    };
})(jQuery);
//# sourceMappingURL=jq.tablesort.js.map