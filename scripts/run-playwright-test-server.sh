#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [ "$#" -eq 0 ]; then
  echo "Usage: $0 <command> [args...]" >&2
  exit 1
fi

cleanup() {
  cd "${ROOT_DIR}"
  docker compose down --remove-orphans >/dev/null 2>&1 || true
}

trap cleanup EXIT INT TERM

cd "${ROOT_DIR}"
docker compose up --build -d db backend frontend nginx

for _ in $(seq 1 120); do
  if curl -fsS http://localhost/login >/dev/null 2>&1 && curl -fsS http://localhost/api/health >/dev/null 2>&1; then
    tail -f /dev/null &
    wait $!
    exit 0
  fi

  sleep 2
done

if ! curl -fsS http://localhost/login >/dev/null 2>&1 || ! curl -fsS http://localhost/api/health >/dev/null 2>&1; then
  echo "Playwright test server did not become ready at http://localhost within 240 seconds." >&2
  exit 1
fi

PLAYWRIGHT_EXTERNAL_SERVER="${PLAYWRIGHT_EXTERNAL_SERVER:-1}" \
PLAYWRIGHT_BASE_URL="${PLAYWRIGHT_BASE_URL:-http://localhost}" \
  "$@"
