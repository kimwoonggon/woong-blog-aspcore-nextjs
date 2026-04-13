#!/usr/bin/env bash
set -euo pipefail

MODE="${1:-${MODE:-}}"

if [[ -z "${MODE}" ]]; then
  echo "usage: $0 <dev|main>" >&2
  exit 1
fi

DOCKER_BIN="${DOCKER_BIN:-docker}"
if ! command -v "${DOCKER_BIN}" >/dev/null 2>&1; then
  DOCKER_BIN="/mnt/c/Program Files/Docker/Docker/resources/bin/docker.exe"
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${ROOT_DIR}"

created_env=0
if [[ ! -f .env && -f .env.example ]]; then
  cp .env.example .env
  created_env=1
fi

keep_running="${KEEP_RUNNING:-0}"

cleanup() {
  if [[ "${keep_running}" != "1" ]]; then
    "${DOCKER_BIN}" compose down --remove-orphans --volumes >/dev/null 2>&1 || true
  fi
  if [[ "${created_env}" -eq 1 ]]; then
    rm -f .env
  fi
}

on_error() {
  echo "compose smoke failed for mode=${MODE}" >&2
  "${DOCKER_BIN}" compose ps || true
  "${DOCKER_BIN}" compose logs --tail=200 frontend backend nginx db || true
}

trap cleanup EXIT
trap on_error ERR

case "${MODE}" in
  dev)
    export ENABLE_LOCAL_ADMIN_SHORTCUT=true
    export Auth__EnableTestLoginEndpoint=true
    expected_local_admin=present
    expected_test_login_status=302
    ;;
  main)
    export ENABLE_LOCAL_ADMIN_SHORTCUT=false
    export Auth__EnableTestLoginEndpoint=false
    expected_local_admin=absent
    expected_test_login_status=404
    ;;
  *)
    echo "unsupported mode: ${MODE}" >&2
    exit 1
    ;;
esac

export NGINX_DEFAULT_CONF=./nginx/default.conf

"${DOCKER_BIN}" compose down --remove-orphans --volumes >/dev/null 2>&1 || true
"${DOCKER_BIN}" compose config >/dev/null
"${DOCKER_BIN}" compose up -d --build db backend frontend nginx

for _ in $(seq 1 90); do
  if curl -fsS http://localhost/api/health >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

curl -fsS http://localhost/api/health -o /tmp/woong-blog-health.json
curl -fsS http://localhost/login -o /tmp/woong-blog-login.html
curl -fsS http://localhost/ -o /tmp/woong-blog-home.html
curl -fsS http://localhost/blog -o /tmp/woong-blog-blog.html
curl -fsS http://localhost/works -o /tmp/woong-blog-works.html

grep -Fq '"status":"ok"' /tmp/woong-blog-health.json
grep -Fq 'Portfolio' /tmp/woong-blog-home.html
grep -Fq 'Admin Login' /tmp/woong-blog-login.html
grep -Fq '>Blog<' /tmp/woong-blog-blog.html
grep -Fq '>Works<' /tmp/woong-blog-works.html

if [[ "${expected_local_admin}" == "present" ]]; then
  grep -Fq "Continue as Local Admin" /tmp/woong-blog-login.html
else
  if grep -Fq "Continue as Local Admin" /tmp/woong-blog-login.html; then
    echo "local admin shortcut should be hidden in mode=${MODE}" >&2
    exit 1
  fi
fi

test_login_status="$(
  curl -sS -o /tmp/woong-blog-test-login.txt -w "%{http_code}" \
    "http://localhost/api/auth/test-login?email=admin%40example.com&returnUrl=%2Fadmin"
)"

if [[ "${test_login_status}" != "${expected_test_login_status}" ]]; then
  echo "unexpected test-login status for mode=${MODE}: got ${test_login_status}, expected ${expected_test_login_status}" >&2
  cat /tmp/woong-blog-test-login.txt >&2 || true
  exit 1
fi

echo "compose smoke passed for mode=${MODE}"
