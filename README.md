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
| OpenAI Codex CLI | Latest | `codex` command, installed in `post-create.sh` |
| Anthropic Claude Code | Latest | `claude` command, installed in `post-create.sh` |

## Forwarded ports

| Port | Service |
|---|---|
| `18888` | Aspire Dashboard |
| `5000` | ASP.NET HTTP |
| `5001` | ASP.NET HTTPS |
| `5173` | Vite Dev Server |

## Setup

### 1. One-time host setup — AI CLI authentication 🖥️

All three CLIs authenticate via environment variables forwarded from your host. Linux containers have no keychain, so credentials must live in your host shell profile. **Set up whichever CLI(s) you plan to use.**

Add the relevant exports to your `~/.zshrc` or `~/.bashrc`, then reload (`source ~/.zshrc`):

---

#### GitHub Copilot CLI (`copilot`)

Requires a GitHub fine-grained PAT with **Copilot Requests (read-only)** permission.

1. Go to https://github.com/settings/personal-access-tokens/new
2. Under **Permissions → Account permissions**, add **Copilot Requests** (read-only)
3. Add to your shell profile:

```bash
export GH_TOKEN=github_pat_...
```

---

#### OpenAI Codex CLI (`codex`)

Requires an OpenAI API key.

1. Go to https://platform.openai.com/api-keys and create a key
2. Add to your shell profile:

```bash
export OPENAI_API_KEY=sk-...
```

---

#### Anthropic Claude Code (`claude`)

Requires an Anthropic API key.

1. Go to https://console.anthropic.com/settings/keys and create a key
2. Add to your shell profile:

```bash
export ANTHROPIC_API_KEY=sk-ant-...
```

---

All configured variables are forwarded automatically into the container — no interactive login needed.

### 2. Open in container 🖥️

1. Open this folder in VS Code.
2. When prompted, click **Reopen in Container** (or run `Dev Containers: Reopen in Container` from the command palette).
3. Wait for the container to build and `post-create.sh` to finish (installs Aspire workload, all AI CLIs, and global npm tools).

> **⚠️ Watch for prompts during setup.** Some steps in `post-create.sh` (particularly the AI CLI installers) may pause and wait for keyboard input — for example, to accept a terms-of-service agreement. VS Code does not automatically focus the terminal when this happens, so the build can appear to be hung. If the container seems stuck, open the terminal panel (**View → Terminal**) or check the **Dev Containers** output panel (**View → Output**, then select *Dev Containers* from the dropdown) and press Enter or follow any on-screen prompt to continue.

### 3. Install frontend dependencies 📦

```bash
cd my-app
npm install
```

## MCP servers

Three MCP (Model Context Protocol) servers are pre-configured in `.vscode/mcp.json` (Copilot Chat) and `.mcp.json` (Claude Code):

| Server | What it gives AI agents |
|---|---|
| **GitHub** (`github-mcp-server`) | Read/write access to issues, PRs, and Actions — uses `GH_TOKEN` automatically |
| **SQLite** (`@modelcontextprotocol/server-sqlite`) | Direct read/query access to the app's local SQLite database (`MyApp/MyApp.Api/app.db`) |
| **Playwright** (`@playwright/mcp`) | Browser control to interact with and test the running app |
| **shadcn** (`shadcn@latest mcp`) | Browse, search, and install shadcn/ui components using natural language |

The GitHub MCP server binary is installed automatically by `post-create.sh`. SQLite, Playwright, and shadcn are downloaded on first use via `npx`.

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
| **Playwright E2E** | `npm ci` → install browsers → `npm run test:e2e` (report uploaded as artifact) |

## Build, test & lint

```bash
# .NET — run inside the container 📦
cd MyApp
dotnet build
dotnet test                          # run all tests
dotnet test --filter "Name=MyTest"   # run a specific test

# React — run inside the container 📦
cd my-app
npm run build       # type-check + Vite production build
npm run lint
npm run test:e2e    # Playwright E2E tests (dev server is started/reused automatically via Playwright webServer)
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
