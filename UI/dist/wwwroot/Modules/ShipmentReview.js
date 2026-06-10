/* eslint-disable no-undef */
// ShipmentReview: handles review step population (PAYMENT-FREE VERSION)
// ============================================================================
// This module has been updated to support payment-free shipment creation.
// All payment-related functions have been commented out or disabled.
// Payment is now handled AFTER shipment creation via a separate modal popup.
// ============================================================================
(function () {
    'use strict';
    const ShipmentReview = {
        // Store payment processing state
        paymentProcessing: false,
        getLabel: function (key, fallback) {
            const labels = window.AppResourceLabels || {};
            const value = labels[key];
            return (value === null || value === undefined || String(value).trim() === '') ? fallback : value;
        },
        populateReviewStep: function () {
            const form = document.querySelector('#createShipmentForm');
            if (!form)
                return;
            // --------------------------------------------------------------------
            // COMMENTED OUT - Payment validation removed from shipment review
            // Payment is now handled AFTER shipment creation, not during.
            // Shipment creation is completely payment-free.
            // --------------------------------------------------------------------
            // if (!this.validatePaymentMethodSelected()) {
            //     if (window.FormValidator && typeof FormValidator.showStep === 'function') {
            //         FormValidator.showStep('#createShipmentForm', 4);
            //     }
            //     return;
            // }
            // Populate sender info
            this.populateSenderReview(form);
            // Populate receiver info
            this.populateReceiverReview(form);
            // Populate package info
            this.populatePackageReview(form);
            // Populate shipping info
            this.populateShippingReview(form);
            // --------------------------------------------------------------------
            // COMMENTED OUT - Payment review removed from shipment creation
            // --------------------------------------------------------------------
            // this.populatePaymentReview(form);
        },
        // --------------------------------------------------------------------
        // DISABLED - Payment Method Validation
        // Payment validation is not needed during shipment creation
        // --------------------------------------------------------------------
        validatePaymentMethodSelected_DISABLED: function () {
            const form = document.querySelector('#createShipmentForm');
            if (!form)
                return false;
            const paymentMethodSelect = form.querySelector('[name="PaymentMethodId"]');
            if (!paymentMethodSelect)
                return false;
            const selectedValue = paymentMethodSelect.value;
            const validationSpan = form.querySelector('[data-valmsg-for="PaymentMethodId"], .field-validation-error[data-valmsg-for="PaymentMethodId"]');
            if (!selectedValue || selectedValue === '' || selectedValue === '00000000-0000-0000-0000-000000000000') {
                if (validationSpan) {
                    validationSpan.textContent = 'Please select a payment method before proceeding to review';
                    validationSpan.className = 'field-validation-error text-danger';
                    validationSpan.style.display = 'block';
                }
                else {
                    const spanNearby = paymentMethodSelect.parentElement.querySelector('.text-danger, [class*="validation"]');
                    if (spanNearby) {
                        spanNearby.textContent = 'Please select a payment method before proceeding to review';
                        spanNearby.className = 'field-validation-error text-danger';
                        spanNearby.style.display = 'block';
                    }
                }
                if (paymentMethodSelect && paymentMethodSelect.scrollIntoView) {
                    paymentMethodSelect.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
                return false;
            }
            if (validationSpan) {
                validationSpan.textContent = '';
                validationSpan.className = 'field-validation-valid';
                validationSpan.style.display = 'none';
            }
            return true;
        },
        populateSenderReview: function (form) {
            var _a, _b, _c, _d, _e, _f;
            const container = document.getElementById('review-sender-info');
            if (!container)
                return;
            const senderName = ((_a = form.querySelector('[name="SenderName"]')) === null || _a === void 0 ? void 0 : _a.value) || '';
            const senderEmail = ((_b = form.querySelector('[name="SenderEmail"]')) === null || _b === void 0 ? void 0 : _b.value) || '';
            const senderPhone = ((_c = form.querySelector('[name="SenderPhone"]')) === null || _c === void 0 ? void 0 : _c.value) || '';
            const senderAddress = ((_d = form.querySelector('[name="SenderAddress"]')) === null || _d === void 0 ? void 0 : _d.value) || '';
            const senderCity = ((_e = form.querySelector('[name="SenderCity"] option:checked')) === null || _e === void 0 ? void 0 : _e.text) || '';
            const senderPostalCode = ((_f = form.querySelector('[name="SenderPostalCode"]')) === null || _f === void 0 ? void 0 : _f.value) || '';
            const emailLabel = this.getLabel('emailAddress', 'Email');
            const phoneLabel = this.getLabel('phoneNumber', 'Phone');
            container.innerHTML = `
                <p><strong>${senderName}</strong></p>
                <p>${senderAddress}</p>
                <p>${senderCity}, ${senderPostalCode}</p>
                <p>${emailLabel}: ${senderEmail}</p>
                <p>${phoneLabel}: ${senderPhone}</p>
            `;
        },
        populateReceiverReview: function (form) {
            var _a, _b, _c, _d, _e, _f;
            const container = document.getElementById('review-receiver-info');
            if (!container)
                return;
            const receiverName = ((_a = form.querySelector('[name="ReceiverName"]')) === null || _a === void 0 ? void 0 : _a.value) || '';
            const receiverEmail = ((_b = form.querySelector('[name="ReceiverEmail"]')) === null || _b === void 0 ? void 0 : _b.value) || '';
            const receiverPhone = ((_c = form.querySelector('[name="ReceiverPhone"]')) === null || _c === void 0 ? void 0 : _c.value) || '';
            const receiverAddress = ((_d = form.querySelector('[name="ReceiverAddress"]')) === null || _d === void 0 ? void 0 : _d.value) || '';
            const receiverCity = ((_e = form.querySelector('[name="ReceiverCity"] option:checked')) === null || _e === void 0 ? void 0 : _e.text) || '';
            const receiverPostalCode = ((_f = form.querySelector('[name="ReceiverPostalCode"]')) === null || _f === void 0 ? void 0 : _f.value) || '';
            const emailLabel = this.getLabel('emailAddress', 'Email');
            const phoneLabel = this.getLabel('phoneNumber', 'Phone');
            container.innerHTML = `
                <p><strong>${receiverName}</strong></p>
                <p>${receiverAddress}</p>
                <p>${receiverCity}, ${receiverPostalCode}</p>
                <p>${emailLabel}: ${receiverEmail}</p>
                <p>${phoneLabel}: ${receiverPhone}</p>
            `;
        },
        populatePackageReview: function (form) {
            var _a, _b, _c, _d, _e, _f;
            const container = document.getElementById('review-package-info');
            if (!container)
                return;
            const width = ((_a = form.querySelector('[name="Width"]')) === null || _a === void 0 ? void 0 : _a.value) || '0';
            const height = ((_b = form.querySelector('[name="Height"]')) === null || _b === void 0 ? void 0 : _b.value) || '0';
            const length = ((_c = form.querySelector('[name="Length"]')) === null || _c === void 0 ? void 0 : _c.value) || '0';
            const weight = ((_d = form.querySelector('[name="Weight"]')) === null || _d === void 0 ? void 0 : _d.value) || '0';
            const packageValue = ((_e = form.querySelector('[name="PackageValue"]')) === null || _e === void 0 ? void 0 : _e.value) || '0';
            const packaging = ((_f = form.querySelector('[name="ShippingPackging"] option:checked')) === null || _f === void 0 ? void 0 : _f.text) || '';
            const dimensionsLabel = this.getLabel('dimensions', 'Dimensions');
            const weightLabel = this.getLabel('weight', 'Weight');
            const packageValueLabel = this.getLabel('packageValueLabel', 'Package Value');
            const packagingTypeLabel = this.getLabel('packagingType', 'Packaging Type');
            const poundsAbbr = this.getLabel('poundsAbbreviation', 'lbs');
            container.innerHTML = `
                <div class="row">
                    <div class="col-6">
                        <p><strong>${dimensionsLabel}:</strong> ${length}" x ${width}" x ${height}"</p>
                        <p><strong>${weightLabel}:</strong> ${weight} ${poundsAbbr}</p>
                    </div>
                    <div class="col-6">
                        <p><strong>${packageValueLabel}:</strong> $${parseFloat(packageValue).toFixed(2)}</p>
                        <p><strong>${packagingTypeLabel}:</strong> ${packaging}</p>
                    </div>
                </div>
            `;
        },
        populateShippingReview: function (form) {
            var _a, _b, _c, _d;
            const container = document.getElementById('review-shipping-info');
            if (!container)
                return;
            const shippingType = ((_a = form.querySelector('[name="ShippingTypes"] option:checked')) === null || _a === void 0 ? void 0 : _a.text) || '';
            const shippingDate = ((_b = form.querySelector('[name="ShippingDate"]')) === null || _b === void 0 ? void 0 : _b.value) || '';
            const deliveryDate = ((_c = form.querySelector('[name="DelivryDate"]')) === null || _c === void 0 ? void 0 : _c.value) || '';
            const shippingRate = parseFloat(((_d = form.querySelector('[name="ShippingRate"]')) === null || _d === void 0 ? void 0 : _d.value) || 0);
            const estimatedRateLabel = this.getLabel('estimatedRate', 'Estimated Rate');
            const shippingTypeLabel = this.getLabel('shippingType', 'Shipping Type');
            const shippingDateLabel = this.getLabel('shippingDate', 'Shipping Date');
            const estimatedDeliveryLabel = this.getLabel('estimatedDelivery', 'Estimated Delivery');
            const toBeDeterminedText = this.getLabel('toBeDetermined', 'To be determined');
            let rateHtml = '';
            if (shippingRate > 0) {
                rateHtml = `<p><strong>${estimatedRateLabel}:</strong> $${shippingRate.toFixed(2)}</p>`;
            }
            container.innerHTML = `
                <p><strong>${shippingTypeLabel}:</strong> ${shippingType}</p>
                <p><strong>${shippingDateLabel}:</strong> ${shippingDate}</p>
                <p><strong>${estimatedDeliveryLabel}:</strong> ${deliveryDate || toBeDeterminedText}</p>
                ${rateHtml}
            `;
        },
        // --------------------------------------------------------------------
        // DISABLED - populatePaymentReview
        // Payment review is not needed during shipment creation
        // --------------------------------------------------------------------
        populatePaymentReview_DISABLED: function (form) {
            var _a, _b, _c;
            const container = document.getElementById('review-payment-info');
            if (!container)
                return;
            if (!ShipmentService.validatePaymentMethod()) {
                container.innerHTML = '<p class="text-danger">Please select a payment method</p>';
                return;
            }
            container.innerHTML = '<p class="text-muted"><i class="fa fa-spinner fa-spin"></i> Loading payment details...</p>';
            let shippingRate = parseFloat(((_a = form.querySelector('[name="ShippingRate"]')) === null || _a === void 0 ? void 0 : _a.value) || 0);
            if (shippingRate <= 0) {
                const packageValue = parseFloat(((_b = form.querySelector('[name="PackageValue"]')) === null || _b === void 0 ? void 0 : _b.value) || 0);
                const weight = parseFloat(((_c = form.querySelector('[name="Weight"]')) === null || _c === void 0 ? void 0 : _c.value) || 0);
                shippingRate = Math.max(packageValue * 0.10, weight * 10, 25);
            }
            let rateField = form.querySelector('[name="ShippingRate"]');
            if (!rateField) {
                rateField = document.createElement('input');
                rateField.type = 'hidden';
                rateField.name = 'ShippingRate';
                rateField.value = shippingRate.toFixed(2);
                form.appendChild(rateField);
            }
            else {
                rateField.value = shippingRate.toFixed(2);
            }
            ShipmentService.displayPaymentSummary('#review-payment-info')
                .then(() => {
                console.log('Payment summary loaded successfully');
            })
                .catch(err => {
                console.error('Error displaying payment summary:', err);
                container.innerHTML = `
                        <div class="alert alert-danger">
                            <i class="fa fa-exclamation-triangle"></i> 
                            <strong>Unable to load payment details</strong>
                            <p>Error: ${err.message || 'Unknown error'}</p>
                            <p class="text-muted" style="font-size: 12px;">Please make sure a payment method is selected.</p>
                        </div>
                    `;
            });
        },
        populateConfirmation: function () {
            var _a, _b, _c, _d, _e, _f, _g;
            const container = document.getElementById('confirmation-summary');
            if (!container)
                return;
            const form = document.querySelector('#createShipmentForm');
            if (!form)
                return;
            // --------------------------------------------------------------------
            // COMMENTED OUT - Payment details not retrieved during shipment creation
            // Payment is handled separately after shipment creation
            // --------------------------------------------------------------------
            // const paymentDetails = ShipmentService.getPaymentDetails();
            const paymentDetails = null;
            const naText = this.getLabel('notAvailable', 'N/A');
            const unknownText = this.getLabel('unknown', 'Unknown');
            const paymentReceiptLabel = this.getLabel('paymentReceipt', 'Payment Receipt');
            const paymentMethodLabel = this.getLabel('paymentMethod', 'Payment Method');
            const shippingRateLabel = this.getLabel('shippingRate', 'Shipping Rate');
            const processingFeeLabel = this.getLabel('processingFee', 'Processing Fee');
            const totalChargedLabel = this.getLabel('totalCharged', 'Total Charged');
            const simulatedPaymentDisclaimer = this.getLabel('simulatedPaymentDisclaimer', '(This is a simulated payment for educational purposes - no real money was charged)');
            const shipmentCreatedSuccessfullyLabel = this.getLabel('shipmentCreatedSuccessfully', 'Shipment Created Successfully!');
            const shipmentCreatedAndPaymentProcessedLabel = this.getLabel('shipmentCreatedAndPaymentProcessed', 'payment has been processed');
            const shipmentReadyForProcessingLabel = this.getLabel('shipmentReadyForProcessing', 'is ready for processing');
            const shipmentSummaryLabel = this.getLabel('shipmentSummary', 'Shipment Summary');
            const fromLabel = this.getLabel('fromLabel', 'From');
            const toLabel = this.getLabel('toLabel', 'To');
            const cityLabel = this.getLabel('city', 'City');
            const serviceLabel = this.getLabel('serviceLabel', 'Service');
            const weightLabel = this.getLabel('weight', 'Weight');
            const valueLabel = this.getLabel('valueLabel', 'Value');
            const poundsAbbr = this.getLabel('poundsAbbreviation', 'lbs');
            const confirmationEmailNotice = this.getLabel('confirmationEmailNotice', 'You will receive a confirmation email with your tracking number shortly.');
            const senderName = ((_a = form.querySelector('[name="SenderName"]')) === null || _a === void 0 ? void 0 : _a.value) || naText;
            const receiverName = ((_b = form.querySelector('[name="ReceiverName"]')) === null || _b === void 0 ? void 0 : _b.value) || naText;
            const senderCity = ((_c = form.querySelector('[name="SenderCity"] option:checked')) === null || _c === void 0 ? void 0 : _c.text) || naText;
            const receiverCity = ((_d = form.querySelector('[name="ReceiverCity"] option:checked')) === null || _d === void 0 ? void 0 : _d.text) || naText;
            const shippingType = ((_e = form.querySelector('[name="ShippingTypes"] option:checked')) === null || _e === void 0 ? void 0 : _e.text) || naText;
            const weight = ((_f = form.querySelector('[name="Weight"]')) === null || _f === void 0 ? void 0 : _f.value) || '0';
            const packageValue = ((_g = form.querySelector('[name="PackageValue"]')) === null || _g === void 0 ? void 0 : _g.value) || '0';
            let paymentSection = '';
            if (paymentDetails) {
                const methodName = paymentDetails.paymentMethod.methodEname ||
                    paymentDetails.paymentMethod.MethodEname ||
                    paymentDetails.paymentMethod.methdAname ||
                    paymentDetails.paymentMethod.MethdAname || unknownText;
                const shippingRate = paymentDetails.shippingRate || 0;
                const commissionAmount = paymentDetails.commissionAmount || 0;
                const totalAmount = paymentDetails.totalAmount || 0;
                paymentSection = `
                    <!-- Educational: Payment Receipt -->
                    <div class="payment-receipt" style="margin-top: 20px;">
                        <h5><i class="fa fa-check-circle"></i> ${paymentReceiptLabel}</h5>
                        <div class="receipt-item">
                            <span>${paymentMethodLabel}:</span>
                            <span>${methodName}</span>
                        </div>
                        <div class="receipt-item">
                            <span>${shippingRateLabel}:</span>
                            <span>$${shippingRate.toFixed(2)}</span>
                        </div>
                        <div class="receipt-item">
                            <span>${processingFeeLabel}:</span>
                            <span>$${commissionAmount.toFixed(2)}</span>
                        </div>
                        <div class="receipt-item receipt-total">
                            <span><strong>${totalChargedLabel}:</strong></span>
                            <span><strong>$${totalAmount.toFixed(2)}</strong></span>
                        </div>
                        <p style="font-size: 12px; color: #6c757d; font-style: italic; margin-top: 10px; text-align: center;">
                            ${simulatedPaymentDisclaimer}
                        </p>
                    </div>
                `;
            }
            container.innerHTML = `
                <div class="confirmation-details">
                    <div style="text-align: center; margin-bottom: 30px;">
                        <i class="fa fa-check-circle" style="font-size: 48px; color: #28a745; margin-bottom: 10px;"></i>
                        <h3 style="color: #28a745; margin-bottom: 10px;">${shipmentCreatedSuccessfullyLabel}</h3>
                        <p style="font-size: 16px; color: #6c757d;">
                            ${this.getLabel('createdSuccess', 'Your shipment has been created successfully')} ${paymentDetails ? shipmentCreatedAndPaymentProcessedLabel : shipmentReadyForProcessingLabel}.
                        </p>
                    </div>

                    <!-- Shipment Summary -->
                    <div style="background: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 20px;">
                        <h5 style="margin-bottom: 15px; color: #333;"><i class="fa fa-box"></i> ${shipmentSummaryLabel}</h5>
                        <div class="row">
                            <div class="col-md-6">
                                <p><strong>${fromLabel}:</strong> ${senderName}</p>
                                <p><strong>${cityLabel}:</strong> ${senderCity}</p>
                            </div>
                            <div class="col-md-6">
                                <p><strong>${toLabel}:</strong> ${receiverName}</p>
                                <p><strong>${cityLabel}:</strong> ${receiverCity}</p>
                            </div>
                        </div>
                        <hr style="margin: 10px 0;"/>
                        <div class="row">
                            <div class="col-md-4">
                                <p><strong>${serviceLabel}:</strong> ${shippingType}</p>
                            </div>
                            <div class="col-md-4">
                                <p><strong>${weightLabel}:</strong> ${weight} ${poundsAbbr}</p>
                            </div>
                            <div class="col-md-4">
                                <p><strong>${valueLabel}:</strong> $${parseFloat(packageValue).toFixed(2)}</p>
                            </div>
                        </div>
                    </div>

                    ${paymentSection}

                    <div style="margin-top: 25px; text-align: center;">
                        <p style="font-size: 14px; color: #6c757d;">
                            <i class="fa fa-envelope"></i> ${confirmationEmailNotice}
                        </p>
                    </div>
                </div>
            `;
        },
        // --------------------------------------------------------------------
        // DISABLED - Payment processing overlays
        // Payment processing overlays are not needed during shipment creation
        // --------------------------------------------------------------------
        showPaymentProcessing_DISABLED: function () {
            const overlay = document.getElementById('paymentProcessingOverlay');
            if (overlay) {
                overlay.classList.add('active');
            }
        },
        hidePaymentProcessing_DISABLED: function () {
            const overlay = document.getElementById('paymentProcessingOverlay');
            if (overlay) {
                overlay.classList.remove('active');
            }
        },
        getFirstInvalidField: function (form) {
            if (!form)
                return null;
            const explicitInvalid = form.querySelector('.field-error, .is-invalid, .input-validation-error, :invalid, [aria-invalid="true"]');
            if (explicitInvalid)
                return explicitInvalid;
            const requiredCandidates = form.querySelectorAll('[required], [data-val-required]');
            for (const el of requiredCandidates) {
                const value = (el.value || '').trim();
                if (!value || value === '00000000-0000-0000-0000-000000000000') {
                    el.classList.add('field-error');
                    return el;
                }
            }
            const rules = window.ShipmentValidationRules || {};
            for (const fieldName of Object.keys(rules)) {
                const rule = rules[fieldName];
                const el = form.querySelector(`[name="${fieldName}"]`);
                if (!el || !rule)
                    continue;
                const value = (el.value || '').trim();
                if (rule.required && (!value || value === '00000000-0000-0000-0000-000000000000')) {
                    el.classList.add('field-error');
                    return el;
                }
            }
            return null;
        },
        jumpToFirstInvalidField: function (form) {
            if (!form)
                return;
            const firstInvalid = this.getFirstInvalidField(form);
            if (!firstInvalid)
                return;
            const fieldset = firstInvalid.closest('fieldset');
            const stepIndex = parseInt((fieldset === null || fieldset === void 0 ? void 0 : fieldset.getAttribute('data-step')) || '0', 10);
            if (!isNaN(stepIndex) && window.FormValidator && typeof FormValidator.showStep === 'function') {
                FormValidator.showStep(stepIndex);
            }
            setTimeout(function () {
                if (typeof firstInvalid.focus === 'function') {
                    firstInvalid.focus();
                }
            }, 0);
        },
        bindEditLinks: function () {
            const editLinks = document.querySelectorAll('.btn-edit[data-edit-step]');
            editLinks.forEach(link => {
                link.addEventListener('click', (e) => {
                    e.preventDefault();
                    const stepIndex = parseInt(link.getAttribute('data-edit-step'), 10);
                    if (!isNaN(stepIndex) && window.FormValidator && typeof FormValidator.showStep === 'function') {
                        FormValidator.showStep('#createShipmentForm', stepIndex);
                    }
                });
            });
        }
    };
    // Expose to global scope
    window.ShipmentReview = ShipmentReview;
    // Auto-initialize when document is ready
    $(document).ready(function () {
        console.log('[ShipmentReview.js] document ready');
        // Bind edit links
        ShipmentReview.bindEditLinks();
        // --------------------------------------------------------------------
        // COMMENTED OUT - Payment step validation removed (no payment step in form)
        // --------------------------------------------------------------------
        // const paymentStepNext = document.querySelector('fieldset[data-step="4"] .next');
        // if (paymentStepNext) {
        //     paymentStepNext.addEventListener('click', function (e) {
        //         if (!ShipmentReview.validatePaymentMethodSelected()) {
        //             e.preventDefault();
        //             e.stopPropagation();
        //             return false;
        //         }
        //     });
        // }
        // Listen for step changes to populate review (adjusted for payment-free flow)
        $(document).on('stepShown', function (e, stepIndex) {
            // Step 4 is now Review (was step 5 before payment step removal)
            if (stepIndex === 4) {
                ShipmentReview.populateReviewStep();
            }
            else if (stepIndex === 5) {
                // Step 5 is now Complete (was step 6 before payment step removal)
                ShipmentReview.populateConfirmation();
            }
        });
        // Alternative: if using custom step navigation, hook into it
        const form = document.querySelector('#createShipmentForm');
        if (form) {
            console.log('[ShipmentReview.js] form found, binding final submit validation');
            // Final submit validation: jump to first invalid field/step
            const finalSubmitBtn = form.querySelector('input[name="btnPost"], button[name="btnPost"], .btn-submit-final');
            if (finalSubmitBtn) {
                console.log('[ShipmentReview.js] final submit button found');
                finalSubmitBtn.addEventListener('click', function (e) {
                    console.log('[ShipmentReview.js] final submit button click handler triggered');
                    try {
                        const ok = (window.FormValidator && typeof FormValidator.validate === 'function')
                            ? FormValidator.validate(form, window.ShipmentValidationRules)
                            : true;
                        console.log('[ShipmentReview.js] final submit validation result:', ok);
                        const fallbackInvalid = ShipmentReview.getFirstInvalidField(form);
                        if (!ok || fallbackInvalid) {
                            console.log('[ShipmentReview.js] invalid form -> jumpToFirstInvalidField', fallbackInvalid ? (fallbackInvalid.name || fallbackInvalid.id || fallbackInvalid) : '');
                            e.preventDefault();
                            e.stopPropagation();
                            ShipmentReview.jumpToFirstInvalidField(form);
                            return false;
                        }
                    }
                    catch (err) {
                        console.error('[ShipmentReview.js] final submit validation error:', err);
                    }
                });
            }
            else {
                console.log('[ShipmentReview.js] final submit button NOT found');
            }
            // Monitor step changes via fieldset visibility (adjusted for payment-free flow)
            const observer = new MutationObserver(function () {
                const activeFieldset = form.querySelector('fieldset.active');
                if (activeFieldset) {
                    const stepIndex = parseInt(activeFieldset.getAttribute('data-step'), 10);
                    // Step 4 is now Review (was step 5 before payment step removal)
                    if (stepIndex === 4) {
                        ShipmentReview.populateReviewStep();
                    }
                    else if (stepIndex === 5) {
                        // Step 5 is now Complete (was step 6 before payment step removal)
                        ShipmentReview.populateConfirmation();
                    }
                }
            });
            const fieldsets = form.querySelectorAll('fieldset');
            fieldsets.forEach(fieldset => {
                observer.observe(fieldset, { attributes: true, attributeFilter: ['class'] });
            });
        }
    });
})();
//# sourceMappingURL=ShipmentReview.js.map