class FlvPlayer {
    constructor(element, instance) {
        this.player = null;
        this.element = element;
        this.instance = instance;
    }

    init(mediaDataSource, config) {
        //document.getElementById("debug").innerHTML = window.MediaSource;
        if (this.player !== null) return;
        let autoplay = mediaDataSource['autoplay'];

        this.player = mpegts.createPlayer(mediaDataSource, config);
        this.player.attachMediaElement(this.element);
        this.player.load();
        if (autoplay) {
            this.player.muted = true;
            this.player.play();
        }

        let count = 0;

        this.element.onprogress = (event) => {
            let _this = event.target;

            if (count > 10) {
                this.player.pause();
                this.player.unload();
                this.player.detachMediaElement();
                this.player.destroy();
                this.player = null;

                this.init(mediaDataSource, config);
                return;
            }

            if (_this.buffered.length === 0) {
                console.log('Length == 0');
                count++;
                return;
            }

            count = 0;
            
            let end = _this.buffered.end(0);
            let delta = end - _this.currentTime;

            //console.log(delta);
            if (_this.paused && delta > 0.5) _this.play();

            if ((delta >= 2 || delta <= 0) && autoplay) {
                console.log('跳轉', _this.paused);
                _this.currentTime = Math.max(_this.buffered.end(0) - 1.5, _this.buffered.start(0));
                if (_this.paused) _this.play();
                return;
            }

            if (delta > 1) {
                _this.playbackRate = 1.2;
            }
            if (0.35 < delta || delta < 0.7) {
                _this.playbackRate = 1;
            }
            if (delta < 0.3) {
                _this.playbackRate = 0.8;
            }
        };

        this.player.on(mpegts.Events.ERROR, (event) => {
            console.log(this.player);
            this.player.pause();
            this.player.unload();
            this.player.detachMediaElement();
            this.player.destroy();
            this.player = null;

            this.init(mediaDataSource, config);
        });

        this.instance.invokeMethodAsync('GetInit', true);
    }

    dispose() {
        this.player.pause();
        this.player.unload();
        this.player.detachMediaElement();
        this.player.destroy();
        this.player = null;
        this.element = null;
        this.instance = null;
    }
}

class HlsPlayer {
    constructor(element, instance) {
        this.player = null;
        this.element = element;
        this.instance = instance;
    }

    init(mediaDataSource, config) {
        if (this.player !== null || !Hls.isSupported()) return;
        let autoplay = mediaDataSource['autoplay'];
        let src = mediaDataSource['url'];

        this.player = new Hls(config);
        this.player.attachMedia(this.element);
        this.player.on(Hls.Events.MEDIA_ATTACHED, (event) => {
            console.log("video and hls.js are now bound together!", event);
            this.player.loadSource(src);
            if (autoplay) {
                this.player.muted = true;
                this.player.play();
            }
        });

        this.player.on(Hls.Events.ERROR, function (event, data) {
            if (data.fatal) {
                switch (data.type) {
                    case Hls.ErrorTypes.MEDIA_ERROR:
                        console.log('fatal media error encountered, try to recover');
                        this.player.recoverMediaError();
                        break;
                    case Hls.ErrorTypes.NETWORK_ERROR:
                        this.player.error('fatal network error encountered', data);
                        //break;
                    default:
                        this.player.destroy();
                        this.player = null;
                        this.init(mediaDataSource, config);
                        break;
                }
            }
        });

        this.instance.invokeMethodAsync('GetInit', true);
    }

    dispose() {
        this.player.destroy();
        this.player = null;
        this.element = null;
        this.instance = null;
    }
}

function load_flv(instance, id, options) {
    stream_src = options.src;
    stream_type = options.type;

    if (stream_type === 'video/x-flv') {
        let element = document.getElementById(id);
        element.width = options.width;
        element.height = options.height;
        element.classList['value'] = options.class;
        element.controls = options.controls;
        element.autoplay = options.autoplay;
        element.preload = options.preload;
        element.muted = options.muted;
        console.log("element:", element.autoplay);

        let flv_player = new FlvPlayer(element, instance);
        flv_player.init({
            type: 'flv',
            url: stream_src,
            isLive: true,
            hasVideo: true,
            hasAudio: false,
            autoplay: element.autoplay,
        }, {
            enableWorker: true,
            enableStashBuffer: false,
            stashInitialSize: 128,
            autoCleanupSourceBuffer: true,
            autoCleanupMaxBackwardDuration: 60,
            autoCleanupMinBackwardDuration: 30,
        });

        let interval = setInterval(() => {
            if (element != document.getElementById(id)) {
                flv_player.dispose();
                flv_player = null;
                clearInterval(interval);
                console.log('dispose.');
            }
        }, 1000);
    }
}

function load_hls(instance, id, options) {
    stream_src = options.src;
    stream_type = options.type;

    if (stream_type === 'application/x-mpegURL') {
        let element = document.getElementById(id);
        element.width = options.width;
        element.height = options.height;
        element.classList['value'] = options.class;
        element.controls = options.controls;
        element.autoplay = options.autoplay;
        element.preload = options.preload;
        element.muted = options.muted;
        console.log("element:", element.autoplay);

        let hls_player = new HlsPlayer(element, instance);
        hls_player.init({
            url: stream_src,
            autoplay: element.autoplay,
        }, {
            maxBufferLength: 60,
            backBufferLength: 30,
            maxLoadingDelay: 3,
        });

        let interval = setInterval(() => {
            if (element != document.getElementById(id)) {
                hls_player.dispose();
                hls_player = null;
                clearInterval(interval);
                console.log('dispose.');
            }
        }, 1000);
    }
}

