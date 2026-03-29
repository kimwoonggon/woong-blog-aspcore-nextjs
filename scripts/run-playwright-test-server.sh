#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [ "$#" -eq 0 ]; then
  echo "Usage: $0 <command> [args...]" >&2
  exit 1
fi

cleanup() {
  cd "${ROOT_DIR}"
  docker compose down -v --remove-orphans >/dev/null 2>&1 || true
}

trap cleanup EXIT INT TERM

cd "${ROOT_DIR}"
docker compose down -v --remove-orphans >/dev/null 2>&1 || true
docker compose up --build -d db backend frontend nginx

BASE_URL="${PLAYWRIGHT_BASE_URL:-https://localhost}"
LOGIN_URL="${BASE_URL%/}/login"
HEALTH_URL="${BASE_URL%/}/api/health"

for _ in $(seq 1 120); do
  if curl -kfsS "${LOGIN_URL}" >/dev/null 2>&1 && curl -kfsS "${HEALTH_URL}" >/dev/null 2>&1; then
    PLAYWRIGHT_EXTERNAL_SERVER="${PLAYWRIGHT_EXTERNAL_SERVER:-1}" \
    PLAYWRIGHT_BASE_URL="${PLAYWRIGHT_BASE_URL:-https://localhost}" \
      "$@"
    exit $?
  fi

  sleep 2
done

if ! curl -kfsS "${LOGIN_URL}" >/dev/null 2>&1 || ! curl -kfsS "${HEALTH_URL}" >/dev/null 2>&1; then
  echo "Playwright test server did not become ready at ${BASE_URL} within 240 seconds." >&2
  exit 1
fi

exit 1
