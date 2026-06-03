import { expect } from '@playwright/test';

const storageKey = 'tdptransport_user';
const loginPath = '/login';
const signupPath = '/signup';
const homePath = '/acasa';
const observationsPath = '/api/admin/observations';

export const adminCredentials = {
  email: process.env.PLAYWRIGHT_ADMIN_EMAIL ?? 'admin@tdptrans.ro',
  password: process.env.PLAYWRIGHT_ADMIN_PASSWORD ?? '12345678',
  securityCode: process.env.PLAYWRIGHT_ADMIN_SECURITY_CODE ?? '246810',
  authenticationPhrase: process.env.PLAYWRIGHT_ADMIN_AUTHENTICATION_PHRASE ?? 'TDP-ADMIN',
};

export const createUniqueUser = (scenarioName) => {
  const slug = scenarioName
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 32);
  const uniqueSuffix = `${Date.now()}-${Math.random().toString(16).slice(2, 8)}`;

  return {
    fullName: `Playwright ${scenarioName}`.trim(),
    email: `pw-${slug}-${uniqueSuffix}@tdptrans.test`,
    password: 'Playwright!234',
    securityCode: '654321',
    authenticationPhrase: `PW-${slug}-${uniqueSuffix}`.slice(0, 32).toUpperCase(),
  };
};

export const loginThroughUi = async (page, credentials = adminCredentials) => {
  await page.goto(loginPath);
  await page.locator('input[placeholder="Email"]').fill(credentials.email);
  await page.locator('input[placeholder="Parola"]').fill(credentials.password);
  await page.locator('input[placeholder="Cod de securitate (6 cifre)"]').fill(credentials.securityCode);
  await page.locator('input[placeholder="Fraza de autentificare"]').fill(credentials.authenticationPhrase);
  await page.getByRole('button', { name: 'Login' }).click();
  await page.waitForURL(`**${homePath}`);
};

export const signupThroughUi = async (page, account) => {
  await page.goto(signupPath);
  await page.locator('input[placeholder="Nume complet"]').fill(account.fullName);
  await page.locator('input[placeholder="Email"]').fill(account.email);
  await page.locator('input[placeholder="Parola"]').fill(account.password);
  await page.locator('input[placeholder="Confirma parola"]').fill(account.password);
  await page.locator('input[placeholder="Cod de securitate (6 cifre)"]').fill(account.securityCode);
  await page.locator('input[placeholder="Confirma codul de securitate"]').fill(account.securityCode);
  await page.locator('input[placeholder="Fraza de autentificare"]').fill(account.authenticationPhrase);
  await page.getByRole('button', { name: 'Signup' }).click();
  await page.waitForURL(`**${homePath}`);
};

export const performFailedLogins = async (page, account, attempts = 3) => {
  for (let attempt = 0; attempt < attempts; attempt++) {
    await page.goto(loginPath);
    await page.locator('input[placeholder="Email"]').fill(account.email);
    await page.locator('input[placeholder="Parola"]').fill(`${account.password}-gresit`);
    await page.locator('input[placeholder="Cod de securitate (6 cifre)"]').fill(account.securityCode);
    await page.locator('input[placeholder="Fraza de autentificare"]').fill(account.authenticationPhrase);
    await page.getByRole('button', { name: 'Login' }).click();
    await expect(page.getByText('Email sau parola sunt incorecte.')).toBeVisible();
  }
};

export const fetchAuthorizedJson = async (page, path, init = {}) => {
  return page.evaluate(
    async ({ storageKeyValue, pathValue, initValue }) => {
      const rawUser = sessionStorage.getItem(storageKeyValue);
      const user = rawUser ? JSON.parse(rawUser) : null;
      const headers = new Headers(initValue.headers ?? {});

      if (user?.accessToken) {
        headers.set('Authorization', `Bearer ${user.accessToken}`);
      }

      if (initValue.body && !headers.has('Content-Type')) {
        headers.set('Content-Type', 'application/json');
      }

      const response = await fetch(pathValue, {
        ...initValue,
        headers: Object.fromEntries(headers.entries()),
      });

      const text = await response.text();
      let data = null;

      if (text) {
        try {
          data = JSON.parse(text);
        } catch {
          data = text;
        }
      }

      return {
        status: response.status,
        data,
      };
    },
    {
      storageKeyValue: storageKey,
      pathValue: path,
      initValue: init,
    });
};

export const fetchActiveObservations = async (adminPage) => {
  const response = await fetchAuthorizedJson(adminPage, observationsPath);
  expect(response.status, 'Admin observations endpoint should stay reachable during verification.').toBe(200);
  return Array.isArray(response.data) ? response.data : [];
};

export const waitForObservation = async (adminPage, userEmail, reason) => {
  await expect
    .poll(async () => {
      const observations = await fetchActiveObservations(adminPage);
      return observations.find((observation) =>
        observation.email?.toLowerCase() === userEmail.toLowerCase() &&
        observation.reason === reason);
    }, {
      message: `Expected an active observation for ${userEmail} with reason "${reason}".`,
      timeout: 20_000,
      intervals: [500, 1_000, 1_500],
    })
    .not.toBeNull();
};
