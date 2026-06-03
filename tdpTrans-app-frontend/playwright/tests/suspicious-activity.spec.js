import { expect, test } from '@playwright/test';
import {
  adminCredentials,
  createUniqueUser,
  fetchActiveObservations,
  fetchAuthorizedJson,
  loginThroughUi,
  performFailedLogins,
  signupThroughUi,
  waitForObservation,
} from '../support/securityHarness';

test.describe.serial('Suspicious activity monitoring', () => {
  test('flags repeated failed logins for an existing account', async ({ browser }) => {
    const seededUserPage = await browser.newPage();
    const attackerPage = await browser.newPage();
    const adminPage = await browser.newPage();
    const user = createUniqueUser('failed-logins');

    await signupThroughUi(seededUserPage, user);
    await performFailedLogins(attackerPage, user, 3);
    await loginThroughUi(adminPage, adminCredentials);
    await waitForObservation(adminPage, user.email, 'Multiple failed logins');

    const observations = await fetchActiveObservations(adminPage);
    const matchedObservation = observations.find((observation) =>
      observation.email?.toLowerCase() === user.email.toLowerCase() &&
      observation.reason === 'Multiple failed logins');

    expect(matchedObservation?.details).toContain('tentative esuate');

    await Promise.all([
      seededUserPage.close(),
      attackerPage.close(),
      adminPage.close(),
    ]);
  });

  test('flags repeated permission denials for a standard user', async ({ browser }) => {
    const userPage = await browser.newPage();
    const adminPage = await browser.newPage();
    const user = createUniqueUser('permission-probe');

    await signupThroughUi(userPage, user);

    for (let attempt = 0; attempt < 3; attempt++) {
      const forbiddenResponse = await fetchAuthorizedJson(userPage, '/api/admin/observations');
      expect(forbiddenResponse.status).toBe(403);
    }

    await loginThroughUi(adminPage, adminCredentials);
    await waitForObservation(adminPage, user.email, 'Repeated permission denials');

    const observations = await fetchActiveObservations(adminPage);
    const matchedObservation = observations.find((observation) =>
      observation.email?.toLowerCase() === user.email.toLowerCase() &&
      observation.reason === 'Repeated permission denials');

    expect(matchedObservation?.details).toContain('accesari refuzate');

    await Promise.all([
      userPage.close(),
      adminPage.close(),
    ]);
  });

  test('flags a chat burst when a user floods the chat endpoint', async ({ browser }) => {
    const userPage = await browser.newPage();
    const adminPage = await browser.newPage();
    const user = createUniqueUser('chat-burst');

    await signupThroughUi(userPage, user);

    const contactsResponse = await fetchAuthorizedJson(userPage, '/api/chat/contacts');
    expect(contactsResponse.status).toBe(200);

    const adminContact = contactsResponse.data?.find((contact) => contact.roleName === 'admin') ?? contactsResponse.data?.[0];
    expect(adminContact, 'Chat burst test requires at least one available chat contact.').toBeTruthy();

    for (let index = 0; index < 20; index++) {
      const messageResponse = await fetchAuthorizedJson(userPage, '/api/chat/messages', {
        method: 'POST',
        body: JSON.stringify({
          recipientUserId: adminContact.id,
          message: `Playwright burst ${index + 1}`,
        }),
      });
      expect(messageResponse.status).toBe(200);
    }

    await loginThroughUi(adminPage, adminCredentials);
    await waitForObservation(adminPage, user.email, 'Chat burst pattern');

    const observations = await fetchActiveObservations(adminPage);
    const matchedObservation = observations.find((observation) =>
      observation.email?.toLowerCase() === user.email.toLowerCase() &&
      observation.reason === 'Chat burst pattern');

    expect(matchedObservation?.details).toContain('mesaje trimise');

    await Promise.all([
      userPage.close(),
      adminPage.close(),
    ]);
  });
});
