(function () {
    var cfg = document.getElementById('rate-limit-error-config');
    if (!cfg)
        return;
    var secondsRemaining = parseInt(cfg.dataset.retrySeconds || '0', 10);
    function updateCountdown() {
        var minutes = Math.floor(secondsRemaining / 60);
        var seconds = secondsRemaining % 60;
        var timer = document.getElementById('countdown-timer');
        if (timer) {
            timer.textContent = minutes + ' minutes ' + seconds + ' seconds';
        }
        if (secondsRemaining > 0) {
            secondsRemaining--;
            setTimeout(updateCountdown, 1000);
        }
        else {
            var container = document.getElementById('countdown');
            if (container) {
                container.innerHTML = '<strong class="text-success">✅ You can try again now!</strong>';
            }
        }
    }
    updateCountdown();
})();
//# sourceMappingURL=rate-limit-error.js.map