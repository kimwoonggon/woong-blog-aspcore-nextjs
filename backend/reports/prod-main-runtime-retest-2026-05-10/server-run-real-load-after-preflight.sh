#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="${REPO_DIR:-/root/service/woong-blog-aspcore-nextjs}"
BASE_URL="${BASE_URL:-https://woonglab.com}"
WORK_READ_PATH="${WORK_READ_PATH:-/api/public/works/smoke-fluid-simulation}"
STUDY_READ_PATH="${STUDY_READ_PATH:-}"
RATES="${RATES:-100 200 300 400}"
DURATION_SECONDS="${DURATION_SECONDS:-30}"
MAX_VUS="${MAX_VUS:-500}"
PRE_ALLOCATED_VUS="${PRE_ALLOCATED_VUS:-100}"
OUTPUT_DIR="${OUTPUT_DIR:-backend/reports/prod-real-load-steps-$(date -u +%Y%m%dT%H%M%SZ)/loadtest}"

cd "${REPO_DIR}"

require_command() {
  local name="$1"
  if ! command -v "${name}" >/dev/null 2>&1; then
    echo "missing required command: ${name}" >&2
    exit 2
  fi
}

reject_seed_path() {
  local label="$1"
  local value="$2"
  case "${value}" in
    *seeded*|*fixture*)
      echo "${label} must be a real public target, not seed/fixture: ${value}" >&2
      exit 3
      ;;
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

require_command git
require_command docker
require_command curl
require_command node
require_command k6

if [[ -z "${STUDY_READ_PATH}" ]]; then
  STUDY_READ_PATH="$(resolve_first_non_seed_public_path '/api/public/blogs?page=1&pageSize=12' '/api/public/blogs/')"
fi

reject_seed_path WORK_READ_PATH "${WORK_READ_PATH}"
reject_seed_path STUDY_READ_PATH "${STUDY_READ_PATH}"

printf 'Using real load targets:\n'
printf '  BASE_URL=%s\n' "${BASE_URL}"
printf '  WORK_READ_PATH=%s\n' "${WORK_READ_PATH}"
printf '  STUDY_READ_PATH=%s\n' "${STUDY_READ_PATH}"
printf '  RATES=%s\n' "${RATES}"
printf '  DURATION_SECONDS=%s\n' "${DURATION_SECONDS}"
printf '  MAX_VUS=%s\n' "${MAX_VUS}"
printf '  PRE_ALLOCATED_VUS=%s\n' "${PRE_ALLOCATED_VUS}"
printf '  OUTPUT_DIR=%s\n' "${OUTPUT_DIR}"

BASE_URL="${BASE_URL}" \
REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1 \
WORK_READ_PATH="${WORK_READ_PATH}" \
./scripts/prod-runtime-preflight.sh

BASE_URL="${BASE_URL}" \
WORK_READ_PATH="${WORK_READ_PATH}" \
STUDY_READ_PATH="${STUDY_READ_PATH}" \
RATES="${RATES}" \
DURATION_SECONDS="${DURATION_SECONDS}" \
MAX_VUS="${MAX_VUS}" \
PRE_ALLOCATED_VUS="${PRE_ALLOCATED_VUS}" \
OUTPUT_DIR="${OUTPUT_DIR}" \
./scripts/prod-real-load-steps.sh
