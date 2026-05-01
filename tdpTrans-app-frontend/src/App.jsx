import { BrowserRouter as Router, Routes, Route} from 'react-router-dom';
import Login from './pages/Login.jsx';
import Signup from './pages/Signup.jsx';
import Acasa from './pages/Acasa.jsx';
import Comenzi from './pages/Comenzi.jsx';


function App() {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/signup" element={<Signup />} />
        <Route path="/acasa" element={<Acasa />} />
        <Route path="/comenzi" element={<Comenzi />} />
        <Route path='/' element={<Login />} />
      </Routes>
    </Router>
  );
}

export default App;