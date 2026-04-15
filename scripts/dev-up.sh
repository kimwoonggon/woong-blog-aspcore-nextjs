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

if [[ -z "${CODEX_HOME_DIR:-}" ]]; then
  if [[ -n "${HOME:-}" ]]; then
    CODEX_HOME_DIR="${HOME}/.codex"
  else
    SHELL_HOME="$(getent passwd "$(id -u)" | cut -d: -f6)"
    CODEX_HOME_DIR="${SHELL_HOME}/.codex"
  fi
fi

export CODEX_HOME_DIR

COMPOSE_ENV_FILE="$(mktemp)"
cp "${APP_ENV_FILE}" "${COMPOSE_ENV_FILE}"
if ! grep -q '^CODEX_HOME_DIR=' "${COMPOSE_ENV_FILE}"; then
  printf '\nCODEX_HOME_DIR=%s\n' "${CODEX_HOME_DIR}" >> "${COMPOSE_ENV_FILE}"
fi

cleanup() {
  rm -f "${COMPOSE_ENV_FILE}"
}
trap cleanup EXIT

if [[ -z "${POSTGRES_DATA_DIR:-}" ]]; then
  if [[ "$(pwd)" == /mnt/* ]]; then
    POSTGRES_DATA_DIR="${HOME}/.woong-blog-docker/dev/postgres"
  else
    POSTGRES_DATA_DIR="./.docker-data/dev/postgres"
  fi
fi
mkdir -p "${POSTGRES_DATA_DIR}"
export POSTGRES_DATA_DIR

NGINX_DEFAULT_CONF="${NGINX_DEFAULT_CONF:-./nginx/local-https.conf}" \
APP_ENV_FILE="${APP_ENV_FILE}" \
"${DOCKER_BIN}" compose --env-file "${COMPOSE_ENV_FILE}" -f docker-compose.dev.yml up --build -d db frontend backend nginx
