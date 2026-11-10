(function () {
    if (window.showAlert)
        return;
    window.showAlert = {
        ConfirmDelete: function (callBack, cancelCallBack) {
            const alerts = window.AppResourceAlerts || {};
            Swal.fire({
                title: alerts.confirmDeleteTitle || 'Are you sure?',
                text: alerts.confirmDeleteText || alerts.confirmDelete || "You won't be able to revert this!",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: alerts.confirmDeleteButton || 'Yes, delete it!',
                cancelButtonText: alerts.cancel || 'Cancel'
            }).then((result) => {
                if (result.isConfirmed) {
                    if (typeof callBack === 'function')
                        callBack(result);
                }
                else {
                    if (typeof cancelCallBack === 'function')
                        cancelCallBack(result);
                }
            });
        },
        Confirm: function (title, text, confirmButtonText, callBack, cancelCallBack) {
            const alerts = window.AppResourceAlerts || {};
            Swal.fire({
                title: title || 'Are you sure?',
                text: text || '',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: confirmButtonText || 'Yes Lock ',
                cancelButtonText: alerts.cancel || 'Cancel'
            }).then((result) => {
                if (result.isConfirmed) {
                    if (typeof callBack === 'function')
                        callBack(result);
                }
                else {
                    if (typeof cancelCallBack === 'function')
                        cancelCallBack(result);
                }
            });
        },
        Success: function (title, text) {
            Swal.fire({
                title: title,
                text: text,
                icon: 'success'
            });
        },
        Error: function (title, text) {
            Swal.fire({
                title: title,
                text: text,
                icon: 'error'
            });
        },
        // ═══════════════════════════════════════════════════════════════
        // 🆕 BOOTSTRAP 5 TOAST NOTIFICATIONS (Non-blocking, Auto-dismiss)
        // ═══════════════════════════════════════════════════════════════
        /**
         * Show a toast notification with Bootstrap 5
         * @param {string} type - 'success', 'error', 'warning', 'info'
         * @param {string} message - The message to display
         * @param {number} duration - Auto-dismiss time in ms (0 = manual only)
         * @param {string} title - Optional title (default based on type)
         */
        Notify: function (type, message, duration = 5000, title = null) {
            const toastId = 'toast-' + Date.now();
            const typeMap = {
                'success': { class: 'success', icon: '✅', title: 'Success' },
                'error': { class: 'danger', icon: '❌', title: 'Error' },
                'warning': { class: 'warning', icon: '⚠️', title: 'Warning' },
                'info': { class: 'info', icon: 'ℹ️', title: 'Info' }
            };
            const config = typeMap[type.toLowerCase()] || typeMap['info'];
            const finalTitle = title || config.title;
            const toastHtml = `
                <div id="${toastId}" class="toast show align-items-center border-0 bg-${config.class} text-white" 
                     role="alert" aria-live="assertive" aria-atomic="true"
                     style="position: fixed; top: 20px; right: 20px; z-index: 9999; min-width: 300px; animation: slideIn 0.3s ease-out;">
                    <div class="d-flex">
                        <div class="toast-body">
                            <strong>${config.icon} ${finalTitle}</strong><br/>
                            <small>${message}</small>
                        </div>
                        <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                    </div>
                </div>
            `;
            document.body.insertAdjacentHTML('beforeend', toastHtml);
            const toastElement = document.getElementById(toastId);
            const toast = new bootstrap.Toast(toastElement);
            toast.show();
            // Auto-dismiss after duration
            if (duration > 0) {
                setTimeout(() => {
                    toast.hide();
                    setTimeout(() => toastElement.remove(), 150);
                }, duration);
            }
        },
        /**
         * Shortcuts for common notifications
         */
        Success: function (title, text, options = {}) {
            if (!text) {
                // If called with new style: showAlert.Success('message')
                this.Notify('success', title, options.duration || 5000, options.title || 'Success');
            }
            else {
                // If called with old style: showAlert.Success('title', 'text')
                Swal.fire({
                    title: title,
                    text: text,
                    icon: 'success'
                });
            }
        },
        Error: function (title, text, options = {}) {
            if (!text) {
                // If called with new style: showAlert.Error('message')
                this.Notify('error', title, options.duration || 5000, options.title || 'Error');
            }
            else {
                // If called with old style: showAlert.Error('title', 'text')
                Swal.fire({
                    title: title,
                    text: text,
                    icon: 'error'
                });
            }
        },
        Warning: function (title, text, options = {}) {
            if (!text) {
                this.Notify('warning', title, options.duration || 5000, options.title || 'Warning');
            }
            else {
                Swal.fire({
                    title: title,
                    text: text,
                    icon: 'warning'
                });
            }
        },
        Info: function (title, text, options = {}) {
            if (!text) {
                this.Notify('info', title, options.duration || 5000, options.title || 'Info');
            }
            else {
                Swal.fire({
                    title: title,
                    text: text,
                    icon: 'info'
                });
            }
        }
    };
    // Add CSS for toast animation
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideIn {
            from { transform: translateX(400px); opacity: 0; }
            to { transform: translateX(0); opacity: 1; }
        }
        .bg-success { background-color: #198754 !important; }
        .bg-danger { background-color: #dc3545 !important; }
        .bg-warning { background-color: #ffc107 !important; }
        .bg-info { background-color: #0dcaf0 !important; }
    `;
    document.head.appendChild(style);
    // Connect to the SignalR AdminNotificationHub
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/adminNotificationHub") // must match the MapHub route in Program.cs
        .build();
    // Listen for ShowAlert events from the server
    connection.on("ShowAlert", function (alert) {
        var _a;
        // Use your existing generic ShowAlert functions
        // Severity mapping: danger -> error, warning -> warning, info -> info, success -> success
        switch ((_a = alert.Severity) === null || _a === void 0 ? void 0 : _a.toLowerCase()) {
            case "danger":
            case "error":
                window.showAlert.Error(alert.Title, alert.Message);
                break;
            case "warning":
                window.showAlert.Warning(alert.Title, alert.Message);
                break;
            case "info":
                window.showAlert.Info(alert.Title, alert.Message);
                break;
            case "success":
                window.showAlert.Success(alert.Title, alert.Message);
                break;
            default:
                window.showAlert.Info(alert.Title, alert.Message);
                break;
        }
    });
    // Start the connection
    connection.start()
        .then(() => console.log("✅ Connected to AdminNotificationHub"))
        .catch(err => console.error("❌ SignalR connection error:", err));
})();
//# sourceMappingURL=showAlert.js.map