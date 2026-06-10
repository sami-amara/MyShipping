# Manual Console Test Script

## Step 1: Restart App
1. **Stop debugging**
2. Press **F5** to restart

## Step 2: Navigate to List Page Directly
1. In browser, go to: `http://localhost:PORT/Shipments/List?created=1`
2. Replace `PORT` with your actual port (e.g., 5000, 7000, etc.)

## Step 3: Open Browser Console (F12)

You should see these logs **immediately**:
```
🔵 UserListShipments.js script file loaded (before jQuery ready)
🟢 UserListShipments IIFE executing
🟡 UserListShipments jQuery ready fired
UserListShipments.js loaded
URL params: ?created=1
created param: 1
Creating success alert...
showAlert available? object
Using showAlert.Success
URL cleaned to: /Shipments/List
```

---

## If You See NO Logs

Run these tests **in browser console**:

### Test 1: Check if script file exists
```javascript
// Check if any UserListShipments errors
console.log('Window has ShipmentService?', typeof window.ShipmentService);
```

### Test 2: Check jQuery
```javascript
// Check jQuery
console.log('jQuery version:', $.fn.jquery);
console.log('jQuery ready state:', document.readyState);
```

### Test 3: Manually load the alert code
Paste this **entire block** into console:

```javascript
(function() {
	console.log('🔴 MANUAL TEST: Starting...');

	const params = new URLSearchParams(window.location.search);
	console.log('URL:', window.location.href);
	console.log('Search params:', window.location.search);
	console.log('created param:', params.get('created'));

	if (params.get('created') === '1') {
		console.log('✅ created=1 detected!');

		console.log('showAlert type:', typeof window.showAlert);
		console.log('Swal type:', typeof window.Swal);
		console.log('AppHelper type:', typeof window.AppHelper);

		if (window.Swal) {
			console.log('🎯 Using Swal directly...');
			Swal.fire({
				title: 'Shipment Created',
				text: 'Your shipment has been created successfully!',
				icon: 'success',
				confirmButtonText: 'OK'
			});
		} else if (window.showAlert && typeof showAlert.Success === 'function') {
			console.log('🎯 Using showAlert...');
			showAlert.Success('Shipment Created', 'Your shipment has been created successfully!');
		} else if (window.AppHelper && typeof AppHelper.showToast === 'function') {
			console.log('🎯 Using AppHelper toast...');
			AppHelper.showToast('Shipment created successfully!', 'success');
		} else {
			console.log('❌ No alert method available!');
			alert('Shipment created successfully!');
		}
	} else {
		console.log('❌ created param is NOT "1", it is:', params.get('created'));
	}
})();
```

### Test 4: Check script loading
```javascript
// Check all loaded scripts
Array.from(document.scripts).forEach(s => {
	if (s.src.includes('UserListShipments') || s.src.includes('showAlert')) {
		console.log('Script:', s.src, 'loaded?', s.src ? 'yes' : 'no');
	}
});
```

---

## If Manual Test Works

If the manual test **SHOWS THE ALERT**, it means:
- ✅ Swal/showAlert is working
- ✅ URL parameter works
- ❌ UserListShipments.js is NOT executing

**Solution**: Script loading issue

---

## If Manual Test Doesn't Work

### Scenario A: "created param is NOT "1""
**Problem**: URL parameter not present or wrong value

**Check**:
```javascript
window.location.href  // What's the full URL?
```

**Fix**: Make sure you're testing with `?created=1` in URL

### Scenario B: "No alert method available!"
**Problem**: SweetAlert not loaded

**Check**:
```javascript
// Check if SweetAlert script is in page
document.querySelector('script[src*="sweetalert"]')
```

**Fix**: Add SweetAlert to List.cshtml scripts section

### Scenario C: Nothing happens
**Problem**: JavaScript error preventing execution

**Check**: Look for RED errors in console

---

## Quick Fixes to Try

### Fix 1: Add Direct Script in List.cshtml

**File**: `UI/Views/Shipments/List.cshtml`

Add this **at the bottom** of the `@section Scripts` block:

```html
@section Scripts {
	<script src="~/Modules/AppHelper.js"></script>
	<script src="~/Modules/ApiClient.js"></script>
	<script src="~/Modules/ShipmentApiClient.js"></script>
	<script src="~/Modules/ShipmentService.js"></script>
	<script src="~/Modules/UserListShipments.js"></script>

	<!-- ✅ INLINE TEST: Show alert if created=1 -->
	<script>
		console.log('🔴 INLINE SCRIPT: Running...');

		$(document).ready(function() {
			console.log('🔴 INLINE SCRIPT: jQuery ready');

			const params = new URLSearchParams(window.location.search);
			if (params.get('created') === '1') {
				console.log('🔴 INLINE SCRIPT: created=1 detected!');

				setTimeout(function() {
					if (window.Swal) {
						Swal.fire({
							title: 'Shipment Created',
							text: 'Your shipment has been created successfully! Payment has been processed.',
							icon: 'success'
						});
					} else if (window.showAlert) {
						showAlert.Success('Shipment Created', 'Your shipment has been created successfully! Payment has been processed.');
					} else {
						alert('Shipment created successfully!');
					}

					// Clean URL
					if (window.history && window.history.replaceState) {
						window.history.replaceState({}, document.title, window.location.pathname);
					}
				}, 500);
			}
		});
	</script>
}
```

This inline script will **definitely execute** if the page loads, so we can confirm if it's a script loading issue or something else.

---

## Testing Checklist

After adding inline script:

1. ☐ Restart app
2. ☐ Navigate to `/Shipments/List?created=1`
3. ☐ Open console (F12)
4. ☐ Look for `🔴 INLINE SCRIPT: Running...`
5. ☐ Did alert show?

**If inline script works**: Problem is with UserListShipments.js file loading  
**If inline script doesn't work**: Problem is with SweetAlert or URL parameter

---

Please run the **Manual Console Test** (Test 3 above) and share what you see!
