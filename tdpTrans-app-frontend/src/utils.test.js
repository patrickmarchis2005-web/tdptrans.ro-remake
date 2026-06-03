import { describe, expect, it } from 'vitest';
import { authSchema, credentialChangeCodeRequestSchema, getPaginatedItems, missionSchema, recoverySchema, signupSchema } from './utils';

describe('Pagination Logic', () => {
  it('returns the expected slice and page count', () => {
    const mockData = [1, 2, 3, 4, 5];

    const page1 = getPaginatedItems(mockData, 1, 2);
    const page3 = getPaginatedItems(mockData, 3, 2);

    expect(page1.currentItems).toEqual([1, 2]);
    expect(page1.totalPages).toBe(3);
    expect(page3.currentItems).toEqual([5]);
  });
});

describe('Zod validation: authSchema', () => {
  it('accepts valid login credentials', () => {
    const result = authSchema.safeParse({
      email: 'admin@tdptrans.ro',
      password: 'parola_sigura',
      securityCode: '246810',
      authenticationPhrase: 'TDP-ADMIN',
    });

    expect(result.success).toBe(true);
  });

  it('rejects a security code that is not exactly 6 digits', () => {
    const result = authSchema.safeParse({
      email: 'admin@tdptrans.ro',
      password: 'parola_sigura',
      securityCode: '24A810',
      authenticationPhrase: 'TDP-ADMIN',
    });

    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toBe('Codul de securitate trebuie sa contina exact 6 cifre');
  });
});

describe('Validare Zod: signupSchema', () => {
  it('accepts matching passwords and security codes', () => {
    const result = signupSchema.safeParse({
      email: 'sofer@tdptrans.ro',
      password: 'parola_puternica',
      confirmPassword: 'parola_puternica',
      securityCode: '112233',
      confirmSecurityCode: '112233',
      authenticationPhrase: 'FRAZA-NOUA',
    });

    expect(result.success).toBe(true);
  });

  it('rejects mismatched security codes', () => {
    const result = signupSchema.safeParse({
      email: 'sofer@tdptrans.ro',
      password: 'parola_puternica',
      confirmPassword: 'parola_puternica',
      securityCode: '112233',
      confirmSecurityCode: '221133',
      authenticationPhrase: 'FRAZA-NOUA',
    });

    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toBe('Codurile de securitate nu coincid');
    expect(result.error.issues[0].path[0]).toBe('confirmSecurityCode');
  });
});

describe('Validare Zod: recoverySchema', () => {
  it('accepts matching credential-change values', () => {
    const result = recoverySchema.safeParse({
      email: 'recover@tdptrans.ro',
      credentialChangeCode: '482915',
      newPassword: 'parola_noua',
      confirmNewPassword: 'parola_noua',
      newSecurityCode: '112233',
      confirmNewSecurityCode: '112233',
      newAuthenticationPhrase: 'PHRASE-RESET',
    });

    expect(result.success).toBe(true);
  });

  it('rejects mismatched recovery security codes', () => {
    const result = recoverySchema.safeParse({
      email: 'recover@tdptrans.ro',
      credentialChangeCode: '482915',
      newPassword: 'parola_noua',
      confirmNewPassword: 'parola_noua',
      newSecurityCode: '112233',
      confirmNewSecurityCode: '445566',
      newAuthenticationPhrase: 'PHRASE-RESET',
    });

    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toBe('Codurile de securitate nu coincid');
    expect(result.error.issues[0].path[0]).toBe('confirmNewSecurityCode');
  });
});

describe('Validare Zod: credentialChangeCodeRequestSchema', () => {
  it('accepts a valid recovery email before requesting a confirmation code', () => {
    const result = credentialChangeCodeRequestSchema.safeParse({
      email: 'recover@tdptrans.ro',
    });

    expect(result.success).toBe(true);
  });
});

describe('Data Validation with Zod, for Missions', () => {
  const validMission = {
    client: 'Ion Popescu',
    phone: '0744 123-456',
    email: 'ionpopescu@tdptrans.ro',
    missionType: 'Transport',
    truckId: '192400',
    date: '2026-05-20',
    cost: 500,
    address: 'Strada Observatorului 72, Cluj',
    missionStatus: 'Programata',
  };

  it('accepts a valid mission', () => {
    const result = missionSchema.safeParse(validMission);
    expect(result.success).toBe(true);
  });

  it('rejects a short client name', () => {
    const result = missionSchema.safeParse({ ...validMission, client: 'Io' });
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toContain('minim 3 caractere');
  });

  it('rejects a phone number with invalid characters', () => {
    const result = missionSchema.safeParse({ ...validMission, phone: '0744#12345' });
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toBe('Numarul de telefon contine caractere invalide');
  });

  it('accepts a cost supplied as a numeric string', () => {
    const result = missionSchema.safeParse({ ...validMission, cost: '1200' });
    expect(result.success).toBe(true);
  });

  it('rejects a negative cost', () => {
    const result = missionSchema.safeParse({ ...validMission, cost: '-1200' });
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toBe('Costul trebuie sa fie un numar mai mare decat 0');
  });

  it('rejects a non-numeric cost', () => {
    const result = missionSchema.safeParse({ ...validMission, cost: 'Not a number' });
    expect(result.success).toBe(false);
  });

  it('rejects an unsupported mission type', () => {
    const result = missionSchema.safeParse({ ...validMission, missionType: 'Asamblare' });
    expect(result.success).toBe(false);
  });

  it('rejects a truck id that is not 6 digits', () => {
    const result = missionSchema.safeParse({ ...validMission, truckId: '' });
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toBe('ID-ul camionului trebuie sa contina fix 6 cifre!');
  });

  it('rejects an address that is too short', () => {
    const result = missionSchema.safeParse({ ...validMission, address: '' });
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toBe('Adresa este prea scurta');
  });

  it('rejects an empty email address', () => {
    const result = missionSchema.safeParse({ ...validMission, email: '' });
    expect(result.success).toBe(false);
    expect(result.error.issues[0].message).toBe('Format email invalid');
  });
});
