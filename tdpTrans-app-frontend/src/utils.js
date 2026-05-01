import { z } from 'zod';


export const missionSchema = z.object({
  client: z.string().min(3, "Numele clientului trebuie sa aiba minim 3 caractere"),

  phone: z.string().regex(/^[0-9+ \-]+$/, "Numarul de telefon contine caractere invalide"),

  email: z.string().email("Format email invalid"),

  cost: z.string().refine(val => {9
    const num = parseFloat(val.replace('$', ''));
    return !isNaN(num) && num > 0;
  }, "Costul trebuie sa fie un numar pozitiv"),

  type: z.string().toLowerCase()
    .refine(val => ["transport", "tractare"].includes(val), "Tipul comenzii trebuie sa fie 'Transport' sau 'Tractare'"),

  truckId: z.string().regex(/^[0-9]{6}$/, "ID-ul camionului este obligatoriu"),
  
  address: z.string().min(5, "Adresa este prea scurta"),

  date: z.string(),

  status: z.string()
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

