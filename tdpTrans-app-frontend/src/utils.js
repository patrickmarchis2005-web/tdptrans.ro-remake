import { z } from 'zod';

export const missionSchema = z.object({
  client: z.string().min(3, "Numele clientului trebuie sa aiba minim 3 caractere"),

  phone: z.coerce.string()
    .min(10, "Numarul de telefon e prea scurt")
    .regex(/^[0-9+ \-]+$/, "Numarul de telefon contine caractere invalide"),

  email: z.string().email("Format email invalid"),

  cost: z.coerce.number({
      invalid_type_error: "Costul trebuie sa fie un numar valid"
    })
    .positive("Costul trebuie sa fie un numar mai mare decat 0"),

  missionType: z.enum(["Transport", "Tractare"], {
    invalid_type_error: "Tipul comenzii trebuie sa fie 'Transport' sau 'Tractare'",
    required_error: "Tipul comenzii este obligatoriu"
  }),

  truckId: z.coerce.string().regex(/^[0-9]{6}$/, "ID-ul camionului trebuie sa contina fix 6 cifre!"),

  address: z.string().min(5, "Adresa este prea scurta"),

  date: z.string().min(1, "Data este obligatorie!"),

  missionStatus: z.enum(["Programata", "In_desfasurare", "Finalizata"], {
    invalid_type_error: "Statusul comenzii trebuie sa fie 'Programata', 'In_desfasurare' sau 'Finalizata'",
    required_error: "Statusul comenzii este obligatoriu"
  })
});

export const getPaginatedItems = (items, currentPage, itemsPerPage) => {
  const indexOfLastItem = currentPage * itemsPerPage;
  const indexOfFirstItem = indexOfLastItem - itemsPerPage;

  const currentItems = items.slice(indexOfFirstItem, indexOfLastItem);

  const totalPages = Math.ceil(items.length / itemsPerPage);

  return {
    currentItems,
    totalPages
  };
};

const authenticationPhraseSchema = z.string()
  .trim()
  .min(6, "Fraza de autentificare trebuie sa aiba intre 6 si 64 de caractere")
  .max(64, "Fraza de autentificare trebuie sa aiba intre 6 si 64 de caractere");

export const authSchema = z.object({
  email: z.string().trim().email("Format email invalid"),
  password: z.string().min(8, "Parola trebuie sa aiba minim 8 caractere"),
  securityCode: z.string().trim().regex(/^\d{6}$/, "Codul de securitate trebuie sa contina exact 6 cifre"),
  authenticationPhrase: authenticationPhraseSchema,
});

export const credentialChangeCodeRequestSchema = z.object({
  email: z.string().trim().email("Format email invalid"),
});

export const signupSchema = z.object({
  email: z.string().trim().email("Format email invalid"),
  password: z.string().min(8, "Parola trebuie sa aiba minim 8 caractere"),
  confirmPassword: z.string(),
  securityCode: z.string().trim().regex(/^\d{6}$/, "Codul de securitate trebuie sa contina exact 6 cifre"),
  confirmSecurityCode: z.string().trim(),
  authenticationPhrase: authenticationPhraseSchema,
}).refine((data) => data.password === data.confirmPassword, {
  message: "Parolele nu coincid",
  path: ["confirmPassword"],
}).refine((data) => data.securityCode === data.confirmSecurityCode, {
  message: "Codurile de securitate nu coincid",
  path: ["confirmSecurityCode"],
});

export const recoverySchema = z.object({
  email: z.string().trim().email("Format email invalid"),
  credentialChangeCode: z.string().trim().regex(/^\d{6}$/, "Codul de confirmare trebuie sa contina exact 6 cifre"),
  newPassword: z.string().min(8, "Parola trebuie sa aiba minim 8 caractere"),
  confirmNewPassword: z.string(),
  newSecurityCode: z.string().trim().regex(/^\d{6}$/, "Codul de securitate trebuie sa contina exact 6 cifre"),
  confirmNewSecurityCode: z.string().trim(),
  newAuthenticationPhrase: authenticationPhraseSchema,
}).refine((data) => data.newPassword === data.confirmNewPassword, {
  message: "Parolele nu coincid",
  path: ["confirmNewPassword"],
}).refine((data) => data.newSecurityCode === data.confirmNewSecurityCode, {
  message: "Codurile de securitate nu coincid",
  path: ["confirmNewSecurityCode"],
});
