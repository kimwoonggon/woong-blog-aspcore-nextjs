#!/usr/bin/env bash
set -euo pipefail

# Run this on the production host after changing to the repository directory,
# or set REPO_DIR to the absolute repository path before execution.
export REPO_DIR="${REPO_DIR:-/root/service/woong-blog-aspcore-nextjs}"
export BASE_URL="${BASE_URL:-https://woonglab.com}"

export EXPECTED_MAIN_SHA="4e50c0f899e2bae8b41238fd737a802ccad91a81"
export EXPECTED_BACKEND_IMAGE_DIGEST="sha256:8701e95460c966cb62a6cbf5df5c7471edceb3d8a3fb10411aa9fced03c4c10b"
export EXPECTED_FRONTEND_IMAGE_DIGEST="sha256:5a9b6e3d07b916bbb07c2744dc303bdc2f785a6ba602b9cd7c9ac9730ebc09bc"

export RATES="${RATES:-100 200 300 400}"
export DURATION_SECONDS="${DURATION_SECONDS:-30}"
export MAX_VUS="${MAX_VUS:-500}"
export PRE_ALLOCATED_VUS="${PRE_ALLOCATED_VUS:-100}"

cd "${REPO_DIR}"
exec bash backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh
