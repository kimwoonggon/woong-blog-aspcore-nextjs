# Production Runtime Redeploy SSH Secret Blocker - 2026-05-10

Generated at: 2026-05-10T21:23:36+09:00

## Objective Restated

The active objective requires the production server to pull/deploy the latest `main` runtime images, pass production preflight, rerun Real Backend Test with `pageSize=12`, no seed, no cache workaround, and real Work/Study URLs, then select and complete the next backend performance/fix slice with full e2e and CI success.

## Current Decision

The objective is not complete. Production deploy/preflight/load have not run because required GitHub production SSH secrets are missing.

## Evidence

- Workflow: `Production Runtime Redeploy`
- First attempted run: `25628508822`
- Additional accidental duplicate run: `25628614468`
- Additional accidental setup-placeholder run: `25628772593`
- Latest run URL: https://github.com/kimwoonggon/woong-blog-aspcore-nextjs/actions/runs/25628772593
- Branch: `main`
- Head SHA: `757ab203e55cadf8f89ee0da42b7ef580deebad3`
- Current `origin/main`: `757ab203e55cadf8f89ee0da42b7ef580deebad3`
- `CI Main Runtime` run `25628240637`: success for `757ab203e55cadf8f89ee0da42b7ef580deebad3`
- `Publish GHCR Main` run `25628355351`: success for `757ab203e55cadf8f89ee0da42b7ef580deebad3`
- Job: `Pull runtime images, run preflight, optional real load`
- Failed step: `Prepare SSH`
- Error in runs `25628508822` and `25628614468`: `Missing required secret: PROD_SSH_HOST`
- Error in accidental placeholder run `25628772593`: `Missing required secret: PROD_SSH_PRIVATE_KEY`
- Skipped step in all runs: `Pull runtime images and run production checks`
- Repository secrets observed through `gh secret list`: only `PROMOTION_TOKEN`
- Repository environments observed through `gh api repos/kimwoonggon/woong-blog-aspcore-nextjs/environments --jq '.environments[]?.name'`: none
- Repository variables observed through `gh variable list`: none
- Repository owner check: `kimwoonggon` is a user account, not an organization; `gh secret list --org kimwoonggon` returns HTTP 404, so no org-level Actions secret source is available.
- Workflow environment usage: `.github/workflows/prod-runtime-redeploy.yml` does not declare a job `environment`, so it reads repo-level `secrets.PROD_SSH_*`.

## Incident Remediation

During setup runbook generation, placeholder commands were accidentally executed by an unquoted heredoc. This created bogus GitHub Actions secrets `PROD_SSH_HOST`, `PROD_SSH_USER`, `PROD_SSH_KNOWN_HOSTS`, and `PROD_SSH_PORT`, and dispatched run `25628772593`. That run failed in `Prepare SSH` before production server execution. The bogus secrets were deleted immediately; `gh secret list` again shows only `PROMOTION_TOKEN`.

## Latest Public Probe

Checked at `2026-05-10T21:27:36+09:00` against `https://woonglab.com`:

- `/api/health`: HTTP 200 with `server: cloudflare` and `x-app-elapsed-ms`; no nginx timing header was observed.
- `/api/public/works?page=1&pageSize=12`: first item still exposes stale fields `period` and `iconUrl`.
- `/api/public/works/smoke-fluid-simulation`: detail still exposes stale fields `period`, `iconUrl`, and `contentJson`.
- Work video payload still exposes stale fields `originalFileName`, `fileSize`, and `createdAt`.

This confirms the production server has not yet been redeployed to the latest `main` runtime image.

## Prompt-To-Artifact Checklist

| Requirement | Evidence | Status |
| --- | --- | --- |
| Server pulls latest `main` runtime image | Server execution step skipped in runs `25628508822`, `25628614468`, and `25628772593`. | Missing |
| Server deploys latest runtime image | Server execution step skipped. | Missing |
| Production preflight runs after deploy | Preflight is inside skipped server step. | Missing |
| Real Backend Test reruns | Load runner is inside skipped server step. | Missing |
| `pageSize=12` retained | Workflow/script inputs are prepared to use `pageSize=12`; no production run yet. | Prepared, not executed |
| Seed/fixture targets avoided | Workflow input uses real Work/Study paths and remote script rejects seed/fixture; no production run yet. | Prepared, not executed |
| Cache workaround avoided | No cache change was made; no production result yet. | Prepared, not executed |
| Next slice selected from result | No post-deploy result exists. | Missing |
| Selected slice implemented | Cannot start result-driven slice yet. | Missing |
| Full e2e and CI pass after selected slice | Not applicable until selected slice exists. | Missing |

## Required GitHub Secrets

- `PROD_SSH_HOST`: production SSH host.
- `PROD_SSH_USER`: production SSH user, likely `root` if the repo path remains `/root/service/woong-blog-aspcore-nextjs`.
- `PROD_SSH_PRIVATE_KEY`: private key for the production SSH user.
- `PROD_SSH_KNOWN_HOSTS`: host key entry matching `PROD_SSH_HOST` and `PROD_SSH_PORT`.
- Optional `PROD_SSH_PORT`: set only if not `22`.
- Optional `PROD_GHCR_TOKEN`: set only if public/anonymous GHCR pulls fail on the server.

## Rerun Command After Secrets Exist

```bash
gh workflow run prod-runtime-redeploy.yml --repo kimwoonggon/woong-blog-aspcore-nextjs --ref main \
  -f base_url=https://woonglab.com \
  -f repo_dir=/root/service/woong-blog-aspcore-nextjs \
  -f work_read_path=/api/public/works/smoke-fluid-simulation \
  -f 'study_read_path=/api/public/blogs/c-gc%EB%93%B1-%EB%B6%80%ED%95%98%ED%85%8C%EC%8A%A4%ED%8A%B8-%EC%9D%B4%ED%9B%84-%EA%B2%80%EC%A6%9D%ED%95%AD%EB%AA%A9' \
  -f run_real_load=true \
  -f 'rates=100 200 300 400' \
  -f duration_seconds=30 \
  -f max_vus=500 \
  -f pre_allocated_vus=100
```

## Local Yellow Flag

During report generation, an unquoted heredoc caused Markdown backticks to be interpreted by the shell. That accidentally dispatched the duplicate workflow run `25628614468` and attempted a local default `docker compose up`. Current local compose state is `db` running and `backend`/`frontend`/`nginx` created, not running. I did not run further local compose repair commands because that would change the local environment again without explicit confirmation.

Follow-up repair: `BACKEND_PUBLISH_PORT=18080 docker compose -f docker-compose.dev.yml up -d --force-recreate backend frontend nginx` restored the local dev stack. Health checks passed through nginx at `http://127.0.0.1:3000/api/health` and backend-direct at `http://127.0.0.1:18080/api/health`.

## Intentionally Not Changed

- No production/test target was changed to seed data.
- No list target was reduced from `pageSize=12`.
- No cache workaround was introduced.
- No backend performance code slice was selected without a valid post-deploy production load result.
- No production host changes occurred because the SSH step failed before server execution.

## Recommendation

Add the required production SSH secrets, rerun the workflow above, and only then interpret Real Backend Test numbers or choose the next code slice.

If production SSH secrets cannot be added, use `pasteable-server-commands.md` on the production host. That fallback path was rechecked with `bash -n`, keeps list targets at `pageSize=12`, rejects seed/fixture read paths in the load block, uses real public Work/Study read targets, and does not introduce cache behavior. The load block also verifies `prod-real-load-steps-summary.md/json`, prints the summary, and creates `prod-real-load-steps-artifacts.tgz` for transfer.
