import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Login from './pages/Login.jsx';
import Signup from './pages/Signup.jsx';
import Acasa from './pages/Acasa.jsx';
import Comenzi from './pages/Comenzi.jsx';
import Servicii from './pages/Servicii.jsx';
import CalculatorPret from './pages/CalculatorPret.jsx';
import Flota from './pages/Flota.jsx';
import Contact from './pages/Contact.jsx';
import ChatWidget from './components/ChatWidget.jsx';

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/signup" element={<Signup />} />
        <Route path="/acasa" element={<Acasa />} />
        <Route path="/servicii" element={<Servicii />} />
        <Route path="/calculator-pret" element={<CalculatorPret />} />
        <Route path="/flota" element={<Flota />} />
        <Route path="/contact" element={<Contact />} />
        <Route path="/comenzi" element={<Comenzi />} />
        <Route path="/" element={<Login />} />
      </Routes>
      <ChatWidget />
    </Router>
  );
}

export default App;
