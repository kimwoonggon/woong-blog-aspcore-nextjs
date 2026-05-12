# Current Main Handoff Dynamic Fix - 2026-05-12

## Summary

Updated the current-main server evidence handoff so it no longer embeds a hard-coded default main SHA or fixed image digests. The script now fetches `origin/main`, derives the target SHA at run time, computes immutable `sha-${resolvedShaShort}` runtime image tags, resolves GHCR digests at run time, and still supports `EXPECTED_MAIN_SHA=<40-char-sha>` for exact pinning.

## Changed

- Added a focused Vitest contract test for the handoff artifact behavior.
- Updated `server-current-main-preflight-load-evidence.sh` to resolve latest fetched `origin/main` dynamically.
- Updated handoff Markdown, HTML, and JSON to describe dynamic latest-main behavior.
- Updated the active-goal completion audit to reflect current `main`/GHCR evidence and the dynamic handoff correction.
- Recorded the future UI slice: Work/Study search should update related results while typing, not only after Enter.

## Intentionally Not Changed

- Did not run production SSH or remote server commands.
- Did not run production Real Backend Test from this workspace.
- Did not change cache behavior, seed handling, or `pageSize=12` load-test target rules.
- Did not implement live Work/Study search in this slice.
- Did not mark the active goal complete.

## Goal Verification

- The handoff no longer becomes stale just because the handoff commit itself is promoted to `main`.
- Operators still have an exact pin option through `EXPECTED_MAIN_SHA=<40-char-sha>`.
- Server-side deploy/preflight/load evidence remains required before Real Backend Test results can drive the next backend slice.
- The new live-search requirement is preserved as future scope rather than mixed into this production handoff fix.

## Validations

- `npx vitest run src/test/current-main-server-evidence-handoff.test.ts --pool=threads --maxWorkers=2`: passed, 1 test.
- `bash -n backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh`: passed.
- JSON parse check for current-main handoff and active-goal audit JSON: passed.
- Grep check rejecting hard-coded default SHA and literal `sha-<12hex>` tags in the handoff artifacts: passed.
- `git diff --check`: passed.
- `npx eslint src/test/current-main-server-evidence-handoff.test.ts`: passed.
- GitHub Actions check: `CI Main Runtime` run `25724499083` success for `74bd832467de1094497359a364fa655c340724a2`.
- GitHub Actions check: `Publish GHCR Main` run `25724820291` success for `74bd832467de1094497359a364fa655c340724a2`.
- GHCR manifest check: backend/frontend `sha-74bd832467de` manifests are readable.

## Risks And Follow-Up

- The active goal remains incomplete until server-side deploy/preflight/load evidence is returned.
- If an operator overrides `FRONTEND_IMAGE` or `BACKEND_IMAGE`, the compose config check still expects the derived current-main SHA tag; this is intentional for the current-main handoff path.
- Live Work/Study search needs its own frontend TDD slice with debounce, URL-state behavior, loading state, and Playwright coverage.

## Recommendation

Merge this fix before giving the server command to an operator. After that, use the dynamic handoff script from current `main` and return the generated evidence bundle before selecting the HLS/detail/DB optimization slice.
