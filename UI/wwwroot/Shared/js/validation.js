/* eslint-disable no-undef */

//// This module handles field-level validation, error display, and step navigation.
window.FormValidator = (function () {
    // 🔹 Show error message under the field
    function showError(input, message) {
        clearError(input);
        input.classList.add('field-error');

        const parent = input.closest('.input-comment') || input.closest('.form-group');
        if (parent && !parent.querySelector('.field-error-message')) {
            const errorSpan = document.createElement('span');
            errorSpan.className = 'field-error-message text-danger';
            errorSpan.textContent = message;
            parent.appendChild(errorSpan);
        }

        // Hide Razor-generated error span only if it matches the same message
        const backendSpan = parent ? Array.from(parent.querySelectorAll('.text-danger')).find(
            el => !el.classList.contains('field-error-message') && el.textContent.trim() === message
        ) : null;
        if (backendSpan) backendSpan.style.display = 'none';
    }

    // 🔹 Clear error message from the field
    function clearError(input) {
        input.classList.remove('field-error');
        const parent = input.closest('.input-comment') || input.closest('.form-group');
        if (parent) {
            const errorMsg = parent.querySelector('.field-error-message');
            if (errorMsg) errorMsg.remove();

            const backendSpan = parent.querySelector('.text-danger');
            if (backendSpan) backendSpan.style.display = '';
        }
    }

    // 🔹 Validate a single field based on rules
    function validateField(input, fieldRules) {
        clearError(input);
        const value = input.value.trim();

        // Required check
        if (fieldRules.required && !value) {
            showError(input, fieldRules.requiredMessage || 'This field is required.');
            return false;
        }

        // Number validation
        if (input.type === 'number') {
            const numericValue = parseFloat(value);
            if (isNaN(numericValue)) {
                showError(input, fieldRules.requiredMessage || 'This field is required.');
                return false;
            }
            if (fieldRules.min !== undefined && numericValue < fieldRules.min) {
                showError(input, fieldRules.rangeMessage || `Minimum value is ${fieldRules.min}.`);
                return false;
            }
            if (fieldRules.max !== undefined && numericValue > fieldRules.max) {
                showError(input, fieldRules.rangeMessage || `Maximum value is ${fieldRules.max}.`);
                return false;
            }
        }

        // String length check
        if (fieldRules.minLength && value.length < fieldRules.minLength) {
            showError(input, fieldRules.minLengthMessage || `Minimum length is ${fieldRules.minLength}.`);
            return false;
        }

        // Pattern check
        if (fieldRules.pattern && !fieldRules.pattern.test(value)) {
            showError(input, fieldRules.patternMessage || 'Invalid format.');
            return false;
        }

        return true;
    }

    // 🔹 Validate the entire form
    function validate(form, rules) {
        let isValid = true;

        Object.keys(rules).forEach(fieldName => {
            const input = form.querySelector(`[name="${fieldName}"]`);
            if (!input) return;
            const fieldValid = validateField(input, rules[fieldName]);
            if (!fieldValid) isValid = false;
        });

        // Disable continue button if validation fails
        const continueBtn = form.querySelector('button[type="button"], .next');
        if (continueBtn) {
            continueBtn.disabled = !isValid;
        }

        return isValid;
    }

    // 🔹 Attach validation to form submit and input events
    function attach(formSelector, rules) {
        const form = document.querySelector(formSelector);
        if (!form) return;

        form.addEventListener('submit', function (e) {
            const isValid = validate(form, rules);
            if (!isValid) {
                e.preventDefault();
                const firstError = form.querySelector('.field-error');
                if (firstError && window.ScrollHelper?.scrollToCenter) {
                    ScrollHelper.scrollToCenter(firstError);
                }
            }
        });

        Object.keys(rules).forEach(fieldName => {
            const input = form.querySelector(`[name="${fieldName}"]`);
            if (input) {
                input.addEventListener('input', () => validateField(input, rules[fieldName]));
                input.addEventListener('change', () => validateField(input, rules[fieldName]));
            }
        });
    }

    // Robust showStep: find fieldset by data-step attribute (not array index),
    // set explicit inline styles and update progressbar accordingly.
    function showStep(index) {
        const form = document.querySelector('#createShipmentForm');
        if (!form) return;

        const targetStep = Number(index) || 0;
        const fieldsets = Array.from(form.querySelectorAll('fieldset[data-step]'));

        // hide/show based on data-step attribute
        fieldsets.forEach(fs => {
            const stepAttr = fs.getAttribute('data-step');
            const stepNum = stepAttr != null ? parseInt(stepAttr, 10) : null;
            if (stepNum === targetStep) {
                fs.style.display = 'block';
                fs.style.opacity = '1';
                fs.style.visibility = 'visible';
                fs.style.position = 'relative';
                fs.classList.add('active');
            } else {
                fs.style.display = 'none';
                fs.style.opacity = '0';
                fs.style.visibility = 'hidden';
                fs.style.position = 'absolute';
                fs.classList.remove('active');
            }
        });

        // update progressbar (li index corresponds to logical step 0..N)
        const progressItems = Array.from(form.querySelectorAll('#progressbar li'));
        if (progressItems.length) {
            progressItems.forEach((li, i) => {
                if (i <= targetStep) li.classList.add('active'); else li.classList.remove('active');
            });
        }

        // focus first focusable in active fieldset
        const activeFs = form.querySelector(`fieldset[data-step="${targetStep}"]`);
        if (activeFs) {
            const focusable = activeFs.querySelector('input, select, textarea, button, a');
            if (focusable) {
                try { focusable.focus(); } catch (e) { /* ignore */ }
            }
        }

        try { window.scrollTo({ top: 0, behavior: 'smooth' }); } catch (e) { /* ignore */ }
    }

    // 🔹 Step-by-step validation for multi-fieldset forms
    function enableStepValidation(formSelector, rules) {
        const form = document.querySelector(formSelector);
        if (!form) return;

        const allFieldsets = Array.from(form.querySelectorAll('fieldset[data-step]'));
        const maxStep = allFieldsets.length ? Math.max(...allFieldsets.map(f => parseInt(f.getAttribute('data-step') || '0', 10))) : 0;

        // determine current step from active fieldset or default to first defined one
        function getCurrentStepFromDom() {
            const activeFs = form.querySelector('fieldset[data-step].active') || form.querySelector('fieldset[data-step]');
            if (!activeFs) return 0;
            const s = activeFs.getAttribute('data-step');
            return s != null ? parseInt(s, 10) : 0;
        }

        function validateCurrentStep(step) {
            const currentFieldset = form.querySelector(`fieldset[data-step="${step}"]`);
            if (!currentFieldset) return true;
            const inputs = currentFieldset.querySelectorAll('[name]');
            let isValid = true;
            inputs.forEach(input => {
                const name = input.name;
                if (name && rules[name]) {
                    const valid = validateField(input, rules[name]);
                    if (!valid) isValid = false;
                }
            });
            return isValid;
        }

        // wire next buttons
        form.querySelectorAll('.next').forEach(btn => {
            btn.addEventListener('click', function () {
                const currentStep = getCurrentStepFromDom();
                if (validateCurrentStep(currentStep)) {
                    const nextStep = Math.min(currentStep + 1, maxStep);
                    showStep(nextStep);
                } else {
                    const firstError = form.querySelector(`fieldset[data-step="${currentStep}"] .field-error`);
                    if (firstError && window.ScrollHelper?.scrollToCenter) {
                        ScrollHelper.scrollToCenter(firstError);
                    }
                }
            });
        });

        // previous controls
        form.querySelectorAll('.previous, .prev').forEach(btn => {
            btn.addEventListener('click', function () {
                const currentStep = getCurrentStepFromDom();
                const prevStep = Math.max(0, currentStep - 1);
                showStep(prevStep);
            });
        });

        // expose individual field validation on change/input events already handled by attach
        // ensure initial rendering follows DOM/data-step
        showStep(getCurrentStepFromDom());
    }

    // 🔹 Public API exposed to other modules
    return {
        attach,
        validate,
        enableStepValidation,
        validateField,
        showStep // ✅ expose this function
    };
})();





