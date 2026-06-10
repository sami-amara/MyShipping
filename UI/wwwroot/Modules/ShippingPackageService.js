




(function () {
    'use strict';

    const svc = (window.BaseServiceFactory && typeof BaseServiceFactory.makeCrudService === 'function')
        ? BaseServiceFactory.makeCrudService('api/ShippingPackag', true)
        : {
            GetAll: function (onSuccess, onError) {
                ApiClient.get('api/ShippingPackag', onSuccess, onError, true); console.debug('ShippingPackageService: requesting api/ShippingPackag');
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

    window.ShippingPackageService = window.ShippingPackageService || svc;
})();
