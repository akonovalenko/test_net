# MarketRateInfo

MarketRateInfo is a full-stack market-rate dashboard built with **ASP.NET Core 10**, **C#**, **Entity Framework Core**, **SQLite**, **Vue 3**, **TypeScript**, and **Vite**. It provides currency-pair management, historical rate queries, chart/table views, cookie-based authentication, role-based administration, and a weather widget.

> **Repository snapshot note:** The supplied archive references `MarketRateInfo.Server.BusinessLogic` namespaces from controllers and startup code, but does not contain the corresponding `BusinessLogic` source directory. It also calls `UseCors("DefaultCorsPolicy")` without registering that named policy. Resolve these issues before expecting the supplied snapshot to build and run successfully. The .NET SDK was not available in the review environment, so a backend build/test result could not be verified.

## Features

- Current market-rate overview and historical rate queries by currency pair and date range.
- Rate history visualization and tabular display.
- Configurable external rate providers: Frankfurter, ExchangeRate-API, and CoinGecko.
- Background rate refresh service and manual refresh endpoint for administrators.
- Currency-pair management for administrators.
- ASP.NET Core Identity cookie authentication with `Admin` and `User` roles.
- User registration, login, logout, and current-user endpoints.
- Admin user management: list, create, update, and delete users.
- Weather widget in the Vue client.
- SQLite persistence, EF Core migrations, and xUnit/Vitest test projects.

## Technology stack

### Server

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 with SQLite
- ASP.NET Core Identity and cookie authentication
- Serilog console and rolling-file logging
- Swagger / OpenAPI in Development
- Akka packages are referenced by the server

### Client

- Vue 3 and TypeScript
- Vite
- Pinia for client state
- Axios for API calls
- PrimeVue 4 and PrimeIcons
- Tailwind CSS 4
- Chart.js
- Vitest, Vue Testing Library, and jsdom
- ESLint and Oxlint

## Repository structure

```text
MarketRateInfo/
├── MarketRateInfo.slnx
├── README.md
├── MarketRateInfo.Server/
│   ├── Controllers/
│   ├── DataAccess/
│   │   ├── Entities/
│   │   ├── Repositories/
│   │   ├── DatabaseInitializer.cs
│   │   ├── IdentitySeedService.cs
│   │   └── MarketRateDbContext.cs
│   ├── Migrations/
│   ├── Middleware/
│   ├── Akka/
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
├── MarketRateInfo.Server.Tests/
│   ├── HomeControllerTests.cs
│   └── MarketRateRepositoryTests.cs
└── marketrateinfo.client/
    ├── src/
    │   ├── api/
    │   ├── components/
    │   ├── stores/
    │   ├── views/
    │   └── router.ts
    ├── tests/
    ├── package.json
    └── vite.config.ts
```

The archive also contains local SQLite database files under `MarketRateInfo.Server/App_Data`. Treat these as local data, not as required source files; avoid committing databases that contain real or personal data.

## Prerequisites

- .NET 10 SDK
- Node.js **24.1 or later**, as declared in `marketrateinfo.client/package.json`
- npm

Check installed versions:

```bash
dotnet --version
node --version
npm --version
```

## Run locally

### 1. Restore .NET dependencies

From the repository root:

```bash
dotnet restore MarketRateInfo.slnx
```

### 2. Install client dependencies

```bash
cd marketrateinfo.client
npm ci
cd ..
```

### 3. Configure HTTPS development certificates

The Vite configuration expects the ASP.NET Core HTTPS development certificate files under the platform's ASP.NET HTTPS certificate directory. If they are not present, it attempts to export one using `dotnet dev-certs`.

On Windows, trust the development certificate if required:

```powershell
dotnet dev-certs https --trust
```

### 4. Start the application

The server project configures ASP.NET Core SPA Proxy to launch the Vite development server in the development workflow:

```bash
dotnet run --project MarketRateInfo.Server
```

The Vite development server is configured for `https://localhost:60028`. The Vite proxy forwards `/api` requests to `https://localhost:7000`; verify the server URL against the active launch profile if your local ports differ.

You can also run the client independently:

```bash
cd marketrateinfo.client
npm run dev
```

The provided archive should first be completed with the missing `BusinessLogic` sources and the CORS policy registration described in the review note above.

## API overview

The API uses cookie authentication. Most market-rate endpoints require an authenticated user; management and refresh endpoints require the `Admin` role.

