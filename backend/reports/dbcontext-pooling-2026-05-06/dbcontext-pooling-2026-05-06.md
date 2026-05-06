# DbContext Pooling - 2026-05-06

## Status
validated-locally-not-yet-promoted

## Summary
- Switched backend persistence registration from `AddDbContext` to bounded `AddDbContextPool` for `WoongBlogDbContext`.
- Added a TDD integration test proving context reuse across scopes and reset of tracked state after scope disposal.
- Kept Real Backend Test semantics realistic: list targets stay `pageSize=12`, read targets came from current public list order, and no cache was introduced.

## Changed
- Replaced AddDbContext with bounded AddDbContextPool for WoongBlogDbContext.
- Added default DbContext pool size 128 with optional Persistence:DbContextPoolSize override clamped to 16..1024.
- Kept existing load-test DB command and connection diagnostics interceptors registered in the pooled options pipeline.
- Added integration coverage proving a pooled context instance can be reused across scopes and that ChangeTracker state is reset when the scope is disposed.

## Intentionally Not Changed
- No public API response-shape change.
- No seeded target priority or pageSize=1 shortcut.
- No DB schema migration or representative image write model in this slice.
- No claim that this local dev run proves 1000rps capacity on a 2CPU/8GiB production host.

## Expected Backend Test Impact
- Primary expected movement: lower per-request EF service/context allocation and less GC pressure under sustained RPS.
- Secondary expected movement: small improvement or stabilization in ASP.NET app elapsed p95 when hot paths are DB-light but request-rate is high.
- Metrics to watch in Real Backend Test: app elapsed p95, GC heap delta, Gen2 GC delta, ThreadPool queue, DB command/open latency, and HTTP 5xx/timeout counts.
- This does not increase DB connection capacity by itself; Npgsql/Postgres pool sizing remains a separate operational constraint.

## Validation
- PASS: `Focused RED test before implementation` - Assert.Same failed because AddDbContext produced different DbContext instances across scopes.
- PASS: `Focused GREEN test after implementation` - Pooled context test passed and ChangeTracker state was empty after scope reuse.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter FullyQualifiedName~StartupCompositionTests --no-restore` - 15/15 passed.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter FullyQualifiedName~DbContextModelContractComponentTests --no-restore` - 4/4 passed.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` - Contract 1 skipped, Component 124, Unit 56, Architecture 35, Integration 225 passed. Existing AWSSDK.Core NU1901 warning remains.
- PASS: `git diff --check` - No whitespace errors.
- PASS: `Changed-source diff guard` - No added pageSize=1, seeded-work, seeded-blog, cache, or Cache in changed production/test source diff.
- PASS: `BACKEND_PUBLISH_PORT=18080 ./scripts/dev-up.sh` - Dev compose stack rebuilt/recreated with updated backend image.
- PASS: `curl /api/health through nginx and backend direct` - Both returned HTTP 200.
- PASS: `Real Backend 10rps/10s smoke` - 101 requests, 0 failed, p95 6.3ms, p99 24.6ms.
- PASS: `Real Backend 100rps/30s realistic target smoke` - 3001 requests, 0 failed, 100rps, p95 5ms, app p95 4.5ms, DB command p95 3.7ms.

## Load Test Evidence
- Selected Work slug: `real-load-work-fixture-pw-w0-r0-mou1dtp1-ac9ekh`
- Selected Blog slug: `real-load-study-fixture-pw-w0-r0-mou1dttl-q7ukcc`
- List target page size: `12`
- 10rps/10s smoke: 101 requests, 0 failed, p95 6.3ms, p99 24.6ms.
- 100rps/30s smoke: 3001 requests, 0 failed, 100rps, p95 5ms, p99 5.9ms, app p95 4.5ms.
- Runtime snapshot from 100rps/30s: processorCount 8, memory 216MiB, GC heap 10.3MiB, Gen2 2, DB command p95 3.7ms, DB connection open p95 0ms.
- Caveat: Local dev compose reported processorCount=8, so this is a regression/per-request-cost smoke, not a 2CPU/8GiB production capacity proof.

## Risks And Follow-Up
- DbContext pooling requires DbContext instances to remain stateless except for EF-tracked state; the new test covers ChangeTracker reset but not arbitrary future mutable fields.
- The default pool size 128 is intentionally bounded, but production should still align DbContextPoolSize with Npgsql Maximum Pool Size and the 2CPU/8GiB host profile if saturation appears.
- This slice will likely show modest gains, not a step-change in 1000rps spike capacity. Bigger remaining wins are payload/body split, JSON serialization source generation, and write-time representative media read models.

## Recommendation
Promote this low-risk structural optimization through dev CI, then continue with a measurement-heavy slice: verify deployed DB pool settings under real 2CPU/8GiB constraints and target heavy detail payload serialization/body split next.
