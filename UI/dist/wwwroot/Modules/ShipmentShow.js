var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
/* eslint-disable no-undef */
// ShipmentShow.js
// Renders Shipment Show blocks (shipment, sender, receiver, extras)
// Usage: ShipmentShow.init(serverDto, options)
(function () {
    const ShipmentShow = {};
    const tolerant = payload => payload && (payload.Data || payload.data) ? (payload.Data || payload.data) : payload;
    const safe = v => (v === null || v === undefined || v === '') ? '-' : String(v);
    const fmtDate = d => d ? new Date(d).toLocaleString() : '-';
    const fmtCurrency = v => (v !== null && v !== undefined) ? new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(Number(v)) : '-';
    function renderShipmentBlock(dto) {
        var _a, _b, _c, _d, _e, _f;
        const el = document.getElementById('shipment-block');
        if (!el)
            return;
        el.innerHTML = `
            <div class="card shadow-sm border-0 shipment-card">
                <div class="card-header"><strong>Shipment</strong></div>
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-start mb-2">
                        <div>
                            <div class="shipment-header-title mb-1"><strong>${safe((_a = dto.TrackingNumber) !== null && _a !== void 0 ? _a : dto.ReferenceId)}</strong></div>
                            <div class="small text-muted"><strong>#${safe((_b = dto.ReferenceId) !== null && _b !== void 0 ? _b : '')}</strong></div>
                        </div>
                        <div class="text-end">
                            <span class="badge ${(dto.CurrentState === 1 || (dto.Status && String(dto.Status).toLowerCase() === 'active')) ? 'bg-success' : 'bg-secondary'}">
                                <strong>${(_c = dto.Status) !== null && _c !== void 0 ? _c : (dto.CurrentState === 1 ? 'Active' : dto.CurrentState === 0 ? 'Inactive' : 'Unknown')}</strong>
                            </span>
                        </div>
                    </div>
                    <div class="mt-3">
                        <div class="shipment-prop-row"><strong>Shipping Date:</strong> <strong class="ms-2 value">${fmtDate(dto.ShippingDate)}</strong></div>
                        <div class="shipment-prop-row"><strong>Delivery Date:</strong> <strong class="ms-2 value">${fmtDate(dto.DelivryDate)}</strong></div>
                        <div class="shipment-prop-row"><strong>Package Value:</strong> <strong class="ms-2 value">${fmtCurrency(dto.PackageValue)}</strong></div>
                        <div class="shipment-prop-row"><strong>Shipping Rate:</strong> <strong class="ms-2 value">${fmtCurrency(dto.ShippingRate)}</strong></div>
                        <div class="shipment-prop-row"><strong>Weight (kg):</strong> <strong class="ms-2 value">${safe(dto.Weight)}</strong></div>
                        <div class="shipment-prop-row"><strong>Dimensions (W×H×L cm):</strong> <strong class="ms-2 value">${safe(dto.Width)} × ${safe(dto.Height)} × ${safe(dto.Length)}</strong></div>
                        <div class="shipment-prop-row"><strong>Packaging:</strong> <strong class="ms-2 value">${safe((_d = dto.PackagingName) !== null && _d !== void 0 ? _d : dto.ShipingPackgingId)}</strong></div>
                        <div class="shipment-prop-row"><strong>Shipping Type:</strong> <strong class="ms-2 value">${safe((_e = dto.ShippingTypeName) !== null && _e !== void 0 ? _e : dto.ShippingTypeId)}</strong></div>
                        <div class="shipment-prop-row"><strong>Payment Method:</strong> <strong class="ms-2 value">${safe(dto.PaymentMethodName || dto.PaymentMethodId)}</strong></div>
                        <div class="shipment-prop-row"><strong>Carrier:</strong> <strong class="ms-2 value">${safe((_f = dto.CarrierName) !== null && _f !== void 0 ? _f : dto.CarrierId)}</strong></div>
                        <div class="shipment-prop-row"><strong>Tracking Number:</strong> <strong class="ms-2 value">${safe(dto.TrackingNumber)}</strong></div>
                    </div>
                </div>
            </div>
        `;
    }
    function renderPartyBlock(containerId, title, party) {
        var _a, _b, _c, _d;
        const el = document.getElementById(containerId);
        if (!el)
            return;
        el.innerHTML = `
            <div class="card shadow-sm border-0 h-100 party-card">
                <div class="card-header"><strong>${title}</strong></div>
                <div class="card-body">
                    <div class="mb-2"><div class="fw-bold">${safe((_c = (_b = (_a = party === null || party === void 0 ? void 0 : party.SenderName) !== null && _a !== void 0 ? _a : party === null || party === void 0 ? void 0 : party.ReceiverName) !== null && _b !== void 0 ? _b : party === null || party === void 0 ? void 0 : party.senderName) !== null && _c !== void 0 ? _c : party === null || party === void 0 ? void 0 : party.receiverName)}</div></div>
                    <div class="mb-1 text-muted">${safe(party === null || party === void 0 ? void 0 : party.Contact)}</div>
                    <div class="mb-1"><i class="far fa-envelope me-2 text-secondary"></i>${safe(party === null || party === void 0 ? void 0 : party.Email)}</div>
                    <div class="mb-1"><i class="fas fa-phone me-2 text-secondary"></i>${safe(party === null || party === void 0 ? void 0 : party.Phone)}</div>
                    <div class="mb-1 text-muted"><strong>City:</strong> ${safe(party === null || party === void 0 ? void 0 : party.CityName)}</div>
                    <div class="mb-1 text-muted"><strong>Country:</strong> ${safe(party === null || party === void 0 ? void 0 : party.CountryName)}</div>
                    <div class="mt-2 text-muted">${safe((_d = party === null || party === void 0 ? void 0 : party.Address) !== null && _d !== void 0 ? _d : party === null || party === void 0 ? void 0 : party.OtherAddress)}</div>
                </div>
            </div>
        `;
    }
    function renderExtras(dto) {
        const extras = document.getElementById('show-extras');
        if (!extras)
            return;
        let html = '';
        if (dto.TbShippmentStatuses && dto.TbShippmentStatuses.length) {
            const rows = dto.TbShippmentStatuses.map(s => {
                var _a, _b;
                return `
                <tr>
                    <td>${fmtDate(s.CreatedDate)}</td>
                    <td>${(s.CurrentState === 1) ? 'Active' : (s.CurrentState === 0 ? 'Inactive' : safe(s.CurrentState))}</td>
                    <td>${safe((_a = s.Notes) !== null && _a !== void 0 ? _a : s.Description)}</td>
                    <td>${safe((_b = s.UpdatedBy) !== null && _b !== void 0 ? _b : s.CreatedBy)}</td>
                </tr>`;
            }).join('');
            html += `
                <div class="card mt-3 shadow-sm extras-card">
                    <div class="card-header"><strong>Status History</strong></div>
                    <div class="card-body p-0">
                        <div class="table-responsive">
                            <table class="table table-sm mb-0">
                                <thead class="table-light"><tr><th>Time</th><th>State</th><th>Notes</th><th>By</th></tr></thead>
                                <tbody>${rows}</tbody>
                            </table>
                        </div>
                    </div>
                </div>`;
        }
        html += `
            <div class="card mt-3 shadow-sm extras-card">
                <div class="card-header"><strong>Notes</strong></div>
                <div class="card-body small">${dto.OtherNotes ? dto.OtherNotes : '<span class="text-muted">—</span>'}</div>
            </div>`;
        extras.innerHTML = html;
    }
    function getIdFromUrl() {
        const parts = location.pathname.split('/').filter(Boolean);
        const last = parts[parts.length - 1] || '';
        if (last && /[0-9a-fA-F\-]{36}/.test(last))
            return last;
        return new URLSearchParams(window.location.search).get('id');
    }
    function loadAndRender(serverDto_1) {
        return __awaiter(this, arguments, void 0, function* (serverDto, options = {}) {
            var _a, _b;
            try {
                const shipmentId = (serverDto && serverDto.Id) ? String(serverDto.Id) : getIdFromUrl();
                let dto = null;
                if (serverDto)
                    dto = serverDto;
                else if (shipmentId && window.ShipmentApiClient && typeof ShipmentApiClient.getById === 'function')
                    dto = yield ShipmentApiClient.getById(shipmentId);
                else if (shipmentId) {
                    const resp = yield fetch('/api/Shipments/' + encodeURIComponent(shipmentId), { credentials: 'same-origin' });
                    if (!resp.ok)
                        dto = null;
                    else {
                        const payload = yield resp.json();
                        dto = tolerant(payload);
                    }
                }
                const editLink = document.getElementById('editLink');
                const deleteFormId = document.getElementById('deleteForm_Id');
                const deleteBtn = document.getElementById('btnDelete') || document.querySelector('.btn-delete');
                if (!dto) {
                    const shipEl = document.getElementById('shipment-block');
                    if (shipEl)
                        shipEl.innerHTML = '<div class="alert alert-warning">Shipment not found.</div>';
                    if (document.getElementById('sender-block'))
                        document.getElementById('sender-block').innerHTML = '';
                    if (document.getElementById('receiver-block'))
                        document.getElementById('receiver-block').innerHTML = '';
                    if (document.getElementById('show-extras'))
                        document.getElementById('show-extras').innerHTML = '';
                    if (deleteBtn)
                        deleteBtn.disabled = true;
                    return;
                }
                // ✅ SIMPLE FIX: Always set href (like before)
                if (editLink)
                    editLink.href = '/Shipments/Edit/' + encodeURIComponent(dto.Id);
                if (deleteFormId)
                    deleteFormId.value = dto.Id;
                const trackingSmall = document.getElementById('trackingSmall');
                if (trackingSmall)
                    trackingSmall.textContent = '#' + ((_b = (_a = dto.TrackingNumber) !== null && _a !== void 0 ? _a : dto.ReferenceId) !== null && _b !== void 0 ? _b : '-');
                renderShipmentBlock(dto);
                renderPartyBlock('sender-block', 'Sender', dto.UserSender || dto.userSender || {});
                renderPartyBlock('receiver-block', 'Receiver', dto.UserReceiver || dto.userReceiver || {});
                renderExtras(dto);
                // ✅ DELETE BUTTON HANDLER (unchanged)
                if (deleteBtn) {
                    try {
                        deleteBtn.removeEventListener('click', deleteBtn._handler);
                    }
                    catch (_c) { }
                    deleteBtn._handler = function (e) {
                        e.preventDefault();
                        const id = deleteBtn.getAttribute('data-id') || (deleteFormId === null || deleteFormId === void 0 ? void 0 : deleteFormId.value) || shipmentId;
                        if (!id) {
                            //console.error('Missing shipment id');
                            alert('Missing shipment ID');
                            return;
                        }
                        if (!window.ShipmentService || typeof ShipmentService.deleteShipment !== 'function') {
                            //console.error('ShipmentService.deleteShipment not available');
                            alert('Delete service not available');
                            return;
                        }
                        const deleteRedirectUrl = options.redirectUrl
                            ? (options.redirectUrl.includes('?')
                                ? `${options.redirectUrl}&deleted=1&deletedId=${encodeURIComponent(id)}`
                                : `${options.redirectUrl}?deleted=1&deletedId=${encodeURIComponent(id)}`)
                            : `/Shipments/List?deleted=1&deletedId=${encodeURIComponent(id)}`;
                        ShipmentService.deleteShipment(id, {
                            button: deleteBtn,
                            skipConfirm: false,
                            redirectUrl: deleteRedirectUrl,
                            redirectDelayMs: 1500,
                            refreshList: false,
                            onSuccess: function (result) {
                                console.log('✅ Shipment deleted successfully:', result);
                            },
                            onError: function (err) {
                                console.error('❌ Delete failed:', err);
                            }
                        }).catch(err => {
                            console.error('Delete error:', err);
                        });
                    };
                    deleteBtn.addEventListener('click', deleteBtn._handler);
                }
                // ✅ APPROVE BUTTON HANDLER (unchanged - keep existing code)
                // ... existing approve button logic ...
            }
            catch (err) {
                //console.error('loadAndRender error', err);
                const shipEl = document.getElementById('shipment-block');
                if (shipEl)
                    shipEl.innerHTML = '<div class="alert alert-danger">Error loading details.</div>';
            }
        });
    }
    ShipmentShow.init = function (serverDto, options) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', function () {
                loadAndRender(serverDto, options || {});
            });
        }
        else {
            // DOM already loaded
            loadAndRender(serverDto, options || {});
        }
    };
    window.ShipmentShow = window.ShipmentShow || ShipmentShow;
})();
//# sourceMappingURL=ShipmentShow.js.map