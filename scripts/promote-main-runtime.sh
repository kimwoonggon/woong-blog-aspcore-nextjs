#!/usr/bin/env bash
set -euo pipefail

SOURCE_BRANCH="${SOURCE_BRANCH:-dev}"
TARGET_BRANCH="${TARGET_BRANCH:-main}"
WORKTREE_DIR="${WORKTREE_DIR:-../woong-blog-main-runtime}"
ALLOWLIST_FILE="${ALLOWLIST_FILE:-scripts/main-runtime-allowlist.txt}"
PROMOTION_BRANCH="${PROMOTION_BRANCH:-release/main-promote}"
AUTO_COMMIT="${AUTO_COMMIT:-false}"
AUTO_PUSH="${AUTO_PUSH:-false}"
GIT_USER_NAME="${GIT_USER_NAME:-github-actions[bot]}"
GIT_USER_EMAIL="${GIT_USER_EMAIL:-41898282+github-actions[bot]@users.noreply.github.com}"
COMMIT_MESSAGE="${COMMIT_MESSAGE:-Promote runtime-only tree from ${SOURCE_BRANCH} to ${TARGET_BRANCH}}"

ROOT_DIR="$(git rev-parse --show-toplevel)"
ALLOWLIST_PATH="${ROOT_DIR}/${ALLOWLIST_FILE}"

if [[ ! -f "${ALLOWLIST_PATH}" ]]; then
  echo "Allowlist file not found: ${ALLOWLIST_PATH}" >&2
  exit 1
fi

mapfile -t ALLOWLIST < <(grep -vE '^\s*(#|$)' "${ALLOWLIST_PATH}")

if [[ "${#ALLOWLIST[@]}" -eq 0 ]]; then
  echo "Allowlist is empty." >&2
  exit 1
fi

git fetch origin "${SOURCE_BRANCH}" "${TARGET_BRANCH}"

if [[ -d "${WORKTREE_DIR}" ]]; then
  git worktree remove --force "${WORKTREE_DIR}"
fi

git worktree add -B "${PROMOTION_BRANCH}" "${WORKTREE_DIR}" "origin/${TARGET_BRANCH}"

find "${WORKTREE_DIR}" -mindepth 1 -maxdepth 1 ! -name '.git' -exec rm -rf {} +

git archive --format=tar "origin/${SOURCE_BRANCH}" "${ALLOWLIST[@]}" | tar -xf - -C "${WORKTREE_DIR}"

(
  cd "${WORKTREE_DIR}"
  git config user.name "${GIT_USER_NAME}"
  git config user.email "${GIT_USER_EMAIL}"
  git add -A
  if ! git diff --cached --quiet; then
    if [[ "${AUTO_COMMIT}" == "true" ]]; then
      git commit -m "${COMMIT_MESSAGE}"
    fi
    if [[ "${AUTO_PUSH}" == "true" ]]; then
      git push origin HEAD:"${PROMOTION_BRANCH}" --force-with-lease
    fi
  fi
  echo "Prepared runtime-only tree in ${WORKTREE_DIR}"
  echo "Review with: git -C ${WORKTREE_DIR} status --short"
  echo "Push with: git -C ${WORKTREE_DIR} push origin HEAD:${PROMOTION_BRANCH}"
  echo "Then open PR: ${PROMOTION_BRANCH} -> ${TARGET_BRANCH}"
)
