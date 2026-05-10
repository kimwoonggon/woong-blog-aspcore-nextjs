# GHCR Legacy Alias Permission Gate Audit - 2026-05-10

## Objective
Restore the CI/publish path after the main GHCR publish failed while pushing legacy package aliases, without blocking the required runtime image publish needed for production deploy.

## Changed
- `publish-ghcr-main.yml` now keeps runtime image tags as the required publish output.
- `publish-ghcr-dev.yml` now keeps runtime image tags as the required publish output.
- Legacy compose-compatible aliases are now pushed only by separate optional steps gated by `GHCR_PACKAGES_TOKEN`.
- `src/test/publish-ghcr-tags.test.ts` now verifies that legacy aliases are not part of the mandatory runtime push step.

## Intentionally Not Changed
- No application runtime code was changed.
- No production server deploy was performed from this environment.
- Existing legacy GHCR packages were not modified directly.
- Existing server helper scripts still make the production path use `-runtime-` image names.

## Evidence
- Failure being corrected: `Publish GHCR Main` run `25626302716` failed with GHCR `403 Forbidden` when pushing `woong-blog-aspcore-nextjs-backend/frontend:main` legacy aliases.
- Repository secrets check showed `PROMOTION_TOKEN` but no `GHCR_PACKAGES_TOKEN` at the time of this audit.
- Therefore legacy alias push must not be mandatory for the runtime image publish path.

## Validation
- RED: `npm test -- --run src/test/publish-ghcr-tags.test.ts` failed 2/2 before workflow changes.
- PASS: `npm test -- --run src/test/publish-ghcr-tags.test.ts` passed 2/2 after workflow changes.
- PASS: `npm test -- --run src/test/publish-ghcr-tags.test.ts src/test/compose-loadtesting-baseurl.test.ts` passed 7/7.
- PASS: `npm run typecheck` passed.
- PASS: `npm run lint` passed with existing warnings only.
- PASS: `git diff --check` passed.

## Risks And Follow-Up
- Legacy alias publishing will remain skipped until a repository secret named `GHCR_PACKAGES_TOKEN` with write access to the legacy GHCR packages is added.
- Production deployment should use `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main` and `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main`.
- The active production load-test objective is still incomplete until the server pulls/recreates the latest main runtime images and preflight/load tests are rerun.

## Recommendation
Promote this fix through `dev` and `main`, rerun `Publish GHCR Main`, verify runtime packages update successfully, then execute the production redeploy/preflight helper against the real server.
