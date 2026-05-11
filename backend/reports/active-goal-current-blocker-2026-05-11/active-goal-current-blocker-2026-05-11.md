# Active Goal Current Blocker - 2026-05-11

## Objective Restated

1. Production server pulls and deploys the current `main` runtime images.
2. Production preflight passes after deploy.
3. Real Backend Test reruns with list `pageSize=12`, no seed/fixture target, no cache shortcut, and real Work/Study read URLs.
4. A next slice is selected from the fresh result: HLS fatal fix, public detail serialization/body optimization, or DB/index optimization.
5. The selected slice is implemented and verified by full E2E and CI.

## Current Decision

The objective is not complete and cannot advance to Real Backend Test interpretation yet.

The code and images are ready, but there is still no evidence that the production server has pulled/deployed the current `main` images. A public-origin probe still shows stale DTO fields on `https://woonglab.com`, which indicates deployment/runtime drift.

## Current Main / Image Evidence

- `origin/main`: `08978b2f8cb472d4c50cf29e165d758cc4ffd382`
- `CI Main Runtime` push run `25669840505`: success
- `Publish GHCR Main` run `25670130541`: success
- Backend `main` image index digest: `sha256:677068ac570d8550e40b4c9985f606f47d6334f2ee9abbcb4fd0572459e976d8`
- Backend `linux/amd64` manifest: `sha256:d1ef5eb9eeec2597168717b13530afb0030c8747bbcbda54da4c3958709a7282`
- Frontend `main` image index digest: `sha256:9cf9d1160d7155870a20249a589781439235ace68fdcc40e1393c2a5e93d5088`
- Frontend `linux/amd64` manifest: `sha256:1851158728ad4d1d7bbbe9182ffb24256b21fe8747ab008865f58d044736d5c7`

## Production Automation Evidence

- Workflow exists: `Production Runtime Redeploy` / `.github/workflows/prod-runtime-redeploy.yml`
- Workflow trigger: `workflow_dispatch` only
- Required repo secrets are absent.
- `gh secret list` currently shows only `PROMOTION_TOKEN`.
- `gh variable list` currently returns no variables.
- Repository environments list is empty.
- Last production redeploy runs were all failures on the older SHA `757ab203e55c`:
  - `25628508822`: failure
  - `25628614468`: failure
  - `25628772593`: failure
- No production redeploy run exists for current main SHA `08978b2f8cb472d4c50cf29e165d758cc4ffd382`.

## Public-Origin Probe

Checked `https://woonglab.com` without SSH or server mutation.

### `/api/health`

- HTTP 200
- `server: cloudflare`
- `x-app-elapsed-ms: 0.4349`
- Health body: `{"status":"ok","service":"portfolio-api"}`

### `/api/public/works?page=1&pageSize=12`

First item keys still include stale fields:

- `period`
- `iconUrl`

### `/api/public/works/smoke-fluid-simulation`

Detail keys still include stale fields:

- `period`
- `iconUrl`
- `contentJson`

### `/api/public/blogs?page=1&pageSize=12`

No checked stale fields were observed in the first blog list item.

## Prompt-To-Artifact Checklist

| Requirement | Evidence | Status |
| --- | --- | --- |
| Current `main` images are published | `CI Main Runtime` run `25669840505` and `Publish GHCR Main` run `25670130541` succeeded for `08978b2f8cb472d4c50cf29e165d758cc4ffd382`; GHCR digests inspected | Complete |
| Server pulled current `main` images | No production redeploy run for current main; public DTO still stale | Missing |
| Server deployed current `main` images | Public-origin probe still exposes stale Work DTO fields | Missing |
| Production preflight passed after deploy | No current deploy evidence; no preflight log | Missing |
| Real Backend Test reran with `pageSize=12` | No current real-load evidence | Missing |
| No seed/fixture target | Scripts enforce this, but no current production run exists | Prepared, not executed |
| No cache shortcut | Scripts avoid cache shortcut by appending request identity, but no current production run exists | Prepared, not executed |
| Real Work/Study URLs | Scripts require public read paths/URLs, but no current production run exists | Prepared, not executed |
| Next slice selected from fresh result | Blocked by missing fresh result | Missing |
| Full E2E and CI after selected slice | No selected slice yet | Missing |

## Recommendation

Do not run or interpret Real Backend Test against `https://woonglab.com` until the server is confirmed to be running `main@08978b2f8cb472d4c50cf29e165d758cc4ffd382` or newer and `prod-runtime-preflight.sh` passes.

Because direct production SSH is intentionally not being used from this environment, the next required external action is one of:

1. Configure real `PROD_SSH_*` repository secrets and dispatch `Production Runtime Redeploy` with `run_real_load=false` first.
2. Manually run the documented `docs/walkthroughs/main-server-setup.md` flow on the server, then run preflight and return the evidence bundle.

Until that evidence exists, the active goal must remain open.

## Public-Origin Preflight Script Result

Command:

```bash
BASE_URL=https://woonglab.com \
WORK_READ_PATH=/api/public/works/smoke-fluid-simulation \
STUDY_READ_PATH=/api/public/blogs/c-gc%EB%93%B1-%EB%B6%80%ED%95%98%ED%85%8C%EC%8A%A4%ED%8A%B8-%EC%9D%B4%ED%9B%84-%EA%B2%80%EC%A6%9D%ED%95%AD%EB%AA%A9 \
./scripts/prod-public-origin-preflight.sh
```

Result:

```text
[prod-public-origin-preflight] FAIL: public Work list missing X-Nginx-Request-Time
```

This is additional evidence that the current public origin is not ready for trusted Real Backend Test interpretation.
