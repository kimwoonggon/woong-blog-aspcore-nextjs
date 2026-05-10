# Staging LoadTesting Base URL CI Fix - 2026-05-10

## Scope
- Fix the `Publish GHCR Dev` failure caused by staging compose not exposing `LoadTesting__BaseUrl`.
- Keep Real Backend Test routing through the nginx/public origin, not backend-direct.
- Preserve the existing prod compose behavior.

## Changed
- Added `LoadTesting__BaseUrl` to `docker-compose.staging.yml` backend environment with this precedence:
  - explicit `LoadTesting__BaseUrl`
  - `NEXT_PUBLIC_SITE_URL`
  - `https://staging.example.com`
- Added the same staging default to `.env.staging.example`.
- Added `src/test/compose-loadtesting-baseurl.test.ts` to lock prod and staging compose/env examples to the public nginx-origin contract.
- Updated `todolist-2026-05-10.md` with the RED/GREEN/verification trail.

## Not Changed
- No backend application code changed.
- No HLS transcoding logic changed.
- No prod compose behavior changed beyond existing expectations.
- No direct backend load-test URL was introduced.

## Verification
- RED observed: focused Vitest failed for `docker-compose.staging.yml` and `.env.staging.example` before implementation.
- PASS: `npm test -- --run src/test/compose-loadtesting-baseurl.test.ts`.
- PASS: `docker compose --env-file <tmp> -f docker-compose.staging.yml config | grep -F 'LoadTesting__BaseUrl: http://localhost'`.
- PASS: `bash -n scripts/ci-compose-smoke.sh scripts/prod-runtime-preflight.sh scripts/prod-real-load-steps.sh`.
- PASS: `git diff --check -- docker-compose.staging.yml .env.staging.example src/test/compose-loadtesting-baseurl.test.ts todolist-2026-05-10.md`.
- PASS: `npm test -- --run src/test/compose-loadtesting-baseurl.test.ts src/test/prod-runtime-preflight.test.ts src/test/prod-real-load-steps.test.ts`.
- PASS: `npm run typecheck`.

## Risks And Follow-Up
- The failed `Publish GHCR Dev` run must be superseded by a new dev run after this fix lands on `dev`.
- Main promotion already carried the previous runtime preflight guard; this staging-only fix still needs normal dev CI and image publish confirmation.
- This does not resolve the HLS upload root cause. The HLS diagnosis remains: capture the actual upload endpoint status/body/logs, especially 413/504/HTML vs backend JSON ffmpeg errors.

## Recommendation
Merge this fix through `dev`, confirm `Publish GHCR Dev` passes on the next workflow run, then continue HLS remediation using the captured upload response or server logs.
