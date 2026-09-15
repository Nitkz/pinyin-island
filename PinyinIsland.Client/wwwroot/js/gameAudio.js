window.gameAudio = {
    playVideo: function (videoId) {
        var video = document.getElementById(videoId);
        if (video) {
            video.muted = true;
            video.defaultMuted = true;
            video.playsInline = true;
            var playPromise = video.play();
            if (playPromise !== undefined) {
                playPromise.catch(function (error) {
                    console.log("Autoplay was prevented, waiting for user interaction:", error);
                    var startPlayOnInteraction = function () {
                        video.play();
                        document.removeEventListener('click', startPlayOnInteraction);
                        document.removeEventListener('touchstart', startPlayOnInteraction);
                    };
                    document.addEventListener('click', startPlayOnInteraction, { once: true });
                    document.addEventListener('touchstart', startPlayOnInteraction, { once: true });
                });
            }
        }
    },
    setVideoMuted: function (videoId, isMuted) {
        var video = document.getElementById(videoId);
        if (video) {
            video.muted = isMuted;
            if (!isMuted) {
                video.play().catch(function (e) {
                    console.log("Audio play error:", e);
                });
            }
        }
    }
};
