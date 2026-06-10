/*
 * Shipment status/payment filter dropdown handler.
 * Splits a combined dropdown value into hidden form fields and submits the parent form.
 */
function handleFilterChange(select) {
    var value = select.value;
    var statusInput = document.getElementById('hiddenStatus');
    var isPaidInput = document.getElementById('hiddenIsPaid');

    if (!value) {
        statusInput.value = '';
        isPaidInput.value = '';
    } else if (value.startsWith('status:')) {
        statusInput.value = value.replace('status:', '');
        isPaidInput.value = '';
    } else if (value.startsWith('payment:')) {
        statusInput.value = '';
        isPaidInput.value = value === 'payment:paid' ? 'true' : 'false';
    }

    select.form.submit();
}
