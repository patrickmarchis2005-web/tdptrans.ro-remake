import { getApiBase } from './config';
import { buildAuthHeaders, getClientKey } from '../utils/session';

const parseError = async (response) => {
  const text = await response.text();
  return text || 'A aparut o eroare de comunicare cu serverul.';
};

const executeRequest = async (path, payload) => {
  try {
    const response = await fetch(`${getApiBase()}${path}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Client-Key': getClientKey(),
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      throw new Error(await parseError(response));
    }

    return response.json();
  } catch (error) {
    if (error instanceof TypeError) {
      throw new Error('Backend-ul nu poate fi contactat. Verifica daca backend-ul este pornit local pe portul 7092 sau daca masina virtuala Ubuntu este pornita, apoi reporneste frontend-ul.');
    }

    throw error;
  }
};

export const loginUser = async (credentials) => {
  return executeRequest('/auth/login', credentials);
};

export const signupUser = async (payload) => {
  return executeRequest('/auth/signup', payload);
};

export const requestCredentialChangeCode = async (payload) => {
  return executeRequest('/auth/request-credential-change-code', payload);
};

export const recoverPassword = async (payload) => {
  return executeRequest('/auth/recover-password', payload);
};

export const logoutUser = async () => {
  try {
    await fetch(`${getApiBase()}/auth/logout`, {
      method: 'POST',
      headers: buildAuthHeaders(),
    });
  } catch {
    // Best effort only; local session cleanup still happens on the client.
  }
};
