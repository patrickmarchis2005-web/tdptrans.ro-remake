import { API_BASE } from './config';
import { buildAuthHeaders } from '../utils/session';

const parseChatError = async (response, fallbackMessage) => {
  const text = await response.text();
  throw new Error(text || fallbackMessage);
};

export const fetchChatContacts = async () => {
  const response = await fetch(`${API_BASE}/chat/contacts`, {
    headers: buildAuthHeaders(),
  });

  if (!response.ok) {
    await parseChatError(response, 'Lista conversatiilor nu a putut fi incarcata.');
  }

  return response.json();
};

export const fetchChatHistory = async (withUserId, take = 40) => {
  const response = await fetch(`${API_BASE}/chat/history?withUserId=${withUserId}&take=${take}`, {
    headers: buildAuthHeaders(),
  });

  if (!response.ok) {
    await parseChatError(response, 'Istoricul conversatiei nu a putut fi incarcat.');
  }

  return response.json();
};

export const sendChatMessage = async (recipientUserId, message) => {
  const response = await fetch(`${API_BASE}/chat/messages`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...buildAuthHeaders(),
    },
    body: JSON.stringify({
      recipientUserId,
      message,
    }),
  });

  if (!response.ok) {
    await parseChatError(response, 'Mesajul nu a putut fi trimis.');
  }

  return response.json();
};
