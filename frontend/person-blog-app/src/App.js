import logo from './logo.svg';
import './App.css';
import VideoComponent from './components/VideoComponent';

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <VideoComponent sourceUrl="http://localhost:7892/profile/Blog/video/chunks?postId=42c113cc-b4a7-41b5-b0c8-2e059087124f" />
      </header>
    </div>
  );
}

export default App;
