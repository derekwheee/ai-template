# AI Agent Dev Container Template

A VS Code Dev Container with a full-stack scaffold ready to use: a .NET 10 + Aspire backend and a React + Vite frontend, wired together and pre-configured with auth, routing, and a component library.

> **Host vs container:** Commands marked 🖥️ run on your **host machine**. Commands marked 📦 run **inside the dev container** (i.e. in the VS Code integrated terminal after reopening in container).

## What's included

| Tool | Version | Notes |
|---|---|---|
| Git | Latest | Via devcontainer feature |
| .NET SDK | 10.0 | Via devcontainer feature |
| .NET Aspire | Latest workload | Installed in `post-create.sh` |
| Node.js | LTS | Via devcontainer feature (nvm-managed) |
| Vite / create-vite | Latest | Installed globally in `post-create.sh` |
| GitHub CLI (`gh`) | Latest | Via devcontainer feature |
| GitHub Copilot CLI | Latest | `copilot` command, installed in `post-create.sh` |

## Forwarded ports

| Port | Service |
|---|---|
| `18888` | Aspire Dashboard |
| `5000` | ASP.NET HTTP |
| `5001` | ASP.NET HTTPS |
| `5173` | Vite Dev Server |

## Setup

### 1. One-time host setup — Copilot CLI authentication 🖥️

Linux containers have no keychain, so the Copilot CLI authenticates via a `GH_TOKEN` environment variable forwarded from your host machine.

1. Go to https://github.com/settings/personal-access-tokens/new
2. Under **Permissions → Account permissions**, add **Copilot Requests** (read-only)
3. Generate the token and add it to your shell profile:

```bash
# ~/.zshrc or ~/.bashrc  (run on your host machine)
export GH_TOKEN=github_pat_...
```

4. Reload your shell: `source ~/.zshrc`

The token is forwarded automatically into the container — no `gh auth login` needed.

### 2. Open in container 🖥️

1. Open this folder in VS Code.
2. When prompted, click **Reopen in Container** (or run `Dev Containers: Reopen in Container` from the command palette).
3. Wait for the container to build and `post-create.sh` to finish (installs Aspire workload, Copilot CLI, and global npm tools).

### 3. Install frontend dependencies 📦

```bash
cd my-app
npm install
```

## Running the app

### Full stack via Aspire (recommended) 📦

```bash
cd MyApp
dotnet run --project MyApp.AppHost --launch-profile http
```

This starts both the API and the React dev server, and opens the Aspire dashboard at **http://localhost:18888**.

| Service | URL |
|---|---|
| Aspire Dashboard | http://localhost:18888 |
| React app | http://localhost:5173 |
| API | http://localhost:5000 |

The Vite dev server automatically proxies `/api/*` requests to the backend, so you never need to configure CORS manually in development.

### API only 📦

```bash
cd MyApp/MyApp.Api
dotnet run
```

### React dev server only 📦

```bash
cd my-app
npm run dev
```

> When running the React app standalone (without Aspire), it proxies `/api` to `http://localhost:5000` by default.

## CI

GitHub Actions runs automatically on every push and pull request to `main` (see `.github/workflows/ci.yml`):

| Job | Steps |
|-----|-------|
| **.NET build & test** | restore → build (Release) → test |
| **Frontend lint & build** | `npm ci` → lint → `npm run build` |

## Build, test & lint

```bash
# .NET — run inside the container 📦
cd MyApp
dotnet build
dotnet test                          # run all tests
dotnet test --filter "Name=MyTest"   # run a specific test

# React — run inside the container 📦
cd my-app
npm run build    # type-check + Vite production build
npm run lint
```

## Project structure

