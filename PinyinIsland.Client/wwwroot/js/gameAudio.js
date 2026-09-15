window.gameAudio = {
    audioCtx: null,
    bgmAudio: null,
    currentVoiceAudio: null,

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

    playVoiceAudio: function (audioSrc) {
        if (!audioSrc) return;
        try {
            var ctx = this.getAudioContext();
            if (this.currentVoiceAudio) {
                this.currentVoiceAudio.pause();
                this.currentVoiceAudio.currentTime = 0;
            }
            var fullUrl = new URL(audioSrc, document.baseURI).href;
            this.currentVoiceAudio = new Audio(fullUrl);
            this.currentVoiceAudio.volume = 1.0;
            var playPromise = this.currentVoiceAudio.play();
            if (playPromise !== undefined) {
                playPromise.catch(function (err) {
                    console.log("Audio play error for voice:", fullUrl, err);
                });
            }
        } catch (e) {
            console.error("playVoiceAudio exception:", e);
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

    playSfx: function (type) {
        var ctx = this.getAudioContext();
        if (!ctx) return;

        var now = ctx.currentTime;

        if (type === 'correct') {
            // Bright cheerful 2-tone Ding-Dong (Major third chime)
            var osc1 = ctx.createOscillator();
            var gain1 = ctx.createGain();
            osc1.type = 'sine';
            osc1.frequency.setValueAtTime(587.33, now); // D5
            osc1.frequency.exponentialRampToValueAtTime(880.00, now + 0.12); // A5
            gain1.gain.setValueAtTime(0.3, now);
            gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.5);
            osc1.connect(gain1);
            gain1.connect(ctx.destination);
            osc1.start(now);
            osc1.stop(now + 0.5);

            var osc2 = ctx.createOscillator();
            var gain2 = ctx.createGain();
            osc2.type = 'triangle';
            osc2.frequency.setValueAtTime(1174.66, now + 0.12); // D6
            gain2.gain.setValueAtTime(0.25, now + 0.12);
            gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.7);
            osc2.connect(gain2);
            gain2.connect(ctx.destination);
            osc2.start(now + 0.12);
            osc2.stop(now + 0.7);
        } else if (type === 'wrong') {
            // Gentle cartoon "Boing / Wobble" spring (Kid-friendly, non-punishing)
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.type = 'sine';
            osc.frequency.setValueAtTime(220, now);
            osc.frequency.exponentialRampToValueAtTime(130, now + 0.28);
            osc.frequency.exponentialRampToValueAtTime(180, now + 0.38);
            gain.gain.setValueAtTime(0.25, now);
            gain.gain.exponentialRampToValueAtTime(0.01, now + 0.45);
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.start(now);
            osc.stop(now + 0.45);
        } else if (type === 'cheer' || type === 'fanfare') {
            // Stage Clear Victory Arpeggio (C5 - E5 - G5 - C6)
            var notes = [523.25, 659.25, 783.99, 1046.50];
            notes.forEach(function (freq, index) {
                var osc = ctx.createOscillator();
                var gain = ctx.createGain();
                var startTime = now + (index * 0.1);
                osc.type = 'triangle';
                osc.frequency.setValueAtTime(freq, startTime);
                gain.gain.setValueAtTime(0.3, startTime);
                gain.gain.exponentialRampToValueAtTime(0.001, startTime + 0.6);
                osc.connect(gain);
                gain.connect(ctx.destination);
                osc.start(startTime);
                osc.stop(startTime + 0.6);
            });
        } else if (type === 'star') {
            // Sparkly Star pop chime
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.type = 'sine';
            osc.frequency.setValueAtTime(987.77, now); // B5
            osc.frequency.exponentialRampToValueAtTime(1318.51, now + 0.15); // E6
            gain.gain.setValueAtTime(0.3, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.4);
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.start(now);
            osc.stop(now + 0.4);
        } else if (type === 'tap') {
            // Snappy bubble button tap
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.type = 'sine';
            osc.frequency.setValueAtTime(440, now);
            osc.frequency.exponentialRampToValueAtTime(880, now + 0.08);
            gain.gain.setValueAtTime(0.2, now);
            gain.gain.exponentialRampToValueAtTime(0.01, now + 0.08);
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.start(now);
            osc.stop(now + 0.08);
        } else if (type === 'trainWhistle') {
            // Bright cheerful cartoon train whistle (Harmonic chord: D5 (587.33), A5 (880), D6 (1174.66))
            var chords = [587.33, 880, 1174.66];
            
            // First Toot (Short)
            chords.forEach(function (f) {
                var osc = ctx.createOscillator();
                var gain = ctx.createGain();
                osc.type = 'triangle';
                osc.frequency.setValueAtTime(f, now);
                osc.frequency.linearRampToValueAtTime(f + 15, now + 0.22);
                gain.gain.setValueAtTime(0.25, now);
                gain.gain.exponentialRampToValueAtTime(0.001, now + 0.25);
                osc.connect(gain);
                gain.connect(ctx.destination);
                osc.start(now);
                osc.stop(now + 0.25);
            });

            // Second Toot (Long & Powerful)
            chords.forEach(function (f) {
                var osc = ctx.createOscillator();
                var gain = ctx.createGain();
                osc.type = 'triangle';
                osc.frequency.setValueAtTime(f, now + 0.28);
                osc.frequency.linearRampToValueAtTime(f + 25, now + 0.85);
                gain.gain.setValueAtTime(0.35, now + 0.28);
                gain.gain.exponentialRampToValueAtTime(0.001, now + 0.9);
                osc.connect(gain);
                gain.connect(ctx.destination);
                osc.start(now + 0.28);
                osc.stop(now + 0.9);
            });
        } else if (type === 'cardFlip') {
            // Quick whoosh/swish
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.type = 'sine';
            osc.frequency.setValueAtTime(300, now);
            osc.frequency.exponentialRampToValueAtTime(700, now + 0.12);
            gain.gain.setValueAtTime(0.15, now);
            gain.gain.exponentialRampToValueAtTime(0.01, now + 0.12);
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.start(now);
            osc.stop(now + 0.12);
        } else if (type === 'cardMatch') {
            // Sweet sparkly chime match
            [880, 1108.73, 1318.51, 1760].forEach(function (freq, i) {
                var osc = ctx.createOscillator();
                var gain = ctx.createGain();
                var start = now + (i * 0.08);
                osc.type = 'sine';
                osc.frequency.setValueAtTime(freq, start);
                gain.gain.setValueAtTime(0.2, start);
                gain.gain.exponentialRampToValueAtTime(0.001, start + 0.35);
                osc.connect(gain);
                gain.connect(ctx.destination);
                osc.start(start);
                osc.stop(start + 0.35);
            });
        } else if (type === 'blockDrop') {
            // Soft tactile drop click
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.type = 'triangle';
            osc.frequency.setValueAtTime(520, now);
            osc.frequency.exponentialRampToValueAtTime(260, now + 0.09);
            gain.gain.setValueAtTime(0.25, now);
            gain.gain.exponentialRampToValueAtTime(0.01, now + 0.09);
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.start(now);
            osc.stop(now + 0.09);
        } else if (type === 'shuffle') {
            // Energetic game mode switch fanfare
            [440, 554.37, 659.25, 880, 1108.73].forEach(function (freq, i) {
                var osc = ctx.createOscillator();
                var gain = ctx.createGain();
                var start = now + (i * 0.06);
                osc.type = 'triangle';
                osc.frequency.setValueAtTime(freq, start);
                gain.gain.setValueAtTime(0.2, start);
                gain.gain.exponentialRampToValueAtTime(0.001, start + 0.25);
                osc.connect(gain);
                gain.connect(ctx.destination);
                osc.start(start);
                osc.stop(start + 0.25);
            });
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
                this.bgmAudio.play().catch(function (e) { console.log(e); });
            }
        }
    },

    pauseBgm: function () {
        if (this.bgmAudio) {
            this.bgmAudio.pause();
        }
    }
};
