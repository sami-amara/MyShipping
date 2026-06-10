///* eslint-disable no-undef */
//const AuthApiClient = (function () {
//    'use strict';
//    return {
//        login: async function (email, password) {
//            const response = await fetch('/api/auth/login', {
//                method: 'POST',
//                headers: { 'Content-Type': 'application/json' },
//                credentials: 'include',
//                body: JSON.stringify({ email, password })
//            });
//            const apiResponse = await response.json();
//            // Return normalized format
//            return {
//                success: apiResponse.isSuccess || false,
//                message: apiResponse.message || null,
//                data: apiResponse.data || null,
//                errors: apiResponse.errors || [],
//                statusCode: response.status
//            };
//        },
//        logout: async function () {
//            const response = await fetch('/api/auth/logout', {
//                method: 'POST',
//                credentials: 'include'
//            });
//            const apiResponse = await response.json();
//            // Clear local storage
//            localStorage.removeItem('accessToken');
//            return {
//                success: apiResponse.isSuccess || false,
//                message: apiResponse.message || 'Logged out'
//            };
//        },
//        refreshAccessToken: async function () {
//            try {
//                const response = await fetch('/api/auth/refresh-access-token', {
//                    method: 'POST',
//                    credentials: 'include' // Sends refresh token cookie
//                });
//                if (!response.ok) {
//                    throw new Error('Token refresh failed');
//                }
//                const apiResponse = await response.json();
//                if (apiResponse.isSuccess && apiResponse.data?.accessToken) {
//                    // Update stored access token
//                    localStorage.setItem('accessToken', apiResponse.data.accessToken);
//                    return { success: true, accessToken: apiResponse.data.accessToken };
//                }
//                throw new Error(apiResponse.message || 'Token refresh failed');
//            } catch (error) {
//                console.error('Token refresh error:', error);
//                // Clear token and redirect to login
//                localStorage.removeItem('accessToken');
//                window.location.href = '/Account/Login';
//                return { success: false, error: error.message };
//            }
//        }
//    };
//})();
//window.AuthApiClient = AuthApiClient;
//# sourceMappingURL=AuthApiClient.js.map