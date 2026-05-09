# Production Real Load Step Runner Audit - 2026-05-10

## Summary
- Added a production-safe real backend load step runner for the public Work/Study API mix.
- The runner keeps list targets realistic with `pageSize=12` and requires caller-provided real public Work/Study read paths instead of seeded defaults.
- The runner executes constant-arrival-rate k6 steps and writes per-rate plus aggregate JSON/Markdown artifacts to support the next performance decision.
- Added focused Vitest coverage for the script contract and included the script in the main runtime promotion allowlist.

## Changed
- `scripts/prod-real-load-steps.sh`
  - Runs real public API mix steps at `RATES`, defaulting to `200 300 400 500` rps.
  - Uses `BASE_URL`, `WORK_READ_PATH`, `STUDY_READ_PATH`, `DURATION_SECONDS`, `MAX_VUS`, and `PRE_ALLOCATED_VUS` as explicit operator inputs.
  - Enforces `LIST_PAGE_SIZE=12` so list targets remain production-realistic.
  - Requires real read target paths and fails fast if they are missing.
  - Captures per-target latency, response bytes, receiving time, app/nginx timing headers, failed rate, dropped iterations, and optional admin diagnostics.
  - Writes `step-<rate>rps-summary.json`, `step-<rate>rps-k6-raw.json`, optional diagnostics snapshots, `prod-real-load-steps-summary.json`, and `prod-real-load-steps-summary.md`.
- `src/test/prod-real-load-steps.test.ts`
  - Added RED/GREEN behavior coverage using fake `k6` and `curl` executables.
  - Covers required real read targets, pageSize 12 enforcement, configured rate execution, aggregate JSON/Markdown generation, and next-focus classification.
- `scripts/main-runtime-allowlist.txt`
  - Added `scripts/prod-real-load-steps.sh` so runtime promotion can carry the tool to main.
- `todolist-2026-05-10.md`
  - Recorded the slice plan, user-instruction mapping, RED/GREEN status, and validations.

## Intentionally Not Changed
- No backend public query code was changed in this slice.
- No DTO or serialization model was changed in this slice.
- No DB pool size, Npgsql setting, EF query, nginx config, Docker config, or cache behavior was changed.
- No seeded target fallback was introduced.
- No list page size reduction was introduced.
- No production load test was executed from this local workspace.

## Goal Verification
- Goal: provide a repeatable real backend load step gate before choosing the next heavy-detail optimization.
  - Satisfied. The script produces per-step and aggregate artifacts that classify the next focus as payload/network, app CPU/serialization, DB pool/resource pressure, or rate increase/soak extension.
- Goal: keep realistic public list target shape.
  - Satisfied. `LIST_PAGE_SIZE` defaults to `12` and any other value fails fast.
- Goal: avoid seed/default read targets.
  - Satisfied. `WORK_READ_PATH` and `STUDY_READ_PATH` are required inputs.
- Goal: improve bottleneck attribution.
  - Partially satisfied by tooling. The runner captures app/nginx timing, payload size, receive time, dropped iterations, failures, and optional DB diagnostics. Actual attribution still requires running it against production after runtime preflight is green.
- Goal: continue dev/main promotion compatibility.
  - Satisfied for this artifact. The script is in the main runtime allowlist.

## Validations
- RED: `npm test -- --run src/test/prod-real-load-steps.test.ts`
  - Failed before implementation because `scripts/prod-real-load-steps.sh` did not exist.
- GREEN focused: `npm test -- --run src/test/prod-real-load-steps.test.ts`
  - Passed 3/3.
- GREEN related Vitest bundle: `npm test -- --run src/test/prod-real-load-steps.test.ts src/test/prod-runtime-preflight.test.ts src/test/load-test-dashboard.test.ts`
  - Passed 35/35.
- TypeScript: `npm run typecheck`
  - Passed.
- Shell syntax: `for script in scripts/prod-real-load-steps.sh scripts/prod-runtime-preflight.sh scripts/promote-main-runtime.sh scripts/ci-compose-smoke.sh; do bash -n "$script"; done`
  - Passed.
- Whitespace: `git diff --check -- scripts/prod-real-load-steps.sh src/test/prod-real-load-steps.test.ts scripts/main-runtime-allowlist.txt todolist-2026-05-10.md`
  - Passed.
- JSON audit artifact: `python3 -m json.tool backend/reports/prod-real-load-steps-2026-05-10/prod-real-load-steps-2026-05-10.json`
  - Passed.

## Operator Command
Run after production runtime preflight is green and after choosing real public read targets from current content:

```bash
BASE_URL=https://woonglab.com \
WORK_READ_PATH=/api/public/works/<real-work-slug> \
STUDY_READ_PATH=/api/public/blogs/<real-study-slug> \
RATES="200 300 400 500" \
DURATION_SECONDS=30 \
MAX_VUS=500 \
PRE_ALLOCATED_VUS=100 \
ADMIN_COOKIE_FILE=/tmp/admin.cookies \
./scripts/prod-real-load-steps.sh
```

If admin diagnostics are not available, omit `ADMIN_COOKIE_FILE`; latency, failure, payload, receive, app, and nginx metrics are still captured.

## Risks And Follow-Up
- Local production load was not run here because this workspace does not have the real production server context, admin session, and k6 execution environment attached.
- The script depends on production exposing timing headers. If `X-Nginx-Request-Time` is missing, run the production runtime preflight first and fix stale nginx/runtime before interpreting load numbers.
- The aggregate next-focus heuristic is intentionally conservative. It guides the next code slice but does not replace reviewing raw step JSON and diagnostics.
- If the first saturated step shows large `responseBytesP95` or `receiveP95Ms`, the next code slice should target heavy public detail payload/body projection.
- If DB diagnostics show high pool/open latency or active connections, the next code slice should target detail roundtrips, EF query shape, and Npgsql pool/resource settings.

## Recommendation
Ship this runner through a PR to `dev`, wait for CI, then promote to `main` through the existing runtime path. On the server, run `prod-runtime-preflight.sh` first, then run `prod-real-load-steps.sh` against real current public read targets. Use the generated aggregate to choose the next heavy-detail code optimization instead of guessing from one spike run.
