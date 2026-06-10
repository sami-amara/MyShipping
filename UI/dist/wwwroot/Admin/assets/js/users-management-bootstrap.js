/*
 * Users management bootstrap.
 * Exposes lock/unlock endpoints from #user-management-config to users-management.js.
 */
(function () {
    function setUserManagementUrls() {
        var configElement = document.getElementById('user-management-config');
        if (!configElement) {
            return;
        }
        window.userManagementUrls = {
            lockUrl: configElement.dataset.lockUrl,
            unlockUrl: configElement.dataset.unlockUrl
        };
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setUserManagementUrls);
    }
    else {
        setUserManagementUrls();
    }
})();
//# sourceMappingURL=users-management-bootstrap.js.map