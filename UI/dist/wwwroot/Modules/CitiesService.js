(function () {
    'use strict';
    const svc = (window.BaseServiceFactory && typeof BaseServiceFactory.makeCrudService === 'function')
        ? Object.assign(BaseServiceFactory.makeCrudService('api/Cities', true), {
            GetByCountryId: function (id, onSuccess, onError) { ApiClient.get(`api/Cities/GetByCountryId/${id}`, onSuccess, onError, true); }
        })
        : {
            GetAll: function (onSuccess, onError) {
                ApiClient.get('api/Cities', onSuccess, onError, true);
            },
            GetById: function (id, onSuccess, onError) {
                ApiClient.get(`api/Cities/${id}`, onSuccess, onError, true);
            },
            GetByCountryId: function (id, onSuccess, onError) {
                ApiClient.get(`api/Cities/GetByCountryId/${id}`, onSuccess, onError, true);
            }
        };
    window.CitiesService = window.CitiesService || svc;
})();
//# sourceMappingURL=CitiesService.js.map