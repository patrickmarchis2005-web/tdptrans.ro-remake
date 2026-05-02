const BASE_URL = 'http://localhost:5152/api/missions';

// 1. GET: aduc comenzile (cu paginare)
export const fetchMissions = async (page = 1, limit = 5, searchTerm = '') => {
    try {
        let url = `${BASE_URL}?page=${page}&limit=${limit}`;
        
        if (searchTerm) {
            url += `&search=${encodeURIComponent(searchTerm)}`;
        }

        const response = await fetch(url);
        if (!response.ok) throw new Error('Eroare la aducerea comenzilor');
        return await response.json(); 
    } catch (error) {
        console.error(error);
        return null;
    }
};

// 2. GET: aduc statisticile pentru grafice
export const fetchStatistics = async () => {
    try {
        const response = await fetch(`${BASE_URL}/statistics`);
        if (!response.ok) throw new Error('Eroare la aducerea statisticilor');
        return await response.json();
    } catch (error) {
        console.error(error);
        return null;
    }
};

// 3. POST: adaug o comanda noua
export const createMission = async (missionData) => {
    try {
        const response = await fetch(BASE_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(missionData)
        });
        if (!response.ok) throw new Error('Eroare la crearea comenzii');
        return await response.json();
    } catch (error) {
        console.error(error);
        throw error;
    }
};

// 4. PUT: modific o comanda existenta
export const updateMission = async (id, missionData) => {
    try {
        const response = await fetch(`${BASE_URL}/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(missionData)
        });
        if (!response.ok) throw new Error('Eroare la actualizarea comenzii');
        return await response.json();
    } catch (error) {
        console.error(error);
        throw error;
    }
};

// 5. DELETE: sterg o comanda
export const deleteMission = async (id) => {
    try {
        const response = await fetch(`${BASE_URL}/${id}`, {
            method: 'DELETE'
        });
        if (!response.ok) throw new Error('Eroare la ștergerea comenzii');
        return true;
    } catch (error) {
        console.error(error);
        return false;
    }
};