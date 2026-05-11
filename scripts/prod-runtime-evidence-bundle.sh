#!/usr/bin/env bash
set -euo pipefail
trap 'echo "prod-runtime-evidence-bundle failed at line ${LINENO}" >&2' ERR

PREFLIGHT_LOG="${PREFLIGHT_LOG:-}"
REAL_LOAD_DIR="${REAL_LOAD_DIR:-}"
OUTPUT_DIR="${OUTPUT_DIR:-backend/reports/production-runtime-evidence-$(date -u +%Y%m%dT%H%M%SZ)}"
MAIN_SHA="${MAIN_SHA:-}"
BACKEND_IMAGE_DIGEST="${BACKEND_IMAGE_DIGEST:-}"
FRONTEND_IMAGE_DIGEST="${FRONTEND_IMAGE_DIGEST:-}"

fail() {
  echo "[prod-runtime-evidence-bundle] ERROR: $*" >&2
  exit 1
}

info() {
  echo "[prod-runtime-evidence-bundle] $*"
}

require_file() {
  local name="$1"
  local value="$2"
  if [[ -z "${value}" ]]; then
    fail "${name} is required"
  fi
  if [[ ! -f "${value}" ]]; then
    fail "${name} does not exist: ${value}"
  fi
}

require_dir() {
  local name="$1"
  local value="$2"
  if [[ -z "${value}" ]]; then
    fail "${name} is required"
  fi
  if [[ ! -d "${value}" ]]; then
    fail "${name} does not exist: ${value}"
  fi
}

require_log_line() {
  local label="$1"
  local pattern="$2"
  if ! grep -Eq "${pattern}" "${PREFLIGHT_LOG}"; then
    fail "preflight log is missing ${label}"
  fi
}

require_file "PREFLIGHT_LOG" "${PREFLIGHT_LOG}"
require_dir "REAL_LOAD_DIR" "${REAL_LOAD_DIR}"

REAL_LOAD_JSON="${REAL_LOAD_DIR%/}/prod-real-load-steps-summary.json"
REAL_LOAD_MD="${REAL_LOAD_DIR%/}/prod-real-load-steps-summary.md"
require_file "prod-real-load-steps-summary.json" "${REAL_LOAD_JSON}"
require_file "prod-real-load-steps-summary.md" "${REAL_LOAD_MD}"

require_log_line "PASS" '\[prod-runtime-preflight\] PASS'
require_log_line "nginx timing" 'nginx request_time header: available'
require_log_line "app timing" 'app elapsed header: available'
require_log_line "gzip public response" 'gzip public response: available'
require_log_line "public Work list contract" 'public Work list contract: current'
require_log_line "public Work detail contract" 'public Work detail contract: current'

if [[ -z "${MAIN_SHA}" ]]; then
  MAIN_SHA="$(git rev-parse HEAD 2>/dev/null || echo unavailable)"
fi

mkdir -p "${OUTPUT_DIR}"
MANIFEST_JSON="${OUTPUT_DIR%/}/production-runtime-evidence-manifest.json"
SUMMARY_MD="${OUTPUT_DIR%/}/production-runtime-evidence-summary.md"
TARBALL="${OUTPUT_DIR%/}/production-runtime-evidence.tar.gz"
STAGING_DIR="$(mktemp -d)"

cleanup() {
  rm -rf "${STAGING_DIR}"
}
trap cleanup EXIT

node - "${REAL_LOAD_JSON}" "${MANIFEST_JSON}" "${SUMMARY_MD}" "${MAIN_SHA}" "${BACKEND_IMAGE_DIGEST}" "${FRONTEND_IMAGE_DIGEST}" <<'NODE'
const fs = require('node:fs')

const [summaryPath, manifestPath, markdownPath, mainSha, backendDigest, frontendDigest] = process.argv.slice(2)
const summary = JSON.parse(fs.readFileSync(summaryPath, 'utf8'))

function fail(message) {
  console.error(`[prod-runtime-evidence-bundle] ERROR: ${message}`)
  process.exit(1)
}

function valuesFromTargets(targets) {
  return Object.values(targets || {})
    .map((target) => String(target?.path || ''))
    .filter(Boolean)
}

const targetPaths = summary.steps.flatMap((step) => valuesFromTargets(step.targets))
const hasSeedOrFixture = targetPaths.some((target) => /seed|fixture/i.test(target))
if (hasSeedOrFixture) {
  fail('real load summary contains seed/fixture target')
}

const listPageSizes = summary.steps
  .map((step) => step.listPageSize ?? summary.listPageSize)
  .filter((value) => value !== undefined && value !== null)
if (listPageSizes.length === 0 || listPageSizes.some((value) => Number(value) !== 12)) {
  fail('real load summary must use listPageSize=12')
}

if (!summary.steps.length) {
  fail('real load summary has no steps')
}

if (String(summary.baseUrl || '').match(/^https?:\/\/(backend|127\.0\.0\.1|localhost):?8080/i)) {
  fail('real load summary baseUrl bypasses public nginx origin')
}

const manifest = {
  generatedAt: new Date().toISOString(),
  mainSha,
  images: {
    backendDigest: backendDigest || null,
    frontendDigest: frontendDigest || null,
  },
  preflight: {
    passed: true,
  },
  realLoad: {
    baseUrl: summary.baseUrl,
    cleanCeilingRps: summary.cleanCeilingRps,
    firstSaturationRate: summary.firstSaturationRate,
    nextFocus: summary.nextFocus,
    listPageSize: 12,
    stepCount: summary.steps.length,
    rates: summary.steps.map((step) => step.rate),
  },
}

const markdown = [
  '# Production Runtime Evidence Summary',
  '',
  `- Main SHA: \`${mainSha}\``,
  `- Backend image digest: \`${backendDigest || 'not supplied'}\``,
  `- Frontend image digest: \`${frontendDigest || 'not supplied'}\``,
  '- Preflight: `PASS`',
  `- Real load base URL: \`${summary.baseUrl}\``,
  `- Clean ceiling RPS: \`${summary.cleanCeilingRps}\``,
  `- First saturation rate: \`${summary.firstSaturationRate ?? 'unavailable'}\``,
  `- Next focus: \`${summary.nextFocus}\``,
  '- List page size: `12`',
  '',
  'Use this bundle as evidence input for selecting the next slice. Do not select a performance slice from CI or image manifests alone.',
  '',
].join('\n')

fs.writeFileSync(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`)
fs.writeFileSync(markdownPath, markdown)
NODE

cp "${PREFLIGHT_LOG}" "${STAGING_DIR}/prod-runtime-preflight.log"
cp "${REAL_LOAD_JSON}" "${STAGING_DIR}/prod-real-load-steps-summary.json"
cp "${REAL_LOAD_MD}" "${STAGING_DIR}/prod-real-load-steps-summary.md"
cp "${MANIFEST_JSON}" "${STAGING_DIR}/production-runtime-evidence-manifest.json"
cp "${SUMMARY_MD}" "${STAGING_DIR}/production-runtime-evidence-summary.md"

tar -czf "${TARBALL}" -C "${STAGING_DIR}" .

info "manifest: ${MANIFEST_JSON}"
info "summary: ${SUMMARY_MD}"
info "tarball: ${TARBALL}"
info "PASS"
