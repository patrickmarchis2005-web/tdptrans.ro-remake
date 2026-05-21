import { API_BASE } from './config';
import { buildAuthHeaders } from '../utils/session';

const BASE_URL = `${API_BASE}/missions`;

const parseError = async (response, fallbackMessage) => {
  const text = await response.text();
  return text || fallbackMessage;
};

export const fetchMissions = async (page = 1, pageSize = 5, searchTerm = '') => {
  try {
    let url = `${BASE_URL}?page=${page}&pageSize=${pageSize}`;

    if (searchTerm) {
      url += `&search=${encodeURIComponent(searchTerm)}`;
    }

    const response = await fetch(url, {
      headers: buildAuthHeaders(),
    });

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
    const response = await fetch(`${BASE_URL}/statistics`, {
      headers: buildAuthHeaders(),
    });

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
  const response = await fetch(BASE_URL, {
    method: 'POST',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json' }),
    body: JSON.stringify(missionData),
  });

  if (!response.ok) {
    throw new Error(await parseError(response, `Eroare la crearea comenzii: ${response.status}`));
  }

  return response.json();
};

export const updateMission = async (id, missionData) => {
  const response = await fetch(`${BASE_URL}/${id}`, {
    method: 'PUT',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json' }),
    body: JSON.stringify(missionData),
  });

  if (!response.ok) {
    throw new Error(await parseError(response, 'Eroare la actualizarea comenzii.'));
  }

  return response.json();
};

export const deleteMission = async (id) => {
  try {
    const response = await fetch(`${BASE_URL}/${id}`, {
      method: 'DELETE',
      headers: buildAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error(await parseError(response, 'Eroare la stergerea comenzii.'));
    }

    return true;
  } catch (error) {
    console.error(error);
    return false;
  }
};
