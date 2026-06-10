

/* eslint-disable no-undef */
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) {
        return value instanceof P ? value : new P(function (resolve)
        { resolve(value); });
    }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) {
            try {
                step(generator.next(value));
            } catch (e) { reject(e); }
        }
        function rejected(value) {
            try {
                step(generator["throw"](value));
            } catch (e) { reject(e); }
        }
        function step(result) {
            result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected);
        }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
const LoginHandler = (function () {
    'use strict';
    return {
        init: function (formSelector = '#loginForm') {
            const form = document.querySelector(formSelector);
            if (!form)
                return;
            form.addEventListener('submit', (e) => __awaiter(this, void 0, void 0, function* () {
                //.preventDefault();
                yield this.handleLogin(form);
            }));
        },
        handleLogin: function (form) {
            return __awaiter(this, void 0, void 0, function* () {
                var _a, _b;
                const email = (_a = form.querySelector('[name="Email"]')) === null
                    || _a === void 0 ? void 0 : _a.value;
                const password = (_b = form.querySelector('[name="Password"]')) === null
                    || _b === void 0 ? void 0 : _b.value;
                if (!email || !password) {
                    this.showError('Please enter email and password');
                    return;
                }
                const submitButton = form.querySelector('button[type="submit"]');
                if (submitButton)
                    submitButton.disabled = true;
                try {
                    // ✅ UPDATED: Use credentials: 'include' for cookies
                    const response = yield fetch('/api/auth/login', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        credentials: 'include', // ✅ CRITICAL: Send/receive cookies
                        body: JSON.stringify({ email, password })
                    });

                    // ✅ NEW: Check for rate limiting FIRST
                    if (response.status === 429) {
                        const rateLimitData = yield response.json();
                        console.warn('⚠️ Rate limit exceeded', rateLimitData);

                        // Store retry info and reload page
                        sessionStorage.setItem('rateLimitRetry', rateLimitData.retryAfterSeconds || 300);

                        // Reload page to show server-side countdown
                        window.location.reload();
                        return;
                    }

                    const apiResponse = yield response.json();
                    // ✅ UPDATED: Use your ApiResponse format
                    if (apiResponse.isSuccess) {
                        // Store access token
                        localStorage.setItem('accessToken', apiResponse.data.accessToken);
                        this.showSuccess(apiResponse.message || 'Login successful');
                        // Redirect after short delay
                        setTimeout(() => {
                            window.location.href = '/Shipments/Index';
                        }, 1000);
                    }
                    else {
                        // ✅ UPDATED: Handle structured errors
                        this.handleLoginError(apiResponse);
                    }
                }
                catch (error) {
                    console.error('Login error:', error);
                    this.showError('An error occurred during login. Please try again.');
                }
                finally {
                    if (submitButton)
                        submitButton.disabled = false;
                }
            });
        },
        handleLoginError: function (apiResponse) {
            // ✅ UPDATED: Extract errors from your format
            let errorMessage = apiResponse.message || 'Login failed';
            if (apiResponse.errors && Array.isArray(apiResponse.errors) &&
                apiResponse.errors.length > 0) {
                // Show first error description
                errorMessage = apiResponse.errors[0].description || errorMessage;
                // Log all errors for debugging
                console.warn('Login errors:', apiResponse.errors);
                // Optionally show all errors
                apiResponse.errors.forEach(err => {
                    console.error(`${err.code}: ${err.description}`);
                });
            }
            this.showError(errorMessage);
        },
        showError: function (message) {
            // Use your preferred notification library
            if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                AppHelper.showToast(message, 'error');
            }
            else if (window.showAlert && typeof showAlert.Error === 'function') {
                showAlert.Error('Login Failed', message);
            }
            else {
                alert(message);
            }
        },
        showSuccess: function (message) {
            if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                AppHelper.showToast(message, 'success');
            }
            else if (window.showAlert && typeof showAlert.Success === 'function') {
                showAlert.Success('Success', message);
            }
        }
    };
})();
// Auto-initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    LoginHandler.init('#loginForm');
});
window.LoginHandler = LoginHandler;
//# sourceMappingURL=Login.js.map