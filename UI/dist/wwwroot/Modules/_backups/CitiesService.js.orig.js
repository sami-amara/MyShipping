// Backup created by automated refactor
// Original content preserved for revert
const CitiesService = {
    GetAll: function (onSuccess, onError) {
        ApiClient.get('api/Cities', onSuccess, onError, true);
    },
    GetById: function (id, onSuccess, onError) {
        ApiClient.get(`api/Cities/${id}`, onSuccess, onError, true);
    },
    GetByCountryId: function (id, onSuccess, onError) {
        //console.log("CitiesService.GetByCountryId called with id:", id);
        ApiClient.get(`api/Cities/GetByCountryId/${id}`, onSuccess, onError, true);
    }
};
//# sourceMappingURL=CitiesService.js.orig.js.map