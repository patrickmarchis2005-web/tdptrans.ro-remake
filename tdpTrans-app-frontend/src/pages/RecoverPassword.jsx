import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { recoverPassword, requestCredentialChangeCode } from '../api/authApi';
import { credentialChangeCodeRequestSchema, recoverySchema } from '../utils';
import { storeUser } from '../utils/session';
import styles from './styles/Auth.module.css';

const RecoverPassword = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    email: '',
    credentialChangeCode: '',
    newPassword: '',
    confirmNewPassword: '',
    newSecurityCode: '',
    confirmNewSecurityCode: '',
    newAuthenticationPhrase: '',
  });
  const [error, setError] = useState('');
  const [credentialChangeCodePreview, setCredentialChangeCodePreview] = useState('');
  const [codeRequestMessage, setCodeRequestMessage] = useState('');
  const [isRequestingCode, setIsRequestingCode] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleRequestCredentialChangeCode = async () => {
    const result = credentialChangeCodeRequestSchema.safeParse({
      email: formData.email,
    });

    if (!result.success) {
      setError(result.error.issues[0].message);
      return;
    }

    try {
      setError('');
      setCodeRequestMessage('');
      setIsRequestingCode(true);

      const response = await requestCredentialChangeCode({
        email: result.data.email,
      });

      setCredentialChangeCodePreview(response.credentialChangeCode);
      setCodeRequestMessage('Copiaza codul afisat si introdu-l in campul de confirmare pentru a continua schimbarea credentialelor.');
      setFormData((currentFormData) => ({
        ...currentFormData,
        email: result.data.email,
        credentialChangeCode: '',
      }));
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setIsRequestingCode(false);
    }
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    const result = recoverySchema.safeParse(formData);
    if (!result.success) {
      setError(result.error.issues[0].message);
      return;
    }

    try {
      setError('');
      setIsSubmitting(true);

      const authenticatedUser = await recoverPassword({
        email: result.data.email,
        credentialChangeCode: result.data.credentialChangeCode,
        newPassword: result.data.newPassword,
        newSecurityCode: result.data.newSecurityCode,
        newAuthenticationPhrase: result.data.newAuthenticationPhrase,
      });

      storeUser(authenticatedUser);
      window.location.href = '/acasa';
    } catch (recoveryError) {
      setError(recoveryError.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className={styles.authContainer}>
      <h1>TDP TRANSPORT</h1>
      <div className={styles.card}>
        <h2>Actualizare credentiale</h2>
        {error && <p style={{ color: 'red' }}>{error}</p>}
        <form onSubmit={handleSubmit}>
          <input
            type="email"
            placeholder="Email"
            autoComplete="email"
            onChange={(event) => {
              setFormData({ ...formData, email: event.target.value, credentialChangeCode: '' });
              setCredentialChangeCodePreview('');
              setCodeRequestMessage('');
            }}
          />
          <button type="button" disabled={isSubmitting || isRequestingCode} onClick={handleRequestCredentialChangeCode}>
            {isRequestingCode ? 'Se genereaza codul...' : 'Solicita codul de confirmare'}
          </button>
          {credentialChangeCodePreview && (
            <div style={{ marginBottom: '16px', padding: '12px', borderRadius: '12px', backgroundColor: '#f4f7fb', textAlign: 'left' }}>
              <p style={{ margin: '0 0 8px', fontWeight: 700 }}>Codul tau de confirmare</p>
              <div style={{ fontSize: '1.35rem', letterSpacing: '0.18em', fontWeight: 800 }}>
                {credentialChangeCodePreview}
              </div>
              {codeRequestMessage && <p style={{ margin: '8px 0 0', color: '#4f5d6e' }}>{codeRequestMessage}</p>}
            </div>
          )}
          <input
            type="password"
            inputMode="numeric"
            placeholder="Codul de confirmare afisat"
            autoComplete="one-time-code"
            maxLength={6}
            onChange={(event) => setFormData({ ...formData, credentialChangeCode: event.target.value })}
          />
          <input
            type="password"
            placeholder="Parola noua"
            autoComplete="new-password"
            onChange={(event) => setFormData({ ...formData, newPassword: event.target.value })}
          />
          <input
            type="password"
            placeholder="Confirma parola noua"
            autoComplete="new-password"
            onChange={(event) => setFormData({ ...formData, confirmNewPassword: event.target.value })}
          />
          <input
            type="password"
            inputMode="numeric"
            placeholder="Cod de securitate nou"
            autoComplete="one-time-code"
            maxLength={6}
            onChange={(event) => setFormData({ ...formData, newSecurityCode: event.target.value })}
          />
          <input
            type="password"
            inputMode="numeric"
            placeholder="Confirma codul de securitate nou"
            autoComplete="one-time-code"
            maxLength={6}
            onChange={(event) => setFormData({ ...formData, confirmNewSecurityCode: event.target.value })}
          />
          <input
            type="text"
            placeholder="Fraza de autentificare noua"
            autoComplete="off"
            onChange={(event) => setFormData({ ...formData, newAuthenticationPhrase: event.target.value })}
          />
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Se actualizeaza...' : 'Actualizeaza credentialele'}
          </button>
        </form>
        <p className={styles.switchLink} onClick={() => navigate('/login')}>Inapoi la login</p>
      </div>
    </div>
  );
};

export default RecoverPassword;
