/* eslint-disable no-undef */
// Wrap in IIFE to avoid accidental globals and make the script reversible via _backups
(function () {
    'use strict';

    // ✅ NEW: Async initialization to ensure token is loaded before populating dropdowns
    async function initializePageControls() {
        try {
            // ✅ Preload the access token to ensure it's cached before any API calls
            if (window.ApiClient && typeof window.ApiClient.getAccessToken === 'function') {
                console.log('🔐 Preloading access token...');
                await window.ApiClient.getAccessToken();
                console.log('✅ Access token ready');
            }
        } catch (e) {
            console.warn('⚠️ Could not preload access token:', e);
            // Continue anyway - some pages might not need auth
        }

        // 📦 Load defaults from hidden JSON if present
        let defaults = {
            // Shared (Create page / Settings preselection): same country+city for both sender & receiver
            countryId: null,
            cityId: null,
            // Per-party (Edit / Approve pages): sender and receiver each have their own country+city
            senderCountryId: null,
            senderCityId: null,
            receiverCountryId: null,
            receiverCityId: null,
            packageId: null,
            typeId: null,
            carrierId: null
        };

        try {
            const defaultsJson = document.querySelector('#shipment-defaults-json')?.textContent?.trim();
            if (defaultsJson) {
                const parsed = JSON.parse(defaultsJson);
                const val = (v) => v && v !== '' ? v : null;

                // Shared keys (Create / Settings)
                defaults.countryId = val(parsed.defaultCountryId);
                defaults.cityId = val(parsed.defaultCityId);

                // Per-party keys (Edit / Approve) — take precedence over shared keys when present
                defaults.senderCountryId = val(parsed.defaultSenderCountryId);
                defaults.senderCityId = val(parsed.defaultSenderCityId);
                defaults.receiverCountryId = val(parsed.defaultReceiverCountryId);
                defaults.receiverCityId = val(parsed.defaultReceiverCityId);

                defaults.packageId = val(parsed.defaultShippingPackageId);
                defaults.typeId = val(parsed.defaultShippingTypeId);
                defaults.carrierId = val(parsed.defaultCarrierId);

                console.log('📦 Loaded shipment defaults:', defaults);
            }
        } catch (e) {
            console.warn('⚠️ Could not parse defaults:', e);
        }

        // Resolve the effective country+city for each party:
        // Per-party keys win when present (Edit/Approve); fall back to shared key (Create/Settings).
        const senderCountryId = defaults.senderCountryId ?? defaults.countryId;
        const senderCityId = defaults.senderCityId ?? defaults.cityId;
        const receiverCountryId = defaults.receiverCountryId ?? defaults.countryId;
        const receiverCityId = defaults.receiverCityId ?? defaults.cityId;

        // Now initialize all dropdowns (token is cached and ready)
        // Countries first, then dependent cities to avoid race conditions
        await ManagePageControlls.fillCountryDropdownAsync($('select[name="SenderCountry"]'), senderCountryId);
        await ManagePageControlls.fillCountryDropdownAsync($('select[name="ReceiverCountry"]'), receiverCountryId);

        // Load cities only when a country is known; each party uses its own country+city pair
        if (senderCountryId) {
            await ManagePageControlls.fillCityDropdownAsync($('select[name="SenderCity"]'), senderCountryId, senderCityId);
        }
        if (receiverCountryId) {
            await ManagePageControlls.fillCityDropdownAsync($('select[name="ReceiverCity"]'), receiverCountryId, receiverCityId);
        }

        // For Shipping Types - pass default type ID
        await ManagePageControlls.fillShippingTypesDropdownAsync('select[name="ShippingTypes"]', defaults.typeId);

        // For Shipping Packaging - pass default package ID
        await ManagePageControlls.fillShippingPackgingDropdownAsync('select[name="ShippingPackging"]', defaults.packageId);

        // For Carrier dropdown (MakeShipmentReadyForShipp admin view) - only populate when element exists
        if (document.querySelector('#deliveryManId')) {
            await ManagePageControlls.fillCarrierDropdownAsync('#deliveryManId', defaults.carrierId);
        }

        // ✅ NEW: Preload and restore Settings page country+city if this is the Settings page
        const $settingsCountry = $('#settingsCountry');
        if ($settingsCountry.length) {
            console.log('📍 Settings page detected - preloading country and city dropdowns');
            // Get the current selected country from the dropdown (already populated by server)
            const currentCountryId = $settingsCountry.val();
            const $settingsCity = $('#settingsCity');
            const currentCityId = $settingsCity.val();

            // If a country is already selected, reload cities to match
            if (currentCountryId) {
                console.log('🔄 Reloading cities for preselected country:', currentCountryId);
                await ManagePageControlls.fillCityDropdownAsync($settingsCity, currentCountryId, currentCityId);
            }
        }

        // Event listeners for Shipment pages (SenderCountry / ReceiverCountry)
        $(document).on('change', 'select[name="SenderCountry"]', function () {
            const countryId = $(this).val();
            ManagePageControlls.fillCityDropdown($('select[name="SenderCity"]'), countryId);
        });

        $(document).on('change', 'select[name="ReceiverCountry"]', function () {
            const countryId = $(this).val();
            ManagePageControlls.fillCityDropdown($('select[name="ReceiverCity"]'), countryId);
        });

        // ✅ NEW: Event listener for Settings page country dropdown
        $(document).on('change', '#settingsCountry', function () {
            const countryId = $(this).val();
            console.log('📍 Settings country changed to:', countryId);
            if (countryId) {
                ManagePageControlls.fillCityDropdown($('#settingsCity'), countryId);
            } else {
                // Clear city dropdown if no country selected
                $('#settingsCity').empty().append('<option value="">Select city</option>');
            }
        });
    }

    // Call async initialization when DOM is ready
    $(document).ready(function () {
        initializePageControls().catch(err => {
            console.error('❌ Error initializing page controls:', err);
        });
    });
})();













