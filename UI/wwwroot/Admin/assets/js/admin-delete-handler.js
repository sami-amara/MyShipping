/*
 * Admin delete handler bootstrap.
 * Reads delete config from #admin-delete-config and wires confirmation + redirect.
 */
(function () {
    function bindDeleteHandlers() {
        var configElement = document.getElementById('admin-delete-config');
        if (!configElement) {
            return;
        }

        var selector = configElement.dataset.selector || '.btn-danger';
        var deleteUrl = configElement.dataset.deleteUrl;

        if (!deleteUrl || typeof showAlert === 'undefined' || !showAlert.ConfirmDelete) {
            return;
        }

        var buttons = document.querySelectorAll(selector);
        buttons.forEach(function (button) {
            button.addEventListener('click', function () {
                var id = button.dataset.id;
                if (!id) {
                    return;
                }

                showAlert.ConfirmDelete(function (result) {
                    if (result) {
                        window.location.href = deleteUrl + '/' + id;
                    }
                });
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bindDeleteHandlers);
    } else {
        bindDeleteHandlers();
    }
})();
