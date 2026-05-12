# Active Goal Runtime Redeploy Blocker - 2026-05-13

## Decision

Do **not** mark the active goal complete.

The repository still lacks the SSH secrets required for the GitHub-hosted `Production Runtime Redeploy` workflow. No new successful production redeploy run exists, so there is still no evidence that the server pulled and deployed the latest `main` runtime images.

## Objective Restated As Concrete Deliverables

1. Server pulls and deploys the current `main` runtime images.
2. Production preflight passes against that deployed runtime.
3. Real Backend Test reruns after deployment with:
   - `pageSize=12`
   - no seed or fixture read targets
   - no cache workaround
   - real Work and Study URLs
4. The result selects one next slice: HLS fatal fix, public detail serialization/body optimization, or DB/index optimization.
5. Full E2E passes after the selected slice.
6. CI passes through the dev-to-main path after the selected slice.

## Prompt-To-Artifact Checklist

| Requirement | Evidence inspected | Status |
| --- | --- | --- |
| Latest `dev` ref | `git ls-remote origin refs/heads/dev` | Present: `c63426237052c8a503b7209befdf0855d7a8d8ad` |
| Latest `main` ref | `git ls-remote origin refs/heads/main` | Present: `0bb3fc8c3077112396ce9afaacd34ed0b9c23e6d` |
| Latest `main` CI green | Prior verified `CI Main Runtime` run `25744361298` | Present: success |
| Latest `main` runtime images published | Prior verified `Publish GHCR Main` run `25744784930` | Present: success |
| Production redeploy workflow can reach server | `gh secret list --repo kimwoonggon/woong-blog-aspcore-nextjs` | Missing required SSH secrets |
| New successful production redeploy exists | `gh run list --workflow "Production Runtime Redeploy" --limit 8` | Missing; latest runs are 2026-05-10 failures |
| Server-side pull/deploy happened | Workflow output or server evidence bundle | Missing |
| Production preflight after pull/deploy | Workflow output or server evidence bundle | Missing |
| Real Backend Test reran after current deploy | Current-runtime load summary | Missing |
| Real load used `pageSize=12` | Current-runtime load summary | Missing |
| Real load used no seed/fixture/cache workaround | Current-runtime load summary/guards | Missing |
| Result-based next slice selected | Valid current-runtime load result | Missing |
| Full E2E after selected slice | Test evidence after selected slice | Missing |
| CI green after selected slice | CI evidence after selected slice | Missing |

## Current Evidence

### Branch State

- `origin/dev`: `c63426237052c8a503b7209befdf0855d7a8d8ad`
- `origin/main`: `0bb3fc8c3077112396ce9afaacd34ed0b9c23e6d`

### Already Verified Runtime Image State

- `CI Main Runtime` run `25744361298`: success for `main@0bb3fc8c3077112396ce9afaacd34ed0b9c23e6d`
- `Publish GHCR Main` run `25744784930`: success for `main@0bb3fc8c3077112396ce9afaacd34ed0b9c23e6d`
- Backend image digest: `sha256:441aed27ca41e588f045954c5092e2401ce4ad7fc54d8fb1f95fa4c798caa692`
- Frontend image digest: `sha256:02d6269629745fe581850b9ece4ffca8246f32a214ec515263c90be7af74a204`

### Secret Readiness Recheck

`gh secret list --repo kimwoonggon/woong-blog-aspcore-nextjs` returned only:

- `PROMOTION_TOKEN`

Still missing:

- `PROD_SSH_HOST`
- `PROD_SSH_USER`
- `PROD_SSH_PRIVATE_KEY`
- `PROD_SSH_KNOWN_HOSTS`

### Production Runtime Redeploy Recheck

`gh run list --workflow "Production Runtime Redeploy" --limit 8` returned only old failed workflow-dispatch runs:

- `25628772593`, failure, created `2026-05-10T12:30:18Z`, head SHA `757ab203e55cadf8f89ee0da42b7ef580deebad3`
- `25628614468`, failure, created `2026-05-10T12:22:28Z`, head SHA `757ab203e55cadf8f89ee0da42b7ef580deebad3`
- `25628508822`, failure, created `2026-05-10T12:17:16Z`, head SHA `757ab203e55cadf8f89ee0da42b7ef580deebad3`

There is no successful production redeploy run for current `main@0bb3fc8c3077112396ce9afaacd34ed0b9c23e6d`.

## Intentionally Not Done

- Did not dispatch `Production Runtime Redeploy`; it would fail before useful evidence because required SSH secrets are absent.
- Did not SSH to production directly; the user explicitly excluded direct production SSH handling.
- Did not run or interpret a Real Backend Test from the public origin; current-main server deployment is still unproven.
- Did not select HLS/detail/DB optimization; selection requires a valid current-runtime load result.
- Did not call `update_goal`.

## Remaining Blocker

One of these must happen before this goal can continue materially:

1. Add `PROD_SSH_HOST`, `PROD_SSH_USER`, `PROD_SSH_PRIVATE_KEY`, and `PROD_SSH_KNOWN_HOSTS`, then dispatch `Production Runtime Redeploy` with `run_real_load=true`.
2. Run `backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh` on the server and return the evidence bundle.

## Recommendation

Keep the goal active. The next executable step is outside the current local/GitHub CI path: provide production deploy/preflight/load evidence through the workflow secrets or the server-side handoff bundle.
