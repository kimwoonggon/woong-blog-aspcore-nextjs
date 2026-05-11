#!/usr/bin/env bash
set -euo pipefail

EXPECTED_MAIN_SHA="${EXPECTED_MAIN_SHA:-}"
EXPECTED_BACKEND_IMAGE_DIGEST="${EXPECTED_BACKEND_IMAGE_DIGEST:-}"
EXPECTED_FRONTEND_IMAGE_DIGEST="${EXPECTED_FRONTEND_IMAGE_DIGEST:-}"
BACKEND_IMAGE="ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main"
FRONTEND_IMAGE="ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main"

REPO_DIR="${REPO_DIR:-/root/service/woong-blog-aspcore-nextjs}"
BASE_URL="${BASE_URL:-https://woonglab.com}"
WORK_READ_PATH="${WORK_READ_PATH:-/api/public/works/smoke-fluid-simulation}"
STUDY_READ_PATH="${STUDY_READ_PATH:-}"
RATES="${RATES:-100 200 300 400}"
DURATION_SECONDS="${DURATION_SECONDS:-30}"
MAX_VUS="${MAX_VUS:-500}"
PRE_ALLOCATED_VUS="${PRE_ALLOCATED_VUS:-100}"
GHCR_USER="${GHCR_USER:-kimwoonggon}"
GHCR_TOKEN="${GHCR_TOKEN:-}"
RUN_ID="${RUN_ID:-$(date -u +%Y%m%dT%H%M%SZ)}"
RUN_DIR="${RUN_DIR:-backend/reports/current-main-production-evidence-${RUN_ID}}"
LOAD_DIR="${RUN_DIR%/}/loadtest"
EVIDENCE_DIR="${RUN_DIR%/}/evidence"
PREFLIGHT_LOG="${RUN_DIR%/}/prod-runtime-preflight.log"

die() {
  echo "[server-current-main] ERROR: $*" >&2
  exit 1
}

info() {
  echo "[server-current-main] $*"
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || die "missing required command: $1"
}

upsert_env() {
  local key="$1"
  local value="$2"
  if grep -q "^${key}=" .env.prod; then
    sed -i "s#^${key}=.*#${key}=${value}#" .env.prod
  else
    printf '\n%s=%s\n' "${key}" "${value}" >> .env.prod
  fi
}

reject_seed_path() {
  local label="$1"
  local value="$2"
  case "${value,,}" in
    *seed*|*fixture*) die "${label} must be a real public target, not seed/fixture: ${value}" ;;
  esac
}

resolve_first_non_seed_public_path() {
  local api_path="$1"
  local public_prefix="$2"
  curl -fsS "${BASE_URL%/}${api_path}" | node -e '
const fs = require("node:fs")
const prefix = process.argv[1]
const input = fs.readFileSync(0, "utf8")
const payload = JSON.parse(input)
const items = Array.isArray(payload.items) ? payload.items : []
const selected = items.find((item) => {
  const slug = String(item.slug || "")
  const title = String(item.title || "")
  return slug && !/seed|fixture/i.test(slug) && !/seed|fixture/i.test(title)
})
if (!selected) {
  console.error("no non-seed public target found")
  process.exit(4)
}
console.log(`${prefix}${encodeURIComponent(selected.slug)}`)
' "${public_prefix}"
}

repo_digest_for() {
  local image="$1"
  docker image inspect "${image}" --format '{{index .RepoDigests 0}}' | sed 's/^.*@//'
}

require_digest_if_expected() {
  local label="$1"
  local actual="$2"
  local expected
  expected="${3:-}"
  if [[ -z "${expected}" ]]; then
    info "${label} digest observed: ${actual}"
    return 0
  fi

  [[ "${actual}" == "${expected}" ]] || die "${label} digest mismatch: expected ${expected}, got ${actual}"
}

require_command git
require_command docker
require_command curl
require_command node
require_command k6
require_command tar

docker_auth_config="$(mktemp -d)"
cleanup() {
  rm -rf "${docker_auth_config}"
}
trap cleanup EXIT

cd "${REPO_DIR}"
[[ -f .env.prod ]] || die ".env.prod is required; create it from .env.prod.example and fill server secrets first"

git fetch origin main --prune
git checkout main
git pull --ff-only origin main
actual_sha="$(git rev-parse HEAD)"
if [[ -n "${EXPECTED_MAIN_SHA}" && "${actual_sha}" != "${EXPECTED_MAIN_SHA}" ]]; then
  die "main SHA mismatch: expected ${EXPECTED_MAIN_SHA}, got ${actual_sha}. Refresh this runbook before interpreting results."
fi
info "Using main SHA: ${actual_sha}"

