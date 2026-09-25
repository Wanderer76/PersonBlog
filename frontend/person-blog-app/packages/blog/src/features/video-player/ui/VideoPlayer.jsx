import { useEffect, useRef } from 'react';
import videojs from 'video.js';
import 'video.js/dist/video-js.css';
import './quality-selector/plugin.js';
import './Player.css';
import { BaseApUrl } from '@/shared/api/client.js';
import { JwtTokenService } from '@/shared/auth/tokenStorage';

const addAuthorizationHeader = (requestOptions) => {
  const authorization = JwtTokenService.getFormatedTokenForHeader();

  if (authorization && requestOptions.uri?.startsWith(BaseApUrl)) {
    requestOptions.headers = {
      ...requestOptions.headers,
      Authorization: authorization,
    };
  }

  return requestOptions;
};

if (videojs.Vhs?.xhr?.onRequest) {
  const previousHook = videojs.Vhs.xhr.playViewAuthorizationHook;

  if (previousHook && videojs.Vhs.xhr.offRequest) {
    videojs.Vhs.xhr.offRequest(previousHook);
  }

  videojs.Vhs.xhr.onRequest(addAuthorizationHeader);
  videojs.Vhs.xhr.playViewAuthorizationHook = addAuthorizationHeader;
}

export const VideoPlayer = ({
  thumbnail,
  path,
  onTimeupdate,
  currentTime,
  onUserSeek,
  setPlayerRef,
  onPause,
  onPlay,
  onEnded,
  className = '',
}) => {
  const videoRef = useRef(null);
  const playerRef = useRef(null);
  const sourceRef = useRef(null);
  const callbacksRef = useRef({});

  callbacksRef.current = {
    onTimeupdate,
    onUserSeek,
    setPlayerRef,
    onPause,
    onPlay,
    onEnded,
  };

  const sourceUrl = `${BaseApUrl}/video/Video/${path.blogId}/${path.postId}/${path.objectName}`;
  const autoplay = path.autoplay ?? false;

  useEffect(() => {
    if (!videoRef.current || playerRef.current) return undefined;

    const videoElement = document.createElement('video-js');
    videoElement.classList.add('vjs-big-play-centered', 'vjs-default-skin');
    videoRef.current.appendChild(videoElement);

    const player = videojs(videoElement, {
      autoplay,
      controls: true,
      playbackRates: [0.5, 1, 1.5, 2],
      preload: 'metadata',
      responsive: true,
      fluid: true,
      html5: {
        vhs: {
          overrideNative: true,
        },
      },
      aspectRatio: '16:9',
      poster: thumbnail,
      plugins: {
        qualitySelectorHls: {
          displayCurrentQuality: true,
          vjsIconClass: 'vjs-icon-hd',
        },
      },
      controlBar: {
        playToggle: true,
        volumePanel: { inline: false },
        skipButtons: { forward: 10, backward: 10 },
        fullscreenToggle: true,
      },
      sources: [{ src: sourceUrl, type: 'application/x-mpegURL' }],
    });

    playerRef.current = player;
    sourceRef.current = sourceUrl;

    player.on('timeupdate', () => callbacksRef.current.onTimeupdate?.(player));
    player.on('pause', () => callbacksRef.current.onPause?.(player));
    player.on('ended', () => callbacksRef.current.onEnded?.(player));
    player.on('play', () => callbacksRef.current.onPlay?.(player));
    player.on('seeked', () => callbacksRef.current.onUserSeek?.(player.currentTime()));

    const initialTime = Number(currentTime);
    if (Number.isFinite(initialTime) && initialTime > 0) {
      player.one('loadedmetadata', () => player.currentTime(initialTime));
    }

    callbacksRef.current.setPlayerRef?.(player);

    return () => {
      if (!player.isDisposed()) player.dispose();
      playerRef.current = null;
      sourceRef.current = null;
    };
    // The player is intentionally created once. Dynamic values are handled below.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    const player = playerRef.current;
    if (!player || player.isDisposed()) return;

    player.autoplay(autoplay);
    player.poster(thumbnail ?? '');

    if (sourceRef.current !== sourceUrl) {
      sourceRef.current = sourceUrl;
      player.src({ src: sourceUrl, type: 'application/x-mpegURL' });
    }
  }, [autoplay, sourceUrl, thumbnail]);

  return (
    <div className={`video-player-root ${className}`.trim()} data-vjs-player>
      <div ref={videoRef} />
    </div>
  );
};

export default VideoPlayer;
