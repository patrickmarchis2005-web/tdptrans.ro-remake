import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { signupUser } from '../api/authApi';
import { signupSchema } from '../utils';
import { storeUser } from '../utils/session';
import styles from './styles/Auth.module.css';

const Signup = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({ fullName: '', email: '', password: '', confirmPassword: '' });
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSignup = async (event) => {
    event.preventDefault();

    const result = signupSchema.safeParse({
      email: formData.email,
      password: formData.password,
      confirmPassword: formData.confirmPassword,
    });

    if (!result.success) {
      setError(result.error.issues[0].message);
      return;
    }

    if (formData.fullName.trim().length < 3) {
      setError('Numele complet trebuie sa aiba minim 3 caractere.');
      return;
    }

    try {
      setIsSubmitting(true);
      setError('');
      const authenticatedUser = await signupUser({
        fullName: formData.fullName.trim(),
        email: formData.email.trim(),
        password: formData.password,
      });
      storeUser(authenticatedUser);
      window.location.href = '/acasa';
    } catch (signupError) {
      setError(signupError.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className={styles.authContainer}>
      <h1>TDP TRANSPORT</h1>
      <div className={styles.card}>
        <h2>Signup</h2>
        {error && <p style={{ color: 'red' }}>{error}</p>}
        <form onSubmit={handleSignup}>
          <input
            type="text"
            placeholder="Nume complet"
            autoComplete="name"
            onChange={(event) => setFormData({ ...formData, fullName: event.target.value })}
          />
          <input
            type="email"
            placeholder="Email"
            autoComplete="email"
            onChange={(event) => setFormData({ ...formData, email: event.target.value })}
          />
          <input
            type="password"
            placeholder="Parola"
            autoComplete="new-password"
            onChange={(event) => setFormData({ ...formData, password: event.target.value })}
          />
          <input
            type="password"
            placeholder="Confirma parola"
            autoComplete="new-password"
            onChange={(event) => setFormData({ ...formData, confirmPassword: event.target.value })}
          />
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Se creeaza contul...' : 'Signup'}
          </button>
        </form>
        <p className={styles.switchLink} onClick={() => navigate('/login')}>Ai deja cont? Autentifica-te!</p>
      </div>
    </div>
  );
};

export default Signup;
