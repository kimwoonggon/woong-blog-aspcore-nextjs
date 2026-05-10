# Production SSH Secrets Setup Runbook - 2026-05-10

Generated at: 2026-05-10T21:31:22+09:00

## Purpose

This runbook exists because the active production runtime retest is blocked before server execution. The workflow is ready, but GitHub repository secrets for production SSH are missing.

## Current Evidence

- `gh secret list --repo kimwoonggon/woong-blog-aspcore-nextjs` shows only `PROMOTION_TOKEN` after remediation.
- `gh api repos/kimwoonggon/woong-blog-aspcore-nextjs/environments --jq '.environments[]?.name'` returns no environments.
- `gh variable list --repo kimwoonggon/woong-blog-aspcore-nextjs` returns no variables.
- `.github/workflows/prod-runtime-redeploy.yml` does not declare a job `environment`, so it reads repo-level `secrets.PROD_SSH_*`.
- `gh repo view kimwoonggon/woong-blog-aspcore-nextjs --json owner,visibility` shows owner `kimwoonggon` is a user account and the repository is public.
- `gh secret list --org kimwoonggon` returns HTTP 404, so no org-level Actions secret source is available.
- `origin/main` is `757ab203e55cadf8f89ee0da42b7ef580deebad3`.
- `CI Main Runtime` run `25628240637` succeeded for `757ab203e55cadf8f89ee0da42b7ef580deebad3`.
- `Publish GHCR Main` run `25628355351` succeeded for `757ab203e55cadf8f89ee0da42b7ef580deebad3`.
- `Production Runtime Redeploy` runs `25628508822`, `25628614468`, and `25628772593` failed in `Prepare SSH`.
- Run `25628772593` specifically failed with `Missing required secret: PROD_SSH_PRIVATE_KEY` because bogus placeholder secrets were incomplete; those bogus secrets have since been deleted.
- All three runs skipped `Pull runtime images and run production checks`, so no production `docker compose pull/up`, preflight, or load test ran.
- Public probe still shows stale DTO fields on `https://woonglab.com`, confirming the latest `main` runtime is not deployed.

## Incident Remediation Note

During this runbook generation, placeholder commands were accidentally executed by an unquoted heredoc. This created bogus GitHub Actions secrets `PROD_SSH_HOST`, `PROD_SSH_USER`, `PROD_SSH_KNOWN_HOSTS`, and `PROD_SSH_PORT`, and dispatched run `25628772593`. The run failed before server execution. The bogus secrets were deleted immediately, and `gh secret list` again shows only `PROMOTION_TOKEN`.

## Required Inputs

Do not commit these values to the repository.

- `PROD_SSH_HOST`: production SSH host, likely `srv1592512.hstgr.cloud` or `187.127.111.73` if that is still the active origin.
- `PROD_SSH_USER`: production SSH user. If the repo path is `/root/service/woong-blog-aspcore-nextjs`, this is likely `root`.
- `PROD_SSH_PRIVATE_KEY`: private key that can authenticate as `PROD_SSH_USER` on `PROD_SSH_HOST`.
- `PROD_SSH_KNOWN_HOSTS`: strict host-key entry for the exact host/port.
- Optional `PROD_SSH_PORT`: set only if SSH is not port `22`.
- Optional `PROD_GHCR_TOKEN`: set only if anonymous GHCR pulls fail on the server.

## Setup Commands

Replace placeholders before running. Do not paste a private key directly into shell history.

```bash
# Required scalar values.
gh secret set PROD_SSH_HOST --repo kimwoonggon/woong-blog-aspcore-nextjs --body '<production-ssh-host>'
gh secret set PROD_SSH_USER --repo kimwoonggon/woong-blog-aspcore-nextjs --body '<production-ssh-user>'

# Required private key from a local file.
gh secret set PROD_SSH_PRIVATE_KEY --repo kimwoonggon/woong-blog-aspcore-nextjs < /absolute/path/to/production_private_key

# Required known_hosts entry for port 22.
ssh-keyscan -H '<production-ssh-host>' | gh secret set PROD_SSH_KNOWN_HOSTS --repo kimwoonggon/woong-blog-aspcore-nextjs

# If using a non-22 port, use these instead of the previous known_hosts command.
gh secret set PROD_SSH_PORT --repo kimwoonggon/woong-blog-aspcore-nextjs --body '<port>'
ssh-keyscan -p '<port>' -H '<production-ssh-host>' | gh secret set PROD_SSH_KNOWN_HOSTS --repo kimwoonggon/woong-blog-aspcore-nextjs
```

## Verification Commands

```bash
gh secret list --repo kimwoonggon/woong-blog-aspcore-nextjs
```

Expected names:

- `PROD_SSH_HOST`
- `PROD_SSH_USER`
- `PROD_SSH_PRIVATE_KEY`
- `PROD_SSH_KNOWN_HOSTS`
- `PROD_SSH_PORT` only if non-22
- `PROD_GHCR_TOKEN` only if required

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

## Success Criteria For The Next Run

- `Prepare SSH` succeeds.
- `Pull runtime images and run production checks` runs on the production host.
- Runtime backend/frontend images resolve to `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-*:main`.
- `prod-runtime-preflight.sh` passes with `REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1`.
- Real Backend Test writes load artifacts with list targets at `pageSize=12` and real read targets.

## Non-Goals

- Do not switch to seed/fixture targets.
- Do not reduce list targets to `pageSize=1`.
- Do not add cache as a performance shortcut.
- Do not select HLS/body/DB optimization slice until a valid post-deploy production load result exists.
