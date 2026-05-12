# Active Goal Runtime Redeploy Blocker Post-Publish Audit - 2026-05-13

## Decision

Do **not** mark the active goal complete.

The 2026-05-13 blocker report was published through `dev` and promoted to `main`, with CI and GHCR publication green. However, the original active goal still requires production server pull/deploy, production preflight, and a real backend load rerun. Those remain blocked by missing production SSH secrets and no successful `Production Runtime Redeploy` run.

## Current Published State

- Rechecked at: `2026-05-12T17:14:31Z`
- `origin/dev`: `e34765ed70b2abba7eb1e14ef963af3cfa8c7a60`
- `origin/main`: `4e50c0f899e2bae8b41238fd737a802ccad91a81`
- Report PR: #202, merged to `dev`
- Promotion PR: #203, merged to `main`
- `CI Dev` PR run: `25746491051`, success
- `CI Dev` push run: `25746982628`, success
- `CI Main Runtime` PR run: `25747364772`, success
- `CI Main Runtime` push run: `25747743785`, success
- `Publish GHCR Main` run: `25748067720`, success

## Current Runtime Images

- Backend image tag: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:sha-4e50c0f899e2`
- Backend digest: `sha256:8701e95460c966cb62a6cbf5df5c7471edceb3d8a3fb10411aa9fced03c4c10b`
- Frontend image tag: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:sha-4e50c0f899e2`
- Frontend digest: `sha256:5a9b6e3d07b916bbb07c2744dc303bdc2f785a6ba602b9cd7c9ac9730ebc09bc`

## Prompt-To-Artifact Checklist

| Requirement | Evidence inspected | Status |
| --- | --- | --- |
| Latest report state reaches `dev` | PR #202, `CI Dev` runs `25746491051` and `25746982628` | Complete |
| Latest report state reaches `main` | PR #203, `CI Main Runtime` runs `25747364772` and `25747743785` | Complete |
| Latest `main` runtime images published | `Publish GHCR Main` run `25748067720`, GHCR package API | Complete |
| Server pulled/deployed current `main` images | Required production workflow/server evidence | Missing |
| Production preflight after current deploy | Required production workflow/server evidence | Missing |
| Real Backend Test after current deploy | Required load summary | Missing |
| Real Backend Test uses `pageSize=12`, no seed/cache workaround, real Work/Study URL | Required load summary and guards | Missing |
| Select HLS/detail/DB next slice from result | Requires valid current-runtime load result | Missing |
| Full E2E after selected slice | Requires selected slice | Missing |
| CI green after selected slice | Requires selected slice | Missing |

## Current Blocker

`gh secret list --repo kimwoonggon/woong-blog-aspcore-nextjs` still exposes only:

- `PROMOTION_TOKEN`

Missing for `Production Runtime Redeploy`:

- `PROD_SSH_HOST`
- `PROD_SSH_USER`
- `PROD_SSH_PRIVATE_KEY`
- `PROD_SSH_KNOWN_HOSTS`

Latest `Production Runtime Redeploy` runs are still old failures from 2026-05-10:

- `25628772593`, failure
- `25628614468`, failure
- `25628508822`, failure

The workflow is active, but there is still no redeploy run for current `main@4e50c0f899e2bae8b41238fd737a802ccad91a81`.

The workflow path was re-inspected in `.github/workflows/prod-runtime-redeploy.yml`; `run_real_load=true` would execute server pull/deploy, production preflight, real load steps, and `scripts/prod-runtime-evidence-verify.sh`. That path remains unreachable until the required SSH secrets exist.

Secret routing was rechecked:

- `gh api repos/kimwoonggon/woong-blog-aspcore-nextjs/actions/secrets --jq '.secrets[].name'`: only `PROMOTION_TOKEN`
- `gh api orgs/kimwoonggon/actions/secrets --jq '.secrets[].name'`: HTTP 404 from GitHub API
- `gh api repos/kimwoonggon/woong-blog-aspcore-nextjs/environments --jq '.environments[].name'`: no environments returned
- `rg -n '^\s*environment:' .github/workflows/prod-runtime-redeploy.yml .github/workflows/*.yml`: no `environment:` binding in the production redeploy workflow

That means the workflow currently has no repo, org, or environment route that would populate `secrets.PROD_SSH_HOST`, `secrets.PROD_SSH_USER`, `secrets.PROD_SSH_PRIVATE_KEY`, or `secrets.PROD_SSH_KNOWN_HOSTS`.

Local environment input availability was also rechecked without printing secret values:

- `PROD_SSH_HOST`: missing
- `PROD_SSH_USER`: missing
- `PROD_SSH_PRIVATE_KEY`: missing
- `PROD_SSH_KNOWN_HOSTS`: missing
- `PROD_SSH_PORT`: missing
- `PROD_GHCR_TOKEN`: missing

So this agent session cannot populate GitHub Actions secrets or dispatch a useful production redeploy from locally available secret material.

## Public Origin Recheck

Public origin probes still do not prove current `main` deployment:

- `https://woonglab.com/api/health`: HTTP 200
- `https://woonglab.com/api/public/works?page=1&pageSize=12`: HTTP 200, returned 7 items
- `https://woonglab.com/api/public/blogs?page=1&pageSize=12`: HTTP 200, returned 12 items

The Work list still exposes stale fields:

- `iconUrl`
- `period`

Those stale keys mean the public origin is not acceptable evidence that `main@4e50c0f899e2bae8b41238fd737a802ccad91a81` has been pulled and deployed.

## Verifier Coverage Recheck

- `npm test -- --run src/test/prod-runtime-evidence-verify.test.ts`: passed, 1 file / 9 tests.
- This verifies the evidence validator behavior, not the active goal itself.
- Current-main handoff digest resolution was rechecked. `docker buildx imagetools inspect` reports OCI index digests matching the published GHCR values:
  - backend: `sha256:8701e95460c966cb62a6cbf5df5c7471edceb3d8a3fb10411aa9fced03c4c10b`
  - frontend: `sha256:5a9b6e3d07b916bbb07c2744dc303bdc2f785a6ba602b9cd7c9ac9730ebc09bc`
- The handoff script now resolves OCI index digests first and falls back to platform manifest digests only if Buildx is unavailable. This prevents exact pinned handoff runs from failing before deploy due to index-vs-platform digest mismatch.
- Root-wide evidence search found no `current-main-preflight-load-evidence.tgz`, `production-runtime-evidence.tar.gz`, `current-main-evidence-manifest.json`, or `production-runtime-evidence-manifest.json`.
- The only discovered `prod-real-load-steps-summary.json` is `.agent-runtime/dev-full-validation-20260511/backend/reports/main-runtime-local-prodlike-2026-05-11/evidence/local-real-load/prod-real-load-steps-summary.json`.
- That summary is not valid current-production evidence: `baseUrl` is `http://127.0.0.1:3020`, `generatedAt` is `2026-05-11T07:54:12.698Z`, and the first step is only `20` rps.

## Recommendation

Keep the goal active. The next material action is to provide production deploy/preflight/load evidence by either configuring the missing GitHub secrets and dispatching `Production Runtime Redeploy` with `run_real_load=true`, or running the server-side handoff script on the server and returning the evidence bundle.
