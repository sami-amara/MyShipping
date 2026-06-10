/* eslint-disable */
// @ts-nocheck
// ValidationErrorMapper.js — enhanced to show all errors and integrate with FormValidator
const ValidationErrorMapper = (function () {
    'use strict';
    function mapFieldKeyToInputNames(key) {
        if (!key)
            return [];
        key = key.replace(/^\$\./, '').trim();
        const parts = key.split('.');
        const last = parts[parts.length - 1];
        const candidates = new Set();
        candidates.add(key);
        candidates.add(key.replace(/\./g, '_'));
        if (last)
            candidates.add(last);
        if (parts.length >= 2) {
            const prefix = parts[parts.length - 2];
            if (/usersender/i.test(prefix)) {
                candidates.add('Sender' + last);
                candidates.add('UserSender.' + last);
            }
            else if (/userreceiver/i.test(prefix)) {
                candidates.add('Receiver' + last);
                candidates.add('UserReceiver.' + last);
            }
            else {
                candidates.add(prefix + last);
            }
        }
        if (last && last.length > 0) {
            const lowerLast = last.charAt(0).toLowerCase() + last.slice(1);
            candidates.add(lowerLast);
            candidates.add(last.toLowerCase());
        }
        return Array.from(candidates);
    }
    function escapeHtml(str) {
        return String(str || '').replace(/[&<>\"'\/]/g, function (s) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '\"': '&quot;', "'": '&#39;', '/': '&#x2F;' })[s];
        });
    }
    function findInputForKey(formEl, key) {
        if (!formEl || !key)
            return null;
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
            try {
                const el = formEl.querySelector(sel);
                if (el)
                    return el;
            }
            catch (e) { }
        }
        const allInputs = Array.from(formEl.querySelectorAll('input[name], textarea[name], select[name]'));
        const lowerFlat = flat.toLowerCase();
        for (const inp of allInputs) {
            const nm = (inp.getAttribute('name') || '').toLowerCase();
            if (nm === lowerFlat || nm.endsWith(lowerFlat) || nm.includes(lowerFlat))
                return inp;
            if (nm.endsWith(('sender' + lowerFlat).toLowerCase()) || nm.endsWith(('receiver' + lowerFlat).toLowerCase()))
                return inp;
        }
        return null;
    }
    //function showInlineMessage($form, inputRef, message) {
    //    let $field = null;
    //    try {
    //        if (inputRef && inputRef.jquery) $field = $(inputRef).first();
    //        else if (inputRef && inputRef.nodeType) $field = $(inputRef);
    //        else $field = $form.find(inputRef).first();
    //    } catch (e) {
    //        try { $field = $form.find(inputRef).first(); } catch (e) { return false; }
    //    }
    //    if (!$field || $field.length === 0) return false;
    //    let $input = $field;
    //    if (!$field.is('input,textarea,select')) {
    //        const $inner = $field.find('input,textarea,select').first();
    //        if ($inner && $inner.length) $input = $inner;
    //    }
    //    const dom = $input.get(0);
    //    if (!dom) return false;
    //    // Prefer FormValidator.showError if available
    //    if (window.FormValidator && typeof FormValidator.showError === 'function') {
    //        try { FormValidator.showError(dom, message); } catch (e) { /* fallback below */ }
    //    }
    //    // Ensure there is a visible inline message element
    //    try {
    //        const parent = dom.closest('.form-group') || dom.parentElement;
    //        if (parent) {
    //            let errorSpan = parent.querySelector('.field-error-message[data-vem]');
    //            if (!errorSpan) errorSpan = parent.querySelector('.field-error-message');
    //            if (!errorSpan) {
    //                errorSpan = document.createElement('span');
    //                errorSpan.className = 'field-error-message';
    //                errorSpan.setAttribute('data-vem', '1');
    //                parent.appendChild(errorSpan);
    //            }
    //            errorSpan.textContent = String(message);
    //            // Add an inline error icon if not present
    //            let icon = parent.querySelector('.field-error-icon[data-vem]');
    //            if (!icon) {
    //                icon = document.createElement('span');
    //                icon.className = 'field-error-icon';
    //                icon.setAttribute('data-vem', '1');
    //                icon.innerHTML = '&#9888;'; // warning symbol
    //                parent.classList.add('has-field-error');
    //                parent.appendChild(icon);
    //            }
    //        }
    //    } catch (e) { }
    //    // Mark input as errored
    //    try { dom.classList.add('field-error'); } catch (e) { }
    //    try { dom.setAttribute('aria-invalid', 'true'); } catch (e) { }
    //    // Attach handlers to clear messages when user edits
    //    try {
    //        const clearHandler = function () {
    //            const parent = this.closest('.form-group') || this.parentElement;
    //            if (parent) {
    //                parent.querySelectorAll('.field-error-message[data-vem]').forEach(el => el.remove());
    //                parent.querySelectorAll('.field-error-icon[data-vem]').forEach(el => el.remove());
    //                parent.classList.remove('has-field-error');
    //            }
    //            this.classList.remove('field-error');
    //            this.removeAttribute('aria-invalid');
    //            $(this).off('.ValidationErrorMapper');
    //        };
    //        $input.off('.ValidationErrorMapper');
    //        $input.on('input.ValidationErrorMapper change.ValidationErrorMapper', clearHandler);
    //    }
    //    catch (e) { }
    //    return true;
    //}
    //Show an inline message. inputRef may be a selector string, jQuery object or DOM element.
    function showInlineMessage($form, inputRef, message) {
        let $field = null;
        try {
            if (inputRef && inputRef.jquery)
                $field = $(inputRef).first();
            else if (inputRef && inputRef.nodeType)
                $field = $(inputRef);
            else
                $field = $form.find(inputRef).first();
        }
        catch (e) {
            try {
                $field = $form.find(inputRef).first();
            }
            catch (e) {
                return false;
            }
        }
        if (!$field || $field.length === 0)
            return false;
        let $input = $field;
        if (!$field.is('input,textarea,select')) {
            const $inner = $field.find('input,textarea,select').first();
            if ($inner && $inner.length)
                $input = $inner;
        }
        const dom = $input.get(0);
        if (!dom)
            return false;
        // If FormValidator.showError exists prefer it (it should handle state/clearing)
        if (window.FormValidator && typeof FormValidator.showError === 'function') {
            try {
                FormValidator.showError(dom, message);
            }
            catch (e) { /* fallback below */ }
        }
        // Ensure there is a visible inline message element. Reuse if present.
        try {
            // Find or create a span with class field-error-message inside the input's form-group
            const parent = dom.closest('.form-group') || dom.parentElement;
            if (parent) {
                // Prefer existing element that we created (data attribute), otherwise any matching span
                let errorSpan = parent.querySelector('.field-error-message[data-vem]');
                if (!errorSpan)
                    errorSpan = parent.querySelector('.field-error-message');
                if (!errorSpan) {
                    errorSpan = document.createElement('span');
                    errorSpan.className = 'field-error-message text-danger';
                    errorSpan.setAttribute('data-vem', '1');
                    errorSpan.style.fontSize = '0.95em';
                    parent.appendChild(errorSpan);
                }
                // Update text
                if (errorSpan.textContent !== String(message))
                    errorSpan.textContent = message;
                // Add an inline error icon inside the input's container
                try {
                    let icon = parent.querySelector('.field-error-icon[data-vem]');
                    if (!icon) {
                        icon = document.createElement('span');
                        icon.className = 'field-error-icon';
                        icon.setAttribute('data-vem', '1');
                        /* visual styles placed inline to avoid requiring external CSS*/
                        icon.style.position = 'absolute';
                        icon.style.right = '10px';
                        icon.style.top = '50%';
                        icon.style.transform = 'translateY(-50%)';
                        icon.style.pointerEvents = 'none';
                        icon.style.color = '#dc3545';
                        icon.style.fontSize = '1.1em';
                        icon.style.lineHeight = '1';
                        icon.innerHTML = '&#9888;'; // warning symbol
                        // ensure parent positioned so absolute works
                        try {
                            const cs = window.getComputedStyle(parent);
                            if (!cs || cs.position === 'static' || cs.position === '')
                                parent.style.position = 'relative';
                        }
                        catch (e) { }
                        try {
                            parent.classList.add('has-field-error');
                        }
                        catch (e) { }
                        parent.appendChild(icon);
                    }
                }
                catch (e) { /* ignore icon insertion errors */ }
            }
        }
        catch (e) { /* ignore DOM update errors */ }
        // Mark input as errored
        try {
            dom.classList.add('field-error');
        }
        catch (e) { }
        try {
            dom.setAttribute('aria-invalid', 'true');
        }
        catch (e) { }
        // Attach namespaced handlers to clear our messages when user edits the field
        try {
            const clearHandler = function () {
                try {
                    const $this = $(this);
                    // Remove VEM-created messages in parent
                    try {
                        const parent = this.closest('.form-group') || this.parentElement;
                        if (parent) {
                            const our = parent.querySelectorAll('.field-error-message[data-vem]');
                            our.forEach(el => { try {
                                el.remove();
                            }
                            catch (e) { } });
                            // remove icon(s) we created
                            const icons = parent.querySelectorAll('.field-error-icon[data-vem]');
                            icons.forEach(el => { try {
                                el.remove();
                            }
                            catch (e) { } });
                            try {
                                parent.classList.remove('has-field-error');
                            }
                            catch (e) { }
                        }
                    }
                    catch (e) { }
                    // Remove class and aria-invalid
                    try {
                        this.classList.remove('field-error');
                    }
                    catch (e) { }
                    try {
                        this.removeAttribute('aria-invalid');
                    }
                    catch (e) { }
                    // Clean aria-describedby entries we added (if any)
                    try {
                        const described = ($this.attr('aria-describedby') || '').split(/\s+/).filter(Boolean).filter(id => id && id.indexOf('field-error-') !== 0);
                        if (described.length)
                            $this.attr('aria-describedby', described.join(' '));
                        else
                            $this.removeAttr('aria-describedby');
                    }
                    catch (e) { }
                    // Also try to update any Razor data-valmsg-for spans to empty
                    try {
                        const nm = $this.attr('name') || $this.attr('id') || null;
                        if (nm) {
                            const $rv = $form.find(`[data-valmsg-for="${nm}"]`);
                            $rv.each((i, el) => { try {
                                const $el = $(el);
                                $el.text('');
                            }
                            catch (e) { } });
                        }
                    }
                    catch (e) { }
                }
                catch (e) { }
                try {
                    $(this).off('.ValidationErrorMapper');
                }
                catch (e) { }
            };
            // remove previous then attach
            try {
                $input.off('.ValidationErrorMapper');
            }
            catch (e) { }
            $input.on('input.ValidationErrorMapper change.ValidationErrorMapper', clearHandler);
        }
        catch (e) { /* ignore attach errors */ }
        return true;
    }
    function revealStepForField($form, $fieldArg) {
        if (!$fieldArg)
            return;
        const $field = ($fieldArg instanceof jQuery) ? $fieldArg : $($fieldArg);
        if (!$field || $field.length === 0)
            return;
        const $fieldset = $field.closest('fieldset[data-step]');
        if (!$fieldset || $fieldset.length === 0)
            return;
        const stepAttr = $fieldset.attr('data-step');
        const stepIndex = stepAttr !== null ? parseInt(stepAttr, 10) : null;
        const $allFieldsets = $form.find('fieldset[data-step]');
        if ($allFieldsets.length) {
            $allFieldsets.hide();
            $fieldset.show();
        }
        if (window.FormValidator && typeof FormValidator.showStep === 'function' && stepIndex !== null) {
            FormValidator.showStep(stepIndex);
        }
    }
    function mapErrorsToFields(errorMap, formElement) {
        var _a;
        if (!errorMap)
            return false;
        const $form = $(formElement || '#createShipmentForm');
        let errors = errorMap;
        if (errorMap && errorMap.errors && typeof errorMap.errors === 'object') {
            errors = errorMap.errors;
        }
        // Collect mappings so we can reveal the proper step and show all errors in that step
        let anyMapped = false;
        let firstMappedDom = null;
        const mappedEntries = []; // { $el, dom, msg, step }
        Object.keys(errors).forEach(key => {
            const messages = errors[key];
            const msgs = Array.isArray(messages) ? messages : [messages];
            const inputNames = mapFieldKeyToInputNames(key);
            for (const baseName of inputNames) {
                const selectors = [
                    `[name="${baseName}"]`,
                    `#${baseName}`,
                    `[name="${baseName}"] input`,
                    `[name="${baseName}[]"]`,
                    `[name="${baseName}"] textarea`,
                    `[name="${baseName}"] select`
                ];
                for (const sel of selectors) {
                    const $el = $form.find(sel).first();
                    if (!$el || $el.length === 0)
                        continue;
                    const dom = $el.get(0);
                    msgs.forEach(msg => {
                        try {
                            let step = null;
                            try {
                                const $fs = $($el.get(0)).closest('fieldset[data-step]');
                                if ($fs && $fs.length)
                                    step = parseInt($fs.attr('data-step'), 10);
                            }
                            catch (e) { }
                            mappedEntries.push({ $el: $el, dom: dom, msg: msg, step: Number.isInteger(step) ? step : null });
                        }
                        catch (e) { }
                    });
                    anyMapped = true;
                    if (!firstMappedDom)
                        firstMappedDom = dom;
                    break;
                }
            }
        });
        if (!anyMapped)
            return false;
        // Determine target step: lowest numeric step if present, otherwise null
        let targetStep = null;
        mappedEntries.forEach(me => { if (me.step !== null) {
            if (targetStep === null || me.step < targetStep)
                targetStep = me.step;
        } });
        if (targetStep === null && firstMappedDom) {
            try {
                const $fs = $(firstMappedDom).closest('fieldset[data-step]');
                if ($fs && $fs.length)
                    targetStep = parseInt($fs.attr('data-step'), 10);
            }
            catch (e) { }
        }
        try {
            if (targetStep !== null) {
                const anyEntry = mappedEntries.find(me => me.step === targetStep) || { dom: firstMappedDom };
                if (anyEntry && anyEntry.dom)
                    revealStepForField($form, anyEntry.dom);
            }
            else if (firstMappedDom) {
                revealStepForField($form, firstMappedDom);
            }
        }
        catch (e) { }
        // Render all messages that belong to the revealed step (or those without step if none)
        mappedEntries.forEach(me => {
            try {
                const belongs = (targetStep === null) ? (me.step === null) : (me.step === targetStep);
                if (belongs)
                    showInlineMessage($form, me.$el, me.msg);
            }
            catch (e) { }
        });
        // scroll/pulse first error
        try {
            const $first = $form.find('.field-error').first();
            if ($first && $first.length) {
                const dom = $first.get(0);
                if (dom) {
                    if ((_a = window.ScrollHelper) === null || _a === void 0 ? void 0 : _a.scrollToCenter) {
                        try {
                            ScrollHelper.scrollToCenter(dom);
                        }
                        catch (e) { }
                    }
                    else {
                        try {
                            dom.scrollIntoView({ behavior: 'smooth', block: 'center' });
                        }
                        catch (e) { }
                    }
                    try {
                        dom.classList.add('field-pulse');
                        setTimeout(() => dom.classList.remove('field-pulse'), 900);
                    }
                    catch (e) { }
                }
            }
        }
        catch (e) { }
        return true;
    }
    const svc = {
        mapErrorsToFields,
        mapFieldKeyToInputNames,
        findInputForKey,
        showInlineMessage,
        escapeHtml,
        revealStepForField
    };
    return svc;
})();
window.ValidationErrorMapper = ValidationErrorMapper;
//# sourceMappingURL=ValidationErrorMapper.js.map