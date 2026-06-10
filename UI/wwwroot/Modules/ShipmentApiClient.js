/* eslint-disable no-undef */

// ShipmentApiClient: thin adapter that reuses ApiClient and normalizes responses.
// ✅ UPDATED: All WebApi calls now pass useJwt=true for Bearer token authentication

const ShipmentApiClient = (function () {
    'use strict';

    const client = {
        apiListUrl: 'api/Shipments',
        apiCreateUrl: 'api/Shipments/Create',

        _getAntiForgeryHeaders() {
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.getAntiForgeryHeaders === 'function') {
                try { return ApiClient.getAntiForgeryHeaders(); } catch { /* ignore */ }
            }
            const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
            return tokenEl && tokenEl.value ? { 'RequestVerificationToken': tokenEl.value } : {};
        },

        normalizeResponse: function (payload) {
            if (payload === null || payload === undefined) return { success: true, message: null, data: null, errors: null, raw: payload };
            const success = !!(payload === true || payload === 'true' || payload?.IsSuccess === true || payload?.isSuccess === true || payload?.success === true || payload?.Success === true);
            const message = payload?.Message || payload?.message || payload?.Title || payload?.title || null;
            const data = payload?.Data ?? payload?.data ?? payload;
            const errors = payload?.Errors || payload?.errors || null;
            return { success, message, data, errors, raw: payload };
        },

        // ✅ UPDATED: Now passes useJwt=true to attach Bearer token
        _getJson: function (url) {
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.getJson === 'function') {
                return ApiClient.getJson(url, true);  // ✅ CHANGED: Added true for JWT
            }
            return fetch(url, { credentials: 'include' })
                .then(resp => {
                    if (resp.ok) return resp.json().catch(() => ({}));
                    return resp.json().then(err => Promise.reject(err));
                });
        },

        // ✅ UPDATED: Now passes useJwt=true to attach Bearer token
        _postJson: function (url, payload) {
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                return ApiClient.postJson(url, payload, true);  // ✅ CHANGED: Added true for JWT
            }
            const headers = Object.assign({ 'Content-Type': 'application/json' }, this._getAntiForgeryHeaders());
            return fetch(url, {
                method: 'POST',
                headers: headers,
                body: payload !== null && payload !== undefined ? JSON.stringify(payload) : null,
                credentials: 'include'
            }).then(async resp => {
                const text = await resp.text().catch(() => null);
                let json = null;
                try { json = text ? JSON.parse(text) : null; } catch { json = null; }

                if (resp.ok) return json ?? {};
                const err = new Error('Request failed: ' + resp.status);
                err.status = resp.status;
                err.responseJSON = json || null;
                err.responseText = text || null;
                throw err;
            });
        },

        // ✅ UPDATED: Now passes useJwt=true to attach Bearer token
        _putJson: function (url, payload) {
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.request === 'function') {
                return ApiClient.request('PUT', url, payload, true).then(r => r.response);  // ✅ CHANGED: Added true for JWT
            }
            const headers = Object.assign({ 'Content-Type': 'application/json' }, this._getAntiForgeryHeaders());

            return fetch(url, {
                method: 'PUT',
                headers: headers,
                credentials: 'include',
                body: payload !== null && payload !== undefined ? JSON.stringify(payload) : null
            }).then(async resp => {
                const text = await resp.text().catch(() => null);
                let json = null;
                try { json = text ? JSON.parse(text) : null; } catch { json = null; }

                if (resp.ok) return json ?? {};
                const err = new Error('Request failed: ' + resp.status);
                err.status = resp.status;
                err.responseJSON = json || null;
                err.responseText = text || null;
                throw err;
            });
        },

        update: function (id, shipmentDto) {
            if (!id) return Promise.reject(new Error('Invalid id'));
            const url = `${this.apiListUrl}/${encodeURIComponent(id)}`;

            console.debug('ShipmentApiClient.update: PUT', url, shipmentDto);
            return this._putJson(url, shipmentDto)  // ✅ Uses _putJson which now includes JWT
                .then(payload => {
                    
                    return payload;
                });
        },

        _extractData(payload) {
            if (payload === null || payload === undefined) return [];
            if (Array.isArray(payload)) return payload;
            if (payload.Data || payload.data) return payload.Data || payload.data;
            if (payload.IsSuccess || payload.isSuccess || payload.success) {
                return payload.Data || payload.data || [];
            }
            return payload;
        },

        _ensureStatus(item) {
            if (item === null || item === undefined) return item;
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
            if (!id) return Promise.reject(new Error('Invalid id'));
            const relativeUrl = `${this.apiListUrl}/${encodeURIComponent(id)}`;

            if (typeof ApiClient !== 'undefined' && typeof ApiClient.delete === 'function') {
                return ApiClient.delete(relativeUrl, true)  // ✅ CHANGED: Added true for JWT
                    .then(payload => {
                        const p = payload ?? null;
                        const success = (p === null || p === undefined) || !!(p.isSuccess || p.IsSuccess || p.success || p.Success);
                        const message = p?.message || p?.Message || (success ? 'Deleted' : 'Delete returned no message');
                        const data = p?.data || p?.Data || null;
                        return { success, message, data };
                    })
                    .catch(async err => {
                        if (err && (err.status === 404 || err.status === 405)) {
                            const fallbackUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/changestatus?newState=0`;
                            const fb = await this._postJson(fallbackUrl, null);  // ✅ Uses _postJson with JWT
                            return {
                                success: !!(fb?.isSuccess || fb?.IsSuccess || fb?.success || fb?.Success),
                                message: fb?.message || fb?.Message || 'Status changed (fallback)',
                                data: fb?.data || fb?.Data || null
                            };
                        }
                        throw err;
                    });
            }

            // Fallback implementation remains the same
            const headers = Object.assign({ Accept: 'application/json' }, this._getAntiForgeryHeaders());
            const url = `${this.apiListUrl}/${encodeURIComponent(id)}`;
            return fetch(url, { method: 'DELETE', credentials: 'same-origin', headers })
                .then(async resp => {
                    const text = await resp.text().catch(() => null);
                    let payload = null;
                    try { payload = text ? JSON.parse(text) : null; } catch { payload = null; }

                    if (!resp.ok) {
                        if (resp.status === 404 || resp.status === 405) {
                            const fallbackUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/changestatus?newState=0`;
                            const fallback = await this._postJson(fallbackUrl, null).catch(e => { throw e; });
                            return {
                                success: !!(fallback?.isSuccess || fallback?.IsSuccess || fallback?.success || fallback?.Success),
                                message: fallback?.message || fallback?.Message || 'Status changed (fallback)',
                                data: fallback?.data || fallback?.Data || null
                            };
                        }

                        const err = new Error('Delete failed: ' + resp.status);
                        err.status = resp.status;
                        err.body = text;
                        throw err;
                    }

                    const message = payload?.message || payload?.Message || 'Deleted';
                    const success = !!(payload?.isSuccess || payload?.IsSuccess || payload?.success || payload?.Success) || payload === null;
                    return { success, message, data: payload?.data ?? payload?.Data ?? null };
                })
                .catch(async ex => {
                    try {
                        const fallbackUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/changestatus?newState=0`;
                        const fallback = await this._postJson(fallbackUrl, null);
                        return {
                            success: !!(fallback?.isSuccess || fallback?.IsSuccess || fallback?.success || fallback?.Success),
                            message: fallback?.message || fallback?.Message || 'Status changed (fallback)',
                            data: fallback?.data || fallback?.Data || null
                        };
                    } catch (fallbackEx) {
                        throw ex;
                    }
                });
        },

        changeStatus: function (id, newStateOrPayload) {
            if (!id) return Promise.reject(new Error('Invalid id'));
            // If a payload object is passed, try posting to id-specific ChangeStatus endpoint first, then fallback
            if (newStateOrPayload && typeof newStateOrPayload === 'object') {
                const relativeUrlId = `${this.apiListUrl}/${encodeURIComponent(id)}/ChangeStatus`;
                const relativeUrl = `${this.apiListUrl}/ChangeStatus`;
                if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                    return ApiClient.postJson(relativeUrlId, newStateOrPayload, true)
                        .catch(err => {
                            // if id-specific endpoint not found, fallback to non-id endpoint
                            if (err && (err.status === 404 || err.status === 405)) return ApiClient.postJson(relativeUrl, newStateOrPayload, true);
                            throw err;
                        })
                        .then(resp => resp);
                }
                // fallback fetch path: try id-specific then non-id
                return this._postJson(relativeUrlId, newStateOrPayload).catch(err => {
                    if (err && err.status === 404) return this._postJson(relativeUrl, newStateOrPayload);
                    throw err;
                });
            }

            // Otherwise treat second argument as a primitive newState and send as query string
            const url = `${this.apiListUrl}/${encodeURIComponent(id)}/changestatus?newState=${encodeURIComponent(newStateOrPayload)}`;
            if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                return ApiClient.postJson(url, null, true).then(resp => resp);
            }
            return this._postJson(url, null);  // uses JWT-enabled helper
        },

        getPaged: function (page = 1, pageSize = 10, sortBy = 'CreatedDate', sortDir = 'desc') {
            const url = `${this.apiListUrl}/paged?page=${encodeURIComponent(page)}&pageSize=${encodeURIComponent(pageSize)}&sortBy=${encodeURIComponent(sortBy)}&sortDir=${encodeURIComponent(sortDir)}`;
            return this._getJson(url)  // ✅ Uses _getJson which now includes JWT
                .then(payload => {
                    const paged = (payload && (payload.Data || payload.data)) ? (payload.Data || payload.data) : payload;
                    if (!paged) return paged;

                    const itemsRaw = paged.Items || paged.items || [];
                    const items = Array.isArray(itemsRaw) ? itemsRaw.map(it => this._ensureStatus(it)) : itemsRaw;

                    return Object.assign({}, paged, { Items: items });
                });
        },

        getAll: function () {
            return this._getJson(this.apiListUrl)  // ✅ Uses _getJson which now includes JWT
                .then(payload => {
                    const data = this._extractData(payload) || [];
                    if (!Array.isArray(data)) return data;
                    return data.map(it => this._ensureStatus(it));
                });
        },

        // ✅ UPDATED: Now uses _getJson helper with JWT support
        getById: function (id) {
            const url = `${this.apiListUrl}/${encodeURIComponent(id)}`;
            return this._getJson(url)  // ✅ CHANGED: Use _getJson instead of fetch
                .then(payload => {
                    return payload && (payload.Data || payload.data) ? (payload.Data || payload.data) : payload;
                });
        },

        create: function (shipmentDto) {
           
            return this._postJson(this.apiCreateUrl, shipmentDto)  // ✅ Uses _postJson which now includes JWT
                .then(payload => {
                   
                    return payload;
                });
        },


        approved: function (id, shipmentDto) {
            if (!id) return Promise.reject(new Error('Invalid id'));
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
            }).then(async resp => {
                const text = await resp.text().catch(() => null);
                let json = null;
                try { json = text ? JSON.parse(text) : null; } catch { json = null; }
                if (resp.ok) {
                    console.debug('ShipmentApiClient.approved response (fetch):', json ?? {});
                    return json ?? {};
                }
                const err = new Error('Request failed: ' + resp.status);
                err.status = resp.status;
                err.responseJSON = json || null;
                err.responseText = text || null;
                throw err;
            });
        },

        // without DeliveryDate time 
        ready: function (id, payloadOrCarrierId) {
            if (!id) return Promise.reject(new Error('Invalid id'));

            let carrierId = null;
            if (payloadOrCarrierId && typeof payloadOrCarrierId === 'object') {
                carrierId = payloadOrCarrierId.CarrierId || payloadOrCarrierId.carrierId || null;
            } else if (payloadOrCarrierId) {
                carrierId = payloadOrCarrierId;
            }

            const qs = carrierId ? `?carrierId=${encodeURIComponent(carrierId)}` : '';
            const relativeUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/ready${qs}`;
            

            if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                return ApiClient.postJson(relativeUrl, null, true)  // ✅ CHANGED: false → true for JWT
                    .then(resp => {
                        console.debug('ShipmentApiClient.ready response (via ApiClient):', resp);
                        return resp;
                    });
            }

            const headers = Object.assign({ Accept: 'application/json' }, this._getAntiForgeryHeaders());
            return fetch(relativeUrl, { method: 'POST', credentials: 'include', headers })
                .then(async resp => {
                    const text = await resp.text().catch(() => null);
                    let json = null;
                    try { json = text ? JSON.parse(text) : null; } catch { json = null; }
                    if (resp.ok) {
                        console.debug('ShipmentApiClient.ready response (fetch):', json ?? {});
                        return json ?? {};
                    }
                    const err = new Error('Request failed: ' + resp.status);
                    err.status = resp.status;
                    err.responseJSON = json || null;
                    err.responseText = text || null;
                    throw err;
                });
        },

        // Hits /ChangeStatus endpoint with minimal body { Id, CurrentState: 4, CarrierId }
        readyGeneric: function (id, payloadOrCarrierId) {
            if (!id) return Promise.reject(new Error('Invalid id'));

            let carrierId = null;
            if (payloadOrCarrierId && typeof payloadOrCarrierId === 'object') {
                carrierId = payloadOrCarrierId.CarrierId || payloadOrCarrierId.carrierId || null;
            } else if (payloadOrCarrierId) {
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
                .then(async resp => {
                    const text = await resp.text().catch(() => null);
                    let json = null;
                    try { json = text ? JSON.parse(text) : null; } catch { json = null; }
                    if (resp.ok) {
                        console.debug('ShipmentApiClient.readyGeneric response (fetch):', json ?? {});
                        return json ?? {};
                    }
                    const err = new Error('Request failed: ' + resp.status);
                    err.status = resp.status;
                    err.responseJSON = json || null;
                    err.responseText = text || null;
                    throw err;
                });
        },



        // Calls POST /api/Shipments/{id}/UpdateStatus?newState=N
        // Serves: Deleted=0, Shipped=5, Delivered=6, Cancelled=7, Returned=8
        updateStatus: function (id, newState) {
            if (!id) return Promise.reject(new Error('Invalid id'));
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
                .then(async resp => {
                    const text = await resp.text().catch(() => null);
                    let json = null;
                    try { json = text ? JSON.parse(text) : null; } catch { json = null; }
                    if (resp.ok) return json ?? {};
                    const err = new Error('Request failed: ' + resp.status);
                    err.status = resp.status;
                    err.responseJSON = json || null;
                    err.responseText = text || null;
                    throw err;
                });
        },

        shipped: function (id) {
            if (!id) return Promise.reject(new Error('Invalid id'));

            const relativeUrl = `${this.apiListUrl}/${encodeURIComponent(id)}/shipped`;

            // ✅ DEBUG: Log the exact URL being called
           

            if (typeof ApiClient !== 'undefined' && typeof ApiClient.postJson === 'function') {
                
                return ApiClient.postJson(relativeUrl, null, true)  // ✅ false for cookies
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
                credentials: 'include',  // ✅ Send cookies
                headers
            })
                .then(async resp => {
                    const text = await resp.text().catch(() => null);
                    

                    let json = null;
                    try { json = text ? JSON.parse(text) : null; } catch { json = null; }

                    if (resp.ok) {
                        
                        return json ?? {};
                    }

                    
                    const err = new Error('Request failed: ' + resp.status);
                    err.status = resp.status;
                    err.responseJSON = json || null;
                    err.responseText = text || null;
                    throw err;
                });
        }
       
    };

    return client;
})();

window.ShipmentApiClient = ShipmentApiClient;



