#!/usr/bin/env bash
set -euo pipefail

DOCKER_BIN="${DOCKER_BIN:-docker}"
if ! command -v "${DOCKER_BIN}" >/dev/null 2>&1; then
  DOCKER_BIN="/mnt/c/Program Files/Docker/Docker/resources/bin/docker.exe"
fi

ENABLE_LOCAL_ADMIN_SHORTCUT=true \
Auth__EnableTestLoginEndpoint=true \
"${DOCKER_BIN}" compose up --build -d db frontend backend nginx
