# Current Main Server Evidence Handoff 2026-05-12

## Purpose

This handoff is the next concrete action toward the active goal while production SSH remains out of scope. It gives the server operator a single script that proves the server pulled and started the current main runtime images, runs production preflight, runs realistic Real Backend Test steps, and bundles evidence for review.

## Current Main Runtime Evidence

- Current `origin/main`: `389a117ee8cda43e84536c85164bf13afd8e38bf`
- Main merge source: PR #186, `release/main-promote -> main`
- `CI Main Runtime` run: `25717479751`, success
- `Publish GHCR Main` run: `25717736048`, success
- Runtime image short tag: `sha-389a117ee8cd`

Published runtime images:

- Frontend:
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main`
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:sha-389a117ee8cd`
  - digest `sha256:8cd7cdea54ad6388bcb34f3b8b694cf87e9721a35273cafd35d0f5fea0bfb5f3`
- Backend:
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main`
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:sha-389a117ee8cd`
  - digest `sha256:ae0679f8166380e0edef07c3d546cc31c9c291380e6f7eccc691c225c44afdf2`

## Why This Is Needed

The latest public-origin probe returned HTTP 200, but it did not prove the current runtime is deployed:

- API responses still lacked `X-Nginx-Request-Time`.
- Works list still exposed stale `period` and `iconUrl` keys that current `origin/main` `WorkCardDto` does not expose.

Therefore, production load results remain ambiguous until the server-side pull/deploy/preflight evidence exists.

## Server Command

Run from any shell on the production host where Docker and the repository are available:

```bash
bash backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh
```

If GHCR anonymous pull fails, run with a read-packages token:

```bash
GHCR_USER=kimwoonggon GHCR_TOKEN=<github-token-with-read-packages> \
bash backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh
```

Optional overrides:

```bash
WORK_READ_PATH=/api/public/works/<real-work-slug> \
STUDY_READ_PATH=/api/public/blogs/<real-study-slug> \
RATES="100 200 300 400" \
DURATION_SECONDS=30 \
MAX_VUS=500 \
PRE_ALLOCATED_VUS=100 \
bash backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh
```

## Script Guarantees

- Checks out `main` and fails unless the checked-out SHA is `389a117ee8cda43e84536c85164bf13afd8e38bf`.
- Sets runtime images to immutable `sha-389a117ee8cd` tags.
- Verifies GHCR manifests are readable before compose pull.
- Backs up `.env.prod`.
- Sets:
  - `FRONTEND_IMAGE`
  - `BACKEND_IMAGE`
  - `LoadTesting__BaseUrl=https://woonglab.com`
  - `APP_ENV_FILE=.env.prod`
  - `NGINX_DEFAULT_CONF=./nginx/prod.conf`
- Runs `docker compose pull` and `docker compose up -d --remove-orphans`.
- Runs `scripts/prod-runtime-preflight.sh`.
- Checks public list DTOs after deploy and fails on stale list keys.
- Runs `scripts/prod-real-load-steps.sh` with `LIST_PAGE_SIZE=12`.
- Rejects seed/fixture read paths.
- Verifies the generated k6 script contains the identity suffix guard.
- Verifies every real-load step reports `listPageSize: 12`.
- Writes and bundles evidence.

## Required Evidence To Return

The server-side run must produce:

- terminal output ending with `[current-main-evidence] PASS`
- `current-main-evidence-manifest.json`
- `prod-real-load-steps-summary.json`
- `prod-real-load-steps-summary.md`
- `current-main-preflight-load-evidence.tgz`

## Slice Selection Rule

Use `prod-real-load-steps-summary.json` after this run:

| Evidence | Next slice |
| --- | --- |
| HLS upload or HLS processing fails during preflight/manual upload | HLS fatal fix |
| High response body size, high receiving time, or app elapsed dominated by serialization | Public detail serialization/body optimization |
| High DB command latency, high DB connection-open latency, or active connection pressure | DB/index optimization |
| Clean run through current rates | Increase rate, extend soak, or defer backend change |

## Current Completion State

Still not complete:

- Server-side evidence is not yet present in this workspace.
- Production preflight has not passed against a proven current runtime.
- Real Backend Test summary from a proven current runtime is missing.
- No result-based next slice can be selected yet.
- Full E2E after the selected slice remains pending.
