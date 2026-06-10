/* eslint-disable no-undef */
// Backup of ShippingPackageService.js
const ShippingPackageService = {
    GetAll: function (onSuccess, onError) {
        ApiClient.get('api/ShippingPackag', onSuccess, onError, true);
                // Use the plural controller route and log the request for easier debugging.
                console.debug('ShippingPackageService: requesting api/ShippingPackagings');
    },

    GetById: function (id, onSuccess, onError) {
        ApiClient.get(`api/ShippingPackag/${id}`, onSuccess, onError, true);
    },

    Create: function (data, onSuccess, onError) {
        ApiClient.post('api/ShippingPackag', data, onSuccess, onError, true);
    },

    Delete: function (id, onSuccess, onError) {
        ApiClient.delete(`api/ShippingPackag/${id}`, onSuccess, onError, true);
    }
};
