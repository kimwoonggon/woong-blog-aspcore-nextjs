# UI E2E Fix + AI Batch Deepening Audit

Generated: `2026-04-21T13:13:26+09:00`
Branch: `feat/phase-2-backend-discompostion`
Commit SHA: `066c48c1ed04d096d6e2a2cd9322c4b967c6aed1`

## Executive Summary

Overall status: **PASS**

This work completed the small isolated UI/test-fix PR and the selective AI batch chapter2.5 deepening without changing public API contracts, endpoint routes, DTO JSON shape, or backend physical boundaries.

The two known full-e2e failures are fixed:

- `/blog` mobile horizontal overflow now passes.
- Mermaid batch prompt textarea value update now passes.

AI batch persistence boundaries are clearer:

- Target selection/update methods are behind `IAiBatchTargetQueryStore`.
- Batch job read/query methods are behind `IAiBatchJobQueryStore`.
- Batch job mutation/unit-of-work methods are behind `IAiBatchJobCommandStore`.
- `IAiBlogFixBatchStore` remains only as a compatibility aggregate implemented by the existing EF store.

Final recommendation: **ready for the next low-risk cleanup PR**. The next cleanup should avoid broad redesign and can focus on removing compatibility aggregate usage later, or documenting/cleaning remaining namespace/folder naming friction.

## Root Cause And Fixes For E2E Failures

| Failure | Root cause | Fix |
| --- | --- | --- |
| `tests/ui-pub-overflow.spec.ts` -> `/blog avoids horizontal overflow on mobile` | Blog list cards could be widened by unbroken/generated titles, excerpts, or tags. The card path did not defensively force wrapping at every relevant level. | Added `min-w-0` and `[overflow-wrap:anywhere]`/`break-words` protection to the blog list link/card/header/title/excerpt/tag path. |
| `tests/mermaid-batch-prompt-0419.spec.ts` -> prompt textarea not updating | The batch AI panel async runtime-config initialization could rerun and overwrite a user/test-filled prompt after the textarea changed. | Added a prompt-touched ref guard so runtime config initialization does not overwrite prompt state after user input. |

## AI Batch Deepening Changes

| Area | Change | Behavior impact |
| --- | --- | --- |
| Store interfaces | Split broad store responsibilities into `IAiBatchTargetQueryStore`, `IAiBatchJobQueryStore`, and `IAiBatchJobCommandStore`. | No behavior change; method bodies remain on existing EF store. |
| Compatibility | Kept `IAiBlogFixBatchStore` as aggregate interface extending the narrow interfaces. | Low-risk migration path; no concrete store split yet. |
| DI | Registered the narrow interfaces to resolve to the existing scoped `IAiBlogFixBatchStore`. | Preserves one scoped EF store instance. |
| Handlers/orchestrators | Updated AI batch handlers, scheduler, runner, dispatcher, and item processor to depend on narrow interfaces. | Preserves current roles and behavior. |
| Architecture tests | Added guard preventing AI Application services from injecting the aggregate store directly. | Prevents responsibility drift. |
| Integration tests | Startup composition now resolves new AI batch store interfaces. | Confirms DI wiring. |

## Files Changed

UI/test-fix files:

- `src/app/(public)/blog/page.tsx`
- `src/components/admin/AdminBlogBatchAiPanel.tsx`

AI batch files:

- `backend/src/WoongBlog.Application/Modules/AI/Application/Abstractions/IAiBlogFixBatchStore.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BlogFix/FixBlogBatchCommandHandler.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/CreateBlogFixBatchJobCommandHandler.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/ListBlogFixBatchJobsQueryHandler.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/GetBlogFixBatchJobQueryHandler.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/ApplyBlogFixBatchJobCommandHandler.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/CancelBlogFixBatchJobCommandHandler.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/CancelQueuedBlogFixBatchJobsCommandHandler.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/ClearCompletedBlogFixBatchJobsCommandHandler.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/RemoveBlogFixBatchJobCommandHandler.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/AiBatchJobScheduler.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/AiBatchJobRunner.cs`
- `backend/src/WoongBlog.Application/Modules/AI/Application/BatchJobs/AiBatchJobItemProcessor.cs`
- `backend/src/WoongBlog.Infrastructure/Infrastructure/Ai/AiBatchJobItemDispatcher.cs`
- `backend/src/WoongBlog.Api/Modules/AI/AiModuleServiceCollectionExtensions.cs`

Test/report files:

- `backend/tests/WoongBlog.Api.ArchitectureTests/ArchitectureBoundaryTests.cs`
- `backend/tests/WoongBlog.Api.IntegrationTests/StartupCompositionTests.cs`
- `todolist-2026-04-21.md`
- `backend/reports/ui-e2e-fix-ai-batch-deepening-audit/*`

## Tests Added Or Updated

| Test | Purpose |
| --- | --- |
| `ArchitectureBoundaryTests.Ai_Batch_ApplicationServices_DoNotDepend_OnAggregateBatchStore` | Ensures AI Application services use narrower store interfaces instead of the compatibility aggregate. |
| `StartupCompositionTests.ServiceProvider_ResolvesApiApplicationAndInfrastructureServices` | Verifies `IAiBatchTargetQueryStore`, `IAiBatchJobQueryStore`, and `IAiBatchJobCommandStore` resolve from DI. |
| Existing Playwright e2e tests | Confirmed the two known UI failures are fixed and full stack is green. |

## Behavior Preservation Notes

- No AI endpoint route changed.
- No request/response DTO JSON shape changed.
- AI HTTP result mapping remains in Api.
- `IBlogAiFixService` remains an infrastructure adapter boundary.
- Background processor, scheduler, runner, dispatcher, and item processor roles remain intact.
- No atomic claim / DB concurrency redesign was introduced.
- Existing concrete EF store remains; only interface ownership changed.
- WorkVideo and other backend modules were not redesigned.

## Verification Performed

| Command | Result |
| --- | --- |
| `npx vitest run src/test/admin-blog-batch-ai-panel.test.tsx --pool=threads` | 10 passed |
| `npm run typecheck` | passed |
| `npm run test:e2e:stack -- tests/ui-pub-overflow.spec.ts:6 tests/mermaid-batch-prompt-0419.spec.ts:126` | 9 passed |
| `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj` | 13 passed |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~AdminAiEndpointsTests|FullyQualifiedName~StartupCompositionTests"` | 16 passed |
| `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj` | 27 passed |
| `dotnet test backend/WoongBlog.sln` | Contract 1, Unit 13, Component 26, Architecture 27, Integration 93 passed |
| `npm run test:e2e:stack` | 563 passed, 8 skipped |

Known non-blocking warning: existing low-severity `NU1901` warning for `AWSSDK.Core` remains.

## Deferred Items

- Removing `IAiBlogFixBatchStore` compatibility aggregate in a later cleanup PR.
- Splitting the concrete EF store into multiple classes only if future churn justifies it.
- Atomic claim / DB concurrency redesign for AI batch item claiming.
- AI options ownership split.
- Namespace normalization.
- Middleware relocation.
- Per-module csproj split.
- Further decomposition of IntegrationTests categories.

## Final Recommendation

The branch is ready for the next **low-risk cleanup PR**.

Suggested next cleanup target:

1. Remove or further shrink compatibility aggregate interfaces where consumers no longer need them.
2. Clean up naming/folder friction documented by the module-boundary audit.
3. Avoid another large selective deepening until the current branch is committed and reviewed.
