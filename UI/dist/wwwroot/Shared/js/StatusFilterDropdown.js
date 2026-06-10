function handleFilterChange(select) {
    var value = select.value;
    var statusInput = document.getElementById('hiddenStatus');
    var isPaidInput = document.getElementById('hiddenIsPaid');
    if (!value) {
        statusInput.value = '';
        isPaidInput.value = '';
    }
    else if (value.startsWith('status:')) {
        statusInput.value = value.replace('status:', '');
        isPaidInput.value = '';
    }
    else if (value.startsWith('payment:')) {
        statusInput.value = '';
        isPaidInput.value = value === 'payment:paid' ? 'true' : 'false';
    }
    select.form.submit();
}
//# sourceMappingURL=StatusFilterDropdown.js.map