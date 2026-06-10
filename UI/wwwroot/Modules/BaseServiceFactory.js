(function(){
    'use strict';

    function makeCrudService(basePath, useJwt = true) {
        const s = {
            GetAll: function(onSuccess, onError) { ApiClient.get(basePath, onSuccess, onError, useJwt); },
            GetById: function(id, onSuccess, onError){ ApiClient.get(`${basePath}/${encodeURIComponent(id)}`, onSuccess, onError, useJwt); },
            Create: function(data, onSuccess, onError){ ApiClient.post(basePath, data, onSuccess, onError, useJwt); },
            Update: function(id, data, onSuccess, onError){ ApiClient.request && ApiClient.request('PUT', `${basePath}/${encodeURIComponent(id)}`, data, useJwt).then(r=>onSuccess && onSuccess(r.response)).catch(e=>onError && onError(e)); },
            Delete: function(id, onSuccess, onError){ ApiClient.delete(`${basePath}/${encodeURIComponent(id)}`, onSuccess, onError, useJwt); }
        };
        return s;
    }

    window.BaseServiceFactory = window.BaseServiceFactory || { makeCrudService };
})();
