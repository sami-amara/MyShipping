// Backup of DiagnosticsService.js
const DiagnosticsService = {
    GetAll: function (onSuccess, onError) {
        ApiClient.get('api/Diagnostics', onSuccess, onError, true);
    },
    GetById: function (id, onSuccess, onError) {
        ApiClient.get(`api/Diagnostics/${id}`, onSuccess, onError, true);
    },
};
//# sourceMappingURL=DiagnosticsService.js.backup.js.map