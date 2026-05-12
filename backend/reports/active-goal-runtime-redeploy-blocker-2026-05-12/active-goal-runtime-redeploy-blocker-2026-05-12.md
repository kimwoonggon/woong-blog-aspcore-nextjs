# Active Goal Runtime Redeploy Blocker - 2026-05-12

## Decision

Do **not** mark the active goal complete.

Do **not** dispatch `Production Runtime Redeploy` yet. The workflow cannot reach the server because its required repository secrets are absent. Dispatching it now would fail at the `Prepare SSH` step before pull/deploy/preflight/load, so it would not produce useful evidence for the active goal.

## Objective Restated As Concrete Deliverables

1. Server pulls and deploys the current `main` runtime images.
2. Production preflight passes against that deployed runtime.
3. Real Backend Test reruns after deployment with:
   - `pageSize=12`
   - no seed or fixture read targets
   - no cache workaround
   - real Work and Study URLs
4. The result selects the next implementation slice:
   - HLS fatal fix, or
   - public detail serialization/body optimization, or
   - DB/index optimization
5. Full E2E is green after the selected slice.
6. CI is green through the dev-to-main path after the selected slice.

## Prompt-To-Artifact Checklist

| Requirement | Evidence inspected | Status |
| --- | --- | --- |
| Current `main` exists | `git rev-parse origin/main` | Present: `2c6b8b418990f5388aabbf24c07c3bfe6c3f507c` |
| Current `main` source | `git show origin/main --no-patch` | Present: merge PR #199 |
| Current `main` CI green | `gh run list --branch main` / `gh run view 25738704187` | Present |
| Current runtime images published | `gh run view 25739101934 --log` | Present |
| Frontend runtime immutable image | publish log | Present: `sha-2c6b8b418990`, digest `sha256:d26607a2756a1557c6a50532618269d315cd555f7110e3c72f050dcc9e75c2fb` |
| Backend runtime immutable image | publish log | Present: `sha-2c6b8b418990`, digest `sha256:26014f5ccaab828c21066eec9818038796d243ed0dfeb68a8e9c7e6d38c92e2c` |
| GitHub Actions production redeploy can reach server | `gh secret list --repo kimwoonggon/woong-blog-aspcore-nextjs` and workflow YAML | Missing required secrets |
| Server-side pull/deploy happened | Required output from workflow or handoff script | Missing |
| Production preflight after pull/deploy | Required output from workflow or handoff script | Missing |
| Real Backend Test rerun after current deploy | Required `prod-real-load-steps-summary.json` from current runtime | Missing |
| Real load used `pageSize=12` | Required summary/list target evidence | Missing |
| Real load used no seed/fixture/cache workaround | Required runner summary and guard output | Missing |
| Result-based next slice selected | Requires valid load result | Missing |
| Full E2E after selected slice | Requires selected slice implementation | Missing |
| CI green after selected slice | Requires PR/main CI evidence | Missing |

## Current Evidence

### Latest Main And Runtime Images

- `origin/main`: `2c6b8b418990f5388aabbf24c07c3bfe6c3f507c`
- Merge source: PR #199, `release/main-promote -> main`
- `CI Main Runtime`: run `25738704187`, success
- `Publish GHCR Main`: run `25739101934`, success
- Frontend image:
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main`
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:sha-2c6b8b418990`
  - digest `sha256:d26607a2756a1557c6a50532618269d315cd555f7110e3c72f050dcc9e75c2fb`
- Backend image:
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main`
  - `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:sha-2c6b8b418990`
  - digest `sha256:26014f5ccaab828c21066eec9818038796d243ed0dfeb68a8e9c7e6d38c92e2c`

### GitHub Actions Secret Readiness

`Production Runtime Redeploy` requires:

- `PROD_SSH_HOST`
- `PROD_SSH_USER`
- `PROD_SSH_PRIVATE_KEY`
- `PROD_SSH_KNOWN_HOSTS`

Current repository secret list returned only:

- `PROMOTION_TOKEN`

Therefore the GitHub-hosted redeploy path is not executable right now.

### Public Origin Probe

All public probes returned HTTP 200:

- `https://woonglab.com/api/health`
- `https://woonglab.com/api/public/works?page=1&pageSize=12`
- `https://woonglab.com/api/public/blogs?page=1&pageSize=12`

But the Work list response still includes stale list fields:

- `iconUrl`
- `period`

Current `origin/main` `WorkCardDto` contains:

- `Id`
- `Slug`
- `Title`
- `Excerpt`
- `Category`
- `Tags`
- `ThumbnailUrl`
- `PublishedAt`

So the public origin HTTP 200 responses are not enough to prove the current main runtime is deployed.

## Action Taken

Verified the existing dynamic server-side evidence handoff remains valid for latest `origin/main`:

- `backend/reports/current-main-server-evidence-handoff-2026-05-12/current-main-server-evidence-handoff-2026-05-12.md`
- `backend/reports/current-main-server-evidence-handoff-2026-05-12/current-main-server-evidence-handoff-2026-05-12.json`
- `backend/reports/current-main-server-evidence-handoff-2026-05-12/current-main-server-evidence-handoff-2026-05-12.html`
- `backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh`

Verification:

- `node -e "JSON.parse(...current-main-server-evidence-handoff-2026-05-12.json...)"`
- `bash -n backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh`
- grep confirmed no old pinned SHA/digest/run IDs remain in the handoff and blocker reports.
- the handoff defaults to fetched `origin/main` and can still be exact-pinned with `EXPECTED_MAIN_SHA`, `EXPECTED_BACKEND_IMAGE_DIGEST`, and `EXPECTED_FRONTEND_IMAGE_DIGEST`.

## Remaining Blocker

One of these must happen before the active goal can continue:

1. Add the missing repo secrets and dispatch `Production Runtime Redeploy` with `run_real_load=true`.
2. Run the refreshed server-side handoff script on the server and return its evidence bundle.

Until then, Real Backend Test results are ambiguous and cannot be used to select HLS/detail/DB optimization.

## Recommendation

Do not run or interpret another load test from the current public origin yet. First produce server-side current-main deploy/preflight evidence using the refreshed handoff or the GitHub workflow after secrets are configured.
