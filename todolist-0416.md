# Homepage / Admin UX TODO

Date: 2026-04-16
Scope: public homepage, public inline editing, admin pages/settings, admin blog/work flows, theme defaults, Playwright coverage

## Summary

This TODO consolidates repeated reports into a single implementation list.
The order below is intentional:

1. Fix data persistence and broken flows first
2. Fix navigation state retention and editing return flows
3. Clean up homepage and public UX
4. Reduce noisy background behavior
5. Add or update Playwright coverage for each fixed regression

## P0 Functional Bugs

- [ ] Home page editor values do not read back in admin after save
  - Symptom: profile image, headline, and intro text update the public homepage, but the admin editor still shows old or default values.
  - Expected: admin page/settings rehydrates from the saved backend content, not stale defaults.
  - Likely files:
    - `src/components/admin/HomePageEditor.tsx`
    - `src/app/admin/pages/page.tsx`
    - `src/app/(public)/page.tsx`
    - `src/lib/content/page-content.ts`
    - `src/lib/api/admin-pages.ts`
  - Playwright:
    - extend `tests/admin-home-extreme-input.spec.ts`
    - extend `tests/admin-home-image-upload.spec.ts`

- [ ] Resume upload fails in admin
  - Symptom: resume upload errors instead of linking the uploaded PDF into site settings.
  - Expected: PDF upload succeeds, admin reflects the linked asset, public resume page updates.
  - Likely files:
    - `src/components/admin/ResumeEditor.tsx`
    - `src/lib/api/site-settings.ts`
    - `backend/src/WoongBlog.Api/Modules/Site/Api/UpdateSiteSettings/*`
    - `backend/src/WoongBlog.Api/Modules/Media/*`
  - Playwright:
    - review `tests/admin-resume-upload.spec.ts`
    - review `tests/admin-resume-validation.spec.ts`

- [ ] Admin works list does not reflect resolved thumbnail used by public works
  - Symptom: public work cards show the right thumbnail, admin works list does not.
  - Expected: admin list and public cards derive from the same thumbnail resolution logic or same resolved field.
  - Likely files:
    - `src/components/admin/AdminWorksTableClient.tsx`
    - `src/lib/api/works.ts`
    - `src/lib/content/work-thumbnail-resolution.ts`
    - backend work DTO/query files
  - Playwright:
    - extend `tests/admin-work-auto-thumbnail.spec.ts`
    - extend `tests/admin-work-image-upload.spec.ts`

## P1 Return Flow / State Retention

- [ ] Admin blog edit should return to the current filtered page instead of page 1
  - Symptom: editing from another admin blog page returns to page 1.
  - Expected: current page, page size, and active search/filter survive edit/delete/save.
  - Likely files:
    - `src/components/admin/AdminBlogTableClient.tsx`
    - `src/app/admin/blog/[id]/page.tsx`
    - `src/components/admin/BlogEditor.tsx`
  - Playwright:
    - strengthen `tests/admin-search-pagination.spec.ts`

- [ ] Admin work edit should preserve current list page/filter too
  - Symptom: likely same bug as blog flow, needs parity check.
  - Expected: work editor returns to the originating admin works page and filter state.
  - Likely files:
    - `src/components/admin/AdminWorksTableClient.tsx`
    - `src/app/admin/works/[id]/page.tsx`
    - `src/components/admin/WorkEditor.tsx`
  - Playwright:
    - add admin works pagination return test

- [ ] Blog Notion library should preserve current position instead of jumping to the top
  - Symptom: after editing, opening Library resets the list position to the first item.
  - Expected: selected item, search term, and scroll position are preserved while the user stays in the workspace.
  - Likely files:
    - `src/components/admin/BlogNotionWorkspace.tsx`
    - `src/app/admin/blog/notion/page.tsx`
  - Playwright:
    - extend `tests/admin-blog-edit.spec.ts`

- [ ] Public work inline edit should close cleanly after clicking Edit / save flow
  - Symptom: inline work edit affordance does not close as expected.
  - Expected: open/close state is explicit and the editor closes after the intended action.
  - Likely files:
    - `src/app/(public)/works/[slug]/page.tsx`
    - `src/components/admin/InlineAdminEditorShell.tsx`
    - `src/components/admin/WorkEditor.tsx`
  - Playwright:
    - extend `tests/work-inline-redirects.spec.ts`

## P1 Public Editing UX

- [ ] Public blog and work editing actions should move higher in the page
  - Symptom: edit affordance is too low and easy to miss.
  - Expected: admin-only edit/manage controls appear near the title/header area.
  - Likely files:
    - `src/app/(public)/blog/[slug]/page.tsx`
    - `src/app/(public)/works/[slug]/page.tsx`
    - `src/components/admin/PublicAdminLink.tsx`

