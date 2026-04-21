# Test Rebalancing PR Audit

Generated: `2026-04-21T11:20:13+09:00`
Branch: `feat/phase-2-backend-discompostion`
Commit SHA: `066c48c1ed04d096d6e2a2cd9322c4b967c6aed1`

## Executive Summary

Overall status: **PASS**

The test-rebalancing work satisfies the stated PR direction. Backend test taxonomy is now clearer:

- `WoongBlog.Api.UnitTests` contains focused Application/support helper and validator tests only.
- `WoongBlog.Api.ComponentTests` contains tests that instantiate Infrastructure, EF InMemory, ASP.NET runtime objects, filesystem/process fakes, or `HttpClient`.
- `WoongBlog.Api.IntegrationTests` remains the full ASP.NET host and endpoint/integration suite.
- `WoongBlog.Api.ArchitectureTests` remains the boundary enforcement suite and now includes a guard preventing UnitTests from reintroducing Infrastructure, ASP.NET App, or EF InMemory references.
- `WoongBlog.Api.ContractTests` remains Pact provider verification.

Production behavior and endpoint contracts were not changed. The only production-code change was adding `InternalsVisibleTo("WoongBlog.Api.ComponentTests")` to preserve the same internal Infrastructure access previously held by UnitTests after moving the component-style tests.

Final recommendation: **ready for selective chapter2.5 modular deepening PR**.

## What Changed

| Area | Change |
| --- | --- |
| Test project structure | Added `backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj` and added it to `backend/WoongBlog.sln`. |
| Unit test cleanup | Removed Infrastructure, Domain, ASP.NET App, EF InMemory, Bogus, NSubstitute, and Shouldly dependencies from `WoongBlog.Api.UnitTests`. UnitTests now references only Application. |
| Test movement | Moved Infrastructure/EF/ASP.NET/filesystem/HttpClient-backed tests from UnitTests to ComponentTests. |
| Test labeling | Added `Component` category and relabeled moved files with `[Trait(TestCategories.Key, TestCategories.Component)]`; remaining UnitTests now have explicit `Unit` traits. |
| Architecture guard | Added `UnitTestProject_DoesNotReference_Infrastructure_AspNetCore_Or_EfInMemory` to ArchitectureTests. |
| Documentation | Updated README test taxonomy note and added `backend/reports/test-rebalancing/test-inventory.md`. |
| Tiny production support | Added `InternalsVisibleTo("WoongBlog.Api.ComponentTests")` in Infrastructure assembly info. |

## Tests Moved

| Before | After | Reason |
| --- | --- | --- |
| `WoongBlog.Api.UnitTests/AuthRecorderTests.cs` | `WoongBlog.Api.ComponentTests/AuthRecorderComponentTests.cs` | Uses Infrastructure auth/persistence, EF InMemory, and ASP.NET `DefaultHttpContext`. |
| `WoongBlog.Api.UnitTests/AuthRedirectUriResolverTests.cs` | `WoongBlog.Api.ComponentTests/AuthRedirectUriResolverComponentTests.cs` | Uses Infrastructure auth resolver and ASP.NET request objects. |
| `WoongBlog.Api.UnitTests/BlogAiFixServiceCodexRuntimeTests.cs` | `WoongBlog.Api.ComponentTests/BlogAiFixServiceCodexRuntimeComponentTests.cs` | Uses Infrastructure AI service, temp filesystem, fake shell process path, and `HttpClient`. |
| `WoongBlog.Api.UnitTests/CodexRuntimeEnvironmentTests.cs` | `WoongBlog.Api.ComponentTests/CodexRuntimeEnvironmentComponentTests.cs` | Uses Infrastructure AI service, temp filesystem, fake executable script, and `HttpClient`. |
| `WoongBlog.Api.UnitTests/PublicQueryHandlerTests.cs` | `WoongBlog.Api.ComponentTests/PublicQueryHandlerComponentTests.cs` | Uses concrete Infrastructure query stores, DbContext, and EF InMemory. |

## Before / After Inventory

Detailed inventory: `backend/reports/test-rebalancing/test-inventory.md`.

Summary:

| Suite | Before | After |
| --- | --- | --- |
| ArchitectureTests | Boundary tests | Boundary tests plus UnitTests taxonomy guard |
| ContractTests | Pact provider verification | unchanged |
| IntegrationTests | Full host / endpoint / persistence integration | unchanged |
| UnitTests | Mixed true unit + component-style tests | true Application/helper/validator unit tests only |
| ComponentTests | did not exist | Infrastructure-backed component-style integration tests |

## Verification Performed

| Command | Result |
| --- | --- |
| `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj` | 12 passed |
| `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj` | 26 passed |
| `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj` | 25 passed |
| `dotnet test backend/WoongBlog.sln` | Contract 1, Unit 12, Component 26, Architecture 25, Integration 93 passed |
| `dotnet test backend/WoongBlog.sln --filter "Category=Unit"` | Unit 12 passed; other suites reported no matching tests |
| `dotnet test backend/WoongBlog.sln --filter "Category=Component"` | Component 26 passed; other suites reported no matching tests |
| `rg` check under `backend/tests/WoongBlog.Api.UnitTests` for Infrastructure/ASP.NET/EF InMemory/HttpClient/filesystem/runtime objects | no matches |
| `git diff --check` | clean |

Known non-blocking warning: existing low-severity `NU1901` warning for `AWSSDK.Core` remains.

## Goals / Non-Goals Check

| Item | Status | Evidence |
| --- | --- | --- |
| Rebalance backend test coverage toward architecture/integration/focused unit tests | PASS | New ComponentTests suite separates infrastructure-backed tests from UnitTests. |
| Reduce ambiguity in current test classification | PASS | Test inventory report and explicit traits added. |
| Preserve production behavior and physical architecture boundaries | PASS | No endpoint/DTO/handler/store redesign. ArchitectureTests remain green. |
| Do not split modules further or create per-module csproj projects | PASS | Only a test project was added. Production modules/projects unchanged. |
| Do not change routes or DTO JSON shape | PASS | No endpoint contract changes made. |
| Do not spend PR on namespace normalization, AI options split, middleware relocation, EF migrations | PASS | None of those changes were made. |

## Risks / Yellow Flags

| Risk | Impact | Disposition |
| --- | --- | --- |
| `ComponentTests` still depends on Infrastructure and ASP.NET | Intentional; this is the point of the new suite. | Accepted. |
| Existing IntegrationTests still mix full endpoint tests and some component-style integration tests | Could be refined later, but current split already removes ambiguity from UnitTests. | Deferred. |
| Codex runtime component tests overlap in coverage | Some duplication remains. | Deferred to cleanup if it becomes maintenance burden. |
| Infrastructure internal access now includes `WoongBlog.Api.ComponentTests` | Expands test-only internal visibility. | Accepted as minimal migration support. |
| `AWSSDK.Core` NU1901 warning remains | Existing dependency warning unrelated to this taxonomy PR. | Deferred. |

## Deferred Follow-Up Items

- Further separate endpoint integration tests from component-style integration tests inside `WoongBlog.Api.IntegrationTests` only if selective running becomes painful.
- Consolidate overlapping Codex runtime tests later.
- Keep namespace normalization for a separate PR.
- Keep AI options split, middleware relocation, EF migrations workflow, and per-module csproj extraction out of this PR.
- Consider a future architecture guard ensuring UnitTests contains only `Category=Unit`, if useful.

## Final Recommendation

The repository is ready for the next **selective chapter2.5 modular deepening PR**.

This PR has clarified test taxonomy without changing production behavior or weakening the Phase 2 physical split.
