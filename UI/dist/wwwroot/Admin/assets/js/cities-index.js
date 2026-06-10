$(document).ready(function () {
    $('.btn-danger').on('click', function () {
        var id = $(this).data('id');
        var deleteUrl = $(this).data('delete-url');
        showAlert.ConfirmDelete(function (result) {
            if (result) {
                window.location.href = deleteUrl + '/' + id;
            }
        });
    });
});
//# sourceMappingURL=cities-index.js.map