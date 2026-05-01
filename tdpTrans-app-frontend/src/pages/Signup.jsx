import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { signupSchema } from '../utils';
import styles from './styles/Auth.module.css';


const Signup = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({email: '', password: '', confirmPassword: ''});
  const [error, setError] = useState('');

  const handleSignup = (e) => {
    e.preventDefault();
    
    const result = signupSchema.safeParse(formData);

    if (!result.success) {
      setError(result.error.issues[0].message);
      return;
    }

    console.log("Successful signup with: ", result.data);
    navigate('/acasa');
  };

  return (
    <div className={styles.authContainer}>
      <h1>TDP TRANS</h1>
      <div className={styles.card}>
        <h2>Signup</h2>
        {error && <p style={{ color: 'red' }}>{error}</p>}
        <form onSubmit={handleSignup}>
          <input 
            type="email" 
            placeholder="Email"
            onChange={(e) => setFormData({...formData, email: e.target.value})} />
          <input 
            type="password" 
            placeholder="Parola" 
            onChange={(e) => setFormData({...formData, password: e.target.value})} />
          <input 
            type="password" placeholder="Confirma parola" 
            onChange={(e) => setFormData({...formData, confirmPassword: e.target.value})} 
          />
          <button type="submit">Signup</button>
        </form>
        <p onClick={() => navigate('/login')}>Ai deja cont? Autentifica-te!</p>
      </div>
    </div>
  );
};

export default Signup;