import React, { useEffect } from 'react';
import videojs from 'video.js';
import 'video.js/dist/video-js.css';
import 'videojs-resolution-switcher-v8';

// FOR MORE VIDEO PLAYER OPTIONS, VISIT: https://videojs.com/guides/options/

// const thumbnail = 'https://www.animenewsnetwork.com/hotlink/thumbnails/crop1200x630gNE/youtube/wm0pDk3HChM.jpg';
// const videoUrl = 'http://localhost:8000/videos/587a4d55-661f-4a2c-b3d1-b3c4dfbfbfde/playlist.m3u8';

// Fetch the link to playlist.m3u8 of the video you want to play
export const VideoPlayer = ({ thumbnail, qualities }) => {
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
    // plugins: {
    //   videoJsResolutionSwitcher: {
    //     default: 480,
    //     dynamicLabel: true,
    //     ui:true
    //   }
    // },
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
    sources:qualities.map(x => {
      return {
        src: x.path,
        label: `${x.label}p`,
        type: 'application/x-mpegURL',
        res: x.label
      }
    }).reverse()
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
        console.log(options.sources)
        player.on('resolutionchange', function () {
          console.log(player);
          console.log(player.currentResolution());
   
        });
        // player.updateSrc(qualities.map(x => {
        //   return {
        //     src: x.path,
        //     label: `${x.label}p`,
        //     type: 'application/x-mpegURL',
        //     res: x.label
        //   }
        // }).reverse())
      });

    } else {
      const player = playerRef.current;

      player.autoplay(options.autoplay);
      player.src(options.sources);
    }
  }, [options, videoRef, qualities]);

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

// export class VideoPlayer extends React.Component {

//   constructor(props) {
//     super(this.props);
//     // ({ thumbnail, videoUrl, qualities }) =>
//     this.videoRef = React.createRef();
//     this.player = null;
//     this.state = {
//       thumbnail: props.thumbnail,
//       videoUrl: props.videoUrl,
//       qualities: props.qualities,
//       options :{
//         autoplay: false,
//         controls: true,
//         playbackRates: [0.5, 1, 1.5, 2],
//         height: 400,
//         responsive: true,
//         poster: props.thumbnail,
//         controlBar: {
//           playToggle: true,
//           volumePanel: {
//             inline: false
//           },
//           skipButtons: {
//             forward: 10,
//             backward: 10
//           },
//           html5: {
//             nativeAudioTracks: true,
//             nativeVideoTracks: true,
//             nativeTextTracks: true
//           },
//           fullscreenToggle: true
//         },
//         sources: props.qualities.map(x => {
//           return {
//             src: x.path,
//             label: `${x.label}p`,
//             type: 'video/mp4'//'application/x-mpegURL'
//           }
//         }),
//       }
//     }
//   }

//   componentDidMount() {
//     this.player = videojs(this.videoRef.current, this.state.options);
//     this.player.qualitySelector();
//   }

//   render() {
//     return (
//         <div data-vjs-player>
//             <video ref={this.videoRef} className="video-js vjs-default-skin"/>
//         </div>
//     );
// }
// }
