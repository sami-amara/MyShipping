(function () {
    'use strict';
    const svc = (window.BaseServiceFactory && typeof BaseServiceFactory.makeCrudService === 'function')
        ? BaseServiceFactory.makeCrudService('api/Countries', true)
        : {
            GetAll: function (onSuccess, onError) {
                ApiClient.get('api/Countries', onSuccess, onError, true);
            },
            GetById: function (id, onSuccess, onError) {
                ApiClient.get(`api/Countries/${id}`, onSuccess, onError, true);
            }
        };
    window.CountryService = window.CountryService || svc;
})();
//const CountryService = {
//    GetAll: function (onSuccess, onError) {
//        ApiClient.get('api/Countries', onSuccess, onError, true);
//    },
//    GetById: function (id, onSuccess, onError) {
//        ApiClient.get(`api/Countries/${id}`, onSuccess, onError, true);
//    },
//    const CountryService = (window.BaseServiceFactory && typeof BaseServiceFactory.makeCrudService === 'function')
//        ? BaseServiceFactory.makeCrudService('api/Countries', true)
//        : {
//            GetAll: function (onSuccess, onError) { ApiClient.get('api/Countries', onSuccess, onError, true); },
//            GetById: function (id, onSuccess, onError) { ApiClient.get(`api/Countries/${id}`, onSuccess, onError, true); }
//        };
//}
//# sourceMappingURL=CountryService.js.map