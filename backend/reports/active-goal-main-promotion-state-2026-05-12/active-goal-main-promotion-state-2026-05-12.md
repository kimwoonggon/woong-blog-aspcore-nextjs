# Active Goal Main Promotion State Audit - 2026-05-12

## Objective Restatement

The active objective requires all of the following:

- Main runtime images are pulled and deployed on the server.
- Production preflight runs after that deploy.
- Real Backend Test reruns with `pageSize=12`, no seed shortcut, no cache shortcut, and real Work/Study URLs.
- A next performance/fix slice is selected from the Real Backend Test result.
- The selected slice is implemented and verified by full E2E and CI.

## Evidence Checklist

| Requirement | Evidence | Status |
| --- | --- | --- |
| Dev-scoped slice implemented | PR #185, commit `78f32f350802ce5a05dcfc7a2151208d6ef99901` | Complete |
| Local full dev E2E after slice | `npm run test:e2e`: 431 passed, 4 skipped, latency budget failures 0 | Complete |
| Dev PR CI | PR #185 CI Dev all checks success | Complete |
| Dev merge | PR #185 merged into `dev`, merge commit `304dc6595b28eff171ad33c7f039578dd1e0fb36` | Complete |
| Dev push CI | CI Dev run `25716884120` success after rerunning transient registry failure | Complete |
| Main promotion PR | PR #186 `release/main-promote -> main` created by Promote Main Runtime workflow | Complete |
| Main PR CI | CI Main Runtime run `25717220280` success | Complete |
| Main merge | PR #186 merged into `main`, merge commit `389a117ee8cda43e84536c85164bf13afd8e38bf` | Complete |
| Main push CI | CI Main Runtime run `25717479751` success | Complete |
| GHCR main runtime image publish | Publish GHCR Main run `25717736048` success | Complete |
| GHCR backend `:main` manifest | `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main` digest `sha256:ae0679f8166380e0edef07c3d546cc31c9c291380e6f7eccc691c225c44afdf2` | Complete |
| GHCR frontend `:main` manifest | `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main` digest `sha256:8cd7cdea54ad6388bcb34f3b8b694cf87e9721a35273cafd35d0f5fea0bfb5f3` | Complete |
| Anonymous GHCR manifest inspect | Empty `DOCKER_CONFIG` manifest inspect succeeded for backend and frontend `:main` images | Complete |
| Real public target candidates identified | Public GET scan from `https://woonglab.com` pageSize=12 lists selected `/api/public/works/water-fluid-simulation` and the long Study read path documented in handoff | Complete |
| Production workflow exists | `Production Runtime Redeploy` workflow is active and dispatchable | Complete |
| Production workflow required secrets exist | Repo secrets only contain `PROMOTION_TOKEN`; no repository environments are configured | Missing |
| Server pulls/deploys `main` runtime images | Not executed because production SSH/server work was explicitly excluded by user instruction | Missing |
| Production preflight after deploy | Not executed because server deploy was not executed | Missing |
| Real Backend Test after deploy with `pageSize=12`, no seed, no cache, real URLs | Input candidates are identified, but the test was not executed because production deploy/preflight was not executed | Missing |
| Next slice selected from production Real Backend Test result | Not possible without the Real Backend Test result | Missing |

## Current Conclusion

The code, dev CI, main runtime promotion, main CI, and GHCR `:main` runtime image publish are complete.

The active goal is not complete because production/server deploy, production preflight, and the post-deploy Real Backend Test were not executed. This is an intentional boundary, not a technical completion signal, because the user repeatedly instructed not to use production SSH or remote server operations.

There is also a concrete workflow readiness blocker: the `Production Runtime Redeploy` workflow requires `PROD_SSH_HOST`, `PROD_SSH_USER`, `PROD_SSH_PRIVATE_KEY`, `PROD_SSH_KNOWN_HOSTS`, and optionally `PROD_SSH_PORT`. `PROD_GHCR_TOKEN` is optional and currently not required for manifest visibility because anonymous manifest inspect succeeded for both runtime `:main` images. The repository currently exposes only `PROMOTION_TOKEN` through `gh secret list`, and no repository environments are configured.

## Recommendation

If production work is authorized later, first add the required production SSH secrets, then run the existing `Production Runtime Redeploy` workflow or equivalent server-side commands to pull the published `:main` images, execute production preflight, and then rerun Real Backend Test with real URLs and `pageSize=12`.