////window.FormValidator = (function () {
////    // 🔹 Show error message under the field
////    function showError(input, message) {
////        clearError(input);
////        input.classList.add('field-error');

////        const parent = input.closest('.input-comment') || input.closest('.form-group');
////        if (parent && !parent.querySelector('.field-error-message')) {
////            const errorSpan = document.createElement('span');
////            errorSpan.className = 'field-error-message text-danger';
////            errorSpan.textContent = message;
////            parent.appendChild(errorSpan);
////        }

////        // Hide Razor-generated error span only if it matches the same message
////        const backendSpan = parent ? Array.from(parent.querySelectorAll('.text-danger')).find(
////            el => !el.classList.contains('field-error-message') && el.textContent.trim() === message
////        ) : null;
////        if (backendSpan) backendSpan.style.display = 'none';
////    }

////    // 🔹 Clear error message from the field
////    function clearError(input) {
////        input.classList.remove('field-error');
////        const parent = input.closest('.input-comment') || input.closest('.form-group');
////        if (parent) {
////            const errorMsg = parent.querySelector('.field-error-message');
////            if (errorMsg) errorMsg.remove();

////            const backendSpan = parent.querySelector('.text-danger');
////            if (backendSpan) backendSpan.style.display = '';
////        }
////    }

////    // 🔹 Validate a single field based on rules
////    function validateField(input, fieldRules) {
////        clearError(input);
////        const value = input.value.trim();

