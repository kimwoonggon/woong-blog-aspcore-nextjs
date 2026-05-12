# Current Main Server Evidence Handoff 2026-05-12

## Purpose

This handoff is the next concrete action toward the active goal while production SSH remains out of scope. It gives the server operator a single script that proves the server pulled and started the current `main` runtime images, runs production preflight, runs realistic Real Backend Test steps, and bundles evidence for review.

## Runtime Resolution

The handoff script defaults to the latest fetched `origin/main`. It does not pin a commit in the repository artifact because that artifact itself is merged forward through `dev` and `main`; hard-coding a default SHA would become stale as soon as the handoff update reaches `main`.

Set `EXPECTED_MAIN_SHA=<40-char-sha>` only when an exact deployment pin is required. When this value is set, the script still fetches `origin/main` first and fails if the fetched main does not match the requested pin.

Resolved runtime images are computed at run time:

- Frontend: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:sha-${resolvedShaShort}`
- Backend: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:sha-${resolvedShaShort}`

The script resolves GHCR manifest digests at run time before `docker compose pull`, then writes those digests into `current-main-evidence-manifest.json`.

## Why This Is Needed

Green CI and public-origin HTTP 200 responses do not prove the production host has pulled and started the current runtime images. The required evidence is server-side:

- fetched `origin/main` SHA
- checked-out local `main` SHA
- immutable runtime image tags derived from that SHA
- resolved GHCR image digests
- compose pull/up output
- production preflight output
- realistic Real Backend Test summary

Until that evidence exists, public load-test results remain ambiguous and cannot select the next backend slice.

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

Optional exact pin and target overrides:

```bash
EXPECTED_MAIN_SHA=<40-char-sha> \
WORK_READ_PATH=/api/public/works/<real-work-slug> \
STUDY_READ_PATH=/api/public/blogs/<real-study-slug> \
RATES="100 200 300 400" \
DURATION_SECONDS=30 \
MAX_VUS=500 \
PRE_ALLOCATED_VUS=100 \
bash backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh
```

## Script Guarantees

- Defaults to the latest fetched `origin/main` instead of a hard-coded commit SHA.
- Preserves explicit `EXPECTED_MAIN_SHA` pinning when a specific main deployment must be enforced.
- Sets runtime images to immutable `sha-${resolvedShaShort}` tags unless explicit image overrides are provided.
- Resolves GHCR manifest digests before compose pull.
- Backs up `.env.prod` before editing it.
- Sets:
  - `FRONTEND_IMAGE`
  - `BACKEND_IMAGE`
  - `LoadTesting__BaseUrl=https://woonglab.com`
  - `APP_ENV_FILE=.env.prod`
  - `NGINX_DEFAULT_CONF=./nginx/prod.conf`
- Runs `docker compose pull` and `docker compose up -d --remove-orphans`.
- Runs `scripts/prod-runtime-preflight.sh` and persists its output as `current-main-preflight.log`.
- Checks public list DTOs after deploy and fails on stale list keys.
- Runs `scripts/prod-real-load-steps.sh` with `LIST_PAGE_SIZE=12`.
- Rejects seed/fixture read paths.
- Verifies the generated k6 script contains the identity suffix guard.
- Verifies every real-load step reports `listPageSize: 12`.
- Writes and bundles evidence.

## Required Evidence To Return

The server-side run must produce:

- terminal output ending with `[current-main-evidence] PASS`
- `current-main-preflight.log`
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

## Future UI Slice

Work and Study search should update related results while the user types, not only after pressing Enter. That is a separate frontend behavior slice and is intentionally not implemented by this production handoff fix.

## Current Completion State

Still not complete:

- Server-side evidence is not yet present in this workspace.
- Production preflight has not passed against a proven current runtime.
- Real Backend Test summary from a proven current runtime is missing.
- No result-based next slice can be selected yet.
- Full E2E after the selected slice remains pending.
