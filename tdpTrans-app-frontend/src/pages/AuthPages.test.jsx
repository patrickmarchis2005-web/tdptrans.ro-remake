// @vitest-environment jsdom

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

const {
  loginUserMock,
  recoverPasswordMock,
  requestCredentialChangeCodeMock,
  signupUserMock,
  storeUserMock,
} = vi.hoisted(() => ({
  loginUserMock: vi.fn(),
  recoverPasswordMock: vi.fn(),
  requestCredentialChangeCodeMock: vi.fn(),
  signupUserMock: vi.fn(),
  storeUserMock: vi.fn(),
}));

vi.mock('../api/authApi', () => ({
  loginUser: loginUserMock,
  recoverPassword: recoverPasswordMock,
  requestCredentialChangeCode: requestCredentialChangeCodeMock,
  signupUser: signupUserMock,
}));

vi.mock('../utils/session', () => ({
  storeUser: storeUserMock,
}));

import Login from './Login';
import RecoverPassword from './RecoverPassword';
import Signup from './Signup';

const renderInRouter = (ui, initialEntries = ['/login']) => {
  return render(<MemoryRouter initialEntries={initialEntries}>{ui}</MemoryRouter>);
};

afterEach(() => {
  cleanup();
});

describe('Login page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows the current validation error when the authentication phrase is missing', async () => {
    const user = userEvent.setup();
    renderInRouter(<Login />);

    await user.type(screen.getByPlaceholderText('Email'), 'admin@tdptrans.ro');
    await user.type(screen.getByPlaceholderText('Parola'), 'parola_sigura');
    await user.type(screen.getByPlaceholderText('Cod de securitate (6 cifre)'), '246810');
    await user.click(screen.getByRole('button', { name: 'Login' }));

    expect(screen.getByText('Fraza de autentificare trebuie sa aiba intre 6 si 64 de caractere')).toBeTruthy();
    expect(loginUserMock).not.toHaveBeenCalled();
  });

  it('submits valid credentials to the auth API and enters the pending state', async () => {
    const user = userEvent.setup();
    loginUserMock.mockReturnValue(new Promise(() => {}));
    renderInRouter(<Login />);

    await user.type(screen.getByPlaceholderText('Email'), 'admin@tdptrans.ro');
    await user.type(screen.getByPlaceholderText('Parola'), 'parola_sigura');
    await user.type(screen.getByPlaceholderText('Cod de securitate (6 cifre)'), '246810');
    await user.type(screen.getByPlaceholderText('Fraza de autentificare'), 'TDP-ADMIN');
    await user.click(screen.getByRole('button', { name: 'Login' }));

    await waitFor(() => {
      expect(loginUserMock).toHaveBeenCalledWith({
        email: 'admin@tdptrans.ro',
        password: 'parola_sigura',
        securityCode: '246810',
        authenticationPhrase: 'TDP-ADMIN',
      });
    });

    expect(screen.getByRole('button', { name: 'Se autentifica...' }).disabled).toBe(true);
    expect(storeUserMock).not.toHaveBeenCalled();
  });
});