| Method | Endpoint | Access | Purpose |
|---|---|---|---|
| `POST` | `/api/auth/register` | Anonymous | Register a user |
| `POST` | `/api/auth/login` | Anonymous | Sign in and issue an auth cookie |
| `POST` | `/api/auth/logout` | Authenticated | Sign out |
| `GET` | `/api/auth/me` | Authenticated | Get the current user and roles |
| `GET` | `/api/home` | Authenticated | Get the home dashboard data |
| `GET` | `/api/rates/symbols` | Authenticated | List available currency pairs |
| `GET` | `/api/rates?symbol=EUR%2FUSD&from=2026-08-17&to=2026-08-24` | Authenticated | Query historical rates; the supplied server README documents inclusive dates |
| `POST` | `/api/rates/symbols` | Admin | Add a currency pair and request a refresh for today |
| `DELETE` | `/api/rates/symbols?symbol=EUR%2FUSD` | Admin | Remove a currency pair |
| `POST` | `/api/rates/refresh` | Admin | Refresh rates, optionally filtered by symbol and date range |
| `GET` | `/api/admin/users` | Admin | List users and roles |
| `POST` | `/api/admin/users` | Admin | Create a user and assign a role |
| `PUT` | `/api/admin/users/{id}` | Admin | Update a user's email, optional password, and role |
| `DELETE` | `/api/admin/users/{id}` | Admin | Delete a user |
| `GET` | `/api/weatherforecast` | Authenticated, based on the controller attributes | Get the weather forecast endpoint used by the client |

Swagger is configured to be enabled only in the Development environment.

## Data and configuration

### SQLite

The server startup code creates `MarketRateInfo.Server/App_Data` and constructs a database path at:

```text
MarketRateInfo.Server/App_Data/marketrateinfo.db
```

It then applies EF Core migrations and ensures the default currency pairs exist. **In the supplied `Program.cs`, the database path is constructed directly rather than read from `ConnectionStrings:DefaultConnection`;** changing that configuration value alone will not redirect the database unless startup code is updated.

### External rate providers

Provider settings are under `MarketRates` in `appsettings.json`:

- `Frankfurter`: base URL, enabled flag, timeout
- `ExchangeRateApi`: base URL, enabled flag, timeout
- `CryptoRateApi`: base URL, enabled flag, timeout, optional API key

Provider registration order is documented in the server's dependency-injection code as the fallback order. Store any real API keys in user secrets or environment variables; do not commit secrets.

### Development administrator

The supplied `appsettings.Development.json` contains a development administrator configuration:

```text
Email:    admin@marketrateinfo.local
Password: ChangeMe123!
```

This credential is for local development only. Change it or remove it before any shared or production deployment. Production credentials should be supplied through a secure configuration source such as user secrets for local development or environment/secret-manager configuration for deployment.

## Frontend scripts

Run these commands from `marketrateinfo.client`:

```bash
npm run dev          # Start Vite development server
npm run build        # Create production client build
npm run preview      # Preview the production build
npm run typecheck    # Check TypeScript and Vue component types
npm run lint         # Run Oxlint and ESLint
npm test             # Run Vitest once
npm run test:watch   # Run Vitest in watch mode
npm run test:coverage # Run tests with coverage
```

## Backend tests

From the repository root:

```bash
dotnet test MarketRateInfo.Server.Tests/MarketRateInfo.Server.Tests.csproj
```

The included tests cover home-controller behavior and repository operations. The test project references the server project; backend test execution depends on all server source files and dependencies being present.

## Review priorities before deployment

1. **Restore missing source files and register CORS.** The supplied archive does not include the `BusinessLogic` implementation referenced by the server, and `Program.cs` uses a named CORS policy without an `AddCors` registration.
2. **Protect cookie-authenticated state-changing endpoints against CSRF.** Review antiforgery protection for registration, logout, and administrative POST/PUT/DELETE operations; `SameSite=Lax` is useful but should not be treated as the only CSRF control in every deployment topology.
3. **Remove the committed development admin password from any production configuration path.** Keep a development-only seed credential only for local development.
4. **Validate admin-user updates before making changes.** In `AdminUsersController.UpdateUser`, email/user-name updates happen before password confirmation is checked, so a validation failure can still leave part of the update persisted.
5. **Use asynchronous database queries and cancellation consistently.** `AdminUsersController.GetUsers` currently materializes the user list synchronously and retrieves roles one user at a time.
6. **Review database path and deployment behavior.** Startup currently hardcodes the SQLite path and performs migrations during application startup; decide whether that is appropriate for the intended deployment and multiple-instance scenarios.
7. **Add integration/security tests.** Cover auth cookies, role restrictions, CSRF protections, user-management edge cases, provider failures, and migration from an existing database.

## License

No license file was included in the supplied archive. Add a license if you intend to publish or distribute the project.
