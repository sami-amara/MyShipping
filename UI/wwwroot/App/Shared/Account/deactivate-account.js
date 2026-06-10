(function () {
    'use strict';

    /**
     * DeactivateAccount - Handles user account deactivation with confirmation dialog
     */
    const DeactivateAccount = {
        config: {
            buttonId: '#btn-deactivate-account',
            apiUrl: '/Account/DeactivateAccount',
            debug: true
        },

        /**
         * Initialize event handlers
         */
        init: function () {
            const button = $(this.config.buttonId);

            if (button.length === 0) {
                if (this.config.debug) console.warn('❌ Deactivate button not found:', this.config.buttonId);
                return;
            }

            // Use document delegation for safety
            $(document).on('click', this.config.buttonId, (e) => this.onDeactivateClick(e));

            if (this.config.debug) {
                console.log('✅ DeactivateAccount initialized');
                console.log('   Button found:', button.attr('class'));
            }
        },

        /**
         * Handle deactivate button click
         */
        onDeactivateClick: function (e) {
            if (this.config.debug) console.log('🔘 Deactivate button clicked');

            e.preventDefault();
            e.stopPropagation();

            // Check if button is disabled
            if ($(e.currentTarget).prop('disabled')) {
                if (this.config.debug) console.warn('⚠️ Button is disabled, ignoring click');
                return;
            }

            this.showDeactivationConfirmation();
        },

        /**
         * Show confirmation dialog using SweetAlert2
         */
        showDeactivationConfirmation: function () {
            if (this.config.debug) console.log('💬 Showing confirmation dialog...');

            // Check if showAlert is available
            if (!window.showAlert || !window.showAlert.Confirm) {
                console.error('❌ showAlert.Confirm not available');
                alert('Error: Alert system not loaded. Please refresh the page.');
                return;
            }

            showAlert.Confirm(
                '⚠️ Permanently Deactivate Account?',
                `<p>This action is <strong>PERMANENT</strong> and <strong>CANNOT</strong> be undone.</p>
                 <p><strong>After deactivation:</strong></p>
                 <ul style="text-align: left;">
                    <li>You will NOT be able to login</li>
                    <li>Your data will be retained for 30 days</li>
                    <li>You can request recovery within 30 days</li>
                 </ul>
                 <p><strong>This action is logged for compliance.</strong></p>`,
                '❌ Yes, Deactivate My Account',
                (result) => this.onConfirmationResult(result)
            );
        },

        /**
         * Handle confirmation dialog result
         */
        onConfirmationResult: function (result) {
            if (this.config.debug) console.log('📊 Dialog result:', result);

            if (result && result.isConfirmed) {
                this.submitDeactivationForm();
            } else {
                if (this.config.debug) console.log('🚫 User cancelled deactivation');
            }
        },

        /**
         * Submit deactivation form
         */
        submitDeactivationForm: function () {
            if (this.config.debug) console.log('📤 Submitting deactivation request...');

            try {
                const csrfToken = this.getCsrfToken();

                if (!csrfToken) {
                    console.error('❌ CSRF token not found');
                    alert('Security error: CSRF token missing. Please refresh and try again.');
                    return;
                }

                const form = $('<form>', {
                    'method': 'POST',
                    'action': this.config.apiUrl
                });

                form.append($('<input>', {
                    'type': 'hidden',
                    'name': '__RequestVerificationToken',
                    'value': csrfToken
                }));

                $('body').append(form);

                if (this.config.debug) {
                    console.log('🔐 Form details:', {
                        action: form.attr('action'),
                        method: form.attr('method'),
                        tokenPresent: !!csrfToken
                    });
                }

                form.submit();
            } catch (error) {
                console.error('❌ Error submitting form:', error);
                alert('An error occurred. Please try again.');
            }
        },

        /**
         * Get CSRF token from page
         */
        getCsrfToken: function () {
            // Try input field first
            let token = $('input[name="__RequestVerificationToken"]').val();

            if (token) return token;

            // Try data attribute from button
            token = $(this.config.buttonId).data('csrf-token');
            if (token) return token;

            // Try cookie
            const cookies = document.cookie.split(';');
            for (let cookie of cookies) {
                cookie = cookie.trim();
                if (cookie.startsWith('XSRF-TOKEN=')) {
                    return cookie.substring('XSRF-TOKEN='.length);
                }
            }

            return null;
        }
    };

    // ✅ Initialize when document is ready
    $(document).ready(function () {
        // Ensure showAlert is loaded
        if (window.showAlert) {
            DeactivateAccount.init();
        } else {
            console.warn('⚠️ Waiting for showAlert to load...');
            // Retry after a delay
            setTimeout(function () {
                if (window.showAlert) {
                    DeactivateAccount.init();
                } else {
                    console.error('❌ showAlert failed to load');
                }
            }, 1000);
        }
    });

    // Expose globally for debugging
    window.DeactivateAccount = DeactivateAccount;
})();



















