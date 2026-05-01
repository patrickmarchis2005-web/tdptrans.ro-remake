import { missionSchema } from '../utils.js';


export class MissionsStore {
  constructor() {
    this.missions = [
      { id: 1, type: 'Transport', truckId: '100400', date: '2026-01-01', cost: '100$', client: 'Ion Pop', phone: '0700-000 000', email: 'ion@tdptrans.ro', address: 'Str. Lalelelor 5, Cluj', status: 'In desfasurare' },
      { id: 2, type: 'Tractare', truckId: '365365', date: '2026-02-02', cost: '400$', client: 'Andrei Pop', phone: '0711-111 111', email: 'andrei@tdptrans.ro', address: 'Bvd. Eroilor 12, Turda', status: 'In desfasurare' },
      { id: 3, type: 'Transport', truckId: '100401', date: '2026-01-25', cost: '1000$', client: 'Bernard Albu', phone: '0785 604 876', email: 'bernarddd@tdptrans.ro', address: 'Bulevardul Antonia Georgescu 4, Cluj', status: 'Programata' },
      { id: 4, type: 'Tractare', truckId: '365366', date: '2024-03-04', cost: '300$', client: 'Haralambie Popescu', phone: '0268 759 382', email: 'harhalambie@tdptrans.ro', address: 'Bulevardul Nistor 31, Bistrita', status: 'Finalizata' },
      { id: 5, type: 'Transport', truckId: '100402', date: '2025-10-10', cost: '200$', client: 'Romanița Popa', phone: '0742 565 938', email: 'poparomanita@tdptrans.ro', address: 'Bulevardul Varvara Stănescu 17, Dej', status: 'Programata' },
      { id: 6, type: 'Tractare', truckId: '365367', date: '2026-09-17', cost: '250$', client: 'Niculiță Eftimie', phone: '0266 219 489', email: 'nicu123@tdptrans.ro', address: 'Soseaua Dima 108, Jucu', status: 'Finalizata' },
      { id: 7, type: 'Transport', truckId: '100401', date: '2026-06-27', cost: '700$', client: 'Otilia Dima', phone: '0748 411 578', email: 'enigmaotiliei@tdptrans.ro', address: 'Aleea Cristina Tabacu 3, Chinteni', status: 'In desfasurare' },
    ];
  }

  getAll() {
    return [...this.missions];
  }

  saveMission(formData, isAdding) {
    console.log("COMPLETE DATA:", JSON.stringify(formData, null, 2));
    const result = missionSchema.safeParse(formData);

    if (!result.success) {
      console.log("DETAILED ZOD ERRORS:", JSON.stringify(result.error.issues, null, 2));
      const errorMessage = result.error.issues[0]?.message;
      throw new Error(errorMessage);
    }

    if (isAdding) {
      const newId = this.missions.length > 0 ? Math.max(...this.missions.map(m => m.id)) + 1 : 1;
      const newMission = { ...result.data, id: newId, status: "Noua"};
      this.missions.push(newMission);
      return newMission;
    } else {
      this.missions = this.missions.map(m => m.id === formData.id ? { ...result.data, id: m.id } : m);
      return { ...result.data, id: formData.id };
    }
  }

  deleteMission(id) {
    this.missions = this.missions.filter(m => m.id !== id);
    return this.getAll();
  }
}

export const missionsStore = new MissionsStore(); // singleton for UI