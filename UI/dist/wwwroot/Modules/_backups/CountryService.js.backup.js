// Backup of CountryService.js
const CountryService = {
    GetAll: function (onSuccess, onError) {
        ApiClient.get('api/Countries', onSuccess, onError, true);
    },
    GetById: function (id, onSuccess, onError) {
        ApiClient.get(`api/Countries/${id}`, onSuccess, onError, true);
    },
};
//# sourceMappingURL=CountryService.js.backup.js.map