// (function () {
//     'use strict';

//     /**
//      * DeactivateAccount - Handles user account deactivation with confirmation
//      */
//     const DeactivateAccount = {
//         /**
//          * Initialize event handlers
//          */
//         init: function () {
//             $(document).on('click', '#btn-deactivate-account', this.onDeactivateClick.bind(this));
//             console.log('✅ DeactivateAccount handler initialized');
//         },

//         /**
//          * Handle deactivate button click
//          */
//         onDeactivateClick: function (e) {
//             e.preventDefault();
//             this.showDeactivationConfirmation();
//         },

//         /**
//          * Show confirmation dialog using SweetAlert2
//          */
//         showDeactivationConfirmation: function () {
//             showAlert.Confirm(
//                 '⚠️ Permanently Deactivate Account?',
//                 `This action is PERMANENT and CANNOT be undone.<br><br>
//                  <strong>After deactivation:</strong><br>
//                  • You will NOT be able to login<br>
//                  • Your data will be retained for 30 days<br>
//                  • You can request recovery within 30 days<br><br>
//                  <strong>This action is logged for compliance.</strong>`,
//                 '❌ Yes, Deactivate My Account',
//                 (result) => {
//                     // ✅ FIXED: Check result.isConfirmed
//                     if (result && result.isConfirmed) {
//                         this.submitDeactivationForm();
//                     }
//                 }
//             );
//         },

//         /**
//          * Submit deactivation form
//          */
//         submitDeactivationForm: function () {
//             // Get CSRF token
//             const form = $('<form>', {
//                 'method': 'POST',
//                 'action': window.deactivateAccountUrl || '/Account/DeactivateAccount'
//             });

//             const tokenInput = $('<input>', {
//                 'type': 'hidden',
//                 'name': '__RequestVerificationToken',
//                 'value': $('input[name="__RequestVerificationToken"]').val()
//             });

//             form.append(tokenInput);
//             $('body').append(form);

//             console.log('📤 Submitting deactivation form...');
//             form.submit();
//         }
//     };

//     // Initialize when document is ready
//     $(document).ready(function () {
//         DeactivateAccount.init();
//     });
// })();


















// // Deactivation Confirmation
// $(document).ready(function () {
//     $('#btn-deactivate-account').on('click', function (e) {
//         e.preventDefault();

//         showAlert.Confirm(
//             '⚠️ Permanently Deactivate Account?',
//             `This action is PERMANENT and CANNOT be undone.<br><br>
//              <strong>After deactivation:</strong><br>
//              • You will NOT be able to login<br>
//              • Your data will be retained for 30 days<br>
//              • You can request recovery within 30 days<br><br>
//              <strong>This action is logged for compliance.</strong>`,
//             '❌ Yes, Deactivate My Account',
//             function (result) {
//                 if (result) {
//                     // Get CSRF token
//                     var form = $('<form>', {
//                         'method': 'POST',
//                         'action': window.deactivateAccountUrl || '/Account/DeactivateAccount'
//                     });

//                     var tokenInput = $('<input>', {
//                         'type': 'hidden',
//                         'name': '__RequestVerificationToken',
//                         'value': $('input[name="__RequestVerificationToken"]').val()
//                     });

//                     form.append(tokenInput);
//                     $('body').append(form);
//                     form.submit();
//                 }
//             },
//             'Cancel',
//             'md'
//         );
//     });
// });