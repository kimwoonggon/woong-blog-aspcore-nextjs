#!/usr/bin/env bash
set -euo pipefail
trap 'echo "prod-runtime-evidence-verify failed at line ${LINENO}" >&2' ERR

EVIDENCE_INPUT="${1:-${EVIDENCE_DIR:-}}"
EXPECTED_MAIN_SHA="${EXPECTED_MAIN_SHA:-}"
EXPECTED_BACKEND_IMAGE_DIGEST="${EXPECTED_BACKEND_IMAGE_DIGEST:-}"
EXPECTED_FRONTEND_IMAGE_DIGEST="${EXPECTED_FRONTEND_IMAGE_DIGEST:-}"
FAIL_RATE_LIMIT="${FAIL_RATE_LIMIT:-0.001}"
DROPPED_ITERATION_LIMIT="${DROPPED_ITERATION_LIMIT:-0}"

fail() {
  echo "[prod-runtime-evidence-verify] ERROR: $*" >&2
  exit 1
}

info() {
  echo "[prod-runtime-evidence-verify] $*"
}

require_file() {
  local label="$1"
  local path="$2"
  if [[ ! -f "${path}" ]]; then
    fail "${label} does not exist: ${path}"
  fi
}

require_log_line() {
  local log_path="$1"
  local label="$2"
  local pattern="$3"
  if ! grep -Eq "${pattern}" "${log_path}"; then
    fail "preflight log is missing ${label}"
  fi
}

if [[ -z "${EVIDENCE_INPUT}" ]]; then
  fail "EVIDENCE_DIR or first argument is required"
fi

WORK_DIR=""
cleanup() {
  if [[ -n "${WORK_DIR}" && -d "${WORK_DIR}" ]]; then
    rm -rf "${WORK_DIR}"
  fi
}
trap cleanup EXIT

EVIDENCE_DIR="${EVIDENCE_INPUT%/}"
if [[ -f "${EVIDENCE_INPUT}" ]]; then
  WORK_DIR="$(mktemp -d)"
  tar -xzf "${EVIDENCE_INPUT}" -C "${WORK_DIR}"
  EVIDENCE_DIR="${WORK_DIR}"
elif [[ -d "${EVIDENCE_DIR}" && ! -f "${EVIDENCE_DIR}/prod-runtime-preflight.log" && -f "${EVIDENCE_DIR}/production-runtime-evidence.tar.gz" ]]; then
  WORK_DIR="$(mktemp -d)"
  tar -xzf "${EVIDENCE_DIR}/production-runtime-evidence.tar.gz" -C "${WORK_DIR}"
  EVIDENCE_DIR="${WORK_DIR}"
elif [[ ! -d "${EVIDENCE_DIR}" ]]; then
  fail "EVIDENCE_DIR does not exist: ${EVIDENCE_INPUT}"
fi

PREFLIGHT_LOG="${EVIDENCE_DIR}/prod-runtime-preflight.log"
REAL_LOAD_JSON="${EVIDENCE_DIR}/prod-real-load-steps-summary.json"
REAL_LOAD_MD="${EVIDENCE_DIR}/prod-real-load-steps-summary.md"
MANIFEST_JSON="${EVIDENCE_DIR}/production-runtime-evidence-manifest.json"
SUMMARY_MD="${EVIDENCE_DIR}/production-runtime-evidence-summary.md"

require_file "prod-runtime-preflight.log" "${PREFLIGHT_LOG}"
require_file "prod-real-load-steps-summary.json" "${REAL_LOAD_JSON}"
require_file "prod-real-load-steps-summary.md" "${REAL_LOAD_MD}"
require_file "production-runtime-evidence-manifest.json" "${MANIFEST_JSON}"
require_file "production-runtime-evidence-summary.md" "${SUMMARY_MD}"

require_log_line "${PREFLIGHT_LOG}" "PASS" '\[prod-runtime-preflight\] PASS'
require_log_line "${PREFLIGHT_LOG}" "nginx timing" 'nginx request_time header: available'
require_log_line "${PREFLIGHT_LOG}" "app timing" 'app elapsed header: available'
require_log_line "${PREFLIGHT_LOG}" "gzip public response" 'gzip public response: available'
require_log_line "${PREFLIGHT_LOG}" "public Work list contract" 'public Work list contract: current'
require_log_line "${PREFLIGHT_LOG}" "public Work detail contract" 'public Work detail contract: current'