function load_player(instance, id, options) {
    stream_src = options.sources[0]['src'];
    stream_type = options.sources[0]['type'];

    console.log('player id:', id);
    console.log('stream src:', stream_src);
    console.log('stream type:', stream_type);

    if (stream_type === 'video/x-flv') {
        if (mpegts.isSupported()) {
            e = document.getElementById(id);
            let flv_player = new FlvPlayer(document.getElementById(id), instance);
            flv_player.init({
                type: 'flv',
                url: stream_src,
                isLive: true,
                hasVideo: true,
                hasAudio: false,
            }, {
                enableWorker: true,
                enableStashBuffer: false,
                stashInitialSize: 128,
                autoCleanupSourceBuffer: true,
                autoCleanupMaxBackwardDuration: 60,
                autoCleanupMinBackwardDuration: 30,
            });

            interval = setInterval(() => {
                if (e != document.getElementById(id)) {
                    flv_player.dispose();
                    flv_player = null;
                    console.log('dispose');
                    clearInterval(interval);
                }
            }, 1000);

            /*
            let player = document.getElementById(id);
            console.log(player);
            let flv = mpegts.createPlayer({
                url: stream_src,
                type: 'flv',
                //isLive: true,
            });
            flv.attachMediaElement(player);
            flv.load();
            console.log(player.muted);
            flv.muted = true;
            flv.play();

            player.addEventListener('progress', (event) => {
                //console.log('event: ', event.target);
                let _this = event.target;
                let end = _this.buffered.end(0);
                let delta = end - _this.currentTime;
                //console.log(delta);

                if (delta > 10 || delta < 0) {
                    _this.currentTime = _this.buffered.end(0) - 1;
                    return;
                }

                if (delta > 1) {
                    _this.playbackRate = 1.1;
                    //console.log("go speed");
                } else {
                    _this.playbackRate = 1;
                }
            });

            flv.on(mpegts.Events.ERROR, (event) => {
                console.log(flv);
                flv.pause();
                flv.unload();
                flv.detachMediaElement();
                flv.destroy();

                load_player(instance, id, options);
            });*/
        }
    }

    else if (stream_type === 'application/x-mpegUR') {
        if (Hls.isSupported()) {
            let player = document.getElementById(id);
            let hls = new Hls();
            hls.attachMedia(player);
            console.log("player", player)

            hls.on(Hls.Events.MEDIA_ATTACHED, function () {
                console.log("video and hls.js are now bound together !");
                hls.loadSource("https://bitdash-a.akamaihd.net/content/MI201109210084_1/m3u8s/f08e80da-bf1d-4e3d-8899-f0f6155f6efa.m3u8");
            });
        }
    }

    else {
        let player = videojs(id, options);
        player.ready(function () {
            var promise = player.play();
            console.log('player.ready');

            if (promise !== undefined) {
                promise.then(function () {
                    console.log('Autoplay started!');
                }).catch(function (error) {
                    console.log('Autoplay was prevented.', error);
                    instance.invokeMethodAsync('GetError', 'Autoplay was prevented.' + error);
                });
            }

            instance.invokeMethodAsync('GetInit', true);
        });
    }

    return;
}

function destroy(id) {
    if (player !== undefined && player !== null) {
        player = null;
        console.log('destroy');
    }
}

function notification(msg) {
    if (("Notification" in window)) {
        // 要求權限
        Notification.requestPermission().then((permission) => {
            if (permission === 'granted') {
                var notification = new Notification(msg);
            }
        })
    }
}

/*
stream_src = options.sources[0]['src'];
    stream_type = options.sources[0]['type'];

    console.log('player id:', id);
    console.log('stream src:', stream_src);
    console.log('stream type:', stream_type);

    if (mpegts.getFeatureList().mseLivePlayback) {
        let player = document.getElementById(id);

        let flv = mpegts.createPlayer({
            type: 'flv',
            url: stream_src,
            isLive: true,
            hasVideo: true,
            hasAudio: false,
        }, {
            enableWorker: true,
            enableStashBuffer: false,
            stashInitialSize: 128,
        });

        flv.attachMediaElement(player);
        flv.muted = true;
        flv.load();
        flv.play();

        player.addEventListener('progress', (event) => {
            //console.log('event: ', event.target);
            let _this = event.target;
            let end = _this.buffered.end(0);
            let delta = end - _this.currentTime;
            //console.log(delta);

            if (delta > 10 || delta < 0) {
                _this.currentTime = _this.buffered.end(0) - 1;
                return;
            }

            if (delta > 1) {
                _this.playbackRate = 1.1;
                //console.log("go speed");
            } else {
                _this.playbackRate = 1;
            }
        });

        flv.on(mpegts.Events.ERROR, (event) => {
            let _this = event.target;
            console.log(_this);
            _this.pause();
            _this.unload();
            flv.detachMediaElement();
            flv.destroy();
        });
    }
*/




/*
            try {
                _this.buffered.end(0);
            } catch (e) {
                console.log('Some error:', e);
                
                _this.pause();
                var promise = _this.play();
                if (promise !== undefined) {
                    promise.then(_ => {
                        console.log('auto-play suc');
                    })
                    .catch(error => {
                        console.log('Bad error2', error);
                    });
                }
                return;
            }
*/

/*
            
                //_this.pause();
                //_this.play();
                if (promise !== undefined) {
                    promise.then(_ => {
                        console.log('auto-play suc');
                    })
                    .catch(error => {
                        console.log('Bad error', error);
                    });
                }
*/