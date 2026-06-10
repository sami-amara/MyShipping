# Success Alert Not Showing - Debugging Guide

## Problem
The success alert is not appearing on the List page after shipment creation, even though:
- ✅ Toast was removed from Create page
- ✅ Alert was removed from Create page
- ✅ Redirect to `/Shipments/List?created=1` is working
- ✅ UserListShipments.js is loaded in List.cshtml

## Debugging Steps

### Step 1: Verify Redirect URL
1. **Stop debugging** and **restart** the app (F5)
2. Navigate to `/Shipments/Create`
3. Fill out all required fields and select payment
4. Click **Submit**
5. After payment animation, **check the browser address bar**
6. **Expected**: `/Shipments/List?created=1`
7. **If different**: The redirect isn't working

### Step 2: Open Browser Console (F12)
After redirect to List page, check console for logs:

**Expected Console Output**:
```
UserListShipments.js loaded
URL params: ?created=1
created param: 1
Creating success alert...
showAlert available? object
Using showAlert.Success
URL cleaned to: /Shipments/List
```

**If you see different output**, it will tell us what's wrong:

#### Scenario A: No console logs at all
**Cause**: UserListShipments.js not loaded or not executing  
**Fix**: Check if script is referenced in List.cshtml

#### Scenario B: "created param: null"
**Cause**: URL parameter is not `?created=1`  
**Fix**: Check ShipmentService.js redirect code

#### Scenario C: "showAlert available? undefined"
**Cause**: showAlert.js not loaded or timing issue  
**Fix**: Check if showAlert.js is in _Layout.cshtml and loaded before UserListShipments.js

#### Scenario D: "No alert method available!"
**Cause**: Neither showAlert nor AppHelper is available  
**Fix**: Check script loading order

### Step 3: Check Script Loading Order
In List.cshtml, scripts should load in this order:

```html
@section Scripts {
	<script src="~/Modules/AppHelper.js"></script>        <!-- 1st -->
	<script src="~/Modules/ApiClient.js"></script>
	<script src="~/Modules/ShipmentApiClient.js"></script>
	<script src="~/Modules/ShipmentService.js"></script>
	<script src="~/Modules/UserListShipments.js"></script>  <!-- Last -->
}
```

**Also check _Layout.cshtml** for:
```html
<script src="~/App/Shared/showAlert.js"></script>
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
```

### Step 4: Manual Test in Console
After page loads, run in browser console:

```javascript
// Test if showAlert exists
window.showAlert

// Test showAlert.Success
showAlert.Success('Test Title', 'Test Message')

// Test AppHelper.showToast
AppHelper.showToast('Test Toast', 'success')
```

**Expected**: Alert popup should appear

### Step 5: Check URL Parameter
In browser console on List page:

```javascript
// Check current URL
window.location.href

// Check search params
window.location.search

// Check created parameter
new URLSearchParams(window.location.search).get('created')
```

**Expected**: Should return `"1"` right after redirect

---

## Common Issues & Fixes

### Issue 1: Alert Shows Then Immediately Disappears
**Cause**: URL is cleaned too fast before SweetAlert renders  
**Fix**: Add delay before cleaning URL

```javascript
if (params.get('created') === '1') {
	const title = alerts.createdTitle || 'Shipment Created';
	const message = alerts.createdSuccess || 'Shipment created successfully! Payment has been processed.';

	if (window.showAlert && typeof showAlert.Success === 'function') {
		showAlert.Success(title, message);
	}

	// Clean URL after delay to let alert render
	setTimeout(() => {
		if (window.history && window.history.replaceState) {
			const cleanUrl = window.location.pathname;
			window.history.replaceState({}, document.title, cleanUrl);
		}
	}, 500);  // Wait 500ms before cleaning URL
}
```

### Issue 2: showAlert.Success Takes 3 Parameters
**Cause**: showAlert.Success might expect a callback as 3rd parameter  
**Check**: Look at showAlert.js definition

If it's like this:
```javascript
Success: function (title, text, callback) {
	Swal.fire({
		title: title,
		text: text,
		icon: 'success'
	}).then(() => {
		if (typeof callback === 'function') callback();
	});
}
```

**Fix**: Modify UserListShipments.js to not clean URL, or pass callback:
```javascript
showAlert.Success(title, message, () => {
	// Clean URL in callback
	if (window.history && window.history.replaceState) {
		window.history.replaceState({}, document.title, window.location.pathname);
	}
});
```

### Issue 3: Scripts Load in Wrong Order
**Cause**: UserListShipments.js executes before showAlert.js loads  
**Fix**: Add defer or ensure showAlert.js loads first

### Issue 4: jQuery Not Ready
**Cause**: $(document).ready() might fire before showAlert is available  
**Fix**: Add check and retry:

```javascript
function showCreatedAlert() {
	const params = new URLSearchParams(window.location.search);
	if (params.get('created') !== '1') return;

	if (!window.showAlert || typeof showAlert.Success !== 'function') {
		// Retry after a delay
		setTimeout(showCreatedAlert, 100);
		return;
	}

	const alerts = window.AppResourceAlerts || {};
	const title = alerts.createdTitle || 'Shipment Created';
	const message = alerts.createdSuccess || 'Shipment created successfully! Payment has been processed.';
	showAlert.Success(title, message);
}

$(document).ready(showCreatedAlert);
```

---

## Quick Fix to Try First

If console shows everything is available but alert still doesn't show, try this version with explicit delay:

**File**: `UI/wwwroot/Modules/UserListShipments.js`

```javascript
if (params.get('created') === '1') {
	console.log('Showing created alert...');

	// Use setTimeout to ensure DOM and SweetAlert are ready
	setTimeout(() => {
		const title = alerts.createdTitle || 'Shipment Created';
		const message = alerts.createdSuccess || 'Shipment created successfully! Payment has been processed.';

		if (window.Swal) {
			// Use Swal directly
			Swal.fire({
				title: title,
				text: message,
				icon: 'success',
				confirmButtonText: 'OK'
			});
		} else if (window.showAlert && typeof showAlert.Success === 'function') {
			showAlert.Success(title, message);
		} else if (window.AppHelper && typeof AppHelper.showToast === 'function') {
			AppHelper.showToast(message, 'success');
		}

		// Clean URL after alert is shown
		setTimeout(() => {
			if (window.history && window.history.replaceState) {
				window.history.replaceState({}, document.title, window.location.pathname);
			}
		}, 1000);
	}, 300);  // Wait 300ms for scripts to fully load
}
```

---

## Testing Procedure

1. **Restart app** (stop debugging, press F5)
2. **Open browser console** (F12)
3. **Navigate to** `/Shipments/Create`
4. **Fill and submit** shipment with payment
5. **Watch console** for logs during redirect
6. **Check if alert appears**
7. **Report findings**:
   - What URL did you land on?
   - What console logs appeared?
   - Did any errors appear?
   - Did the alert show?

---

## Next Steps Based on Console Output

Please **test now** and **share**:
1. The URL you landed on after submit
2. The console log output (all lines)
3. Any errors in console
4. Whether the alert appeared or not

Based on your findings, I'll provide the exact fix needed!

---

**Files Modified** (with debug logging):
- ✅ `UI/wwwroot/Modules/UserListShipments.js` - Added console.log statements

**Ready to debug!** Please test and share console output.
