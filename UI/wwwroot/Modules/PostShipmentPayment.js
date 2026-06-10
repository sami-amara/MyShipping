// /* eslint-disable no-undef */
// // ═══════════════════════════════════════════════════════════════════════════════════
// // PostShipmentPayment: Best Practice Payment Modal After Shipment Creation
// // ═══════════════════════════════════════════════════════════════════════════════════
// // This module handles the payment choice popup that appears AFTER successful
// // shipment creation, implementing the recommended pattern of separating shipment
// // creation from payment processing using PayPal JavaScript SDK.
// // ═══════════════════════════════════════════════════════════════════════════════════

// (function () {
//     'use strict';

//     const PostShipmentPayment = {
//         currentShipmentId: null,
//         currentTrackingNumber: null,
//         paypalButtonRendered: false,

//         /**
//          * Show the post-shipment payment modal
//          * @param {string} shipmentId - The created shipment ID
//          * @param {string} trackingNumber - The shipment tracking number
//          */
//         showPaymentModal: function (shipmentId, trackingNumber) {
//             console.log('📦 Showing payment modal for shipment:', { shipmentId, trackingNumber });

//             this.currentShipmentId = shipmentId;
//             this.currentTrackingNumber = trackingNumber;
//             this.paypalButtonRendered = false;

//             // Update modal with shipment details
//             $('#modal-shipment-id').text(shipmentId || 'N/A');
//             $('#modal-tracking-number').text(trackingNumber || 'N/A');

//             // Load payment methods
//             this.loadPaymentMethods();

//             // Reset payment containers
//             $('#modal-paypal-button-container').hide();
//             $('#modal-other-payment-container').hide();
//             $('#payment-status-message').hide();

//             // Show the modal
//             $('#postShipmentPaymentModal').modal('show');
//         },

//         /**
//          * Load available payment methods into the modal dropdown
//          */
//         loadPaymentMethods: function () {
//             console.log('💳 Loading payment methods for modal...');

//             // Use existing ManagePageControlls to load payment methods
//             // Correct method name is 'fillPaymentMethodDropdown' not 'GetPaymentMethodsDropdown'
//             if (window.ManagePageControlls && typeof ManagePageControlls.fillPaymentMethodDropdown === 'function') {
//                 try {
//                     ManagePageControlls.fillPaymentMethodDropdown('#modalPaymentMethodId');
//                     console.log('✅ Payment methods loaded in modal via ManagePageControlls');
//                 } catch (err) {
//                     console.error('❌ Failed to load payment methods via ManagePageControlls:', err);
//                     this.tryFallbackPaymentLoad();
//                 }
//             } else {
//                 console.warn('⚠️ ManagePageControlls not available, using fallback');
//                 this.tryFallbackPaymentLoad();
//             }
//         },

//         /**
//          * Fallback method to load payment methods if ManagePageControlls fails
//          */
//         tryFallbackPaymentLoad: function () {
//             // Fallback: try PaymentMethodService directly
//             if (window.PaymentMethodService && typeof PaymentMethodService.GetAll === 'function') {
//                 PaymentMethodService.GetAll(
//                     (response) => {
//                         const list = this.extractPaymentList(response);
//                         if (list && list.length > 0) {
//                             this.populatePaymentDropdown(list);
//                             console.log('✅ Payment methods loaded via PaymentMethodService fallback');
//                         } else {
//                             console.error('❌ No payment methods returned from PaymentMethodService');
//                             this.showError('No payment methods available. Please refresh the page.');
//                         }
//                     },
//                     (err) => {
//                         console.error('❌ Failed to load payment methods via PaymentMethodService:', err);
//                         this.showError('Failed to load payment methods. Please refresh the page.');
//                     }
//                 );
//             } else if (window.ApiClient && typeof ApiClient.get === 'function') {
//                 // Second fallback: direct API call
//                 ApiClient.get(
//                     'api/PaymentMethods',
//                     (response) => {
//                         const list = this.extractPaymentList(response);
//                         if (list && list.length > 0) {
//                             this.populatePaymentDropdown(list);
//                             console.log('✅ Payment methods loaded via ApiClient fallback');
//                         } else {
//                             console.error('❌ No payment methods returned from ApiClient');
//                             this.showError('No payment methods available. Please refresh the page.');
//                         }
//                     },
//                     (err) => {
//                         console.error('❌ Failed to load payment methods via ApiClient:', err);
//                         this.showError('Failed to load payment methods. Please refresh the page.');
//                     },
//                     true
//                 );
//             } else {
//                 console.error('❌ No payment loading method available');
//                 this.showError('Payment service not available. Please refresh the page.');
//             }
//         },

//         /**
//          * Extract payment method list from API response
//          */
//         extractPaymentList: function (response) {
//             if (!response) return null;
//             if (Array.isArray(response)) return response;
//             if (Array.isArray(response.data)) return response.data;
//             if (Array.isArray(response.Data)) return response.Data;
//             if (Array.isArray(response.items)) return response.items;
//             return null;
//         },

