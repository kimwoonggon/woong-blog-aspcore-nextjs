# Public Work Thumbnail Read Path Audit - 2026-05-06

## Summary
- Changed public Work list and home featured Work cards to resolve `thumbnailUrl` only from explicit `ThumbnailAssetId` assets.
- Changed public Work detail to keep returning video DTOs but stop using video rows to derive `thumbnailUrl`.
- Added behavior tests for home, list, and detail cases where a Work has a YouTube video but no explicit thumbnail asset.

## Intentionally Not Changed
- No cache layer was added.
- No load-test target URL selection, seed priority, or `pageSize=12` behavior was changed.
- No DB schema or migration was added.
- Admin Work reads still retain the legacy fallback resolver because admin behavior is not the hot public load path in this slice.
- Public Work and Blog detail still return `ContentJson`; public/admin DTO splitting remains a separate next slice.

## Goal Verification
- Real backend structure improved: public Work list/home no longer issue a video-table query solely to compute card thumbnails.
- Required DB shape improved: list/home continue projecting only card fields and asset IDs, then resolve explicit asset URLs.
- Public detail separation improved: video rows are used for the video list, not representative thumbnail fallback.
- Non-goals preserved: no cache-first fix, no fake seed target, no smaller `pageSize`, no unrealistic target selection.

## Validations
- RED: focused component tests initially failed 3 new tests because YouTube video fallback populated `thumbnailUrl`.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests"` passed 39/39 after implementation.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj` passed 121/121.
- PASS: `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj` passed 56/56.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj` passed 35/35.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PublicEndpointsTests|FullyQualifiedName~WorkVideoEndpointsTests"` passed 45/45.
- PASS: `dotnet test backend/WoongBlog.sln` passed component 121/121, unit 56/56, architecture 35/35, integration 202/202; contract provider test skipped because no pact files were present.
- PASS: `git diff --check`.
- PASS: grep check confirmed `HomeQueryStore` has no public `WorkVideos` thumbnail fallback path.

## Risks And Follow-Up
- Works without explicit `ThumbnailAssetId` now return an empty public card/detail thumbnail even if they have YouTube videos. This is intentional for this slice, but existing content should set representative thumbnail assets at write/admin time.
- Public detail still returns full `ContentJson`; large work/blog detail payload reduction remains necessary.
- DB command p95/p99 is still partially unavailable in the load-test UI; query instrumentation remains necessary for accurate DB timing.
- Real server load-test rerun is still required after this reaches the production-like deployment path.

## Final Recommendation
Merge this slice after CI, then continue with public/admin content DTO separation and DB command timing instrumentation. Those two are the next highest-value backend improvements for the 2 CPU / 8GB shared runtime constraint.

Generated: 2026-05-06T07:19:38+09:00
