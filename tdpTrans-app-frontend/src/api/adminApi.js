import { API_BASE } from './config';
import { buildAuthHeaders } from '../utils/session';

const parseError = async (response) => {
  const text = await response.text();
  return text || 'Cererea nu a putut fi procesata.';
};

export const fetchObservations = async () => {
  const response = await fetch(`${API_BASE}/admin/observations`, {
    headers: buildAuthHeaders(),
  });

  if (!response.ok) {
    throw new Error(await parseError(response));
  }

  return response.json();
};

export const fetchActivityLogs = async (take = 24) => {
  const response = await fetch(`${API_BASE}/admin/activity-logs?take=${take}`, {
    headers: buildAuthHeaders(),
  });

  if (!response.ok) {
    throw new Error(await parseError(response));
  }

  return response.json();
};
