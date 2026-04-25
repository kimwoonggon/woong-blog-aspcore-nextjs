# Backend AI Provider Failure Coverage - 2026-04-26

## Summary

Implemented backend-only AI provider failure and edge-path coverage without production-code changes and without real OpenAI, Azure OpenAI, Codex, or external service calls.

The change adds deterministic component coverage for `BlogAiFixService` OpenAI/Azure paths and unit coverage for `AiHttpResultMapper`. It intentionally leaves WorkVideo/R2/HLS coverage untouched.

## Previous Hotspot Summary

The prior backend coverage review identified AI provider execution as a P1 gap:

| Class | Previous line | Previous branch | Risk |
|---|---:|---:|---|
| `BlogAiFixService` | 62.8% | 38.2% | Provider failures, malformed JSON, empty choices, fallback behavior, and config failures were thin. |
| `AiHttpResultMapper` | 70.0% | 40.0% | NotFound, Conflict, and default 500 mapping were not fully locked down. |

Overall full backend coverage at that point was 90.1% line and 60.9% branch.

## Changes Made

- Added `backend/tests/WoongBlog.Api.ComponentTests/BlogAiFixServiceProviderComponentTests.cs`.
- Added `backend/tests/WoongBlog.Api.UnitTests/AiHttpResultMapperTests.cs`.
- Added a minimal UnitTests project reference to `WoongBlog.Api` so existing `InternalsVisibleTo("WoongBlog.Api.UnitTests")` can exercise the internal mapper.
- Added task tracking to `todolist-2026-04-26.md`.
- Added pre-change backup artifacts under `backend/reports/backend-ai-provider-failure-coverage-2026-04-26/prechange-backup/`.

No production APIs or production implementation files were changed.

## Cases Covered

OpenAI provider component coverage:

- success returns `choices[0].message.content` as cleaned HTML
- markdown-fenced HTML output is cleaned
- non-2xx responses throw `InvalidOperationException` with OpenAI context and payload
- malformed JSON throws
- empty `choices` returns empty `FixedHtml`
- missing `message.content` returns empty `FixedHtml`
- missing API key throws `OpenAI is not configured.`
- unknown provider falls back to OpenAI
- `OPENAI_MODEL` overrides configured `OpenAiModel` and is sent in the request

Azure OpenAI provider component coverage:

- success returns cleaned HTML
- trailing slash endpoint does not create a malformed double-slash URL
- request URL contains deployment and escaped `api-version`
- request includes `api-key` header
- non-2xx responses throw `InvalidOperationException` with Azure context and payload
- malformed JSON throws
- empty `choices` returns empty `FixedHtml`
- missing `message.content` returns empty `FixedHtml`
- missing API key or endpoint throws `Azure OpenAI is not configured.`
- `AZURE_OPENAI_DEPLOYMENT` and `AZURE_DEPLOYMENT_NAME` override configured deployment
- `AZURE_OPENAI_API_VERSION` overrides configured API version

Mapper unit coverage:

- `Ok` maps to HTTP 200
- `BadRequest` maps to HTTP 400 with error body
- `NotFound` maps to HTTP 404
- `Conflict` maps to HTTP 409 with error body
- unknown/default enum value maps to HTTP 500

## Coverage Results

| Scope | Line coverage | Branch coverage | Notes |
|---|---:|---:|---|
| Full backend, previous | 90.1% | 60.9% | From previous full coverage review. |
| Full backend, current | 91.3% | 64.8% | `coverage/backend/full/report/Summary.json`. |
| Component coverage, current | 41.9% | 33.6% | `coverage/backend/component/report/Summary.json`. |
| `BlogAiFixService`, previous full | 62.8% | 38.2% | Prior P1 hotspot. |
| `BlogAiFixService`, current component | 79.2% | 57.4% | Component-only provider path coverage. |
| `BlogAiFixService`, current full | 84.1% | 62.7% | Full backend coverage. |
| `AiHttpResultMapper`, previous full | 70.0% | 40.0% | Prior mapper gap. |
| `AiHttpResultMapper`, current full | 100.0% | 100.0% | Unit coverage through API internals. |

Measured improvements against the previous full report:

- Full backend line coverage: +1.2 percentage points.
- Full backend branch coverage: +3.9 percentage points.
- `BlogAiFixService` line coverage: +21.3 percentage points.
- `BlogAiFixService` branch coverage: +24.5 percentage points.
- `AiHttpResultMapper` line coverage: +30.0 percentage points.
- `AiHttpResultMapper` branch coverage: +60.0 percentage points.

## Validation Performed

| Command | Result |
|---|---|
| `mkdir -p /home/kimwoonggon/.codex/plugins/cache /home/kimwoonggon/.codex/.tmp/plugins` | Passed. |
| `readlink -f /home/kimwoonggon/.codex/plugins/cache` | Passed; absolute path verified. |
| `readlink -f /home/kimwoonggon/.codex/.tmp/plugins` | Passed; absolute path verified. |
| `npx --yes skills find dotnet testing` | Completed; no external skill installed because returned skills had low install counts. |
| `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter BlogAiFixServiceProviderComponentTests` | Passed; 20 passed. |
| `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj --filter AiHttpResultMapperTests` | Passed; 5 passed. |
| `./scripts/run-backend-coverage.sh component -v minimal` | Passed; 96 component tests passed and component coverage report regenerated. |
| `./scripts/run-backend-coverage.sh full -v minimal` | Passed after removing an overly broad direct UnitTests framework reference; coverage report regenerated. |
| `dotnet test backend/WoongBlog.sln` | Passed; contract verification skipped as configured, component/unit/architecture/integration tests passed. |
| `git diff --check` | Passed. |
| Node parse of component/full `Summary.json` | Passed; class metrics extracted. |

Known validation warning: `NU1901` for `AWSSDK.Core` 4.0.0.17 low-severity vulnerability appeared during .NET restores/tests. This was pre-existing and not introduced by this task.

## Intentionally Not Changed

- No production code was changed.
- No new production API surface was exposed.
- No real provider calls were made.
- WorkVideo, R2, object storage, and HLS coverage were not touched.
- Codex real process behavior beyond existing tests was not expanded in this task.

## Risks And Remaining AI Gaps

- Codex real process edge behavior still has room for deeper coverage: start failures, timeout boundaries, unauthenticated home variants, and stderr/stdout parsing beyond the existing fake-process tests.
- Image artifact resolution branches in `BlogAiFixService` remain partially covered.
- Prompt catalog fallback and malformed prompt-file behavior remain broader AI service gaps.
- Runtime config/provider policy matrix coverage can still be expanded for combined OpenAI, Azure, and Codex variants.
- AI batch/runtime policy behavior remains covered for major flows but not every policy edge.

## Final Recommendation

Treat the AI provider failure matrix as substantially reinforced. The next AI-focused coverage step should target Codex process and image artifact resolution edge cases. Keep WorkVideo/R2/HLS as a separate follow-up, as requested.
