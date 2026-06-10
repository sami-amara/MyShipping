/*
 * Rate limit error countdown.
 * Updates the remaining wait time and changes the notice when the countdown completes.
 */
(function () {
    function initRateLimitCountdown() {
        var countdown = document.getElementById('countdown');
        var timer = document.getElementById('countdown-timer');
        var config = document.getElementById('rate-limit-config');
        if (!countdown || !timer || !config) {
            return;
        }
        var secondsRemaining = parseInt(config.dataset.retrySeconds || '0', 10);
        function updateCountdown() {
            var minutes = Math.floor(secondsRemaining / 60);
            var seconds = secondsRemaining % 60;
            timer.textContent = minutes + ' minutes ' + seconds + ' seconds';
            if (secondsRemaining > 0) {
                secondsRemaining--;
                setTimeout(updateCountdown, 1000);
            }
            else {
                countdown.innerHTML = '<strong class="text-success">You can try again now!</strong>';
            }
        }
        updateCountdown();
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initRateLimitCountdown);
    }
    else {
        initRateLimitCountdown();
    }
})();
//# sourceMappingURL=rate-limit-error.js.map