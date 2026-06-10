(function () {
    'use strict';

    const svc = (window.BaseServiceFactory && typeof BaseServiceFactory.makeCrudService === 'function')
        ? BaseServiceFactory.makeCrudService('api/Diagnostics', true)
        : {
            GetAll: function (onSuccess, onError) { ApiClient.get('api/Diagnostics', onSuccess, onError, true); },
            GetById: function (id, onSuccess, onError) { ApiClient.get(`api/Diagnostics/${id}`, onSuccess, onError, true); }
        };

    window.DiagnosticsService = window.DiagnosticsService || svc;
})();
