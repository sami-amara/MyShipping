//window.ScrollHelper = {
//    scrollToCenter: function (input) {
//        if (!input) return;

//        const rect = input.getBoundingClientRect();
//        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
//        const targetY = rect.top + scrollTop - (window.innerHeight / 2);

//        const startY = window.pageYOffset;
//        const distance = targetY - startY;
//        const duration = 500;
//        let startTime = null;

//        function step(timestamp) {
//            if (!startTime) startTime = timestamp;
//            const progress = timestamp - startTime;
//            const percent = Math.min(progress / duration, 1);
//            const easeInOut = percent < 0.5
//                ? 2 * percent * percent
//                : -1 + (4 - 2 * percent) * percent;

//            window.scrollTo(0, startY + distance * easeInOut);

//            if (progress < duration) {
//                window.requestAnimationFrame(step);
//            } else {
//                input.focus();
//                input.classList.add('field-pulse');
//                setTimeout(() => input.classList.remove('field-pulse'), 1000);
//            }
//        }

//        window.requestAnimationFrame(step);
//    }
//};