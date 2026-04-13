#!/usr/bin/env bash
set -euo pipefail

DOCKER_BIN="${DOCKER_BIN:-docker}"
if ! command -v "${DOCKER_BIN}" >/dev/null 2>&1; then
  DOCKER_BIN="/mnt/c/Program Files/Docker/Docker/resources/bin/docker.exe"
fi

APP_ENV_FILE="${APP_ENV_FILE:-.env}"
if [[ ! -f "${APP_ENV_FILE}" && -f .env.example ]]; then
  cp .env.example "${APP_ENV_FILE}"
fi

NGINX_DEFAULT_CONF="${NGINX_DEFAULT_CONF:-./nginx/local-https.conf}" \
APP_ENV_FILE="${APP_ENV_FILE}" \
"${DOCKER_BIN}" compose --env-file "${APP_ENV_FILE}" -f docker-compose.dev.yml up --build -d db frontend backend nginx
