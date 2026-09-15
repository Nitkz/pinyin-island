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
    },
    bgmAudio: null,
    playBgm: function (audioSrc, volume) {
        if (!this.bgmAudio) {
            this.bgmAudio = new Audio(audioSrc);
            this.bgmAudio.loop = true;
        } else {
            if (this.bgmAudio.src !== new URL(audioSrc, window.location.href).href) {
                this.bgmAudio.src = audioSrc;
            }
        }
        this.bgmAudio.volume = volume !== undefined ? volume : 0.45;
        var playPromise = this.bgmAudio.play();
        if (playPromise !== undefined) {
            playPromise.catch(function (err) {
                console.log("BGM autoplay prevented, waiting for touch/click:", err);
                var resumeOnInteraction = function () {
                    if (window.gameAudio.bgmAudio && !window.gameAudio.bgmAudio.muted) {
                        window.gameAudio.bgmAudio.play();
                    }
                    document.removeEventListener('click', resumeOnInteraction);
                    document.removeEventListener('touchstart', resumeOnInteraction);
                };
                document.addEventListener('click', resumeOnInteraction, { once: true });
                document.addEventListener('touchstart', resumeOnInteraction, { once: true });
            });
        }
    },
    setBgmMuted: function (isMuted) {
        if (this.bgmAudio) {
            this.bgmAudio.muted = isMuted;
            if (!isMuted && this.bgmAudio.paused) {
                this.bgmAudio.play().catch(function(e) { console.log(e); });
            }
        }
    },
    pauseBgm: function () {
        if (this.bgmAudio) {
            this.bgmAudio.pause();
        }
    }
};

