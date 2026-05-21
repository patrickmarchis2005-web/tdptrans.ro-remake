const configuredApiOrigin = import.meta.env.VITE_API_ORIGIN?.trim()?.replace(/\/$/, '');

// export const API_ORIGIN = configuredApiOrigin || 'http://localhost:5152';
export const API_ORIGIN = configuredApiOrigin || 'http://172.30.251.117:5152';
export const API_BASE = `${API_ORIGIN}/api`;
export const CHAT_WS_URL = `${API_ORIGIN.replace(/^http/, 'ws')}/ws/chat`;
