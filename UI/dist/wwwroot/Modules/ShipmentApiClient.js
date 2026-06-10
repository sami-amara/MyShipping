/* eslint-disable no-undef */
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
// ShipmentApiClient: thin adapter that reuses ApiClient and normalizes responses.
// ✅ UPDATED: All WebApi calls now pass useJwt=true for Bearer token authentication
const ShipmentApiClient = (function () {
    'use strict';
    const client = {
        apiListUrl: 'api/Shipments',
        apiCreateUrl: 'api/Shipments/Create',
        _getAntiForgeryHeaders() {
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.getAntiForgeryHeaders === 'function') {
                try {
                    return ApiClient.getAntiForgeryHeaders();
                }
                catch ( /* ignore */_a) { /* ignore */ }
            }
            const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
            return tokenEl && tokenEl.value ? { 'RequestVerificationToken': tokenEl.value } : {};
        },
        normalizeResponse: function (payload) {
            var _a, _b;
            if (payload === null || payload === undefined)
                return { success: true, message: null, data: null, errors: null, raw: payload };
            const success = !!(payload === true || payload === 'true' || (payload === null || payload === void 0 ? void 0 : payload.IsSuccess) === true || (payload === null || payload === void 0 ? void 0 : payload.isSuccess) === true || (payload === null || payload === void 0 ? void 0 : payload.success) === true || (payload === null || payload === void 0 ? void 0 : payload.Success) === true);
            const message = (payload === null || payload === void 0 ? void 0 : payload.Message) || (payload === null || payload === void 0 ? void 0 : payload.message) || (payload === null || payload === void 0 ? void 0 : payload.Title) || (payload === null || payload === void 0 ? void 0 : payload.title) || null;
            const data = (_b = (_a = payload === null || payload === void 0 ? void 0 : payload.Data) !== null && _a !== void 0 ? _a : payload === null || payload === void 0 ? void 0 : payload.data) !== null && _b !== void 0 ? _b : payload;
            const errors = (payload === null || payload === void 0 ? void 0 : payload.Errors) || (payload === null || payload === void 0 ? void 0 : payload.errors) || null;
            return { success, message, data, errors, raw: payload };
        },
        // ✅ UPDATED: Now passes useJwt=true to attach Bearer token
        _getJson: function (url) {
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.getJson === 'function') {
                return ApiClient.getJson(url, true); // ✅ CHANGED: Added true for JWT
            }
            return fetch(url, { credentials: 'include' })
                .then(resp => {
                if (resp.ok)
                    return resp.json().catch(() => ({}));
                return resp.json().then(err => Promise.reject(err));
            });
        },
        // ✅ UPDATED: Now passes useJwt=true to attach Bearer token
        _postJson: function (url, payload) {
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                return ApiClient.postJson(url, payload, true); // ✅ CHANGED: Added true for JWT
            }
            const headers = Object.assign({ 'Content-Type': 'application/json' }, this._getAntiForgeryHeaders());
            return fetch(url, {
                method: 'POST',
                headers: headers,
                body: payload !== null && payload !== undefined ? JSON.stringify(payload) : null,
                credentials: 'include'
            }).then((resp) => __awaiter(this, void 0, void 0, function* () {
                const text = yield resp.text().catch(() => null);
                let json = null;
                try {
                    json = text ? JSON.parse(text) : null;
                }
                catch (_a) {
                    json = null;
                }
                if (resp.ok)
                    return json !== null && json !== void 0 ? json : {};
                const err = new Error('Request failed: ' + resp.status);
                err.status = resp.status;
                err.responseJSON = json || null;
                err.responseText = text || null;
                throw err;
            }));
        },
        // ✅ UPDATED: Now passes useJwt=true to attach Bearer token
        _putJson: function (url, payload) {
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.request === 'function') {
                return ApiClient.request('PUT', url, payload, true).then(r => r.response); // ✅ CHANGED: Added true for JWT
            }
            const headers = Object.assign({ 'Content-Type': 'application/json' }, this._getAntiForgeryHeaders());
            return fetch(url, {
                method: 'PUT',
                headers: headers,
                credentials: 'include',
                body: payload !== null && payload !== undefined ? JSON.stringify(payload) : null
            }).then((resp) => __awaiter(this, void 0, void 0, function* () {
                const text = yield resp.text().catch(() => null);
                let json = null;
                try {
                    json = text ? JSON.parse(text) : null;
                }
                catch (_a) {
                    json = null;
                }
                if (resp.ok)
                    return json !== null && json !== void 0 ? json : {};
                const err = new Error('Request failed: ' + resp.status);
                err.status = resp.status;
                err.responseJSON = json || null;
                err.responseText = text || null;
                throw err;
            }));
        },
        update: function (id, shipmentDto) {
            if (!id)
                return Promise.reject(new Error('Invalid id'));
            const url = `${this.apiListUrl}/${encodeURIComponent(id)}`;
            console.debug('ShipmentApiClient.update: PUT', url, shipmentDto);
            return this._putJson(url, shipmentDto) // ✅ Uses _putJson which now includes JWT
                .then(payload => {
                return payload;
            });
        },
        _extractData(payload) {
            if (payload === null || payload === undefined)
                return [];
            if (Array.isArray(payload))
                return payload;
            if (payload.Data || payload.data)
                return payload.Data || payload.data;
            if (payload.IsSuccess || payload.isSuccess || payload.success) {
                return payload.Data || payload.data || [];
            }
            return payload;
        },
        _ensureStatus(item) {
            if (item === null || item === undefined)
                return item;
            if (item.Status || item.status) {
                item.Status = item.Status || item.status;
                return item;
            }
            const cs = (item.CurrentState !== undefined) ? item.CurrentState : (item.currentState !== undefined ? item.currentState : null);
            if (cs !== null && cs !== undefined) {
                item.Status = (Number(cs) === 1) ? 'Active' : 'Inactive';
                return item;
            }
            const statuses = item.TbShippmentStatuses || item.tbShippmentStatuses || item.shippmentStatuses || item.statuses;
            if (Array.isArray(statuses) && statuses.length > 0) {
                const latest = statuses[0];
                const st = (latest.CurrentState !== undefined) ? latest.CurrentState : (latest.currentState !== undefined ? latest.currentState : null);
                if (st !== null && st !== undefined) {
                    item.Status = (Number(st) === 1) ? 'Active' : 'Inactive';
                    return item;
                }
                item.Status = latest.Notes || latest.notes || latest.Description || latest.description || null;
            }
            return item;
        },
        // ✅ UPDATED: Now uses ApiClient.delete with useJwt=true
        delete: function (id) {
            if (!id)
                return Promise.reject(new Error('Invalid id'));
            const relativeUrl = `${this.apiListUrl}/${encodeURIComponent(id)}`;
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.delete === 'function') {
                return ApiClient.delete(relativeUrl, true) // ✅ CHANGED: Added true for JWT
                    .then(payload => {
                    const p = payload !== null && payload !== void 0 ? payload : null;
                    const success = (p === null || p === undefined) || !!(p.isSuccess || p.IsSuccess || p.success || p.Success);
                    const message = (p === null || p === void 0 ? void 0 : p.message) || (p === null || p === void 0 ? void 0 : p.Message) || (success ? 'Deleted' : 'Delete returned no message');
                    const data = (p === null || p === void 0 ? void 0 : p.data) || (p === null || p === void 0 ? void 0 : p.Data) || null;
                    return { success, message, data };
                })
                    .catch((err) => __awaiter(this, void 0, void 0, function* () {
                    if (err && (err.status === 404 || err.status === 405)) {
                        const fallbackUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/changestatus?newState=0`;
                        const fb = yield this._postJson(fallbackUrl, null); // ✅ Uses _postJson with JWT
                        return {
                            success: !!((fb === null || fb === void 0 ? void 0 : fb.isSuccess) || (fb === null || fb === void 0 ? void 0 : fb.IsSuccess) || (fb === null || fb === void 0 ? void 0 : fb.success) || (fb === null || fb === void 0 ? void 0 : fb.Success)),
                            message: (fb === null || fb === void 0 ? void 0 : fb.message) || (fb === null || fb === void 0 ? void 0 : fb.Message) || 'Status changed (fallback)',
                            data: (fb === null || fb === void 0 ? void 0 : fb.data) || (fb === null || fb === void 0 ? void 0 : fb.Data) || null
                        };
                    }
                    throw err;
                }));
            }
            // Fallback implementation remains the same
            const headers = Object.assign({ Accept: 'application/json' }, this._getAntiForgeryHeaders());
            const url = `${this.apiListUrl}/${encodeURIComponent(id)}`;
            return fetch(url, { method: 'DELETE', credentials: 'same-origin', headers })
                .then((resp) => __awaiter(this, void 0, void 0, function* () {
                var _a, _b;
                const text = yield resp.text().catch(() => null);
                let payload = null;
                try {
                    payload = text ? JSON.parse(text) : null;
                }
                catch (_c) {
                    payload = null;
                }
                if (!resp.ok) {
                    if (resp.status === 404 || resp.status === 405) {
                        const fallbackUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/changestatus?newState=0`;
                        const fallback = yield this._postJson(fallbackUrl, null).catch(e => { throw e; });
                        return {
                            success: !!((fallback === null || fallback === void 0 ? void 0 : fallback.isSuccess) || (fallback === null || fallback === void 0 ? void 0 : fallback.IsSuccess) || (fallback === null || fallback === void 0 ? void 0 : fallback.success) || (fallback === null || fallback === void 0 ? void 0 : fallback.Success)),
                            message: (fallback === null || fallback === void 0 ? void 0 : fallback.message) || (fallback === null || fallback === void 0 ? void 0 : fallback.Message) || 'Status changed (fallback)',
                            data: (fallback === null || fallback === void 0 ? void 0 : fallback.data) || (fallback === null || fallback === void 0 ? void 0 : fallback.Data) || null
                        };
                    }
                    const err = new Error('Delete failed: ' + resp.status);
                    err.status = resp.status;
                    err.body = text;
                    throw err;
                }
                const message = (payload === null || payload === void 0 ? void 0 : payload.message) || (payload === null || payload === void 0 ? void 0 : payload.Message) || 'Deleted';
                const success = !!((payload === null || payload === void 0 ? void 0 : payload.isSuccess) || (payload === null || payload === void 0 ? void 0 : payload.IsSuccess) || (payload === null || payload === void 0 ? void 0 : payload.success) || (payload === null || payload === void 0 ? void 0 : payload.Success)) || payload === null;
                return { success, message, data: (_b = (_a = payload === null || payload === void 0 ? void 0 : payload.data) !== null && _a !== void 0 ? _a : payload === null || payload === void 0 ? void 0 : payload.Data) !== null && _b !== void 0 ? _b : null };
            }))
                .catch((ex) => __awaiter(this, void 0, void 0, function* () {
                try {
                    const fallbackUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/changestatus?newState=0`;
                    const fallback = yield this._postJson(fallbackUrl, null);
                    return {
                        success: !!((fallback === null || fallback === void 0 ? void 0 : fallback.isSuccess) || (fallback === null || fallback === void 0 ? void 0 : fallback.IsSuccess) || (fallback === null || fallback === void 0 ? void 0 : fallback.success) || (fallback === null || fallback === void 0 ? void 0 : fallback.Success)),
                        message: (fallback === null || fallback === void 0 ? void 0 : fallback.message) || (fallback === null || fallback === void 0 ? void 0 : fallback.Message) || 'Status changed (fallback)',
                        data: (fallback === null || fallback === void 0 ? void 0 : fallback.data) || (fallback === null || fallback === void 0 ? void 0 : fallback.Data) || null
                    };
                }
                catch (fallbackEx) {
                    throw ex;
                }
            }));
        },
        changeStatus: function (id, newStateOrPayload) {
            if (!id)
                return Promise.reject(new Error('Invalid id'));
            // If a payload object is passed, try posting to id-specific ChangeStatus endpoint first, then fallback
            if (newStateOrPayload && typeof newStateOrPayload === 'object') {
                const relativeUrlId = `${this.apiListUrl}/${encodeURIComponent(id)}/ChangeStatus`;
                const relativeUrl = `${this.apiListUrl}/ChangeStatus`;
                if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                    return ApiClient.postJson(relativeUrlId, newStateOrPayload, true)
                        .catch(err => {
                        // if id-specific endpoint not found, fallback to non-id endpoint
                        if (err && (err.status === 404 || err.status === 405))
                            return ApiClient.postJson(relativeUrl, newStateOrPayload, true);
                        throw err;
                    })
                        .then(resp => resp);
                }
                // fallback fetch path: try id-specific then non-id
                return this._postJson(relativeUrlId, newStateOrPayload).catch(err => {
                    if (err && err.status === 404)
                        return this._postJson(relativeUrl, newStateOrPayload);
                    throw err;
                });
            }
            // Otherwise treat second argument as a primitive newState and send as query string
            const url = `${this.apiListUrl}/${encodeURIComponent(id)}/changestatus?newState=${encodeURIComponent(newStateOrPayload)}`;
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                return ApiClient.postJson(url, null, true).then(resp => resp);
            }
            return this._postJson(url, null); // uses JWT-enabled helper
        },
        getPaged: function (page = 1, pageSize = 10, sortBy = 'CreatedDate', sortDir = 'desc') {
            const url = `${this.apiListUrl}/paged?page=${encodeURIComponent(page)}&pageSize=${encodeURIComponent(pageSize)}&sortBy=${encodeURIComponent(sortBy)}&sortDir=${encodeURIComponent(sortDir)}`;
            return this._getJson(url) // ✅ Uses _getJson which now includes JWT
                .then(payload => {
                const paged = (payload && (payload.Data || payload.data)) ? (payload.Data || payload.data) : payload;
                if (!paged)
                    return paged;
                const itemsRaw = paged.Items || paged.items || [];
                const items = Array.isArray(itemsRaw) ? itemsRaw.map(it => this._ensureStatus(it)) : itemsRaw;
                return Object.assign({}, paged, { Items: items });
            });
        },
        getAll: function () {
            return this._getJson(this.apiListUrl) // ✅ Uses _getJson which now includes JWT
                .then(payload => {
                const data = this._extractData(payload) || [];
                if (!Array.isArray(data))
                    return data;
                return data.map(it => this._ensureStatus(it));
            });
        },
        // ✅ UPDATED: Now uses _getJson helper with JWT support
        getById: function (id) {
            const url = `${this.apiListUrl}/${encodeURIComponent(id)}`;
            return this._getJson(url) // ✅ CHANGED: Use _getJson instead of fetch
                .then(payload => {
                return payload && (payload.Data || payload.data) ? (payload.Data || payload.data) : payload;
            });
        },
        create: function (shipmentDto) {
            return this._postJson(this.apiCreateUrl, shipmentDto) // ✅ Uses _postJson which now includes JWT
                .then(payload => {
                return payload;
            });
        },
        approved: function (id, shipmentDto) {
            if (!id)
                return Promise.reject(new Error('Invalid id'));
            const relativeUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/approved`;
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                return ApiClient.postJson(relativeUrl, shipmentDto, true)
                    .then(resp => resp);
            }
            const headers = Object.assign({ 'Content-Type': 'application/json' }, this._getAntiForgeryHeaders());
            return fetch(relativeUrl, {
                method: 'POST',
                credentials: 'include',
                headers,
                body: shipmentDto ? JSON.stringify(shipmentDto) : null
            }).then((resp) => __awaiter(this, void 0, void 0, function* () {
                const text = yield resp.text().catch(() => null);
                let json = null;
                try {
                    json = text ? JSON.parse(text) : null;
                }
                catch (_a) {
                    json = null;
                }
                if (resp.ok) {
                    console.debug('ShipmentApiClient.approved response (fetch):', json !== null && json !== void 0 ? json : {});
                    return json !== null && json !== void 0 ? json : {};
                }
                const err = new Error('Request failed: ' + resp.status);
                err.status = resp.status;
                err.responseJSON = json || null;
                err.responseText = text || null;
                throw err;
            }));
        },
        // without DeliveryDate time 
        ready: function (id, payloadOrCarrierId) {
            if (!id)
                return Promise.reject(new Error('Invalid id'));
            let carrierId = null;
            if (payloadOrCarrierId && typeof payloadOrCarrierId === 'object') {
                carrierId = payloadOrCarrierId.CarrierId || payloadOrCarrierId.carrierId || null;
            }
            else if (payloadOrCarrierId) {
                carrierId = payloadOrCarrierId;
            }
            const qs = carrierId ? `?carrierId=${encodeURIComponent(carrierId)}` : '';
            const relativeUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/ready${qs}`;
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                return ApiClient.postJson(relativeUrl, null, true) // ✅ CHANGED: false → true for JWT
                    .then(resp => {
                    console.debug('ShipmentApiClient.ready response (via ApiClient):', resp);
                    return resp;
                });
            }
            const headers = Object.assign({ Accept: 'application/json' }, this._getAntiForgeryHeaders());
            return fetch(relativeUrl, { method: 'POST', credentials: 'include', headers })
                .then((resp) => __awaiter(this, void 0, void 0, function* () {
                const text = yield resp.text().catch(() => null);
                let json = null;
                try {
                    json = text ? JSON.parse(text) : null;
                }
                catch (_a) {
                    json = null;
                }
                if (resp.ok) {
                    console.debug('ShipmentApiClient.ready response (fetch):', json !== null && json !== void 0 ? json : {});
                    return json !== null && json !== void 0 ? json : {};
                }
                const err = new Error('Request failed: ' + resp.status);
                err.status = resp.status;
                err.responseJSON = json || null;
                err.responseText = text || null;
                throw err;
            }));
        },
        // Hits /ChangeStatus endpoint with minimal body { Id, CurrentState: 4, CarrierId }
        readyGeneric: function (id, payloadOrCarrierId) {
            if (!id)
                return Promise.reject(new Error('Invalid id'));
            let carrierId = null;
            if (payloadOrCarrierId && typeof payloadOrCarrierId === 'object') {
                carrierId = payloadOrCarrierId.CarrierId || payloadOrCarrierId.carrierId || null;
            }
            else if (payloadOrCarrierId) {
                carrierId = payloadOrCarrierId;
            }
            // Build minimal body — ChangeStatus requires [FromBody] ShippmentDto (non-null)
            const body = { Id: id, CurrentState: 4, CarrierId: carrierId || null };
            const relativeUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/ChangeStatus`;
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                return ApiClient.postJson(relativeUrl, body, true)
                    .then(resp => {
                    console.debug('ShipmentApiClient.readyGeneric response (via ApiClient):', resp);
                    return resp;
                });
            }
            const headers = Object.assign({ 'Content-Type': 'application/json' }, this._getAntiForgeryHeaders());
            return fetch(relativeUrl, {
                method: 'POST',
                credentials: 'include',
                headers,
                body: JSON.stringify(body)
            })
                .then((resp) => __awaiter(this, void 0, void 0, function* () {
                const text = yield resp.text().catch(() => null);
                let json = null;
                try {
                    json = text ? JSON.parse(text) : null;
                }
                catch (_a) {
                    json = null;
                }
                if (resp.ok) {
                    console.debug('ShipmentApiClient.readyGeneric response (fetch):', json !== null && json !== void 0 ? json : {});
                    return json !== null && json !== void 0 ? json : {};
                }
                const err = new Error('Request failed: ' + resp.status);
                err.status = resp.status;
                err.responseJSON = json || null;
                err.responseText = text || null;
                throw err;
            }));
        },
        // Calls POST /api/Shipments/{id}/UpdateStatus?newState=N
        // Serves: Deleted=0, Shipped=5, Delivered=6, Cancelled=7, Returned=8
        updateStatus: function (id, newState) {
            if (!id)
                return Promise.reject(new Error('Invalid id'));
            const relativeUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/UpdateStatus?newState=${encodeURIComponent(newState)}`;
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                return ApiClient.postJson(relativeUrl, null, true)
                    .then(resp => {
                    console.debug('ShipmentApiClient.updateStatus response:', resp);
                    return resp;
                });
            }
            const headers = Object.assign({ Accept: 'application/json' }, this._getAntiForgeryHeaders());
            return fetch(relativeUrl, { method: 'POST', credentials: 'include', headers })
                .then((resp) => __awaiter(this, void 0, void 0, function* () {
                const text = yield resp.text().catch(() => null);
                let json = null;
                try {
                    json = text ? JSON.parse(text) : null;
                }
                catch (_a) {
                    json = null;
                }
                if (resp.ok)
                    return json !== null && json !== void 0 ? json : {};
                const err = new Error('Request failed: ' + resp.status);
                err.status = resp.status;
                err.responseJSON = json || null;
                err.responseText = text || null;
                throw err;
            }));
        },
        shipped: function (id) {
            if (!id)
                return Promise.reject(new Error('Invalid id'));
            const relativeUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/shipped`;
            // ✅ DEBUG: Log the exact URL being called
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                return ApiClient.postJson(relativeUrl, null, true) // ✅ false for cookies
                    .then(resp => {
                    return resp;
                })
                    .catch(err => {
                    throw err;
                });
            }
            const headers = Object.assign({ Accept: 'application/json' }, this._getAntiForgeryHeaders());
            return fetch(relativeUrl, {
                method: 'POST',
                credentials: 'include', // ✅ Send cookies
                headers
            })
                .then((resp) => __awaiter(this, void 0, void 0, function* () {
                const text = yield resp.text().catch(() => null);
                let json = null;
                try {
                    json = text ? JSON.parse(text) : null;
                }
                catch (_a) {
                    json = null;
                }
                if (resp.ok) {
                    return json !== null && json !== void 0 ? json : {};
                }
                const err = new Error('Request failed: ' + resp.status);
                err.status = resp.status;
                err.responseJSON = json || null;
                err.responseText = text || null;
                throw err;
            }));
        }
    };
    return client;
})();
window.ShipmentApiClient = ShipmentApiClient;
//# sourceMappingURL=ShipmentApiClient.js.map