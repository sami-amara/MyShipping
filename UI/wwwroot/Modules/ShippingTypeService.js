(function () {
    'use strict';

    const svc = (window.BaseServiceFactory && typeof BaseServiceFactory.makeCrudService === 'function')
        ? BaseServiceFactory.makeCrudService('api/ShippingTypes', true)
        : {
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

    window.ShippingTypeService = window.ShippingTypeService || svc;
})();





//const ShippingTypeService = {
//    GetAll: function (onSuccess, onError) {
//        ApiClient.get('api/ShippingTypes', onSuccess, onError, true);
//    },
//    const ShippingTypeService = (window.BaseServiceFactory && typeof BaseServiceFactory.makeCrudService === 'function')
//        ? BaseServiceFactory.makeCrudService('api/ShippingTypes', true)
//        : {
//            GetAll: function (onSuccess, onError) { ApiClient.get('api/ShippingTypes', onSuccess, onError, true); },
//            GetById: function (id, onSuccess, onError) { ApiClient.get(`api/ShippingTypes/${id}`, onSuccess, onError, true); },
//            Create: function (data, onSuccess, onError) { ApiClient.post('api/ShippingTypes', data, onSuccess, onError, true); },
//            Delete: function (id, onSuccess, onError) { ApiClient.delete(`api/ShippingTypes/${id}`, onSuccess, onError, true); }
//        };

//    GetById: function (id, onSuccess, onError) {
//        ApiClient.get(`api/ShippingTypes/${id}`, onSuccess, onError, true);
//    },

//    Create: function (data, onSuccess, onError) {
//        ApiClient.post('api/ShippingTypes', data, onSuccess, onError, true);
//    },

//    Delete: function (id, onSuccess, onError) {
//        ApiClient.delete(`api/ShippingTypes/${id}`, onSuccess, onError, true);
//    }
//};
