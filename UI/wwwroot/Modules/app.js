/* eslint-disable no-undef */
(function () {
    'use strict';

    const urlParams = new URLSearchParams(window.location.search);
    const shipmentId = urlParams.get('shipmentId');

    const shipmentIdText = document.getElementById('shipment-id-text');
    const shipmentAmountText = document.getElementById('shipment-amount-text');
    const resultMessage = document.getElementById('result-message');

    let shipmentAmount = 10.00;

    function result(message) {
        if (resultMessage) {
            resultMessage.innerHTML = message;
        }
    }

    async function loadShipmentInfo() {
        if (!shipmentId) {
            result('<span class="text-danger">Shipment ID is missing in URL.</span>');
            return;
        }

        if (shipmentIdText) shipmentIdText.textContent = shipmentId;

        try {
            if (window.ShipmentApiClient && typeof ShipmentApiClient.getById === 'function') {
                const shipment = await ShipmentApiClient.getById(shipmentId);
                const rate = parseFloat(shipment?.ShippingRate || shipment?.shippingRate || 0);
                if (rate > 0) {
                    shipmentAmount = rate;
                }
            }
        } catch (err) {
            console.warn('Failed to load shipment details, using default amount.', err);
        }

        if (shipmentAmountText) shipmentAmountText.textContent = `$${shipmentAmount.toFixed(2)} USD`;
    }

    async function createOrderCallback() {
        try {
            const token = await ApiClient.getAccessToken();

            // Resolve PayPal payment method id from API list
            const methodsResponse = await ApiClient.getJson('api/PaymentMethods', true);
            const methods = methodsResponse?.data || methodsResponse?.Data || methodsResponse || [];
            const paypalMethod = (Array.isArray(methods) ? methods : []).find(function (m) {
                const name = (m.methodEname || m.MethodEname || m.methdAname || m.MethdAname || m.name || m.Name || '').toLowerCase();
                return name.includes('paypal');
            });

            const paymentMethodId = paypalMethod?.id || paypalMethod?.Id;
            if (!paymentMethodId) {
                const alerts = window.AppResourceAlerts || {};
                const errorMsg = alerts.paymentMethodNotFound || 'PayPal payment method not found';
                if (window.showAlert) {
                    window.showAlert.Error(
                        alerts.configurationErrorTitle || 'Configuration Error',
                        errorMsg
                    );
                }
                throw new Error(errorMsg);
            }

            const response = await fetch('https://localhost:7228/api/Payment/CreateOrder', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    shipmentId: shipmentId,
                    paymentMethodId: paymentMethodId,
                    amount: shipmentAmount,
                    currency: 'USD'
                })
            });

            const text = await response.text();
            const data = text ? JSON.parse(text) : {};

            if (!response.ok) {
                const alerts = window.AppResourceAlerts || {};
                const errorMsg = data.error || alerts.orderCreationFailedMessage || 'Failed to create order';
                if (window.showAlert) {
                    window.showAlert.Error(
                        alerts.orderCreationFailedTitle || 'Order Creation Failed',
                        errorMsg
                    );
                }
                throw new Error(errorMsg);
            }

            if (!data.orderId) {
                const alerts = window.AppResourceAlerts || {};
                const errorMsg = alerts.noOrderIdReturned || 'No orderId returned from server';
                if (window.showAlert) {
                    window.showAlert.Error(
                        alerts.orderErrorTitle || 'Order Error',
                        errorMsg
                    );
                }
                throw new Error(errorMsg);
            }

            // Save for capture callback
            window.__paymentContext = {
                shipmentId: shipmentId,
                paymentMethodId: paymentMethodId,
                amount: shipmentAmount
            };

            return data.orderId;
        } catch (error) {
            const alerts = window.AppResourceAlerts || {};
            if (window.showAlert && !error.message.includes('PayPal payment method not found')) {
                window.showAlert.Error(
                    alerts.paymentErrorTitle || 'Payment Error',
                    error.message || alerts.orderCreationErrorMessage || 'An error occurred while creating the order'
                );
            }
            throw error;
        }
    }

    async function onApproveCallback(data) {
        const token = await ApiClient.getAccessToken();
        const paymentContext = window.__paymentContext || {};

        try {
            const response = await fetch('https://localhost:7228/api/Payment/CaptureOrder', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    orderId: data.orderID,
                    shipmentId: paymentContext.shipmentId,
                    paymentMethodId: paymentContext.paymentMethodId,
                    amount: paymentContext.amount || shipmentAmount,
                    currency: 'USD'
                })
            });

            const text = await response.text();
            const captureData = text ? JSON.parse(text) : {};

            if (!response.ok) {
                const alerts = window.AppResourceAlerts || {};
                const errorMessage = captureData.error || alerts.paymentFailedMessage || 'Failed to capture payment';
                if (window.showAlert) {
                    window.showAlert.Error(
                        alerts.paymentFailedTitle || 'Payment Failed',
                        errorMessage
                    );
                }
                result(`<span class="text-danger">${errorMessage}</span>`);
                return;
            }

            const alerts = window.AppResourceAlerts || {};
            const transactionRef = captureData.transactionReference || captureData.transactionId || '-';
            if (window.showAlert) {
                window.showAlert.Success(
                    alerts.paymentSuccessTitle || 'Payment Successful',
                    `${alerts.paymentSuccessMessage || 'Your payment has been completed successfully.'} ${alerts.transactionReference || 'Transaction'}: ${transactionRef}`
                );
            }
            result(`
                <span class="text-success">
                    Payment completed successfully.<br />
                    Transaction: ${transactionRef}
                </span>
            `);

            // Redirect to Show view after 2 seconds and force page reload
            setTimeout(() => {
                window.location.href = `/Shipments/Show/${paymentContext.shipmentId}?paid=true`;
            }, 2000);
        } catch (error) {
            const alerts = window.AppResourceAlerts || {};
            const errorMessage = error?.message || alerts.paymentErrorMessage || 'An unexpected error occurred during payment';
            if (window.showAlert) {
                window.showAlert.Error(
                    alerts.paymentErrorTitle || 'Payment Error',
                    errorMessage
                );
            }
            result(`<span class="text-danger">${errorMessage}</span>`);
        }
    }

    function initializePayPal() {
        // Render the button component
        paypal.Buttons({
            // Sets up the transaction when a payment button is clicked
            createOrder: createOrderCallback,
            onApprove: onApproveCallback,
            onError: function (error) {
                const alerts = window.AppResourceAlerts || {};
                const errorMessage = error?.message || error || alerts.paymentErrorMessage || 'An error occurred during payment';
                if (window.showAlert) {
                    window.showAlert.Error(
                        alerts.paymentErrorTitle || 'Payment Error',
                        errorMessage
                    );
                }
                result(`<span class="text-danger">${errorMessage}</span>`);
            },

            style: {
                shape: 'rect',
                layout: 'vertical',
                color: 'gold',
                label: 'paypal',
            },
            message: {
                amount: shipmentAmount,
            },
        }).render('#paypal-button-container');

        // Render each field after checking for eligibility
        const cardField = window.paypal.CardFields({
            createOrder: createOrderCallback,
            onApprove: onApproveCallback,
            style: {
                input: {
                    'font-size': '16px',
                    'font-family': 'courier, monospace',
                    'font-weight': 'lighter',
                    color: '#ccc',
                },
                '.invalid': { color: 'purple' },
            },
        });

        if (cardField.isEligible()) {
            const nameField = cardField.NameField({
                style: { input: { color: 'blue' }, '.invalid': { color: 'purple' } },
            });
            nameField.render('#card-name-field-container');

            const numberField = cardField.NumberField({
                style: { input: { color: 'blue' } },
            });
            numberField.render('#card-number-field-container');

            const cvvField = cardField.CVVField({
                style: { input: { color: 'blue' } },
            });
            cvvField.render('#card-cvv-field-container');

            const expiryField = cardField.ExpiryField({
                style: { input: { color: 'blue' } },
            });
            expiryField.render('#card-expiry-field-container');

            // Add click listener to submit button and call the submit function on the CardField component
            document
                .getElementById('card-field-submit-button')
                .addEventListener('click', () => {
                    cardField
                        .submit({
                            // From your billing address fields
                            billingAddress: {
                                addressLine1: document.getElementById('card-billing-address-line-1').value,
                                addressLine2: document.getElementById('card-billing-address-line-2').value,
                                adminArea1: document.getElementById('card-billing-address-admin-area-line-1').value,
                                adminArea2: document.getElementById('card-billing-address-admin-area-line-2').value,
                                countryCode: document.getElementById('card-billing-address-country-code').value,
                                postalCode: document.getElementById('card-billing-address-postal-code').value,
                            },
                        })
                        .then(() => {
                            // submit successful
                        })
                        .catch((err) => {
                            const alerts = window.AppResourceAlerts || {};
                            const errorMessage = err?.message || err || alerts.cardSubmissionFailed || 'Card submission failed';
                            if (window.showAlert) {
                                window.showAlert.Error(
                                    alerts.cardPaymentErrorTitle || 'Card Payment Error',
                                    errorMessage
                                );
                            }
                            result(`<span class="text-danger">${errorMessage}</span>`);
                        });
                });
        }
    }

    (async function init() {
        await loadShipmentInfo();
        initializePayPal();
    })();
})();