node - "${MANIFEST_JSON}" "${REAL_LOAD_JSON}" "${EXPECTED_MAIN_SHA}" "${EXPECTED_BACKEND_IMAGE_DIGEST}" "${EXPECTED_FRONTEND_IMAGE_DIGEST}" "${FAIL_RATE_LIMIT}" "${DROPPED_ITERATION_LIMIT}" <<'NODE'
const fs = require('node:fs')

const [
  manifestPath,
  realLoadPath,
  expectedMainSha,
  expectedBackendDigest,
  expectedFrontendDigest,
  failRateLimitRaw,
  droppedIterationLimitRaw,
] = process.argv.slice(2)

const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'))
const realLoad = JSON.parse(fs.readFileSync(realLoadPath, 'utf8'))
const failRateLimit = Number(failRateLimitRaw)
const droppedIterationLimit = Number(droppedIterationLimitRaw)

function fail(message) {
  console.error(`[prod-runtime-evidence-verify] ERROR: ${message}`)
  process.exit(1)
}

function requireEqual(label, actual, expected) {
  if (expected && String(actual || '') !== expected) {
    fail(`${label} mismatch: expected ${expected}, got ${actual || 'missing'}`)
  }
}

function valuesFromTargets(targets) {
  return Object.entries(targets || {}).map(([key, value]) => ({
    key,
    path: String(value?.path || ''),
  }))
}

requireEqual('main SHA', manifest.mainSha, expectedMainSha)
requireEqual('backend image digest', manifest.images?.backendDigest, expectedBackendDigest)
requireEqual('frontend image digest', manifest.images?.frontendDigest, expectedFrontendDigest)

if (manifest.preflight?.passed !== true) {
  fail('manifest preflight.passed must be true')
}

if (!String(realLoad.baseUrl || '').match(/^https:\/\//i)) {
  fail('real load summary baseUrl must use public HTTPS origin')
}

if (String(realLoad.baseUrl || '').match(/^https?:\/\/(backend|127\.0\.0\.1|localhost)(?::\d+)?/i)) {
  fail('real load summary baseUrl bypasses public nginx origin')
}

if (!Array.isArray(realLoad.steps) || realLoad.steps.length === 0) {
  fail('real load summary has no steps')
}

const summaryListPageSize = Number(realLoad.listPageSize ?? 12)
if (summaryListPageSize !== 12) {
  fail('real load summary must use listPageSize=12')
}

for (const [index, step] of realLoad.steps.entries()) {
  const listPageSize = Number(step.listPageSize ?? realLoad.listPageSize)
  if (listPageSize !== 12) {
    fail(`step ${index + 1} must use listPageSize=12`)
  }

  const targets = valuesFromTargets(step.targets)
  if (targets.length === 0) {
    fail(`step ${index + 1} has no targets`)
  }

  for (const target of targets) {
    if (/seed|fixture/i.test(target.path)) {
      fail(`step ${index + 1} contains seed/fixture target: ${target.path}`)
    }
  }

  const workList = targets.find((target) => target.key === 'work_list')?.path
  const studyList = targets.find((target) => target.key === 'study_list')?.path
  const workRead = targets.find((target) => target.key === 'work_read')?.path
  const studyRead = targets.find((target) => target.key === 'study_read')?.path

  if (workList !== '/api/public/works?page=1&pageSize=12') {
    fail(`step ${index + 1} work_list must be /api/public/works?page=1&pageSize=12`)
  }
  if (studyList !== '/api/public/blogs?page=1&pageSize=12') {
    fail(`step ${index + 1} study_list must be /api/public/blogs?page=1&pageSize=12`)
  }
  if (!String(workRead || '').startsWith('/api/public/works/')) {
    fail(`step ${index + 1} work_read must be a public Work detail path`)
  }
  if (!String(studyRead || '').startsWith('/api/public/blogs/')) {
    fail(`step ${index + 1} study_read must be a public Study detail path`)
  }

  const failedRate = Number(step.http?.failedRate ?? 0)
  const droppedIterations = Number(step.http?.droppedIterations ?? 0)
  if (failedRate > failRateLimit) {
    fail(`step ${index + 1} failedRate ${failedRate} exceeds ${failRateLimit}`)
  }
  if (droppedIterations > droppedIterationLimit) {
    fail(`step ${index + 1} droppedIterations ${droppedIterations} exceeds ${droppedIterationLimit}`)
  }
}
NODE

info "evidence: ${EVIDENCE_INPUT}"
info "main SHA: ${EXPECTED_MAIN_SHA:-not checked}"
info "PASS"
