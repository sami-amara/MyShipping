/*
 * Payment transactions page helpers.
 * Handles receipt print action and lightweight diagnostics hooks.
 */
(function () {
    function initPaymentTransactionPages() {
        var printButton = document.getElementById('print-receipt-button');
        if (printButton) {
            printButton.addEventListener('click', function () {
                window.print();
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initPaymentTransactionPages);
    } else {
        initPaymentTransactionPages();
    }
})();
