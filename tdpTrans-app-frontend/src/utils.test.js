import { expect, it, describe } from 'vitest';
import { getPaginatedItems, missionSchema, authSchema, signupSchema } from './utils';


describe('Pagination Logic', () => {
  it('should return the correct data slices', () => {
    const mockData = [1, 2, 3, 4, 5];
    const pageSize = 2;

    const page1 = getPaginatedItems(mockData, 1, pageSize);
    expect(page1.currentItems).toEqual([1, 2]);
    expect(page1.totalPages).toBe(3);

    const page3 = getPaginatedItems(mockData, 3, pageSize);
    expect(page3.currentItems).toEqual([5]);
  });
});


describe('Zod validation: authSchema', () => {

  it('validate a correct account', () => {
    const validData = {
      email: 'admin@tdptrans.ro',
      password: 'parola_secreta'
    };
    const result = authSchema.safeParse(validData);
    expect(result.success).toBe(true);
  });

  it('should not validate data with invalid email', () => {
    const invalidEmailData = {
      email: 'admin_la_tdptrans.ro',
      password: 'parola_secreta'
    };
    const result = authSchema.safeParse(invalidEmailData);
    
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toBe("Format email invalid");
  });

  it('should not validate data with too short password', () => {
    const shortPasswordData = {
      email: 'admin@tdptrans.ro',
      password: '12345'
    };
    const result = authSchema.safeParse(shortPasswordData);
    
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toBe("Parola trebuie să aibă minim 6 caractere");
  });

});

describe('Validare Zod: signupSchema', () => {

  it('ar trebui să valideze când parolele coincid', () => {
    const validSignup = {
      email: 'sofer@tdptrans.ro',
      password: 'parola_puternica',
      confirmPassword: 'parola_puternica'
    };
    const result = signupSchema.safeParse(validSignup);
    expect(result.success).toBe(true);
  });

  it('ar trebui să pice când parolele nu coincid (refine)', () => {
    const mismatchedPasswords = {
      email: 'sofer@tdptrans.ro',
      password: 'parola_puternica',
      confirmPassword: 'alta_parola_gresita'
    };
    const result = signupSchema.safeParse(mismatchedPasswords);
    
    expect(result.success).toBe(false);
    // Verificăm dacă eroarea vine exact de la acel "refine" de pe confirmPassword
    expect(result.error.issues[0].message).toBe("Parolele nu coincid");
    expect(result.error.issues[0].path[0]).toBe("confirmPassword");
  });

});


describe('Data Validation with Zod, for Missions', () => {
  const validMission = {
    client: "Ion Popescu",
    phone: "0744 123-456",
    email: 'ionpopescu@tdptrans.ro',
    type: "Transport",
    truckId: "192400",
    date: "2026-05-20",
    cost: "500$",
    address: "Strada Observatorului 72, Cluj",
    status: "Programata"
  };


  it('should accept a new correct mission', () => {
    const result = missionSchema.safeParse(validMission);
    expect(result.success).toBe(true);
  });


  it('should reject the short name', () => {
    const wrongMission = { ...validMission, client: "Io" };
    const result = missionSchema.safeParse(wrongMission);
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toContain("minim 3 caractere");
  });


  it('should reject the wrong phone number', () => {
    const wrongMission = {...validMission, phone: "07@#!123" };
    const result = missionSchema.safeParse(wrongMission);
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toContain("Numarul de telefon contine caractere invalide");
  });


  it('should validate the cost', () => {
    const missionWithDollarAtCost = { ...validMission, cost: "1200$" };
    const result = missionSchema.safeParse(missionWithDollarAtCost);
    expect(result.success).toBe(true);
  });


  it('should reject the negative cost', () => {
    const missionWithDollarAtCost = { ...validMission, cost: "-1200" };
    const result = missionSchema.safeParse(missionWithDollarAtCost);
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toContain("Costul trebuie sa fie un numar pozitiv");
  });


  it('should reject the NaN cost', () => {
    const missionWithDollarAtCost = { ...validMission, cost: "Not a number" };
    const result = missionSchema.safeParse(missionWithDollarAtCost);
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toContain("Costul trebuie sa fie un numar pozitiv");
  });


  it('should reject an inexistent mission type', () => {
    const wrongMission = { ...validMission, type: "Asamblare" };
    const result = missionSchema.safeParse(wrongMission);
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toContain("Tipul comenzii trebuie sa fie 'Transport' sau 'Tractare'");
  });


  it('should reject mission with missing truckId', () => {
    const wrongMission = { ...validMission, truckId: "" };
    const result = missionSchema.safeParse(wrongMission);
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toContain("ID-ul camionului este obligatoriu");
  });


  it('should reject mission with too short address', () => {
    const wrongMission = { ...validMission, address: "" };
    const result = missionSchema.safeParse(wrongMission);
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toContain("Adresa este prea scurta");
  });


  it('should reject mission with empty string as email', () => {
    const wrongMission = { ...validMission, email: "" };
    const result = missionSchema.safeParse(wrongMission);
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toContain("Format email invalid");
  });
});