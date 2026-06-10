///* eslint-disable no-undef */
//// ShipmentWorkflowClient: small helper to call WebAPI workflow endpoints
//// Usage: ShipmentWorkflowClient.post('/api/Shipments/{id}/approve') or use provided helpers.
//window.ShipmentWorkflowClient = (function () {
//    const client = {
//        baseUrl: '/api/Shipments'
//    };
//    function handleResponse(resp) {
//        if (!resp) return Promise.reject(new Error('No response'));
//        if (resp.ok) return resp.json().catch(() => null);
//        return resp.json().then(j => Promise.reject(j)).catch(() => Promise.reject({ message: resp.statusText }));
//    }
//    client.approve = function (id) {
//        return fetch(`${client.baseUrl}/${encodeURIComponent(id)}/approve`, { method: 'POST', credentials: 'same-origin' }).then(handleResponse);
//    };
//    client.markReady = function (id) {
//        return fetch(`${client.baseUrl}/${encodeURIComponent(id)}/ready`, { method: 'POST', credentials: 'same-origin' }).then(handleResponse);
//    };
//    client.markShipped = function (id) {
//        return fetch(`${client.baseUrl}/${encodeURIComponent(id)}/ship`, { method: 'POST', credentials: 'same-origin' }).then(handleResponse);
//    };
//    client.markReturned = function (id) {
//        return fetch(`${client.baseUrl}/${encodeURIComponent(id)}/return`, { method: 'POST', credentials: 'same-origin' }).then(handleResponse);
//    };
//    return client;
//})();
//# sourceMappingURL=ShipmentWorkflowClient.js.map