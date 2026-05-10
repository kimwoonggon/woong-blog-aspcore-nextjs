# Dev VA-022 Home Rhythm Fix - 2026-05-11

## Scope

Fix the repeated dev full E2E failure:

- `tests/ui-quality-layout-rhythm.spec.ts:7`
- `VA-022 home sections keep a consistent vertical rhythm`

This task intentionally stays on the dev validation path. Production deploy/preflight and CI promotion were not executed in this slice.

## What Changed

Updated the global fade-in motion distance:

- File: `src/app/globals.css`
- Before: `@keyframes fadeInUp` started at `transform: translateY(20px)`
- After: `@keyframes fadeInUp` starts at `transform: translateY(8px)`

Reason: section containers use `animate-fade-in-up` with staggered delays. Playwright reads `boundingBox()` while delayed section transforms can still be active, so the first section gap was measured about 17px smaller than later gaps. Reducing the transient translate distance keeps the reveal motion and delay contract while keeping the measured rhythm inside the existing 16px tolerance.

## Intentionally Not Changed

- Did not change `tests/ui-quality-layout-rhythm.spec.ts` or relax the assertion.
- Did not remove `animate-fade-in-up` from home sections.
- Did not remove or reorder animation delays.
- Did not change homepage structure, content queries, cards, public API DTOs, backend behavior, or Real Backend Test target selection.
- Did not touch production deployment or CI workflow configuration.

## Backup

Before code edits:

- `.agent-backups/dev-va022-home-rhythm-2026-05-11/globals.css.before`
- `.agent-backups/dev-va022-home-rhythm-2026-05-11/todolist-2026-05-11.md.before`

## Validation

### Dev Stack Rebuild

Command:

```bash
COMPOSE_PROJECT_NAME=woong-blog-aspcore-nextjs APP_ENV_FILE=.env POSTGRES_DATA_DIR=.docker-data/dev/postgres LOCAL_CERTS_DIR=.local-certs BACKEND_PUBLISH_PORT=18080 NGINX_HTTP_PORT=3000 NGINX_HTTPS_PORT=3001 docker compose --env-file .env -f docker-compose.dev.yml up -d --build
```

Result: PASS.

Notes:
- Next.js production build completed successfully inside the frontend image.
- Existing `NU1901` low-severity `AWSSDK.Core` restore/publish warnings were emitted by backend build.
- Docker build context was large, about 2.01 GB, because local generated artifacts are present in the repository tree.

### Health Checks

Commands:

```bash
curl -fsS -o /dev/null -w 'health %{http_code}\n' http://127.0.0.1:3000/api/health
curl -fsS -o /dev/null -w 'home %{http_code}\n' http://127.0.0.1:3000/
```

Result:

```text
health 200
home 200
```

### Browser Gap Probe

After rebuild, direct browser measurement returned:

```json
{
  "gaps": [40.816925048828125, 48, 48],
  "diffsFromFirst": [7.183074951171875, 7.183074951171875]
}
```

The existing `VA-022` contract allows a maximum gap delta of `16px`, so this probe is within budget.

### Targeted E2E Subset

Command:

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible PLAYWRIGHT_E2E_PROFILE=core npx playwright test tests/ui-quality-layout-rhythm.spec.ts tests/ui-quality-motion-access-targets.spec.ts --project=chromium-authenticated --workers=1
```

Result: PASS, 6 passed.

Covered:
- `VA-022 home sections keep a consistent vertical rhythm`
- `VA-024 featured works grid keeps a consistent gap token`
- `VA-100 hero text and portrait stay visually balanced on desktop`
- `VA-305 public pagination controls keep 44px touch targets`
- `VA-400 and VA-401 mobile sheet motion stays under 400ms and avoids linear easing`
- `VA-405 home sections reveal with increasing fade-in delays`

### Full E2E

Command:

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npm run test:e2e
```

Result: PASS.

Summary:
- 429 passed
- 5 skipped
- 0 failed
- Latency artifacts: 434
- Latency budget failures: 0
- Latency warnings: 53

Artifacts:
- `/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/test-results/playwright/e2e-latency-summary.md`
- `/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/test-results/playwright/e2e-latency-summary.json`

## Goal Verification

- The repeated `VA-022` full-suite failure is fixed locally.
- Full E2E is green in the dev environment after rebuilding from the root worktree.
- Existing Real Backend Test smoke from the prior dev validation remains green and was not affected by this CSS-only change.

## Risks And Follow-Up

- The repository root remains dirty with unrelated pre-existing changes; those were not reverted or audited in this slice.
- CI was not run in this slice, so the broader objective's CI-success requirement is still not proven by this report.
- Production deployment/preflight remains out of this dev-focused slice.
- Docker context size is too large because generated local artifacts are included; consider excluding `.agent-runtime`, reports artifacts, and test output from Docker context in a separate cleanup.
- Full E2E latency warnings increased to 53, but there are 0 hard latency budget failures.

## Final Recommendation

Accept the minimal CSS motion-distance fix for the dev E2E failure. The next concrete gate, if pursuing the broader active objective, is CI verification for the branch/PR that contains this fix; production deployment remains a separate blocked/non-dev concern unless the user re-enables that scope.
