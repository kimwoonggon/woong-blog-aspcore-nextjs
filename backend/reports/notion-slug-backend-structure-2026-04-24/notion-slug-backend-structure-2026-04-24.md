# Notion / Slug / Backend Structure Audit

- Date: 2026-04-24
- Scope: notion typing/autosave behavior, public slug detail route failures, backend folder and namespace cleanup across `WoongBlog.Application`, `WoongBlog.Api`, `WoongBlog.Infrastructure`

## Changed
- Reworked notion editor state in [BlogNotionWorkspace.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/components/admin/BlogNotionWorkspace.tsx) so live editor HTML is held in refs during typing instead of forcing the full parent workspace to rerender on every keystroke.
- Switched notion autosave to a 10-second interval window instead of near-immediate save, while keeping throttled revalidation and explicit metadata save behavior.
- Added a short authenticated-session TTL cache in [auth.ts](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/lib/api/auth.ts) to reduce repeated autosave preflight round-trips.
- Fixed public detail route 500s on `/blog/[slug]` and `/works/[slug]` by isolating `useSearchParams()` usage behind Suspense-safe client boundaries and wrapping the shared public navbar in Suspense at [layout.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/app/(public)/layout.tsx).
- Flattened duplicate backend folder layers:
  - `backend/src/WoongBlog.Application/Modules/**/Application/*` -> `backend/src/WoongBlog.Application/Modules/**/*`
  - `backend/src/WoongBlog.Api/Modules/**/Api/*` -> `backend/src/WoongBlog.Api/Modules/**/*`
  - `backend/src/WoongBlog.Infrastructure/Modules/Identity/Infrastructure/*` -> `backend/src/WoongBlog.Infrastructure/Modules/Identity/Services/*`
- Flattened the redundant infrastructure root layer:
  - `backend/src/WoongBlog.Infrastructure/Infrastructure/*` -> `backend/src/WoongBlog.Infrastructure/*`
  - root namespaces now align as `WoongBlog.Infrastructure.*`
- Realigned namespaces and usings across production and test code so application types use `WoongBlog.Application.Modules.*`, API types use `WoongBlog.Api.Modules.*`, and infrastructure implementation namespaces use `WoongBlog.Infrastructure.Modules.*`.
- Realigned infrastructure root namespaces from `WoongBlog.Api.Infrastructure.*` to `WoongBlog.Infrastructure.*`.
- Updated backend composition and tests for the new namespace layout, including [Program.cs](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/backend/src/WoongBlog.Api/Program.cs), [ArchitectureBoundaryTests.cs](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/backend/tests/WoongBlog.Api.ArchitectureTests/ArchitectureBoundaryTests.cs), and [WorkVideoEndpointsTests.cs](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/backend/tests/WoongBlog.Api.IntegrationTests/WorkVideoEndpointsTests.cs).

## Not Changed
- Public content cards still use explicit `excerpt` text for preview surfaces; this pass did not switch cards to render body-first previews.
- The backend was not fully renamed beyond the duplicated folder/namespace layers. Some broader architectural naming choices remain intentionally out of scope for this pass.
- The backend was not fully redesigned beyond the duplicated folder/namespace layers. Some broader module naming choices remain intentionally out of scope for this pass.
- Existing lint warnings unrelated to this work remain.

## Goal Check
- Notion autosave now follows a wider interval instead of immediate-save: yes.
- Notion typing no longer routes every keystroke through full workspace state: yes.
- Public blog/work detail routes no longer 500 in production-style runtime: yes.
- Backend folder structure is less redundant across `Application`, `Api`, and `Infrastructure`: yes.
- Test references were updated with the namespace moves: yes.

## Validation
- `npm run typecheck`
  - Result: pass
- `npm run lint`
  - Result: pass with warnings only
- `npx vitest run src/test/blog-notion-workspace.test.tsx src/test/auth-csrf.test.ts`
  - Result: pass, 14 tests
- `dotnet test backend/WoongBlog.sln`
  - Result: pass, backend solution test projects all green
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/mobile-public-navigation.spec.ts tests/public-blog-pagination.spec.ts tests/public-works-pagination.spec.ts tests/public-seo-metadata.spec.ts tests/public-blog-toc-layout.spec.ts tests/ui-admin-notion-autosave-info.spec.ts --workers=1`
  - Result: pass, 18 passed and 1 skipped
- `curl http://127.0.0.1:3000/blog/<slug>` and `curl http://127.0.0.1:3000/works/<slug>`
  - Result: 200 after docker rebuild
- Local production-style runtime check with `node .next/standalone/server.js`
  - Result: detail routes 200 after Suspense and search-param boundary fixes

## Risks / Follow-up
- A 10-second autosave interval reduces churn but also increases the window in which unsaved edits exist in memory only.
- The authenticated-session TTL cache intentionally delays strict session revalidation in favor of fewer mutation round-trips.
- The backend namespace cleanup is a substantial 1-pass normalization, not a final naming end-state. Additional consolidation around infrastructure root namespaces and module naming can still be done later.
- Public detail routes remain static (`SSG`) and now rely on Suspense-safe client boundaries; future client hooks added to shared layout or detail subtrees should be checked against static-route constraints.

## Recommendation
- Keep the current notion/editor behavior and structure cleanup.
- If notion typing still feels heavy after real use, the next step should be moving AI dialog input and other secondary UI off the hot typing path entirely, then profiling the Tiptap `getHTML()` path.
