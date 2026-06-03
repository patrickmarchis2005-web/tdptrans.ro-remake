# TdpTrans Backend

The backend uses a relational database for both application data and chat persistence.

- Local default database: `TdpTrans.Controllers/App_Data/tdptrans.db` (SQLite)
- Cloud-ready database options: any PostgreSQL connection string, including Neon
- Default HTTP endpoint: `http://<host>:5152`
- Default HTTPS endpoint: `https://<host>:7092`
- Health check endpoint: `/health`

Connection-string behavior:

- if `ConnectionStrings:DefaultConnection` or `DATABASE_URL` contains a PostgreSQL-style string, the backend uses PostgreSQL
- if no external connection string is supplied, the backend falls back to the local SQLite file above
- PostgreSQL URLs such as Neon `postgresql://...?...sslmode=require...` are accepted directly

Run the backend from `tdpTrans-app-backend` with:

```powershell
dotnet run --launch-profile https --project TdpTrans.Controllers
```

On first start, the backend creates the local SQLite database file automatically. If you need a fresh local database, delete `TdpTrans.Controllers/App_Data/tdptrans.db` and start the backend again.

The launch profiles bind to `0.0.0.0`, so the API can be reached from another machine in the same LAN. The recommended default is the `https` profile on `https://<server-ip>:7092`. The plain HTTP profile on `http://<server-ip>:5152` should be treated as a local fallback only.

When you access the development HTTPS endpoint through an IP address, the browser may warn that the certificate is not trusted for that host. That is expected for a local development certificate; trust the certificate locally or configure your own LAN certificate if you need a clean HTTPS browser flow.

Authentication notes:

- login now requires password, 6-digit security code, and authentication phrase
- credential recovery now requires a short-lived 6-digit confirmation code generated on screen before the new credentials can be submitted
- the credential update flow changes the password, security code, and authentication phrase
- when an older SQLite database is upgraded, the bootstrap step fills in missing security codes and prints the generated defaults to the backend console

Free-cloud deployment notes:

- Render: use the root `render.yaml` blueprint and `tdpTrans-app-backend/Dockerfile`
- Neon: set either `ConnectionStrings__DefaultConnection` or `DATABASE_URL` to the Neon PostgreSQL connection string
- CORS: set `CORS_ALLOWED_ORIGINS` to the frontend origin, for example `https://your-frontend.vercel.app`
- HTTPS on Render is terminated by Render, so the container itself should keep listening on plain HTTP through the `PORT` environment variable
- Existing LiteDB chat history is not migrated automatically; once this version runs, chat history is stored in the relational database instead
