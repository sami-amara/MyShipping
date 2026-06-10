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
// Approve.js: Handles shipment approval workflow (Reviewer/Operation roles)
// Wraps logic to avoid global scope pollution
(function () {
    'use strict';
    $(document).ready(function () {
        var _a, _b;
        const formSelector = '#createShipmentForm';
        const form = document.querySelector(formSelector);
        if (!form)
            return;
        // Prevent double initialization
        if (form._approveBound)
            return;
        form._approveBound = true;
        // ═══════════════════════════════════════════════════════════════
        // 0. VALIDATION SETUP
        // ═══════════════════════════════════════════════════════════════
        const validationRules = window.ShipmentValidationRules;
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
                    showAlert.Error('Error', msg || 'Failed to update shipment.');
                }
                else {
                    alert(msg || 'Failed to update shipment.');
                }
            };
            // Use ShipmentService.update
            if (window.ShipmentService && typeof ShipmentService.approveShipment === 'function') {
                ShipmentService.approveShipment(function () {
                    cleanup();
                    window.location.href = '/admin/Shipments/List';
                }, function (err) {
                    var _a, _b;
                    cleanup();
                    const message = err && (err.message || ((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.message) || ((_b = err.responseJSON) === null || _b === void 0 ? void 0 : _b.Message)) || 'Failed to update shipment.';
                    notifyError(message);
                });
                return;
            }
            // Fallback to direct PUT
            const payload = (window.ShipmentService && typeof ShipmentService.getShipmentFormData === 'function')
                ? ShipmentService.getShipmentFormData()
                : {};
            if (id)
                payload.Id = id;
            fetch('/api/Shipments/' + encodeURIComponent(id), {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify(payload)
            })
                .then((resp) => __awaiter(this, void 0, void 0, function* () {
                var _a, _b;
                cleanup();
                const body = yield resp.json().catch(() => null);
                if (resp.ok) {
                    const redirectId = (body && (((_a = body.Data) === null || _a === void 0 ? void 0 : _a.Id) || body.Id || ((_b = body.data) === null || _b === void 0 ? void 0 : _b.Id))) || id;
                    if (redirectId)
                        window.location.href = '/admin/Shipments/Approve/' + encodeURIComponent(redirectId);
                }
                else {
                    const errMsg = (body && (body.Message || body.message || body.title || body.Title)) || ('Server responded ' + resp.status);
                    notifyError(errMsg);
                }
            }))
                .catch(err => {
                cleanup();
                notifyError((err === null || err === void 0 ? void 0 : err.message) || 'Network error');
            });
        });
        // ═══════════════════════════════════════════════════════════════
        // 3. APPROVE BUTTON HANDLER
        // ═══════════════════════════════════════════════════════════════
        const shipmentId = (_b = form.querySelector('[name="Id"]')) === null || _b === void 0 ? void 0 : _b.value;
        const btnApprove = document.getElementById('btnApprove');
        if (btnApprove && shipmentId) {
            btnApprove.addEventListener('click', function () {
                if (window.ShipmentService && typeof ShipmentService.approveShipment === 'function') {
                    // The current ShipmentService.approveShipment implementation uses the callback style
                    // approveShipment(onSuccess, onError). Call it accordingly so redirect and validation mapping work.
                    ShipmentService.approveShipment(function () {
                        // success -> redirect to list with approved query
                        try {
                            window.location.href = '/admin/Shipments/List?approved=1&approvedId=' + encodeURIComponent(shipmentId);
                        }
                        catch (_a) { }
                    }, function (err) {
                        var _a, _b;
                        // error -> map or show
                        const message = err && (err.message || ((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.message) || ((_b = err.responseJSON) === null || _b === void 0 ? void 0 : _b.Message)) || 'Failed to approve shipment.';
                        if (window.showAlert && typeof showAlert.Error === 'function')
                            showAlert.Error('Error', message);
                        else
                            alert(message);
                    });
                    /*
                    // Previous/alternate call (modern signature) kept commented for reference:
                    // ShipmentService.approveShipment(shipmentId, {
                    //     button: this,
                    //     redirectUrl: '/admin/Shipments/List?approved=1&approvedId=' + encodeURIComponent(shipmentId)
                    // });
                    */
                }
                else {
                    console.error('ShipmentService.approveShipment not available');
                    if (window.showAlert && typeof showAlert.Error === 'function') {
                        showAlert.Error('Error', 'Approval service not available');
                    }
                }
            });
        }
        // ═══════════════════════════════════════════════════════════════
        // 4. MARK READY BUTTON HANDLER
        // ═══════════════════════════════════════════════════════════════
        const btnMarkReady = document.getElementById('btnMarkReady');
        if (btnMarkReady && shipmentId) {
            btnMarkReady.addEventListener('click', function () {
                const carrierId = null;
                if (window.ShipmentService && typeof ShipmentService.markReadyShipment === 'function') {
                    ShipmentService.markReadyShipment(shipmentId, {
                        button: this,
                        carrierId: carrierId,
                        redirectUrl: '/admin/Shipments/List?ready=1&readyId=' + encodeURIComponent(shipmentId)
                    });
                }
                else {
                    console.error('ShipmentService.markReadyShipment not available');
                    if (window.showAlert && typeof showAlert.Error === 'function') {
                        showAlert.Error('Error', 'Mark ready service not available');
                    }
                }
            });
        }
    });
})();
//# sourceMappingURL=Approve.js.map