import React, { useEffect } from 'react';
import videojs from 'video.js';
import 'video.js/dist/video-js.css';
import './qualitySelector/plugin.js';
import 'hls.js';


// Fetch the link to playlist.m3u8 of the video you want to play
export const VideoPlayer = ({ thumbnail, path }) => {
  const videoRef = React.useRef(null);
  const playerRef = React.useRef(null);
  const options = {
    autoplay: false,
    controls: true,
    playbackRates: [0.5, 1, 1.5, 2],
    width: 500,
    height: 500,
    responsive: true,
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

        qualities.on('addqualitylevel', () => {

        });
        qualities.on('change', (a) => {
        });

      });

    } else {
      const player = playerRef.current;

      player.autoplay(options.autoplay);
      player.src(options.sources);
    }
  }, [options, videoRef, path]);

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


export default VideoPlayer;