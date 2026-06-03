import { getApiBase } from './config';
import { buildAuthHeaders, handleUnauthorizedSession } from '../utils/session';

const parseError = async (response) => {
  const text = await response.text();
  return text || 'Cererea nu a putut fi procesata.';
};

const executeAuthorizedRequest = async (path, options = {}) => {
  const response = await fetch(`${getApiBase()}${path}`, {
    ...options,
    headers: buildAuthHeaders(options.headers),
  });

  if (handleUnauthorizedSession(response)) {
    throw new Error('Sesiunea a expirat. Autentifica-te din nou.');
  }

  if (!response.ok) {
    throw new Error(await parseError(response));
  }

  return response.status === 204 ? null : response.json();
};

export const fetchActivityLogs = async (take = 24) => {
  return executeAuthorizedRequest(`/admin/activity-logs?take=${take}`);
};

export const fetchObservations = async () => {
  return executeAuthorizedRequest('/admin/observations');
};

export const fetchSecurityStatistics = async (mode = 'optimized', lookbackHours = 24) => {
  return executeAuthorizedRequest(`/admin/security-statistics?mode=${encodeURIComponent(mode)}&lookbackHours=${lookbackHours}`);
};

export const seedSecurityLab = async (payload = {}) => {
  return executeAuthorizedRequest('/admin/security-seed', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(payload),
  });
};
