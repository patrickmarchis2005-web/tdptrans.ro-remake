const STORAGE_KEY = 'tdptransport_user';
const LEGACY_STORAGE_KEY = 'tdptrans_user';
const LOGIN_FLAG_KEY = 'esteLogat';
const ROLE_KEY = 'rol';
const CHAT_CLIENT_KEY = 'tdptransport_chat_client';

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
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(user));
    sessionStorage.setItem(LOGIN_FLAG_KEY, 'da');
    sessionStorage.setItem(ROLE_KEY, user.roleName === 'admin' ? 'admin' : 'user');
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
  removeLegacyLocalState();
};

export const clearStoredUser = () => {
  sessionStorage.removeItem(STORAGE_KEY);
  sessionStorage.removeItem(LOGIN_FLAG_KEY);
  sessionStorage.removeItem(ROLE_KEY);
  sessionStorage.removeItem(CHAT_CLIENT_KEY);
  removeLegacyLocalState();
};

export const buildAuthHeaders = (headers = {}) => {
  const user = getStoredUser();

  if (!user?.id) {
    return headers;
  }

  return {
    ...headers,
    'X-User-Id': String(user.id),
  };
};

export const isAdminUser = (user = getStoredUser()) => user?.roleName === 'admin';

export const getChatClientKey = () => {
  const existingKey = sessionStorage.getItem(CHAT_CLIENT_KEY);
  if (existingKey) {
    return existingKey;
  }

  const nextKey = generateClientKey();
  sessionStorage.setItem(CHAT_CLIENT_KEY, nextKey);
  return nextKey;
};