```
.devcontainer/
  devcontainer.json       # Container definition, VS Code extensions & settings
  post-create.sh          # Runs once after container creation
.github/
  copilot-instructions.md # Copilot context — architecture guide for AI agents
MyApp/                    # .NET 10 solution
  MyApp.AppHost/          # Aspire orchestrator — wires API + React npm app
  MyApp.ServiceDefaults/  # Shared Aspire observability config
  MyApp.Api/
    Program.cs            # Thin wiring — AddServiceDefaults, AddApplicationServices, MapAuthEndpoints
    Config/
      JwtSettings.cs      # Typed record for JWT config; registered as singleton in DI
    Extensions/
      ServiceCollectionExtensions.cs  # AddApplicationServices — EF Core, Identity, JWT, CORS
      WebApplicationExtensions.cs     # ApplyMigrations, UseApplicationMiddleware
    Routes/
      AuthRoutes.cs       # MapAuthEndpoints — register, login, me handlers + GenerateJwtToken
    Data/
      AppDbContext.cs      # IdentityDbContext<ApplicationUser> + SQLite
      ApplicationUser.cs  # Extends IdentityUser (add custom fields here)
    Migrations/           # EF Core migrations (auto-applied on startup in dev)
    appsettings.json      # JWT config, connection string, allowed origins
  MyApp.Tests/            # xUnit integration tests (WebApplicationFactory)
my-app/                   # React + Vite frontend
  src/
    main.tsx              # QueryClient + RouterProvider setup
    router.ts             # All routes (TanStack Router, code-based)
    lib/
      auth.ts             # Token storage, apiFetch wrapper, auth API calls
      utils.ts            # shadcn cn() helper
    routes/
      __root.tsx          # Root layout
      login.tsx           # /login — redirects to / if already authenticated
      register.tsx        # /register — registers then auto-logs in
      dashboard.tsx       # / — protected, redirects to /login if not authenticated
    components/ui/        # shadcn/ui components (do not edit directly)
```

## Architecture

### Backend (`MyApp/`)

- All API logic uses **Minimal APIs** — no controllers. Route handlers live in `Routes/`, wired up via `MapAuthEndpoints()` in `Program.cs`.
- Service registration is split into extension methods in `Extensions/` to keep `Program.cs` as a thin orchestrator.
- **SQLite** database (`MyApp.Api/app.db`) with **EF Core** + ASP.NET Core Identity.
- **JWT authentication**: tokens are issued on login and validated on protected endpoints.
- EF Core migrations are **applied automatically on startup** in the `Development` environment.
- JWT config (`Key`, `Issuer`, `Audience`, `ExpiryMinutes`) lives in `appsettings.json`. `Jwt:Key` is intentionally blank there — it's set in `appsettings.Development.json` for local dev. **Set it via an environment variable or secrets manager before deploying.**

### Frontend (`my-app/`)

- **TanStack Router** for type-safe, code-based routing.
- **TanStack Query** for server state and data fetching.
- **shadcn/ui** + **Tailwind CSS v4** for components and styling.
- **react-hook-form** + **zod** for form validation.
- API calls go through `lib/auth.ts`'s `apiFetch`, which automatically attaches the Bearer token from `localStorage`.
- Protected routes use `beforeLoad` with `isAuthenticated()` to redirect unauthenticated users.
- Path alias: `@/` maps to `src/` — use it for all non-relative imports.

## Auth flow

| Step | Endpoint | Notes |
|---|---|---|
| Register | `POST /api/auth/register` | Body: `{ username, password }` |
| Login | `POST /api/auth/login` | Returns `{ token, username }` |
| Get current user | `GET /api/auth/me` | Requires `Authorization: Bearer <token>` |

The token is stored in `localStorage` via `setToken()` in `lib/auth.ts`.

## Common development tasks

### Add a new API endpoint 📦

Create a handler in an existing file under `Routes/`, or add a new `Routes/SomethingRoutes.cs` with a `MapSomethingEndpoints()` extension method and call it from `Program.cs`. Group related endpoints with `app.MapGroup(...)`.

### Add a new database entity 📦

1. Add a `DbSet<T>` to `AppDbContext.cs`.
2. Run from `MyApp/MyApp.Api/`:

```bash
dotnet ef migrations add <MigrationName>
```

Migrations are applied automatically on next app startup.

### Add a new page 📦

1. Create a component in `my-app/src/routes/`.
2. Register the route in `my-app/src/router.ts`.

### Add a new shadcn component 📦

```bash
cd my-app
npx shadcn@latest add <component>
```
