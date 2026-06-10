/*
 * Home dashboard bootstrap.
 * Rebuilds dashboardData from hidden JSON fields for dashboard-charts.js consumption.
 */
(function () {
    function parseJson(id) {
        var element = document.getElementById(id);
        if (!element) {
            return null;
        }
        try {
            return JSON.parse(element.value || 'null');
        }
        catch (_a) {
            return null;
        }
    }
    var shipmentsData = parseJson('dashboard-shipments-data');
    var revenueData = parseJson('dashboard-revenue-data');
    var statusData = parseJson('dashboard-status-data');
    var citiesData = parseJson('dashboard-cities-data');
    var labels = parseJson('dashboard-labels-data');
    if (!shipmentsData || !revenueData || !statusData || !citiesData || !labels) {
        return;
    }
    window.dashboardData = {
        shipmentsData: shipmentsData,
        revenueData: revenueData,
        statusData: statusData,
        citiesData: citiesData,
        labels: labels
    };
})();
//# sourceMappingURL=home-dashboard-bootstrap.js.map