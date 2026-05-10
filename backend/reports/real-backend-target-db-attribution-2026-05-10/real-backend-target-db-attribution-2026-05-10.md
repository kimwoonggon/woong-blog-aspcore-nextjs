# Real Backend Target DB Attribution Audit - 2026-05-10

## Objective
Add target-level database command attribution to Real Backend Test so Work list/read and Study list/read rows show whether latency is caused by DB command time, app/serialization CPU, payload transfer, or nginx/network time.

## Changed
- Added request-scoped DB command elapsed/count aggregation.
- EF command interceptor now records each command into both the existing global diagnostics collector and the active request-scoped diagnostics scope.
- API timing middleware now emits `X-Db-Command-Elapsed-Ms` and `X-Db-Command-Count` alongside `X-App-Elapsed-Ms`, reading from the request scope object so delayed `OnStarting` callbacks keep the correct snapshot.
- k6 real runner records those headers as per-target trends.
- `RealLoadTestTargetMetrics` now includes `DbCommandElapsedP95Ms` and `DbCommandCountP95`.
- Fake real runner emits deterministic DB command target metrics for UI/control-plane coverage.
- Real backend target summary UI now shows `DB P95` and `DB Cmds` columns.
- Dashboard parser preserves target-level DB command metrics in snapshots.
- Added behavior tests for k6 script, request-scoped diagnostics, startup headers, fake-runner metrics, and dashboard parsing.
- Added `next-plan-after-ci.md` so the next plan is persisted for use after CI completes.

## Intentionally Not Changed
- No cache behavior was added or changed.
- No load-test target selection logic was changed.
- `pageSize=12` behavior was not changed.
- No seed target shortcut was introduced.
- No public content model/query projection change was made in this slice; this is measurement groundwork for the next heavy detail optimization.
- No nginx config change was made; this slice uses backend response headers consumed by k6.

## Prompt-To-Artifact Checklist
- Target-level DB/pool pressure visibility: `RealLoadTestTargetMetrics` has DB command elapsed/count fields and k6 records target header trends.
- App vs DB vs network diagnosis: target summary now has `P95`, `DB P95`, `DB Cmds`, `Payload P95`, `Receive P95`, while existing component timing still shows app/nginx/db aggregate.
- No cache: no cache files or cache policy changed.
- Keep realistic targets: no `pageSize` or seed target behavior changed.
- TDD: RED failures were observed for missing k6 DB metrics and dashboard parsing before implementation.
- Persistent TODO: `todolist-2026-05-10.md` updated with mapping and validation results.
- Backup: `.agent-backups/real-backend-target-db-attribution-2026-05-10/` created before edits.
- Audit artifacts: Markdown, HTML, JSON, and next-plan files created in this report directory.

## Validation
- RED: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~RealLoadTestRunnerComponentTests.K6Script_RecordsTargetDbCommandTimingForHeavyDetailAttribution" --no-restore --logger "console;verbosity=minimal"` failed before implementation because k6 did not record target DB command metrics.
- RED: `npm test -- --run src/test/load-test-dashboard.test.ts -t "summarizes real backend run status"` failed before implementation because dashboard target metrics dropped DB command fields.
- PASS: focused k6 script test passed 1/1 after implementation.
- PASS: `npm test -- --run src/test/load-test-dashboard.test.ts -t "summarizes real backend run status"` passed 1/1.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~RealLoadTestRunnerComponentTests|FullyQualifiedName~RequestDatabaseDiagnosticsTests" --no-restore --logger "console;verbosity=minimal"` passed 6/6.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~StartupCompositionTests.RealLoadTestControlPlane_StartStatusMetricsAndStop_HappyPath_WhenRealRunnerDisabled_ForcesFakeRunner" --no-restore --logger "console;verbosity=minimal"` passed 1/1.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~StartupCompositionTests.HealthOpenApiAndRuntimeConfig_StartWithoutExternalServicesInTesting" --no-restore --logger "console;verbosity=minimal"` passed 1/1.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --no-restore --logger "console;verbosity=minimal"` passed 129/129.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~StartupCompositionTests" --no-restore --logger "console;verbosity=minimal"` passed 17/17.
- PASS: `npm test -- --run src/test/load-test-dashboard.test.ts` passed 29/29.
- PASS: `npm run typecheck` passed.
- PASS: `npm run lint` passed with 0 errors and 7 existing warnings.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"` passed backend unit/component/architecture/integration suites; provider contract test was skipped by its existing pact-file condition.
- PASS: post-hardening focused request diagnostics test passed 2/2.
- PASS: post-hardening focused startup health/header test passed 1/1.
- PASS: post-hardening `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"` passed backend unit/component/architecture/integration suites; provider contract test was skipped by its existing pact-file condition.
- PASS: `git diff --check -- <changed files>` passed.

## Risks And Follow-Ups
- New DB command headers are emitted for all API responses. They contain only count and elapsed milliseconds, not SQL or secrets.
- Request-scoped aggregation uses `AsyncLocal`; this is appropriate for per-request diagnostics but should be kept simple and not used for business logic.
- Real production usefulness is not proven until the updated image is deployed and a Real Backend Test run shows non-null target DB metrics.
- If production nginx strips custom headers, k6 will show unavailable target DB metrics; preflight should check the headers directly after deployment.

## Final Recommendation
Ship this slice through `dev` and `main`, then run production preflight and Real Backend Test. Use the new target-level `DB P95` and `DB Cmds` columns to choose the next performance slice instead of guessing.
