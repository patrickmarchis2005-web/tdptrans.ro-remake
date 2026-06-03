import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { loginUser } from '../api/authApi';
import { authSchema } from '../utils';
import { storeUser } from '../utils/session';
import styles from './styles/Auth.module.css';

const Login = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [formData, setFormData] = useState({ email: '', password: '', securityCode: '', authenticationPhrase: '' });
  const [error, setError] = useState(location.state?.message ?? '');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleLogin = async (event) => {
    event.preventDefault();

    const result = authSchema.safeParse(formData);

    if (!result.success) {
      setError(result.error.issues[0].message);
      return;
    }

    try {
      setIsSubmitting(true);
      setError('');
      const authenticatedUser = await loginUser(result.data);
      storeUser(authenticatedUser);
      window.location.href = '/acasa';
    } catch (loginError) {
      setError(loginError.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className={styles.authContainer}>
      <h1>TDP TRANSPORT</h1>
      <div className={styles.card}>
        <h2>Login</h2>
        {error && <p style={{ color: 'red' }}>{error}</p>}
        <form onSubmit={handleLogin}>
          <input
            type="email"
            placeholder="Email"
            autoComplete="email"
            onChange={(event) => setFormData({ ...formData, email: event.target.value })}
          />
          <input
            type="password"
            placeholder="Parola"
            autoComplete="current-password"
            onChange={(event) => setFormData({ ...formData, password: event.target.value })}
          />
          <input
            type="password"
            inputMode="numeric"
            placeholder="Cod de securitate (6 cifre)"
            autoComplete="one-time-code"
            maxLength={6}
            onChange={(event) => setFormData({ ...formData, securityCode: event.target.value })}
          />
          <input
            type="text"
            placeholder="Fraza de autentificare"
            autoComplete="off"
            onChange={(event) => setFormData({ ...formData, authenticationPhrase: event.target.value })}
          />
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Se autentifica...' : 'Login'}
          </button>
        </form>
        <p className={styles.switchLink} onClick={() => navigate('/recover-password')}>Ai uitat parola sau codul de securitate?</p>
        <p className={styles.switchLink} onClick={() => navigate('/signup')}>Nu ai cont? Inregistreaza-te!</p>
      </div>
    </div>
  );
};

export default Login;
