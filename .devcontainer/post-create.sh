#!/usr/bin/env bash
set -euo pipefail

echo "==> Installing .NET Aspire workload..."
sudo dotnet workload install aspire

echo "==> Installing GitHub Copilot CLI..."
curl -fsSL https://gh.io/copilot-install | bash

echo "==> Updating npm to latest..."
npm install -g npm@latest

echo "==> Installing Vite globally for scaffolding convenience..."
npm install -g vite create-vite

echo "==> Dev container setup complete."
echo "    .NET version : $(dotnet --version)"
echo "    Node version : $(node --version)"
echo "    npm version  : $(npm --version)"
