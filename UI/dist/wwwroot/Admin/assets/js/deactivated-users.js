(function () {
    'use strict';
    /**
     * DeactivatedUsers - Handles reactivation of deactivated user accounts
     */
    const DeactivatedUsers = {
        /**
         * Initialize event handlers
         */
        init: function () {
            // Attach click handler to all reactivate buttons
            $(document).on('click', '.reactivate-user', this.onReactivateClick.bind(this));
            console.log('✅ DeactivatedUsers initialized');
        },
        /**
         * Handle reactivate button click
         */
        onReactivateClick: function (e) {
            e.preventDefault();
            const button = $(e.currentTarget);
            const userId = button.data('user-id');
            const userEmail = button.data('user-email');
            if (!userId || !userEmail) {
                console.error('❌ Missing userId or userEmail');
                alert('Error: Unable to identify user');
                return;
            }
            // Show confirmation dialog
            this.showReactivationConfirmation(userId, userEmail);
        },
        /**
         * Show confirmation dialog using SweetAlert2
         */
        showReactivationConfirmation: function (userId, userEmail) {
            showAlert.Confirm('Reactivate User Account?', `Are you sure you want to reactivate <strong>${this.escapeHtml(userEmail)}</strong>?<br>
                 This user will be able to login again.`, 'Yes, Reactivate', (result) => {
                // ✅ FIXED: Properly check SweetAlert2 result
                if (result && result.isConfirmed) {
                    this.submitReactivationRequest(userId);
                }
            });
        },
        /**
      * Submit POST request to reactivate user
      */
        submitReactivationRequest: function (userId) {
            // ✅ FIXED: Use correct hardcoded URL: /admin/Users/ReactivateUser
            const form = $('<form>', {
                'method': 'POST',
                'action': '/admin/Users/ReactivateUser'
            });
            // /**
            //  * Submit POST request to reactivate user
            //  */
            // submitReactivationRequest: function (userId) {
            //     const form = $('<form>', {
            //         'method': 'POST',
            //         'action': '@Url.Action("ReactivateUser", "Users", new { area = "admin" })'
            //     });
            // Add form fields
            form.append($('<input>', {
                'type': 'hidden',
                'name': 'userId',
                'value': userId
            }));
            form.append($('<input>', {
                'type': 'hidden',
                'name': 'reason',
                'value': 'Admin reactivation'
            }));
            // ✅ FIXED: Get CSRF token from cookie or page
            const csrfToken = this.getAntiForgeryToken();
            if (csrfToken) {
                form.append($('<input>', {
                    'type': 'hidden',
                    'name': '__RequestVerificationToken',
                    'value': csrfToken
                }));
            }
            // Submit form
            $('body').append(form);
            form.submit();
        },
        /**
         * Get CSRF token from page
         */
        getAntiForgeryToken: function () {
            // Try to get from input field first
            let token = $('input[name="__RequestVerificationToken"]').val();
            if (!token) {
                // Try to get from cookie
                const cookies = document.cookie.split(';');
                for (let i = 0; i < cookies.length; i++) {
                    const cookie = cookies[i].trim();
                    if (cookie.startsWith('XSRF-TOKEN=')) {
                        token = cookie.substring('XSRF-TOKEN='.length);
                        break;
                    }
                }
            }
            if (!token) {
                console.warn('⚠️ CSRF token not found - form submission may fail');
            }
            return token;
        },
        /**
         * Escape HTML to prevent XSS
         */
        escapeHtml: function (text) {
            const map = {
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#039;'
            };
            return text.replace(/[&<>"']/g, m => map[m]);
        }
    };
    // Initialize when document is ready
    $(document).ready(function () {
        DeactivatedUsers.init();
    });
})();
//# sourceMappingURL=deactivated-users.js.map