# Dev, Staging, and Main Gate Audit - 2026-04-26

## Summary

The requested guarded flow completed successfully:

1. `origin/dev` passed GitHub Actions.
2. Staging publish from `dev` passed.
3. Main promotion used the protected workflow/PR path.
4. `origin/main` passed main runtime CI after merge.
5. Main GHCR runtime image publish passed.

No direct push to `main` was used.

## What Changed

- Updated the staging GHCR publish workflow so `dev` tags publish to the linked runtime package family:
  - `woong-blog-aspcore-nextjs-runtime-backend`
  - `woong-blog-aspcore-nextjs-runtime-frontend`
- Updated the promotion workflow to run `scripts/promote-main-runtime.sh` through `bash`, avoiding runner failure when the script file mode is not executable.
- Promoted those workflow fixes from `dev` to `main` through `Promote Main Runtime` and protected PR #19.

## What Was Intentionally Not Changed

- No application runtime code was changed during the final gate repair.
- No direct `main` push was performed.
- The user-side GitHub branch protection settings were not modified through the API.
- Production HTTPS defaults were not changed. The repo currently runs production services in `docker-compose.prod.yml`, but nginx still defaults to `./nginx/prod-bootstrap.conf`; switching the default to `./nginx/prod.conf` is a separate deployment behavior change.
- README simplification was not included in the runtime promotion path.

## Validation

- PR #18 merged to `dev` as `6dbcddb63a6edf8fec2a73657e92292f34a87774`.
- `CI Dev` on `origin/dev`, run `24952283239`: success.
- `Publish GHCR Dev` on `dev`, run `24952387360`: success.
- `Promote Main Runtime`, run `24952539505`: success; created `release/main-promote-20260426`.
- PR #19 `CI Main Runtime`, run `24952563035`: success.
- PR #19 merged to `main` as `704ac021e858e45c30295ce1c3d36320e0b42b74`.
- `CI Main Runtime` on `main`, run `24952671567`: success.
- `Publish GHCR Main`, run `24952770138`: success.
  - Backend runtime image job: success in 2m28s.
  - Frontend runtime image job: success in 7m15s.
- Local checks used during the final workflow repair:
  - `bash -n scripts/promote-main-runtime.sh`
  - `git diff --check`

## Goal Check

- Dev before staging: satisfied.
- Staging before main: satisfied.
- Main through protected PR/workflow path: satisfied.
- No direct `main` push: satisfied.
- Broken gate cannot be bypassed by this flow: satisfied operationally when branch protection requires the relevant CI checks before merge.

## Risks and Follow-Up

- GitHub Actions emitted Node.js 20 deprecation warnings for `actions/checkout@v4`, `docker/build-push-action@v5`, `docker/login-action@v3`, and `docker/setup-buildx-action@v3`. This did not fail the gate, but it should be updated before GitHub's Node 20 action runtime removal.
- Earlier stale default-branch workflow runs failed before main had the fixed workflow definition. After PR #19, the current main workflow definition is updated.
- Production HTTPS default is ambiguous: production services and backend HTTPS/HSTS flags are enabled, but nginx defaults to `prod-bootstrap.conf` unless `NGINX_DEFAULT_CONF=./nginx/prod.conf` is supplied.
- Local worktree still has unrelated untracked `.next-playwright-verify/` and `.vscode/` directories.

## Final Recommendation

Keep `dev` protected with `CI Dev` required, keep `main` protected with `CI Main Runtime` required, and require staging publish success as an explicit promotion prerequisite before dispatching the main promotion workflow. Handle the HTTPS nginx default and Node 20 action upgrade as separate follow-up tasks.
