# Public Work Visible DTO Trim - 2026-05-10

## Summary
Trimmed public Work DTOs and read projections to carry only fields currently rendered by the public UI. Public Work list/home cards no longer include `period` or `iconUrl`; public Work detail no longer includes `iconUrl` while retaining detail-visible `period` and `thumbnailUrl`.

## Goals Verified
- Keep load-test behavior realistic: no change to `pageSize=12`, target selection, seed shortcuts, or cache behavior.
- Reduce backend public response and projection width for Work list/home/detail reads.
- Preserve thumbnail behavior and existing resolver-equivalence coverage.
- Ensure stale production runtimes are caught before real load interpretation.

## Changed
- Removed `Period` and `IconUrl` from `WorkCardDto`.
- Removed `IconUrl` from `WorkDetailDto`.
- Updated `WorkQueryStore` public list/detail projections to stop selecting `Period` for cards and `PublicIconUrl` for public list/detail.
- Updated `HomeQueryStore` featured Work projection to stop selecting `Period` and `PublicIconUrl`.
- Updated frontend public Work/Home types and public Work detail parser.
- Updated public Pact contract and generated pact artifact.
- Extended `scripts/prod-runtime-preflight.sh` to fail when public Work detail still exposes stale `iconUrl` or stale public-video admin fields.

## Intentionally Not Changed
- Admin DTOs and admin editor behavior still expose icon asset/icon URL where needed.
- Database `PublicIconUrl` column and write-model maintenance remain in place for admin/editing compatibility.
- `thumbnailUrl` resolution and stored public thumbnail equivalence were not changed.
- Response cache behavior was not added or changed.
- Real Backend Test scenario/rate/max VU/page-size behavior was not changed.
- Existing covering index definition was not rebuilt in this slice to avoid startup-time index churn; public read SQL no longer selects hidden fields.

## Validation
- RED: focused public endpoint contract failed 3/3 before implementation because `iconUrl` and card `period` were still serialized.
- PASS: focused public endpoint contract passed 3/3 after implementation.
- PASS: focused component public query contracts passed 3/3.
- PASS: focused Postgres public Work detail/list projection contracts plus endpoint contracts passed 5/5.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"` passed backend suites: unit 61/61, component 129/129, architecture 40/40, integration 231/231; provider contract skipped by existing pact-file condition.
- PASS: `npm run typecheck` passed.
- PASS: public API/Pact/preflight/related frontend tests passed 45/45.
- PASS: `npm run lint` passed with 0 errors and 7 existing warnings.
- PASS: `bash -n scripts/prod-runtime-preflight.sh scripts/prod-real-load-steps.sh scripts/promote-main-runtime.sh scripts/ci-compose-smoke.sh` passed.
- PASS: `git diff --check -- <changed files>` passed.

## Risks And Follow-Up
- This is a public API contract change. Frontend contract tests and Pact artifact were updated, but any external consumer expecting `iconUrl` or list-card `period` must adapt.
- Existing databases may still have a covering index that includes `Period` and `PublicIconUrl`; this does not affect the selected public payload but may be revisited as a separate migration/index-maintenance slice.
- Production must deploy the current runtime before interpreting load-test results; preflight now catches stale `iconUrl` on Work detail when enabled.

## Recommendation
Merge through `dev`, wait for CI/publish, promote to `main`, then run production preflight with a real video Work detail and rerun the Real Backend Test. Use the new target DB/app/payload metrics to decide whether the next slice should target body serialization, DB index maintenance, or HLS upload failure handling.
