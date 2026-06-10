var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
(function () {
    'use strict';
    const ApiClient = {
        baseUrl: 'https://localhost:7228/',
        _isRefreshing: false,
        _cachedToken: null,
        getAntiForgeryHeaders: function () {
            try {
                const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
                if (tokenEl && tokenEl.value) {
                    return { 'RequestVerificationToken': tokenEl.value };
                }
            }
            catch (e) { /* ignore */ }
            return {};
        },
        // ✅ Get AccessToken from UI endpoint
        getAccessToken: function () {
            return __awaiter(this, void 0, void 0, function* () {
                if (this._cachedToken) {
                    // console.log('✅ Using cached AccessToken');
                    return this._cachedToken;
                }
                try {
                    const response = yield fetch('/Account/GetAccessToken', {
                        credentials: 'include'
                    });
                    if (response.ok) {
                        const data = yield response.json();
                        this._cachedToken = data.token;
                        //console.log('✅ AccessToken retrieved from UI claims');
                        return data.token;
                    }
                    else {
                        //console.warn('⚠️ Failed to get AccessToken from UI:', response.status);
                    }
                }
                catch (err) {
                    //console.error('❌ Error getting AccessToken:', err);
                }
                return null;
            });
        },
        refreshToken: function () {
            return __awaiter(this, void 0, void 0, function* () {
                var _a;
                if (this._isRefreshing) {
                    //console.warn('⚠️ Token refresh already in progress');
                    return false;
                }
                try {
                    this._isRefreshing = true;
                    const response = yield fetch(this.baseUrl + 'api/auth/refresh-access-token', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        credentials: 'include',
                        mode: 'cors'
                    });
                    if (!response.ok) {
                        const errorText = yield response.text();
                        return false;
                    }
                    const data = yield response.json();
                    if (!data || !data.isSuccess || !data.data || !data.data.accessToken) { // ✅ FIXED
                        return false;
                    }
                    const newAccessToken = data.data.accessToken; // ✅ FIXED
                    const antiForgeryToken = (_a = document.querySelector('input[name="__RequestVerificationToken"]')) === null || _a === void 0 ? void 0 : _a.value;
                    //console.log('   AntiForgery token:', antiForgeryToken ? 'Found ✅' : 'MISSING ❌');
                    const updateResponse = yield fetch('/Account/RefreshUserToken', {
                        method: 'POST',
                        credentials: 'include',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': antiForgeryToken || ''
                        },
                        body: JSON.stringify({ accessToken: newAccessToken })
                    });
                    if (!updateResponse.ok) {
                        const errorText = yield updateResponse.text();
                        return false;
                    }
                    const updateData = yield updateResponse.json();
                    this._cachedToken = newAccessToken;
                    return true;
                }
                catch (err) {
                    this._cachedToken = null;
                    return false;
                }
                finally {
                    this._isRefreshing = false;
                }
            });
        },
        // ✅ Generic request with auto-retry on 401
        request: function (method_1, url_1) {
            return __awaiter(this, arguments, void 0, function* (method, url, data = null, useJwt = false) {
                const headers = {};
                if (useJwt) {
                    const token = yield this.getAccessToken();
                    if (token) {
                        headers['Authorization'] = 'Bearer ' + token;
                        //console.log('✅ Bearer token attached to', method, url);
                    }
                    else {
                        //console.warn('⚠️ No AccessToken available for', method, url);
                    }
                }
                const ajaxOptions = {
                    url: this.baseUrl + url,
                    type: method,
                    contentType: 'application/json',
                    data: data ? JSON.stringify(data) : undefined,
                    headers: headers,
                    xhrFields: { withCredentials: true },
                    crossDomain: true
                };
                try {
                    const response = yield $.ajax(ajaxOptions);
                    return { response: response, jqXhr: null };
                }
                catch (xhr) {
                    //console.warn('⚠️ Request failed:', method, url, '- Status:', xhr.status);
                    // ✅ Handle 401 - token expired
                    if (useJwt && xhr.status === 401 && !this._isRefreshing) {
                        const refreshed = yield this.refreshToken();
                        if (refreshed) {
                            const newToken = yield this.getAccessToken();
                            if (newToken) {
                                ajaxOptions.headers['Authorization'] = 'Bearer ' + newToken;
                                const retryResponse = yield $.ajax(ajaxOptions);
                                return { response: retryResponse, jqXhr: null };
                            }
                        }
                        else {
                            //console.error('❌ Token refresh failed, redirecting to login...');
                            window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                        }
                    }
                    throw xhr;
                }
            });
        },
        // ✅ Convenience methods
        getJson: function (url, useJwt = false) {
            return this.request('GET', url, null, useJwt).then(r => r.response);
        },
        postJson: function (url, payload, useJwt = false) {
            return this.request('POST', url, payload, useJwt).then(r => r.response);
        },
        putJson: function (url, payload, useJwt = false) {
            return this.request('PUT', url, payload, useJwt).then(r => r.response);
        },
        deleteJson: function (url, useJwt = false) {
            return this.request('DELETE', url, null, useJwt).then(r => r.response);
        },
        // ✅ Backward-compatible callback methods
        get: function (url, onSuccess, onError, useJwt = false) {
            this.getJson(url, useJwt)
                .then(data => { if (onSuccess)
                onSuccess(data); })
                .catch(xhr => { if (onError)
                onError(xhr); });
        },
        post: function (url, data, onSuccess, onError, useJwt = false) {
            this.postJson(url, data, useJwt)
                .then(resp => { if (onSuccess)
                onSuccess(resp); })
                .catch(xhr => { if (onError)
                onError(xhr); });
        },
        delete: function (url, onSuccessOrUseJwt, onError, useJwt) {
            if (typeof onSuccessOrUseJwt === 'function') {
                const onSuccess = onSuccessOrUseJwt;
                const cbUseJwt = !!useJwt;
                this.deleteJson(url, cbUseJwt)
                    .then(resp => { if (onSuccess)
                    onSuccess(resp); })
                    .catch(xhr => { if (onError)
                    onError(xhr); });
                return;
            }
            const promiseUseJwt = !!onSuccessOrUseJwt;
            return this.deleteJson(url, promiseUseJwt);
        }
    };
    window.ApiClient = ApiClient;
})();
//# sourceMappingURL=ApiClient.js.map