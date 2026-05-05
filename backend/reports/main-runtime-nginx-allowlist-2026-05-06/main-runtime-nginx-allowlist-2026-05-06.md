# Main Runtime Nginx Allowlist Fix - 2026-05-06

## Summary
- Added `nginx/nginx.conf` to `scripts/main-runtime-allowlist.txt`.
- This fixes the runtime-only main promotion tree so `docker-compose.prod.yml` can mount the repo-managed nginx main config at `/etc/nginx/nginx.conf`.

## Intentionally Not Changed
- No backend application behavior was changed.
- No public load-test target semantics were changed.
- No `pageSize=12` behavior was changed.
- No cache layer was introduced.
- No Npgsql/nginx runtime setting values were changed in this slice.

## Goal Verification
- PR #47 failed because the runtime-only tree did not contain `nginx/nginx.conf` while `docker-compose.prod.yml` mounted it.
- The allowlist now includes `nginx/nginx.conf`.
- A generated runtime verification tree contains `nginx/nginx.conf`.
- `docker compose config` resolves the nginx main config mount from the runtime verification tree.

## Validations
- RED: `grep -Fx 'nginx/nginx.conf' scripts/main-runtime-allowlist.txt` failed before the edit.
- GREEN: `grep -Fx 'nginx/nginx.conf' scripts/main-runtime-allowlist.txt` passed after the edit.
- PASS: `git archive --format=tar HEAD $(grep -vE '^\\s*(#|$)' scripts/main-runtime-allowlist.txt) | tar -tf - | rg '^nginx/nginx\\.conf$'`.
- PASS: `git diff --check`.
- PASS: `SOURCE_REF=HEAD AUTO_COMMIT=false AUTO_PUSH=false WORKTREE_DIR=../woong-blog-main-runtime-verify PROMOTION_BRANCH=verify/main-runtime-nginx-conf ./scripts/promote-main-runtime.sh`.
- PASS: `test -f ../woong-blog-main-runtime-verify/nginx/nginx.conf`.
- PASS: `docker compose --env-file .env.prod -f docker-compose.prod.yml config` inside the verification runtime tree resolved the source path for `/etc/nginx/nginx.conf` and preserved the Npgsql pool cap string.

## Risks And Follow-Up
- This only fixes the promotion artifact. It does not by itself improve backend throughput.
- The runtime-only PR must still be regenerated from updated `dev` and pass GitHub Actions before `main` is correct.

## Recommendation
- Merge this allowlist fix into `dev`, rerun `dev` CI, regenerate PR #47 from `dev`, and rerun `main` runtime CI.
