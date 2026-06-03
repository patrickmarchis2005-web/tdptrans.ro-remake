import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig } from '@playwright/test';

const frontendUrl = process.env.PLAYWRIGHT_FRONTEND_URL ?? 'https://localhost:5173';
const currentDirectory = path.dirname(fileURLToPath(import.meta.url));
const artifactsRoot = path.resolve(currentDirectory, '..', '.playwright-artifacts', 'tdpTrans-app-frontend');

export default defineConfig({
  testDir: './playwright/tests',
  fullyParallel: false,
  workers: 1,
  timeout: 90_000,
  expect: {
    timeout: 10_000,
  },
  reporter: [
    ['list'],
    ['html', { open: 'never', outputFolder: path.join(artifactsRoot, 'report') }],
  ],
  outputDir: path.join(artifactsRoot, 'results'),
  use: {
    baseURL: frontendUrl,
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: {
        browserName: 'chromium',
      },
    },
  ],
});