describe('Signup page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows the full-name validation error before calling the API', async () => {
    const user = userEvent.setup();
    renderInRouter(<Signup />, ['/signup']);

    await user.type(screen.getByPlaceholderText('Nume complet'), 'Io');
    await user.type(screen.getByPlaceholderText('Email'), 'sofer@tdptrans.ro');
    await user.type(screen.getByPlaceholderText('Parola'), 'parola_puternica');
    await user.type(screen.getByPlaceholderText('Confirma parola'), 'parola_puternica');
    await user.type(screen.getByPlaceholderText('Cod de securitate (6 cifre)'), '112233');
    await user.type(screen.getByPlaceholderText('Confirma codul de securitate'), '112233');
    await user.type(screen.getByPlaceholderText('Fraza de autentificare'), 'FRAZA-NOUA');
    await user.click(screen.getByRole('button', { name: 'Signup' }));

    expect(screen.getByText('Numele complet trebuie sa aiba minim 3 caractere.')).toBeTruthy();
    expect(signupUserMock).not.toHaveBeenCalled();
  });

  it('submits a trimmed signup payload and enters the pending state', async () => {
    const user = userEvent.setup();
    signupUserMock.mockReturnValue(new Promise(() => {}));
    renderInRouter(<Signup />, ['/signup']);

    await user.type(screen.getByPlaceholderText('Nume complet'), '  Utilizator Nou  ');
    await user.type(screen.getByPlaceholderText('Email'), '  nou@tdptrans.ro  ');
    await user.type(screen.getByPlaceholderText('Parola'), 'parola_puternica');
    await user.type(screen.getByPlaceholderText('Confirma parola'), 'parola_puternica');
    await user.type(screen.getByPlaceholderText('Cod de securitate (6 cifre)'), '112233');
    await user.type(screen.getByPlaceholderText('Confirma codul de securitate'), '112233');
    await user.type(screen.getByPlaceholderText('Fraza de autentificare'), '  FRAZA-NOUA  ');
    await user.click(screen.getByRole('button', { name: 'Signup' }));

    await waitFor(() => {
      expect(signupUserMock).toHaveBeenCalledWith({
        fullName: 'Utilizator Nou',
        email: 'nou@tdptrans.ro',
        password: 'parola_puternica',
        securityCode: '112233',
        authenticationPhrase: 'FRAZA-NOUA',
      });
    });

    expect(screen.getByRole('button', { name: 'Se creeaza contul...' }).disabled).toBe(true);
    expect(storeUserMock).not.toHaveBeenCalled();
  });
});

describe('RecoverPassword page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('requests and displays a credential-change confirmation code', async () => {
    const user = userEvent.setup();
    requestCredentialChangeCodeMock.mockResolvedValue({
      credentialChangeCode: '482915',
      expiresAtUtc: '2026-05-25T12:00:00Z',
    });
    renderInRouter(<RecoverPassword />, ['/recover-password']);

    await user.type(screen.getByPlaceholderText('Email'), '  recover@tdptrans.ro  ');
    await user.click(screen.getByRole('button', { name: 'Solicita codul de confirmare' }));

    await waitFor(() => {
      expect(requestCredentialChangeCodeMock).toHaveBeenCalledWith({
        email: 'recover@tdptrans.ro',
      });
    });

    expect(screen.getByText('Codul tau de confirmare')).toBeTruthy();
    expect(screen.getByText('482915')).toBeTruthy();
  });

  it('submits a trimmed credential update payload and enters the pending state', async () => {
    const user = userEvent.setup();
    requestCredentialChangeCodeMock.mockResolvedValue({
      credentialChangeCode: '482915',
      expiresAtUtc: '2026-05-25T12:00:00Z',
    });
    recoverPasswordMock.mockReturnValue(new Promise(() => {}));
    renderInRouter(<RecoverPassword />, ['/recover-password']);

    await user.type(screen.getByPlaceholderText('Email'), '  recover@tdptrans.ro  ');
    await user.click(screen.getByRole('button', { name: 'Solicita codul de confirmare' }));
    await waitFor(() => {
      expect(requestCredentialChangeCodeMock).toHaveBeenCalledWith({
        email: 'recover@tdptrans.ro',
      });
    });
    await user.type(screen.getByPlaceholderText('Codul de confirmare afisat'), '482915');
    await user.type(screen.getByPlaceholderText('Parola noua'), 'parola_noua');
    await user.type(screen.getByPlaceholderText('Confirma parola noua'), 'parola_noua');
    await user.type(screen.getByPlaceholderText('Cod de securitate nou'), '112233');
    await user.type(screen.getByPlaceholderText('Confirma codul de securitate nou'), '112233');
    await user.type(screen.getByPlaceholderText('Fraza de autentificare noua'), '  PHRASE-RESET  ');
    await user.click(screen.getByRole('button', { name: 'Actualizeaza credentialele' }));

    await waitFor(() => {
      expect(recoverPasswordMock).toHaveBeenCalledWith({
        email: 'recover@tdptrans.ro',
        credentialChangeCode: '482915',
        newPassword: 'parola_noua',
        newSecurityCode: '112233',
        newAuthenticationPhrase: 'PHRASE-RESET',
      });
    });

    expect(screen.getByRole('button', { name: 'Se actualizeaza...' }).disabled).toBe(true);
    expect(storeUserMock).not.toHaveBeenCalled();
  });
});
