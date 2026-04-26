# Works / Study Hotfix Audit (2026-04-25)

## Summary
This pass stabilizes the remaining Works and Study issues from the follow-up report.

Implemented scope:
- `On This Work` now remains a Works-specific rail and dynamically moves below overlapping uploaded-video frames, so wide/theater video modes do not get covered.
- Uploaded/HLS videos keep native browser controls, desktop resize modes, and desktop-only hover preview; mobile/touch preview remains disabled.
- Works mobile list now auto-appends with an intersection sentinel instead of `Load more`, and stops at the end of the list.
- Study mobile back restore remains exact on mobile, while desktop width now keeps desktop pagination/layout instead of rendering the mobile restored-reading layout.
- Study excerpts are manual-only; blank excerpts stay blank, while body text is still extracted only for search indexing.
- Study list cards show all saved tags.
- WorkEditor client-side IDs now tolerate browsers where `crypto.randomUUID` is unavailable, including existing Work edit screens with metadata and video controls.
- E2E latency coverage now explicitly measures desktop public nav clicks and Work/Study detail-card opens.

## Intentionally Not Changed
- YouTube embeds still do not receive uploaded-video resize controls.
- Historical videos without preview assets were not reprocessed.
- Home recent-post cards and `More Studies` keep their existing tag-display policy.
- Public API wire shapes were not changed.
- Tablet compact lists keep manual `Load more`; automatic infinite scroll is mobile-focused.

## Goal Check
- Works rail covering videos: fixed. The rail calculates overlap against `[data-work-video-frame="true"]` and translates below the overlapping frame.
- Works mobile infinite scroll: fixed. Mobile Works uses `auto-infinite` mode and renders no `works-load-more` button.
- Study desktop/mobile switching: fixed. Exact mobile restore is mobile-only; desktop width returns to desktop pagination.
- Video control stability: fixed. Native controls remain on uploaded/HLS videos, and WorkEditor no longer crashes when `crypto.randomUUID` is missing.
- Study excerpt policy: fixed. Create/update preserve blank manual excerpts without content fallback.
- Intermittent stale public detail view after edit: not reproduced during this addendum. Existing save paths still await public revalidation before routing/refreshing; if it recurs, capture the exact slug and whether the URL was opened before save so cache tags can be traced.
- Public click slowness diagnosis: current local browser server is `next dev` behind the `3000` proxy. Cold first-click latency was dominated by route compilation and `generateStaticParams`, not slow backend APIs. Observed dev logs included `/blog/[slug]` around 6.8s and `/works/[slug]` around 8.7s on first compile; warm-state e2e latency was within budget.

## Validation
- `npm test -- --run src/test/table-of-contents.test.tsx src/test/public-responsive-feed.test.tsx src/test/responsive-page-size-sync.test.tsx src/test/work-video-player.test.tsx src/test/interactive-renderer.test.tsx`
  - Result: passed (`44/44`).
- `npm test -- --run src/test/work-editor.test.tsx`
  - Result: passed (`20/20`), including an existing-work edit screen with `crypto.randomUUID` unavailable.
- `node -e "... playwright chromium ... /api/auth/test-login?...returnUrl=/admin/works/new ..."`
  - Result: passed. Browser loaded `/admin/works/new` and rendered `Upload MP4 Video as HLS` with `window.crypto.randomUUID` removed.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible node scripts/run-e2e-latency.mjs -- tests/e2e-response-time.spec.ts --workers=1`
  - Result: passed (`9/9`), budget failures `0`, warnings `0`.
  - Key warm-state measured steps: Work detail card open `1241.78ms`, Study detail card open `1055.98ms`, Works nav `787.78ms`, Introduction nav `620.27ms`, Study nav `570.05ms`, Contact nav `504.86ms`.
  - Artifacts: `test-results/playwright/e2e-latency-summary.json`, `test-results/playwright/e2e-latency-summary.md`.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test --list`
  - Result: suite inventory only, `589 tests in 189 files`.
- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~AdminContentEndpointsTests|FullyQualifiedName~WorkVideoEndpointsTests"`
  - Result: passed (`30/30`) with existing `AWSSDK.Core` NU1901 low-severity advisory warnings.
- `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter FullyQualifiedName~PublicQueryHandlerComponentTests`
  - Result: passed (`18/18`) with existing `AWSSDK.Core` NU1901 low-severity advisory warnings.
- `BACKEND_PUBLISH_PORT=18080 docker compose -f docker-compose.dev.yml up -d --build frontend`
  - Result: passed; rebuilt backend/frontend containers for browser validation.
- `docker run --rm --network woong-blog-aspcore-nextjs_default ... npx playwright test tests/public-work-videos.spec.ts --workers=1`
  - Result: passed (`5/5`).
- `docker run --rm --network woong-blog-aspcore-nextjs_default ... npx playwright test tests/public-work-toc.spec.ts tests/public-works-pagination.spec.ts tests/public-blog-pagination.spec.ts --workers=1`
  - Result: passed (`8/8`).
- `npm run typecheck`
  - Result: passed.
- `npm run lint`
  - Result: passed with 6 non-blocking warnings in existing test/backup/script files.
- `git diff --check`
  - Result: passed.

## Risks / Yellow Flags
- Docker Desktop port publishing for nginx failed in this WSL session, so Playwright used a no-port nginx container on the compose network and ran from a devcontainer image in that same network.
- `npm run lint` still reports existing non-blocking warnings unrelated to this production path.
- Backend restore/build still emits the existing low-severity `AWSSDK.Core` advisory warning.
- Desktop back after mobile reading now prioritizes desktop layout stability over raw mobile scroll restoration, by design.
- `npm run build` was attempted during the crypto addendum but was stopped after more than 4 minutes with no output on the `/mnt/d` workspace. The local browser server was switched to `next dev` on `0.0.0.0:3002` behind the `0.0.0.0:3000` proxy so the user can verify the latest source immediately.
- The full Playwright suite was not run in this addendum because it currently lists 589 tests and the active browser server is `next dev`; running the full suite there would primarily measure dev compilation, video recording, and test-data setup overhead. The latency-specific e2e spec now covers the reported public click paths.

## Recommendation
Ship this hotfix. The key unstable edges reported by the user are now covered by unit, backend, and browser tests.
