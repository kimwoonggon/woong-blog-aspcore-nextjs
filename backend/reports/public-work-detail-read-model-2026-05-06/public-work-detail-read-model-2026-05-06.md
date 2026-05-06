# Public Work Detail Read Model - 2026-05-06

## Objective
Improve the heavy public Work detail read path without cache, seeded-target shortcuts, or `pageSize=12` reduction. Specifically, remove request-time parsing of admin `AllPropertiesJson` for public `socialShareMessage` and keep the public/admin content model separated through stored public read-model fields.

## Changed
- Added `Work.PublicSocialShareMessage` as a stored public read-model field.
- Added EF model default and bootstrap schema patch/backfill for `Works.PublicSocialShareMessage`.
- Added write-time synchronization from `AllPropertiesJson.socialShareMessage` into `PublicSocialShareMessage` through the existing save-time synchronizer.
- Changed public Work detail projection to select `PublicSocialShareMessage` instead of `AllPropertiesJson`.
- Removed request-time `JsonDocument.Parse` from the public Work detail social-share path.
- Added component, integration, Postgres, and admin mutation coverage for the new read model.

## Intentionally Not Changed
- No cache was introduced.
- Real Backend target selection was not changed.
- Public list `pageSize=12` behavior was not changed.
- Seed priority behavior was not changed.
- Blog detail behavior was not changed in this slice.
- Work video payload shape was not changed; that remains the next heavy detail read target.
- Frontend code was not changed.

## Verification Against Goals
- Request-time body/admin JSON parsing reduction: satisfied for Work public `socialShareMessage`; public detail no longer projects or parses `AllPropertiesJson` for that value.
- Public/admin model separation: strengthened by storing the public social-share field separately from admin `AllPropertiesJson`.
- DB roundtrip goal: unchanged at current Work detail shape because videos are still a second query; this slice reduces selected payload and CPU allocation, not roundtrip count.
- JSON serialization allocation reduction: small positive impact by avoiding admin JSON projection and parsing; larger DTO/source-generation work remains deferred.
- Measurement realism: local load smoke kept `pageSize=12` for list and selected the Work detail slug from the current list response instead of hardcoding a seeded target.

## Validations
- RED: focused component tests failed to compile before implementation because `Work.PublicSocialShareMessage` did not exist.
- GREEN: focused component tests passed 4/4.
- GREEN: focused integration/Postgres/admin tests passed 5/5.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` passed Unit 56/56, Architecture 35/35, Component 122/122, Integration 206/206, Contract 1 skipped.
- PASS: `git diff --check` passed.
- PASS: current-branch backend Docker image rebuilt under the existing 2 CPU / 8 GiB split compose overlay.
- PASS: health/list probes returned HTTP 200 after rebuild.
- PASS: k6 Work detail current-slug smoke: 3,001 requests, 100.03 rps, 0 failures, p95 11.49 ms, max 235.50 ms.
- PASS: k6 Work list `pageSize=12` smoke: 3,001 requests, 100.03 rps, 0 failures, p95 3.52 ms, max 12.52 ms.

## Artifacts
- `backend/reports/public-work-detail-read-model-2026-05-06/loadtest/k6-work-detail-current-100rps-30s.json`
- `backend/reports/public-work-detail-read-model-2026-05-06/loadtest/k6-work-list-100rps-30s.json`
- `backend/reports/public-work-detail-read-model-2026-05-06/public-work-detail-read-model-2026-05-06.md`
- `backend/reports/public-work-detail-read-model-2026-05-06/public-work-detail-read-model-2026-05-06.html`
- `backend/reports/public-work-detail-read-model-2026-05-06/public-work-detail-read-model-2026-05-06.json`

## Risks And Follow-Up
- Work detail still performs a second DB query for `WorkVideos`; this is the next structural bottleneck to reduce.
- This slice does not prove 1000rps spike readiness. It proves the current branch remains healthy at 100rps under the constrained local compose profile.
- The local database has seeded/dev-sized content, so the load smoke validates query shape and runtime stability, not final production capacity.
- Host `k6` was unavailable; the same k6 script was run through `grafana/k6:1.0.0` on the compose network.

## Recommendation
Promote this slice through `dev` CI. The next backend performance slice should target Work detail video projection/serialization and detail request DB roundtrip counting before considering source-generated JSON or cache.
