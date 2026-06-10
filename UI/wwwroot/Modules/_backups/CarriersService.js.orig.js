// Backup created by automated refactor
// Original content preserved for revert

const CarriersService = {
    GetAll: function (onSuccess, onError) {
        ApiClient.get('api/Carrier', onSuccess, onError, true);
    },

    GetById: function (id, onSuccess, onError) {
        ApiClient.get(`api/Carrier/${id}`, onSuccess, onError, true);
    },
    Create: function (data, onSuccess, onError) {
        ApiClient.post('api/Carrier', data, onSuccess, onError, true);
    },

    Delete: function (id, onSuccess, onError) {
        ApiClient.delete(`api/Carrier/${id}`, onSuccess, onError, true);
    }

};
