import { z } from 'zod';

export const missionSchema = z.object({
  client: z.string().min(3, "Numele clientului trebuie sa aiba minim 3 caractere"),

  phone: z.coerce.string()
    .min(10, "Numarul de telefon e prea scurt")
    .regex(/^[0-9+ \-]+$/, "Numarul de telefon contine caractere invalide"),

  email: z.string().email("Format email invalid"),

  cost: z.coerce.number({
      invalid_type_error: "Costul trebuie să fie un număr valid"
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


export const authSchema = z.object({
  email: z.string().email("Format email invalid"),
  password: z.string().min(6, "Parola trebuie să aibă minim 6 caractere"),
});


export const signupSchema = z.object({
  email: z.string().email("Format email invalid"),
  password: z.string().min(6, "Parola trebuie să aibă minim 6 caractere"),
  confirmPassword: z.string()
}).refine((data) => data.password === data.confirmPassword, {
  message: "Parolele nu coincid",
  path: ["confirmPassword"],
});

