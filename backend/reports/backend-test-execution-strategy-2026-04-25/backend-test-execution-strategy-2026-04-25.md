# Backend Test Execution Strategy Audit

Generated: `2026-04-25T18:16:12+09:00`
Branch: `feat/backend-architecture-test-code`
Commit SHA at start: `a3c8a9afc117159c260e72c9f075082548d76ee4`

## Executive Summary

Overall status: **PASS**

Backend test execution is now easier to run locally and safer to split in CI. The test taxonomy remains `Unit`, `Component`, `Architecture`, `Integration`, and `Contract`; runner scripts provide simple category commands; backend testing documentation now explains local/CI usage and external-service requirements; and both backend CI workflows split tests into logical unit, component, architecture, and integration jobs.

Final recommendation: keep these changes and let the split GitHub Actions jobs validate on the next push/PR. Contract verification still needs pact files and `PACT_PROVIDER_BASE_URL` when the team wants real provider verification.

## What Changed

| Area | Change |
| --- | --- |
| Taxonomy constants | Added `Contract` to each backend test project's `TestCategories` helper and added a matching helper to `WoongBlog.Api.ContractTests`. |
| Contract trait | Changed `ProviderContractVerificationTests` to use `[Trait(TestCategories.Key, TestCategories.Contract)]` instead of literal strings. |
| Runner scripts | Added `scripts/run-unit-tests.sh`, `scripts/run-component-tests.sh`, `scripts/run-architecture-tests.sh`, `scripts/run-integration-tests.sh`, and `scripts/run-backend-tests.sh`. |
| Testing docs | Added `backend/TESTING.md` with category meanings, when to run each suite, local commands, CI commands, and external-service notes. |
| README | Updated the backend test section with runner commands and a pointer to `backend/TESTING.md`. |
| CI | Split backend tests in `ci-dev.yml` and `ci-main-runtime.yml` into unit, component, architecture, and integration jobs. Integration keeps the Docker availability check for Testcontainers. |
| Audit/TODO | Updated `todolist-2026-04-25.md` and generated this persistent audit report. |

## What Was Intentionally Not Changed

- No production source code was changed.
- No new feature tests were added.
- No endpoint routes, DTO contracts, persistence behavior, auth behavior, Docker compose files, package versions, or runtime configuration were changed.
- Pact provider verification behavior was not loosened; it still skips unless pact inputs are configured.
- Existing frontend jobs and browser smoke logic were not redesigned beyond updating backend job dependencies.

## Test Category Policy

| Category | Meaning | External services |
| --- | --- | --- |
| `Unit` | Pure Application/helper/validator tests. | None. |
| `Component` | Application plus Infrastructure behavior through EF InMemory, fakes, temp filesystem, fake process seams, or direct service/store composition. | None; no real external AI/storage/network. |
| `Architecture` | Project reference, layer boundary, source-token, and composition-order rules. | None. |
| `Integration` | Full ASP.NET host/startup/endpoint behavior and persistence integration contracts. | Docker is required for `PostgresPersistenceContractTests` via Testcontainers and `postgres:16-alpine`. |
| `Contract` | Pact provider verification. | Requires `PACT_PROVIDER_BASE_URL` and pact files under `tests/contracts/pacts` or `PACT_FILE_DIRECTORY`; otherwise explicitly skipped. |

## Goal Check

| Goal | Status | Evidence |
| --- | --- | --- |
| Inspect backend test projects and category traits | PASS | Solution and trait inventory completed with `dotnet sln`, `rg`, and source reads. |
| Ensure backend tests have correct category traits | PASS | Every executable backend test file with `Fact`, `Theory`, or `PactProviderFact` has a category trait; ContractTests now uses `TestCategories`. |
| Add/update backend test runner scripts | PASS | Five executable runner scripts added under `scripts/`. |
| Update backend testing documentation | PASS | `backend/TESTING.md` covers category meaning, run timing, local commands, CI commands, and external services/Testcontainers. |
| Split GitHub Actions backend jobs | PASS | `ci-dev.yml` and `ci-main-runtime.yml` now have separate unit, component, architecture, and integration jobs. |
| Avoid unnecessary CI slowdown | PASS | Category scripts target their owning test projects, and only integration checks Docker. Jobs can run in parallel. |
| Do not add feature tests | PASS | No feature tests were added. |
| Run required commands | PASS | All five requested `dotnet test` commands passed locally. |

## Validation Log

| Command | Result |
| --- | --- |
| `npx skills find dotnet testing ci` | Completed; no skill installed because returned skills had low install counts and local repo skills were sufficient. |
| `dotnet sln backend/WoongBlog.sln list` | Passed; confirmed five backend test projects are included. |
| `comm -23 <(rg -l "\[(Fact\|Theory\|PactProviderFact)" backend/tests -g "*.cs" \| sort) <(rg -l "\[Trait\(" backend/tests -g "*.cs" \| sort)` | Passed; no executable backend test files without a trait. |
| `bash -n scripts/run-unit-tests.sh scripts/run-component-tests.sh scripts/run-architecture-tests.sh scripts/run-integration-tests.sh scripts/run-backend-tests.sh` | Passed. |
| `python3 -c 'import sys, yaml; [yaml.safe_load(open(path, encoding="utf-8")) for path in sys.argv[1:]]; print("yaml ok")' .github/workflows/ci-dev.yml .github/workflows/ci-main-runtime.yml` | Passed. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Unit"` | Passed: 14 unit tests. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Component"` | Passed: 76 component tests. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Architecture"` | Passed: 31 architecture tests. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Integration"` | Passed: 194 integration tests. |
| `dotnet test backend/WoongBlog.sln` | Passed: Contract 1 skipped, Unit 14 passed, Component 76 passed, Architecture 31 passed, Integration 194 passed. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Contract"` | Passed with 1 skipped Pact provider test because pact inputs are not configured. |

All `dotnet test` runs emitted the existing low-severity `NU1901` advisory warning for `AWSSDK.Core` 4.0.0.17 (`GHSA-9cvc-h2w8-phrp`).

## Risks And Yellow Flags

| Risk | Impact | Disposition |
| --- | --- | --- |
| GitHub Actions were edited but not executed locally. | Workflow syntax was parsed, but runner behavior still needs GitHub validation. | Accept; next push/PR should verify. |
| Integration tests require Docker/Testcontainers. | Developers without Docker cannot run the full integration category or full backend suite. | Documented in `backend/TESTING.md`; CI integration job checks `docker version`. |
| Contract tests skip without pact inputs. | Full suite can be green while provider verification is not performed. | Existing explicit skip preserved and documented; use `scripts/pact-provider-verify.sh` with pact files for real verification. |
| Runner scripts use project-specific category runs for speed. | A miscategorized test in a different project would not be selected by the matching script. | Direct solution filter commands remain documented and were used for validation. |
| Existing `AWSSDK.Core` advisory warning remains. | Build/test logs stay noisy and should be remediated separately. | Deferred; unrelated to execution strategy. |

## Final Recommendation

Keep the execution strategy changes. The backend suite now has clear local commands, CI category jobs, documented Testcontainers/Pact requirements, and verified solution-level category filters. Next step is to let GitHub Actions run the split jobs and separately configure Pact provider inputs when real contract verification is required.
