# Active Goal Post Main Promotion Audit 2026-05-12

## Objective Restated

The active goal is complete only when all of the following are true:

1. The server has pulled and deployed the current `main` runtime images.
2. Production preflight has passed on that deployed server runtime.
3. Real Backend Test has been rerun with `pageSize=12`, no seed/fixture targets, no cache workaround, and real public Work/Study URLs.
4. The returned Real Backend Test evidence has been verified and used to choose one next slice: HLS fatal fix, public detail serialization/body optimization, or DB/index optimization.
5. The selected slice has been implemented and full E2E plus CI are green afterward.

## Prompt-To-Artifact Checklist

| Requirement | Required Artifact Or Gate | Current Evidence | Status |
| --- | --- | --- | --- |
| Current `main` promoted | `origin/main` SHA and runtime-only promotion PR | `origin/main=3885eaeb16266de752fc984258bc30aafd3d7a62`; PR #193 merged | Complete |
| Main CI green | `CI Main Runtime` push run after merge | Run `25730480720` succeeded for SHA `3885eaeb16266de752fc984258bc30aafd3d7a62` | Complete |
| GHCR runtime images published | `Publish GHCR Main` run and immutable image tags | Run `25730793221` succeeded; backend `sha-3885eaeb1626` digest `sha256:112355097189a1f160ca5d4335bd00a95301b82c8c7b9e50408c932ce4eb774b`; frontend `sha256:e43ea1f01e4e9d6bd550ea78c4bf2a13dccdeaad3a499120850040a319518232` | Complete |
| Server pulled/deployed current images | Server-side handoff output ending `[current-main-evidence] PASS`, manifest with `mainSha`, image digests, compose pull/up evidence | No returned server evidence yet | Missing |
| Production preflight passed on deployed server | `current-main-preflight.log` containing `[prod-runtime-preflight] PASS`, nginx timing, app timing, gzip, public DTO contract lines | No returned `current-main-preflight.log` yet | Missing |
| Real Backend Test rerun | `prod-real-load-steps-summary.json` and `.md` from server run | No returned real-load summary yet | Missing |
| `pageSize=12` retained | Verifier checks summary and every step uses `listPageSize=12` and exact list paths | Verifier supports this, but no returned evidence to verify | Blocked |
| No seed/fixture targets | Verifier rejects seed/fixture paths | Verifier supports this, but no returned evidence to verify | Blocked |
| No cache workaround | Handoff uses public origin and real-load script; no cache-specific bypass added in this slice | No returned evidence to verify runtime behavior | Blocked |
| Real Work/Study URLs | Verifier requires public Work/Study list and detail paths or HTTPS URLs | Verifier supports this, but no returned evidence to verify | Blocked |
| Result-based next slice selected | Verified real-load summary `nextFocus` and metrics | No valid returned result yet | Missing |
| Selected slice implemented | Code/test changes for HLS, serialization/body, or DB/index | Not selected yet | Missing |
| Full E2E after selected slice | Full E2E result after selected slice | Not applicable until slice is selected/implemented | Missing |
| CI after selected slice | CI evidence after selected slice | Not applicable until slice is selected/implemented | Missing |

## Evidence Inspected

### Main And CI

- `origin/main`: `3885eaeb16266de752fc984258bc30aafd3d7a62`.
- `origin/dev`: `df7faf91b96de4dea2c0580054fba0f3bfba83e7`.
- PR #193 merged `release/main-promote -> main` at `2026-05-12T11:06:45Z`.
- `CI Main Runtime` push run `25730480720` completed successfully for `3885eaeb16266de752fc984258bc30aafd3d7a62`.
- `Publish GHCR Main` run `25730793221` completed successfully for `3885eaeb16266de752fc984258bc30aafd3d7a62`.

### GHCR Images

- Backend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:sha-3885eaeb1626`.
- Backend digest: `sha256:112355097189a1f160ca5d4335bd00a95301b82c8c7b9e50408c932ce4eb774b`.
- Frontend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:sha-3885eaeb1626`.
- Frontend digest: `sha256:e43ea1f01e4e9d6bd550ea78c4bf2a13dccdeaad3a499120850040a319518232`.

