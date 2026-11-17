


(function () {
    'use strict';

    // Handles the admin "Reactivate" button on the Deactivated Accounts page.
    // Texts and the POST url come from data-* attributes on #deactivatedUsersTable,
    // so nothing is hard-coded here and the dialog follows the page language.
    const DeactivatedUsers = {
        init: function () {
            $(document).on('click', '.reactivate-user', this.onReactivateClick.bind(this));
        },

        onReactivateClick: function (e) {
            e.preventDefault();

            const button = $(e.currentTarget);
            const userId = button.data('user-id');
            const userEmail = button.attr('data-user-email');
            const table = $('#deactivatedUsersTable');
            const url = table.attr('data-reactivate-url');

            if (!userId || !userEmail || !url) {
                console.error('Reactivate: missing user id, email or url.');
                return;
            }

            // attr() instead of data(): data() tries to parse values that look like JSON
            const title = table.attr('data-confirm-title') || 'Reactivate user account?';
            const text = (table.attr('data-confirm-text') || 'Are you sure you want to reactivate {0}?')
                .replace('{0}', userEmail);
            const confirmButton = table.attr('data-confirm-button') || 'Yes, reactivate';

            // showAlert.Confirm renders plain text (not HTML), so no markup in the message
            showAlert.Confirm(title, text, confirmButton, (result) => {
                if (result && result.isConfirmed) {
                    this.submit(userId, url);
                }
            });
        },

        submit: function (userId, url) {
            const token = $('input[name="__RequestVerificationToken"]').first().val();

            const form = $('<form>', { method: 'POST', action: url });
            form.append($('<input>', { type: 'hidden', name: 'userId', value: userId }));

            if (token) {
                form.append($('<input>', { type: 'hidden', name: '__RequestVerificationToken', value: token }));
            } else {
                console.warn('Reactivate: antiforgery token not found, the request will be rejected.');
            }

            $('body').append(form);
            form.get(0).submit();
        }
    };

    $(function () { DeactivatedUsers.init(); });
})();


















