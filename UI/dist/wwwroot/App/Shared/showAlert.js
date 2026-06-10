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
        }
    };
})();
//# sourceMappingURL=showAlert.js.map