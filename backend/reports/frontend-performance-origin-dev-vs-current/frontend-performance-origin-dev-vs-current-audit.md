# Frontend Performance Benchmark Audit

Generated: 2026-04-22

## Changed

- Added `scripts/benchmark-frontend-performance.mjs`, an external Node/Playwright benchmark runner for comparing `origin/dev` against the current working tree.
- Added `src/test/frontend-performance-benchmark.test.ts` to exercise report generation and classification behavior through the runner CLI self-test.
- Added `.tmp` to `.dockerignore` so local runtime Postgres/temp data is not sent to Docker build contexts.
- Generated benchmark reports under `backend/reports/frontend-performance-origin-dev-vs-current/`.
- Updated `todolist-2026-04-22.md` with task instructions, verification commands, report paths, and benchmark limitations.

## Not Changed

- No production frontend UI behavior was changed.
- No backend API, auth, persistence, or domain behavior was changed.
- No application performance optimization was attempted; this task only added benchmark tooling and reports.
- The existing dev compose stack on `127.0.0.1:3000` was not stopped.

## Goal Verification

- Baseline candidate: `origin/dev` in a temporary detached git worktree.
- Current candidate: current working tree on `feat/phase-2-backend-discompostion`, including uncommitted benchmark/report changes.
- Both candidates were built and served through Docker/nginx using isolated ports `32080/32081/18180`.
- Both candidates were seeded through authenticated admin APIs with Study posts, Works, pages, site settings, editable blog/work records, and a resume upload fixture.
- Reports were written as Markdown, HTML, and JSON.
- Runtime/load metrics covered public routes, public APIs, pagination interactions, admin editor saves, AI dialog readiness, resume load/API, and mutation-to-public-visible paths.

## Validation

- `node --check scripts/benchmark-frontend-performance.mjs` -> passed.
- `npx vitest run src/test/frontend-performance-benchmark.test.ts --pool=threads` -> passed.
- `npx eslint scripts/benchmark-frontend-performance.mjs src/test/frontend-performance-benchmark.test.ts` -> passed.
- `git diff --check` -> passed.
- `npm run typecheck` -> passed.
- Current-only runner smoke against the existing stack -> 45 scenarios, 0 failed after hardening.
- Comparative Docker smoke: `node scripts/benchmark-frontend-performance.mjs --warmups 0 --iterations 1 --mutation-warmups 0 --mutation-iterations 1 --base-url http://127.0.0.1:32080 --http-port 32080 --https-port 32081 --backend-port 18180 --report-dir backend/reports/frontend-performance-origin-dev-vs-current` -> reports generated.

## Risks And Yellow Flags

- The persisted comparison uses 0 warmups and 1 measured iteration per scenario, not the full requested 5/25 route/API and 2/10 mutation profile. Treat the numbers as a validated smoke comparison, not a statistically stable release benchmark.
- `127.0.0.1:3000` was already occupied by the existing dev stack, so the Docker comparison used isolated equivalent ports instead of the requested port.
- One current Study Next pagination click sample timed out in the final one-sample report. Direct Study page-2 load still measured, but the click sample should be rerun during the full benchmark.
- One-sample regressions in the report are sensitive to Docker startup, cold cache, and host load. The final release decision should use the default runner profile.

## Recommendation

Use the new runner for the full acceptance benchmark when `127.0.0.1:3000` can be dedicated to the comparison:

```bash
node scripts/benchmark-frontend-performance.mjs --report-dir backend/reports/frontend-performance-origin-dev-vs-current
```

If port `3000` remains occupied, use an isolated equivalent port set and keep the overridden ports recorded in the report.
