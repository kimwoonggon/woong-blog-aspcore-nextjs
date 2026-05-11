# Current Main Server Evidence Runbook - 2026-05-11

## Summary

Generated a current-main, digest-aware server handoff script for the active production evidence goal.

The script is intended to be copied or run on the production server after `.env.prod` secrets are already filled. It pulls and starts the current `main` runtime images, runs production preflight, runs Real Backend Test with realistic public targets, creates an evidence bundle, and verifies that bundle against the current `main` SHA and pulled image digests.

This does not complete the active goal because the script has not been executed on the server and no production evidence bundle has been returned.

## Generated Artifact

- `backend/reports/current-main-server-evidence-runbook-2026-05-11/server-current-main-preflight-load-evidence.sh`
- `docs/walkthroughs/main-server-setup.md` now links to the current-main script so it is discoverable from the standard server setup flow.

## Fixed Current Main Inputs

- Expected `main` SHA: `08978b2f8cb472d4c50cf29e165d758cc4ffd382`
- Backend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main`
- Frontend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main`
- Backend index digest: `sha256:677068ac570d8550e40b4c9985f606f47d6334f2ee9abbcb4fd0572459e976d8`
- Backend linux/amd64 manifest: `sha256:d1ef5eb9eeec2597168717b13530afb0030c8747bbcbda54da4c3958709a7282`
- Frontend index digest: `sha256:9cf9d1160d7155870a20249a589781439235ace68fdcc40e1393c2a5e93d5088`
- Frontend linux/amd64 manifest: `sha256:1851158728ad4d1d7bbbe9182ffb24256b21fe8747ab008865f58d044736d5c7`

The script accepts either index digest or linux/amd64 manifest digest from local Docker image inspection, then writes the actual pulled digest into the evidence bundle and verifier call.

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
3. Fetches `origin/main`, checks out `main`, pulls fast-forward only, and fails unless HEAD is `08978b2f8cb472d4c50cf29e165d758cc4ffd382`.
4. Backs up `.env.prod` to `.env.prod.backup.<RUN_ID>`.
5. Upserts non-secret runtime keys:
   - `FRONTEND_IMAGE`
   - `BACKEND_IMAGE`
   - `NEXT_PUBLIC_SITE_URL`
   - `LoadTesting__BaseUrl`
   - `APP_ENV_FILE`
   - `NGINX_DEFAULT_CONF`
6. Pulls and starts `docker-compose.prod.yml`.
7. Verifies the pulled backend/frontend image digests match the current published main images.
8. Resolves `STUDY_READ_PATH` from `/api/public/blogs?page=1&pageSize=12` if omitted.
9. Rejects seed/fixture Work/Study read targets.
10. Runs `scripts/prod-runtime-preflight.sh`.
11. Runs `scripts/prod-real-load-steps.sh` with list `pageSize=12` and public read targets.
12. Runs `scripts/prod-runtime-evidence-bundle.sh`.
13. Runs `scripts/prod-runtime-evidence-verify.sh` against the produced tarball.
14. Prints the evidence tarball path to return for result-based slice selection.

## Prompt-To-Artifact Checklist

| Requirement | Artifact / Evidence | Status |
| --- | --- | --- |
| Server pull/deploy current `main` image | Script performs `docker compose pull` and `up -d` after exact SHA check | Prepared, not executed |
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

## Risks / Yellow Flags

- This script intentionally mutates server `.env.prod` non-secret runtime keys and restarts compose; it must only be run by the server operator.
- If `origin/main` advances, the script fails by design and the runbook must be regenerated for the new SHA/digests.
- Current public-origin probe still shows stale Work DTO fields and missing nginx request timing, so Real Backend Test results remain untrusted until this script or equivalent server deploy/preflight succeeds.

## Recommendation

Run the script on the server only after `.env.prod` secrets are present. Return `production-runtime-evidence.tar.gz` and `production-runtime-evidence-summary.md`. Then run result-based slice selection from that verified evidence.