//         /**
//          * Populate the payment dropdown with payment methods
//          */
//         populatePaymentDropdown: function (methods) {
//             const $select = $('#modalPaymentMethodId');
//             $select.empty().append('<option value="">-- Select Payment Method --</option>');

//             methods.forEach(method => {
//                 const name = method.methodEname || method.MethodEname || method.methdAname || method.MethdAname || 'Unknown';
//                 const id = method.id || method.Id;
//                 const token = method.paymentMethodToken || method.PaymentMethodToken || '';

//                 const $option = $(`<option value="${id}">${name}</option>`);
//                 if (token) {
//                     $option.attr('data-payment-token', token);
//                 }
//                 $select.append($option);
//             });
//         },

//         /**
//          * Initialize the PayPal button in the modal
//          */
//         initializePayPalButton: function () {
//             if (this.paypalButtonRendered) {
//                 console.log('ℹ️ PayPal button already rendered');
//                 return;
//             }

//             if (!window.PaymentService) {
//                 console.error('❌ PaymentService not available');
//                 this.showError('Payment service is not available. Please refresh the page.');
//                 return;
//             }

//             if (!this.currentShipmentId) {
//                 console.error('❌ No shipment ID available for payment');
//                 this.showError('Shipment ID is missing. Cannot process payment.');
//                 return;
//             }

//             console.log('🔄 Initializing PayPal button in modal...');

//             // Get selected payment method
//             const selectedPaymentMethodId = $('#modalPaymentMethodId').val();
//             if (!selectedPaymentMethodId) {
//                 console.warn('⚠️ No payment method selected');
//                 return;
//             }

//             // Show PayPal container
//             $('#modal-paypal-button-container').slideDown();

//             // Clear any existing button
//             $('#modal-paypal-button').empty();

//             // Get payment amount - we need to fetch the shipment details to get the shipping rate
//             this.getShipmentAmount(this.currentShipmentId)
//                 .then(amount => {
//                     console.log(`💰 Payment amount calculated: $${amount}`);

//                     // Prepare payment data for PaymentService
//                     const paymentData = {
//                         shipmentId: this.currentShipmentId,
//                         paymentMethodId: selectedPaymentMethodId,
//                         amount: amount,
//                         currency: 'USD'
//                     };

//                     // Render PayPal button using PaymentService with correct signature
//                     PaymentService.renderPayPalButton(
//                         'modal-paypal-button', // container ID (without #)
//                         paymentData,
//                         (result) => {
//                             // onSuccess callback
//                             console.log('✅ PayPal payment approved:', result);
//                             this.handlePaymentSuccess({
//                                 orderID: result.orderId || result.OrderId,
//                                 paymentId: result.paymentId || result.PaymentId,
//                                 transactionId: result.transactionId || result.TransactionId
//                             });
//                         },
//                         (error) => {
//                             // onError callback
//                             console.error('❌ PayPal payment error:', error);

//                             // Check if user cancelled
//                             if (error && error.message && error.message.includes('cancelled')) {
//                                 this.handlePaymentCancel();
//                             } else {
//                                 this.handlePaymentError(error);
//                             }
//                         }
//                     );

//                     console.log('✅ PayPal button rendered successfully in modal');
//                     this.paypalButtonRendered = true;
//                 })
//                 .catch(err => {
//                     console.error('❌ Failed to get shipment amount:', err);
//                     this.showError('Failed to calculate payment amount. Please try again.');
//                     this.paypalButtonRendered = false;
//                 });
//         },

//         /**
//          * Get shipment amount for payment calculation
//          * @param {string} shipmentId - The shipment ID
//          * @returns {Promise<number>} - The payment amount
//          */
//         getShipmentAmount: function (shipmentId) {
//             return new Promise((resolve, reject) => {
//                 // Try to get shipment details via API
//                 if (window.ApiClient && typeof ApiClient.get === 'function') {
//                     ApiClient.get(
//                         `Shipments/${shipmentId}`,
//                         (response) => {
//                             try {
//                                 // Extract shipment data from response
//                                 const shipment = response?.data || response?.Data || response;

//                                 // Get shipping rate from shipment
//                                 const shippingRate = shipment?.shippingRate || shipment?.ShippingRate || 0;

//                                 if (shippingRate > 0) {
//                                     console.log(`📦 Shipment shipping rate: $${shippingRate}`);
//                                     resolve(shippingRate);
//                                 } else {
//                                     console.warn('⚠️ Shipping rate is 0 or not found, using default amount');
//                                     resolve(25.00); // Default minimum amount
//                                 }
//                             } catch (err) {
//                                 console.error('❌ Error parsing shipment response:', err);
//                                 reject(err);
//                             }
//                         },
//                         (error) => {
//                             console.error('❌ Failed to fetch shipment details:', error);
//                             // Fallback: use a default amount
//                             console.warn('⚠️ Using default payment amount');
//                             resolve(25.00);
//                         },
//                         true // use auth
//                     );
//                 } else {
//                     // If ApiClient is not available, use a default amount
//                     console.warn('⚠️ ApiClient not available, using default amount');
//                     resolve(25.00);
//                 }
//             });
//         },

//         /**
//          * Handle successful payment
//          */
//         handlePaymentSuccess: function (paymentData) {
//             console.log('✅ Payment successful:', paymentData);

