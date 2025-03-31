import React, { useEffect, useState } from 'react';
import videojs from 'video.js';
import 'video.js/dist/video-js.css';
import './qualitySelector/plugin.js';
import 'hls.js';
import './Player.css';

// Fetch the link to playlist.m3u8 of the video you want to play
export const VideoPlayer = ({ thumbnail, path, onTimeupdate, currentTime, onUserSeek, setPlayerRef, onPause, onPlay }) => {
  const videoRef = React.useRef(null);
  const playerRef = React.useRef(null);
  let viewRecorded = false;
  let sessionHash = null;

  const options = {
    autoplay: path.autoplay == undefined ? false : path.autoplay,
    controls: true,
    playbackRates: [0.5, 1, 1.5, 2],
    preload: 'none',//path.preload == undefined ? 'none' : path.preload,
    responsive: true,
    fluid: true, // Добавить fluid mode
    aspectRatio: '16:9', // Установить соотношение сторон
    poster: thumbnail,
    plugins: {
      qualitySelectorHls: {
        displayCurrentQuality: true,
        vjsIconClass: 'vjs-icon-hd'
      }
    },
    controlBar: {
      playToggle: true,
      volumePanel: {
        inline: false
      },
      skipButtons: {
        forward: 10,
        backward: 10
      },

      fullscreenToggle: true
    },
    sources: {
      src: path.url,
      type: 'application/x-mpegURL',
    }
  };

  useEffect(() => {
    // Make sure Video.js player is only initialized once
    if (!playerRef.current) {
      // The Video.js player needs to be _inside_ the component el for React 18 Strict Mode. 
      const videoElement = document.createElement("video-js");
      videoElement.classList.add('vjs-big-play-centered');
      videoElement.classList.add('vjs-default-skin');
      videoRef.current.appendChild(videoElement);
      playerRef.current = videojs(videoElement, options, function () {
        var player = this;
        var qualities = player.qualityLevels();

        player.on('timeupdate', () => {
          if (onTimeupdate)
            onTimeupdate(player);
        })

        player.on('pause', () => {
          if (onPause) {
            onPause(player.currentTime());
          }
        });

        player.on('play', () => {
          if (onPlay) {
            onPlay();
          }
        });

        player.on('seeked', () => {
          if (onUserSeek != undefined && onUserSeek != null) {
            onUserSeek(player.currentTime());
          }
        })

        if (currentTime) {
          player.currentTime(currentTime)
        }

        if (setPlayerRef != undefined && setPlayerRef != null) {
          setPlayerRef(player)
        }

        qualities.on('addqualitylevel', () => {

        });

      });

    } else {
      const player = playerRef.current;

      player.autoplay(options.autoplay);
      player.src(options.sources);
      player.poster(options.poster);

    }
  }, [options, videoRef, path.postId]);

  React.useEffect(() => {
    const player = playerRef.current;

    return () => {
      if (player && !player.isDisposed()) {
        player.dispose();
        playerRef.current = null;
      }
    };
  }, [playerRef]);

  return (
    <div data-vjs-player>
      <div
        ref={videoRef}
      >

      </div>

    </div>
  );
}
const generateDeviceHash = () => {
  const data = [
    navigator.userAgent,
    window.screen.width,
    window.screen.height,
    new Date().getTimezoneOffset()
  ].join('|');
  return btoa(data).slice(0, 32);
};

export default VideoPlayer;