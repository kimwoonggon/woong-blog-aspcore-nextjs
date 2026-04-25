# Backend Code Coverage Reporting - 2026-04-25

## Summary

Backend code coverage reporting was added for the existing .NET test taxonomy without changing production code and without enforcing a high threshold.

Changed:

- Added `backend/coverage.runsettings` for `XPlat Code Coverage` collection through `coverlet.collector`.
- Added `backend/.config/dotnet-tools.json` pinning `dotnet-reportgenerator-globaltool` 5.5.6.
- Added `scripts/run-backend-coverage.sh` with `unit`, `component`, `integration`, and `full` modes.
- Updated `backend/TESTING.md` with local coverage commands, output paths, quality guidance, and the unit/component/integration ownership split.
- Added `BackendCoverageToolingTests` to lock the coverage tooling contract.
- Updated `backend/reports/backend-test-coverage-audit-2026-04-24/` with current line/branch coverage, low-coverage areas, high-risk low-coverage areas, and recommended next targets.

Intentionally not changed:

- No production code was modified.
- No strict high coverage threshold was added.
- No CI gate was added for coverage percentages.
- Generated coverage output under `coverage/backend/` was left as ignored local output, not committed report artifacts.

## Goal Check

| Goal | Result |
|---|---|
| Backend-only scope | Passed. Changes are backend tooling/docs/tests/reports plus one root script matching existing backend runner style. |
| Existing .NET test tooling style | Passed. The runner uses `dotnet test`, category filters, project-specific targets, and the backend solution target. |
| Reports for UnitTests, ComponentTests, IntegrationTests, and full backend solution | Passed. All four reports were generated under `coverage/backend/<suite>/report/`. |
| Documentation for local generation and output | Passed in `backend/TESTING.md`. |
| Documentation that percentage is not the only quality metric | Passed in `backend/TESTING.md` and the updated audit. |
| Explain component/integration-owned backend areas | Passed in `backend/TESTING.md`. |
| Avoid strict high threshold | Passed. No threshold is enforced. |
| Update backend test audit/report with metrics and next targets | Passed. Markdown, HTML, and JSON audit artifacts were updated. |
| Run `dotnet test backend/WoongBlog.sln` | Passed. |

## Current Coverage

| Suite | Line coverage | Branch coverage | Report |
|---|---:|---:|---|
| UnitTests | 11.0% (141 / 1,276) | 4.0% (8 / 200) | `coverage/backend/unit/report/index.html` |
| ComponentTests | 40.3% (1,125 / 2,785) | 30.0% (193 / 642) | `coverage/backend/component/report/index.html` |
| IntegrationTests | 85.0% (3,329 / 3,912) | 51.5% (343 / 665) | `coverage/backend/integration/report/index.html` |
| Full backend solution | 90.1% (3,527 / 3,912) | 60.9% (405 / 665) | `coverage/backend/full/report/index.html` |

Full-suite assembly coverage:

| Assembly | Line coverage | Branch coverage |
|---|---:|---:|
| `WoongBlog.Api` | 99.3% | 69.5% |
| `WoongBlog.Application` | 93.7% | 60.2% |
| `WoongBlog.Domain` | 85.7% | 50.0% |
| `WoongBlog.Infrastructure` | 80.3% | 60.8% |

## Risk Areas

Low coverage production areas:

- `R2VideoStorageService`: 9.0% line, 0.0% branch.
- `ValidationExceptionFilter`: 0.0% line, 0.0% branch.
- `AppOpenIdConnectEvents`: 0.0% line.
- `AppCookieAuthenticationEvents`: 11.1% line, 0.0% branch.
- `BlogAiFixService`: 62.8% line, 38.2% branch.
- `WorkVideoPolicy`: 52.7% line, 31.6% branch.
- `AuthOptionsValidator`: 59.7% line, 66.6% branch.
- `SecurityOptionsValidator`: 50.0% line, 64.2% branch.
- `ForwardedHeadersOptionsFactory`: 55.1% line, 43.7% branch.
- `FfmpegVideoTranscoder`: 61.7% line, 50.0% branch.
- `ContentSearchText`: 60.0% line, 60.0% branch.

High-risk low-coverage areas are R2/object storage video paths, auth/OIDC/cookie event handling, AI provider execution and Codex runtime failures, WorkVideo policy/transcoding failures, validation exception payload shape, and proxy/security configuration.

## Recommended Next Targets

1. Add fake-backed R2/object-storage component and endpoint tests for WorkVideo issue, confirm, delete, and cleanup paths.
2. Add auth/OIDC event and cookie-auth edge tests for callback failure, missing claims, disabled auth, stale cookies, and role drift.
3. Add fake `HttpClient` AI provider tests for success, provider errors, timeouts, empty responses, and malformed responses.
4. Add validation exception filter tests for stable error payload shape.
5. Add WorkVideo policy/transcoder tests for unsupported MIME, empty or invalid video bytes, ffmpeg or ffprobe failures, stale versions, max-count limits, and HLS cleanup/rollback.
6. Add `ContentSearchText` and admin content JSON extraction tests for search update edge cases.

## Validation

| Command | Result |
|---|---|
| `npx skills find dotnet coverage` | Completed; no low-install external skill installed. |
| `dotnet tool search dotnet-reportgenerator-globaltool --take 1` | Found 5.5.6 and used it in the backend-local tool manifest. |
| `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj --filter FullyQualifiedName~BackendCoverageToolingTests` | Red first for missing artifacts, then passed 4 tests after implementation. |
| `bash -n scripts/run-backend-coverage.sh` | Passed. |
| `node -e "JSON.parse(...backend/.config/dotnet-tools.json...)"` | Passed. |
| `rg -n "run-backend-coverage|coverage/backend|coverage percentage|Component|Integration|Unit" backend/TESTING.md` | Passed. |
| `./scripts/run-backend-coverage.sh unit -v minimal` | Passed: Unit 14; generated unit coverage report. |
| `./scripts/run-backend-coverage.sh component -v minimal` | Passed: Component 76; generated component coverage report. |
| `./scripts/run-backend-coverage.sh integration -v minimal` | Passed: Integration 194; generated integration coverage report. |
| `./scripts/run-backend-coverage.sh full -v minimal` | Passed: Contract 1 skipped, Unit 14, Component 76, Architecture 35, Integration 194; generated full backend coverage report. |
| `dotnet test backend/WoongBlog.sln` | Passed: Contract 1 skipped, Unit 14, Component 76, Architecture 35, Integration 194. Existing NU1901 low-severity `AWSSDK.Core` advisory warning remains. |

## Risks And Deferred Items

- Coverage runs are slower than normal tests because ReportGenerator renders HTML and summaries after each collector run.
- Full coverage currently includes architecture tests and a skipped contract project; that is useful for the full solution baseline but should not be mistaken for behavior-only coverage.
- Branch coverage remains the more useful signal for several risk areas; full line coverage is high, but provider failures, invalid auth states, storage failures, and rollback paths still need targeted tests.
- Pact provider verification still requires `PACT_PROVIDER_BASE_URL` and pact files; local full runs skip it when absent.

## Recommendation

Keep the coverage tooling and documentation. Use the 90.1% line / 60.9% branch full-backend baseline as a starting point for prioritization, not as a gate. The next coverage work should focus on the high-risk low-coverage areas listed above before considering any threshold.

