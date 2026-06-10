/* eslint-disable no-undef */
// Approve.js: Handles shipment approval workflow (Reviewer/Operation roles)
// Wraps logic to avoid global scope pollution
(function () {
    'use strict';
    $(document).ready(function () {
        var _a, _b;
        const alerts = window.AppResourceAlerts || {};
        // Support multiple page forms: create, makeReady, shipped
        const form = document.querySelector('#createShipmentForm') || document.querySelector('#makeReadyForm') || document.querySelector('#shippedForm') || null;
        try {
            console.debug('ChangeStatus.js initializing. form=', form ? form.getAttribute('id') : null);
        }
        catch (e) { }
        // Prevent double initialization when a form is present
        if (form) {
            if (form._approveBound)
                return;
            form._approveBound = true;
        }
        // ═══════════════════════════════════════════════════════════════
        // 0. VALIDATION SETUP
        // ═══════════════════════════════════════════════════════════════
        const validationRules = window.ShipmentValidationRules;
        const localizedValidation = window.ShipmentValidationTexts || {};
        if (validationRules) {
            Object.keys(localizedValidation).forEach(function (fieldName) {
                if (validationRules[fieldName]) {
                    validationRules[fieldName].requiredMessage = localizedValidation[fieldName];
                }
            });
        }
        // formSelector is used by FormValidator; derive from the bound form if present
        const formSelector = form ? ('#' + (form.getAttribute('id') || 'createShipmentForm')) : '#createShipmentForm';
        if (window.FormValidator && validationRules) {
            try {
                FormValidator.attach(formSelector, validationRules);
                FormValidator.enableStepValidation(formSelector, validationRules);
            }
            catch (e) {
                console.error('Approve.js: FormValidator setup failed', e);
            }
        }
        else {
            console.error('Approve.js: FormValidator or validationRules not available!');
        }
        // ═══════════════════════════════════════════════════════════════
        // 1. STATUS PILLS UPDATE
        // ═══════════════════════════════════════════════════════════════
        const select = document.querySelector('#shipmentStatusSelect');
        const label = document.querySelector('#selectedStatusLabel');
        const pills = document.querySelectorAll('.nav-pills [data-status-value]');
        if (select) {
            const updatePills = (val) => {
                const num = parseInt(val, 10);
                pills.forEach(p => {
                    const v = parseInt(p.getAttribute('data-status-value') || '0', 10);
                    let stateCls = 'btn-sm me-2';
                    if (v === num) {
                        stateCls = 'btn-sm btn-gradient-info me-2';
                    }
                    else if (v < num) {
                        stateCls = 'btn-sm btn-gradient-success me-2';
                    }
                    else {
                        stateCls = 'btn-sm btn-outline-secondary me-2';
                    }
                    p.className = 'btn ' + stateCls;
                });
            };
            select.addEventListener('change', function () {
                const txt = select.options[select.selectedIndex].text;
                const val = select.value;
                if (label)
                    label.textContent = txt;
                updatePills(val);
            });
            try {
                const initVal = select.value;
                if (label)
                    label.textContent = ((_a = select.options[select.selectedIndex]) === null || _a === void 0 ? void 0 : _a.text) || '';
                updatePills(initVal);
            }
            catch (e) { /* ignore */ }
        }
        // ═══════════════════════════════════════════════════════════════
        // 2. FORM SUBMISSION (Update Shipment)
        // ═══════════════════════════════════════════════════════════════
        if (form) {
            form.addEventListener('submit', function (e) {
                var _a;
                e.preventDefault();
                if (form._submitting)
                    return;
                form._submitting = true;
                const btn = document.querySelector('#btnSave');
                if (btn)
                    btn.disabled = true;
                const id = (_a = form.querySelector('[name="Id"]')) === null || _a === void 0 ? void 0 : _a.value;
                const cleanup = () => {
                    form._submitting = false;
                    if (btn)
                        btn.disabled = false;
                };
                const notifyError = (msg) => {
                    if (window.showAlert && typeof showAlert.Error === 'function') {
                        showAlert.Error((alerts.deleteFailedTitle || 'Error'), msg || (alerts.updateFailed || 'Failed to update shipment.'));
                    }
                    else {
                        alert(msg || (alerts.updateFailed || 'Failed to update shipment.'));
                    }
                };
                // Use ShipmentService.changeStatus (promise-based) if available
                if (window.ShipmentService && typeof ShipmentService.changeStatus === 'function') {
                    try {
                        const payload = (window.ShipmentService && typeof ShipmentService.getShipmentFormData === 'function') ? ShipmentService.getShipmentFormData() : {};
                        if (id)
                            payload.Id = id;
                        // call changeStatus which returns a Promise
                        ShipmentService.changeStatus(id, payload, { redirect: true, redirectUrl: '/admin/Shipments/List' })
                            .then(() => {
                            cleanup();
                        })
                            .catch(err => {
                            var _a, _b;
                            cleanup();
                            const message = err && (err.message || ((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.message) || ((_b = err.responseJSON) === null || _b === void 0 ? void 0 : _b.Message)) || (alerts.updateFailed || 'Failed to update shipment.');
                            notifyError(message);
                        });
                        return;
                    }
                    catch (e) {
                        cleanup();
                        notifyError(alerts.changeStatusCallFailed || 'Failed to call changeStatus');
                        return;
                    }
                }
                // NOTE: legacy duplicate submit fallbacks are intentionally commented for testing cleanup.
                // They can be removed permanently after verification.
                /*
                // Fallback: call ShipmentApiClient.changeStatus directly (no ShipmentService available)
                if (window.ShipmentApiClient && typeof ShipmentApiClient.changeStatus === 'function') {
                    try {
                        const payload = (window.ShipmentService && typeof ShipmentService.getShipmentFormData === 'function') ? ShipmentService.getShipmentFormData() : {};
                        if (id) payload.Id = id;
                        ShipmentApiClient.changeStatus(id, payload)
                            .then(() => { cleanup(); })
                            .catch(err => { cleanup(); notifyError(err?.message || (alerts.changeStatusFailed || 'Failed to change status')); });
                        return;
                    } catch (e) {
                        cleanup();
                        notifyError(alerts.changeStatusApiCallFailed || 'Failed to call ShipmentApiClient.changeStatus');
                        return;
                    }
                }

                // Fallback to direct PUT
                const payload = (window.ShipmentService && typeof ShipmentService.getShipmentFormData === 'function')
                    ? ShipmentService.getShipmentFormData()
                    : {};

                if (id) payload.Id = id;

                fetch('/api/Shipments/' + encodeURIComponent(id), {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify(payload)
            })
                    .then(async resp => {
                        cleanup();
                        const body = await resp.json().catch(() => null);
                        if (resp.ok) {
                            const redirectId = (body && (body.Data?.Id || body.Id || body.data?.Id)) || id;
                            if (redirectId) window.location.href = '/admin/Shipments/ChangeStatus/' + encodeURIComponent(redirectId);
                        } else {
                            const errMsg = (body && (body.Message || body.message || body.title || body.Title)) || ('Server responded ' + resp.status);
                            notifyError(errMsg);
                        }
                    })
                    .catch(err => {
                        cleanup();
                        notifyError(err?.message || (alerts.networkError || 'Network error'));
                    });
                */
                cleanup();
                notifyError(alerts.changeStatusApiCallFailed || 'Failed to call ShipmentApiClient.changeStatus');
                return;
            });
        }
        // ═══════════════════════════════════════════════════════════════
        // 3. APPROVE BUTTON HANDLER
        // ═══════════════════════════════════════════════════════════════
        const shipmentId = (_b = document.querySelector('[name="Id"]')) === null || _b === void 0 ? void 0 : _b.value;
        const btnApprove = document.getElementById('btnApprove');
        // Approve action function (can be called from direct handlers or delegated clicks)
        const approveAction = function (btnElem) {
            var _a, _b, _c, _d, _e;
            const id = ((_a = document.querySelector('[name="Id"]')) === null || _a === void 0 ? void 0 : _a.value) || ((_b = document.querySelector('#Id')) === null || _b === void 0 ? void 0 : _b.value) || shipmentId || null;
            if (!id) {
                console.error('Approve: no shipment id found');
                if (window.showAlert && typeof showAlert.Error === 'function')
                    showAlert.Error((alerts.deleteFailedTitle || 'Error'), (alerts.missingShipmentId || 'Missing shipment id'));
                return Promise.reject(new Error(alerts.missingShipmentId || 'Missing shipment id'));
            }
            try {
                // determine approved numeric state
                const approvedState = (typeof Business !== 'undefined' && ((_e = (_d = (_c = Business.Services) === null || _c === void 0 ? void 0 : _c.Shipment) === null || _d === void 0 ? void 0 : _d.ShipmentStatusEnum) === null || _e === void 0 ? void 0 : _e.Approved))
                    ? Business.Services.Shipment.ShipmentStatusEnum.Approved
                    : 3;
                if (window.ShipmentService && typeof ShipmentService.adminActions === 'function') {
                    return ShipmentService.adminActions(id, { action: 'changeStatus', targetState: approvedState, button: btnElem || null, redirect: true, redirectUrl: '/admin/Shipments/List?approved=1&approvedId=' + encodeURIComponent(id) });
                }
                return Promise.reject(new Error(alerts.approveServiceUnavailable || 'ShipmentService.adminActions not available'));
            }
            catch (e) {
                return Promise.reject(e);
            }
        };
        if (btnApprove) {
            console.debug('ChangeStatus: binding approve button', btnApprove);
            btnApprove.addEventListener('click', function (evt) {
                evt.preventDefault();
                approveAction(this).catch(err => {
                    var _a, _b;
                    const message = err && (err.message || ((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.message) || ((_b = err.responseJSON) === null || _b === void 0 ? void 0 : _b.Message)) || (alerts.approveFailed || 'Failed to approve shipment.');
                    if (window.showAlert && typeof showAlert.Error === 'function')
                        showAlert.Error((alerts.deleteFailedTitle || 'Error'), message);
                    else
                        alert(message);
                });
            });
        }
        // Populate carrier dropdown if present on the page
        const carrierSelectEl = document.querySelector('#deliveryManId');
        if (carrierSelectEl) {
            const callFill = (sel) => {
                const currentCarrier = sel.getAttribute('data-current-carrier') || null;
                if (window.ManagePageControlls && typeof ManagePageControlls.fillCarrierDropdown === 'function') {
                    try {
                        ManagePageControlls.fillCarrierDropdown('#deliveryManId', currentCarrier);
                    }
                    catch (e) {
                        console.warn('fillCarrierDropdown failed', e);
                    }
                    return;
                }
                // Fallback: if ManagePageControlls isn't available, try CarriersService directly
                if (window.CarriersService && typeof CarriersService.GetAll === 'function') {
                    try {
                        CarriersService.GetAll(function (response) {
                            var _a, _b, _c, _d, _e, _f;
                            // normalize list shapes similar to ManagePageControlls.extractList
                            let list = null;
                            if (Array.isArray(response))
                                list = response;
                            else if (response && Array.isArray(response.data))
                                list = response.data;
                            else if (response && Array.isArray(response.Data))
                                list = response.Data;
                            else if (response && typeof response === 'object') {
                                for (const k in response) {
                                    if (Object.prototype.hasOwnProperty.call(response, k) && Array.isArray(response[k])) {
                                        list = response[k];
                                        break;
                                    }
                                }
                            }
                            if (!list)
                                return;
                            // populate select element
                            try {
                                sel.innerHTML = '';
                                const opt = document.createElement('option');
                                opt.value = '';
                                opt.textContent = 'Select Carrier';
                                sel.appendChild(opt);
                                for (const it of list) {
                                    const val = (_c = (_b = (_a = it.id) !== null && _a !== void 0 ? _a : it.Id) !== null && _b !== void 0 ? _b : it.carrierId) !== null && _c !== void 0 ? _c : it.CarrierId;
                                    const txt = (_f = (_e = (_d = it.carrierName) !== null && _d !== void 0 ? _d : it.CarrierName) !== null && _e !== void 0 ? _e : it.name) !== null && _f !== void 0 ? _f : it.Name;
                                    if (!val)
                                        continue;
                                    const o = document.createElement('option');
                                    o.value = val;
                                    o.textContent = txt || String(val);
                                    sel.appendChild(o);
                                }
                                if (currentCarrier)
                                    sel.value = currentCarrier;
                            }
                            catch (e) {
                                console.warn('Failed to populate carriers fallback', e);
                            }
                        }, function (err) {
                            console.warn('CarriersService.GetAll failed', err);
                        });
                    }
                    catch (e) {
                        console.warn('CarriersService.GetAll threw', e);
                    }
                    return;
                }
                console.debug('ManagePageControlls.fillCarrierDropdown and CarriersService.GetAll not available');
            };
            // Initial call (may run before inline script sets data-current-carrier)
            callFill(carrierSelectEl);
            // If the attribute may be set later by inline scripts, observe changes and re-run once
            try {
                const observer = new MutationObserver((mutations, obs) => {
                    for (const m of mutations) {
                        if (m.type === 'attributes' && m.attributeName === 'data-current-carrier') {
                            callFill(carrierSelectEl);
                            obs.disconnect(); // only need to run once
                            break;
                        }
                    }
                });
                observer.observe(carrierSelectEl, { attributes: true });
            }
            catch (e) {
                // fallback: schedule a delayed retry in case attribute set after scripts
                setTimeout(() => callFill(carrierSelectEl), 150);
            }
        }
        // ═══════════════════════════════════════════════════════════════
        // 4. MARK READY BUTTON HANDLER
        // ═══════════════════════════════════════════════════════════════
        // const btnMarkReady = document.getElementById('btnMarkReady');
        // const btnMakeReady = document.getElementById('btnMakeReady'); // unused (kept commented for cleanup testing)
        // ═══════════════════════════════════════════════════════════════
        // 5. MARK SHIPPED BUTTON HANDLER
        // ═══════════════════════════════════════════════════════════════
        const btnMarkShipped = document.getElementById('btnMarkShipped');
        if (btnMarkShipped) {
            console.debug('ChangeStatus: binding mark shipped button', btnMarkShipped);
            btnMarkShipped.addEventListener('click', function () {
                var _a, _b, _c, _d;
                const id = ((_a = document.querySelector('[name="Id"]')) === null || _a === void 0 ? void 0 : _a.value) || shipmentId || null;
                console.debug('ChangeStatus: mark shipped clicked for shipmentId=', id);
                if (!id) {
                    if (window.showAlert && typeof showAlert.Error === 'function')
                        showAlert.Error((alerts.deleteFailedTitle || 'Error'), (alerts.missingShipmentId || 'Missing shipment id'));
                    else
                        alert(alerts.missingShipmentId || 'Missing shipment id');
                    return;
                }
                if (window.ShipmentService && typeof ShipmentService.adminActions === 'function') {
                    const shippedState = (typeof Business !== 'undefined' && ((_d = (_c = (_b = Business.Services) === null || _b === void 0 ? void 0 : _b.Shipment) === null || _c === void 0 ? void 0 : _c.ShipmentStatusEnum) === null || _d === void 0 ? void 0 : _d.Shipped)) ? Business.Services.Shipment.ShipmentStatusEnum.Shipped : 5;
                    ShipmentService.adminActions(id, { action: 'changeStatus', targetState: shippedState, button: this, redirect: true, redirectUrl: '/admin/Shipments/List?shipped=1&shippedId=' + encodeURIComponent(id) })
                        .catch(err => {
                        var _a, _b;
                        const message = err && (err.message || ((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.message) || ((_b = err.responseJSON) === null || _b === void 0 ? void 0 : _b.Message)) || (alerts.shippedFailed || 'Failed to mark shipment as shipped.');
                        if (window.showAlert && typeof showAlert.Error === 'function')
                            showAlert.Error((alerts.deleteFailedTitle || 'Error'), message);
                        else
                            alert(message);
                    });
                    return;
                }
            });
        }
    });
})();
//# sourceMappingURL=ChangeStatus.js.map