- [ ] Public blog and work views should expose delete action when admin is present
  - Symptom: edit exists, delete does not.
  - Expected: destructive affordance is available for authenticated admin in the public inline management area, with confirmation.
  - Likely files:
    - `src/app/(public)/blog/[slug]/page.tsx`
    - `src/app/(public)/works/[slug]/page.tsx`
    - shared inline shell components
  - Playwright:
    - add admin-visible delete affordance tests

## P2 Homepage / Navigation UX

- [ ] Remove the `Portfolio` eyebrow above `woonggonkim` in the top nav
  - Likely file:
    - `src/components/layout/Navbar.tsx`

- [ ] Remove the right-side nav tagline `Works, writing, and experiments in one balanced shell.`
  - Likely file:
    - `src/components/layout/Navbar.tsx`

- [ ] Change nav label `Blog` to `Study`
  - Scope:
    - top nav
    - public blog listing heading and supporting copy where appropriate
    - homepage CTA and route labels that say Blog
  - Likely files:
    - `src/components/layout/Navbar.tsx`
    - `src/app/(public)/blog/page.tsx`
    - `src/app/(public)/page.tsx`
    - footer / tests that assert `Blog`

- [ ] Keep default theme as light mode
  - Expected:
    - initial load is always light
    - do not follow system theme by default
    - hide the `system` option for now
  - Likely files:
    - `src/components/ui/ThemeToggle.tsx`
    - theme provider component
    - app layout where theme provider is configured
  - Playwright:
    - update dark/light tests to reflect new default behavior

- [ ] Swap visual left/right meaning of previous/next links under blog and work detail pages
  - Current report means current labels/order feel reversed.
  - Expected: left card = previous, right card = next, with naming aligned to time/order intent.
  - Likely files:
    - `src/app/(public)/blog/[slug]/page.tsx`
    - `src/app/(public)/works/[slug]/page.tsx`
  - Playwright:
    - update `tests/e2e-visitor-content-exploration.spec.ts`
    - update `tests/ui-improvement-related-page-retention.spec.ts`

- [ ] `More posts` should keep the current article in sequence and highlight it instead of removing it
  - Expected: current post remains in the ordered set with a visible current-state border or badge.
  - Likely files:
    - blog detail page related content section
    - related content component
  - Playwright:
    - add visible current-state assertion

- [ ] Move `On this page` a bit further right
  - Expected: better spacing/alignment on blog detail layout.
  - Likely files:
    - `src/components/content/TableOfContents.tsx`
    - blog detail layout styles

## P2 Content / Copy Refresh

- [ ] Homepage CTA and section copy should align with the new `Study` terminology where needed
- [ ] Remove any remaining `Portfolio`/old copy that feels redundant in the main header area

## P2 Admin Dashboard / Pages UX

- [ ] Verify admin dashboard page/settings reload behavior for homepage fields after save
  - This is the user-facing symptom behind the stale home editor issue, but dashboard readback should be explicitly verified.

## P3 Background Behavior / Noise Reduction

- [ ] Remove continuous AI batch job queue polling unless the user explicitly opens or requests it
  - Symptom: background queue checking feels unnecessary.
  - Expected: request-driven refresh, not constant polling.
  - Likely files:
    - `src/components/admin/AdminBlogBatchAiPanel.tsx`
    - `src/components/admin/admin-blog-batch-ai-panel/useBatchJobPolling.ts`
  - Playwright:
    - add a network-behavior assertion or reduce polling expectations in existing tests

## Playwright Coverage To Add / Update

- [ ] Home editor readback after save stays correct in admin and public
- [ ] Resume upload happy path works again
- [ ] Admin blog pagination/search return state survives edit/delete
- [ ] Admin work pagination return state survives edit/save
- [ ] Blog Notion library preserves selected item and scroll position
- [ ] Admin works list reflects resolved thumbnail source
- [ ] Public inline work edit closes cleanly
- [ ] Public inline admin controls appear near the title area and expose delete
- [ ] Nav copy changes from `Blog` to `Study`
- [ ] Default theme is light and `system` option is hidden
- [ ] Blog/work previous-next order matches the intended left/right semantics
- [ ] `More posts` keeps current article visible with highlighted current state
- [ ] `On this page` layout offset remains stable
- [ ] AI batch jobs do not poll continuously when the panel is idle

## Suggested Implementation Order

1. Home editor readback bug
2. Resume upload bug
3. Admin blog/work return-state fixes
4. Notion library state retention
5. Work thumbnail parity in admin
6. Public inline edit/delete affordance cleanup
7. Navbar/home copy cleanup + `Blog` -> `Study`
8. Theme default/light-only cleanup
9. Previous/next order + related/current highlighting + TOC spacing
10. AI batch polling reduction
