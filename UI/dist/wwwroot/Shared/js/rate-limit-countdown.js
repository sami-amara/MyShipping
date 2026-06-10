/**
 * Rate Limit Countdown Timer with Progress Bar (DEBUG VERSION)
 */
const RateLimitCountdown = {
    start: function (totalSeconds, countdownElementId, timeRemainingElementId, alertElementId, progressBarId) {
        // Get DOM elements
        const countdownElement = document.getElementById(countdownElementId);
        const timeRemainingElement = document.getElementById(timeRemainingElementId);
        const alertElement = document.getElementById(alertElementId);
        const progressBar = progressBarId ? document.getElementById(progressBarId) : null;
        const originalTotal = totalSeconds;
        let secondsRemaining = totalSeconds;
        let updateCount = 0;
        function updateCountdown() {
            updateCount++;
            //// Log every 5 updates (every 5 seconds)
            //if (updateCount % 5 === 0) {
            //    console.log(`⏱️ Update #${updateCount}: ${secondsRemaining}s remaining`);
            //}
            // Timer expired
            if (secondsRemaining <= 0) {
                /*  console.log('✅ Timer completed!');*/
                alertElement.className = 'alert alert-success mt-3';
                alertElement.innerHTML = `
                    <i class="fa fa-check-circle"></i>
                    <strong>Time's Up!</strong><br />
                    You can now try again. Please refresh the page.
                `;
                return;
            }
            // Calculate progress
            const elapsedSeconds = originalTotal - secondsRemaining;
            const percentComplete = (elapsedSeconds / originalTotal) * 100;
            // Update progress bar
            if (progressBar) {
                progressBar.style.width = percentComplete.toFixed(2) + '%';
                progressBar.setAttribute('aria-valuenow', Math.round(percentComplete));
                // Change color
                if (percentComplete < 33) {
                    progressBar.className = 'progress-bar progress-bar-striped progress-bar-animated bg-danger';
                }
                else if (percentComplete < 66) {
                    //progressBar.className = 'progress-bar progress-bar-striped progress-bar-animated bg-warning';
                    progressBar.className = 'progress-bar progress-bar-striped progress-bar-animated bg-danger';
                }
                else {
                    //progressBar.className = 'progress-bar progress-bar-striped progress-bar-animated bg-success';
                    progressBar.className = 'progress-bar progress-bar-striped progress-bar-animated bg-danger';
                }
            }
            else {
                //if (updateCount === 1) {
                //    console.warn('⚠️ Progress bar not found, continuing without it');
                //}
            }
            // Calculate time display
            const minutes = Math.floor(secondsRemaining / 60);
            const seconds = secondsRemaining % 60;
            const timeString = `${minutes}:${seconds.toString().padStart(2, '0')}`;
            // Update displays
            countdownElement.textContent = timeString;
            timeRemainingElement.textContent = timeString;
            // Decrease and schedule next update
            secondsRemaining--;
            setTimeout(updateCountdown, 1000);
        }
        console.log('🚀 Starting countdown...');
        updateCountdown();
    },
    formatTime: function (seconds) {
        const minutes = Math.floor(seconds / 60);
        const secs = seconds % 60;
        return `${minutes}:${secs.toString().padStart(2, '0')}`;
    },
    formatMinutes: function (seconds) {
        const minutes = Math.floor(seconds / 60);
        return `${minutes} ${minutes === 1 ? 'minute' : 'minutes'}`;
    }
};
window.RateLimitCountdown = RateLimitCountdown;
/*console.log('✅ RateLimitCountdown loaded');*/
//# sourceMappingURL=rate-limit-countdown.js.map