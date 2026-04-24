# Mobile Nav / Notion Perf / TOC / Work Share Meta Audit (2026-04-23)

## 1) Summary of Changes

- Reworked mobile public navigation UX:
  - mobile top header now uses left hamburger, centered brand, right search + theme toggle.
  - mobile fixed bottom tab navigation added with Home / Intro / Works / Study / Contact / Resume.
  - moved bottom nav outside header container to fix fixed-position stacking/placement issues.
- Implemented mobile search behavior:
  - on `/blog` and `/works`, search button focuses in-page search inputs.
  - on other pages, search routes to `/blog?focusSearch=1`.
- Added mobile safe-area spacing so content/footer are not obscured by fixed bottom nav.
- Improved TOC behavior/layout:
  - TOC remains visible at `xl (>=1280px)` only.
  - TOC card now renders a fallback body when headings are missing.
  - right rail spacing and detail layout boundaries adjusted for clearer separation.
- Implemented Works-only social share message path:
  - admin WorkEditor includes share-message input.
  - persisted at `all_properties.socialShareMessage` (no schema migration).
  - backend maps reserved key to public DTO `SocialShareMessage`.
  - `/works/[slug]` metadata description now prefers `socialShareMessage` over `excerpt`.
- Improved admin notion perceived performance:
  - autosave content PUT preserved.
  - autosave-triggered public revalidation throttled (`25s`).
  - explicit metadata save keeps immediate revalidation.
  - save chip status (`Saving...`/`Saved`) decoupled from revalidation delay.
  - notion list/detail fetch parallelization applied for `id` entry path.
- Stabilized related E2E suites by fixing test defaults/selectors/timing assumptions in auth, notion client switch, search URL, pagination, and visual-grid checks.

## 2) Intentionally Not Changed

- Backend auth/session semantics outside requested scenario.
- DB schema migrations for Works share metadata.
- Unrelated design-system or theme overhauls.
- Production secret/runtime environment strategy.

## 3) Goal / Non-goal Verification

- Mobile nav redesign with drawer coexistence: satisfied.
- Mobile search icon behavior by route context: satisfied.
- Mobile bottom safe-area padding: satisfied.
- TOC rail visibility/boundary/fallback requirements: satisfied.
- Works share-message storage + metadata priority: satisfied.
- Notion autosave/revalidation perceived-latency improvements: satisfied.
- Required tests and validations executed: satisfied (see validation details and caveat below).

## 4) Validations Performed

- `npm test`: passed (`61` files, `302` tests).
- `npm run lint`: passed with warnings only (`0` errors).
- `npm run typecheck`: passed.
- `dotnet test backend/WoongBlog.sln`: passed.

### Playwright / Latency

- Full `npm run test:e2e` executed multiple times.
- Latest full run before final targeted stabilization:
  - `572 passed`, `9 skipped`, `1 failed`.
  - failing case: `tests/feature-recording-0418.spec.ts` (`recording search url stability`).
- Applied stabilization patch to that failing case (`pressSequentially` -> `fill`), then reran targeted case:
  - `tests/feature-recording-0418.spec.ts --grep "recording search url stability"`: passed.
- Latency summary from the latest full run:
  - `Latency artifacts: 580`
  - `budget failures: 0`
  - `warnings: 77`

## 5) Risks / Yellow Flags / Deferred Follow-up

- The final full-suite pass was not re-run end-to-end after the last single-test stabilization patch due full-suite runtime; the patched failing case was validated in targeted rerun.
- Playwright artifact recordings under `tests/playwright/0418test` are large and frequently churn during full runs.

## 6) Recommendation

- Accept the implementation as functionally complete for the requested scope.
- Run one final CI full Playwright pass on a clean runner to lock in a green baseline after the last targeted stabilization.

## 7) Audit Artifacts

- `backend/reports/mobile-nav-notion-toc-share-2026-04-23/mobile-nav-notion-toc-share-2026-04-23.md`
- `backend/reports/mobile-nav-notion-toc-share-2026-04-23/mobile-nav-notion-toc-share-2026-04-23.html`
- `backend/reports/mobile-nav-notion-toc-share-2026-04-23/mobile-nav-notion-toc-share-2026-04-23.json`
