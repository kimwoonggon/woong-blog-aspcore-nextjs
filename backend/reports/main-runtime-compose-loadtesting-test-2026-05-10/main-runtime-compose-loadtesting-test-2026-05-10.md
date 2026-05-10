# Main Runtime Compose LoadTesting Test Stability - 2026-05-10

## Scope
- Fix the `CI Main Runtime` frontend unit-test blocker on PR #123.
- Keep the `LoadTesting__BaseUrl` compose contract covered on `dev` while allowing the runtime-only `main` tree to omit staging-only files.
- No backend production code, Docker runtime behavior, nginx config, or load-test runner behavior was changed.

## Root Cause
PR #123 promotes a runtime-only tree from `dev` to `main`. That tree includes `src/test/compose-loadtesting-baseurl.test.ts` because `src` is allowlisted, but intentionally does not include `docker-compose.staging.yml` or `.env.staging.example`. The new test unconditionally read those staging files, so `CI Main Runtime` failed in Vitest with `ENOENT`.

Failing evidence:
- PR #123 `Frontend lint, types, and unit tests` failed in run `25620049774`, job `75204885360`.
- Failed test file: `src/test/compose-loadtesting-baseurl.test.ts`.
- Failure: missing `docker-compose.staging.yml` and `.env.staging.example` in the runtime-only checkout.

## Changed
- `src/test/compose-loadtesting-baseurl.test.ts`
  - Added file-existence aware compose/env contract selection.
  - Kept production contract mandatory.
  - Preserved dev coverage for staging when both staging files are present.
  - Added a guard that fails if a compose file and matching env example are promoted inconsistently.

## Intentionally Not Changed
- Did not add staging compose/env files to the main runtime allowlist.
- Did not change `docker-compose.prod.yml`, `docker-compose.staging.yml`, `.env.prod.example`, or `.env.staging.example`.
- Did not change publish, promotion, or CI workflow behavior.
- Did not change any backend query, DTO, load-test, nginx, Docker, or runtime code.

## Goal Verification
- `dev` coverage remains intact: prod and staging compose/env contracts are both verified when files exist.
- `main` runtime tree is now supported: prod contract remains verified and missing staging-only files no longer fail the unit suite.
- This unblocks the promotion chain needed for the ongoing backend performance loop.

## Validations
- PASS: `npm test -- --run src/test/compose-loadtesting-baseurl.test.ts` passed 5/5 on the dev tree.
- PASS: A minimal main-runtime-style tree without staging compose/env files passed `npm test -- --run src/test/compose-loadtesting-baseurl.test.ts` 3/3.
- PASS: `npm test -- --run src/test/compose-loadtesting-baseurl.test.ts src/test/prod-runtime-preflight.test.ts src/test/prod-real-load-steps.test.ts` passed 14/14.
- PASS: `npm run typecheck` passed.
- PASS: `git diff --check -- src/test/compose-loadtesting-baseurl.test.ts todolist-2026-05-10.md` passed.
- PASS: `gh pr checks 123` and job logs were inspected to confirm the root cause.

## Risks And Follow-Up
- PR #123 will not recover until this fix is merged into `dev` and a new `Promote Main Runtime` run refreshes `release/main-promote`.
- Browser smoke on PR #123 was skipped only because frontend unit tests failed; it must be rechecked after the refreshed promotion PR CI runs.
- This is a CI/promote-tree stability fix, not a new backend performance optimization slice.

## Recommendation
Merge this fix through `dev`, wait for `CI Dev`, `Publish GHCR Dev`, `Promote Main Runtime`, and the refreshed `CI Main Runtime` checks, then continue the next backend heavy detail read optimization slice.
