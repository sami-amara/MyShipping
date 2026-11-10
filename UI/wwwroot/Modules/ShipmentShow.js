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
        const el = document.getElementById('shipment-block');
        if (!el) return;
        el.innerHTML = `
            <div class="card shadow-sm border-0 shipment-card">
                <div class="card-header"><strong>Shipment</strong></div>
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-start mb-2">
                        <div>
                            <div class="shipment-header-title mb-1"><strong>${safe(dto.TrackingNumber ?? dto.ReferenceId)}</strong></div>
                            <div class="small text-muted"><strong>#${safe(dto.ReferenceId ?? '')}</strong></div>
                        </div>
                        <div class="text-end">
                            <span class="badge ${(dto.CurrentState === 1 || (dto.Status && String(dto.Status).toLowerCase() === 'active')) ? 'bg-success' : 'bg-secondary'}">
                                <strong>${dto.Status ?? (dto.CurrentState === 1 ? 'Active' : dto.CurrentState === 0 ? 'Inactive' : 'Unknown')}</strong>
                            </span>
                        </div>
                    </div>
                    <div class="mt-3">
                        <div class="shipment-prop-row">
                        <strong>Created By:</strong>
                        <strong class="ms-2 value">
                            ${safe(dto.CreatedByUserName ?? dto.CreatedBy)} 
                            ${dto.CreatedDate ? `<small class="text-muted">(${fmtDate(dto.CreatedDate)})</small>` : ''}
                            </strong>
                            </div>
                            <div class="shipment-prop-row">
                            <strong>Delivery Date:</strong>
                            <strong class="ms-2 value">
                                ${dto.DelivryDate && !dto.DelivryDate.startsWith('0001')
                                        ? fmtDate(dto.DelivryDate)
                                        : '<span class="text-muted">Pending Delivery</span>'}
                            </strong>
                        </div>
                        <div class="shipment-prop-row"><strong>Shipping Date:</strong> <strong class="ms-2 value">${fmtDate(dto.ShippingDate)}</strong></div>
                        <div class="shipment-prop-row"><strong>Package Value:</strong> <strong class="ms-2 value">${fmtCurrency(dto.PackageValue)}</strong></div>
                        <div class="shipment-prop-row"><strong>Shipping Rate:</strong> <strong class="ms-2 value">${fmtCurrency(dto.ShippingRate)}</strong></div>
                        <div class="shipment-prop-row"><strong>Weight (kg):</strong> <strong class="ms-2 value">${safe(dto.Weight)}</strong></div>
                        <div class="shipment-prop-row"><strong>Dimensions (W×H×L cm):</strong> <strong class="ms-2 value">${safe(dto.Width)} × ${safe(dto.Height)} × ${safe(dto.Length)}</strong></div>
                        <div class="shipment-prop-row"><strong>Packaging:</strong> <strong class="ms-2 value">${safe(dto.PackagingName ?? dto.ShipingPackgingId)}</strong></div>
                        <div class="shipment-prop-row"><strong>Shipping Type:</strong> <strong class="ms-2 value">${safe(dto.ShippingTypeName ?? dto.ShippingTypeId)}</strong></div>
                          <div class="shipment-prop-row">
                            <strong> Payment Mothod:</strong>
                            <strong class="ms-2 value">
                                ${(dto.PaymentMethodName ?? dto.PaymentMethodName) ? `
                                    ${safe(dto.PaymentMethodName ?? dto.PaymentMethodName)} 
                                    ${dto.ApprovedAt ? `<small class="text-muted">(${fmtDate(dto.ApprovedAt)})</small>` : ''}
                                ` : `<span class="text-muted fst-italic">No Payment yet</span>`}
                            </strong>
                        </div>
                        

                            <!-- Carrier -->
                        <div class="shipment-prop-row">
                            <strong>Carrier:</strong>
                            <strong class="ms-2 value">
                                ${dto.CarrierName ?? (dto.CarrierName
                                    ? safe(dto.CarrierName)
                                    : `<span class="text-muted fst-italic">${dto.CarrierName ? 'Carrier not assigned yet' : 'Not approved yet'}</span>`)}
                            </strong>
                        </div>
                        <!-- Approved By -->
                        <div class="shipment-prop-row">
                            <strong>Approved By:</strong>
                            <strong class="ms-2 value">
                                ${(dto.ApprovedByUserName ?? dto.ApprovedBy) ? `
                                    ${safe(dto.ApprovedByUserName ?? dto.ApprovedBy)} 
                                    ${dto.ApprovedAt ? `<small class="text-muted">(${fmtDate(dto.ApprovedAt)})</small>` : ''}
                                ` : `<span class="text-muted fst-italic">Not approved yet</span>`}
                            </strong>
                        </div>
                        <!-- Ready For Shipping By -->
                        <div class="shipment-prop-row">
                            <strong>Ready For Shipping By:</strong> 
                            <strong class="ms-2 value">
                                ${(dto.ReadyByUserName ?? dto.ReadyBy) ? `
                                    ${safe(dto.ReadyByUserName ?? dto.ReadyBy)} 
                                    ${dto.ReadyAt ? `<small class="text-muted">(${fmtDate(dto.ReadyAt)})</small>` : ''}
                                ` : `<span class="text-muted fst-italic">Shipment not ready for shipping yet</span>`}
                            </strong>
                        </div>
                        <!-- Shipped By -->
                        <div class="shipment-prop-row">
                            <strong>Shipped By:</strong> 
                            <strong class="ms-2 value">
                                ${(dto.ShippedByUserName ?? dto.ShippedBy) ? `
                                    ${safe(dto.ShippedByUserName ?? dto.ShippedBy)} 
                                    ${dto.ShippedAt ? `<small class="text-muted">(${fmtDate(dto.ShippedAt)})</small>` : ''}
                                ` : `<span class="text-muted fst-italic">Not shipped yet</span>`}
                            </strong>
                        </div>
                        <!-- Delivered By -->
                        <div class="shipment-prop-row">
                            <strong>Delivered By:</strong> 
                            <strong class="ms-2 value">
                                ${(dto.DeliveredByUserName ?? dto.DeliveredBy) ? `
                                    ${safe(dto.DeliveredByUserName ?? dto.DeliveredBy)} 
                                    ${dto.DeliveredAt ? `<small class="text-muted">(${fmtDate(dto.DeliveredAt)})</small>` : ''}
                                ` : `<span class="text-muted fst-italic">Not delivered yet</span>`}
                            </strong>
                        </div>
                        <!-- Cancelled By (Only displays if shipment was actually cancelled) -->
                        ${(dto.CancelledByUserName || dto.CancelledBy) ? `
                            <div class="shipment-prop-row text-danger">
                                <strong>Cancelled By:</strong>
                                <strong class="ms-2 value">
                                    ${safe(dto.CancelledByUserName ?? dto.CancelledBy)} 
                                    ${dto.CancelledAt ? `<small>(${fmtDate(dto.CancelledAt)})</small>` : ''}
                                </strong>
                                ${dto.CancellationReason ? `<div class="small text-muted ms-2">Reason: ${safe(dto.CancellationReason)}</div>` : ''}
                            </div>
                        ` : ''}
                         <!-- Deleted By (Only displays if shipment was actually Deleted) -->
                        ${(dto.DeletedByUserName || dto.DeletedBy) ? `
                            <div class="shipment-prop-row text-danger">
                                <strong>Deleted By:</strong>
                                <strong class="ms-2 value">
                                    ${safe(dto.DeletedByUserName ?? dto.DeletedBy)}
                                    ${dto.DeletedDate ? `<small>(${fmtDate(dto.DeletedDate)})</small>` : ''}
                                </strong>
                            
                            </div>
                        ` : ''}

                               <!-- Refunded By (Only displays if shipment was actually Refunded) -->
                                   ${(dto.RefundedByUserName || dto.RefundedBy) ? `
                            <div class="shipment-prop-row text-danger">
                                <strong>Refunded By:</strong>
                                <strong class="ms-2 value">
                                    ${safe(dto.RefundedByUserName ?? dto.RefundedBy)}
                                    ${dto.RefundedAt ? `<small>(${fmtDate(dto.RefundedAt)})</small>` : ''}
                                </strong>
                                ${dto.RefundReason ? `<div class="small text-muted ms-2">Reason: ${safe(dto.RefundReason)}</div>` : ''}
                            </div>
                        ` : ''}
                     
                        <!-- Returned By (Only displays if shipment was actually returned) -->
                        ${(dto.ReturnedByUserName || dto.ReturnedBy) ? `
                            <div class="shipment-prop-row text-warning">
                                <strong>Returned By:</strong>
                                <strong class="ms-2 value">
                                    ${safe(dto.ReturnedByUserName ?? dto.ReturnedBy)} 
                                    ${dto.ReturnedAt ? `<small>(${fmtDate(dto.ReturnedAt)})</small>` : ''}
                                </strong>
                                ${dto.ReturnReason ? `<div class="small text-muted ms-2">Reason: ${safe(dto.ReturnReason)}</div>` : ''}
                            </div>
                        ` : ''}
                     
                        <div class="shipment-prop-row"><strong>Tracking Number:</strong> <strong class="ms-2 value">${safe(dto.TrackingNumber)}</strong></div
                </div>
            </div>
        `;
    }

    function renderPartyBlock(containerId, title, party) {
        const el = document.getElementById(containerId);
        if (!el) return;
        el.innerHTML = `
            <div class="card shadow-sm border-0 h-100 party-card">
                <div class="card-header"><strong>${title}</strong></div>
                <div class="card-body">
                    <div class="mb-2"><div class="fw-bold">${safe(party?.SenderName ?? party?.ReceiverName ?? party?.senderName ?? party?.receiverName)}</div></div>
                    <div class="mb-1 text-muted">${safe(party?.Contact)}</div>
                    <div class="mb-1"><i class="far fa-envelope me-2 text-secondary"></i>${safe(party?.Email)}</div>
                    <div class="mb-1"><i class="fas fa-phone me-2 text-secondary"></i>${safe(party?.Phone)}</div>
                    <div class="mb-1 text-muted"><strong>City:</strong> ${safe(party?.CityName)}</div>
                    <div class="mb-1 text-muted"><strong>Country:</strong> ${safe(party?.CountryName)}</div>
                    <div class="mt-2 text-muted">${safe(party?.Address ?? party?.OtherAddress)}</div>
                </div>
            </div>
        `;
    }

    function renderExtras(dto) {
        const extras = document.getElementById('show-extras');
        if (!extras) return;
        let html = '';
        if (dto.TbShippmentStatuses && dto.TbShippmentStatuses.length) {
            const rows = dto.TbShippmentStatuses.map(s => `
                <tr>
                    <td>${fmtDate(s.CreatedDate)}</td>
                    <td>${(s.CurrentState === 1) ? 'Active' : (s.CurrentState === 0 ? 'Inactive' : safe(s.CurrentState))}</td>
                    <td>${safe(s.Notes ?? s.Description)}</td>
                    <td>${safe(s.UpdatedBy ?? s.CreatedBy)}</td>
                </tr>`).join('');
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
        if (last && /[0-9a-fA-F\-]{36}/.test(last)) return last;
        return new URLSearchParams(window.location.search).get('id');
    }

    async function loadAndRender(serverDto, options = {}) {
        try {
            const shipmentId = (serverDto && serverDto.Id) ? String(serverDto.Id) : getIdFromUrl();
            let dto = null;

            if (serverDto) dto = serverDto;
            else if (shipmentId && window.ShipmentApiClient && typeof ShipmentApiClient.getById === 'function') dto = await ShipmentApiClient.getById(shipmentId);
            else if (shipmentId) {
                const resp = await fetch('/api/Shipments/' + encodeURIComponent(shipmentId), { credentials: 'same-origin' });
                if (!resp.ok) dto = null;
                else { const payload = await resp.json(); dto = tolerant(payload); }
            }

            const editLink = document.getElementById('editLink');
            const deleteFormId = document.getElementById('deleteForm_Id');
            const deleteBtn = document.getElementById('btnDelete') || document.querySelector('.btn-delete');

            if (!dto) {
                const shipEl = document.getElementById('shipment-block');
                if (shipEl) shipEl.innerHTML = '<div class="alert alert-warning">Shipment not found.</div>';
                if (document.getElementById('sender-block')) document.getElementById('sender-block').innerHTML = '';
                if (document.getElementById('receiver-block')) document.getElementById('receiver-block').innerHTML = '';
                if (document.getElementById('show-extras')) document.getElementById('show-extras').innerHTML = '';
                if (deleteBtn) deleteBtn.disabled = true;
                return;
            }

            // ✅ SIMPLE FIX: Always set href (like before)
            if (editLink) editLink.href = '/Shipments/Edit/' + encodeURIComponent(dto.Id);

            if (deleteFormId) deleteFormId.value = dto.Id;
            const trackingSmall = document.getElementById('trackingSmall');
            if (trackingSmall) trackingSmall.textContent = '#' + (dto.TrackingNumber ?? dto.ReferenceId ?? '-');

            renderShipmentBlock(dto);
            renderPartyBlock('sender-block', 'Sender', dto.UserSender || dto.userSender || {});
            renderPartyBlock('receiver-block', 'Receiver', dto.UserReceiver || dto.userReceiver || {});
            renderExtras(dto);

            // ✅ DELETE BUTTON HANDLER (unchanged)
            if (deleteBtn) {
                try { deleteBtn.removeEventListener('click', deleteBtn._handler); } catch { }

                deleteBtn._handler = function (e) {
                    e.preventDefault();

                    const id = deleteBtn.getAttribute('data-id') || deleteFormId?.value || shipmentId;

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

        } catch (err) {
            //console.error('loadAndRender error', err);
            const shipEl = document.getElementById('shipment-block');
            if (shipEl) shipEl.innerHTML = '<div class="alert alert-danger">Error loading details.</div>';
        }
    }




    ShipmentShow.init = function (serverDto, options) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', function () {
                loadAndRender(serverDto, options || {});
            });
        } else {
            // DOM already loaded
            loadAndRender(serverDto, options || {});
        }
    };

    window.ShipmentShow = window.ShipmentShow || ShipmentShow;
})();
