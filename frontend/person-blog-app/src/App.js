import logo from './logo.svg';
import './App.css';

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <video controls
          width="620">
          <source src="http://localhost:7892/profile/Blog/video/chunks?postId=42c113cc-b4a7-41b5-b0c8-2e059087124f" />
        </video>
      </header>
    </div>
  );
}

export default App;
