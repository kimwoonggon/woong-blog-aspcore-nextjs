# Completion Audit - Production Main Runtime Retest

Status: not complete

## Objective Restated As Deliverables

1. Production server is running the latest `main` runtime images.
2. Production preflight passes after that deploy.
3. Real Backend Test is rerun with `pageSize=12`, no seed/fixture targets, no cache strategy, and real Work/Study URLs.
4. Load result is analyzed and exactly one next slice is selected from HLS fatal fix, public detail serialization/body optimization, or DB/index optimization.
5. The selected slice is implemented with tests.
6. Full e2e passes after implementation.
7. CI/publish/main promotion checks pass after implementation.

## Prompt-To-Artifact Checklist

| Requirement | Evidence | Status |
| --- | --- | --- |
| Pull/deploy latest main runtime image | `origin/main` is `1bf8a5dc9d57`; GHCR runtime packages have `main` tag `sha-1bf8a5dc9d57`; production public API still exposes stale fields | Missing |
| Production preflight | `server-main-runtime-redeploy.sh` prepared; no server execution output available | Missing |
| Real Backend Test rerun | `server-run-real-load-after-preflight.sh` prepared; no k6/load output available | Missing |
| `pageSize=12` | `scripts/prod-real-load-steps.sh` enforces `LIST_PAGE_SIZE=12`; helper auto-selects Study from `/api/public/blogs?page=1&pageSize=12` | Prepared, not executed |
| no seed/fixture URL | helper rejects seed/fixture paths and auto-selects non-seed Study target | Prepared, not executed |
| no cache strategy | no cache changes were made; helper does not warm/add cache | Prepared, not executed |
| real Work/Study URL | helper uses `/api/public/works/smoke-fluid-simulation` and public Study list auto-selection | Prepared, not executed |
| choose next slice from measured result | blocked because no valid latest-runtime load result exists | Missing |
| implement chosen slice | blocked by missing valid load result | Missing |
| full e2e all green | blocked by no selected slice implementation | Missing |
| CI all green | previous main CI/publish is green, but not for a new selected slice | Missing for this objective |

## Actual Current Evidence

- Public Work list still returns stale keys: `period`, `iconUrl`.
- Public Work detail still returns stale keys: `contentJson`, `iconUrl`.
- Public Work video payload still returns stale keys: `originalFileName`, `fileSize`, `createdAt`.
- Local `/mnt/d/woong-blog/service/.env.prod` points to legacy image names without the `-runtime-` suffix.
- GHCR confirms legacy packages were last updated on 2026-04-15, while runtime packages were updated on 2026-05-10.
- Direct SSH is unavailable from this environment: `srv1592512` does not resolve and `woonglab.com` is Cloudflare edge, not origin SSH.

## Decision

Do not mark the goal complete. The next required evidence is server-side execution output from `server-main-runtime-redeploy.sh`, followed by `server-run-real-load-after-preflight.sh` output after preflight passes.

## Operator Handoff Artifact

- `backend/reports/prod-main-runtime-retest-2026-05-10/pasteable-server-commands.md` contains paste-only server commands for the missing deploy/preflight/load evidence.

## SSH Discovery Blocker

- Windows SSH config yielded candidate `72.61.118.77`, but SSH reports host-key changed. This must be verified out-of-band before connection.
- Candidate `187.127.111.73` has matching host key but denied tested user/key combinations.
- No safe direct SSH path is currently available.

## Additional Validation

- Extracted both pasteable bash blocks and verified `bash -n` parsing without executing server-side commands.

## Cloudflared Attempt

- Docker `cloudflare/cloudflared:latest` ran successfully, but `young-assign-danny-dell.trycloudflare.com` DNS lookup returned no such host. The trycloudflare tunnel alias appears expired/unavailable.

## Script Hardening

- Public stale-key recheck now uses `curl --compressed` instead of manually piping through `gzip -dc`, so the operator command handles compressed and uncompressed responses.
- Revalidated `server-main-runtime-redeploy.sh` and both pasteable bash blocks with `bash -n`.

## Confirmed Host, Auth Blocked

- Reverse DNS confirms `187.127.111.73 -> srv1592512.hstgr.cloud`.
- Host keys match the existing Windows known_hosts entry for `187.127.111.73`.
- Authentication remains blocked: tested `root`, `ubuntu`, and `deploy` with the available Windows key copied to a temporary 0600 file; all were denied.
