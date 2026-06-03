# TdpTrans Frontend

The frontend talks to the backend through bearer-token authentication and supports the assignment security flow:

- login with password and 6-digit security code
- self-signup with password and security code
- password and security-code reset through the on-screen 6-digit confirmation code
- before a credential reset is applied, the user must request an on-screen 6-digit confirmation code and type it back into the update form
- automatic logout after inactivity

API target resolution:

- during `npm run dev` and `npm run preview`, Vite serves the frontend over HTTPS and proxies `/api` and `/ws/chat` to the backend
- `VITE_API_ORIGIN` is used first when it is defined
- without `VITE_API_ORIGIN`, development falls back to `https://localhost:7092`
- without `VITE_API_ORIGIN`, production assumes the backend is available on the same origin under `/api` and `/ws/chat`
- VMware guest auto-detection is available through `npm run dev:vmware` or `VITE_USE_VMWARE_API=true`
- if the VM is stored outside `Documents\Virtual Machines`, set `VITE_VMWARE_VMX_PATH` explicitly
- if the backend HTTPS port differs from `7092`, set `VITE_VMWARE_BACKEND_PORT`

HTTPS notes:

- for LAN access from other devices, run `npm run setup:https-lan` once on the Windows host
- that script creates a local root CA plus a frontend server certificate in `certificates/lan-https`
- trust `certificates/lan-https/tdptrans-lan-root.cer` on every client device that opens the site
- if the Windows host changes its LAN IP, the frontend dev server now regenerates the LAN certificate automatically the next time `npm run dev -- --host` starts

Examples:

```powershell
$env:VITE_API_ORIGIN="https://192.168.1.25:7092"
npm run dev -- --host
```

```powershell
npm run dev:vmware
```

Because `vite.config.js` enables `host: true`, the dev server is reachable from other machines in the same LAN.

Recommended secure LAN flow:

1. Run `npm run setup:https-lan` in `tdpTrans-app-frontend`.
2. Import `certificates/lan-https/tdptrans-lan-root.cer` into the trusted root store on every client device.
3. Start the backend with its `https` launch profile so it listens on port `7092`.
4. Start the frontend on Windows with `npm run dev -- --host`.
5. Open one of the HTTPS URLs listed by the setup script or from `certificates/lan-https/tdptrans-frontend-lan-hosts.json`.

The frontend now defaults to the backend HTTPS endpoint on port `7092`. If you intentionally need the old unsecured backend flow for a local-only setup, set `VITE_API_ORIGIN=http://<host>:5152` explicitly.

Recommended free-cloud deployment flow:

1. Deploy the backend separately to Render.
2. Set `VITE_API_ORIGIN=https://<your-render-service>.onrender.com` in Vercel.
3. Deploy the frontend project to Vercel with `tdpTrans-app-frontend` as the project root.
4. The included `vercel.json` keeps React Router deep links working on Vercel.
5. In Render, set `CORS_ALLOWED_ORIGINS=https://<your-vercel-project>.vercel.app` or your custom frontend domain.

Recommended VMware backend flow:

1. Start the backend inside the Ubuntu VMware guest on HTTPS port `7092`.
2. In `tdpTrans-app-frontend`, run `npm run setup:https-lan` once on the Windows host.
3. Start the frontend with `npm run dev:vmware`.
4. If the frontend cannot detect the VMware guest automatically, create a local `.env.local` with `VITE_VMWARE_VMX_PATH=<absolute-path-to-your-vmx>`.
5. Open the HTTPS frontend URL printed by Vite.

Playwright suspicious-activity flow:

1. Start the backend and frontend first. The Playwright suite talks to the running HTTPS frontend and uses the proxied `/api` routes.
2. Install the Playwright runner and browser once:
   `npm install`
   `npx playwright install chromium`
3. Run the suite with:
   `npm run test:e2e`
4. If you only want to validate discovery and test registration, use:
   `npm run test:e2e:list`

Optional Playwright environment variables:

- `PLAYWRIGHT_FRONTEND_URL`
  Default: `https://localhost:5173`
- `PLAYWRIGHT_ADMIN_EMAIL`
  Default: `admin@tdptrans.ro`
- `PLAYWRIGHT_ADMIN_PASSWORD`
  Default: `12345678`
- `PLAYWRIGHT_ADMIN_SECURITY_CODE`
  Default: `246810`
- `PLAYWRIGHT_ADMIN_AUTHENTICATION_PHRASE`
  Default: `TDP-ADMIN`

The Playwright suite currently covers:

- repeated failed logins
- repeated permission denials against admin endpoints
- chat burst / flood behavior

It does not simulate the multi-IP session-drift rule from a single machine, because the backend uses the real remote IP address recorded for each session.
