# Public List Window Query Audit - 2026-05-06

## Summary

This slice reduces database roundtrips for the real backend list targets:

- `/api/public/works?page=1&pageSize=12`
- `/api/public/blogs?page=1&pageSize=12`

Before this change, each no-search first-page list request executed two PostgreSQL commands: one `COUNT(*)` query and one page projection query. The new PostgreSQL-specific path uses `COUNT(*) OVER()` so the first page and total count are returned in one command.

## Changed

- Added PostgreSQL first-page, no-search window-query paths in `WorkQueryStore` and `BlogQueryStore`.
- Kept the existing EF portable query path for search, page clamping, non-first pages, and non-PostgreSQL providers.
- Added Postgres contract tests that assert public Work/Blog first-page list reads use one DB command.
- Captured scoped k6 smoke results under the existing 2 CPU / 8 GiB split compose profile.

## Intentionally Not Changed

- No cache was added.
- No seeded-target priority or load target selection was changed.
- `pageSize=12` behavior was preserved.
- Public API response DTO shape was not changed.
- Search results and page>1 clamping behavior remain on the previous portable EF path.
- Detail endpoints and Work video loading were not changed in this slice.

## Goal Verification

- Goal: reduce DB pool pressure for realistic list targets without changing test semantics.
- Result: no-search first-page Work/Blog list reads now use 1 PostgreSQL command instead of 2.
- Scope fit: this directly targets 2 of the 4 real backend mix URLs and avoids cache or unrealistic target narrowing.

## Validations

- RED: focused Postgres command-count tests initially failed with `Expected: 1`, `Actual: 2` for Work and Blog list reads.
- GREEN: focused Postgres command-count tests passed 2/2 after implementation.
- `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests" --no-restore` passed 40/40.
- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PostgresPersistenceContractTests" --no-restore` passed 7/7.
- `dotnet test backend/WoongBlog.sln --no-restore` passed Unit 56/56, Architecture 35/35, Component 122/122, Integration 208/208; Contract 1 skipped due no pact files.
- `git diff --check` passed.
- Backend Docker image rebuilt under the existing 2 CPU / 8 GiB split compose overlay.
- Health/list probes returned HTTP 200 for health, Work list, and Blog list.
- k6 Work list cold 300rps/30s: 8,987 requests, 299.57 rps, 0 failures, p95 222.63 ms, max 1,582.32 ms.
- k6 Work list warm 300rps/30s: 9,001 requests, 300.05 rps, 0 failures, p95 4.58 ms, max 58.73 ms.
- k6 Blog list 300rps/30s: 9,001 requests, 300.04 rps, 0 failures, p95 2.54 ms, max 23.55 ms.
- Main promotion PR #64 for the previous slice had all CI Main Runtime checks green and was merged; main push CI later completed successfully while Publish GHCR Main was still in progress at audit time.

## Performance Scan Notes

Modified hot-path files were scanned for common .NET allocation and query-shape signals:

- `WorkQueryStore.cs`: LINQ hot-path hits 22, `.ToList` hits 9, `new List` hits 1, `.Substring` hits 0, `.ToLower/.ToUpper` hits 0, Regex hits 0, `SqlQuery` hits 1.
- `BlogQueryStore.cs`: LINQ hot-path hits 14, `.ToList` hits 5, `new List` hits 0, `.Substring` hits 0, `.ToLower/.ToUpper` hits 0, Regex hits 0, `SqlQuery` hits 1.

The new allocation/query risk is intentionally concentrated in one typed SQL projection per list store.

## Risks And Follow-Up

- The optimized SQL path is PostgreSQL-specific by design. Non-PostgreSQL tests/providers keep the old EF path.
- The first Work list k6 run showed cold-start/ramp p95 222.63 ms; the warm rerun showed p95 4.58 ms. Production assessment should distinguish cold deploy warm-up from steady-state load.
- Page>1 still uses two DB commands to preserve total-count clamping behavior. If page>1 becomes hot, a separate keyset or window-query paging design should be done explicitly.
- This does not fix Work detail video roundtrip or large detail body serialization costs.
- This does not by itself prove 1000rps spike capacity across the full four-target mix; it removes one DB command from each list request, which should reduce pool pressure in that mix.

## Final Recommendation

Merge this slice through `dev` after CI passes. Then continue with the next backend structure slice: Work detail video/read projection and large detail serialization pressure, using the same realistic 2 CPU / 8 GiB constraints.
