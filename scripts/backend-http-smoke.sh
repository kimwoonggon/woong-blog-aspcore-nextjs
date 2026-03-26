#!/usr/bin/env bash
set -euo pipefail

base_url="${BASE_URL:-http://localhost}"
public_iterations="${PUBLIC_ITERATIONS:-10}"
admin_iterations="${ADMIN_ITERATIONS:-5}"
cookie_file="$(mktemp)"
trap 'rm -f "$cookie_file"' EXIT

compose=(docker compose)
"${compose[@]}" ps backend >/dev/null
"${compose[@]}" ps nginx >/dev/null
"${compose[@]}" ps db >/dev/null

check_status() {
  local url="$1"
  curl -fsS "$url" >/dev/null
}

echo "[backend-http-smoke] health"
for _ in $(seq 1 30); do
  if curl -fsS "$base_url/api/health" >/dev/null 2>&1; then
    break
  fi
  sleep 2
done
check_status "$base_url/api/health"

echo "[backend-http-smoke] public reads x${public_iterations}"
for path in /api/public/home /api/public/site-settings /api/public/works /api/public/blogs /introduction /contact /resume; do
  for _ in $(seq 1 "$public_iterations"); do
    check_status "$base_url$path"
  done
done

echo "[backend-http-smoke] acquire admin session"
curl -fsSL -c "$cookie_file" "$base_url/api/auth/test-login?email=admin@example.com&returnUrl=%2Fadmin" >/dev/null
curl -fsS -b "$cookie_file" "$base_url/api/auth/session" | grep -q '"authenticated":true'

echo "[backend-http-smoke] admin reads x${admin_iterations}"
for path in /api/admin/dashboard /api/admin/pages /api/admin/site-settings /api/admin/works /api/admin/blogs; do
  for _ in $(seq 1 "$admin_iterations"); do
    curl -fsS -b "$cookie_file" "$base_url$path" >/dev/null
  done
done

echo "[backend-http-smoke] PASS"