//             // Show success message
//             $('#payment-status-message')
//                 .removeClass('alert-danger')
//                 .addClass('alert alert-success')
//                 .html(`
//                     <i class="fa fa-check-circle"></i>
//                     <strong>Payment Successful!</strong>
//                     <p class="mb-0">Your payment has been processed successfully. Order ID: ${paymentData.orderID || 'N/A'}</p>
//                 `)
//                 .slideDown();

//             // Hide payment method selection
//             $('#payment-method-selection').slideUp();

//             // Update footer buttons
//             $('.modal-footer').html(`
//                 <button type="button" class="btn btn-success" onclick="window.location.href='/Shipments/List'">
//                     <i class="fa fa-list"></i> View My Shipments
//                 </button>
//                 <button type="button" class="btn btn-primary" onclick="window.location.href='/Shipments/Create'">
//                     <i class="fa fa-plus"></i> Create Another Shipment
//                 </button>
//             `);

//             // Optional: Auto-redirect after 3 seconds
//             setTimeout(() => {
//                 window.location.href = '/Shipments/List?paid=1';
//             }, 9000);
//         },

//         /**
//          * Handle payment error
//          */
//         handlePaymentError: function (error) {
//             console.error('❌ Payment error:', error);

//             const errorMessage = error?.message || error || 'An unknown error occurred';

//             $('#payment-status-message')
//                 .removeClass('alert-success')
//                 .addClass('alert alert-danger')
//                 .html(`
//                     <i class="fa fa-exclamation-triangle"></i>
//                     <strong>Payment Failed</strong>
//                     <p class="mb-0">${errorMessage}</p>
//                     <small>Please try again or contact support if the problem persists.</small>
//                 `)
//                 .slideDown();
//         },

//         /**
//          * Handle payment cancellation
//          */
//         handlePaymentCancel: function () {
//             console.log('⚠️ Payment cancelled');

//             $('#payment-status-message')
//                 .removeClass('alert-success alert-danger')
//                 .addClass('alert alert-warning')
//                 .html(`
//                     <i class="fa fa-info-circle"></i>
//                     <strong>Payment Cancelled</strong>
//                     <p class="mb-0">You can complete the payment later from your shipments list.</p>
//                 `)
//                 .slideDown();
//         },

//         /**
//          * Show error message in modal
//          */
//         showError: function (message) {
//             $('#payment-status-message')
//                 .removeClass('alert-success alert-warning')
//                 .addClass('alert alert-danger')
//                 .html(`
//                     <i class="fa fa-exclamation-triangle"></i>
//                     <strong>Error</strong>
//                     <p class="mb-0">${message}</p>
//                 `)
//                 .slideDown();
//         },

//         /**
//          * Initialize event handlers
//          */
//         init: function () {
//             console.log('🎬 Initializing PostShipmentPayment module...');

//             // Store reference to 'this' for use in event handlers
//             const self = this;

//             // Payment method change handler
//             $(document).on('change', '#modalPaymentMethodId', function (e) {
//                 const selectedValue = $(this).val();
//                 const selectedText = $(this).find('option:selected').text().toLowerCase();

//                 console.log('💳 Payment method selected:', selectedText);

//                 // Reset PayPal button state when changing payment method
//                 self.paypalButtonRendered = false;

//                 // Hide all payment containers first
//                 $('#modal-paypal-button-container').slideUp();
//                 $('#modal-other-payment-container').slideUp();
//                 $('#payment-status-message').slideUp();

//                 if (!selectedValue) {
//                     return;
//                 }

//                 // Show appropriate payment UI based on selection
//                 if (selectedText.includes('paypal')) {
//                     console.log('✅ PayPal detected, initializing PayPal button...');
//                     self.initializePayPalButton();
//                 } else {
//                     console.log('ℹ️ Other payment method selected:', selectedText);
//                     // Show other payment method placeholder
//                     $('#modal-other-payment-container').slideDown();
//                 }
//             });

//             // Continue without payment button
//             $(document).on('click', '#modal-continue-without-payment', function () {
//                 console.log('ℹ️ User chose to continue without payment');
//                 $('#postShipmentPaymentModal').modal('hide');

//                 // Redirect to shipments list
//                 window.location.href = '/Shipments/List?created=1';
//             });

//             // Clean up on modal close
//             $('#postShipmentPaymentModal').on('hidden.bs.modal', function () {
//                 console.log('🔄 Payment modal closed, cleaning up...');
//                 self.currentShipmentId = null;
//                 self.currentTrackingNumber = null;
//                 self.paypalButtonRendered = false;
//                 $('#modal-paypal-button').empty();
//                 $('#payment-status-message').hide();
//                 $('#modal-paypal-button-container').hide();
//                 $('#modal-other-payment-container').hide();
//             });

//             console.log('✅ PostShipmentPayment module initialized');
//         }
//     };

//     // Expose to global scope
//     window.PostShipmentPayment = PostShipmentPayment;

//     // Auto-initialize on document ready
//     $(document).ready(function () {
//         PostShipmentPayment.init();
//     });
// })();
