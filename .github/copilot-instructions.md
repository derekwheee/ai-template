# Copilot Instructions

## What this repo is

A VS Code Dev Container template with a full-stack scaffold:

- **`MyApp/`** — .NET 10 solution (Aspire orchestrator + REST API)
- **`my-app/`** — React + Vite frontend

## Running the app

### Full stack via Aspire (recommended)
```bash
cd MyApp
dotnet run --project MyApp.AppHost --launch-profile http
```
Opens the Aspire dashboard at `http://localhost:18888`. The React app runs at `http://localhost:5173`.

### API only
```bash
cd MyApp/MyApp.Api
dotnet run
```

### React dev server only
```bash
cd my-app
npm run dev
```

## Build, test, lint

```bash
# .NET
cd MyApp
dotnet build
dotnet test                          # run all tests
dotnet test --filter "Name=MyTest"   # run a single test

# React
cd my-app
npm run build    # type-check + Vite production build
npm run lint
```

## Architecture

### Backend (`MyApp/`)

```
MyApp.sln
MyApp.AppHost/      # Aspire orchestrator — wires Api + React npm app
MyApp.ServiceDefaults/  # shared Aspire observability config
MyApp.Api/
  Program.cs        # all service registration + minimal API endpoints
  Data/
    AppDbContext.cs      # IdentityDbContext<ApplicationUser> + SQLite
    ApplicationUser.cs   # extends IdentityUser (add custom fields here)
  Migrations/       # EF Core migrations (auto-applied on startup in dev)
```

- All API logic lives in `Program.cs` using Minimal APIs — no controllers.
- Migrations are applied automatically on startup in `Development` environment.
- The SQLite database file (`app.db`) is written to `MyApp.Api/` and is gitignored.
- JWT config (`Key`, `Issuer`, `Audience`, `ExpiryMinutes`) lives in `appsettings.json`. **Change `Jwt:Key` before deploying.**

### Frontend (`my-app/`)

```
src/
  main.tsx          # QueryClient + RouterProvider setup
  router.ts         # all routes defined here (code-based TanStack Router)
  lib/
    auth.ts         # token storage (localStorage), apiFetch wrapper, authApi
    utils.ts        # shadcn cn() helper
  routes/
    __root.tsx      # root layout (Outlet)
    login.tsx       # /login — redirects to / if already authenticated
    register.tsx    # /register — registers then auto-logs in
    dashboard.tsx   # / — protected, redirects to /login if not authenticated
  components/ui/    # shadcn components (do not edit directly)
```

- API calls go through `lib/auth.ts`'s `apiFetch`, which automatically attaches the Bearer token from localStorage.
- All routes are defined in `router.ts`. Add new routes there and create the page component in `routes/`.
- Protected routes use `beforeLoad` with `isAuthenticated()` to redirect.
- The Vite dev server proxies `/api/*` to the backend (URL injected by Aspire via `services__api__https__0` env var, fallback `http://localhost:5000`).

## Key conventions

- **New API endpoints**: add `app.MapGet/Post/...` in `Program.cs`. Group related endpoints with `app.MapGroup(...)`.
- **New EF entities**: add `DbSet<T>` to `AppDbContext`, then `dotnet ef migrations add <Name>` from `MyApp.Api/`.
- **New pages**: create component in `src/routes/`, add route in `src/router.ts`.
- **New shadcn components**: `npx shadcn@latest add <component>` from `my-app/`.
- **Path alias**: `@/` maps to `src/` — use it for all non-relative imports.
- **Forms**: use `react-hook-form` + `zod` schema validation (see `login.tsx` / `register.tsx` for the pattern).

## Auth flow

1. `POST /api/auth/register` — creates user (username + password)
2. `POST /api/auth/login` — returns `{ token, username }`
3. Token stored in `localStorage` via `setToken()` in `lib/auth.ts`
4. `GET /api/auth/me` — returns `{ id, username }`, requires `Authorization: Bearer <token>`

## Environment

| Tool | Details |
|---|---|
| .NET SDK | 10.0 with Aspire workload |
| Node.js | LTS (nvm-managed) |
| GitHub Copilot CLI | `copilot` command |

`GH_TOKEN` is forwarded from the host via `remoteEnv` — must be a fine-grained PAT with **Copilot Requests (read-only)** scope.

## Forwarded ports

| Port | Service |
|---|---|
| `18888` | Aspire Dashboard |
| `5000` | ASP.NET HTTP |
| `5001` | ASP.NET HTTPS |
| `5173` | Vite Dev Server |

