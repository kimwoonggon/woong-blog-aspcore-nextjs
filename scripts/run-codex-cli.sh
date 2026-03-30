#!/usr/bin/env bash
set -euo pipefail

if [ -n "${CODEX_HOME_DIR:-}" ] && [ -d "${CODEX_HOME_DIR}" ]; then
  target_home="${HOME:-/root}"
  mkdir -p "${target_home}"
  if [ ! -e "${target_home}/.codex" ]; then
    ln -s "${CODEX_HOME_DIR}" "${target_home}/.codex"
  fi
fi

if command -v codex >/dev/null 2>&1; then
  exec codex "$@"
fi

if command -v npx >/dev/null 2>&1; then
  exec npx -y @openai/codex "$@"
fi

echo "Codex CLI is not available in this devcontainer session." >&2
exit 127
