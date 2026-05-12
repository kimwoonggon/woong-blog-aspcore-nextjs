# Current Main Handoff Digest Resolution Audit - 2026-05-13

## Summary

The current-main server evidence handoff script now resolves GHCR OCI index digests first, then falls back to platform manifest digests only when Docker Buildx is unavailable.

This prevents exact pinned server handoff runs from failing before deploy when the expected digest comes from the published GHCR image index while `docker manifest inspect` returns the linux/amd64 platform manifest digest.

## Changed

- `backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh`
  - `resolve_image_digest` now tries `docker --config "${DOCKER_CONFIG_DIR}" buildx imagetools inspect "${image}"` and parses the top-level `Digest:` value.
  - Existing `docker --config "${DOCKER_CONFIG_DIR}" manifest inspect "${image}"` platform digest parsing remains as fallback.
- `backend/reports/current-main-server-evidence-handoff-2026-05-12/current-main-server-evidence-handoff-2026-05-12.md`
  - Documents OCI index digest resolution and fallback behavior.
- `backend/reports/current-main-server-evidence-handoff-2026-05-12/current-main-server-evidence-handoff-2026-05-12.json`
  - Updates the guarantee text.
- `backend/reports/active-goal-runtime-redeploy-blocker-post-publish-2026-05-13/`
  - Records the digest-resolution recheck in the active-goal blocker audit.
- `todolist-2026-05-13.md`
  - Records the fix and validation.

## Intentionally Not Changed

- No production SSH, server deploy, Docker compose runtime, preflight, or real load execution was performed.
- The exact `main` SHA and published image digests were not changed.
- The handoff still remains blocked until production SSH secrets or returned server evidence are available.

## Evidence

- `origin/main` after fetch: `4e50c0f899e2bae8b41238fd737a802ccad91a81`
- Backend OCI index digest: `sha256:8701e95460c966cb62a6cbf5df5c7471edceb3d8a3fb10411aa9fced03c4c10b`
- Backend linux/amd64 platform manifest digest: `sha256:52090d120d9c460fc01968a0f4763f82fe3c715a561ecf2d58203d2f5043fa34`
- Frontend OCI index digest: `sha256:5a9b6e3d07b916bbb07c2744dc303bdc2f785a6ba602b9cd7c9ac9730ebc09bc`
- Frontend linux/amd64 platform manifest digest: `sha256:e8a44ac671d85ae1a25d9e3a14894c0d7c1aef327ad4ca0fd6349f265aaa5a8b`

## Validations

- Passed: `bash -n backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh`
- Passed: `bash -n backend/reports/active-goal-runtime-redeploy-blocker-post-publish-2026-05-13/server-run-current-main-4e50c0f-preflight-load.sh`
- Passed: JSON parse for current-main handoff and active-goal blocker JSON artifacts.
- Passed: `docker --config /tmp buildx imagetools inspect ... | awk '/^Digest:/'` returned the expected backend/frontend OCI index digests.
- Passed: `git diff --check` for the modified handoff, blocker audit, and TODO files.

## Remaining Blocker

The active goal is still not complete. Production server pull/deploy, production preflight, and Real Backend Test evidence are still missing because production SSH secrets and local secret material remain unavailable.

## Recommendation

Keep this handoff fix. Once production SSH secrets are configured or the server-side script is run manually, use the fixed handoff to produce `current-main-preflight-load-evidence.tgz` and then verify it with `scripts/prod-runtime-evidence-verify.sh`.
