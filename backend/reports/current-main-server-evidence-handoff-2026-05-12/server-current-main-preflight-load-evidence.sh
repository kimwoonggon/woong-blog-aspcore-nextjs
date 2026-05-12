#!/usr/bin/env bash
set -euo pipefail
trap 'echo "[current-main-evidence] failed at line ${LINENO}" >&2' ERR

REPO_DIR="${REPO_DIR:-/root/service/woong-blog-aspcore-nextjs}"
BASE_URL="${BASE_URL:-https://woonglab.com}"
EXPECTED_MAIN_SHA="${EXPECTED_MAIN_SHA:-}"
FRONTEND_IMAGE="${FRONTEND_IMAGE:-}"
BACKEND_IMAGE="${BACKEND_IMAGE:-}"
FRONTEND_DIGEST="${FRONTEND_DIGEST:-}"
BACKEND_DIGEST="${BACKEND_DIGEST:-}"
APP_ENV_FILE="${APP_ENV_FILE:-.env.prod}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
WORK_READ_PATH="${WORK_READ_PATH:-}"
STUDY_READ_PATH="${STUDY_READ_PATH:-}"
RATES="${RATES:-100 200 300 400}"
DURATION_SECONDS="${DURATION_SECONDS:-30}"
MAX_VUS="${MAX_VUS:-500}"
PRE_ALLOCATED_VUS="${PRE_ALLOCATED_VUS:-100}"
OUTPUT_DIR="${OUTPUT_DIR:-backend/reports/prod-real-load-steps-$(date -u +%Y%m%dT%H%M%SZ)/loadtest}"
ARTIFACT_BUNDLE="${ARTIFACT_BUNDLE:-}"
DOCKER_CONFIG_DIR=""

info() {
  printf '[current-main-evidence] %s\n' "$*"
}

fail() {
  printf '[current-main-evidence] ERROR: %s\n' "$*" >&2
  exit 1
}

cleanup() {
  if [[ -n "${DOCKER_CONFIG_DIR}" && -d "${DOCKER_CONFIG_DIR}" ]]; then
    rm -rf "${DOCKER_CONFIG_DIR}"
  fi
}
trap cleanup EXIT

require_command() {
  local name="$1"
  if ! command -v "${name}" >/dev/null 2>&1; then
    fail "missing required command: ${name}"
  fi
}

upsert_env() {
  local key="$1"
  local value="$2"
  if grep -q "^${key}=" "${APP_ENV_FILE}"; then
    sed -i "s#^${key}=.*#${key}=${value}#" "${APP_ENV_FILE}"
  else
    printf '\n%s=%s\n' "${key}" "${value}" >>"${APP_ENV_FILE}"
  fi
}

reject_seed_or_fixture_path() {
  local label="$1"
  local value="$2"
  local lowered="${value,,}"
  case "${lowered}" in
    *seed*|*fixture*)
      fail "${label} must be a real public target, not seed/fixture: ${value}"
      ;;
  esac
}

resolve_first_non_seed_public_path() {
  local api_path="$1"
  local public_prefix="$2"
  curl -fsS --compressed "${BASE_URL%/}${api_path}" \
    | node -e '
const fs = require("node:fs");
const prefix = process.argv[1];
const input = fs.readFileSync(0, "utf8");
const payload = JSON.parse(input);
const items = Array.isArray(payload.items) ? payload.items : [];
const selected = items.find((item) => {
  const slug = String(item.slug || "");
  const title = String(item.title || "");
  return slug && !/seed|fixture/i.test(slug) && !/seed|fixture/i.test(title);
});
if (!selected) {
  console.error("no non-seed public target found");
  process.exit(4);
}
console.log(`${prefix}${encodeURIComponent(selected.slug)}`);
' "${public_prefix}"
}

assert_public_list_contract() {
  local label="$1"
  local api_path="$2"
  shift 2
  local forbidden=("$@")

  curl -fsS --compressed "${BASE_URL%/}${api_path}" \
    | node -e '
const fs = require("node:fs");
const label = process.argv[1];
const forbidden = process.argv.slice(2);
const input = fs.readFileSync(0, "utf8");
const payload = JSON.parse(input);
const items = Array.isArray(payload.items) ? payload.items : [];
if (!items.length) {
  console.error(`${label}: expected at least one public item`);
  process.exit(10);
}
const keys = Object.keys(items[0]);
const stale = forbidden.filter((key) => Object.prototype.hasOwnProperty.call(items[0], key));
console.log(`${label}: count=${items.length} firstKeys=${keys.sort().join(",")}`);
if (stale.length) {
  console.error(`${label}: stale keys present: ${stale.join(",")}`);
  process.exit(11);
}
' "${label}" "${forbidden[@]}"
}

resolve_image_digest() {
  local image="$1"
  docker --config "${DOCKER_CONFIG_DIR}" manifest inspect "${image}" \
    | node -e '
const fs = require("node:fs");
const manifest = JSON.parse(fs.readFileSync(0, "utf8"));
const digest = manifest.manifests?.[0]?.digest || manifest.config?.digest || "";
if (!digest) {
  console.error("unable to resolve digest from manifest");
  process.exit(30);
}
console.log(digest);
'
}