mkdir -p "${RUN_DIR}" "${LOAD_DIR}" "${EVIDENCE_DIR}"
backup=".env.prod.backup.${RUN_ID}"
cp .env.prod "${backup}"
info "Backed up .env.prod to ${backup}"

upsert_env FRONTEND_IMAGE "${FRONTEND_IMAGE}"
upsert_env BACKEND_IMAGE "${BACKEND_IMAGE}"
upsert_env NEXT_PUBLIC_SITE_URL "${BASE_URL}"
upsert_env LoadTesting__BaseUrl "${BASE_URL}"
upsert_env APP_ENV_FILE .env.prod
upsert_env NGINX_DEFAULT_CONF ./nginx/prod.conf

if [[ -n "${GHCR_TOKEN}" ]]; then
  printf '%s' "${GHCR_TOKEN}" | docker --config "${docker_auth_config}" login ghcr.io -u "${GHCR_USER}" --password-stdin >/dev/null
  info "Logged in to ghcr.io with GHCR_TOKEN using a temporary Docker config."
else
  info "Using a temporary anonymous Docker config for public GHCR runtime images."
fi

docker --config "${docker_auth_config}" manifest inspect "${BACKEND_IMAGE}" >/dev/null
docker --config "${docker_auth_config}" manifest inspect "${FRONTEND_IMAGE}" >/dev/null

docker --config "${docker_auth_config}" compose --env-file .env.prod -f docker-compose.prod.yml config \
  | grep -E 'image: ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-(frontend|backend):main'

docker --config "${docker_auth_config}" compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --remove-orphans
docker compose --env-file .env.prod -f docker-compose.prod.yml ps

backend_digest="$(repo_digest_for "${BACKEND_IMAGE}")"
frontend_digest="$(repo_digest_for "${FRONTEND_IMAGE}")"
require_digest_if_expected "backend" "${backend_digest}" "${EXPECTED_BACKEND_IMAGE_DIGEST}"
require_digest_if_expected "frontend" "${frontend_digest}" "${EXPECTED_FRONTEND_IMAGE_DIGEST}"

if [[ -z "${STUDY_READ_PATH}" ]]; then
  STUDY_READ_PATH="$(resolve_first_non_seed_public_path '/api/public/blogs?page=1&pageSize=12' '/api/public/blogs/')"
fi

reject_seed_path WORK_READ_PATH "${WORK_READ_PATH}"
reject_seed_path STUDY_READ_PATH "${STUDY_READ_PATH}"

info "Using BASE_URL=${BASE_URL}"
info "Using WORK_READ_PATH=${WORK_READ_PATH}"
info "Using STUDY_READ_PATH=${STUDY_READ_PATH}"
info "Using RATES=${RATES}; DURATION_SECONDS=${DURATION_SECONDS}; MAX_VUS=${MAX_VUS}; PRE_ALLOCATED_VUS=${PRE_ALLOCATED_VUS}"

BASE_URL="${BASE_URL}" \
REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1 \
WORK_READ_PATH="${WORK_READ_PATH}" \
./scripts/prod-runtime-preflight.sh 2>&1 | tee "${PREFLIGHT_LOG}"

BASE_URL="${BASE_URL}" \
WORK_READ_PATH="${WORK_READ_PATH}" \
STUDY_READ_PATH="${STUDY_READ_PATH}" \
RATES="${RATES}" \
DURATION_SECONDS="${DURATION_SECONDS}" \
MAX_VUS="${MAX_VUS}" \
PRE_ALLOCATED_VUS="${PRE_ALLOCATED_VUS}" \
OUTPUT_DIR="${LOAD_DIR}" \
./scripts/prod-real-load-steps.sh

PREFLIGHT_LOG="${PREFLIGHT_LOG}" \
REAL_LOAD_DIR="${LOAD_DIR}" \
OUTPUT_DIR="${EVIDENCE_DIR}" \
MAIN_SHA="${actual_sha}" \
BACKEND_IMAGE_DIGEST="${backend_digest}" \
FRONTEND_IMAGE_DIGEST="${frontend_digest}" \
./scripts/prod-runtime-evidence-bundle.sh

EXPECTED_MAIN_SHA="${actual_sha}" \
EXPECTED_BACKEND_IMAGE_DIGEST="${backend_digest}" \
EXPECTED_FRONTEND_IMAGE_DIGEST="${frontend_digest}" \
./scripts/prod-runtime-evidence-verify.sh "${EVIDENCE_DIR}/production-runtime-evidence.tar.gz"

info "PASS"
info "Return this evidence tarball: ${REPO_DIR}/${EVIDENCE_DIR}/production-runtime-evidence.tar.gz"
info "Also return summary markdown: ${REPO_DIR}/${EVIDENCE_DIR}/production-runtime-evidence-summary.md"
