# Main Runtime Digest-Pin Promotion Audit - 2026-05-12

## Objective Restatement

The active objective requires all of the following before it can be considered complete:

1. The server pulls and deploys the current `main` runtime images, then production preflight passes.
2. Real Backend Test reruns with `pageSize=12`, no seed, no cache workaround, and real Work/Study URLs.
3. The load-test result is used to choose and implement one next optimization/fix slice: HLS fatal fix, public detail serialization/body optimization, or DB/index optimization.
4. After the selected slice, full E2E passes and CI is green.

This audit only verifies the GitHub/main/GHCR readiness part. It does not claim server-side deploy, Real Backend Test, result-based slice selection, or after-slice full E2E completion.

## Prompt-To-Artifact Checklist

| Requirement | Evidence inspected | Status |
| --- | --- | --- |
| Digest-pin handoff reaches `dev` | PR #194 merged at `2026-05-12T11:46:51Z`; merge commit `62ac17e97567babb1a3f48309b0b171abb590ba3` | Complete |
| `dev` CI green after digest-pin merge | `CI Dev` run `25732372942` succeeded for `62ac17e97567babb1a3f48309b0b171abb590ba3` | Complete |
| Runtime-only promotion reaches `main` | PR #195 `release/main-promote -> main` merged; `origin/main=bc88b3fd19ded9f463b1fc92a08b9b7fa9782192` | Complete |
| Promotion PR CI green | PR #195 `CI Main Runtime` run `25732671941` succeeded | Complete |
| Main push CI green | Main push `CI Main Runtime` run `25733203959` succeeded | Complete |
| GHCR main runtime images published | `Publish GHCR Main` run `25733621502` succeeded for `bc88b3fd19ded9f463b1fc92a08b9b7fa9782192` | Complete |
| Immutable backend image digest known | `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:sha-bc88b3fd19de` index digest `sha256:7c2b6c32785e874589dddce2b949ad9ca335e0ee4d944f7efd01e7d4c0232210` | Complete |
| Immutable frontend image digest known | `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:sha-bc88b3fd19de` index digest `sha256:e7b025c2304a9085cd9dde17737028add6ae56f7e29d3584580d3cb420cacc85` | Complete |
| Public origin checked only as proxy signal | `https://woonglab.com/api/health` returned 200; public Work list still exposes stale `iconUrl` and `period`; public Blog list sample did not expose stale body/content fields | Proxy only, not completion proof |
| Server deploy/preflight evidence returned | Required evidence bundle has not been returned | Missing |
| Real Backend Test rerun evidence returned | Required `prod-real-load-steps-summary.*` from server bundle has not been returned | Missing |
| Result-based next slice selected and implemented | Cannot choose a valid slice without returned Real Backend Test evidence | Missing |
| Full E2E after selected slice | Not applicable yet because the next slice has not been selected/implemented | Missing |

## Latest Main Runtime Evidence

- `origin/main`: `bc88b3fd19ded9f463b1fc92a08b9b7fa9782192`.
- Short image tag suffix: `sha-bc88b3fd19de`.
- `CI Dev` run after PR #194 merge: `25732372942`, success.
- PR #195 `CI Main Runtime` run: `25732671941`, success.
- Main push `CI Main Runtime` run: `25733203959`, success.
- `Publish GHCR Main` run: `25733621502`, success.

## Runtime Images

- Backend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:sha-bc88b3fd19de`.
- Backend index digest: `sha256:7c2b6c32785e874589dddce2b949ad9ca335e0ee4d944f7efd01e7d4c0232210`.
- Frontend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:sha-bc88b3fd19de`.
- Frontend index digest: `sha256:e7b025c2304a9085cd9dde17737028add6ae56f7e29d3584580d3cb420cacc85`.

## Server Handoff Command

Run this on the server, not from this workspace:

```bash
cd /root/service/woong-blog-aspcore-nextjs

git fetch origin main --prune
git checkout main
git pull --ff-only origin main

EXPECTED_MAIN_SHA=bc88b3fd19ded9f463b1fc92a08b9b7fa9782192 \
EXPECTED_BACKEND_IMAGE_DIGEST=sha256:7c2b6c32785e874589dddce2b949ad9ca335e0ee4d944f7efd01e7d4c0232210 \
EXPECTED_FRONTEND_IMAGE_DIGEST=sha256:e7b025c2304a9085cd9dde17737028add6ae56f7e29d3584580d3cb420cacc85 \
bash backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh
```

The expected successful terminal output should end with:

```text
[current-main-evidence] PASS
```

## Required Returned Artifacts

The server run must return these artifacts before the active objective can move to result-based slice selection:

- `current-main-preflight.log`
- `current-main-evidence-manifest.json`
- `prod-real-load-steps-summary.json`
- `prod-real-load-steps-summary.md`
- `current-main-preflight-load-evidence.tgz`

## Returned Bundle Verification Command

After receiving the tarball, verify it locally with:

```bash
EXPECTED_MAIN_SHA=bc88b3fd19ded9f463b1fc92a08b9b7fa9782192 \
EXPECTED_BACKEND_IMAGE_DIGEST=sha256:7c2b6c32785e874589dddce2b949ad9ca335e0ee4d944f7efd01e7d4c0232210 \
EXPECTED_FRONTEND_IMAGE_DIGEST=sha256:e7b025c2304a9085cd9dde17737028add6ae56f7e29d3584580d3cb420cacc85 \
bash scripts/prod-runtime-evidence-verify.sh /path/to/current-main-preflight-load-evidence.tgz
```

## What Changed

- PR #194 was merged to `dev`, adding optional expected image digest checks to the current-main server evidence handoff.
- PR #195 promoted the runtime-only tree from `dev` to `main`.
- Main push CI and GHCR publish succeeded for `bc88b3fd19ded9f463b1fc92a08b9b7fa9782192`.
- The latest backend/frontend immutable image digests were resolved and recorded.
- Work/Study live-search was added to the future TODO plan, but not implemented in this slice.

## Intentionally Not Changed

- No production SSH was used.
- No server-side deploy/preflight/load test was run from this workspace.
- No cache, seed, or `pageSize` workaround was introduced.
- No backend optimization slice was selected without returned load evidence.
- No production/test code was changed in this status slice.

## Risks And Yellow Flags

- The active objective is still blocked on server-returned evidence.
- Public origin is not proof of current deploy; Work list still exposes stale `iconUrl` and `period` fields in the sampled response.
- GitHub Actions reports Node.js 20 deprecation warnings for several actions. This is not a current failure, but it should be handled before GitHub enforces Node 24 defaults.

## Final Recommendation

Do not mark the active goal complete. The next required action is to run the server handoff command above on the server and return the evidence bundle. Only after the bundle verifies should Real Backend Test results be used to choose the next implementation slice.
