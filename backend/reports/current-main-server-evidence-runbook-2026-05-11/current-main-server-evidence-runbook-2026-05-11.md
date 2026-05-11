# Current Main Server Evidence Runbook - 2026-05-11

## Summary

Updated the server handoff script so it records and verifies the current checked-out `main` SHA and actual pulled image digests without becoming stale after each promotion.

The script is intended to be copied or run on the production server after `.env.prod` secrets are already filled. It pulls and starts the current `main` runtime images, runs production preflight, runs Real Backend Test with realistic public targets, creates an evidence bundle, and verifies that bundle against the checked-out `main` SHA and actual pulled image digests.

This does not complete the active goal because the script has not been executed on the server and no production evidence bundle has been returned.

## Generated Artifact

- `backend/reports/current-main-server-evidence-runbook-2026-05-11/server-current-main-preflight-load-evidence.sh`
- `docs/walkthroughs/main-server-setup.md` now links to the current-main script so it is discoverable from the standard server setup flow.
- GHCR manifest checks and image pulls now use a temporary Docker config so stale server credentials do not break public runtime image pulls.

## Runtime Inputs

- Backend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main`
- Frontend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main`
- Optional `EXPECTED_MAIN_SHA`: if supplied, the script fails unless checked-out `main` matches it.
- Optional `EXPECTED_BACKEND_IMAGE_DIGEST`: if supplied, the script fails unless the pulled backend digest matches it.
- Optional `EXPECTED_FRONTEND_IMAGE_DIGEST`: if supplied, the script fails unless the pulled frontend digest matches it.

When expected values are omitted, the script records the checked-out `main` SHA and actual pulled backend/frontend digests into the evidence bundle, then verifies that same bundle with those observed values. This avoids a self-invalidating runbook after each `main` promotion.

## Server Usage

From the production repository directory, after `.env.prod` contains real secrets:

```bash
bash backend/reports/current-main-server-evidence-runbook-2026-05-11/server-current-main-preflight-load-evidence.sh
```

Optional overrides:

```bash
REPO_DIR=/root/service/woong-blog-aspcore-nextjs \
BASE_URL=https://woonglab.com \
WORK_READ_PATH=/api/public/works/smoke-fluid-simulation \
STUDY_READ_PATH=/api/public/blogs/<real-study-slug> \
RATES="100 200 300 400" \
DURATION_SECONDS=30 \
MAX_VUS=500 \
PRE_ALLOCATED_VUS=100 \
bash backend/reports/current-main-server-evidence-runbook-2026-05-11/server-current-main-preflight-load-evidence.sh
```

If GHCR anonymous pull fails on the server:

```bash
GHCR_USER=kimwoonggon GHCR_TOKEN=<read-packages-token> \
bash backend/reports/current-main-server-evidence-runbook-2026-05-11/server-current-main-preflight-load-evidence.sh
```

## What The Script Does

1. Requires `git`, `docker`, `curl`, `node`, `k6`, and `tar`.
2. Requires existing `.env.prod`; it does not create or fill secrets.
3. Fetches `origin/main`, checks out `main`, pulls fast-forward only, and records the checked-out SHA. If `EXPECTED_MAIN_SHA` is supplied, it must match.
4. Backs up `.env.prod` to `.env.prod.backup.<RUN_ID>`.
5. Upserts non-secret runtime keys:
   - `FRONTEND_IMAGE`
   - `BACKEND_IMAGE`
   - `NEXT_PUBLIC_SITE_URL`
   - `LoadTesting__BaseUrl`
   - `APP_ENV_FILE`
   - `NGINX_DEFAULT_CONF`
6. Uses a temporary Docker config for GHCR manifest inspection and `docker compose pull`.
7. Starts `docker-compose.prod.yml` with the pulled images.
8. Records the pulled backend/frontend image digests. If expected digest env vars are supplied, they must match.
9. Resolves `STUDY_READ_PATH` from `/api/public/blogs?page=1&pageSize=12` if omitted.
10. Rejects seed/fixture Work/Study read targets.
11. Runs `scripts/prod-runtime-preflight.sh`.
12. Runs `scripts/prod-real-load-steps.sh` with list `pageSize=12` and public read targets.
13. Runs `scripts/prod-runtime-evidence-bundle.sh`.
14. Runs `scripts/prod-runtime-evidence-verify.sh` against the produced tarball.
15. Prints the evidence tarball path to return for result-based slice selection.

## Prompt-To-Artifact Checklist

| Requirement | Artifact / Evidence | Status |
| --- | --- | --- |
| Server pull/deploy current `main` image | Script performs `docker compose pull` and `up -d` after checking out current `main`; optional exact SHA check is supported | Prepared, not executed |
| Production preflight | Script runs `prod-runtime-preflight.sh` and captures `prod-runtime-preflight.log` | Prepared, not executed |
| Real Backend Test with `pageSize=12` | Script calls `prod-real-load-steps.sh`; that script enforces `LIST_PAGE_SIZE=12` | Prepared, not executed |
| No seed/fixture target | Script rejects `seed`/`fixture`; load and bundle scripts also reject these | Prepared, not executed |
| No cache shortcut | Script uses existing k6 path; `prod-real-load-steps.sh` appends request identity and does not add cache | Prepared, not executed |
| Real Work/Study URL | Script requires public Work path and auto-selects or accepts public Study path | Prepared, not executed |
| Evidence bundle | Script runs `prod-runtime-evidence-bundle.sh` | Prepared, not executed |
| Evidence verifier | Script runs `prod-runtime-evidence-verify.sh` with actual pulled digests | Prepared, not executed |
| Result-based next slice | Still blocked until script is executed and evidence is returned | Missing |
| After-slice full E2E/CI | Still blocked until next slice is selected and implemented | Missing |

## Validation Performed Locally

- `bash -n backend/reports/current-main-server-evidence-runbook-2026-05-11/server-current-main-preflight-load-evidence.sh`: passed.
- JSON audit parse: passed.
- `git diff --check`: passed for generated artifacts and TODO update.
- `docs/walkthroughs/main-server-setup.md` was updated to reference the generated script.
- The generated script was checked for temporary Docker config use on `manifest inspect`, `compose config`, and `compose pull`.

## Risks / Yellow Flags

- This script intentionally mutates server `.env.prod` non-secret runtime keys and restarts compose; it must only be run by the server operator.
- If `origin/main` advances, the script remains usable because it records the checked-out SHA and pulled digests; supply `EXPECTED_*` env vars only when an exact pin is required.
- Current public-origin probe still shows stale Work DTO fields and missing nginx request timing, so Real Backend Test results remain untrusted until this script or equivalent server deploy/preflight succeeds.

## Recommendation

Run the script on the server only after `.env.prod` secrets are present. Return `production-runtime-evidence.tar.gz` and `production-runtime-evidence-summary.md`. Then run result-based slice selection from that verified evidence.
