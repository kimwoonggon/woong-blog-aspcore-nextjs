# Main GHCR Next.js SWC Fix Audit - 2026-05-11

## Summary

Fixed the `Publish GHCR Main` backend matrix failure that occurred before pushing the backend image. The failed backend matrix job ran the shared smoke build step:

```bash
docker build -f Dockerfile -t local/woong-blog-frontend:publish .
```

That frontend Docker build failed in Alpine during `npm run build` because Next.js could not load native SWC and the WASM fallback does not support Turbopack's `turbo.createProject`.

## Changed

- Added a Dockerfile contract test at `src/test/frontend-dockerfile-swc.test.ts`.
- Updated `Dockerfile` so the `builder` stage installs `libc6-compat` before `npm run build`.
- Updated `todolist-2026-05-11.md` with the blocker, goals, validations, and non-goals.

## Intentionally Not Changed

- No production SSH, deploy, or remote server commands were run.
- No secrets or production environment variables were changed.
- No load-test settings, cache behavior, page size, seed targeting, or backend performance code was changed in this slice.
- No image tag naming or GHCR publish workflow tag policy was changed.

## Goal Verification

- The direct failed path was reproduced by test as a missing Dockerfile contract.
- The direct failed path was then validated by a real local `docker build -f Dockerfile`.
- The fix is suitable for both `Publish GHCR Main` matrix jobs because both matrix jobs run the same local frontend/backend smoke image build step before pushing their selected image.

## Validations

- RED: `npx vitest run src/test/frontend-dockerfile-swc.test.ts --pool=threads --maxWorkers=2` failed before the Dockerfile change.
- GREEN: `npx vitest run src/test/frontend-dockerfile-swc.test.ts --pool=threads --maxWorkers=2` passed after the Dockerfile change.
- Docker build: `docker build -f Dockerfile -t local/woong-blog-frontend:swc-fix .` passed and completed `next build`.
- Targeted regression: `npx vitest run src/test/frontend-dockerfile-swc.test.ts src/test/publish-ghcr-tags.test.ts --pool=threads --maxWorkers=2` passed.

## Risks And Follow-Up

- CI still needs to confirm this in GitHub Actions because the original failure occurred in hosted Ubuntu runners.
- Existing warnings remain: Node.js 20 GitHub Action deprecation warnings and Dockerfile legacy `ENV key value` style warnings.
- `npm ci` still reports existing package audit warnings; this slice does not address dependency audit cleanup.

## Recommendation

Open a PR to `dev`, wait for CI, promote to `main`, then rerun or wait for `Publish GHCR Main`. The expected improvement is that backend matrix no longer fails during the local frontend smoke build and both runtime images can be published for the latest main SHA.
