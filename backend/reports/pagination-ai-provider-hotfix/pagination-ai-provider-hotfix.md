# Pagination And AI Provider Hotfix Audit

Date: 2026-04-22

## Summary

This hotfix addresses four regressions reported after the public cache/revalidation work:

- Study and Works pagination was still coupled to server-side admin session checks.
- The BlogEditor still showed an unwanted top save/action bar.
- AI Fix provider selection only showed OpenAI in the real Docker runtime config.
- Next build surfaced an invalid segment configuration export for public list pages.

## Changed

- Removed server-side session gate usage from public Study and Works list pages.
- Moved public Study/Works admin affordances behind `PublicAdminClientGate`.
- Removed `headers()` usage from Study/Works QA flag checks and limited those flags to local QA conditions.
- Kept list pages revalidated with literal `export const revalidate = 60`.
- Removed the BlogEditor top sticky save/action bar while preserving bottom Create/Update and keyboard save.
- Changed AI Fix provider typing to the explicit `openai | codex` choice set.
- Changed backend AI runtime provider listing so Codex appears when `CodexCommand` is configured, while execution still validates Codex auth at run time.
- Ensured configured OpenAI API keys are exported to the Codex child process when Codex is selected.
- Added/updated frontend and backend tests for these behaviors.

## Intentionally Not Changed

- No mobile/feed/search redesign.
- No autosave workflow redesign.
- No backend auth or role-check loosening.
- No public card/content redesign.
- No global Playwright worker-isolation work.

## Verification Against Goals

- Public pagination no longer waits for server-side session fetching on Study/Works list render.
- Admin affordances still exist, but they load through the client-only public admin gate.
- The unwanted top save button is gone from BlogEditor.
- Bottom submit and keyboard save remain covered.
- Runtime AI config now returns `availableProviders:["openai","codex"]` in the Docker stack when Codex command support exists.
- The invalid Next segment config was fixed by using literal `revalidate = 60`.

## Validations

- `npx vitest run src/test/blog-editor.test.tsx src/test/public-admin-rendering.test.tsx src/test/admin-ai-fix-dialog.test.tsx --pool=threads` -> 3 files passed, 19 tests passed.
- `dotnet test backend/WoongBlog.sln --filter BlogAiFixServiceCodexRuntimeComponentTests` -> target component tests passed, 4 tests passed; existing NU1901 warnings remain.
- `npm run typecheck` -> passed.
- `npm run build` -> initially failed on invalid segment config; after fix, passed.
- Docker compose frontend/backend rebuild -> passed.
- Runtime config curl with admin cookie -> returned `availableProviders:["openai","codex"]`.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible npx playwright test tests/public-blog-pagination.spec.ts tests/public-works-pagination.spec.ts tests/e2e-visitor-pagination-journey.spec.ts tests/ui-admin-blog-excerpt.spec.ts tests/admin-blog-ai-dialog.spec.ts --workers=1` -> 13 passed.
- `npx eslint src tests playwright.config.ts next.config.ts eslint.config.mjs` -> 0 errors, 3 existing warnings.
- `git diff --check` -> passed.

## Risks And Follow-Up

- `npm run lint` may still fail in this working copy because Docker-owned `.tmp/postgres` is unreadable to ESLint's root scan. Scoped lint over source/tests/config passes.
- The AI runtime provider list now represents selectable UI choices, not guaranteed authenticated execution. Codex still fails clearly at execution time if neither Codex auth files nor OpenAI API key config are available.
- Public list pages remain dynamic in the Next route summary because they use query-driven pagination/search and runtime API fetches, but they no longer block public render on server session checks.

## Recommendation

Keep this as a focused hotfix and do not fold in unrelated pagination redesign or mobile/feed work. Follow up separately on the local `.tmp` ESLint ignore issue.
