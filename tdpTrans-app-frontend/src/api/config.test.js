import { afterEach, describe, expect, it, vi } from 'vitest';

describe('config', () => {
  afterEach(() => {
    vi.resetModules();
    vi.unstubAllGlobals();
  });

  it('defaults direct frontend-to-backend calls to the HTTPS endpoint', async () => {
    vi.stubGlobal('window', {
      location: {
        hostname: 'secure-host',
        protocol: 'https:',
        host: 'secure-host:5173',
      },
    });

    const { getApiOrigin } = await import('./config');

    expect(getApiOrigin()).toBe('https://secure-host:7092');
  });

  it('falls back to secure localhost when there is no browser window', async () => {
    const { getApiOrigin } = await import('./config');

    expect(getApiOrigin()).toBe('https://localhost:7092');
  });

  it('uses the explicit VITE_API_ORIGIN when it is configured', async () => {
    vi.stubEnv('VITE_API_ORIGIN', 'https://tdptrans-api.onrender.com/');

    const { getApiOrigin } = await import('./config');

    expect(getApiOrigin()).toBe('https://tdptrans-api.onrender.com');
  });
});
