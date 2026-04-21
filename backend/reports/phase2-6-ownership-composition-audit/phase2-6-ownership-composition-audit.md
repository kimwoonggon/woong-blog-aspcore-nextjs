# Phase 2.6 Ownership & Composition Consolidation Audit

Generated: `2026-04-21T14:02:34+09:00`
Branch: `feat/phase-2-backend-discompostion`
Commit SHA: `066c48c1ed04d096d6e2a2cd9322c4b967c6aed1`

## Executive Summary

Overall status: **PASS**

This Phase 2.6 ownership/composition consolidation preserved behavior while reducing post-split friction:

- Removed the AI batch compatibility aggregate interface and kept only narrow target-query, job-query, and job-command store interfaces.
- Moved concrete Infrastructure adapter registrations behind Infrastructure-owned module registration helpers.
- Kept Application/Api composition readable by making Api module registrations delegate to Infrastructure helpers.
- Cleaned the most confusing Identity infrastructure path/namespace from `Modules/Identity/Application` to `Modules/Identity/Infrastructure`.
- Reviewed `AiOptions` ownership and intentionally deferred splitting it because the change is not low-risk.
- Fixed the two known unrelated full-e2e failures before final AI batch/ownership validation.

Final recommendation: **ready for review/merge**.

## Consolidation Changes

| Area | Change | Behavior impact |
| --- | --- | --- |
| AI compatibility aggregate | Removed `IAiBlogFixBatchStore`; `AiBlogFixBatchStore` now implements `IAiBatchTargetQueryStore`, `IAiBatchJobQueryStore`, and `IAiBatchJobCommandStore` directly. | No route, DTO, query, persistence, or runtime behavior change. |
| AI consumers | AI batch handlers, scheduler, runner, dispatcher, and item processor depend on narrow interfaces. | Same orchestration and scheduling semantics. |
| AI DI | `AddAiModuleInfrastructure` owns provider/store/dispatcher Infrastructure registrations. | Same lifetimes and scoped EF store behavior. |
| Module registrations | Api module service collection extensions delegate to Infrastructure-owned `Add*Infrastructure` helpers for concrete stores/storage/policies/providers. | Same services registered, clearer ownership. |
| Identity naming/path | `IIdentityInteractionService` and `IdentityInteractionService` moved to Infrastructure `Modules/Identity/Infrastructure` namespace/path. | No auth route or behavior change. |
| UI e2e fixes | Fixed `/blog` mobile overflow and batch AI prompt textarea race. | Behavior improved; no API contract changes. |
| Architecture tests | Added guards for AI aggregate removal and Api module registration concrete adapter imports. | Prevents ownership drift. |

## Files Changed

Representative production files:

- `src/app/(public)/blog/page.tsx`
- `src/components/admin/AdminBlogBatchAiPanel.tsx`
- `backend/src/WoongBlog.Application/Modules/AI/Application/Abstractions/IAiBlogFixBatchStore.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/*`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BlogFix/FixBlogBatchCommandHandler.cs`
- `backend/src/WoongBlog.Infrastructure/Infrastructure/Ai/AiBatchJobItemDispatcher.cs`
- `backend/src/WoongBlog.Infrastructure/Modules/AI/AiInfrastructureModuleServiceCollectionExtensions.cs`
- `backend/src/WoongBlog.Infrastructure/Modules/*/*InfrastructureServiceCollectionExtensions.cs`
- `backend/src/WoongBlog.Infrastructure/Modules/Identity/Infrastructure/*`
- `backend/src/WoongBlog.Api/Modules/*/*ServiceCollectionExtensions.cs`

Representative tests/reports:

- `backend/tests/WoongBlog.Api.ArchitectureTests/ArchitectureBoundaryTests.cs`
- `backend/tests/WoongBlog.Api.IntegrationTests/StartupCompositionTests.cs`
- `backend/reports/phase2-6-ownership-composition-audit/*`

## Remaining Compatibility Or Ownership Friction

| Item | Status | Rationale |
| --- | --- | --- |
| `AiOptions` still mixes Application runtime policy and Infrastructure provider/runtime settings | Deferred | Splitting affects startup validation, runtime config responses, provider availability, Codex settings, and component tests. Not low-risk for this consolidation PR. |
| Concrete `AiBlogFixBatchStore` still implements all narrow interfaces | Accepted | This PR improves dependency ownership without changing EF query behavior. Concrete split can wait until churn justifies it. |
| Api still composes Application orchestration services | Accepted | Api remains composition root for Application services; Infrastructure-owned concrete adapters are now behind Infrastructure helpers. |
| Namespaces still broadly use `WoongBlog.Api.*` | Deferred | Broad namespace normalization remains out of scope. |

## Tests Added Or Updated

| Test | Purpose |
| --- | --- |
| `ArchitectureBoundaryTests.Ai_Batch_AggregateBatchStore_IsRemoved` | Ensures aggregate store type and source usage do not return. |
| `ArchitectureBoundaryTests.Api_ModuleRegistrations_DoNotReference_ConcreteInfrastructureAdapters` | Ensures Api module registration files do not import concrete Infrastructure adapter namespaces. |
| `StartupCompositionTests.ServiceProvider_ResolvesApiApplicationAndInfrastructureServices` | Verifies narrow AI batch stores and existing representative services resolve from DI. |
| Existing e2e overflow and mermaid prompt tests | Confirm fixed UI/test failures. |

## Verification Performed

| Command | Result |
| --- | --- |
| `npx vitest run src/test/admin-blog-batch-ai-panel.test.tsx --pool=threads` | 10 passed |
| `npm run typecheck` | passed |
| `npm run test:e2e:stack -- tests/ui-pub-overflow.spec.ts:6 tests/mermaid-batch-prompt-0419.spec.ts:126` | 9 passed |
| `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj` | 13 passed |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~AdminAiEndpointsTests|FullyQualifiedName~StartupCompositionTests"` | 16 passed |
| `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj` | 28 passed |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~StartupCompositionTests|FullyQualifiedName~AdminAiEndpointsTests|FullyQualifiedName~AuthEndpointsTests|FullyQualifiedName~AdminMembersEndpointsTests|FullyQualifiedName~UploadsControllerTests|FullyQualifiedName~WorkVideoEndpointsTests"` | 35 passed |
| `dotnet test backend/WoongBlog.sln` | Contract 1, Unit 13, Component 26, Architecture 28, Integration 93 passed |
| `npm run test:e2e:stack` | 563 passed, 8 skipped |

Known non-blocking warning: existing low-severity `NU1901` warning for `AWSSDK.Core` remains.

## Deferred Items

- `AiOptions` ownership split.
- Atomic claim / DB concurrency redesign for AI batch item claiming.
- Splitting the concrete AI EF store into multiple classes.
- Broad namespace normalization.
- Middleware/auth ownership relocation.
- EF migrations workflow.
- Per-module csproj split.
- Removing old audit reports or consolidating accumulated report artifacts.

## Final Recommendation

The branch is **ready for review/merge**.

If another PR is desired before review, it should be a small low-risk cleanup only, not another deepening pass.