// /* eslint-disable no-undef */
// // Wrap in IIFE to avoid accidental globals and make the script reversible via _backups
// (function () {
//     'use strict';

//     // ✅ NEW: Async initialization to ensure token is loaded before populating dropdowns
//     async function initializePageControls() {
//         try {
//             // ✅ Preload the access token to ensure it's cached before any API calls
//             if (window.ApiClient && typeof window.ApiClient.getAccessToken === 'function') {
//                 console.log('🔐 Preloading access token...');
//                 await window.ApiClient.getAccessToken();
//                 console.log('✅ Access token ready');
//             }
//         } catch (e) {
//             console.warn('⚠️ Could not preload access token:', e);
//             // Continue anyway - some pages might not need auth
//         }

//         // 📦 Load defaults from hidden JSON if present
//         let defaults = {
//             // Shared (Create page / Settings preselection): same country+city for both sender & receiver
//             countryId: null,
//             cityId: null,
//             // Per-party (Edit / Approve pages): sender and receiver each have their own country+city
//             senderCountryId: null,
//             senderCityId: null,
//             receiverCountryId: null,
//             receiverCityId: null,
//             packageId: null,
//             typeId: null,
//             carrierId: null
//         };

//         try {
//             const defaultsJson = document.querySelector('#shipment-defaults-json')?.textContent?.trim();
//             if (defaultsJson) {
//                 const parsed = JSON.parse(defaultsJson);
//                 const val = (v) => v && v !== '' ? v : null;

//                 // Shared keys (Create / Settings)
//                 defaults.countryId  = val(parsed.defaultCountryId);
//                 defaults.cityId     = val(parsed.defaultCityId);

//                 // Per-party keys (Edit / Approve) — take precedence over shared keys when present
//                 defaults.senderCountryId   = val(parsed.defaultSenderCountryId);
//                 defaults.senderCityId      = val(parsed.defaultSenderCityId);
//                 defaults.receiverCountryId = val(parsed.defaultReceiverCountryId);
//                 defaults.receiverCityId    = val(parsed.defaultReceiverCityId);

//                 defaults.packageId = val(parsed.defaultShippingPackageId);
//                 defaults.typeId    = val(parsed.defaultShippingTypeId);
//                 defaults.carrierId = val(parsed.defaultCarrierId);

//                 console.log('📦 Loaded shipment defaults:', defaults);
//             }
//         } catch (e) {
//             console.warn('⚠️ Could not parse defaults:', e);
//         }

//         // Resolve the effective country+city for each party:
//         // Per-party keys win when present (Edit/Approve); fall back to shared key (Create/Settings).
//         const senderCountryId   = defaults.senderCountryId   ?? defaults.countryId;
//         const senderCityId      = defaults.senderCityId      ?? defaults.cityId;
//         const receiverCountryId = defaults.receiverCountryId ?? defaults.countryId;
//         const receiverCityId    = defaults.receiverCityId    ?? defaults.cityId;

//         // Now initialize all dropdowns (token is cached and ready)
//         // Countries first, then dependent cities to avoid race conditions
//         await ManagePageControlls.fillCountryDropdownAsync($('select[name="SenderCountry"]'), senderCountryId);
//         await ManagePageControlls.fillCountryDropdownAsync($('select[name="ReceiverCountry"]'), receiverCountryId);

//         // Load cities only when a country is known; each party uses its own country+city pair
//         if (senderCountryId) {
//             await ManagePageControlls.fillCityDropdownAsync($('select[name="SenderCity"]'), senderCountryId, senderCityId);
//         }
//         if (receiverCountryId) {
//             await ManagePageControlls.fillCityDropdownAsync($('select[name="ReceiverCity"]'), receiverCountryId, receiverCityId);
//         }

//         // For Shipping Types - pass default type ID
//         await ManagePageControlls.fillShippingTypesDropdownAsync('select[name="ShippingTypes"]', defaults.typeId);

//         // For Shipping Packaging - pass default package ID
//         await ManagePageControlls.fillShippingPackgingDropdownAsync('select[name="ShippingPackging"]', defaults.packageId);

//         // For Carrier dropdown (MakeShipmentReadyForShipp admin view) - only populate when element exists
//         if (document.querySelector('#deliveryManId')) {
//             await ManagePageControlls.fillCarrierDropdownAsync('#deliveryManId', defaults.carrierId);
//         }




//         $(document).on('change', 'select[name="SenderCountry"]', function () {
//             const countryId = $(this).val();
//             ManagePageControlls.fillCityDropdown($('select[name="SenderCity"]'), countryId);
//         });

//         $(document).on('change', 'select[name="ReceiverCountry"]', function () {
//             const countryId = $(this).val();
//             ManagePageControlls.fillCityDropdown($('select[name="ReceiverCity"]'), countryId);
//         });
//     }

//     // Call async initialization when DOM is ready
//     $(document).ready(function () {
//         initializePageControls().catch(err => {
//             console.error('❌ Error initializing page controls:', err);
//         });
//     });
// })();
