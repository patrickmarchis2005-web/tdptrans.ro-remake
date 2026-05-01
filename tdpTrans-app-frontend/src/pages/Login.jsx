import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { authSchema } from '../utils';
import styles from './styles/Auth.module.css';


const Login = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({email: '', password: ''});
  const [error, setError] = useState('');

  const handleLogin = (e) => {
    e.preventDefault();
    
    const result = authSchema.safeParse(formData);

    if (!result.success) {
        setError(result.error.issues[0].message);
        return;
    }

    setError('');

    if (result.data.email === 'admin@tdptrans.ro' && result.data.password === '12345678') {
      localStorage.setItem('esteLogat', 'da');
      localStorage.setItem('rol', 'admin');
      console.log("Logat ca ADMIN cu: ", result.data);
      window.location.href = '/acasa';
    } 
    else if (result.data.email === 'sofer@tdptrans.ro' && result.data.password === '12345678') {
      localStorage.setItem('esteLogat', 'da');
      localStorage.setItem('rol', 'user');
      console.log("Logat ca SOFER cu: ", result.data);
      window.location.href = '/acasa';
    }
    else {
      setError("Email sau parolă incorecte!");
    }
  };

  return (
    <div className={styles.authContainer}>
      <h1>TDP TRANS</h1>
      <div className={styles.card}>
        <h2>Login</h2>
        {error && <p style={{color: 'red'}}>{error}</p>}
        <form onSubmit={handleLogin}>
          <input
            type="email" 
            placeholder="Email"
            onChange={(e) => setFormData({...formData, email: e.target.value})} />
          <input
            type="password"
            placeholder="Parola" 
            onChange={(e) => setFormData({...formData, password: e.target.value})} />
          <button type="submit">Login</button>
        </form>
        <p onClick={() => navigate('/signup')}>Nu ai cont? Înregistrează-te!</p>
      </div>
    </div>
  );
};

export default Login;