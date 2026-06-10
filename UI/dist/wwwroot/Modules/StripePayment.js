var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
/**
 * Stripe Payment Module
 * Handles Stripe payment processing for shipments
 */
const StripePayment = (function () {
    let stripe;
    let elements;
    let cardElement;
    let currentShipmentId;
    let currentAmount;
    /**
     * Initialize Stripe with publishable key
     * @param {string} publishableKey - Stripe publishable key
     */
    function init(publishableKey) {
        if (!publishableKey) {
            console.error('StripePayment.init: publishableKey is required');
            return;
        }
        try {
            stripe = Stripe(publishableKey);
            elements = stripe.elements();
            // Create card element with custom styling
            cardElement = elements.create('card', {
                style: {
                    base: {
                        fontSize: '16px',
                        color: '#32325d',
                        fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
                        fontSmoothing: 'antialiased',
                        '::placeholder': {
                            color: '#aab7c4'
                        }
                    },
                    invalid: {
                        color: '#fa755a',
                        iconColor: '#fa755a'
                    }
                },
                hidePostalCode: false
            });
            // Mount card element to the DOM
            cardElement.mount('#stripe-card-element');
            // Handle real-time validation errors
            cardElement.on('change', (event) => {
                const displayError = document.getElementById('stripe-card-errors');
                if (event.error) {
                    displayError.textContent = event.error.message;
                }
                else {
                    displayError.textContent = '';
                }
            });
            console.log('StripePayment initialized successfully');
        }
        catch (error) {
            console.error('StripePayment.init error:', error);
            if (window.showAlert && typeof showAlert.Error === 'function') {
                showAlert.Error('Stripe Initialization Error', error.message);
            }
        }
    }
    /**
     * Process Stripe payment for a shipment
     * @param {string} shipmentId - Shipment GUID
     * @param {number} amount - Payment amount
     */
    function processPayment(shipmentId, amount) {
        return __awaiter(this, void 0, void 0, function* () {
            var _a, _b, _c;
            currentShipmentId = shipmentId;
            currentAmount = amount;
            const submitButton = document.getElementById('stripe-submit-button');
            const buttonText = document.getElementById('stripe-button-text');
            const spinner = document.getElementById('stripe-spinner');
            const errorElement = document.getElementById('stripe-card-errors');
            // Disable submit button and show spinner
            submitButton.disabled = true;
            buttonText.classList.add('d-none');
            spinner.classList.remove('d-none');
            errorElement.textContent = '';
            try {
                console.log('Creating Stripe Payment Intent for shipment:', shipmentId, 'amount:', amount);
                // Ensure ApiClient is available
                if (typeof ApiClient === 'undefined' || typeof ApiClient.postJson !== 'function') {
                    throw new Error('ApiClient is not available. Please refresh the page.');
                }
                // Step 1: Create Payment Intent on server using ApiClient for JWT auth
                const createData = yield ApiClient.postJson('api/Payment/CreateStripeIntent', {
                    shipmentId: shipmentId,
                    amount: amount
                }, true); // true = use JWT authentication
                console.log('CreateStripeIntent response:', createData);
                if (!createData || !createData.clientSecret) {
                    throw new Error((createData === null || createData === void 0 ? void 0 : createData.error) || 'Failed to create payment intent');
                }
                const { clientSecret, paymentIntentId } = createData;
                if (!clientSecret) {
                    throw new Error('No client secret returned from server');
                }
                console.log('Payment Intent created:', paymentIntentId);
                // Step 2: Confirm card payment with Stripe
                const { error, paymentIntent } = yield stripe.confirmCardPayment(clientSecret, {
                    payment_method: {
                        card: cardElement
                    }
                });
                if (error) {
                    console.error('Stripe confirmCardPayment error:', error);
                    throw new Error(error.message);
                }
                console.log('Payment confirmed:', paymentIntent.id, 'status:', paymentIntent.status);
                // Step 3: Capture payment on server (mark shipment as paid) using ApiClient for JWT auth
                const captureData = yield ApiClient.postJson('api/Payment/CaptureStripe', {
                    shipmentId: shipmentId,
                    paymentIntentId: paymentIntent.id
                    // paymentMethodId is looked up by backend from database
                }, true); // true = use JWT authentication
                console.log('Capture response:', captureData);
                if (!captureData || !captureData.success) {
                    throw new Error((captureData === null || captureData === void 0 ? void 0 : captureData.error) || (captureData === null || captureData === void 0 ? void 0 : captureData.message) || 'Failed to capture payment');
                }
                console.log('Payment captured successfully');
                // Success! Close modal and show success message
                $('#stripePaymentModal').modal('hide');
                if (window.showAlert && typeof showAlert.Success === 'function') {
                    showAlert.Success(((_a = window.AppResourceAlerts) === null || _a === void 0 ? void 0 : _a.paymentSuccessTitle) || 'Payment Successful', ((_b = window.AppResourceAlerts) === null || _b === void 0 ? void 0 : _b.paymentSuccessMessage) || 'Your payment has been processed successfully.');
                }
                else {
                    alert('Payment successful!');
                }
                // Reload page after 2 seconds to show updated payment status
                setTimeout(() => {
                    window.location.reload();
                }, 2000);
            }
            catch (error) {
                console.error('StripePayment.processPayment error:', error);
                errorElement.textContent = error.message;
                if (window.showAlert && typeof showAlert.Error === 'function') {
                    showAlert.Error(((_c = window.AppResourceAlerts) === null || _c === void 0 ? void 0 : _c.paymentFailedTitle) || 'Payment Failed', error.message);
                }
                else {
                    alert('Payment failed: ' + error.message);
                }
            }
            finally {
                // Re-enable submit button and hide spinner
                submitButton.disabled = false;
                buttonText.classList.remove('d-none');
                spinner.classList.add('d-none');
            }
        });
    }
    /**
     * Reset the Stripe form
     */
    function resetForm() {
        if (cardElement) {
            cardElement.clear();
        }
        const errorElement = document.getElementById('stripe-card-errors');
        if (errorElement) {
            errorElement.textContent = '';
        }
    }
    // Public API
    return {
        init,
        processPayment,
        resetForm
    };
})();
/**
 * Open Stripe payment modal for a shipment
 * Called from payment method selector
 * @param {string} shipmentId - Shipment GUID
 */
