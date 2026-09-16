window.gameAudio = {
    audioCtx: null,
    bgmAudio: null,
    currentVoiceAudio: null,
    voiceAudioPool: null,
    audioBufferCache: {},
    isAudioUnlocked: false,

    initAudioUnlock: function () {
        if (this.isAudioUnlocked) return;
        var self = this;
        var unlockHandler = function () {
            // Unlock Web Audio Context
            var ctx = self.getAudioContext();
            if (ctx && ctx.state === 'suspended') {
                ctx.resume();
            }

            // Unlock shared HTMLAudioElement for iOS Safari
            if (!self.voiceAudioPool) {
                self.voiceAudioPool = new Audio();
            }
            // Playing a silent 1-sample data URI unlocks future programmatic .play() on iOS
            self.voiceAudioPool.src = "data:audio/wav;base64,UklGRigAAABXQVZFZm10IBIAAAABAAEARKwAAIhYAQACABAAAABkYXRhAgAAAAEA";
            self.voiceAudioPool.play().then(function () {
                self.isAudioUnlocked = true;
            }).catch(function () {});

            document.removeEventListener('touchstart', unlockHandler, true);
            document.removeEventListener('touchend', unlockHandler, true);
            document.removeEventListener('click', unlockHandler, true);
        };

        document.addEventListener('touchstart', unlockHandler, true);
        document.addEventListener('touchend', unlockHandler, true);
        document.addEventListener('click', unlockHandler, true);
    },

    getAudioContext: function () {
        if (!this.audioCtx) {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (AudioContext) {
                this.audioCtx = new AudioContext();
            }
        }
        if (this.audioCtx && this.audioCtx.state === 'suspended') {
            this.audioCtx.resume();
        }
        return this.audioCtx;
    },

    bgmTargetVolume: 0.45,
    isDuckingBgm: false,
    duckingTimer: null,

    duckBgm: function (durationMs) {
        if (!this.bgmAudio || this.bgmAudio.paused || this.bgmAudio.muted) return;
        var self = this;
        var duckVol = Math.min(this.bgmTargetVolume * 0.25, 0.12);
        this.bgmAudio.volume = duckVol;
        this.isDuckingBgm = true;

        if (this.duckingTimer) {
            clearTimeout(this.duckingTimer);
        }

        var resetDelay = durationMs || 1500;
        this.duckingTimer = setTimeout(function () {
            self.restoreBgm();
        }, resetDelay);
    },

    restoreBgm: function () {
        if (!this.bgmAudio || this.bgmAudio.paused || this.bgmAudio.muted) {
            this.isDuckingBgm = false;
            return;
        }
        var self = this;
        var startVol = this.bgmAudio.volume;
        var targetVol = this.bgmTargetVolume || 0.45;
        var steps = 8;
        var stepTime = 30;
        var count = 0;
        
        var interval = setInterval(function () {
            count++;
            if (self.bgmAudio && !self.bgmAudio.paused) {
                self.bgmAudio.volume = startVol + ((targetVol - startVol) * (count / steps));
            }
            if (count >= steps) {
                clearInterval(interval);
                if (self.bgmAudio) self.bgmAudio.volume = targetVol;
                self.isDuckingBgm = false;
            }
        }, stepTime);
    },

    playVoiceAudio: function (audioSrc) {
        if (!audioSrc) return;
        var self = this;
        // Auto-duck BGM so phonetic sound is crisp and loud
        this.duckBgm(1400);

        try {
            var ctx = this.getAudioContext();
            var fullUrl = new URL(audioSrc, document.baseURI).href;

            if (!this.voiceAudioPool) {
                this.voiceAudioPool = new Audio();
            }

            var audio = this.voiceAudioPool;
            audio.pause();
            audio.currentTime = 0;
            audio.src = fullUrl;
            audio.volume = 1.0;

            var playPromise = audio.play();
            if (playPromise !== undefined) {
                playPromise.catch(function (err) {
                    console.log("HTMLAudio play blocked on iOS, falling back to WebAudio decode:", fullUrl, err);
                    self.playVoiceWebAudioFallback(fullUrl);
                });
            }
        } catch (e) {
            console.error("playVoiceAudio exception:", e);
            self.playVoiceWebAudioFallback(new URL(audioSrc, document.baseURI).href);
        }
    },

    playVoiceWebAudioFallback: function (fullUrl) {
        var self = this;
        var ctx = this.getAudioContext();
        if (!ctx) return;

        if (this.audioBufferCache[fullUrl]) {
            self.playAudioBuffer(this.audioBufferCache[fullUrl]);
            return;
        }

        fetch(fullUrl)
            .then(function (res) { return res.arrayBuffer(); })
            .then(function (arrayBuf) { return ctx.decodeAudioData(arrayBuf); })
            .then(function (audioBuf) {
                self.audioBufferCache[fullUrl] = audioBuf;
                self.playAudioBuffer(audioBuf);
            })
            .catch(function (err) {
                console.error("WebAudio fallback failed for:", fullUrl, err);
            });
    },

    playAudioBuffer: function (audioBuf) {
        var ctx = this.getAudioContext();
        if (!ctx || !audioBuf) return;
        try {
            var srcNode = ctx.createBufferSource();
            srcNode.buffer = audioBuf;
            var gainNode = ctx.createGain();
            gainNode.gain.setValueAtTime(1.0, ctx.currentTime);
            srcNode.connect(gainNode);
            gainNode.connect(ctx.destination);
            srcNode.start(0);
        } catch (e) {
            console.error("playAudioBuffer error:", e);
        }
    },

    playPinyinAudio: function (charName) {
        if (!charName) return;
        var clean = charName.toLowerCase().trim();
        // Check if finals (ü/v, a, o, e, i, u, ai, ei, etc.) or initials
        var finals = ["a", "o", "e", "i", "u", "v", "ü", "ai", "ei", "ui", "ao", "ou", "iu", "ie", "ue", "üe", "er", "an", "en", "in", "un", "vn", "ün", "ang", "eng", "ing", "ong"];
        var folder = finals.indexOf(clean) >= 0 ? "finals" : "initials";
        if (clean === "ü") clean = "v";
        if (clean === "üe") clean = "ue";
        if (clean === "ün") clean = "vn";
        
        var audioPath = `assets/audio/${folder}/${clean}.mp3`;
        this.playVoiceAudio(audioPath);
    },

    playSfx: function (type, extraArg) {
        var self = this;
        var ctx = this.getAudioContext();
        if (window.audioSfx && typeof window.audioSfx.play === 'function') {
            window.audioSfx.play(ctx, type, extraArg);
            return;
        }

        // Lazy load audioSfx.js if not loaded yet
        if (!this._loadingAudioSfx) {
            this._loadingAudioSfx = true;
            var script = document.createElement('script');
            script.src = 'js/audioSfx.js';
            script.onload = function () {
                self._loadingAudioSfx = false;
                if (window.audioSfx && typeof window.audioSfx.play === 'function') {
                    window.audioSfx.play(self.getAudioContext(), type, extraArg);
                }
            };
            script.onerror = function () {
                self._loadingAudioSfx = false;
                console.error("Failed to load js/audioSfx.js");
            };
            document.head.appendChild(script);
        }
    },

    playLetterVideo: function (targetIndex, count) {
        for (var i = 0; i < count; i++) {
            var v = document.getElementById("learnVideo_" + i);
            if (v) {
                if (i === targetIndex) {
                    v.classList.remove("video-hidden");
                    v.classList.add("video-active");
                    v.currentTime = 0;
                    v.muted = false;
                    v.volume = 1.0;
                    var p = v.play();
                    if (p !== undefined) {
                        p.catch(function (err) {
                            console.log("Unmuted autoplay restricted on letter video:", err);
                        });
                    }
                } else {
                    v.pause();
                    v.currentTime = 0;
                    v.classList.remove("video-active");
                    v.classList.add("video-hidden");
                }
            }
        }
    },

    pauseAllLetterVideos: function (count) {
        for (var i = 0; i < count; i++) {
            var v = document.getElementById("learnVideo_" + i);
            if (v) {
                v.pause();
                v.muted = true;
            }
        }
    },

    playBgm: function (audioSrc, volume) {
        var fullSrc = new URL(audioSrc, window.location.href).href;
        if (!this.bgmAudio) {
            this.bgmAudio = new Audio(audioSrc);
            this.bgmAudio.loop = true;
        } else {
            if (this.bgmAudio.src !== fullSrc) {
                this.bgmAudio.pause();
                this.bgmAudio.currentTime = 0;
                this.bgmAudio.src = audioSrc;
            }
        }
        this.bgmTargetVolume = volume !== undefined ? volume : 0.45;
        this.bgmAudio.volume = this.isDuckingBgm ? Math.min(this.bgmTargetVolume * 0.25, 0.12) : this.bgmTargetVolume;
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

    playVictoryBgm: function (isBoss, volume) {
        var src = isBoss ? "assets/audio/bgm-victory-boss.mp3" : "assets/audio/bgm-victory-stage.mp3";
        var vol = volume !== undefined ? volume : (isBoss ? 0.42 : 0.38);
        this.playBgm(src, vol);
    },

    fadeOutBgm: function (durationMs) {
        if (!this.bgmAudio || this.bgmAudio.paused) return;
        var self = this;
        var duration = durationMs || 500;
        var startVol = this.bgmAudio.volume;
        var steps = 10;
        var stepTime = duration / steps;
        var stepCount = 0;
        var fadeInterval = setInterval(function () {
            stepCount++;
            if (self.bgmAudio && !self.bgmAudio.paused) {
                self.bgmAudio.volume = Math.max(0, startVol * (1 - stepCount / steps));
            }
            if (stepCount >= steps) {
                clearInterval(fadeInterval);
                self.pauseBgm();
                if (self.bgmAudio) self.bgmAudio.volume = startVol;
            }
        }, stepTime);
    },

    setBgmMuted: function (isMuted) {
        if (this.bgmAudio) {
            this.bgmAudio.muted = isMuted;
            if (!isMuted && this.bgmAudio.paused) {
                this.bgmAudio.play().catch(function (e) { console.log(e); });
            }
        }
    },

    pauseBgm: function () {
        if (this.bgmAudio) {
            this.bgmAudio.pause();
        }
    },

    playVideo: function (elementId) {
        var v = document.getElementById(elementId);
        if (v) {
            v.playsInline = true;
            // Attempt to play with sound
            var p = v.play();
            if (p !== undefined) {
                p.catch(function (err) {
                    console.log("Autoplay with sound restricted, playing muted until interaction:", err);
                    v.muted = true;
                    v.play().catch(function(e) { console.log(e); });
                    
                    var unmuteOnTouch = function () {
                        v.muted = false;
                        v.volume = 1.0;
                        v.play().catch(function (e) { console.log(e); });
                        document.removeEventListener('click', unmuteOnTouch);
                        document.removeEventListener('touchstart', unmuteOnTouch);
                    };
                    document.addEventListener('click', unmuteOnTouch, { once: true });
                    document.addEventListener('touchstart', unmuteOnTouch, { once: true });
                });
            }
        }
    },

    setVideoMuted: function (elementId, isMuted) {
        var v = document.getElementById(elementId);
        if (v) {
            v.muted = isMuted;
            if (!isMuted) {
                v.volume = 1.0;
                v.play().catch(function (err) { console.log("Error unmuting video:", err); });
            }
        }
    },

    playStarSound: function (starIndex) {
        this.playSfx('star', starIndex);
    }
};
