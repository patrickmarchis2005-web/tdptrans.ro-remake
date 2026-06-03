import { getApiBase } from './config';
import { buildAuthHeaders, handleUnauthorizedSession } from '../utils/session';

const parseError = async (response, fallbackMessage) => {
  const text = await response.text();
  return text || fallbackMessage;
};

export const fetchMissions = async (page = 1, pageSize = 5, searchTerm = '') => {
  try {
    const baseUrl = `${getApiBase()}/missions`;
    let url = `${baseUrl}?page=${page}&pageSize=${pageSize}`;

    if (searchTerm) {
      url += `&search=${encodeURIComponent(searchTerm)}`;
    }

    const response = await fetch(url, {
      headers: buildAuthHeaders(),
    });

    if (handleUnauthorizedSession(response)) {
      throw new Error('Sesiunea a expirat. Autentifica-te din nou.');
    }

    if (!response.ok) {
      throw new Error(await parseError(response, 'Eroare la aducerea comenzilor.'));
    }

    return await response.json();
  } catch (error) {
    console.error(error);
    return null;
  }
};

export const fetchStatistics = async () => {
  try {
    const response = await fetch(`${getApiBase()}/missions/statistics`, {
      headers: buildAuthHeaders(),
    });

    if (handleUnauthorizedSession(response)) {
      throw new Error('Sesiunea a expirat. Autentifica-te din nou.');
    }

    if (!response.ok) {
      throw new Error(await parseError(response, 'Eroare la aducerea statisticilor.'));
    }

    return await response.json();
  } catch (error) {
    console.error(error);
    return null;
  }
};

export const createMission = async (missionData) => {
  const response = await fetch(`${getApiBase()}/missions`, {
    method: 'POST',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json' }),
    body: JSON.stringify(missionData),
  });

  if (handleUnauthorizedSession(response)) {
    throw new Error('Sesiunea a expirat. Autentifica-te din nou.');
  }

  if (!response.ok) {
    throw new Error(await parseError(response, `Eroare la crearea comenzii: ${response.status}`));
  }

  return response.json();
};

export const updateMission = async (id, missionData) => {
  const response = await fetch(`${getApiBase()}/missions/${id}`, {
    method: 'PUT',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json' }),
    body: JSON.stringify(missionData),
  });

  if (handleUnauthorizedSession(response)) {
    throw new Error('Sesiunea a expirat. Autentifica-te din nou.');
  }

  if (!response.ok) {
    throw new Error(await parseError(response, 'Eroare la actualizarea comenzii.'));
  }

  return response.json();
};

export const deleteMission = async (id) => {
  try {
    const response = await fetch(`${getApiBase()}/missions/${id}`, {
      method: 'DELETE',
      headers: buildAuthHeaders(),
    });

    if (handleUnauthorizedSession(response)) {
      throw new Error('Sesiunea a expirat. Autentifica-te din nou.');
    }

    if (!response.ok) {
      throw new Error(await parseError(response, 'Eroare la stergerea comenzii.'));
    }

    return true;
  } catch (error) {
    console.error(error);
    return false;
  }
};
