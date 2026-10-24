# MarketRateInfo

MarketRateInfo is a full-stack application built with **ASP.NET Core 10**, **Vue 3**, **TypeScript**, **Entity Framework Core**, and **SQLite**.

The project provides a clean starting point for a market-rates application with a separate Data Access Layer, API, state management, UI components, and automated tests for both backend and frontend.

## Technology Stack

### Backend

* .NET 10 / ASP.NET Core Web API
* C#
* Entity Framework Core 10
* SQLite
* Dependency Injection
* OpenAPI
* ASP.NET Core SPA Proxy

### Data Access Layer

The backend uses a dedicated Data Access Layer:

```text
MarketRateInfo.Server/
└── DataAccess/
    ├── Entities/
    │   └── MarketRate.cs
    ├── Repositories/
    │   ├── IMarketRateRepository.cs
    │   └── MarketRateRepository.cs
    ├── MarketRateDbContext.cs
    └── DatabaseInitializer.cs
```

The repository layer isolates EF Core and SQLite access from controllers and application code.

SQLite is configured through `DefaultConnection` in `appsettings.json` and defaults to:

```text
App\_Data/marketrateinfo.db
```

The database is initialized automatically when the application starts and contains initial market-rate data for development.

### Frontend

* Vue 3
* TypeScript
* Vite
* Pinia
* PrimeVue
* PrimeVue Aura theme
* Tailwind CSS 4
* Vue Test Library
* Vitest
* jsdom

The frontend source is organized as:

```text
marketrateinfo.client/
└── src/
    ├── assets/
    ├── stores/
    │   └── app.ts
    ├── views/
    │   └── Home.vue
    ├── App.vue
    ├── main.ts
    └── vite-env.d.ts
```

## Solution Structure

```text
MarketRateInfo/
│
├── MarketRateInfo.sln
│
├── MarketRateInfo.Server/
│   ├── Controllers/
│   ├── DataAccess/
│   │   ├── Entities/
│   │   ├── Repositories/
│   │   ├── DatabaseInitializer.cs
│   │   └── MarketRateDbContext.cs
│   ├── App\_Data/
│   ├── Program.cs
│   └── appsettings.json
│
├── MarketRateInfo.Server.Tests/
│   ├── HomeControllerTests.cs
│   ├── MarketRateRepositoryTests.cs
│   └── MarketRateInfo.Server.Tests.csproj
│
└── marketrateinfo.client/
    ├── src/
    ├── tests/
    │   ├── Home.test.ts
    │   └── setup.ts
    ├── package.json
    ├── vite.config.ts
    ├── vitest.config.ts
    ├── tsconfig.json
    └── marketrateinfo.client.esproj
```

## Prerequisites

Install:

* .NET 10 SDK
* Node.js 22.18+ or Node.js 24.12+
* npm

Check the installed versions:

```bash
dotnet --version
node --version
npm --version
```

## Running the Application

### 1\. Restore backend dependencies

From the solution directory:

```bash
dotnet restore
```

### 2\. Install frontend dependencies

```bash
cd marketrateinfo.client
npm install
cd ..
```

### 3\. Start the application

The ASP.NET Core project is configured to use the Vue Vite development server through **SPA Proxy**.

```bash
dotnet run --project MarketRateInfo.Server
```

The application starts the frontend development server automatically in Visual Studio/ASP.NET Core development mode when SPA Proxy is enabled.

The Vite development server is configured for:

```text
https://localhost:60028
```

The API is exposed under `/api`.

## API

The initial API endpoint is:

```text
GET /api/home
```

The endpoint reads market-rate information through the repository/Data Access Layer and returns it to the Vue application.

The intended request flow is:

```text
Vue / Pinia
    │
    ▼
/api/home
    │
    ▼
HomeController
    │
    ▼
IMarketRateRepository
    │
    ▼
MarketRateRepository
    │
    ▼
EF Core
    │
    ▼
SQLite
```

## Frontend Development

Start Vite directly when frontend-only development is required:

```bash
cd marketrateinfo.client
npm run dev
```

Create a production build:

```bash
npm run build
```

Preview the production build:

```bash
npm run preview
```

## TypeScript

The frontend is fully configured for TypeScript.

Run the Vue/TypeScript type checker with:

```bash
cd marketrateinfo.client
npm run typecheck
```

The project uses `vue-tsc` so that types inside `.vue` single-file components are checked as well.

## Frontend Tests

Frontend tests use **Vitest + Vue Test Library + jsdom**.

Run all UI tests:

```bash
cd marketrateinfo.client
npm test
```

Run tests in watch mode:

```bash
npm run test:watch
```

Generate a coverage report:

```bash
npm run test:coverage
```

Current UI tests cover the main `Home.vue` behavior, including successful API loading and API error handling.

## Backend Unit Tests

Backend tests use **xUnit**.

Run all .NET tests:

```bash
dotnet test MarketRateInfo.Server.Tests/MarketRateInfo.Server.Tests.csproj
```

The repository tests use **SQLite in-memory** rather than EF Core's `InMemory` provider. This means repository tests execute against the SQLite provider used by the application while keeping the database isolated for every test.

Current backend tests cover:

* `MarketRateRepository.GetAllAsync`
* `MarketRateRepository.GetBySymbolAsync`
* handling an unknown symbol
* `HomeController` API behavior

## Run All Checks

A typical local verification sequence is:

```bash
# Backend
dotnet restore
dotnet build
dotnet test

# Frontend
cd marketrateinfo.client
npm install
npm run typecheck
npm run lint
npm test
npm run build
```

## Configuration

The backend uses the `DefaultConnection` connection string for SQLite.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App\_Data/marketrateinfo.db"
  }
}
```

For local development, the application can also fall back to the same SQLite path when the connection string is not configured.

## Development Notes

* Do not commit the generated SQLite database if it is intended to contain only local development data.
* Keep database access inside `DataAccess` rather than querying `DbContext` directly from controllers.
* Keep frontend API/state logic separate from UI components as the application grows.
* Use TypeScript types for API responses and Pinia state.
* Use repository tests for persistence behavior and Vue Test Library for user-visible UI behavior.



Автоматическое создание администратора в Development:



admin@marketrateinfo.local

ChangeMe123!

