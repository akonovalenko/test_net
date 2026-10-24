# Market Rate Info Client

Vue 3 + TypeScript frontend for Market Rate Info.

## Stack

- Vue 3
- TypeScript
- Vite
- Pinia
- Axios
- PrimeVue 4
- Tailwind CSS 4

## Architecture

```text
HomeView
    -> Pinia (marketRate)
        -> API service (homeApi)
            -> Axios
                -> /api/*
```

## UI

The home page provides:

- currency-pair selection with PrimeVue Select;
- period selection with PrimeVue DatePicker;
- historical rates in PrimeVue DataTable;
- loading and API error states.

## Development

```bash
npm install
npm run dev
```

The Vite development server runs on `https://localhost:60028` and proxies `/api` to the ASP.NET Core server at `https://localhost:7000`.

## Validation

```bash
npm run typecheck
npm run build
```
