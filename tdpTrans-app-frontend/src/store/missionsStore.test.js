import { describe, it, expect, beforeEach } from 'vitest';
import { missionsStore } from './missionsStore';

describe('OrdersStore Logic', () => {

  beforeEach(() => {
    missionsStore.missions = [
      { id: 1, client: 'Test Client', phone: '0700000000', email: 'johndoe@tdptrans.ro', type: 'Transport', truckId: '123', date: '2026-01-01', cost: '100$', address: 'Cluj', status: 'In desfasurare' }
    ];
  });


  // CREATE/ ADD
  it('should add a new valid mission', () => {
    const newMissionData = {
      client: 'Andrei Pop',
      phone: '0711222333',
      email: 'andreipop@tdptrans.ro',
      type: 'Tractare',
      truckId: '192168',
      date: '2026-05-05',
      cost: '500$',
      address: 'Turda',
      status: 'Noua'
    }

    const result = missionsStore.saveMission(newMissionData, true);

    expect(result.id).toBeDefined();
    expect(missionsStore.getAll().length).toBe(2);
    expect(missionsStore.getAll().find(m => m.client === 'Andrei Pop')).toBeTruthy();
  });


  it('should add a new valid mission in an empty list', () => {
    const newMissionData = {
      client: 'Andrei Pop',
      phone: '0711222333',
      email: 'andreipop@tdptrans.ro',
      type: 'Tractare',
      truckId: '192168',
      date: '2026-05-05',
      cost: '500$',
      address: 'Turda',
      status: 'Noua'
    }

    missionsStore.missions = [];
    const result = missionsStore.saveMission(newMissionData, true);

    expect(result.id).toBeDefined();
    expect(missionsStore.getAll().length).toBe(1);
    expect(missionsStore.getAll().find(m => m.client === 'Andrei Pop')).toBeTruthy();
  });


  it('should throw an error for invalid data (Zod validation)', () => {
    const invalidData = { client: 'Ab', phone: '123' };

    expect(() => missionsStore.saveMission(invalidData, true)).toThrow();
  });


  // READ/ GET
  it('should return all missions', () => {
    const missions = missionsStore.getAll();
    expect(missions.length).toBe(1);
    expect(missions[0].client).toBe('Test Client');
  });


  // UPDATE
  it('should update an existing mission', () => {
    const updatedData = { 
      id: 1, 
      client: 'Client Modificat', 
      phone: '0700000000', 
      email: 'andreipop@tdptrans.ro',
      type: 'Transport', 
      truckId: '123456', 
      date: '2026-01-01', 
      cost: '100$', 
      address: 'Cluj-Napoca', 
      status: 'Finalizata' 
    };

    missionsStore.saveMission(updatedData, false);
    
    expect(missionsStore.getAll().length).toBe(1);
    expect(missionsStore.getAll()[0].client).toBe('Client Modificat');
    expect(missionsStore.getAll()[0].status).toBe('Finalizata');
  });


  it('should NOT update the existing mission, because id is string', () => {
    const updatedData = { 
      id: "1", 
      client: 'Client Modificat', 
      phone: '0700000000', 
      email: 'andreipop@tdptrans.ro',
      type: 'Transport', 
      truckId: '123456', 
      date: '2026-01-01', 
      cost: '100$', 
      address: 'Cluj-Napoca', 
      status: 'Finalizata' 
    };

    missionsStore.saveMission(updatedData, false);
    
    expect(missionsStore.getAll().length).toBe(1);
    expect(missionsStore.getAll()[0].client).toBe('Test Client');
    expect(missionsStore.getAll()[0].status).toBe('In desfasurare');
  });


  // DELETE
  it('should delete a mission by ID', () => {
    missionsStore.deleteMission(1);
    expect(missionsStore.getAll().length).toBe(0);
  });


  it('should NOT delete a mission, with inexistent ID', () => {
    missionsStore.deleteMission(100);
    expect(missionsStore.getAll().length).toBe(1);
  });
});