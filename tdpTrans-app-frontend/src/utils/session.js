const STORAGE_KEY = 'tdptransport_user';
const LEGACY_STORAGE_KEY = 'tdptrans_user';
const LOGIN_FLAG_KEY = 'esteLogat';
const ROLE_KEY = 'rol';
const CLIENT_KEY = 'tdptransport_client_key';
const LAST_ACTIVITY_KEY = 'tdptransport_last_activity';

const removeLegacyLocalState = () => {
  localStorage.removeItem(STORAGE_KEY);
  localStorage.removeItem(LEGACY_STORAGE_KEY);
  localStorage.removeItem(LOGIN_FLAG_KEY);
  localStorage.removeItem(ROLE_KEY);
};

const migrateLegacyUser = () => {
  const raw = localStorage.getItem(STORAGE_KEY) ?? localStorage.getItem(LEGACY_STORAGE_KEY);
  if (!raw) {
    return null;
  }

  try {
    const user = JSON.parse(raw);

    if (!user?.accessToken) {
      removeLegacyLocalState();
      return null;
    }

    storeUser(user);
    removeLegacyLocalState();
    return user;
  } catch {
    removeLegacyLocalState();
    return null;
  }
};

const generateClientKey = () => {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `chat-${Date.now()}-${Math.random().toString(16).slice(2)}`;
};

export const getStoredUser = () => {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (raw) {
      return JSON.parse(raw);
    }

    return migrateLegacyUser();
  } catch {
    return null;
  }
};

export const storeUser = (user) => {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(user));
  sessionStorage.setItem(LOGIN_FLAG_KEY, 'da');
  sessionStorage.setItem(ROLE_KEY, user.roleName === 'admin' ? 'admin' : 'user');
  markSessionActivity();
  removeLegacyLocalState();
};

export const clearStoredUser = () => {
  sessionStorage.removeItem(STORAGE_KEY);
  sessionStorage.removeItem(LOGIN_FLAG_KEY);
  sessionStorage.removeItem(ROLE_KEY);
  sessionStorage.removeItem(CLIENT_KEY);
  sessionStorage.removeItem(LAST_ACTIVITY_KEY);
  removeLegacyLocalState();
};

export const buildAuthHeaders = (headers = {}) => {
  const user = getStoredUser();

  if (!user?.accessToken) {
    return headers;
  }

  return {
    ...headers,
    Authorization: `Bearer ${user.accessToken}`,
  };
};

export const isAdminUser = (user = getStoredUser()) => user?.roleName === 'admin';

export const getClientKey = () => {
  const existingKey = sessionStorage.getItem(CLIENT_KEY);
  if (existingKey) {
    return existingKey;
  }

  const nextKey = generateClientKey();
  sessionStorage.setItem(CLIENT_KEY, nextKey);
  return nextKey;
};

export const getChatClientKey = getClientKey;

export const markSessionActivity = () => {
  sessionStorage.setItem(LAST_ACTIVITY_KEY, String(Date.now()));
};

export const getLastActivityAt = () => {
  const value = Number(sessionStorage.getItem(LAST_ACTIVITY_KEY));
  return Number.isFinite(value) && value > 0 ? value : Date.now();
};

export const handleUnauthorizedSession = (response) => {
  if (response.status !== 401) {
    return false;
  }

  clearStoredUser();

  if (typeof window !== 'undefined' && window.location.pathname !== '/login') {
    window.location.href = '/login';
  }

  return true;
};
