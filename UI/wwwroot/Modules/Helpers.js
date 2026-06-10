/* eslint-disable no-undef */
// Central client helpers: error mapping, input resolution, simple formatters
(function () {
    'use strict';

    // Try to extract payload from various xhr-like shapes
    function _unwrapPayload(x) {
        if (!x) return null;
        if (x.responseJSON) return x.responseJSON;
        if (x.response) return x.response;
        return x;
    }

    function findInputForKey(form, key) {
        if (!form || !key) return null;
        const flat = ('' + key).split('.').pop();
        const candidates = [
            `[name="${key}"]`,
            `[name="${key.replace(/\./g, '_')}"]`,
            `[name="${flat}"]`,
            `[name="Sender${flat}"]`,
            `[name="Receiver${flat}"]`,
            `[name="UserSender.${flat}"]`,
            `[name="UserReceiver.${flat}"]`,
            `[name="${flat.toLowerCase()}"]`
        ];
        for (const sel of candidates) {
            try { const el = form.querySelector(sel); if (el) return el; } catch (e) { /* ignore invalid selectors */ }
        }

        const allInputs = Array.from(form.querySelectorAll('input[name], textarea[name], select[name]'));
        const lowerFlat = flat.toLowerCase();
        for (const inp of allInputs) {
            const nm = (inp.getAttribute('name') || '').toLowerCase();
            if (nm === lowerFlat || nm.endsWith(lowerFlat) || nm.includes(lowerFlat)) return inp;
            if (nm.endsWith(('sender' + lowerFlat).toLowerCase()) || nm.endsWith(('receiver' + lowerFlat).toLowerCase())) return inp;
        }
        return null;
    }

    function attachInlineError(inputEl, message, form) {
        if (!inputEl) return;
        try {
            if (window.FormValidator && typeof FormValidator.validateField === 'function') {
                FormValidator.validateField(inputEl, { required: true, requiredMessage: message });
                return;
            }
        } catch (e) { /* fallthrough to jQuery fallback */ }

        try {
            const $input = $(inputEl);
            const $group = $input.closest('.form-group');
            if ($group.length) {
                if ($group.find('.field-error-message').length === 0) $group.append(`<span class="field-error-message text-danger" style="font-size:0.95em;">${message}</span>`);
                else $group.find('.field-error-message').first().text(message);
            } else {
                if ($input.next('.field-error-message').length === 0) $input.after(`<span class="field-error-message text-danger" style="font-size:0.95em;">${message}</span>`);
                else $input.next('.field-error-message').first().text(message);
            }
            $input.addClass('field-error');
        } catch (e) {
            try { inputEl.classList.add('field-error'); } catch { }
        }
    }

    // Original implementation moved to an internal function mapServerErrorsImpl.
    
    function mapServerErrorsImpl(raw, form) {
        if (!raw || !form) return false;
        const payload = _unwrapPayload(raw);
        let errorMap = null;
        if (payload && typeof payload === 'object') {
            if (payload.errors) errorMap = payload.errors;
            else if (payload.Errors) errorMap = payload.Errors;
            else errorMap = payload;
        }

        let hasFieldErrors = false;
        let firstErrorInput = null;
        let firstErrorStep = null;

        try {
            if (errorMap && typeof errorMap === 'object') {
                Object.entries(errorMap).forEach(([field, messages]) => {
                    const msgs = Array.isArray(messages) ? messages : (typeof messages === 'string' ? [messages] : []);
                    const input = findInputForKey(form, field);
                    if (input && msgs.length > 0) {
                        hasFieldErrors = true;
                        attachInlineError(input, msgs[0], form);
                        if (!firstErrorInput) {
                            firstErrorInput = input;
                            const fs = input.closest('fieldset');
                            firstErrorStep = fs ? parseInt(fs.getAttribute('data-step') || '0') : null;
                        }
                    }
                });
            }
        } catch (ex) { console.error('mapServerErrors: mapping failed', ex); }

        if (hasFieldErrors) {
            try {
                if (typeof FormValidator?.showStep === 'function' && firstErrorStep !== null) FormValidator.showStep(firstErrorStep);
                if (firstErrorInput && window.ScrollHelper?.scrollToCenter) ScrollHelper.scrollToCenter(firstErrorInput);
                else if (firstErrorInput) { try { firstErrorInput.scrollIntoView({ behavior: 'smooth', block: 'center' }); firstErrorInput.focus(); } catch (e) { } }
            } catch (e) {  /*ignore*/  }
            return true;
        }

        // jQuery Validate integration: try to showErrors when validator present
        try {
            const $form = $(form);
            const validatorErrors = {};
            if ($ && $.validator && $form.length) {
                // build simple errors object from payload shape
                if (errorMap && typeof errorMap === 'object') {
                    Object.entries(errorMap).forEach(([field, messages]) => {
                        const flatField = ('' + field).split('.').pop();
                        const msg = Array.isArray(messages) ? messages[0] : (typeof messages === 'string' ? messages : null);
                        if (msg) validatorErrors[flatField] = msg;
                    });
                }
                if (Object.keys(validatorErrors).length > 0) {
                    try {
                        const validator = $form.validate();
                        if (validator && typeof validator.showErrors === 'function') {
                            validator.showErrors(validatorErrors);
                            Object.keys(validatorErrors).forEach(nm => {
                                const el = form.querySelector(`[name="${nm}"]`);
                                if (el) el.classList.add('is-invalid');
                            });
                            return true;
                        }
                    } catch (e) {  /*ignore showErrors issues*/  }
                }
            }
        } catch (e) {  /*ignore*/  }

        return false;
    }
    

    // Public wrapper that prefers ValidationErrorMapper then falls back to local impl
    function mapServerErrors(raw, form) {
        try {
            if (window.ValidationErrorMapper && typeof ValidationErrorMapper.mapErrorsToFields === 'function') {
                try { return !!ValidationErrorMapper.mapErrorsToFields(raw, form); } catch (e) { console.warn('ValidationErrorMapper.mapErrorsToFields threw', e); }
            }
        } catch (ex) {
            console.warn('ClientHelpers.mapServerErrors wrapper failed', ex);
        }

        // Fallback: no-op here (original implementation was moved to a commented mapServerErrorsImpl above).
        return false;
    }

    function extractFirstMessage(errors) {
        try {
            if (!errors) return null;
            if (typeof errors === 'string') return errors.replace(/<[^>]+>/g, '').trim();
            const xhrLike = (obj) => obj && (obj.responseJSON || obj.response || obj.responseText || obj.statusText);
            if (xhrLike(errors)) {
                const rj = errors.responseJSON || errors.response || null;
                if (rj) {
                    const m = rj.Message || rj.message || rj.Title || rj.title;
                    if (m) return Array.isArray(m) ? String(m[0]) : String(m);
                    const inner = rj.errors || rj.Errors || rj.Data?.errors || rj.Data?.Errors;
                    const fm = extractFirstMessage(inner);
                    if (fm) return fm;
                }
                const txt = errors.responseText || errors.statusText || null;
                if (txt) return ('' + txt).replace(/<[^>]+>/g, '').trim() || null;
            }
            if (Array.isArray(errors)) {
                for (const it of errors) {
                    if (!it) continue;
                    if (typeof it === 'string') return it.trim();
                    if (typeof it === 'object') {
                        const msg = it.Message || it.message || it.Error || it.error;
                        if (msg) return Array.isArray(msg) ? String(msg[0]) : String(msg);
                        const nested = it.errors || it.Errors;
                        if (nested) {
                            const fm = extractFirstMessage(nested);
                            if (fm) return fm;
                        }
                    }
                }
            }
            if (typeof errors === 'object') {
                const msg = errors.Message || errors.message || errors.Title || errors.title;
                if (msg && typeof msg === 'string' && msg.trim().length > 0) return msg;
                const dataErr = (errors.Data && (errors.Data.Errors || errors.Data.errors)) || errors.Errors || errors.errors;
                if (dataErr) {
                    const fm = extractFirstMessage(dataErr);
                    if (fm) return fm;
                }
                const keys = Object.keys(errors || {});
                for (const k of keys) {
                    const v = errors[k];
                    if (!v) continue;
                    if (Array.isArray(v) && v.length) return String(v[0]);
                    if (typeof v === 'string' && v.trim().length) return v;
                    if (typeof v === 'object') {
                        const nestedMsg = v.Message || v.message;
                        if (nestedMsg) return Array.isArray(nestedMsg) ? String(nestedMsg[0]) : String(nestedMsg);
                        if (v.Errors || v.errors) {
                            const fm = extractFirstMessage(v.Errors || v.errors);
                            if (fm) return fm;
                        }
                    }
                }
            }
        } catch (e) { console.warn('extractFirstMessage failed', e); }
        return null;
    }

    function formatCurrency(v, currency = 'USD', locale = (navigator.language || 'en-US')) {
        if (v === null || v === undefined || v === '') return '-';
        try { return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(Number(v)); } catch { return String(v); }
    }

    function formatDate(d) {
        if (!d) return '-';
        try { const dt = new Date(d); if (isNaN(dt.getTime())) return String(d); return dt.toLocaleString(); } catch { return String(d); }
    }

        window.ClientHelpers = window.ClientHelpers || {
            findInputForKey,
            attachInlineError,
            mapServerErrors,
            extractFirstMessage,
            formatCurrency,
            formatDate
        };

})();




