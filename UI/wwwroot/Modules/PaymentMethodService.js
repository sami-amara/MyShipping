(function () {
    'use strict';

    const svc = (window.BaseServiceFactory && typeof BaseServiceFactory.makeCrudService === 'function')
        ? BaseServiceFactory.makeCrudService('api/PaymentMethods', true)
        : {
            GetAll: function (onSuccess, onError) {
                ApiClient.get('api/PaymentMethods', onSuccess, onError, true);
            },
            GetById: function (id, onSuccess, onError) {
                ApiClient.get(`api/PaymentMethods/${id}`, onSuccess, onError, true);
            }
        };

    // Add custom methods for payment calculation
    svc.CalculateTotal = function (paymentMethodId, shippingRate, onSuccess, onError) {
        const payload = {
            PaymentMethodId: paymentMethodId,
            ShippingRate: shippingRate
        };
        ApiClient.post('api/PaymentMethods/calculate-total', payload, onSuccess, onError, true);
    };

    svc.GetPaymentDetails = function (paymentMethodId, shippingRate) {
        return new Promise((resolve, reject) => {
            // First get the payment method details
            svc.GetById(paymentMethodId, 
                function(response) {
                    const paymentMethod = response?.data || response?.Data || response;
                    if (!paymentMethod) {
                        reject(new Error('Payment method not found'));
                        return;
                    }

                    // Calculate total with commission
                    svc.CalculateTotal(paymentMethodId, shippingRate,
                        function(totalResponse) {
                            const total = totalResponse?.data || totalResponse?.Data || totalResponse;
                            resolve({
                                paymentMethod: paymentMethod,
                                shippingRate: shippingRate,
                                commission: paymentMethod.commission || paymentMethod.Commission || 0,
                                commissionAmount: total - shippingRate,
                                totalAmount: total
                            });
                        },
                        function(error) {
                            reject(error);
                        }
                    );
                },
                function(error) {
                    reject(error);
                }
            );
        });
    };

    window.PaymentMethodService = window.PaymentMethodService || svc;
})();

