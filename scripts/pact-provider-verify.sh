#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACT_DIR="${PACT_FILE_DIRECTORY:-"$ROOT_DIR/tests/contracts/pacts"}"

if ! find "$PACT_DIR" -maxdepth 1 -name '*.json' -print -quit | grep -q .; then
  echo "No pact files found in $PACT_DIR; skipping Pact provider verification."
  exit 0
fi

PACT_PROVIDER_PORT="${PACT_PROVIDER_PORT:-5088}"
PACT_PROVIDER_BASE_URL="http://127.0.0.1:${PACT_PROVIDER_PORT}"
PACT_TEMP_ROOT="${PACT_TEMP_ROOT:-"/tmp/woong-blog-pact-${PACT_PROVIDER_PORT}"}"
mkdir -p "$PACT_TEMP_ROOT/media" "$PACT_TEMP_ROOT/dp"

cleanup() {
  if [[ -n "${PROVIDER_PID:-}" ]] && kill -0 "$PROVIDER_PID" 2>/dev/null; then
    kill "$PROVIDER_PID" 2>/dev/null || true
    wait "$PROVIDER_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

(
  cd "$ROOT_DIR"
  ASPNETCORE_URLS="$PACT_PROVIDER_BASE_URL" \
  ASPNETCORE_ENVIRONMENT=Testing \
  DatabaseProvider=InMemory \
  InMemoryDatabaseName="pact-provider-${PACT_PROVIDER_PORT}-${RANDOM}" \
  Auth__Enabled=true \
  Auth__Authority=https://example.test \
  Auth__ClientId=test-client \
  Auth__ClientSecret=test-secret \
  Auth__MediaRoot="$PACT_TEMP_ROOT/media" \
  Auth__DataProtectionKeysPath="$PACT_TEMP_ROOT/dp" \
  Auth__SecureCookies=false \
  Auth__RequireHttpsMetadata=false \
  Security__UseHttpsRedirection=false \
  Security__UseHsts=false \
  dotnet run --project backend/src/WoongBlog.Api/WoongBlog.Api.csproj --no-launch-profile
) >"$PACT_TEMP_ROOT/provider.log" 2>&1 &
PROVIDER_PID=$!

for _ in {1..60}; do
  if curl -fsS "$PACT_PROVIDER_BASE_URL/api/health" >/dev/null 2>&1; then
    break
  fi

  if ! kill -0 "$PROVIDER_PID" 2>/dev/null; then
    cat "$PACT_TEMP_ROOT/provider.log"
    echo "Pact provider exited before becoming healthy." >&2
    exit 1
  fi

  sleep 1
done

curl -fsS "$PACT_PROVIDER_BASE_URL/api/health" >/dev/null

PACT_PROVIDER_BASE_URL="$PACT_PROVIDER_BASE_URL" \
PACT_FILE_DIRECTORY="$PACT_DIR" \
dotnet test "$ROOT_DIR/backend/tests/WoongBlog.Api.ContractTests/WoongBlog.Api.ContractTests.csproj" -v minimal
