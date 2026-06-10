const __adminValidationTexts = window.AdminValidationTexts || {};
const __adminMsg = (key, fallback) => {
    const value = __adminValidationTexts[key];
    return (typeof value === 'string' && value.trim() !== '') ? value : fallback;
};

window.AdminValidationRules = {
    // 🔹 Country Form
    countryForm: {
        CountryAname: {
            required: true,
            requiredMessage: __adminMsg('CountryAname', 'Arabic name required.')
        },
        CountryEname: {
            required: true,
            requiredMessage: __adminMsg('CountryEname', 'English name required.')
        }
    },

    // 🔹 Shipping Packaging Form
    shippingPackagingForm: {
        PackageName: {
            required: true,
            requiredMessage: __adminMsg('PackageName', 'Package name is required.')
        },
        ShippimentCount: {
            required: true,
            requiredMessage: __adminMsg('ShippimentCount', 'Shipment count is required.')
        },
        NumberOfKiloMeters: {
            required: true,
            requiredMessage: __adminMsg('NumberOfKiloMeters', 'Number of kilometers is required.')
        },
        TotalWeight: {
            required: true,
            requiredMessage: __adminMsg('TotalWeight', 'Total weight is required.')
        }
    },

    // City Form
    cityForm: {
        CityAname: {
            required: true, requiredMessage: __adminMsg('CityAname', 'Arabic name required.')
        },
        CityEname: {
            required: true, requiredMessage: __adminMsg('CityEname', 'English name required.')
        },
        CountryId: {
            required: true, requiredMessage: __adminMsg('CountryId', 'Country required.')
        }
    },

    // 🔹 Shipping Type Form
    shippingTypeForm: {
        ShippingTypeAname: {
            required: true, requiredMessage: __adminMsg('ShippingTypeAname', 'Arabic name required.')
        },
        ShippingTypeEname: {
            required: true, requiredMessage: __adminMsg('ShippingTypeEname', 'English name required.')
        },
        ShippingFactor: {
            required: true,
            min: 0.25,
            max: 10,
            requiredMessage: __adminMsg('ShippingFactor', 'Factor is required.'),
            rangeMessage: __adminMsg('ShippingFactorRange', 'Factor must be between 0.25 and 10')
        }
    },

    // 🔹 Add more forms here as needed
};



