const configuredApiOrigin = import.meta.env.VITE_API_ORIGIN?.trim()?.replace(/\/$/, '');
const DEFAULT_HTTPS_API_PORT = 7092;

const normalizeApiOrigin = (value) => {
  const trimmedValue = value?.trim() ?? '';
  if (!trimmedValue) {
    return '';
  }

  const withProtocol = /^https?:\/\//i.test(trimmedValue)
    ? trimmedValue
    : `http://${trimmedValue}`;

  try {
    const url = new URL(withProtocol);
    if (url.protocol !== 'http:' && url.protocol !== 'https:') {
      throw new Error('Backend-ul trebuie sa foloseasca http:// sau https://.');
    }

    return url.origin;
  } catch {
    throw new Error('Adresa backend-ului este invalida. Exemplu valid: https://192.168.1.25:7092');
  }
};

const inferHttpsApiOrigin = () => {
  if (typeof window === 'undefined') {
    return `https://localhost:${DEFAULT_HTTPS_API_PORT}`;
  }

  const hostname = window.location.hostname || 'localhost';
  return `https://${hostname}:${DEFAULT_HTTPS_API_PORT}`;
};

const getCurrentOrigin = () => {
  if (typeof window === 'undefined') {
    return '';
  }

  return window.location.origin?.replace(/\/$/, '') ?? '';
};

export const getApiOrigin = () => {
  if (configuredApiOrigin) {
    return normalizeApiOrigin(configuredApiOrigin);
  }

  if (import.meta.env.DEV) {
    return normalizeApiOrigin(inferHttpsApiOrigin());
  }

  return normalizeApiOrigin(getCurrentOrigin() || inferHttpsApiOrigin());
};

const getRelativeChatWsUrl = () => {
  if (!import.meta.env.DEV || typeof window === 'undefined') {
    return '';
  }

  const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
  return `${protocol}//${window.location.host}/ws/chat`;
};

export const getApiBase = () => {
  if (import.meta.env.DEV) {
    return '/api';
  }

  return configuredApiOrigin
    ? `${getApiOrigin()}/api`
    : '/api';
};

export const getChatWsUrl = () => {
  if (import.meta.env.DEV) {
    return getRelativeChatWsUrl();
  }

  if (configuredApiOrigin) {
    return `${getApiOrigin().replace(/^http/i, 'ws')}/ws/chat`;
  }

  if (typeof window !== 'undefined') {
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    return `${protocol}//${window.location.host}/ws/chat`;
  }

  return `${getApiOrigin().replace(/^http/i, 'ws')}/ws/chat`;
};
