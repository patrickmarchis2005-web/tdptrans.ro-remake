import { beforeEach, describe, expect, it, vi } from 'vitest';

const { buildAuthHeadersMock, getApiBaseMock, getClientKeyMock } = vi.hoisted(() => ({
  getApiBaseMock: vi.fn(() => 'https://api.example.test/api'),
  buildAuthHeadersMock: vi.fn(() => ({ Authorization: 'Bearer existing-session' })),
  getClientKeyMock: vi.fn(() => 'client-key-123'),
}));

vi.mock('./config', () => ({
  getApiBase: getApiBaseMock,
}));

vi.mock('../utils/session', () => ({
  buildAuthHeaders: buildAuthHeadersMock,
  getClientKey: getClientKeyMock,
}));

import { loginUser, logoutUser, recoverPassword, requestCredentialChangeCode, signupUser } from './authApi';

const createResponse = ({ ok = true, body, text = '' } = {}) => ({
  ok,
  json: vi.fn().mockResolvedValue(body),
  text: vi.fn().mockResolvedValue(text),
});

describe('authApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('fetch', vi.fn());
  });

  it('sends login requests to the HTTPS auth endpoint with the client key header', async () => {
    const payload = {
      email: 'admin@tdptrans.ro',
      password: 'parola_sigura',
      securityCode: '246810',
      authenticationPhrase: 'TDP-ADMIN',
    };

    fetch.mockResolvedValue(createResponse({ body: { id: 1, accessToken: 'abc' } }));

    await loginUser(payload);

    expect(getClientKeyMock).toHaveBeenCalledOnce();
    expect(fetch).toHaveBeenCalledWith('https://api.example.test/api/auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Client-Key': 'client-key-123',
      },
      body: JSON.stringify(payload),
    });
  });

  it('sends signup requests to the HTTPS auth endpoint with the expected payload', async () => {
    const payload = {
      fullName: 'Utilizator Nou',
      email: 'nou@tdptrans.ro',
      password: 'parola_sigura',
      securityCode: '112233',
      authenticationPhrase: 'FRAZA-NOUA',
    };

    fetch.mockResolvedValue(createResponse({ body: { id: 10, accessToken: 'signup-token' } }));

    await signupUser(payload);

    expect(fetch).toHaveBeenCalledWith('https://api.example.test/api/auth/signup', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Client-Key': 'client-key-123',
      },
      body: JSON.stringify(payload),
    });
  });

  it('surfaces backend login errors from the response body', async () => {
    fetch.mockResolvedValue(createResponse({ ok: false, text: 'Email sau parola sunt incorecte.' }));

    await expect(loginUser({
      email: 'missing@tdptrans.ro',
      password: 'parola_sigura',
      securityCode: '246810',
      authenticationPhrase: 'TDP-USER',
    })).rejects.toThrow('Email sau parola sunt incorecte.');
  });

  it('maps network errors to the user-facing connectivity message', async () => {
    fetch.mockRejectedValue(new TypeError('fetch failed'));

    await expect(signupUser({
      fullName: 'Utilizator Nou',
      email: 'nou@tdptrans.ro',
      password: 'parola_sigura',
      securityCode: '112233',
      authenticationPhrase: 'FRAZA-NOUA',
    })).rejects.toThrow('Backend-ul nu poate fi contactat. Verifica daca backend-ul este pornit local pe portul 7092 sau daca masina virtuala Ubuntu este pornita, apoi reporneste frontend-ul.');
  });

  it('sends credential update requests with the expected payload', async () => {
    const payload = {
      email: 'recover@tdptrans.ro',
      credentialChangeCode: '482915',
      newPassword: 'parola_noua',
      newSecurityCode: '112233',
      newAuthenticationPhrase: 'PHRASE-RESET',
    };

    fetch.mockResolvedValue(createResponse({ body: { id: 5, accessToken: 'recovery-token' } }));

    await recoverPassword(payload);

    expect(fetch).toHaveBeenCalledWith('https://api.example.test/api/auth/recover-password', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Client-Key': 'client-key-123',
      },
      body: JSON.stringify(payload),
    });
  });

  it('requests credential-change codes from the HTTPS auth endpoint with the client key header', async () => {
    const payload = {
      email: 'recover@tdptrans.ro',
    };

    fetch.mockResolvedValue(createResponse({ body: { credentialChangeCode: '482915' } }));

    await requestCredentialChangeCode(payload);

    expect(fetch).toHaveBeenCalledWith('https://api.example.test/api/auth/request-credential-change-code', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Client-Key': 'client-key-123',
      },
      body: JSON.stringify(payload),
    });
  });

  it('uses the stored bearer token for logout and ignores transport failures', async () => {
    fetch.mockRejectedValue(new Error('socket closed'));

    await expect(logoutUser()).resolves.toBeUndefined();

    expect(buildAuthHeadersMock).toHaveBeenCalledOnce();
    expect(fetch).toHaveBeenCalledWith('https://api.example.test/api/auth/logout', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer existing-session',
      },
    });
  });
});
