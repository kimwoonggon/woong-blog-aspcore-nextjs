# Frontend Architect 0410 Refactoring Todolist

## Goal
- Preserve frontend behavior while reducing hotspot complexity.
- Keep REST/CSRF mutation flows unchanged.
- Track phase progress, verification evidence, and `force-dynamic` audit outcomes in one place.

## Phase 0 - Test Hardening
- [x] Add `TiptapEditor` behavior tests for content sync.
- [x] Add `TiptapEditor` behavior tests for image upload orchestration.
- [x] Add `TiptapEditor` behavior tests for video insert request handling.
- [x] Add `AdminBlogBatchAiPanel` behavior tests for selection mode calculation.
- [x] Add `AdminBlogBatchAiPanel` behavior tests for job creation validation.
- [x] Add `AdminBlogBatchAiPanel` behavior tests for polling effect and cleanup.
- [x] Add `BlogEditor` test coverage for save normalization and inline navigation.
- [x] Re-run the baseline 27-test Vitest set and confirm it still passes.

## Phase 1 - Admin Hotspot Refactors
- [x] Keep `@/components/admin/WorkEditor` import path stable while introducing `work-editor/*` internals as needed.
- [x] Refactor `WorkEditor.saveWork` pass 1: create/update decision, payload build, post-save navigation, error handling.
- [x] Refactor `WorkEditor.saveWork` pass 2 and related flows: staged videos, media uploads, thumbnail handling, video mutations.
- [x] Refactor `AdminBlogBatchAiPanel` into feature-local helpers/hooks for selection, polling, and job detail handling.
- [x] Refactor `TiptapEditor` into extension factory, upload adapter, insert controller, and toolbar view modules.
- [x] Keep `BlogEditor` behavior-only unless a low-risk local extraction becomes obviously reusable.

## Phase 2 - App Router Alignment
- [x] Add `loading.tsx` and `error.tsx` for `(public)` segment.
- [x] Add `loading.tsx` and `error.tsx` for `(public)/blog/[slug]`.
- [x] Add `loading.tsx` and `error.tsx` for `admin` segment.
- [x] Add `loading.tsx` and `error.tsx` for `admin/dashboard`.
- [x] Keep page/layout server components thin by moving route-near data shaping into helpers.
- [x] Apply light decomposition to `Navbar`.
- [x] Apply light decomposition to `InteractiveRenderer`.

## Force-Dynamic Audit
- [x] `(public)/page.tsx`
- [x] `(public)/works/page.tsx`
- [x] `(public)/works/[slug]/page.tsx`
- [x] `(public)/blog/page.tsx`
- [x] `(public)/blog/[slug]/page.tsx`
- [x] `admin/dashboard/page.tsx`
- [x] `admin/works/page.tsx`
- [x] `admin/members/page.tsx`
- [x] Result summary recorded
- Summary: no `force-dynamic` removals were applied. Public listing/detail routes still rely on request-time params/content freshness, and admin routes still rely on auth/session or mutation freshness. Audit result is `verified, unchanged`.

## Verification Log
- [x] `npm run lint`
- [x] `npm run typecheck`
- [x] Targeted Vitest for changed areas
- [x] Baseline Vitest 27-test set
- [x] `npm run test:e2e:public`
- [x] `npm run test:e2e:admin`
- [x] `npm run test:e2e:works`
- [x] `npm run test:e2e:blog`
- Evidence:
- `npm test -- --run src/test/tiptap-editor.test.tsx src/test/admin-blog-batch-ai-panel.test.tsx src/test/blog-editor.test.tsx src/test/work-editor.test.tsx src/test/page-editor.test.tsx src/test/admin-dashboard-collections.test.tsx src/test/responsive-page-size.test.ts`
  -> `7 passed`, `36 passed`.
- `npm test -- --run src/test/tiptap-editor.test.tsx src/test/admin-blog-batch-ai-panel.test.tsx src/test/blog-editor.test.tsx src/test/work-editor.test.tsx`
  -> `4 passed`, `21 passed`.
- `npm run test:e2e:public` -> `21 passed`.
- `npm run test:e2e:admin` -> `17 passed`.
- `npm run test:e2e:works` -> `19 passed`.
- `npm run test:e2e:blog` -> `11 passed`.
- `npm test -- --run` default worker mode hit Vitest pool startup timeouts (`failed` tests were not reported; runner emitted worker-start errors).
- `npm test -- --run --maxWorkers=1` was terminated after a prolonged hang with no result summary, so full-suite verification remains environment-limited.

## Notes
- Unrelated worktree changes present at start: `skills-lock.json`, `.agents/skills/refactor-method-complexity-reduce/`.
- Default `force-dynamic` audit expectation: document reasons and keep unchanged unless a safe removal is proven.