### Handoff And Verifier Coverage

- `server-current-main-preflight-load-evidence.sh` dynamically fetches `origin/main`, derives `sha-${SHA_SHORT}` runtime image tags, writes `LoadTesting__BaseUrl`, pulls/up compose, runs production preflight, saves `current-main-preflight.log`, runs `prod-real-load-steps.sh` with `LIST_PAGE_SIZE=12`, rejects seed/fixture targets, writes `current-main-evidence-manifest.json`, and bundles `current-main-preflight-load-evidence.tgz`.
- `prod-runtime-evidence-verify.sh` supports `current-main-evidence-manifest.json`, `current-main-preflight.log`, nested `current-main-preflight-load-evidence.tgz`, exact main SHA/digest checks, public HTTPS base URL, `pageSize=12`, no seed/fixture target, public Work/Study list paths, public detail targets, fail-rate limit, and dropped-iteration limit.

### Public-Origin Proxy Signals

These are not accepted as completion proof because they do not prove which image is currently running on the server.

- `https://woonglab.com/api/health` returned HTTP 200.
- `https://woonglab.com/api/public/works?page=1&pageSize=12` returned stale public Work list keys: `iconUrl`, `period`.
- `https://woonglab.com/api/public/blogs?page=1&pageSize=12` returned no sampled stale body/content keys.
- Because Work list still exposes stale keys, public origin does not prove deployment of the current `main` runtime.

## Completion Decision

The active goal is not complete.

Current main promotion, main CI, GHCR publish, handoff script, and verifier are ready. The missing evidence is the actual server-side deploy/preflight/load bundle generated after pulling the current main images. Without that returned evidence, the next backend slice cannot be selected, and no after-slice full E2E or CI proof can exist.

## Exact Next Evidence Needed

The server operator should run the current-main handoff script on the server and return the generated bundle:

```bash
cd /root/service/woong-blog-aspcore-nextjs

git fetch origin main --prune
git checkout main
git pull --ff-only origin main

EXPECTED_MAIN_SHA=3885eaeb16266de752fc984258bc30aafd3d7a62 \
EXPECTED_BACKEND_IMAGE_DIGEST=sha256:112355097189a1f160ca5d4335bd00a95301b82c8c7b9e50408c932ce4eb774b \
EXPECTED_FRONTEND_IMAGE_DIGEST=sha256:e43ea1f01e4e9d6bd550ea78c4bf2a13dccdeaad3a499120850040a319518232 \
bash backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh
```

Expected returned artifacts:

- terminal output ending with `[current-main-evidence] PASS`
- `current-main-preflight.log`
- `current-main-evidence-manifest.json`
- `prod-real-load-steps-summary.json`
- `prod-real-load-steps-summary.md`
- `current-main-preflight-load-evidence.tgz`

After receiving the bundle, verify it with:

```bash
EXPECTED_MAIN_SHA=3885eaeb16266de752fc984258bc30aafd3d7a62 \
EXPECTED_BACKEND_IMAGE_DIGEST=sha256:112355097189a1f160ca5d4335bd00a95301b82c8c7b9e50408c932ce4eb774b \
EXPECTED_FRONTEND_IMAGE_DIGEST=sha256:e43ea1f01e4e9d6bd550ea78c4bf2a13dccdeaad3a499120850040a319518232 \
bash scripts/prod-runtime-evidence-verify.sh /path/to/current-main-preflight-load-evidence.tgz
```

## Risks And Yellow Flags

- Public Work list still exposes `iconUrl` and `period`, which strongly suggests the public origin is not serving the latest runtime tree or is otherwise stale.
- A health 200 and green CI/GHCR are useful readiness signals, but they are proxy signals and do not satisfy the server deploy/preflight/load requirement.
- No backend optimization slice should be selected until the returned bundle passes verification.

## Recommendation

Do not mark the active goal complete. The next concrete action is to obtain and verify the server-generated `current-main-preflight-load-evidence.tgz`; then select exactly one backend slice from its measured `nextFocus` and metrics.