require_command git
require_command docker
require_command curl
require_command node
require_command tar

cd "${REPO_DIR}"
[[ -f "${APP_ENV_FILE}" ]] || fail "missing ${APP_ENV_FILE} in ${REPO_DIR}"
[[ -f "${COMPOSE_FILE}" ]] || fail "missing ${COMPOSE_FILE} in ${REPO_DIR}"

info "repository: ${REPO_DIR}"
git fetch origin main --prune
target_main_sha="$(git rev-parse origin/main)"
if [[ -n "${EXPECTED_MAIN_SHA}" ]]; then
  if [[ "${target_main_sha}" != "${EXPECTED_MAIN_SHA}" ]]; then
    fail "fetched origin/main SHA ${target_main_sha}, expected pinned ${EXPECTED_MAIN_SHA}"
  fi
  target_main_sha="${EXPECTED_MAIN_SHA}"
fi
SHA_SHORT="${target_main_sha:0:12}"
if [[ -z "${FRONTEND_IMAGE}" ]]; then
  FRONTEND_IMAGE="ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:sha-${SHA_SHORT}"
fi
if [[ -z "${BACKEND_IMAGE}" ]]; then
  BACKEND_IMAGE="ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:sha-${SHA_SHORT}"
fi

git checkout main
git pull --ff-only origin main

actual_sha="$(git rev-parse HEAD)"
if [[ "${actual_sha}" != "${target_main_sha}" ]]; then
  fail "checked out main SHA ${actual_sha}, expected ${target_main_sha}"
fi
info "checked out expected main SHA: ${actual_sha}"

backup="${APP_ENV_FILE}.backup.$(date -u +%Y%m%dT%H%M%SZ)"
cp "${APP_ENV_FILE}" "${backup}"
info "backed up ${APP_ENV_FILE} to ${backup}"

upsert_env FRONTEND_IMAGE "${FRONTEND_IMAGE}"
upsert_env BACKEND_IMAGE "${BACKEND_IMAGE}"
upsert_env LoadTesting__BaseUrl "${BASE_URL}"
upsert_env APP_ENV_FILE "${APP_ENV_FILE}"
upsert_env NGINX_DEFAULT_CONF ./nginx/prod.conf

DOCKER_CONFIG_DIR="$(mktemp -d)"
if [[ -n "${GHCR_TOKEN:-}" ]]; then
  printf '%s' "${GHCR_TOKEN}" \
    | docker --config "${DOCKER_CONFIG_DIR}" login ghcr.io -u "${GHCR_USER:-kimwoonggon}" --password-stdin >/dev/null
  info "logged in to ghcr.io using temporary Docker config"
else
  info "GHCR_TOKEN not set; using anonymous GHCR access with temporary Docker config"
fi

if [[ -z "${FRONTEND_DIGEST}" ]]; then
  FRONTEND_DIGEST="$(resolve_image_digest "${FRONTEND_IMAGE}")"
fi
if [[ -z "${BACKEND_DIGEST}" ]]; then
  BACKEND_DIGEST="$(resolve_image_digest "${BACKEND_IMAGE}")"
fi

info "runtime image targets:"
printf '  FRONTEND_IMAGE=%s\n' "${FRONTEND_IMAGE}"
printf '  BACKEND_IMAGE=%s\n' "${BACKEND_IMAGE}"
printf '  FRONTEND_DIGEST=%s\n' "${FRONTEND_DIGEST}"
printf '  BACKEND_DIGEST=%s\n' "${BACKEND_DIGEST}"

info "compose resolved runtime images:"
docker --config "${DOCKER_CONFIG_DIR}" compose --env-file "${APP_ENV_FILE}" -f "${COMPOSE_FILE}" config \
  | grep -E "image: ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-(frontend|backend):sha-${SHA_SHORT}"

docker --config "${DOCKER_CONFIG_DIR}" compose --env-file "${APP_ENV_FILE}" -f "${COMPOSE_FILE}" pull
docker compose --env-file "${APP_ENV_FILE}" -f "${COMPOSE_FILE}" up -d --remove-orphans
docker compose --env-file "${APP_ENV_FILE}" -f "${COMPOSE_FILE}" ps

info "backend environment check:"
docker compose --env-file "${APP_ENV_FILE}" -f "${COMPOSE_FILE}" exec -T backend printenv \
  | grep -E '^(ASPNETCORE_ENVIRONMENT|LoadTesting__BaseUrl|POSTGRES_MAX_POOL_SIZE)=|^ConnectionStrings__Postgres=' \
  | sed -E 's/(Password=)[^;]*/\1***REDACTED***/'

mkdir -p "${OUTPUT_DIR%/}"
preflight_log="${OUTPUT_DIR%/}/current-main-preflight.log"
info "running production preflight"
BASE_URL="${BASE_URL}" ./scripts/prod-runtime-preflight.sh 2>&1 | tee "${preflight_log}"

info "checking public DTO/list contract after deploy"
assert_public_list_contract "public works" "/api/public/works?page=1&pageSize=12" \
  contentJson period iconUrl body content rawContent
