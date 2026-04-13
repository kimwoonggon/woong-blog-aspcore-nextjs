#!/usr/bin/env bash
set -euo pipefail

DOCKER_BIN="${DOCKER_BIN:-docker}"
if ! command -v "${DOCKER_BIN}" >/dev/null 2>&1; then
  DOCKER_BIN="/mnt/c/Program Files/Docker/Docker/resources/bin/docker.exe"
fi

APP_ENV_FILE="${APP_ENV_FILE:-.env.prod.local}"

if [[ ! -f "${APP_ENV_FILE}" ]]; then
  cat > "${APP_ENV_FILE}" <<'EOF'
FRONTEND_IMAGE=local/woong-blog-frontend:main
BACKEND_IMAGE=local/woong-blog-backend:main
APP_ENV_FILE=.env.prod.local
NGINX_DEFAULT_CONF=./nginx/prod-bootstrap.conf
CERTBOT_WWW_DIR=./certbot/www
LETSENCRYPT_DIR=./certbot/conf
POSTGRES_DB=portfolio
POSTGRES_USER=portfolio
POSTGRES_PASSWORD=portfolio
Auth__Enabled=false
PROXY_KNOWN_NETWORK=172.16.0.0/12
EOF
fi

mkdir -p certbot/www certbot/conf/live/current
"${DOCKER_BIN}" build -f Dockerfile -t local/woong-blog-frontend:main .
"${DOCKER_BIN}" build -f backend/Dockerfile -t local/woong-blog-backend:main .
"${DOCKER_BIN}" compose --env-file "${APP_ENV_FILE}" -f docker-compose.prod.yml up -d db frontend backend nginx
