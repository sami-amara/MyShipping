// Backup created by automated refactor
// Original content preserved for revert
const CountryService = {
    GetAll: function (onSuccess, onError) {
        ApiClient.get('api/Countries', onSuccess, onError, true);
    },
    GetById: function (id, onSuccess, onError) {
        ApiClient.get(`api/Countries/${id}`, onSuccess, onError, true);
    },
};
//# sourceMappingURL=CountryService.js.orig.js.map