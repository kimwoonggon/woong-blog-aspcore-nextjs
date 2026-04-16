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

compose_file="${COMPOSE_FILE_OVERRIDE:-}"
compose_env_file=""
compose_cmd=()
created_env=0
base_url="${BASE_URL:-http://localhost}"
curl_opts=(-fsS)

keep_running="${KEEP_RUNNING:-0}"

cleanup() {
  if [[ "${keep_running}" != "1" ]]; then
    if [[ "${#compose_cmd[@]}" -gt 0 ]]; then
      "${compose_cmd[@]}" down --remove-orphans --volumes >/dev/null 2>&1 || true
    else
      "${DOCKER_BIN}" compose down --remove-orphans --volumes >/dev/null 2>&1 || true
    fi
    if [[ "${created_env}" -eq 1 ]]; then
      rm -f "${compose_env_file}"
    fi
  fi
}

on_error() {
  echo "compose smoke failed for mode=${MODE}" >&2
  if [[ "${#compose_cmd[@]}" -gt 0 ]]; then
    "${compose_cmd[@]}" ps || true
    "${compose_cmd[@]}" logs --tail=200 frontend backend nginx db || true
  else
    "${DOCKER_BIN}" compose ps || true
    "${DOCKER_BIN}" compose logs --tail=200 frontend backend nginx db || true
  fi
}

trap cleanup EXIT
trap on_error ERR

case "${MODE}" in
  dev)
    compose_file="${compose_file:-docker-compose.dev.yml}"
    base_url="${BASE_URL:-https://localhost:3001}"
    compose_env_file="${APP_ENV_FILE:-.env}"
    if [[ ! -f "${compose_env_file}" && -f .env.example ]]; then
      cp .env.example "${compose_env_file}"
      created_env=1
    fi
    export APP_ENV_FILE="${compose_env_file}"
    export NGINX_DEFAULT_CONF="${NGINX_DEFAULT_CONF:-./nginx/local-https.conf}"
    export NGINX_HTTP_PORT="${NGINX_HTTP_PORT:-3000}"
    export NGINX_HTTPS_PORT="${NGINX_HTTPS_PORT:-3001}"
    export NGINX_BIND_HOST="${NGINX_BIND_HOST:-127.0.0.1}"
    export BACKEND_PUBLISH_PORT="${BACKEND_PUBLISH_PORT:-8081}"
    export BACKEND_BIND_HOST="${BACKEND_BIND_HOST:-127.0.0.1}"
    export LOCAL_CERTS_DIR="${LOCAL_CERTS_DIR:-./.local-certs}"
    mkdir -p "${LOCAL_CERTS_DIR}"
    if [[ ! -f "${LOCAL_CERTS_DIR}/localhost.pem" || ! -f "${LOCAL_CERTS_DIR}/localhost-key.pem" ]]; then
      openssl req -x509 -nodes -newkey rsa:2048 -days 7 \
        -keyout "${LOCAL_CERTS_DIR}/localhost-key.pem" \
        -out "${LOCAL_CERTS_DIR}/localhost.pem" \
        -subj "/CN=localhost" \
        -addext "subjectAltName=DNS:localhost,IP:127.0.0.1,IP:::1" >/dev/null 2>&1
    fi
    {
      printf '\nNGINX_DEFAULT_CONF=%s\n' "${NGINX_DEFAULT_CONF}"
      printf '\nNGINX_HTTP_PORT=%s\n' "${NGINX_HTTP_PORT}"
      printf 'NGINX_BIND_HOST=%s\n' "${NGINX_BIND_HOST}"
      printf 'NGINX_HTTPS_PORT=%s\n' "${NGINX_HTTPS_PORT}"
      printf 'BACKEND_PUBLISH_PORT=%s\n' "${BACKEND_PUBLISH_PORT}"
      printf 'BACKEND_BIND_HOST=%s\n' "${BACKEND_BIND_HOST}"
      printf 'LOCAL_CERTS_DIR=%s\n' "${LOCAL_CERTS_DIR}"
    } >> "${compose_env_file}"
    expected_local_admin=present
    expected_test_login_status=302
    ;;
  main)
    compose_file="${compose_file:-docker-compose.prod.yml}"
    base_url="${BASE_URL:-http://localhost}"
    compose_env_file="${APP_ENV_FILE:-.env.prod.ci}"
    if [[ -z "${FRONTEND_IMAGE:-}" ]]; then
      FRONTEND_IMAGE="local/woong-blog-frontend:smoke"
      "${DOCKER_BIN}" build -f Dockerfile -t "${FRONTEND_IMAGE}" .
    fi
    if [[ -z "${BACKEND_IMAGE:-}" ]]; then
      BACKEND_IMAGE="local/woong-blog-backend:smoke"
      "${DOCKER_BIN}" build -f backend/Dockerfile -t "${BACKEND_IMAGE}" .
    fi
    cat > "${compose_env_file}" <<EOF
