

function VideoComponent({sourceUrl}) {
    return (
      <div className="Video">
          <video controls
            width="620">
            <source src={sourceUrl} />
          </video>
      </div>
    );
  }
  
  export default VideoComponent;
  