////        // Required check
////        if (fieldRules.required && !value) {
////            showError(input, fieldRules.requiredMessage || 'This field is required.');
////            return false;
////        }

////        // Number validation
////        if (input.type === "number") {
////            const numericValue = parseFloat(value);
////            if (isNaN(numericValue)) {
////                showError(input, fieldRules.requiredMessage || 'This field is required.');
////                return false;
////            }
////            if (fieldRules.min !== undefined && numericValue < fieldRules.min) {
////                showError(input, fieldRules.rangeMessage || `Minimum value is ${fieldRules.min}.`);
////                return false;
////            }
////            if (fieldRules.max !== undefined && numericValue > fieldRules.max) {
////                showError(input, fieldRules.rangeMessage || `Maximum value is ${fieldRules.max}.`);
////                return false;
////            }
////        }

////        // String length check
////        if (fieldRules.minLength && value.length < fieldRules.minLength) {
////            showError(input, fieldRules.minLengthMessage || `Minimum length is ${fieldRules.minLength}.`);
////            return false;
////        }

////        // Pattern check
////        if (fieldRules.pattern && !fieldRules.pattern.test(value)) {
////            showError(input, fieldRules.patternMessage || 'Invalid format.');
////            return false;
////        }

////        return true;
////    }

////    // 🔹 Validate the entire form
////    function validate(form, rules) {
////        let isValid = true;

////        Object.keys(rules).forEach(fieldName => {
////            const input = form.querySelector(`[name="${fieldName}"]`);
////            if (!input) return;
////            const fieldValid = validateField(input, rules[fieldName]);
////            if (!fieldValid) isValid = false;
////        });

////        // Disable continue button if validation fails
////        const continueBtn = form.querySelector('button[type="button"], .next');
////        if (continueBtn) {
////            continueBtn.disabled = !isValid;
////        }

////        return isValid;
////    }

////    // 🔹 Attach validation to form submit and input events
////    function attach(formSelector, rules) {
////        const form = document.querySelector(formSelector);
////        if (!form) return;

////        form.addEventListener('submit', function (e) {
////            const isValid = validate(form, rules);
////            if (!isValid) {
////                e.preventDefault();
////                const firstError = form.querySelector('.field-error');
////                if (firstError && window.ScrollHelper?.scrollToCenter) {
////                    ScrollHelper.scrollToCenter(firstError);
////                }
////            }
////        });
        
////        //form.addEventListener('submit', function (e) {
////        //    const isValid = validate(form, rules);
////        //    // REMOVE e.preventDefault() so the form submits regardless
////        //});




////        Object.keys(rules).forEach(fieldName => {
////            const input = form.querySelector(`[name="${fieldName}"]`);
////            if (input) {
////                input.addEventListener('input', () => validateField(input, rules[fieldName]));
////                input.addEventListener('change', () => validateField(input, rules[fieldName]));
////            }
////        });
////    }

