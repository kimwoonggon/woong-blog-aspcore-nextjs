# Active Goal Current State Audit - 2026-05-12

## Completion Decision

Do **not** mark the active goal complete.

`main` and GHCR are ready, but the server-side deploy/preflight/load evidence required by the objective is still missing. Public origin responses still do not prove current `main` is deployed because the Works list response exposes stale fields that the current `WorkCardDto` no longer contains.

## Objective Restated As Success Criteria

1. The server pulls and deploys the current `main` runtime images.
2. Production preflight runs and passes after that deploy.
3. Real Backend Test reruns after that deploy with:
   - `pageSize=12`
   - no seed/fixture read targets
   - no cache workaround
   - real Work and Study URLs
4. The valid Real Backend Test result selects one next slice:
   - HLS fatal fix, or
   - public detail serialization/body optimization, or
   - DB/index optimization
5. The selected slice is implemented.
6. Full E2E is green after the selected slice.
7. CI is green through the branch/PR and main promotion path after the selected slice.

## Prompt-To-Artifact Checklist

| Requirement | Evidence inspected | Result |
| --- | --- | --- |
| Current `main` exists | `origin/main` = `acdb7ef8e5070d13b3efb711e168c9da50e5db24` | Present |
| Current `main` CI green | `CI Main Runtime` run `25727092625`, success | Present |
| Current `main` GHCR publish green | `Publish GHCR Main` run `25727410166`, success | Present |
| Backend runtime image readable | `ghcr.io/...runtime-backend:sha-acdb7ef8e507` digest `sha256:d546c00a7340c1b5c764004c2d20017364ee57d48e1d415d9330115a88ef3174` | Present |
| Frontend runtime image readable | `ghcr.io/...runtime-frontend:sha-acdb7ef8e507` digest `sha256:554830dcc8c585451d3e94fdcb5add479858b94f4eb3f725cd887a5629f2c04e` | Present |
| Server-side deploy path via GitHub Actions usable | Repo secrets list and environments list | Missing: only `PROMOTION_TOKEN`; no environments returned |
| Production Runtime Redeploy success for current main | `gh run list --workflow "Production Runtime Redeploy"` and failed-run log `25628772593` | Missing: latest runs are May 10 failures on old SHA `757ab203e55cadf8f89ee0da42b7ef580deebad3`; failed log exits with missing `PROD_SSH_PRIVATE_KEY` |
| Public origin proves current main runtime | `GET https://woonglab.com/api/public/works?page=1&pageSize=12` | Not proven: stale `iconUrl` and `period` keys are still present |
| Current main server handoff exists | `origin/main:backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh` | Present |
| Handoff avoids stale pinned SHA | script defaults `EXPECTED_MAIN_SHA="${EXPECTED_MAIN_SHA:-}"` and resolves `git rev-parse origin/main` | Present |
| Server pulled/deployed current images | Required returned evidence from server-side script | Missing |
| Production preflight passed after deploy | Required returned evidence from server-side script | Missing |
| Real Backend Test reran with pageSize=12, no seed/cache, real URLs | Required returned evidence from server-side script summary | Missing |
| Result-based next slice selected | Requires valid real-load summary | Missing |
| Selected slice implemented | Requires result-based implementation | Missing |
| Full E2E after selected slice | Requires selected slice implementation | Missing |
| CI after selected slice | Requires selected slice PR/main path | Missing |

## Current Main Runtime Evidence

- `origin/main`: `acdb7ef8e5070d13b3efb711e168c9da50e5db24`
- `CI Main Runtime`: run `25727092625`, success
- `Publish GHCR Main`: run `25727410166`, success
- Backend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:sha-acdb7ef8e507`
- Backend digest: `sha256:d546c00a7340c1b5c764004c2d20017364ee57d48e1d415d9330115a88ef3174`
- Frontend image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:sha-acdb7ef8e507`
- Frontend digest: `sha256:554830dcc8c585451d3e94fdcb5add479858b94f4eb3f725cd887a5629f2c04e`

This proves CI/GHCR readiness only. It does not prove the production server pulled or started these images.

## Public Origin Probe

Probe times: `2026-05-12T10:10:35Z` and `2026-05-12T10:17:54Z`.

- `GET https://woonglab.com/api/health`
  - HTTP: `200`
  - total: `1.059873s`
  - response body: `{"status":"ok","service":"portfolio-api",...}`
  - visible headers: Cloudflare, no runtime image/SHA proof
- `GET https://woonglab.com/api/public/works?page=1&pageSize=12`
  - HTTP: `200`
  - item count: `7`
  - first item keys: `category, excerpt, iconUrl, id, period, publishedAt, slug, tags, thumbnailUrl, title`
  - stale keys: `iconUrl`, `period`
- `GET https://woonglab.com/api/public/blogs?page=1&pageSize=12`
  - HTTP: `200`
  - item count: `12`
  - first item keys: `coverUrl, excerpt, id, publishedAt, slug, tags, title`

Current `WorkCardDto` in `origin/main` contains only:

- `Id`
- `Slug`
- `Title`
- `Excerpt`
- `Category`
- `Tags`
- `ThumbnailUrl`
- `PublishedAt`

Therefore the public Works list is still stale against current `main`, or at least does not prove current `main` is deployed. Real Backend Test results from this origin would not be valid evidence for selecting the next slice.

## GitHub Production Redeploy Blocker

`Production Runtime Redeploy` cannot currently produce the missing server evidence. Run `25628772593` failed in `Prepare SSH` with:

```text
Missing required secret: PROD_SSH_PRIVATE_KEY
```

The same log shows `PROD_SSH_PRIVATE_KEY`, `PROD_SSH_KNOWN_HOSTS`, and `PROD_GHCR_TOKEN` were empty. Repository secret enumeration currently exposes only `PROMOTION_TOKEN`, and no GitHub environments were returned. This confirms the GitHub-hosted production SSH deployment path is not usable until real production secrets are configured.

## Next Required Server Action

Run this on the server, not from this local workspace:

```bash
cd /root/service/woong-blog-aspcore-nextjs

git fetch origin main --prune
git checkout main
git pull --ff-only origin main

EXPECTED_MAIN_SHA=acdb7ef8e5070d13b3efb711e168c9da50e5db24 \
bash backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh
```

The script should return evidence ending with:

- terminal output ending in `[current-main-evidence] PASS`
- `current-main-evidence-manifest.json`
- `prod-real-load-steps-summary.json`
- `prod-real-load-steps-summary.md`
- `current-main-preflight-load-evidence.tgz`

## Why Real Backend Test Is Not Run Locally Now

Running Real Backend Test against the current public origin now would violate the objective ordering. The objective requires server pull/deploy and production preflight first. Since public origin still exposes stale Works DTO fields and no server-side deploy evidence is present, a load result would be ambiguous and should not drive HLS/detail/DB slice selection.

## Recommendation

Do not select the HLS/detail/DB implementation slice yet. First return the server-side evidence bundle from the current-main handoff script above. Once the summary is present, choose the next slice from the measured bottleneck and then run full E2E and CI through the normal `dev -> main` path.
