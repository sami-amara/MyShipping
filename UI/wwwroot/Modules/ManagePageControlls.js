/* eslint-disable no-undef */
// Normalized, tolerant dropdown population for Countries, Cities, ShippingTypes, Packaging, Carriers
window.ManagePageControlls = (function () {
    function toJQuery(selectorOrJq) {
        return selectorOrJq && selectorOrJq.jquery ? selectorOrJq : $(selectorOrJq);
    }

    function extractList(response) {
        if (!response && response !== 0) return null;
        if (Array.isArray(response)) return response;
        if (Array.isArray(response.data)) return response.data;
        if (Array.isArray(response.Data)) return response.Data;
        if (Array.isArray(response.items)) return response.items;
        // fallback: find first array property
        if (typeof response === 'object' && response !== null) {
            for (const k in response) {
                if (Object.prototype.hasOwnProperty.call(response, k) && Array.isArray(response[k])) return response[k];
            }
        }
        return null;
    }

    function isArabicUi() {
        try {
            const htmlLang = (document.documentElement.getAttribute('lang') || '').toLowerCase();
            return htmlLang.startsWith('ar');
        } catch {
            return false;
        }
    }

    function pickLocalizedName(item, arabicKeys, englishKeys, fallbackKeys) {
        const prefersArabic = isArabicUi();
        const primary = prefersArabic ? arabicKeys : englishKeys;
        const secondary = prefersArabic ? englishKeys : arabicKeys;
        const orderedKeys = [...primary, ...secondary, ...fallbackKeys];

        for (const key of orderedKeys) {
            const value = item && item[key];
            if (value !== null && value !== undefined && String(value).trim() !== '') {
                return value;
            }
        }

        return null;
    }

    function safePopulate($select, list, textResolver, valueResolver, placeholder = 'Select') {
        $select.empty();
        $select.append(`<option value="">${placeholder}</option>`);
        list.forEach(item => {
            const val = valueResolver(item);
            const txt = textResolver(item);
            if (val === null || txt === null) {
                //console.debug('Skipping invalid item', item);
                return;
            }
            $select.append(`<option value="${val}">${txt}</option>`);
        });
    }

    function getCountriesAsync() {
        return new Promise((resolve, reject) => {
            CountryService.GetAll(resolve, reject);
        });
    }

    function getCitiesByCountryIdAsync(countryId) {
        return new Promise((resolve, reject) => {
            CitiesService.GetByCountryId(countryId, resolve, reject);
        });
    }

    function getShippingTypesAsync() {
        return new Promise((resolve, reject) => {
            ShippingTypeService.GetAll(resolve, reject);
        });
    }

    function getShippingPackgingAsync() {
        return new Promise((resolve, reject) => {
            ShippingPackageService.GetAll(resolve, reject);
        });
    }

    function getCarriersAsync() {
        return new Promise((resolve, reject) => {
            CarriersService.GetAll(resolve, reject);
        });
    }

    return {
        // Countries already working
        async fillCountryDropdownAsync(selectSelector, selectedCountryId = null) {
            const $sel = toJQuery(selectSelector);
            try {
                const response = await getCountriesAsync();
                const list = extractList(response);
                if (!list) {
                    //console.error('Countries: unexpected response', response);
                    AppHelper.showToast('Error: Could not load Countries data.', 'error');
                    return;
                }
                safePopulate($sel, list,
                    c => pickLocalizedName(
                        c,
                        ['countryAname', 'CountryAname'],
                        ['countryEname', 'CountryEname'],
                        ['name', 'Name']
                    ),
                    c => c.id ?? c.Id ?? c.countryId ?? c.CountryId,
                    (window.AppResourceLabels && window.AppResourceLabels.selectCountry) || 'Select Country');

                // Preselect country if provided
                if (selectedCountryId) {
                    $sel.val(selectedCountryId);
                }
            } catch (err) {
                //console.error('Error fetching countries', err);
                AppHelper.showToast('Failed to load countries.', 'error');
            }
        },

        fillCountryDropdown(selectSelector, selectedCountryId = null) {
            this.fillCountryDropdownAsync(selectSelector, selectedCountryId);
        },

        async fillCityDropdownAsync(selectSelector, countryId, selectedCityId = null) {
            const $sel = toJQuery(selectSelector);
            $sel.empty().append('<option value="">Select City</option>');
            if (!countryId) return;

            try {
                const response = await getCitiesByCountryIdAsync(countryId);
                const list = extractList(response);
                if (!list) {
                    //console.error('Cities: unexpected response', response);
                    AppHelper.showToast('Error: Could not load Cities data.', 'error');
                    return;
                }
                safePopulate($sel, list,
                    c => pickLocalizedName(
                        c,
                        ['cityAname', 'CityAname'],
                        ['cityEname', 'CityEname'],
                        ['cityName', 'CityName', 'name', 'Name']
                    ),
                    c => c.id ?? c.Id ?? c.cityId ?? c.CityId,
                    (window.AppResourceLabels && window.AppResourceLabels.city) || 'Select City');

                if (selectedCityId) {
                    $sel.val(selectedCityId);
                    if ($sel.val() !== String(selectedCityId) && $sel.val() !== selectedCityId) {
                        console.warn('Could not preselect city', selectedCityId);
                    }
                }
            } catch (err) {
                //console.error('Error fetching cities', err);
                AppHelper.showToast('Failed to load cities.', 'error');
            }
        },

        fillCityDropdown(selectSelector, countryId, selectedCityId = null) {
            this.fillCityDropdownAsync(selectSelector, countryId, selectedCityId);
        },

        async fillShippingTypesDropdownAsync(selectSelector, selectedTypeId = null) {
            const $sel = toJQuery(selectSelector);
            try {
                const response = await getShippingTypesAsync();
                const list = extractList(response);
                if (!list) {
                   // console.error('ShippingTypes: unexpected response', response);
                    AppHelper.showToast('Error: Could not load Shipping Types.', 'error');
                    return;
                }
                safePopulate($sel, list,
                    t => pickLocalizedName(
                        t,
                        ['shippingTypeAname', 'ShippingTypeAname'],
                        ['shippingTypeEname', 'ShippingTypeEname'],
                        ['shippingTypeName', 'name', 'Name']
                    ),
                    t => t.id ?? t.Id,
                    (window.AppResourceLabels && window.AppResourceLabels.shippingType) || 'Select Shipping Type');

                // ✅ Pre-select the default type if provided
                if (selectedTypeId) {
                    $sel.val(selectedTypeId);
                }
            } catch (err) {
                //console.error('Error fetching shipping types', err);
                AppHelper.showToast('Failed to load Shipping Types.', 'error');
            }
        },

        fillShippingTypesDropdown(selectSelector, selectedTypeId = null) {
            this.fillShippingTypesDropdownAsync(selectSelector, selectedTypeId);
        },

        async fillShippingPackgingDropdownAsync(selectSelector, selectedPackageId = null) {
            const $sel = toJQuery(selectSelector);
            try {
                const response = await getShippingPackgingAsync();
                const list = extractList(response);
                if (!list) {
                    //console.error('Packaging: unexpected response', response);
                    AppHelper.showToast('Error: Could not load Packaging data.', 'error');
                    return;
                }
                safePopulate($sel, list,
                    p => pickLocalizedName(
                        p,
                        ['tbShipingPackginAname', 'TbShipingPackginAname'],
                        ['tbShipingPackginEname', 'TbShipingPackginEname'],
                        ['name', 'Name']
                    ),
                    p => p.id ?? p.Id,
                    (window.AppResourceLabels && window.AppResourceLabels.packagingPackage) || 'Select Shipping Packaging');

                // ✅ Pre-select the default package if provided
                if (selectedPackageId) {
                    $sel.val(selectedPackageId);
                }
            } catch (err) {
                //console.error('Error fetching packaging', err);
                AppHelper.showToast('Failed to load Packaging.', 'error');
            }
        },

        fillShippingPackgingDropdown(selectSelector, selectedPackageId = null) {
            this.fillShippingPackgingDropdownAsync(selectSelector, selectedPackageId);
        },

        async fillCarrierDropdownAsync(selectSelector, selectedCarrierId = null) {
            const $sel = toJQuery(selectSelector);
            try {
                const response = await getCarriersAsync();
                const list = extractList(response);
                if (!list) {
                    //console.error('Carriers: unexpected response', response);
                    AppHelper.showToast('Error: Could not load Carriers data.', 'error');
                    return;
                }
                safePopulate($sel, list,
                    c => pickLocalizedName(
                        c,
                        ['carrierAname', 'CarrierAname'],
                        ['carrierEname', 'CarrierEname'],
                        ['carrierName', 'CarrierName', 'name', 'Name']
                    ),
                    c => c.id ?? c.Id,
                    (window.AppResourceLabels && window.AppResourceLabels.selectCarrier) || 'Select Carrier');

                if (selectedCarrierId) {
                    $sel.val(selectedCarrierId);
                }
            } catch (err) {
                //console.error('Error fetching carriers', err);
                AppHelper.showToast('Failed to load Carriers.', 'error');
            }
        },

        fillCarrierDropdown(selectSelector, selectedCarrierId = null) {
            this.fillCarrierDropdownAsync(selectSelector, selectedCarrierId);
        },

        fillPaymentMethodDropdown(selectSelector) {
            const $sel = toJQuery(selectSelector);

            const onSuccess = function (response) {
                const list = extractList(response);
                if (!list || !Array.isArray(list) || list.length === 0) {
                    if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                        AppHelper.showToast('Error: Could not load Payment Methods.', 'error');
                    }
                    return;
                }

                safePopulate($sel, list,
                    p => pickLocalizedName(
                        p,
                        ['methdAname', 'MethdAname'],
                        ['methodEname', 'MethodEname'],
                        ['name', 'Name']
                    ),
                    p => p.id ?? p.Id,
                    (window.AppResourceLabels && window.AppResourceLabels.paymentMethod) || 'Select Payment Method');

                $sel.find('option').each(function () {
                    const optionValue = $(this).val();
                    if (!optionValue) return;

                    const method = list.find(m => String(m.id ?? m.Id) === String(optionValue));
                    if (!method) return;

                    const token = method.paymentMethodToken ?? method.PaymentMethodToken ?? '';
                    if (token) {
                        $(this).attr('data-payment-token', token);
                    }
                });
            };

            const onError = function (err) {
                try { console.error('Payment methods request failed', err); } catch { }
                if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                    AppHelper.showToast('Failed to load Payment Methods.', 'error');
                }
            };

            if (window.PaymentMethodService && typeof PaymentMethodService.GetAll === 'function') {
                PaymentMethodService.GetAll(onSuccess, onError);
                return;
            }

            if (window.ApiClient && typeof ApiClient.get === 'function') {
                ApiClient.get('api/PaymentMethods', onSuccess, onError, true);
                return;
            }

            onError({ message: 'PaymentMethodService and ApiClient are unavailable' });
        }
    };
})();










