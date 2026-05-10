# GHCR Runtime Legacy Image Alias - Audit Report

## Summary

Added backward-compatible GHCR image aliases to the dev/main publish workflows.

The current publish workflows emitted only `woong-blog-aspcore-nextjs-runtime-frontend/backend` tags, while older server/staging env files can still point to `woong-blog-aspcore-nextjs-frontend/backend`. This mismatch can leave production running stale images even after main CI and GHCR publish succeed.

## Changed

- `.github/workflows/publish-ghcr-main.yml`
  - Keeps current runtime tags: `repo-runtime-frontend/backend:main`, `:sha-*`, `:latest`.
  - Adds legacy aliases: `repo-frontend/backend:main`, `:sha-*`, `:latest`.
- `.github/workflows/publish-ghcr-dev.yml`
  - Keeps current runtime tags: `repo-runtime-frontend/backend:dev`, `:dev-sha-*`.
  - Adds legacy aliases: `repo-frontend/backend:dev`, `:dev-sha-*`.
- `src/test/publish-ghcr-tags.test.ts`
  - Adds static workflow contract coverage for both current runtime tags and legacy compose-compatible aliases.

## Intentionally Not Changed

- No compose service image defaults were changed.
- No secrets or actual `.env.prod` values were modified.
- No server deployment was performed from this environment.
- No cache behavior was added.
- No Real Backend Test result was fabricated or interpreted from stale runtime.

## Verification

- RED: `npm test -- --run src/test/publish-ghcr-tags.test.ts` failed 2/2 before workflow changes because legacy aliases were absent.
- PASS: `npm test -- --run src/test/publish-ghcr-tags.test.ts` passed 2/2 after workflow changes.
- PASS: `npm test -- --run src/test/publish-ghcr-tags.test.ts src/test/compose-loadtesting-baseurl.test.ts` passed 7/7.
- PASS: `npm run typecheck` passed.
- PASS: `npm run lint` passed with existing warnings only: 0 errors, 7 warnings.
- PASS: `git diff --check` passed for changed workflow/test/TODO files.

## Risks And Follow-Up

- This does not deploy the server by itself. The production host still needs `docker compose pull/up` or an equivalent deployment action.
- Existing stale production responses prove the current server has not yet been recreated from latest runtime images.
- Publishing aliases is backward-compatible, but the repo should eventually standardize env examples and server docs around one image naming scheme.

## Recommendation

Push this slice through `dev` and `main` so the next GHCR publish updates both runtime and legacy image names. Then run the production redeploy/preflight/load commands from the server.
