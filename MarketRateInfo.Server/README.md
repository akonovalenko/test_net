# Market Rate Info Server

ASP.NET Core Web API with EF Core and SQLite.

## Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- Swagger / OpenAPI
- Serilog

## API

### Current rates

```http
GET /api/Home
```

### Available currency pairs

```http
GET /api/Rates/symbols
```

### Historical rates

```http
GET /api/Rates?symbol=EUR%2FUSD&from=2026-08-17&to=2026-08-24
```

The `from` and `to` dates are inclusive.

## Database

The SQLite database is stored in `App_Data/marketrateinfo.db`.

On a new database the initializer creates deterministic sample history for `EUR/USD` and `USD/UAH` for the previous 30 days so the period filter can be tested immediately.


## Authentication and roles

ASP.NET Core Identity is configured with cookie authentication and stored in the existing SQLite database.

Roles:
- `Admin` — can add/remove currency pairs and manually refresh rates.
- `User` — can view rates and use the application in read-only mode.

Authentication UI is implemented entirely in the Vue SPA:
- `/login`
- `/register`
- `/access-denied`

The ASP.NET Core server exposes cookie-based authentication endpoints:
- `POST /api/auth/login`
- `POST /api/auth/register`
- `POST /api/auth/logout`
- `GET /api/auth/me`

For development, `appsettings.Development.json` contains a default admin:
- Email: `admin@marketrateinfo.local`
- Password: `ChangeMe123!`

Change this password before using the application outside local development. For production, provide the admin password through user secrets/environment configuration instead of committing it to source control.

The database schema (application tables and ASP.NET Core Identity tables) is managed through EF Core migrations. On startup, `Database.MigrateAsync()` creates `App_Data/marketrateinfo.db` if it does not exist and applies pending migrations. Existing databases created by the former `EnsureCreated` initializer can be adopted by the initial migration because its schema creation uses `IF NOT EXISTS`; back up and verify an existing database before upgrading.

Create a new migration during development with `dotnet ef migrations add <MigrationName>` and commit the generated migration files. Do not use `EnsureCreated()` together with migrations.
