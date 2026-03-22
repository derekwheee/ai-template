# AI Agent Dev Container Template

An isolated VS Code Dev Container environment for running AI agents safely — no host machine pollution.

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
| `7000` | ASP.NET (alternate) |
| `5173` | Vite Dev Server |

## Authentication

Linux containers have no keychain, so the Copilot CLI authenticates via a `GH_TOKEN` environment variable forwarded from your host machine.

**One-time host setup:**

1. Go to https://github.com/settings/personal-access-tokens/new
2. Under **Permissions → Account permissions**, add **Copilot Requests** (read-only)
3. Generate the token and add it to your shell profile on the host:

```bash
# ~/.zshrc or ~/.bashrc
export GH_TOKEN=github_pat_...
```

4. Reload your shell (`source ~/.zshrc`) then rebuild the container. The token is automatically available inside — no `/login` needed.



1. Open this folder in VS Code.
2. When prompted, click **Reopen in Container** (or run `Dev Containers: Reopen in Container` from the command palette).
3. Wait for the container to build and `post-create.sh` to finish.

### Start a new .NET Aspire project

```bash
dotnet new aspire-starter -o MyApp
cd MyApp
dotnet run --project MyApp.AppHost
```

### Scaffold a React + Vite app

```bash
npm create vite@latest my-app -- --template react-ts
cd my-app
npm install
npm run dev
```

## Structure

```
.devcontainer/
  devcontainer.json   # Container definition and VS Code settings
  post-create.sh      # Runs once after container creation
.gitignore
README.md
```