////    //function showStep(index) {
////    //    const fieldsets = document.querySelectorAll('fieldset');
////    //    fieldsets.forEach((fs, i) => {
////    //        fs.style.display = i === index ? 'block' : 'none';
////    //    });
////    //}



////    //function showStep(index, formSelector = '#createShipmentForm') {
////    //    const form = document.querySelector(formSelector);
////    //    if (!form) return;

////    //    const fieldsets = form.querySelectorAll('fieldset');
////    //    fieldsets.forEach((fs, i) => {
////    //        fs.style.display = i === index ? 'block' : 'none';
////    //    });
////    //}


   

////    function showStep(index) {
////        const form = document.querySelector('#createShipmentForm');
////        if (!form) return;

////        const fieldsets = form.querySelectorAll('fieldset[data-step]');
////        fieldsets.forEach((fs, i) => {
////            fs.classList.toggle('active', i === index);
////        });
////    }






////    // 🔹 Step-by-step validation for multi-fieldset forms
////    function enableStepValidation(formSelector, rules) {


////        const form = document.querySelector(formSelector);
////        if (!form) return;

////        const fieldsets = form.querySelectorAll('fieldset');
////        let currentStep = 0;

////        function validateCurrentStep() {
////            const currentFieldset = fieldsets[currentStep];
////            const inputs = currentFieldset.querySelectorAll('[name]');
////            let isValid = true;

////            inputs.forEach(input => {
////                const name = input.name;
////                if (name && rules[name]) {
////                    const valid = validateField(input, rules[name]);
////                    if (!valid) isValid = false;
////                }
////            });

////            return isValid;
////        }

////        form.querySelectorAll('.next').forEach(btn => {
////            btn.addEventListener('click', function () {
////                if (validateCurrentStep()) {
////                    currentStep++;
////                    showStep(currentStep); // ✅ global version
////                    window.scrollTo({ top: 0, behavior: 'smooth' });
////                } else {
////                    const firstError = fieldsets[currentStep].querySelector('.field-error');
////                    if (firstError && window.ScrollHelper?.scrollToCenter) {
////                        ScrollHelper.scrollToCenter(firstError);
////                    }
////                }
////            });
////        });

////        form.querySelectorAll('.prev').forEach(btn => {
////            btn.addEventListener('click', function () {
////                currentStep = Math.max(0, currentStep - 1);
////                showStep(currentStep);
////            });
////        });

////        showStep(currentStep);


////        //const form = document.querySelector(formSelector);
////        //if (!form) return;

////        //const fieldsets = form.querySelectorAll('fieldset');
////        //let currentStep = 0;

////        //function showStep(index) {
////        //    fieldsets.forEach((fs, i) => {
////        //        fs.style.display = i === index ? 'block' : 'none';
////        //    });
////        //}
      
////        //function validateCurrentStep() {
////        //    const currentFieldset = fieldsets[currentStep];
////        //    const inputs = currentFieldset.querySelectorAll('[name]');
////        //    let isValid = true;

////        //    inputs.forEach(input => {
////        //        const name = input.name;
////        //        if (name && rules[name]) {
////        //            const valid = validateField(input, rules[name]);
////        //            if (!valid) isValid = false;
////        //        }
////        //    });

////        //    return isValid;
////        //}

////        //form.querySelectorAll('.next').forEach(btn => {
////        //    btn.addEventListener('click', function () {
////        //        if (validateCurrentStep()) {
////        //            currentStep++;
////        //            showStep(currentStep);
////        //            window.scrollTo({ top: 0, behavior: 'smooth' });
////        //        } else {
////        //            const firstError = fieldsets[currentStep].querySelector('.field-error');
////        //            if (firstError && window.ScrollHelper?.scrollToCenter) {
////        //                ScrollHelper.scrollToCenter(firstError);
////        //            }
////        //        }
////        //    });
////        //});
       


////        //form.querySelectorAll('.prev').forEach(btn => {
////        //    btn.addEventListener('click', function () {
////        //        currentStep = Math.max(0, currentStep - 1);
////        //        showStep(currentStep);
////        //    });
////        //});

////        //showStep(currentStep);
////    }

////    // 🔹 Public API exposed to other modules
////    return {
////        attach,
////        validate,
////        enableStepValidation,
////        validateField,
////        showStep // ✅ expose this function


////    };
////})();










