function openStripePaymentModal(shipmentId) {
    return __awaiter(this, void 0, void 0, function* () {
        try {
            console.log('Opening Stripe payment modal for shipment:', shipmentId);
            // Wait for Stripe.js to load if needed
            if (typeof Stripe === 'undefined') {
                console.log('Waiting for Stripe.js to load...');
                yield waitForStripe();
            }
            // Initialize Stripe if not already done
            if (!window.stripeInitialized) {
                // Load publishable key from server configuration
                const stripeKey = yield getStripePublishableKey();
                if (!stripeKey) {
                    throw new Error('Could not retrieve Stripe publishable key');
                }
                StripePayment.init(stripeKey);
                window.stripeInitialized = true;
            }
            // Load shipment details to get amount
            if (typeof ShipmentApiClient === 'undefined' || typeof ShipmentApiClient.getById !== 'function') {
                throw new Error('ShipmentApiClient is not available');
            }
            const shipment = yield ShipmentApiClient.getById(shipmentId);
            if (!shipment) {
                throw new Error('Shipment not found');
            }
            const amount = shipment.shippingRate || shipment.ShippingRate || 0;
            const trackingNumber = shipment.trackingNumber || shipment.TrackingNumber || shipmentId;
            // Update modal with shipment details
            document.getElementById('stripe-shipment-id').textContent = '#' + trackingNumber;
            document.getElementById('stripe-amount-display').textContent = '$' + amount.toFixed(2);
            // Reset form
            StripePayment.resetForm();
            // Show modal
            $('#stripePaymentModal').modal('show');
            // Attach payment handler to form submit
            const form = document.getElementById('stripe-payment-form');
            form.onsubmit = (e) => __awaiter(this, void 0, void 0, function* () {
                e.preventDefault();
                yield StripePayment.processPayment(shipmentId, amount);
            });
        }
        catch (error) {
            console.error('openStripePaymentModal error:', error);
            if (window.showAlert && typeof showAlert.Error === 'function') {
                showAlert.Error('Error', error.message);
            }
            else {
                alert('Error: ' + error.message);
            }
        }
    });
}
/**
 * Wait for Stripe.js to load
 * @returns {Promise<void>}
 */
function waitForStripe() {
    return new Promise((resolve, reject) => {
        // Check if already loaded
        if (typeof Stripe !== 'undefined') {
            console.log('Stripe.js already loaded');
            resolve();
            return;
        }
        console.log('Waiting for Stripe.js to load...');
        let attempts = 0;
        const maxAttempts = 50; // 5 seconds max wait
        const checkStripe = setInterval(() => {
            attempts++;
            console.log(`Checking for Stripe... attempt ${attempts}/${maxAttempts}`);
            if (typeof Stripe !== 'undefined') {
                clearInterval(checkStripe);
                console.log('Stripe.js loaded successfully after', attempts * 100, 'ms');
                resolve();
            }
            else if (attempts >= maxAttempts) {
                clearInterval(checkStripe);
                console.error('Stripe.js failed to load after', attempts * 100, 'ms');
                console.error('Check: 1) Internet connection, 2) CSP headers, 3) Ad blockers, 4) Network tab for CDN errors');
                reject(new Error('Stripe.js failed to load. Please check your internet connection and try again.'));
            }
        }, 100); // Check every 100ms
    });
}
/**
 * Get Stripe publishable key from server
 * @returns {Promise<string>} Stripe publishable key
 */
function getStripePublishableKey() {
    return __awaiter(this, void 0, void 0, function* () {
        try {
            // Use ApiClient baseUrl to ensure correct API endpoint
            const baseUrl = (typeof ApiClient !== 'undefined' && ApiClient.baseUrl)
                ? ApiClient.baseUrl
                : 'https://localhost:7228/';
            const url = `${baseUrl}api/Payment/GetStripePublishableKey`;
            console.log('Fetching Stripe publishable key from:', url);
            const response = yield fetch(url);
            if (!response.ok) {
                throw new Error(`Failed to get Stripe publishable key: ${response.status} ${response.statusText}`);
            }
            const data = yield response.json();
            if (!data.publishableKey) {
                throw new Error('Stripe publishable key not configured on server');
            }
            console.log('Retrieved Stripe publishable key from server');
            return data.publishableKey;
        }
        catch (error) {
            console.error('Failed to get Stripe key from server:', error);
            throw error; // Don't use fallback - fail properly
        }
    });
}
// Export for use in other modules
if (typeof window !== 'undefined') {
    window.StripePayment = StripePayment;
    window.openStripePaymentModal = openStripePaymentModal;
}
//# sourceMappingURL=StripePayment.js.map