assert_public_list_contract "public blogs" "/api/public/blogs?page=1&pageSize=12" \
  contentJson body content rawContent

if [[ -z "${WORK_READ_PATH}" ]]; then
  WORK_READ_PATH="$(resolve_first_non_seed_public_path '/api/public/works?page=1&pageSize=12' '/api/public/works/')"
fi
if [[ -z "${STUDY_READ_PATH}" ]]; then
  STUDY_READ_PATH="$(resolve_first_non_seed_public_path '/api/public/blogs?page=1&pageSize=12' '/api/public/blogs/')"
fi

reject_seed_or_fixture_path WORK_READ_PATH "${WORK_READ_PATH}"
reject_seed_or_fixture_path STUDY_READ_PATH "${STUDY_READ_PATH}"

info "real backend load targets:"
printf '  BASE_URL=%s\n' "${BASE_URL}"
printf '  WORK_READ_PATH=%s\n' "${WORK_READ_PATH}"
printf '  STUDY_READ_PATH=%s\n' "${STUDY_READ_PATH}"
printf '  LIST_PAGE_SIZE=12\n'
printf '  RATES=%s\n' "${RATES}"
printf '  DURATION_SECONDS=%s\n' "${DURATION_SECONDS}"
printf '  MAX_VUS=%s\n' "${MAX_VUS}"
printf '  PRE_ALLOCATED_VUS=%s\n' "${PRE_ALLOCATED_VUS}"
printf '  OUTPUT_DIR=%s\n' "${OUTPUT_DIR}"

BASE_URL="${BASE_URL}" \
WORK_READ_PATH="${WORK_READ_PATH}" \
STUDY_READ_PATH="${STUDY_READ_PATH}" \
RATES="${RATES}" \
DURATION_SECONDS="${DURATION_SECONDS}" \
MAX_VUS="${MAX_VUS}" \
PRE_ALLOCATED_VUS="${PRE_ALLOCATED_VUS}" \
LIST_PAGE_SIZE=12 \
OUTPUT_DIR="${OUTPUT_DIR}" \
./scripts/prod-real-load-steps.sh

summary_json="${OUTPUT_DIR%/}/prod-real-load-steps-summary.json"
summary_md="${OUTPUT_DIR%/}/prod-real-load-steps-summary.md"
[[ -s "${summary_json}" ]] || fail "missing real load summary json: ${summary_json}"
[[ -s "${summary_md}" ]] || fail "missing real load summary markdown: ${summary_md}"
grep -q '__k6Vu' "${OUTPUT_DIR%/}/public-api-real-mix-k6.js" \
  || fail "generated k6 script is missing identity suffix guard"

node - "${summary_json}" <<'NODE'
const fs = require('node:fs');
const summary = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const steps = Array.isArray(summary.steps) ? summary.steps : [];
if (!steps.length) {
  console.error('summary.steps is empty');
  process.exit(20);
}
for (const step of steps) {
  if (step.listPageSize !== 12) {
    console.error(`step ${step.rate} has listPageSize=${step.listPageSize}`);
    process.exit(21);
  }
  for (const target of Object.values(step.targets || {})) {
    const path = String(target.path || '');
    if (/seed|fixture/i.test(path)) {
      console.error(`seed/fixture target detected: ${path}`);
      process.exit(22);
    }
  }
}
if (!summary.nextFocus) {
  console.error('summary.nextFocus missing');
  process.exit(23);
}
console.log(`summary-ok nextFocus=${summary.nextFocus} cleanCeilingRps=${summary.cleanCeilingRps}`);
NODE

manifest="${OUTPUT_DIR%/}/current-main-evidence-manifest.json"
cat >"${manifest}" <<JSON
{
  "generatedAt": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "repoDir": "${REPO_DIR}",
  "mainSha": "${actual_sha}",
  "baseUrl": "${BASE_URL}",
  "preflightLog": "${preflight_log}",
  "frontendImage": "${FRONTEND_IMAGE}",
  "frontendDigest": "${FRONTEND_DIGEST}",
  "backendImage": "${BACKEND_IMAGE}",
  "backendDigest": "${BACKEND_DIGEST}",
  "workReadPath": "${WORK_READ_PATH}",
  "studyReadPath": "${STUDY_READ_PATH}",
  "listPageSize": 12,
  "summaryJson": "${summary_json}",
  "summaryMarkdown": "${summary_md}"
}
JSON

output_parent="$(dirname "${OUTPUT_DIR%/}")"
output_name="$(basename "${OUTPUT_DIR%/}")"
if [[ -z "${ARTIFACT_BUNDLE}" ]]; then
  ARTIFACT_BUNDLE="${output_parent}/current-main-preflight-load-evidence.tgz"
fi
tar -czf "${ARTIFACT_BUNDLE}" -C "${output_parent}" "${output_name}"

info "evidence manifest: ${manifest}"
info "preflight log: ${preflight_log}"
info "summary json: ${summary_json}"
info "summary markdown: ${summary_md}"
info "artifact bundle: ${ARTIFACT_BUNDLE}"
info "PASS"
