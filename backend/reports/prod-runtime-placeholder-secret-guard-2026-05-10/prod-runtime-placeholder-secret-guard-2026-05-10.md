# Production Runtime Placeholder Secret Guard - 2026-05-10

Generated at: 2026-05-10T21:53:54+09:00

## Summary

Added a safety guard to the manual production runtime redeploy workflow so obvious placeholder SSH secret values fail before key material is written or SSH host-key checks run.

## Changed

- `.github/workflows/prod-runtime-redeploy.yml`
  - Added `reject_placeholder_secret` in `Prepare SSH`.
  - Validates `PROD_SSH_HOST`, `PROD_SSH_USER`, `PROD_SSH_PRIVATE_KEY`, `PROD_SSH_KNOWN_HOSTS`, and optional `PROD_SSH_PORT`.
  - Rejects values such as `<...>`, `production-ssh-*`, `read-packages-token`, `CHANGE_ME`, `TODO`, `placeholder`, `changeme`, `change-me`, and `example.com`.
  - Does not echo secret values in logs.
- `src/test/prod-runtime-redeploy-workflow.test.ts`
  - Added workflow contract coverage for the placeholder guard and non-leaking error message.
- `todolist-2026-05-10.md`
  - Recorded RED/GREEN/verification progress.

## Intentionally Not Changed

- Did not add or modify real production SSH secrets.
- Did not run production deploy/preflight/load.
- Did not change load targets, `pageSize=12`, cache behavior, or seed/fixture policy.
- Did not select HLS/body/DB next slice because no post-deploy production load result exists.

## Verification

- RED: `npm test -- --run src/test/prod-runtime-redeploy-workflow.test.ts` failed before implementation because `reject_placeholder_secret` was missing.
- PASS: `npm test -- --run src/test/prod-runtime-redeploy-workflow.test.ts` passed 1/1.
- PASS: `npm test -- --run src/test/prod-runtime-redeploy-workflow.test.ts src/test/publish-ghcr-tags.test.ts src/test/compose-loadtesting-baseurl.test.ts` passed 8/8.
- PASS: extracted remote heredoc `bash -n /tmp/prod-runtime-redeploy-remote.sh` passed.
- PASS: extracted `Prepare SSH` shell block `bash -n` passed.
- PASS: `npm run typecheck` passed.
- PASS: `git diff --check -- .github/workflows/prod-runtime-redeploy.yml src/test/prod-runtime-redeploy-workflow.test.ts todolist-2026-05-10.md` passed.

## Risks And Follow-Up

- This guard only blocks obvious placeholders. It cannot prove a secret is valid production SSH material.
- The active production objective remains blocked until real production SSH secrets are added or the server-console fallback is executed.
- This workflow change still needs normal dev/main CI promotion if it is to protect future production redeploy attempts on `main`.

## Final Recommendation

Promote this guard through the normal CI path, then add real production SSH secrets and rerun `prod-runtime-redeploy.yml`. If secrets cannot be added, execute `pasteable-server-commands.md` on the production host and provide the generated outputs/artifacts.
