// Users Management JavaScript
$(document).ready(function () {
    // Lock user confirmation
    $('.btn-lock').on('click', function (e) {
        e.preventDefault();
        var userId = $(this).data('id');
        var lockUrl = window.userManagementUrls.lockUrl;
        showAlert.Confirm('Are you sure?', 'You want to lock this user account?', 'Yes, Lock it!', function (result) {
            if (result) {
                // Create a form and submit it
                var form = $('<form>', {
                    'method': 'POST',
                    'action': lockUrl
                });
                var tokenInput = $('<input>', {
                    'type': 'hidden',
                    'name': '__RequestVerificationToken',
                    'value': $('input[name="__RequestVerificationToken"]').val()
                });
                var idInput = $('<input>', {
                    'type': 'hidden',
                    'name': 'id',
                    'value': userId
                });
                form.append(tokenInput).append(idInput);
                $('body').append(form);
                form.submit();
            }
        });
    });
    // Unlock user confirmation
    $('.btn-unlock').on('click', function (e) {
        e.preventDefault();
        var userId = $(this).data('id');
        var unlockUrl = window.userManagementUrls.unlockUrl;
        showAlert.Confirm('Are you sure?', 'You want to unlock this user account?', 'Yes, Unlock it!', function (result) {
            if (result) {
                // Create a form and submit it
                var form = $('<form>', {
                    'method': 'POST',
                    'action': unlockUrl
                });
                var tokenInput = $('<input>', {
                    'type': 'hidden',
                    'name': '__RequestVerificationToken',
                    'value': $('input[name="__RequestVerificationToken"]').val()
                });
                var idInput = $('<input>', {
                    'type': 'hidden',
                    'name': 'id',
                    'value': userId
                });
                form.append(tokenInput).append(idInput);
                $('body').append(form);
                form.submit();
            }
        });
    });
});
//# sourceMappingURL=users-management.js.map