const __shipmentValidationTexts = window.ShipmentValidationTexts || {};
const __msg = (key, fallback) => {
    const value = __shipmentValidationTexts[key];
    return (typeof value === 'string' && value.trim() !== '') ? value : fallback;
};
window.ShipmentValidationRules = {
    // 🔹 Sender
    SenderCountry: { required: true, requiredMessage: __msg('SenderCountry', 'Country is required, Please Select Country.') },
    SenderName: { required: true, requiredMessage: __msg('SenderName', 'Company or Name is required.') },
    SenderContact: { required: true, requiredMessage: __msg('SenderContact', 'Contact is required.') },
    SenderAddress: { required: true, requiredMessage: __msg('SenderAddress', 'Address is required.') },
    SenderPostalCode: { required: true, requiredMessage: __msg('SenderPostalCode', 'Postal Code is required.') },
    SenderCity: { required: true, requiredMessage: __msg('SenderCity', 'City is required, Please Select City.') },
    SenderOtherAddress: { required: true, requiredMessage: __msg('SenderOtherAddress', 'Other Address Information is required.') },
    SenderEmail: { required: true, requiredMessage: __msg('SenderEmail', 'E-mail is required.') },
    SenderPhone: { required: true, requiredMessage: __msg('SenderPhone', 'Phone is required.') },
    // 🔹 Receiver
    ReceiverCountry: { required: true, requiredMessage: __msg('ReceiverCountry', 'Receiver Country is required, Please Select Country.') },
    ReceiverName: { required: true, requiredMessage: __msg('ReceiverName', 'Receiver Name is required.') },
    ReceiverContact: { required: true, requiredMessage: __msg('ReceiverContact', 'Receiver Contact is required.') },
    ReceiverAddress: { required: true, requiredMessage: __msg('ReceiverAddress', 'Receiver Address is required.') },
    ReceiverPostalCode: { required: true, requiredMessage: __msg('ReceiverPostalCode', 'Receiver Postal Code is required.') },
    ReceiverCity: { required: true, requiredMessage: __msg('ReceiverCity', 'Receiver City is required, Please Select City.') },
    ReceiverOtherAddress: { required: true, requiredMessage: __msg('ReceiverOtherAddress', 'Receiver Other Address is required.') },
    ReceiverEmail: { required: true, requiredMessage: __msg('ReceiverEmail', 'Receiver Email is required.') },
    ReceiverPhone: { required: true, requiredMessage: __msg('ReceiverPhone', 'Receiver Phone is required.') },
    // 🔹 Package
    ShippingPackging: { required: true, requiredMessage: __msg('ShippingPackging', 'Shipping Packaging is required, Please Select One.') },
    Weight: { required: true, requiredMessage: __msg('Weight', 'Weight is required.') },
    Length: { required: true, requiredMessage: __msg('Length', 'Length is required.') },
    Width: { required: true, requiredMessage: __msg('Width', 'Width is required.') },
    Height: { required: true, requiredMessage: __msg('Height', 'Height is required.') },
    PackageValue: { required: true, requiredMessage: __msg('PackageValue', 'Declared Value is required.') },
    // 🔹 Shipping Type
    ShippingTypes: { required: true, requiredMessage: __msg('ShippingTypes', 'Shipping Type is required, Please Select One.') },
    DeliveryManId: { required: true, requiredMessage: __msg('DeliveryManId', 'Carrier is required, Please Select One.') },
    DelivryDate: { required: true, requiredMessage: __msg('DelivryDate', 'Delivery Date is required.') },
    // 🔹 Payment
    PaymentMethodId: { required: true, requiredMessage: __msg('PaymentMethodId', 'Payment Method is required, Please Select One.') }
};
//# sourceMappingURL=ShipmentValidationRules.js.map