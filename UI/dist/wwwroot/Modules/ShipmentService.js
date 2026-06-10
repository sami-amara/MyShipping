/* eslint-disable no-undef */
// ShipmentService: centralized create/update form flows, error mapping and listing helpers.
// - Use ShipmentService.init({ autoWireSubmit: true }) on Create/Edit pages when desired.
// - Depends on: jQuery ($), ShipmentApiClient (create/update/getPaged/getAll/delete), ValidationErrorMapper or mapServerErrors fallback,
//   optional FormValidator.showStep and ScrollHelper.scrollToCenter, AppHelper.showToast.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try {
            step(generator.next(value));
        }
        catch (e) {
            reject(e);
        } }
        function rejected(value) { try {
            step(generator["throw"](value));
        }
        catch (e) {
            reject(e);
        } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
// Wrap to avoid leaking internal helpers; expose `window.ShipmentService` at the end
const ShipmentService = (function () {
    'use strict';
    const svc = {
        settings: {
            formSelector: '#createShipmentForm',
            submitButtonSelector: 'input[name="btnPost"], button[name="btnPost"], .btn-submit-final',
            tableSelector: '#shipments-table',
            tableBodySelector: '#shipments-table-body',
            tableCurrency: 'USD',
            autoWireSubmit: false,
            page: 1,
            pageSize: 10,
            sortBy: 'CreatedDate',
            sortDir: 'desc'
        },
        // Admin convenience: perform an admin action by name or numeric target state
        // options: { action: 'approve'|'ready'|'shipped', targetState: number, button, redirect, redirectUrl }
        adminActions: function (id, actionOrOptions = {}) {
            var _a, _b;
            if (!id)
                return Promise.reject(new Error('Shipment id required'));
            let opts = {};
            if (typeof actionOrOptions === 'string' || typeof actionOrOptions === 'number') {
                opts = { action: actionOrOptions };
            }
            else {
                opts = Object.assign({}, actionOrOptions || {});
            }
            const btn = opts.button || null;
            if (btn) {
                try {
                    btn.disabled = true;
                }
                catch (_c) { }
            }
            const restoreBtn = () => {
                if (btn)
                    try {
                        btn.disabled = false;
                    }
                    catch (_a) { }
            };
            // build payload from form when available (build early so payload.CurrentState may be used)
            const form = document.querySelector(this.settings.formSelector);
            let payload = {};
            try {
                if (form && typeof this.getShipmentFormData === 'function')
                    payload = this.getShipmentFormData();
            }
            catch (_d) {
                payload = {};
            }
            payload.Id = id;
            // include any explicit carrierId passed (Make Ready flow)
            if (opts.carrierId) {
                try {
                    payload.CarrierId = opts.carrierId;
                }
                catch (_e) {
                    payload.CarrierId = opts.carrierId;
                }
            }
            // when marking shipped, set delivery date if not provided
            if (String(opts.action).toLowerCase() === 'shipped' || (opts.targetState == 5)) {
                if (!payload.DelivryDate && !payload.DeliveryDate) {
                    try {
                        payload.DelivryDate = new Date().toISOString();
                    }
                    catch ( /* ignore */_f) { /* ignore */ }
                }
            }
            // determine target state
            let targetState = typeof opts.targetState !== 'undefined' && opts.targetState !== null ? opts.targetState : undefined;
            if (typeof targetState === 'undefined' && opts.action) {
                const act = String(opts.action).toLowerCase();
                // support generic 'changestatus' action which uses payload.CurrentState or opts.targetState
                if (act === 'changestatus' || act === 'changeStatus'.toLowerCase()) {
                    if (typeof payload.CurrentState !== 'undefined' && payload.CurrentState !== null)
                        targetState = payload.CurrentState;
                    else if (typeof opts.targetState !== 'undefined' && opts.targetState !== null)
                        targetState = opts.targetState;
                }
                if (typeof targetState === 'undefined' && typeof Business !== 'undefined' && ((_b = (_a = Business.Services) === null || _a === void 0 ? void 0 : _a.Shipment) === null || _b === void 0 ? void 0 : _b.ShipmentStatusEnum)) {
                    const enumObj = Business.Services.Shipment.ShipmentStatusEnum;
                    if (act === 'approve' && typeof enumObj.Approved !== 'undefined')
                        targetState = enumObj.Approved;
                    if ((act === 'ready' || act === 'readyforshipping') && typeof enumObj.ReadyForShipping !== 'undefined')
                        targetState = enumObj.ReadyForShipping;
                    if ((act === 'shipped' || act === 'ship') && typeof enumObj.Shipped !== 'undefined')
                        targetState = enumObj.Shipped;
                }
                if (typeof targetState === 'undefined' && typeof Business === 'undefined') {
                    // fallback numeric mapping (best-effort)
                    if (act === 'approve')
                        targetState = 3;
                    if (act === 'ready' || act === 'readyforshipping')
                        targetState = 4;
                    if (act === 'shipped' || act === 'ship')
                        targetState = 5;
                }
            }
            if (typeof targetState === 'undefined' || targetState === null) {
                restoreBtn();
                return Promise.reject(new Error('Target state not specified'));
            }
            payload.CurrentState = targetState;
            // include any explicit carrierId passed (Make Ready flow)
            if (opts.carrierId) {
                try {
                    payload.CarrierId = opts.carrierId;
                }
                catch (_g) {
                    payload.CarrierId = opts.carrierId;
                }
            }
            // when marking shipped, set delivery date if not provided
            if (String(opts.action).toLowerCase() === 'shipped') {
                if (!payload.DelivryDate && !payload.DeliveryDate) {
                    try {
                        payload.DelivryDate = new Date().toISOString();
                    }
                    catch ( /* ignore */_h) { /* ignore */ }
                }
            }
            // Debug: log payload and endpoint for troubleshooting
            try {
                console.debug('adminActions: calling changeStatus with payload', payload);
            }
            catch (_j) { }
            return this.changeStatus(id, payload, opts)
                .then(res => { restoreBtn(); return res; })
                .catch(err => { restoreBtn(); throw err; });
        },
        // Minimal-payload status transition (no form data) - for Deleted=0, ReadyForShipping=4, Shipped=5, Delivered=6, Cancelled=7, Returned=8
        // NOTE: This function uses ShipmentApiClient.updateStatus() which calls the minimal /api/Shipments/{id}/UpdateStatus endpoint
        // This endpoint should NOT validate full shipment DTO fields (Width, Height, Length, Weight, UserSender, UserReceiver, etc.)
        // It only updates the status field - used for terminal states like Shipped, Delivered, Cancelled, Returned, Deleted
        adminActionsMinimal: function (id, options) {
            if (!id)
                return Promise.reject(new Error('Shipment id required'));
            const opts = Object.assign({}, options || {});
            const btn = opts.button || null;
            if (btn) {
                try {
                    btn.disabled = true;
                }
                catch (_a) { }
            }
            const restoreBtn = function () {
                if (btn)
                    try {
                        btn.disabled = false;
                    }
                    catch (_a) { }
            };
            if (typeof opts.targetState === 'undefined' || opts.targetState === null) {
                restoreBtn();
                return Promise.reject(new Error('targetState is required for adminActionsMinimal'));
            }
            // Minimal states that only update status without requiring full shipment DTO
            const minimalStates = [0, 5, 6, 7, 8]; // Deleted=0, Shipped=5, Delivered=6, Cancelled=7, Returned=8
            if (minimalStates.indexOf(opts.targetState) !== -1) {
                if (!window.ShipmentApiClient || typeof ShipmentApiClient.updateStatus !== 'function') {
                    restoreBtn();
                    return Promise.reject(new Error('ShipmentApiClient.updateStatus not available'));
                }
                // IMPORTANT: This calls the MINIMAL endpoint - /api/Shipments/{id}/UpdateStatus?newState=N
                // This should NOT require full DTO validation (Width, Height, Length, Weight, UserSender, UserReceiver, ShippingTypeId, ShipingPackgingId, PackageValue)
                return ShipmentApiClient.updateStatus(id, opts.targetState)
                    .then(function (res) {
                    restoreBtn();
                    if (opts.redirect && opts.redirectUrl) {
                        try {
                            window.location.href = opts.redirectUrl;
                        }
                        catch (_a) { }
                    }
                    return res;
                })
                    .catch(function (err) { restoreBtn(); throw err; });
            }
            // Fallback for any other state
            try {
                console.debug('adminActionsMinimal: unhandled targetState', opts.targetState);
            }
            catch (_b) { }
            restoreBtn();
            return Promise.reject(new Error('targetState ' + opts.targetState + ' is not handled by adminActionsMinimal'));
        },
        // Promise-based changeStatus wrapper (preferred)
        changeStatus: function (id, payloadOrState, options = {}) {
            if (!id)
                return Promise.reject(new Error('Shipment id required'));
            const opts = Object.assign({ button: null, redirect: false, redirectUrl: null }, options || {});
            const btn = opts.button || null;
            if (btn) {
                try {
                    btn.disabled = true;
                }
                catch (_a) { }
            }
            const restoreBtn = () => {
                if (btn)
                    try {
                        btn.disabled = false;
                    }
                    catch (_a) { }
            };
            const form = document.querySelector(this.settings.formSelector);
            // If ShipmentApiClient.changeStatus available prefer it
            const callClient = () => {
                if (window.ShipmentApiClient && typeof ShipmentApiClient.changeStatus === 'function') {
                    return ShipmentApiClient.changeStatus(id, payloadOrState);
                }
                return Promise.reject(new Error('ShipmentApiClient.changeStatus not available'));
            };
            return Promise.resolve()
                .then(() => callClient())
                .then(resp => {
                restoreBtn();
                if (opts.redirect && opts.redirectUrl) {
                    try {
                        window.location.href = opts.redirectUrl;
                    }
                    catch (_a) { }
                }
                return resp;
            })
                .catch(err => {
                var _a, _b;
                restoreBtn();
                const serverErrors = err && (((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.errors) || ((_b = err.response) === null || _b === void 0 ? void 0 : _b.errors) || err.errors || err.responseJSON || err.response || err);
                if (serverErrors && form && this.tryMapErrorsAndNavigate(serverErrors, form)) {
                    return Promise.reject({ mapped: true, errors: serverErrors, response: err });
                }
                return Promise.reject(err);
            });
        },
        // Initialize: wire the submit button/form when autoWireSubmit=true
        init: function (options = {}) {
            Object.assign(this.settings, options || {});
            if (!this.settings.autoWireSubmit)
                return;
            const btn = document.querySelector(this.settings.submitButtonSelector);
            if (btn) {
                try {
                    btn.removeEventListener('click', this._boundSubmitHandler);
                }
                catch ( /* ignore */_a) { /* ignore */ }
                const self = this;
                this._boundSubmitHandler = function (e) {
                    e.preventDefault();
                    try {
                        self.submitShipment();
                    }
                    catch (ex) {
                        console.error('ShipmentService._boundSubmitHandler failed', ex);
                    }
                };
                btn.addEventListener('click', this._boundSubmitHandler);
            }
            const form = document.querySelector(this.settings.formSelector);
            if (form) {
                try {
                    form.removeEventListener('submit', this._boundFormSubmitHandler);
                }
                catch ( /* ignore */_b) { /* ignore */ }
                const self = this;
                this._boundFormSubmitHandler = function (e) {
                    e.preventDefault();
                    try {
                        self.submitShipment();
                    }
                    catch (ex) {
                        console.error('ShipmentService._boundFormSubmitHandler failed', ex);
                    }
                };
                form.addEventListener('submit', this._boundFormSubmitHandler);
            }
        },
        ////// Map many server error shapes to fields / jQuery validate
        mapServerErrors: function (errorPayload, form) {
            if (!errorPayload || !form)
                return false;
            // If ClientHelpers.mapServerErrors is present prefer it — it contains the original fallback implementation
            // Prefer ClientHelpers which centralizes mapping and integrates with ValidationErrorMapper
            try {
                if (window.ClientHelpers && typeof ClientHelpers.mapServerErrors === 'function') {
                    return !!ClientHelpers.mapServerErrors(errorPayload, form);
                }
            }
            catch (ex) {
                console.warn('ClientHelpers.mapServerErrors threw', ex);
            }
            // Fallback: direct ValidationErrorMapper if available
            try {
                if (window.ValidationErrorMapper && typeof ValidationErrorMapper.mapErrorsToFields === 'function') {
                    return !!ValidationErrorMapper.mapErrorsToFields(errorPayload, form);
                }
            }
            catch (ex) {
                console.warn('ValidationErrorMapper.mapErrorsToFields threw', ex);
            }
            return false;
        },
        // Navigate to first invalid input/fieldset step if possible
        navigateToFirstFieldError: function (form) {
            try {
                const first = form.querySelector('.field-error') ||
                    form.querySelector('.is-invalid') ||
                    form.querySelector('[aria-invalid="true"]') ||
                    (form.querySelector('.field-error-message') && form.querySelector('.field-error-message').previousElementSibling) ||
                    null;
                if (!first)
                    return null;
                const fieldset = first.closest('fieldset') || first.closest('.step') || first.closest('.form-step');
                const stepIndexAttr = fieldset ? (fieldset.getAttribute('data-step') || fieldset.dataset.step) : null;
                const stepIndex = stepIndexAttr !== null ? parseInt(stepIndexAttr, 10) : null;
                if (typeof (FormValidator === null || FormValidator === void 0 ? void 0 : FormValidator.showStep) === 'function' && stepIndex !== null && !isNaN(stepIndex)) {
                    FormValidator.showStep(stepIndex);
                }
                if (window.ScrollHelper && typeof ScrollHelper.scrollToCenter === 'function') {
                    ScrollHelper.scrollToCenter(first);
                }
                else {
                    first.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
                try {
                    first.focus();
                }
                catch ( /* ignore */_a) { /* ignore */ }
                return first;
            }
            catch (ex) {
                console.warn('navigateToFirstFieldError failed', ex);
                return null;
            }
        },
        //// Try to map any server payload to form and navigate
        tryMapErrorsAndNavigate: function (payload, form) {
            if (!payload || !form)
                return false;
            try {
                // Delegate mapping to ValidationErrorMapper (preferred) or ClientHelpers (fallback).
                let mapped = false;
                if (window.ValidationErrorMapper && typeof ValidationErrorMapper.mapErrorsToFields === 'function') {
                    try {
                        mapped = !!ValidationErrorMapper.mapErrorsToFields(payload, form);
                    }
                    catch (ex) {
                        console.warn('ValidationErrorMapper.mapErrorsToFields threw', ex);
                        mapped = false;
                    }
                }
                if (!mapped && window.ClientHelpers && typeof ClientHelpers.mapServerErrors === 'function') {
                    try {
                        mapped = !!ClientHelpers.mapServerErrors(payload, form);
                    }
                    catch (ex) {
                        console.warn('ClientHelpers.mapServerErrors threw', ex);
                        mapped = false;
                    }
                }
                // If mapping succeeded or inline markers exist, reveal first error
                const foundInline = !!(form.querySelector('.field-error') || form.querySelector('.field-error-message') || form.querySelector('.is-invalid') || form.querySelector('[aria-invalid="true"]'));
                if (mapped || foundInline) {
                    this.navigateToFirstFieldError(form);
                    return true;
                }
            }
            catch (ex) {
                console.warn('tryMapErrorsAndNavigate failed', ex);
            }
            return false;
        },
        // Read form and produce DTO
        getShipmentFormData: function () {
            const form = $(this.settings.formSelector);
            const senderCountryId = form.find('[name="SenderCountry"]').val() || null;
            const senderCityId = form.find('[name="SenderCity"]').val() || null;
            const receiverCountryId = form.find('[name="ReceiverCountry"]').val() || null;
            const receiverCityId = form.find('[name="ReceiverCity"]').val() || null;
            const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';
            const parseNumber = (s) => {
                const v = parseFloat(s);
                return isNaN(v) ? 0 : v;
            };
            const toIso = (raw) => {
                if (!raw)
                    return new Date().toISOString();
                const d = new Date(raw);
                if (!isNaN(d.getTime()))
                    return d.toISOString();
                return new Date().toISOString();
            };
            // Read hidden fields for IDs and other properties
            const senderIdField = form.find('[name="SenderId"]').val();
            const receiverIdField = form.find('[name="ReceiverId"]').val();
            const trackingNumberField = form.find('[name="TrackingNumber"]').val();
            const shippingRateField = form.find('[name="ShippingRate"]').val();
            const paymentMethodTokenField = form.find('[name="PaymentMethodToken"]').val();
            const isValidStripeToken = (val) => typeof val === 'string' && val.toLowerCase().startsWith('pm_');
            const resolvedPaymentToken = isValidStripeToken(paymentMethodTokenField)
                ? paymentMethodTokenField
                : null;
            const paymentMethodRaw = form.find('[name="PaymentMethodId"]').val();
            const paymentMethodResolved = (paymentMethodRaw && String(paymentMethodRaw).trim() && String(paymentMethodRaw) !== EMPTY_GUID)
                ? String(paymentMethodRaw)
                : null;
            return {
                SenderId: (senderIdField && String(senderIdField).length) ? String(senderIdField) : EMPTY_GUID,
                ReceiverId: (receiverIdField && String(receiverIdField).length) ? String(receiverIdField) : EMPTY_GUID,
                TrackingNumber: trackingNumberField || null,
                ShippingRate: shippingRateField !== undefined && shippingRateField !== null && shippingRateField !== '' ? parseNumber(shippingRateField) : null,
                ShippingTypeId: form.find('[name="ShippingTypes"]').val() || null,
                ShipingPackgingId: form.find('[name="ShippingPackging"]').val() || null,
                CarrierId: form.find('[name="DeliveryManId"]').val() || null,
                Width: parseNumber(form.find('[name="Width"]').val()),
                Height: parseNumber(form.find('[name="Height"]').val()),
                Weight: parseNumber(form.find('[name="Weight"]').val()),
                Length: parseNumber(form.find('[name="Length"]').val()),
                PackageValue: parseNumber(form.find('[name="PackageValue"]').val()),
                PaymentMethodId: paymentMethodResolved,
                PaymentMethodToken: resolvedPaymentToken,
                ShippingDate: toIso(form.find('[name="ShippingDate"]').val()),
                DelivryDate: toIso(form.find('[name="DelivryDate"]').val()),
                UserSender: {
                    UserId: EMPTY_GUID,
                    SenderName: form.find('[name="SenderName"]').val() || '',
                    Email: form.find('[name="SenderEmail"]').val() || '',
                    Phone: form.find('[name="SenderPhone"]').val() || '',
                    PostalCode: form.find('[name="SenderPostalCode"]').val() || '',
                    Contact: form.find('[name="SenderContact"]').val() || '',
                    OtherAddress: form.find('[name="SenderOtherAddress"]').val() || '',
                    IsDefault: !!form.find('#isDefaultSender').is(':checked'),
                    Address: form.find('[name="SenderAddress"]').val() || '',
                    Address2: form.find('[name="SenderAddress2"]').val() || null,
                    Address3: form.find('[name="SenderAddress3"]').val() || null,
                    CityId: senderCityId,
                    CountryId: senderCountryId
                },
                UserReceiver: {
                    UserId: EMPTY_GUID,
                    ReceiverName: form.find('[name="ReceiverName"]').val() || '',
                    Email: form.find('[name="ReceiverEmail"]').val() || '',
                    Phone: form.find('[name="ReceiverPhone"]').val() || '',
                    PostalCode: form.find('[name="ReceiverPostalCode"]').val() || '',
                    Contact: form.find('[name="ReceiverContact"]').val() || '',
                    OtherAddress: form.find('[name="ReceiverOtherAddress"]').val() || '',
                    IsDefault: !!form.find('#isDefaultReceiver').is(':checked'),
                    CityId: receiverCityId,
                    CountryId: receiverCountryId,
                    Address: form.find('[name="ReceiverAddress"]').val() || ''
                }
            };
        },
        _extractFirstMessage: function (errors) {
            var _a, _b;
            try {
                if (!errors)
                    return null;
                // Plain string (strip possible HTML)
                if (typeof errors === 'string') {
                    return errors.replace(/<\/?[^>]+(>|$)/g, '').trim() || null;
                }
                // jQuery/XHR-like wrappers
                const xhrLike = (obj) => obj && (obj.responseJSON || obj.response || obj.responseText || obj.statusText);
                if (xhrLike(errors)) {
                    const rj = errors.responseJSON || errors.response || null;
                    if (rj) {
                        const m = rj.Message || rj.message || rj.Title || rj.title;
                        if (m)
                            return Array.isArray(m) ? String(m[0]) : String(m);
                        const inner = rj.errors || rj.Errors || ((_a = rj.Data) === null || _a === void 0 ? void 0 : _a.errors) || ((_b = rj.Data) === null || _b === void 0 ? void 0 : _b.Errors);
                        const fm = this._extractFirstMessage(inner);
                        if (fm)
                            return fm;
                    }
                    const txt = errors.responseText || errors.statusText || null;
                    if (txt)
                        return ('' + txt).replace(/<\/?[^>]+(>|$)/g, '').trim() || null;
                }
                // Array shapes: ["field: msg"] or [{ Message: '...' }]
                if (Array.isArray(errors)) {
                    for (const it of errors) {
                        if (!it)
                            continue;
                        if (typeof it === 'string') {
                            const m = (it + '').match(/^([\w\.\[\]]+)\s*:\s*(.+)$/);
                            if (m)
                                return m[2].trim();
                            return it.trim();
                        }
                        if (typeof it === 'object') {
                            const msg = it.Message || it.message || it.Error || it.error;
                            if (msg)
                                return Array.isArray(msg) ? String(msg[0]) : String(msg);
                            // try nested errors
                            const nested = it.errors || it.Errors;
                            if (nested) {
                                const fm = this._extractFirstMessage(nested);
                                if (fm)
                                    return fm;
                            }
                        }
                    }
                }
                // Object shapes (ModelState: { "User.Email": ["..."] } or ApiResponse object)
                if (typeof errors === 'object') {
                    const msg = errors.Message || errors.message || errors.Title || errors.title;
                    if (msg && typeof msg === 'string' && msg.trim().length > 0)
                        return msg;
                    // Try Data.Errors / Errors properties
                    const dataErr = (errors.Data && (errors.Data.Errors || errors.Data.errors)) || errors.Errors || errors.errors;
                    if (dataErr) {
                        const fm = this._extractFirstMessage(dataErr);
                        if (fm)
                            return fm;
                    }
                    const keys = Object.keys(errors || {});
                    for (const k of keys) {
                        const v = errors[k];
                        if (!v)
                            continue;
                        if (Array.isArray(v) && v.length)
                            return String(v[0]);
                        if (typeof v === 'string' && v.trim().length)
                            return v;
                        if (typeof v === 'object') {
                            const nestedMsg = v.Message || v.message;
                            if (nestedMsg)
                                return Array.isArray(nestedMsg) ? String(nestedMsg[0]) : String(nestedMsg);
                            if (v.Errors || v.errors) {
                                const fm = this._extractFirstMessage(v.Errors || v.errors);
                                if (fm)
                                    return fm;
                            }
                        }
                    }
                }
            }
            catch (e) {
                // prevent helper from throwing
                console.warn('ShipmentService._extractFirstMessage failed', e);
            }
            return null;
        },
        // Add lightweight debug logging to submitShipment (create path)
        // Populates the Edit/Create form fields from a shipment data object
        loadShipmentData: function (data) {
            if (!data)
                return;
            const form = $(this.settings.formSelector);
            // Top-level fields
            form.find('[name="Id"]').val(data.Id || '');
            form.find('[name="SenderId"]').val(data.SenderId || '');
            form.find('[name="ReceiverId"]').val(data.ReceiverId || '');
            form.find('[name="TrackingNumber"]').val(data.TrackingNumber || '');
            form.find('[name="ShippingRate"]').val(data.ShippingRate != null ? data.ShippingRate : '');
            form.find('[name="ShippingTypes"]').val(data.ShippingTypeId || '');
            form.find('[name="ShippingPackging"]').val(data.ShipingPackgingId || '');
            form.find('[name="DeliveryManId"]').val(data.CarrierId || '');
            form.find('[name="Width"]').val(data.Width != null ? data.Width : '');
            form.find('[name="Height"]').val(data.Height != null ? data.Height : '');
            form.find('[name="Weight"]').val(data.Weight != null ? data.Weight : '');
            form.find('[name="Length"]').val(data.Length != null ? data.Length : '');
            form.find('[name="PackageValue"]').val(data.PackageValue != null ? data.PackageValue : '');
            form.find('[name="PaymentMethodId"]').val(data.PaymentMethodId || '');
            form.find('[name="ShippingDate"]').val(data.ShippingDate ? new Date(data.ShippingDate).toISOString().split('T')[0] : '');
            form.find('[name="DelivryDate"]').val(data.DelivryDate ? new Date(data.DelivryDate).toISOString().split('T')[0] : '');
            // UserSender fields
            if (data.UserSender) {
                form.find('[name="SenderName"]').val(data.UserSender.SenderName || '');
                form.find('[name="SenderEmail"]').val(data.UserSender.Email || '');
                form.find('[name="SenderPhone"]').val(data.UserSender.Phone || '');
                form.find('[name="SenderPostalCode"]').val(data.UserSender.PostalCode || '');
                form.find('[name="SenderContact"]').val(data.UserSender.Contact || '');
                form.find('[name="SenderOtherAddress"]').val(data.UserSender.OtherAddress || '');
                form.find('[name="SenderAddress"]').val(data.UserSender.Address || '');
                form.find('[name="SenderAddress2"]').val(data.UserSender.Address2 || '');
                form.find('[name="SenderAddress3"]').val(data.UserSender.Address3 || '');
                form.find('[name="SenderCity"]').val(data.UserSender.CityId || '');
                form.find('[name="SenderCountry"]').val(data.UserSender.CountryId || '');
                form.find('#isDefaultSender').prop('checked', !!data.UserSender.IsDefault);
            }
            // UserReceiver fields
            if (data.UserReceiver) {
                form.find('[name="ReceiverName"]').val(data.UserReceiver.ReceiverName || '');
                form.find('[name="ReceiverEmail"]').val(data.UserReceiver.Email || '');
                form.find('[name="ReceiverPhone"]').val(data.UserReceiver.Phone || '');
                form.find('[name="ReceiverPostalCode"]').val(data.UserReceiver.PostalCode || '');
                form.find('[name="ReceiverContact"]').val(data.UserReceiver.Contact || '');
                form.find('[name="ReceiverOtherAddress"]').val(data.UserReceiver.OtherAddress || '');
                form.find('[name="ReceiverAddress"]').val(data.UserReceiver.Address || '');
                form.find('[name="ReceiverCity"]').val(data.UserReceiver.CityId || '');
                form.find('[name="ReceiverCountry"]').val(data.UserReceiver.CountryId || '');
                form.find('#isDefaultReceiver').prop('checked', !!data.UserReceiver.IsDefault);
            }
        },
        submitShipment: function (onSuccess, onError, forceSubmit = false) {
            console.log("SubmitSHipment Called in the Console From the ShipmentService.js");
            const formEl = document.querySelector(this.settings.formSelector);
            if (!formEl) {
                const err = { message: 'Form not found' };
                if (onError)
                    return onError(err);
                return console.error(err);
            }
            const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';
            const idEl = formEl.querySelector('[name="Id"]');
            const id = idEl ? (idEl.value || '').trim() : '';
            // If Id exists treat as update
            if (id && id !== EMPTY_GUID) {
                return this.update(onSuccess, onError);
            }
            const payload = this.getShipmentFormData();
            if (!window.ShipmentApiClient || typeof ShipmentApiClient.create !== 'function') {
                const err = { message: 'ShipmentApiClient.create not available' };
                if (onError)
                    return onError(err);
                return console.error(err);
            }
            ShipmentApiClient.create(payload)
                .then((resp) => {
                var _a, _b, _c, _d;
                // Debug: log server response shape so extractFirstMessage can be tuned if needed
                try {
                    console.debug('ShipmentService.create resp:', resp);
                }
                catch ( /* ignore */_e) { /* ignore */ }
                if (onSuccess)
                    return onSuccess(resp);
                // Prefer canonical normalizeResponse when available
                let nr = null;
                try {
                    if (typeof ShipmentApiClient.normalizeResponse === 'function')
                        nr = ShipmentApiClient.normalizeResponse(resp);
                }
                catch (e) {
                    nr = null;
                }
                if (!nr) {
                    const data = (resp && resp.Data !== undefined) ? resp.Data : resp;
                    const successFlag = !!(resp && (resp.Success === true || resp.success === true || resp.IsSuccess === true));
                    const dataIsTrue = data === true;
                    const dataLooksLikeObject = data && typeof data === 'object' && Object.keys(data).length > 0;
                    const null204Success = resp === null || resp === undefined;
                    const succeeded = successFlag || dataIsTrue || dataLooksLikeObject || null204Success;
                    nr = { success: succeeded, message: (resp && (resp.Message || resp.message || resp.Title || resp.title)) || null, data: data, errors: resp && (resp.Errors || resp.errors || ((_a = resp.Data) === null || _a === void 0 ? void 0 : _a.Errors) || ((_b = resp.Data) === null || _b === void 0 ? void 0 : _b.errors) || null), raw: resp };
                }
                if (nr.success) {
                    /* COMMENTED OUT - PayPal Server-Side Redirect (Incorrect Approach)
                     * This code was part of an incorrect PayPal implementation.
                     * PayPal should be integrated using JavaScript SDK on frontend.
                     * Payment handling will be done separately via PaymentController.
                     *
                    // ═══════════════════════════════════════════════════════════════
                    // ✅ PayPal Integration: Check if payment requires approval
                    // ═══════════════════════════════════════════════════════════════
                    try {
                        const paymentTransaction = nr.data && nr.data.PaymentTransaction;

                        if (paymentTransaction) {
                            const transactionStatus = paymentTransaction.TransactionStatus;
                            const additionalInfo = paymentTransaction.AdditionalInfo;

                            console.debug('Payment transaction detected:', {
                                status: transactionStatus,
                                additionalInfo: additionalInfo
                            });

                            // Check if PayPal approval is required
                            // Status 0 = Pending and AdditionalInfo contains approval URL
                            if (transactionStatus === 0 &&
                                additionalInfo &&
                                typeof additionalInfo === 'string' &&
                                additionalInfo.startsWith('http')) {

                                console.log('PayPal approval required - redirecting to:', additionalInfo);

                                // Show message to user before redirect
                                if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                                    AppHelper.showToast('Redirecting to PayPal for payment approval...', 'info');
                                }

                                // Redirect to PayPal approval page
                                setTimeout(function() {
                                    window.location.href = additionalInfo;
                                }, 1000); // 1 second delay to show message

                                return;
                            }
                        }
                    } catch (paypalCheckError) {
                        console.warn('PayPal approval check failed:', paypalCheckError);
                        // Continue with normal flow if PayPal check fails
                    }
                    */
                    // ═══════════════════════════════════════════════════════════════
                    // Normal success flow (Stripe or completed payment)
                    // ═══════════════════════════════════════════════════════════════
                    const redirectToList = () => {
                        try {
                            // Redirect to list with 'created' query parameter
                            // Success alert will be shown in UserListShipments.js
                            window.location.href = '/Shipments/List?created=1';
                        }
                        catch (e) { /* ignore redirect failures */ }
                    };
                    // Immediate redirect without showing alert on create page
                    redirectToList();
                    return;
                }
                // Handle errors if not successful
                const errorsPayload = nr.errors || (resp && (resp.Errors || resp.errors || ((_c = resp.Data) === null || _c === void 0 ? void 0 : _c.Errors) || ((_d = resp.Data) === null || _d === void 0 ? void 0 : _d.errors) || resp));
                if (errorsPayload && this.tryMapErrorsAndNavigate(errorsPayload, formEl)) {
                    const first = this._extractFirstMessage(errorsPayload) || 'One or more validation errors occurred';
                    if (window.AppHelper && typeof AppHelper.showToast === 'function')
                        AppHelper.showToast(first, 'error');
                    if (onError)
                        return onError({ mapped: true, errors: errorsPayload, response: resp });
                    return;
                }
                const fallbackMessage = nr.message || (resp && (resp.Message || resp.message || resp.Title || resp.title)) || 'Failed to create shipment';
                if (window.AppHelper && typeof AppHelper.showToast === 'function')
                    AppHelper.showToast(fallbackMessage, 'error');
                throw { message: fallbackMessage, response: resp };
            })
                .catch((xhr) => {
                var _a, _b, _c, _d;
                try {
                    console.debug('ShipmentService.create err:', xhr);
                }
                catch ( /* ignore */_e) { /* ignore */ }
                // Try normalize if available
                let nr = null;
                try {
                    if (typeof ShipmentApiClient.normalizeResponse === 'function')
                        nr = ShipmentApiClient.normalizeResponse(xhr.responseJSON || xhr);
                }
                catch (e) {
                    nr = null;
                }
                const serverErrors = (nr === null || nr === void 0 ? void 0 : nr.errors) || xhr && (((_a = xhr.responseJSON) === null || _a === void 0 ? void 0 : _a.errors) || ((_b = xhr.response) === null || _b === void 0 ? void 0 : _b.errors) || xhr.errors || xhr.responseJSON || xhr.response || xhr);
                if (serverErrors && this.tryMapErrorsAndNavigate(serverErrors, formEl)) {
                    const first = this._extractFirstMessage(serverErrors) || 'One or more validation errors occurred';
                    if (window.AppHelper && typeof AppHelper.showToast === 'function')
                        AppHelper.showToast(first, 'error');
                    if (onError)
                        return onError({ mapped: true, errors: serverErrors, response: xhr });
                    return;
                }
                if (onError)
                    return onError(xhr);
                const fallbackMessage = (nr === null || nr === void 0 ? void 0 : nr.message) || (xhr && (xhr.message || xhr.Message || ((_c = xhr.responseJSON) === null || _c === void 0 ? void 0 : _c.message) || ((_d = xhr.responseJSON) === null || _d === void 0 ? void 0 : _d.title))) || 'Failed to create shipment';
                if (window.AppHelper && typeof AppHelper.showToast === 'function')
                    AppHelper.showToast(fallbackMessage, 'error');
                console.error('Shipment create error', xhr);
            });
        },
        update: function (onSuccess, onError) {
            const formEl = document.querySelector(this.settings.formSelector);
            if (!formEl) {
                const err = { message: 'Form not found' };
                if (onError)
                    return onError(err);
                return console.error(err);
            }
            const idEl = formEl.querySelector('[name="Id"]');
            const id = idEl ? (idEl.value || '').trim() : null;
            if (!id) {
                const err = { message: 'Shipment Id missing' };
                if (onError)
                    return onError(err);
                return console.error(err);
            }
            const payload = this.getShipmentFormData();
            payload.Id = id;
            // --- End ensure required fields ---
            if (!window.ShipmentApiClient || typeof ShipmentApiClient.update !== 'function') {
                const err = { message: 'ShipmentApiClient.update not available' };
                if (onError)
                    return onError(err);
                return console.error(err);
            }
            const redirectToResult = (redirectId) => {
                if (redirectId)
                    window.location.href = '/Shipments/Show/' + encodeURIComponent(redirectId);
                else
                    window.location.href = '/Shipments/List';
            };
            const redirectDelayMs = (typeof this.settings.redirectDelayMs === 'number') ? this.settings.redirectDelayMs : 3000;
            Promise.resolve(ShipmentApiClient.update(id, payload))
                .then((resp) => {
                var _a, _b, _c, _d;
                try {
                    console.debug('ShipmentService.update resp:', resp);
                }
                catch ( /* ignore */_e) { /* ignore */ }
                if (onSuccess)
                    return onSuccess(resp);
                // Prefer normalizeResponse where available
                let nr = null;
                try {
                    if (typeof ShipmentApiClient.normalizeResponse === 'function')
                        nr = ShipmentApiClient.normalizeResponse(resp);
                }
                catch (e) {
                    nr = null;
                }
                if (!nr) {
                    const data = (resp && resp.Data !== undefined) ? resp.Data : resp;
                    const successFlag = !!(resp && (resp.Success === true || resp.success === true || resp.IsSuccess === true));
                    const dataIsTrue = data === true;
                    const dataLooksLikeObject = data && typeof data === 'object' && Object.keys(data).length > 0;
                    const null204Success = resp === null || resp === undefined;
                    const succeeded = successFlag || dataIsTrue || dataLooksLikeObject || null204Success;
                    nr = { success: succeeded, message: (resp && (resp.Message || resp.message || resp.Title || resp.title)) || null, data: data, errors: resp && (resp.Errors || resp.errors || ((_a = resp.Data) === null || _a === void 0 ? void 0 : _a.Errors) || ((_b = resp.Data) === null || _b === void 0 ? void 0 : _b.errors) || null), raw: resp };
                }
                if (nr.success) {
                    // Always show success alert and redirect to List view
                    const redirectToList = () => { window.location.href = '/Shipments/List'; };
                    const updatedTitle = (window.AppResourceAlerts && window.AppResourceAlerts.updatedTitle) || 'Shipment Updated';
                    const updatedSuccess = (window.AppResourceAlerts && window.AppResourceAlerts.updatedSuccess) || 'Shipment updated successfully';
                    if (window.showAlert && typeof showAlert.Success === 'function') {
                        try {
                            if (showAlert.Success.length >= 3) {
                                showAlert.Success(updatedTitle, updatedSuccess, redirectToList);
                            }
                            else {
                                showAlert.Success(updatedTitle, updatedSuccess);
                                setTimeout(redirectToList, redirectDelayMs);
                            }
                        }
                        catch (e) {
                            try {
                                alert(updatedSuccess);
                            }
                            catch (_f) { }
                            redirectToList();
                        }
                        return;
                    }
                    if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                        try {
                            AppHelper.showToast(updatedSuccess, 'success');
                        }
                        catch (_g) { }
                        setTimeout(redirectToList, redirectDelayMs);
                        return;
                    }
                    try {
                        alert(updatedSuccess);
                    }
                    catch (_h) { }
                    redirectToList();
                    return;
                }
                const errorsPayload = nr.errors || (resp && (resp.Errors || resp.errors || ((_c = resp.Data) === null || _c === void 0 ? void 0 : _c.Errors) || ((_d = resp.Data) === null || _d === void 0 ? void 0 : _d.errors) || resp));
                if (errorsPayload && this.tryMapErrorsAndNavigate(errorsPayload, formEl)) {
                    const first = this._extractFirstMessage(errorsPayload) || 'One or more validation errors occurred';
                    if (window.AppHelper && typeof AppHelper.showToast === 'function')
                        AppHelper.showToast(first, 'error');
                    if (onError)
                        return onError({ mapped: true, errors: errorsPayload, response: resp });
                    return;
                }
                // fallback: show friendly error
                const messageFromResp = (nr === null || nr === void 0 ? void 0 : nr.message) || (resp && (resp.Message || resp.message || resp.Title || resp.title)) || null;
                const firstMsg = this._extractFirstMessage((nr === null || nr === void 0 ? void 0 : nr.errors) || resp) || messageFromResp;
                const fallbackMessage = firstMsg || 'Failed to update shipment';
                if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                    AppHelper.showToast(fallbackMessage, 'error');
                }
                else if (window.showAlert && typeof showAlert.Error === 'function') {
                    try {
                        showAlert.Error('Update failed', fallbackMessage);
                    }
                    catch ( /* ignore */_j) { /* ignore */ }
                }
                else {
                    try {
                        alert(fallbackMessage);
                    }
                    catch ( /* ignore */_k) { /* ignore */ }
                }
                throw { message: fallbackMessage, response: resp };
            })
                .catch((err) => {
                var _a, _b;
                try {
                    console.debug('ShipmentService.update err:', err);
                }
                catch ( /* ignore */_c) { /* ignore */ }
                const serverErrors = err && (((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.errors) || ((_b = err.response) === null || _b === void 0 ? void 0 : _b.errors) || err.errors || err.responseJSON || err.response || err);
                if (serverErrors && this.tryMapErrorsAndNavigate(serverErrors, formEl)) {
                    const first = this._extractFirstMessage(serverErrors) || 'One or more validation errors occurred';
                    if (window.AppHelper && typeof AppHelper.showToast === 'function')
                        AppHelper.showToast(first, 'error');
                    if (onError)
                        return onError({ mapped: true, errors: serverErrors, response: err });
                    return;
                }
                if (onError)
                    return onError(err);
                const fallbackMessage = (err && (err.message || err.Message)) || 'Failed to update shipment';
                if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                    AppHelper.showToast(fallbackMessage, 'error');
                }
                else if (window.showAlert && typeof showAlert.Error === 'function') {
                    try {
                        showAlert.Error('Error', fallbackMessage);
                    }
                    catch ( /* ignore */_d) { /* ignore */ }
                }
                else {
                    try {
                        alert(fallbackMessage);
                    }
                    catch ( /* ignore */_e) { /* ignore */ }
                }
                console.error('Shipment update error', err);
            });
        },
        approveShipment: function (onSuccess, onError) {
            const formEl = document.querySelector(this.settings.formSelector);
            if (!formEl) {
                const err = { message: 'Form not found' };
                if (onError)
                    return onError(err);
                return console.error(err);
            }
            const idEl = formEl.querySelector('[name="Id"]');
            const id = idEl ? (idEl.value || '').trim() : null;
            if (!id) {
                const err = { message: 'Shipment Id missing' };
                if (onError)
                    return onError(err);
                return console.error(err);
            }
            const payload = this.getShipmentFormData();
            payload.Id = id;
            // --- End ensure required fields ---
            if (!window.ShipmentApiClient || typeof ShipmentApiClient.approved !== 'function') {
                const err = { message: 'ShipmentApiClient.update not available' };
                if (onError)
                    return onError(err);
                return console.error(err);
            }
            const redirectToResult = (redirectId) => {
                if (redirectId)
                    window.location.href = '/admin/Shipments/Show/' + encodeURIComponent(redirectId);
                else
                    window.location.href = '/admin/Shipments/List';
            };
            const redirectDelayMs = (typeof this.settings.redirectDelayMs === 'number') ? this.settings.redirectDelayMs : 3000;
            Promise.resolve(ShipmentApiClient.approved(id, payload))
                .then((resp) => {
                var _a, _b, _c, _d;
                try {
                    console.debug('ShipmentService.update resp:', resp);
                }
                catch ( /* ignore */_e) { /* ignore */ }
                if (onSuccess)
                    return onSuccess(resp);
                // Prefer normalizeResponse where available
                let nr = null;
                try {
                    if (typeof ShipmentApiClient.normalizeResponse === 'function')
                        nr = ShipmentApiClient.normalizeResponse(resp);
                }
                catch (e) {
                    nr = null;
                }
                if (!nr) {
                    const data = (resp && resp.Data !== undefined) ? resp.Data : resp;
                    const successFlag = !!(resp && (resp.Success === true || resp.success === true || resp.IsSuccess === true));
                    const dataIsTrue = data === true;
                    const dataLooksLikeObject = data && typeof data === 'object' && Object.keys(data).length > 0;
                    const null204Success = resp === null || resp === undefined;
                    const succeeded = successFlag || dataIsTrue || dataLooksLikeObject || null204Success;
                    nr = { success: succeeded, message: (resp && (resp.Message || resp.message || resp.Title || resp.title)) || null, data: data, errors: resp && (resp.Errors || resp.errors || ((_a = resp.Data) === null || _a === void 0 ? void 0 : _a.Errors) || ((_b = resp.Data) === null || _b === void 0 ? void 0 : _b.errors) || null), raw: resp };
                }
                if (nr.success) {
                    // Always show success alert and redirect to List view
                    const redirectToList = () => { window.location.href = '/admin/Shipments/List'; };
                    const updatedTitle = (window.AppResourceAlerts && window.AppResourceAlerts.updatedTitle) || 'Shipment Updated';
                    const updatedSuccess = (window.AppResourceAlerts && window.AppResourceAlerts.updatedSuccess) || 'Shipment updated successfully';
                    if (window.showAlert && typeof showAlert.Success === 'function') {
                        try {
                            if (showAlert.Success.length >= 3) {
                                showAlert.Success(updatedTitle, updatedSuccess, redirectToList);
                            }
                            else {
                                showAlert.Success(updatedTitle, updatedSuccess);
                                setTimeout(redirectToList, redirectDelayMs);
                            }
                        }
                        catch (e) {
                            try {
                                alert(updatedSuccess);
                            }
                            catch (_f) { }
                            redirectToList();
                        }
                        return;
                    }
                    if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                        try {
                            AppHelper.showToast(updatedSuccess, 'success');
                        }
                        catch (_g) { }
                        setTimeout(redirectToList, redirectDelayMs);
                        return;
                    }
                    try {
                        alert(updatedSuccess);
                    }
                    catch (_h) { }
                    redirectToList();
                    return;
                }
                const errorsPayload = nr.errors || (resp && (resp.Errors || resp.errors || ((_c = resp.Data) === null || _c === void 0 ? void 0 : _c.Errors) || ((_d = resp.Data) === null || _d === void 0 ? void 0 : _d.errors) || resp));
                if (errorsPayload && this.tryMapErrorsAndNavigate(errorsPayload, formEl)) {
                    const first = this._extractFirstMessage(errorsPayload) || 'One or more validation errors occurred';
                    if (window.AppHelper && typeof AppHelper.showToast === 'function')
                        AppHelper.showToast(first, 'error');
                    if (onError)
                        return onError({ mapped: true, errors: errorsPayload, response: resp });
                    return;
                }
                // fallback: show friendly error
                const messageFromResp = (nr === null || nr === void 0 ? void 0 : nr.message) || (resp && (resp.Message || resp.message || resp.Title || resp.title)) || null;
                const firstMsg = this._extractFirstMessage((nr === null || nr === void 0 ? void 0 : nr.errors) || resp) || messageFromResp;
                const fallbackMessage = firstMsg || 'Failed to update shipment';
                if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                    AppHelper.showToast(fallbackMessage, 'error');
                }
                else if (window.showAlert && typeof showAlert.Error === 'function') {
                    try {
                        showAlert.Error('Update failed', fallbackMessage);
                    }
                    catch ( /* ignore */_j) { /* ignore */ }
                }
                else {
                    try {
                        alert(fallbackMessage);
                    }
                    catch ( /* ignore */_k) { /* ignore */ }
                }
                throw { message: fallbackMessage, response: resp };
            })
                .catch((err) => {
                var _a, _b;
                try {
                    console.debug('ShipmentService.update err:', err);
                }
                catch ( /* ignore */_c) { /* ignore */ }
                const serverErrors = err && (((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.errors) || ((_b = err.response) === null || _b === void 0 ? void 0 : _b.errors) || err.errors || err.responseJSON || err.response || err);
                if (serverErrors && this.tryMapErrorsAndNavigate(serverErrors, formEl)) {
                    const first = this._extractFirstMessage(serverErrors) || 'One or more validation errors occurred';
                    if (window.AppHelper && typeof AppHelper.showToast === 'function')
                        AppHelper.showToast(first, 'error');
                    if (onError)
                        return onError({ mapped: true, errors: serverErrors, response: err });
                    return;
                }
                if (onError)
                    return onError(err);
                const fallbackMessage = (err && (err.message || err.Message)) || 'Failed to update shipment';
                if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                    AppHelper.showToast(fallbackMessage, 'error');
                }
                else if (window.showAlert && typeof showAlert.Error === 'function') {
                    try {
                        showAlert.Error('Error', fallbackMessage);
                    }
                    catch ( /* ignore */_d) { /* ignore */ }
                }
                else {
                    try {
                        alert(fallbackMessage);
                    }
                    catch ( /* ignore */_e) { /* ignore */ }
                }
                console.error('Shipment update error', err);
            });
        },
        ChangeStatus: function (onSuccess, onError) {
            const formEl = document.querySelector(this.settings.formSelector);
            if (!formEl) {
                const err = { message: 'Form not found' };
                if (onError)
                    return onError(err);
                return console.error(err);
            }
            const idEl = formEl.querySelector('[name="Id"]');
            const id = idEl ? (idEl.value || '').trim() : null;
            if (!id) {
                const err = { message: 'Shipment Id missing' };
                if (onError)
                    return onError(err);
                return console.error(err);
            }
            const payload = this.getShipmentFormData();
            payload.Id = id;
            // --- End ensure required fields ---
            if (!window.ShipmentApiClient || typeof ShipmentApiClient.changeStatus !== 'function') {
                const err = { message: 'ShipmentApiClient.update not available' };
                if (onError)
                    return onError(err);
                return console.error(err);
            }
            const redirectToResult = (redirectId) => {
                if (redirectId)
                    window.location.href = '/admin/Shipments/Show/' + encodeURIComponent(redirectId);
                else
                    window.location.href = '/admin/Shipments/List';
            };
            const redirectDelayMs = (typeof this.settings.redirectDelayMs === 'number') ? this.settings.redirectDelayMs : 3000;
            Promise.resolve(ShipmentApiClient.changeStatus(id, payload))
                .then((resp) => {
                var _a, _b, _c, _d;
                try {
                    console.debug('ShipmentService.update resp:', resp);
                }
                catch ( /* ignore */_e) { /* ignore */ }
                if (onSuccess)
                    return onSuccess(resp);
                // Prefer normalizeResponse where available
                let nr = null;
                try {
                    if (typeof ShipmentApiClient.normalizeResponse === 'function')
                        nr = ShipmentApiClient.normalizeResponse(resp);
                }
                catch (e) {
                    nr = null;
                }
                if (!nr) {
                    const data = (resp && resp.Data !== undefined) ? resp.Data : resp;
                    const successFlag = !!(resp && (resp.Success === true || resp.success === true || resp.IsSuccess === true));
                    const dataIsTrue = data === true;
                    const dataLooksLikeObject = data && typeof data === 'object' && Object.keys(data).length > 0;
                    const null204Success = resp === null || resp === undefined;
                    const succeeded = successFlag || dataIsTrue || dataLooksLikeObject || null204Success;
                    nr = { success: succeeded, message: (resp && (resp.Message || resp.message || resp.Title || resp.title)) || null, data: data, errors: resp && (resp.Errors || resp.errors || ((_a = resp.Data) === null || _a === void 0 ? void 0 : _a.Errors) || ((_b = resp.Data) === null || _b === void 0 ? void 0 : _b.errors) || null), raw: resp };
                }
                if (nr.success) {
                    // Always show success alert and redirect to List view
                    const redirectToList = () => { window.location.href = '/admin/Shipments/List'; };
                    const updatedTitle = (window.AppResourceAlerts && window.AppResourceAlerts.updatedTitle) || 'Shipment Updated';
                    const updatedSuccess = (window.AppResourceAlerts && window.AppResourceAlerts.updatedSuccess) || 'Shipment updated successfully';
                    if (window.showAlert && typeof showAlert.Success === 'function') {
                        try {
                            if (showAlert.Success.length >= 3) {
                                showAlert.Success(updatedTitle, updatedSuccess, redirectToList);
                            }
                            else {
                                showAlert.Success(updatedTitle, updatedSuccess);
                                setTimeout(redirectToList, redirectDelayMs);
                            }
                        }
                        catch (e) {
                            try {
                                alert(updatedSuccess);
                            }
                            catch (_f) { }
                            redirectToList();
                        }
                        return;
                    }
                    if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                        try {
                            AppHelper.showToast(updatedSuccess, 'success');
                        }
                        catch (_g) { }
                        setTimeout(redirectToList, redirectDelayMs);
                        return;
                    }
                    try {
                        alert(updatedSuccess);
                    }
                    catch (_h) { }
                    redirectToList();
                    return;
                }
                const errorsPayload = nr.errors || (resp && (resp.Errors || resp.errors || ((_c = resp.Data) === null || _c === void 0 ? void 0 : _c.Errors) || ((_d = resp.Data) === null || _d === void 0 ? void 0 : _d.errors) || resp));
                if (errorsPayload && this.tryMapErrorsAndNavigate(errorsPayload, formEl)) {
                    const first = this._extractFirstMessage(errorsPayload) || 'One or more validation errors occurred';
                    if (window.AppHelper && typeof AppHelper.showToast === 'function')
                        AppHelper.showToast(first, 'error');
                    if (onError)
                        return onError({ mapped: true, errors: errorsPayload, response: resp });
                    return;
                }
                // fallback: show friendly error
                const messageFromResp = (nr === null || nr === void 0 ? void 0 : nr.message) || (resp && (resp.Message || resp.message || resp.Title || resp.title)) || null;
                const firstMsg = this._extractFirstMessage((nr === null || nr === void 0 ? void 0 : nr.errors) || resp) || messageFromResp;
                const fallbackMessage = firstMsg || 'Failed to update shipment';
                if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                    AppHelper.showToast(fallbackMessage, 'error');
                }
                else if (window.showAlert && typeof showAlert.Error === 'function') {
                    try {
                        showAlert.Error('Update failed', fallbackMessage);
                    }
                    catch ( /* ignore */_j) { /* ignore */ }
                }
                else {
                    try {
                        alert(fallbackMessage);
                    }
                    catch ( /* ignore */_k) { /* ignore */ }
                }
                throw { message: fallbackMessage, response: resp };
            })
                .catch((err) => {
                var _a, _b;
                try {
                    console.debug('ShipmentService.update err:', err);
                }
                catch ( /* ignore */_c) { /* ignore */ }
                const serverErrors = err && (((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.errors) || ((_b = err.response) === null || _b === void 0 ? void 0 : _b.errors) || err.errors || err.responseJSON || err.response || err);
                if (serverErrors && this.tryMapErrorsAndNavigate(serverErrors, formEl)) {
                    const first = this._extractFirstMessage(serverErrors) || 'One or more validation errors occurred';
                    if (window.AppHelper && typeof AppHelper.showToast === 'function')
                        AppHelper.showToast(first, 'error');
                    if (onError)
                        return onError({ mapped: true, errors: serverErrors, response: err });
                    return;
                }
                if (onError)
                    return onError(err);
                const fallbackMessage = (err && (err.message || err.Message)) || 'Failed to update shipment';
                if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                    AppHelper.showToast(fallbackMessage, 'error');
                }
                else if (window.showAlert && typeof showAlert.Error === 'function') {
                    try {
                        showAlert.Error('Error', fallbackMessage);
                    }
                    catch ( /* ignore */_d) { /* ignore */ }
                }
                else {
                    try {
                        alert(fallbackMessage);
                    }
                    catch ( /* ignore */_e) { /* ignore */ }
                }
                console.error('Shipment update error', err);
            });
        },
        markReadyShipment: function (id, options = {}) {
            if (!id)
                return Promise.reject(new Error('Shipment id required'));
            const opts = Object.assign({
                button: null,
                saveFirst: true,
                redirect: true,
                redirectUrl: '/admin/Shipments/List?ready=1&readyId=' + encodeURIComponent(id)
            }, options || {});
            const btn = opts.button || null;
            if (btn) {
                try {
                    btn.disabled = true;
                }
                catch (_a) { }
            }
            const restoreBtn = () => {
                if (btn)
                    try {
                        btn.disabled = false;
                    }
                    catch (_a) { }
            };
            const doReadyFlow = () => __awaiter(this, void 0, void 0, function* () {
                var _a, _b, _c;
                // If saveFirst, attempt to persist form values
                if (opts.saveFirst) {
                    try {
                        const form = document.querySelector(this.settings.formSelector);
                        if (form) {
                            const payload = this.getShipmentFormData();
                            payload.Id = id;
                            if (window.ShipmentApiClient && typeof ShipmentApiClient.update === 'function') {
                                yield ShipmentApiClient.update(id, payload);
                            }
                            else if (window.ShipmentApiClient && typeof ShipmentApiClient._putJson === 'function') {
                                yield ShipmentApiClient._putJson(this.apiListUrl + '/' + encodeURIComponent(id), payload).catch(() => null);
                            }
                        }
                    }
                    catch (upErr) {
                        try {
                            const serverErrors = (upErr === null || upErr === void 0 ? void 0 : upErr.responseJSON) || (upErr === null || upErr === void 0 ? void 0 : upErr.response) || upErr;
                            if (serverErrors && this.tryMapErrorsAndNavigate(serverErrors, document.querySelector(this.settings.formSelector))) {
                                const err = new Error('Validation failed');
                                err.mapped = true;
                                throw err;
                            }
                        }
                        catch (mapEx) { /* ignore */ }
                        throw upErr;
                    }
                }
                // Attempt to call dedicated ready endpoint and persist carrier when provided.
                const carrierId = opts.carrierId || null;
                // Prefer ShipmentApiClient.changeStatus then update carrier via update if changeStatus exists
                if (window.ShipmentApiClient && typeof ShipmentApiClient.ready === 'function') {
                    // Use dedicated ready endpoint which accepts carrierId as query parameter
                    yield ShipmentApiClient.ready(id, carrierId || null);
                }
                else if (window.ShipmentApiClient && typeof ShipmentApiClient.changeStatus === 'function') {
                    // older fallback: changeStatus then update
                    yield ShipmentApiClient.changeStatus(id, (typeof Business !== 'undefined' && ((_c = (_b = (_a = Business.Services) === null || _a === void 0 ? void 0 : _a.Shipment) === null || _b === void 0 ? void 0 : _b.ShipmentStatusEnum) === null || _c === void 0 ? void 0 : _c.ReadyForShipping)) ? (Business.Services.Shipment.ShipmentStatusEnum.ReadyForShipping) : 3);
                    if (carrierId) {
                        try {
                            if (typeof ShipmentApiClient.update === 'function') {
                                yield ShipmentApiClient.update(id, { Id: id, CarrierId: carrierId }).catch(() => null);
                            }
                            else {
                                yield fetch('/api/Shipments/' + encodeURIComponent(id), { method: 'PUT', headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin', body: JSON.stringify({ Id: id, CarrierId: carrierId }) }).catch(() => null);
                            }
                        }
                        catch (e) { /* ignore carrier update failures here */ }
                    }
                }
                else {
                    // final fallback to direct POST with carrierId as querystring
                    const qs = carrierId ? ('?carrierId=' + encodeURIComponent(carrierId)) : '';
                    const url = '/api/Shipments/' + encodeURIComponent(id) + '/ready' + qs;
                    const headers = Object.assign({ Accept: 'application/json' }, (document.querySelector('input[name="__RequestVerificationToken"]') ? { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value } : {}));
                    yield fetch(url, { method: 'POST', credentials: 'same-origin', headers }).catch(() => null);
                }
                // fetch fresh DTO
                let dto = null;
                if (window.ShipmentApiClient && typeof ShipmentApiClient.getById === 'function')
                    dto = yield ShipmentApiClient.getById(id).catch(() => null);
                else {
                    const g = yield fetch('/api/Shipments/' + encodeURIComponent(id), { credentials: 'same-origin', headers: { Accept: 'application/json' } });
                    if (g.ok) {
                        const pj = yield g.json().catch(() => null);
                        dto = pj && (pj.Data || pj.data) ? (pj.Data || pj.data) : pj;
                    }
                }
                return dto;
            });
            return doReadyFlow()
                .then(dto => {
                if (opts.redirect && opts.redirectUrl) {
                    try {
                        window.location.href = opts.redirectUrl;
                    }
                    catch (_a) { }
                }
                return dto;
            })
                .catch(err => { throw err; })
                .finally(() => { restoreBtn(); });
        },
        // ✅ SHIPPED SHIPMENT (COPY OF APPROVE - EXACT SAME PATTERN)
        shippedShipment: function (id, options = {}) {
            if (!id)
                return Promise.reject(new Error('Shipment id required'));
            const opts = Object.assign({
                button: null,
                saveFirst: false, // ✅ No need to save first
                redirect: true,
                redirectUrl: '/admin/Shipments/List'
            }, options || {});
            const btn = opts.button || null;
            if (btn) {
                try {
                    btn.disabled = true;
                }
                catch (_a) { }
            }
            const restoreBtn = () => {
                if (btn)
                    try {
                        btn.disabled = false;
                    }
                    catch (_a) { }
            };
            const doShippedFlow = () => __awaiter(this, void 0, void 0, function* () {
                var _a, _b, _c;
                // Unified flow: use changeStatus endpoint with 'Shipped' target state when possible
                // Determine target state from global enum if available or options.targetState
                const targetState = (typeof opts.targetState !== 'undefined' && opts.targetState !== null)
                    ? opts.targetState
                    : (typeof Business !== 'undefined' && ((_c = (_b = (_a = Business.Services) === null || _a === void 0 ? void 0 : _a.Shipment) === null || _b === void 0 ? void 0 : _b.ShipmentStatusEnum) === null || _c === void 0 ? void 0 : _c.Shipped) ? Business.Services.Shipment.ShipmentStatusEnum.Shipped : undefined);
                // Attempt to build payload from current form if present
                const form = document.querySelector(this.settings.formSelector);
                let payload = null;
                if (form) {
                    payload = this.getShipmentFormData();
                    payload.Id = id;
                }
                if (typeof this.changeStatus === 'function' && (targetState !== undefined)) {
                    const statusPayload = Object.assign({}, payload, { CurrentState: targetState });
                    return this.changeStatus(id, statusPayload);
                }
                // Fallback: call existing shipped endpoint
                if (window.ShipmentApiClient && typeof ShipmentApiClient.shipped === 'function') {
                    yield ShipmentApiClient.shipped(id);
                    if (window.ShipmentApiClient && typeof ShipmentApiClient.getById === 'function')
                        return ShipmentApiClient.getById(id).catch(() => null);
                    return null;
                }
                // Final fallback: direct POST
                const url = '/api/Shipments/' + encodeURIComponent(id) + '/shipped';
                const headers = Object.assign({ Accept: 'application/json' }, (document.querySelector('input[name="__RequestVerificationToken"]') ? { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value } : {}));
                yield fetch(url, { method: 'POST', credentials: 'include', headers });
                if (window.ShipmentApiClient && typeof ShipmentApiClient.getById === 'function')
                    return ShipmentApiClient.getById(id).catch(() => null);
                return null;
            });
            return doShippedFlow()
                .then(dto => {
                if (opts.redirect && opts.redirectUrl) {
                    try {
                        window.location.href = opts.redirectUrl;
                    }
                    catch (_a) { }
                }
                return dto;
            })
                .catch(err => { throw err; })
                .finally(() => { restoreBtn(); });
        },
        resetForm: function () {
            const form = document.querySelector(this.settings.formSelector);
            if (form) {
                form.reset();
                $(form).find('.field-error').removeClass('field-error');
                $(form).find('.field-error-message').remove();
                // If jQuery validator exists clear errors
                if ($ && $.validator && $(form).length) {
                    try {
                        $(form).validate().resetForm();
                    }
                    catch ( /* ignore */_a) { /* ignore */ }
                }
            }
        },
        // tolerant property getter (PascalCase, camelCase, snake_case)
        _get: function (obj, propName) {
            if (!obj)
                return undefined;
            if (Object.prototype.hasOwnProperty.call(obj, propName))
                return obj[propName];
            const camel = propName.charAt(0).toLowerCase() + propName.slice(1);
            if (Object.prototype.hasOwnProperty.call(obj, camel))
                return obj[camel];
            const snake = propName.replace(/([A-Z])/g, '_$1').toLowerCase().replace(/^_/, '');
            if (Object.prototype.hasOwnProperty.call(obj, snake))
                return obj[snake];
            const lower = propName.toLowerCase();
            if (Object.prototype.hasOwnProperty.call(obj, lower))
                return obj[lower];
            return undefined;
        },
        // Delete shipment (admin)
        deleteShipment: function (id, options = {}) {
            if (!id)
                return Promise.reject(new Error('Shipment id required'));
            const opts = Object.assign({
                button: null,
                confirmText: (window.AppResourceAlerts && window.AppResourceAlerts.confirmDelete) || (window.AppResource && AppResource.Labels && AppResource.Labels.ConfirmDelete) || 'Are you sure you want to delete this shipment?',
                skipConfirm: false,
                refreshList: false,
                redirectUrl: '/Shipments/List',
                redirectDelayMs: 5000,
                onSuccess: null,
                onError: null
            }, options || {});
            const showConfirm = () => new Promise((resolve) => {
                try {
                    if (opts.skipConfirm)
                        return resolve(true);
                    if (window.showAlert && typeof showAlert.ConfirmDelete === 'function') {
                        try {
                            showAlert.ConfirmDelete(() => resolve(true), () => resolve(false));
                        }
                        catch (_a) {
                            showAlert.ConfirmDelete(() => resolve(true));
                        }
                    }
                    else
                        resolve(confirm(opts.confirmText));
                }
                catch (_b) {
                    resolve(confirm(opts.confirmText));
                }
            });
            const setBusy = (btn) => {
                if (!btn)
                    return null;
                try {
                    const orig = btn.innerHTML;
                    btn.disabled = true;
                    btn.setAttribute('aria-disabled', 'true');
                    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> ' + (((window.AppResourceAlerts && window.AppResourceAlerts.deleting) || 'Deleting...'));
                    return orig;
                }
                catch (_a) {
                    try {
                        btn.disabled = true;
                        btn.setAttribute('aria-disabled', 'true');
                    }
                    catch (_b) { }
                    return null;
                }
            };
            const restore = (btn, orig) => {
                if (!btn)
                    return;
                try {
                    btn.disabled = false;
                    btn.removeAttribute('aria-disabled');
                    if (orig !== null && orig !== undefined)
                        btn.innerHTML = orig;
                }
                catch (_a) { }
            };
            const showSuccess = (msg) => {
                if (window.AppHelper && typeof AppHelper.showToast === 'function')
                    AppHelper.showToast(msg, 'success');
                else if (window.showAlert && typeof showAlert.Success === 'function')
                    showAlert.Success(((window.AppResourceAlerts && window.AppResourceAlerts.deletedTitle) || 'Deleted'), msg);
                else
                    console.info(msg);
            };
            const showError = (msg) => {
                if (window.AppHelper && typeof AppHelper.showToast === 'function')
                    AppHelper.showToast(msg, 'error');
                else if (window.showAlert && typeof showAlert.Error === 'function')
                    showAlert.Error(((window.AppResourceAlerts && window.AppResourceAlerts.deleteFailedTitle) || 'Error'), msg);
                else
                    alert(msg);
            };
            return showConfirm().then(confirmed => {
                if (!confirmed)
                    return Promise.resolve(null);
                const btn = opts.button || null;
                const orig = setBusy(btn);
                // Prefer using ShipmentApiClient.delete (it handles fallback internally)
                const doDelete = () => {
                    if (window.ShipmentApiClient && typeof ShipmentApiClient.delete === 'function') {
                        return ShipmentApiClient.delete(id);
                    }
                    // fallback direct fetch to canonical URL
                    const url = `/api/Shipments/${encodeURIComponent(id)}`;
                    const headers = { Accept: 'application/json' };
                    const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
                    if (tokenEl && tokenEl.value)
                        headers['RequestVerificationToken'] = tokenEl.value;
                    return fetch(url, { method: 'DELETE', credentials: 'same-origin', headers })
                        .then((resp) => __awaiter(this, void 0, void 0, function* () {
                        const text = yield resp.text().catch(() => null);
                        let payload = null;
                        try {
                            payload = text ? JSON.parse(text) : null;
                        }
                        catch (_a) {
                            payload = null;
                        }
                        if (!resp.ok) {
                            const err = new Error(text || `Status ${resp.status}`);
                            err.status = resp.status;
                            err.body = payload !== null && payload !== void 0 ? payload : text;
                            throw err;
                        }
                        return {
                            success: !!((payload === null || payload === void 0 ? void 0 : payload.isSuccess) || (payload === null || payload === void 0 ? void 0 : payload.IsSuccess) || (payload === null || payload === void 0 ? void 0 : payload.success) || (payload === null || payload === void 0 ? void 0 : payload.Success)) || payload === null,
                            message: (payload === null || payload === void 0 ? void 0 : payload.message) || (payload === null || payload === void 0 ? void 0 : payload.Message) || ((window.AppResourceAlerts && window.AppResourceAlerts.deletedSuccess) || 'Deleted'),
                            data: (payload === null || payload === void 0 ? void 0 : payload.data) || (payload === null || payload === void 0 ? void 0 : payload.Data) || null
                        };
                    }));
                };
                return Promise.resolve()
                    .then(() => doDelete())
                    .then(result => {
                    const res = result || {};
                    const ok = !!(res.success || res.isSuccess || res.IsSuccess || res.Success);
                    const message = res.message || res.Message || ((window.AppResourceAlerts && window.AppResourceAlerts.deletedSuccess) || 'Shipment deleted');
                    if (ok) {
                        try {
                            showSuccess(message);
                        }
                        catch (_a) { }
                        if (typeof opts.onSuccess === 'function')
                            try {
                                opts.onSuccess(result);
                            }
                            catch (e) {
                                console.error(e);
                            }
                        setTimeout(() => {
                            if (opts.refreshList && typeof ShipmentService.initList === 'function') {
                                ShipmentService.initList({ page: ShipmentService.settings.page, pageSize: ShipmentService.settings.pageSize });
                            }
                            else if (opts.redirectUrl) {
                                window.location.href = opts.redirectUrl;
                            }
                        }, opts.redirectDelayMs);
                        restore(btn, orig);
                        return result;
                    }
                    const failure = message || ((window.AppResourceAlerts && window.AppResourceAlerts.deleteFailed) || 'Failed to delete shipment');
                    restore(btn, orig);
                    showError(failure);
                    if (typeof opts.onError === 'function')
                        try {
                            opts.onError(result);
                        }
                        catch (e) {
                            console.error(e);
                        }
                    return result;
                })
                    .catch(err => {
                    restore(btn, orig);
                    const msg = (err && (err.message || err.body || err.status)) ? `${err.message || err.status}` : ((window.AppResourceAlerts && window.AppResourceAlerts.deleteFailed) || 'Failed to delete shipment');
                    showError(msg);
                    if (typeof opts.onError === 'function')
                        try {
                            opts.onError(err);
                        }
                        catch (e) {
                            console.error(e);
                        }
                    throw err;
                });
            });
        },
        // Payment validation and summary methods
        validatePaymentMethod: function () {
            const form = document.querySelector(this.settings.formSelector);
            if (!form)
                return false;
            const paymentMethodField = form.querySelector('[name="PaymentMethodId"]');
            const paymentMethodId = paymentMethodField === null || paymentMethodField === void 0 ? void 0 : paymentMethodField.value;
            // Get validation error span
            const validationSpan = form.querySelector('[data-valmsg-for="PaymentMethodId"], .field-validation-error[data-valmsg-for="PaymentMethodId"]');
            if (!paymentMethodId || paymentMethodId === '' || paymentMethodId === '00000000-0000-0000-0000-000000000000') {
                // Show inline error only (no toast)
                if (validationSpan) {
                    validationSpan.textContent = 'Please select a payment method';
                    validationSpan.className = 'field-validation-error text-danger';
                    validationSpan.style.display = 'block';
                }
                else {
                    // Fallback: find validation span by proximity
                    const spanNearby = paymentMethodField === null || paymentMethodField === void 0 ? void 0 : paymentMethodField.parentElement.querySelector('.text-danger, [class*="validation"]');
                    if (spanNearby) {
                        spanNearby.textContent = 'Please select a payment method';
                        spanNearby.className = 'field-validation-error text-danger';
                        spanNearby.style.display = 'block';
                    }
                }
                return false;
            }
            // Clear validation error if payment method is selected
            if (validationSpan) {
                validationSpan.textContent = '';
                validationSpan.className = 'field-validation-valid';
                validationSpan.style.display = 'none';
            }
            return true;
        },
        displayPaymentSummary: function (containerSelector) {
            var _l, _m;
            const form = document.querySelector(this.settings.formSelector);
            const container = document.querySelector(containerSelector);
            if (!form || !container)
                return Promise.reject(new Error('Form or container not found'));
            const paymentMethodId = (_l = form.querySelector('[name="PaymentMethodId"]')) === null || _l === void 0 ? void 0 : _l.value;
            let shippingRate = parseFloat(((_m = form.querySelector('[name="ShippingRate"]')) === null || _m === void 0 ? void 0 : _m.value) || 0);
            if (!paymentMethodId) {
                container.innerHTML = '<p class="text-danger">Please select a payment method</p>';
                return Promise.reject(new Error('Payment method not selected'));
            }
            if (shippingRate <= 0) {
                container.innerHTML = '<p class="text-warning"><i class="fa fa-exclamation-triangle"></i> Unable to calculate shipping rate. Please ensure package details and shipping type are selected.</p>';
                return Promise.reject(new Error('Invalid shipping rate'));
            }
            // Show loading state
            container.innerHTML = '<p class="text-muted"><i class="fa fa-spinner fa-spin"></i> Calculating payment details...</p>';
            return PaymentMethodService.GetPaymentDetails(paymentMethodId, shippingRate)
                .then(details => {
                const methodName = details.paymentMethod.methodEname || details.paymentMethod.MethodEname ||
                    details.paymentMethod.methdAname || details.paymentMethod.MethdAname || 'Unknown';
                const commission = details.commission || 0;
                const shippingRateAmount = details.shippingRate || 0;
                const commissionAmount = details.commissionAmount || 0;
                const totalAmount = details.totalAmount || shippingRateAmount;
                const html = `
                        <div class="payment-summary">
                            <h4><i class="fa fa-credit-card"></i> Payment Summary</h4>
                            <div class="payment-details">
                                <div class="payment-row">
                                    <span>Payment Method:</span>
                                    <span><strong>${methodName}</strong></span>
                                </div>
                                <div class="payment-row">
                                    <span>Shipping Rate:</span>
                                    <span>$${shippingRateAmount.toFixed(2)}</span>
                                </div>
                                <div class="payment-row">
                                    <span>Processing Fee (${commission.toFixed(2)}%):</span>
                                    <span>$${commissionAmount.toFixed(2)}</span>
                                </div>
                                <hr/>
                                <div class="payment-row payment-total">
                                    <strong>Total Amount:</strong>
                                    <strong style="color: #28a745;">$${totalAmount.toFixed(2)}</strong>
                                </div>
                            </div>
                            <p style="font-size: 12px; color: #6c757d; margin-top: 10px; font-style: italic;">
                                <i class="fa fa-info-circle"></i> Final charges will be calculated upon shipment creation
                            </p>
                        </div>
                    `;
                container.innerHTML = html;
                // Store payment details for later use
                this._paymentDetails = Object.assign({}, details, {
                    paymentMethod: Object.assign({}, details.paymentMethod || {}, {
                        MethodEname: methodName,
                        methodEname: methodName
                    })
                });
                return this._paymentDetails;
            })
                .catch(err => {
                console.error('Error loading payment details:', err);
                container.innerHTML = `
                        <div class="alert alert-danger">
                            <i class="fa fa-exclamation-triangle"></i> 
                            <strong>Failed to load payment details</strong>
                            <p style="font-size: 14px; margin-top: 8px;">${err.message || 'Unknown error occurred'}</p>
                        </div>
                    `;
                throw err;
            });
        },
        getPaymentDetails: function () {
            return this._paymentDetails || null;
        },
        // Paged list initializer
        initList: function (options = {}) {
            Object.assign(this.settings, options || {});
            const tbodySelector = this.settings.tableBodySelector;
            const tableSelector = this.settings.tableSelector;
            const page = parseInt(this.settings.page || 1, 10);
            const pageSize = parseInt(this.settings.pageSize || 10, 10);
            const sortBy = this.settings.sortBy || 'CreatedDate';
            const sortDir = this.settings.sortDir || 'desc';
            const tbody = document.querySelector(tbodySelector);
            if (tbody)
                tbody.innerHTML = '<tr><td colspan="8" class="text-center">Loading...</td></tr>';
            if (window.ShipmentApiClient && typeof ShipmentApiClient.getPaged === 'function') {
                return ShipmentApiClient.getPaged(page, pageSize, sortBy, sortDir)
                    .then(paged => {
                    const items = paged && Array.isArray(paged.Items) ? paged.Items : [];
                    this._lastPayload = paged || {};
                    const normalizedPage = (paged && (paged.Page || paged.page)) ? (paged.Page || paged.page) : page;
                    const normalizedPageSize = (paged && (paged.PageSize || paged.pageSize)) ? (paged.PageSize || paged.pageSize) : pageSize;
                    const baseIndex = Math.max(0, (normalizedPage - 1) * normalizedPageSize);
                    this.renderTable(tbodySelector, items, tableSelector, baseIndex);
                    if (window.ShipmentsPager && typeof window.ShipmentsPager.renderPagination === 'function') {
                        window.ShipmentsPager.renderPagination(paged, tableSelector, (p) => { this.initList({ page: p, pageSize: normalizedPageSize }); });
                        if (typeof window.ShipmentsPager.bindHeaderSorts === 'function')
                            window.ShipmentsPager.bindHeaderSorts(tableSelector, { sortBy, sortDir }, (k, d) => this.initList({ page: 1, sortBy: k, sortDir: d }));
                    }
                    else {
                        this._renderPager(paged, tableSelector);
                        this._bindHeaderSorts(tableSelector);
                    }
                    return paged;
                })
                    .catch(err => {
                    if (tbody)
                        tbody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Error loading shipments</td></tr>';
                    console.error('Failed to load paged shipments', err);
                });
            }
            if (window.ShipmentApiClient && typeof ShipmentApiClient.getAll === 'function') {
                return ShipmentApiClient.getAll()
                    .then(list => {
                    this._lastPayload = { Items: list || [], Page: 1, PageSize: (list === null || list === void 0 ? void 0 : list.length) || 0, TotalCount: (list === null || list === void 0 ? void 0 : list.length) || 0, TotalPages: 1 };
                    this.renderTable(tbodySelector, list || [], tableSelector, 0);
                    this._renderPager(this._lastPayload, tableSelector);
                    this._bindHeaderSorts(tableSelector);
                    return this._lastPayload;
                })
                    .catch(err => {
                    if (tbody)
                        tbody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Error loading shipments</td></tr>';
                    console.error('Failed to load shipments', err);
                });
            }
            if (tbody)
                tbody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Client not initialized</td></tr>';
            return Promise.resolve({ Items: [] });
        },
        // Inline fallback pager
        _renderPager: function (paged, tableSelector) {
            if (!paged)
                return;
            const page = paged.Page || 1;
            const pageSize = paged.PageSize || 10;
            const total = paged.TotalCount || 0;
            const totalPages = paged.TotalPages || Math.max(1, Math.ceil(total / pageSize));
            const containerId = 'shipments-pagination';
            let container = document.getElementById(containerId);
            const table = document.querySelector(tableSelector);
            if (!container && table && table.parentElement) {
                container = document.createElement('div');
                container.id = containerId;
                container.className = 'mt-3';
                table.parentElement.appendChild(container);
            }
            if (!container)
                return;
            container.innerHTML = '';
            const ul = document.createElement('ul');
            ul.className = 'pagination justify-content-center';
            function createLi(text, targetPage, disabled, active) {
                const li = document.createElement('li');
                li.className = 'page-item' + (disabled ? ' disabled' : '') + (active ? ' active' : '');
                if (active) {
                    const span = document.createElement('span');
                    span.className = 'page-link';
                    span.setAttribute('aria-current', 'page');
                    span.textContent = text;
                    li.appendChild(span);
                }
                else {
                    const a = document.createElement('a');
                    a.className = 'page-link';
                    a.href = '#';
                    a.textContent = text;
                    a.addEventListener('click', (e) => {
                        e.preventDefault();
                        if (!disabled)
                            this.initList({ page: targetPage, pageSize: pageSize });
                    });
                    li.appendChild(a);
                }
                return li;
            }
            ul.appendChild(createLi.call(this, '« Previous', Math.max(1, page - 1), page <= 1, false));
            const windowSize = 7;
            const half = Math.floor(windowSize / 2);
            let start = Math.max(1, page - half);
            const end = Math.min(totalPages, start + windowSize - 1);
            if (end - start + 1 < windowSize)
                start = Math.max(1, end - windowSize + 1);
            for (let p = start; p <= end; p++)
                ul.appendChild(createLi.call(this, String(p), p, false, p === page));
            ul.appendChild(createLi.call(this, 'Next »', Math.min(totalPages, page + 1), page >= totalPages, false));
            container.appendChild(ul);
            const info = document.createElement('div');
            info.className = 'text-center mt-2';
            info.innerHTML = `<small class="text-muted">Page ${page} of ${totalPages} — Showing ${paged.Items ? (Array.isArray(paged.Items) ? paged.Items.length : 0) : 0} of ${total} items</small>`;
            container.appendChild(info);
        },
        // Bind sortable headers
        _bindHeaderSorts: function (tableSelector) {
            const table = document.querySelector(tableSelector);
            if (!table)
                return;
            const headers = table.querySelectorAll('th[data-sort]');
            headers.forEach(h => {
                h.style.cursor = 'pointer';
                try {
                    h.removeEventListener('click', h._sortHandler);
                }
                catch ( /* ignore */_a) { /* ignore */ }
                h._sortHandler = (e) => {
                    const key = h.getAttribute('data-sort');
                    let dir = this.settings.sortDir === 'asc' ? 'desc' : 'asc';
                    if (this.settings.sortBy !== key)
                        dir = 'desc';
                    this.initList({ page: 1, sortBy: key, sortDir: dir });
                };
                h.addEventListener('click', h._sortHandler);
            });
        }
    };
    return svc;
})();
window.ShipmentService = ShipmentService;
//# sourceMappingURL=ShipmentService.js.map