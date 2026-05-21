# TdpTrans Backend

The backend uses a local SQLite database by default so it can run on both Windows and Ubuntu without requiring SQL Server LocalDB.

- Main relational database: `TdpTrans.Controllers/App_Data/tdptrans.db`
- Chat message store: `TdpTrans.Controllers/App_Data/chat-store.db`

Run the backend from `tdpTrans-app-backend` with:

```powershell
dotnet run --project TdpTrans.Controllers
```

On first start, the backend creates the SQLite database file automatically. If you need a fresh local database, delete `TdpTrans.Controllers/App_Data/tdptrans.db` and start the backend again.
