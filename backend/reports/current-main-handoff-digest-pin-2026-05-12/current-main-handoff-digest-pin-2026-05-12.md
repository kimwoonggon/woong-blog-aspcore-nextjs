# Current Main Handoff Digest Pin Audit 2026-05-12

## Objective

Strengthen the server-side current-main evidence handoff so a server run can fail immediately when the resolved GHCR runtime image digests do not match the immutable digests expected from the latest successful `main` publish.

## Changed

- Added optional `EXPECTED_BACKEND_IMAGE_DIGEST` and `EXPECTED_FRONTEND_IMAGE_DIGEST` inputs to `server-current-main-preflight-load-evidence.sh`.
- Added fail-fast checks after GHCR manifest digest resolution and before `docker compose pull`.
- Updated current-main handoff Markdown, HTML, and JSON to document digest pin usage and the fail-fast guarantee.
- Updated the post-main active-goal audit command to include the expected backend/frontend digests for `main` SHA `3885eaeb16266de752fc984258bc30aafd3d7a62`.
- Added focused Vitest contract coverage for the new expected digest inputs and mismatch checks.

## Intentionally Not Changed

- No production SSH was used.
- No server-side deploy, production preflight, or Real Backend Test was executed from this workspace.
- No cache workaround, seed/fixture target, or `pageSize=12` criterion was changed.
- No backend optimization slice was selected because returned server evidence is still missing.

## Requirement Verification

- Requirement: server handoff can pin expected immutable image digests.
  Evidence: script now reads `EXPECTED_BACKEND_IMAGE_DIGEST` and `EXPECTED_FRONTEND_IMAGE_DIGEST`.
- Requirement: mismatch fails before deploy/load execution proceeds.
  Evidence: script compares resolved `BACKEND_DIGEST`/`FRONTEND_DIGEST` to expected values immediately after resolution and calls `fail` on mismatch before compose pull/up.
- Requirement: operator guidance includes digest pins.
  Evidence: handoff Markdown/HTML/JSON and post-main audit command include expected digest usage.
- Requirement: no target weakening.
  Evidence: no changes were made to `LIST_PAGE_SIZE=12`, seed/fixture rejection, public-origin target selection, or verifier load-result checks.

## Validations

- RED: `npx vitest run src/test/current-main-server-evidence-handoff.test.ts --pool=threads --maxWorkers=2` failed before implementation because the script lacked `EXPECTED_*_IMAGE_DIGEST` inputs.
- GREEN: `npx vitest run src/test/current-main-server-evidence-handoff.test.ts --pool=threads --maxWorkers=2` passed, 2 tests.
- Shell syntax: `bash -n backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh` passed.
- JSON parse: current-main handoff JSON and post-main active-goal audit JSON parsed successfully.
- ESLint: `npx eslint src/test/current-main-server-evidence-handoff.test.ts` passed.

## Risks And Follow-Up

- The active goal remains incomplete until the server-side evidence bundle is returned and verified.
- The server script resolves GHCR manifest digests before `docker compose pull`; the returned bundle verifier should still be run afterward with the same expected digests.
- Public-origin health/list probes remain proxy signals only and are not accepted as completion evidence.

## Recommendation

Use the digest-pinned server handoff command for the next server run, then verify the returned `current-main-preflight-load-evidence.tgz` with the same expected SHA and digests.
