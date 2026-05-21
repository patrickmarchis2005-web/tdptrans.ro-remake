import { API_BASE } from './config';

const parseError = async (response) => {
  const text = await response.text();
  return text || 'A aparut o eroare de comunicare cu serverul.';
};

const executeRequest = async (path, payload) => {
  try {
    const response = await fetch(`${API_BASE}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      throw new Error(await parseError(response));
    }

    return response.json();
  } catch (error) {
    if (error instanceof TypeError) {
      throw new Error('Backend-ul nu poate fi contactat. Verifica daca serverul ruleaza pe http://localhost:5152 sau seteaza VITE_API_ORIGIN.');
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
