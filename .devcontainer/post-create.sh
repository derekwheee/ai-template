#!/usr/bin/env bash
set -euo pipefail

echo "==> Installing .NET Aspire workload..."
sudo dotnet workload install aspire

echo "==> Installing GitHub Copilot CLI..."
curl -fsSL https://gh.io/copilot-install | bash

echo "==> Installing GitHub MCP server..."
_mcp_arch=$(uname -m); [ "$_mcp_arch" = "aarch64" ] && _mcp_arch="arm64"
gh release download \
  --repo github/github-mcp-server \
  --pattern "github-mcp-server_Linux_${_mcp_arch}.tar.gz" \
  --output /tmp/github-mcp-server.tar.gz
sudo tar -xzf /tmp/github-mcp-server.tar.gz -C /usr/local/bin github-mcp-server
rm -f /tmp/github-mcp-server.tar.gz

echo "==> Installing OpenAI Codex CLI..."
npm install -g @openai/codex

echo "==> Installing Anthropic Claude Code CLI..."
npm install -g @anthropic-ai/claude-code

echo "==> Updating npm to latest..."
npm install -g npm@latest

echo "==> Installing Vite globally for scaffolding convenience..."
npm install -g vite create-vite

echo "==> Dev container setup complete."
echo "    .NET version  : $(dotnet --version)"
echo "    Node version  : $(node --version)"
echo "    npm version   : $(npm --version)"
echo "    Copilot CLI   : $(copilot --version 2>/dev/null || echo 'not available')"
echo "    GitHub MCP    : $(github-mcp-server --version 2>/dev/null || echo 'not available')"
echo "    Codex CLI     : $(codex --version 2>/dev/null || echo 'not available')"
echo "    Claude Code   : $(claude --version 2>/dev/null || echo 'not available')"
