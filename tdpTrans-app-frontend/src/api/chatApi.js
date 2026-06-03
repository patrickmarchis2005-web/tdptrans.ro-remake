import { getApiBase } from './config';
import { buildAuthHeaders, handleUnauthorizedSession } from '../utils/session';

const parseChatError = async (response, fallbackMessage) => {
  const text = await response.text();
  throw new Error(text || fallbackMessage);
};

export const fetchChatContacts = async () => {
  const response = await fetch(`${getApiBase()}/chat/contacts`, {
    headers: buildAuthHeaders(),
  });

  if (handleUnauthorizedSession(response)) {
    throw new Error('Sesiunea a expirat. Autentifica-te din nou.');
  }

  if (!response.ok) {
    await parseChatError(response, 'Lista conversatiilor nu a putut fi incarcata.');
  }

  return response.json();
};

export const fetchChatHistory = async (withUserId, take = 40) => {
  const response = await fetch(`${getApiBase()}/chat/history?withUserId=${withUserId}&take=${take}`, {
    headers: buildAuthHeaders(),
  });

  if (handleUnauthorizedSession(response)) {
    throw new Error('Sesiunea a expirat. Autentifica-te din nou.');
  }

  if (!response.ok) {
    await parseChatError(response, 'Istoricul conversatiei nu a putut fi incarcat.');
  }

  return response.json();
};

export const sendChatMessage = async (recipientUserId, message) => {
  const response = await fetch(`${getApiBase()}/chat/messages`, {
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

  if (handleUnauthorizedSession(response)) {
    throw new Error('Sesiunea a expirat. Autentifica-te din nou.');
  }

  if (!response.ok) {
    await parseChatError(response, 'Mesajul nu a putut fi trimis.');
  }

  return response.json();
};
