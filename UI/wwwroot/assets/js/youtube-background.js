var tag = document.createElement('script');
tag.src = 'https://www.youtube.com/player_api';
var firstScriptTag = document.getElementsByTagName('script')[0];
firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);

(function ($) {

    $.fn.youtube_background = function() {
        const YOUTUBE = /(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?\/ ]{11})/i;

        const $this = $(this);

        function onVideoPlayerReady(event) {
            event.target.playVideo();

            $(event.target.a).css({
                'top' : '50%',
                'left' : '50%',
                'transform': 'translateX(-50%) translateY(-50%)',
                'position': 'absolute'
            });

            const $root = $(event.target.a).parent();

            function onResize() {
                const h = $root.outerHeight() + 100; // since showinfo is deprecated and ignored after September 25, 2018. we add +100
                const w = $root.outerWidth() + 100;
                const res = 1.77777778;

                if (res > w/h) {
                    $root.find('iframe').width(h*res).height(h);
                } else {
                    $root.find('iframe').width(w).height(w/res);
                }
            }
            $(window).on('resize', onResize);
            onResize();
        }

        function onVideoStateChange(event) {
            event.target.playVideo();
        }

        let ytp = null;
        let yt_event_triggered = false;

        window.onYouTubeIframeAPIReady = function () {
            yt_event_triggered = true;

             //element loop
            for (let i = 0; i < $this.length; i++) {
                const $elem = $($this[i]);

                if ($elem.parent().hasClass('youtube-background')) {
                    continue;
                }

                $elem.wrap('<div class="youtube-background" />');
                const $root = $elem.parent();

                $root.css({
                    'height' : '100%',
                    'width' : '100%',
                    'z-index': '0',
                    'position': 'absolute',
                    'overflow': 'hidden'
                });

                $root.parent().parent().css({
                    'position': 'relative'
                });

                let ytid = $elem.data('youtube');

                const pts = ytid.match(YOUTUBE);
                if (pts && pts.length) {
                    ytid = pts[1];
                }

                ytp = new YT.Player($elem[0], {
                    height: '1080',
                    width: '1920',
                    videoId: ytid,
                    playerVars: {
                        'controls': 0,
                        'autoplay': 1,
                        'mute': 1,
                        'loop': 1,
                        'rel': 0,
                        'showinfo': 0,
                        'modestbranding': 1
                    },
                    events: {
                        'onReady': onVideoPlayerReady,
                        'onStateChange': onVideoStateChange
                    }
                });
            }
        };

        if (window.hasOwnProperty('YT') && window.YT.loaded && !yt_event_triggered) {
            window.onYouTubeIframeAPIReady();
        }

 		return $this;
 	};
})(jQuery);
