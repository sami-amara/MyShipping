/* eslint-disable no-undef */
// ═══════════════════════════════════════════════════════════════
// AdminListShipmentsSignalR.js
// Listens for real-time shipment status changes and patches the
// status and payment badges on the admin list page without reload.
// ═══════════════════════════════════════════════════════════════
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
    // ── Badge map: mirrors ShipmentHelpers.GetStatusBadge in C# ────────────
    const STATUS_BADGES = {
        0: { cls: 'badge bg-secondary', icon: 'mdi-delete', text: 'Deleted' },
        1: { cls: 'badge bg-warning text-dark', icon: 'mdi-file-document-outline', text: 'Created' },
        2: { cls: 'badge bg-info text-dark', icon: 'mdi-update', text: 'Updated' },
        3: { cls: 'badge bg-primary', icon: 'mdi-check-circle', text: 'Approved' },
        4: { cls: 'badge bg-info text-dark', icon: 'mdi-package-variant', text: 'Ready for Shipping' },
        5: { cls: 'badge bg-primary', icon: 'mdi-truck-delivery', text: 'Shipped' },
        6: { cls: 'badge bg-success', icon: 'mdi-inbox-arrow-down', text: 'Delivered' },
        7: { cls: 'badge bg-danger', icon: 'mdi-close-circle', text: 'Cancelled' },
        8: { cls: 'badge bg-dark', icon: 'mdi-keyboard-return', text: 'Returned' },
        9: { cls: 'badge bg-secondary', icon: 'mdi-cash-refund', text: 'Refunded' }
    };
    function buildStatusBadge(newState) {
        const def = STATUS_BADGES[newState] || { cls: 'badge bg-secondary', icon: 'mdi-help-circle', text: String(newState) };
        return `<span class="${def.cls}" style="display:inline-flex;align-items:center;gap:4px;justify-content:center;">` +
            `<i class="mdi ${def.icon}"></i>${def.text}</span>`;
    }
    // Payment badge: Active (paid) or Inactive (not paid / terminal state)
    function buildPaymentBadge(isPaid) {
        if (isPaid) {
            return `<span class="badge bg-success"><i class="mdi mdi-check-circle"></i> Active</span>`;
        }
        return `<span class="badge bg-secondary"><i class="mdi mdi-close-circle-outline"></i> Inactive</span>`;
    }
    function patchRow(shipmentId, newState, isPaid) {
        const row = document.querySelector(`tr[data-id="${shipmentId}"]`);
        if (!row)
            return;
        // Payment badge is in the 7th <td> (index 6)
        const paymentCell = row.cells[6];
        if (paymentCell) {
            paymentCell.innerHTML = buildPaymentBadge(isPaid);
        }
        // Status badge is in the 8th <td> (index 7) — wrapped in a div
        const statusCell = row.cells[7];
        if (statusCell) {
            const wrapper = statusCell.querySelector('div') || statusCell;
            wrapper.innerHTML = buildStatusBadge(newState);
        }
    }
    // ── Wire SignalR once SignalRClient is ready ─────────────────────────────
    function init() {
        return __awaiter(this, void 0, void 0, function* () {
            if (typeof SignalRClient === 'undefined') {
                console.warn('AdminListShipmentsSignalR: SignalRClient not available');
                return;
            }
            const started = yield SignalRClient.start();
            if (!started) {
                console.warn('AdminListShipmentsSignalR: SignalR could not connect');
                return;
            }
            SignalRClient.connection.on('ShipmentStatusUpdated', function (data) {
                if (!data || !data.shipmentId)
                    return;
                patchRow(data.shipmentId, data.newState, data.isPaid);
            });
            console.log('AdminListShipmentsSignalR: listening for ShipmentStatusUpdated');
        });
    }
    // Start after DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    }
    else {
        init();
    }
}());
//# sourceMappingURL=AdminListShipmentsSignalR.js.map