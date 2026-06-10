// Backup of ShippingTypeService.js
const ShippingTypeService = {
    GetAll: function (onSuccess, onError) {
        ApiClient.get('api/ShippingTypes', onSuccess, onError, true);
    },
    GetById: function (id, onSuccess, onError) {
        ApiClient.get(`api/ShippingTypes/${id}`, onSuccess, onError, true);
    },
    Create: function (data, onSuccess, onError) {
        ApiClient.post('api/ShippingTypes', data, onSuccess, onError, true);
    },
    Delete: function (id, onSuccess, onError) {
        ApiClient.delete(`api/ShippingTypes/${id}`, onSuccess, onError, true);
    }
};
//# sourceMappingURL=ShippingTypeService.js.backup.js.map