FRONTEND_IMAGE=${FRONTEND_IMAGE}
BACKEND_IMAGE=${BACKEND_IMAGE}
APP_ENV_FILE=${compose_env_file}
NGINX_DEFAULT_CONF=${NGINX_DEFAULT_CONF:-./nginx/prod-bootstrap.conf}
CERTBOT_WWW_DIR=./certbot/www
LETSENCRYPT_DIR=./certbot/conf
POSTGRES_DB=portfolio
POSTGRES_USER=portfolio
POSTGRES_PASSWORD=portfolio
Auth__Enabled=false
PROXY_KNOWN_NETWORK=172.16.0.0/12
EOF
    created_env=1
    mkdir -p certbot/www certbot/conf/live/current
    export APP_ENV_FILE="${compose_env_file}"
    expected_local_admin=absent
    expected_test_login_status=404
    ;;
  *)
    echo "unsupported mode: ${MODE}" >&2
    exit 1
    ;;
esac

curl_opts=(-fsS)
if [[ "${base_url}" == https://* ]]; then
  curl_opts+=(-k)
fi

compose_cmd=("${DOCKER_BIN}" compose --env-file "${compose_env_file}" -f "${compose_file}")

"${compose_cmd[@]}" down --remove-orphans --volumes >/dev/null 2>&1 || true
"${compose_cmd[@]}" config >/dev/null
if [[ "${MODE}" == "dev" ]]; then
  "${compose_cmd[@]}" up -d --build db backend frontend nginx
else
  "${compose_cmd[@]}" up -d db backend frontend nginx
fi

for _ in $(seq 1 90); do
  if curl "${curl_opts[@]}" "${base_url}/api/health" >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

curl "${curl_opts[@]}" "${base_url}/api/health" -o /tmp/woong-blog-health.json
curl "${curl_opts[@]}" "${base_url}/login" -o /tmp/woong-blog-login.html
curl "${curl_opts[@]}" "${base_url}/" -o /tmp/woong-blog-home.html
curl "${curl_opts[@]}" "${base_url}/blog" -o /tmp/woong-blog-blog.html
curl "${curl_opts[@]}" "${base_url}/works" -o /tmp/woong-blog-works.html

grep -Fq '"status":"ok"' /tmp/woong-blog-health.json
grep -Fq 'View My Works' /tmp/woong-blog-home.html
grep -Fq 'Admin Login' /tmp/woong-blog-login.html
grep -Fq '>Study<' /tmp/woong-blog-blog.html
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
  curl "${curl_opts[@]/-fsS/-sS}" -o /tmp/woong-blog-test-login.txt -w "%{http_code}" \
    "${base_url}/api/auth/test-login?email=admin%40example.com&returnUrl=%2Fadmin"
)"

if [[ "${test_login_status}" != "${expected_test_login_status}" ]]; then
  echo "unexpected test-login status for mode=${MODE}: got ${test_login_status}, expected ${expected_test_login_status}" >&2
  cat /tmp/woong-blog-test-login.txt >&2 || true
  exit 1
fi

echo "compose smoke passed for mode=${MODE}"
