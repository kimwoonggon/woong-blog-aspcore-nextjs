# WorkVideo Chapter 2.5 Deepening Audit

Generated: `2026-04-21T11:34:05+09:00`
Branch: `feat/phase-2-backend-discompostion`
Commit SHA: `066c48c1ed04d096d6e2a2cd9322c4b967c6aed1`

## Executive Summary

Overall status: **PASS**

The selective chapter2.5 WorkVideo deepening stayed within scope. It improved internal ownership clarity in two narrow places:

- HLS job storage-key/entity planning is now isolated in `WorkVideoHlsJobPlan`, keeping `StartWorkVideoHlsJobCommandHandler` focused on orchestration.
- Background cleanup persistence responsibilities moved out of `IWorkVideoCommandStore` into `IWorkVideoCleanupStore` and `WorkVideoCleanupStore`.

No endpoint routes, request/response DTO JSON shapes, upload/confirm/reorder/delete/HLS/playback behavior, or API result mapping were changed. The current handler -> port/store -> infrastructure adapter direction remains intact.

Final recommendation: **the next selective chapter2.5 PR can target AI batch**.

## Summary Of WorkVideo Deepening Changes

| Area | Change | Behavior impact |
| --- | --- | --- |
| HLS job planning | Added `WorkVideoHlsJobPlan` to build HLS prefix, manifest storage key, source key, sanitized original name, and `WorkVideo` entity. | No external behavior change; same key shape and entity values. |
| HLS command handler | `StartWorkVideoHlsJobCommandHandler` now delegates HLS naming/entity construction to `WorkVideoHlsJobPlan`. | No external behavior change; orchestration sequence remains validate -> inspect -> transcode -> publish -> persist -> project. |
| Cleanup persistence port | Added `IWorkVideoCleanupStore` for cleanup job/session expiration operations. | No external behavior change; same DbContext and same cleanup semantics. |
| Cleanup persistence implementation | Added `WorkVideoCleanupStore`; removed cleanup-only methods from `WorkVideoCommandStore`. | No external behavior change; cleanup queue behavior remains covered by endpoint/integration tests. |
| Delete video flow | `DeleteWorkVideoCommandHandler` now depends on cleanup store for cleanup enqueue. | No route/response change. |
| DI | Registered `IWorkVideoCleanupStore` in Works module composition. | Startup composition still resolves. |
| Architecture tests | Added guard that `IWorkVideoCommandStore` does not expose background cleanup-only methods. | Prevents responsibility drift. |

## Files Changed

Production files:

- `backend/src/WoongBlog.Application/Modules/Content/Works/Application/WorkVideos/WorkVideoHlsJobPlan.cs`
- `backend/src/WoongBlog.Application/Modules/Content/Works/Application/WorkVideos/IWorkVideoCleanupStore.cs`
- `backend/src/WoongBlog.Application/Modules/Content/Works/Application/WorkVideos/IWorkVideoCommandStore.cs`
- `backend/src/WoongBlog.Application/Modules/Content/Works/Application/WorkVideos/StartWorkVideoHlsJobCommandHandler.cs`
- `backend/src/WoongBlog.Application/Modules/Content/Works/Application/WorkVideos/DeleteWorkVideoCommandHandler.cs`
- `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/Persistence/WorkVideoCleanupStore.cs`
- `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/Persistence/WorkVideoCommandStore.cs`
- `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/WorkVideos/WorkVideoCleanupService.cs`
- `backend/src/WoongBlog.Api/Modules/Content/Works/WorksModuleServiceCollectionExtensions.cs`

Test/report files:

- `backend/tests/WoongBlog.Api.UnitTests/WorkVideoHlsJobPlanTests.cs`
- `backend/tests/WoongBlog.Api.IntegrationTests/StartupCompositionTests.cs`
- `backend/tests/WoongBlog.Api.ArchitectureTests/ArchitectureBoundaryTests.cs`
- `todolist-2026-04-21.md`
- `backend/reports/workvideo-deepening-audit/*`

## Tests Added Or Updated

| Test | Purpose |
| --- | --- |
| `WorkVideoHlsJobPlanTests.Create_BuildsHlsStoragePathsAndSanitizedVideoEntity` | Verifies the extracted HLS plan preserves path/source-key/entity construction. |
| `StartupCompositionTests.ServiceProvider_ResolvesApiApplicationAndInfrastructureServices` | Now also verifies `IWorkVideoCleanupStore` is registered. |
| `ArchitectureBoundaryTests.WorkVideo_CommandStore_DoesNotExpose_BackgroundCleanupResponsibilities` | Ensures cleanup-only responsibilities do not drift back into `IWorkVideoCommandStore`. |

Existing WorkVideo integration coverage was rerun:

- HLS upload/job behavior
- WorkVideo add/reorder/delete
- local upload confirm/delete cleanup
- cleanup expiration behavior
- startup composition

## Behavior Preservation Notes

- HLS source key format remains `storageType:videos/{workId:N}/{videoId:N}/hls/master.m3u8`.
- HLS manifest file name remains `master.m3u8`.
- HLS MIME type remains `application/vnd.apple.mpegurl`.
- HLS endpoint route remains `/api/admin/works/{id}/videos/hls-job`.
- WorkVideo endpoint result mapping remains in Api.
- Upload, confirm, reorder, delete, HLS, cleanup, and public playback behavior remain covered by existing integration tests.
- Cleanup enqueue still ignores YouTube sources and parses HLS source keys before enqueueing storage cleanup jobs.
- Cleanup and command stores are separate ports but use the same scoped `WoongBlogDbContext` through separate Infrastructure adapters.

## Verification Performed

| Command | Result |
| --- | --- |
| `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj` | 13 passed |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~WorkVideoEndpointsTests|FullyQualifiedName~StartupCompositionTests"` | 9 passed |
| `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj` | 26 passed |
| `dotnet test backend/WoongBlog.sln` | Contract 1, Unit 13, Component 26, Architecture 26, Integration 93 passed |
| `npm run test:e2e:stack` | 563 passed, 8 skipped |

Known non-blocking warning: existing low-severity `NU1901` warning for `AWSSDK.Core` remains.

Full e2e passed after the unrelated UI/test failures were fixed in the follow-up work. WorkVideo e2e coverage passed, including create, reorder, edit, mixed upload, S3-compatible HLS, public video playback/order, inline create/edit, and delete flows.

## Deferred Items

- Further split WorkVideo into its own project or module-level csproj.
- Larger `IWorkVideoCommandStore` decomposition into separate work, video, and upload-session stores.
- Broader WorkVideo orchestration redesign.
- Namespace normalization.
- Moving WorkVideo module DI registration out of Api into Infrastructure-owned registration helpers.
- Refactoring `ConfirmWorkVideoUploadCommandHandler`, which is also dense but was outside the requested primary target.
- Reworking cleanup semantics or storage deletion policy.

## Final Recommendation

The next selective chapter2.5 modular deepening PR should target **AI batch**.

Rationale:

- WorkVideo now has a clearer split between command mutation and background cleanup persistence.
- HLS-specific planning is isolated and tested.
- The remaining WorkVideo opportunities are larger and less urgent than AI batch complexity.
- AI batch still contains provider/background orchestration and batch-processing boundaries that would benefit from the same selective, conservative deepening approach.
