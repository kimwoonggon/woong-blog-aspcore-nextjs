# Admin Blog Notion Autosave Request Path Audit (2026-04-24)

## Summary
This is an audit-only inspection of the current working tree request path for autosave from `/admin/blog/notion`, limited to HTTP/API handlers and autosave-triggered revalidation/cache invalidation.

## What Changed
- Added audit artifacts for this inspection.
- Appended a scoped audit-only section to `todolist-2026-04-24.md`.

## What Was Intentionally Not Changed
- No production code.
- No test code.
- No runtime instrumentation or benchmark setup.

## Goals And Non-Goals
- Goal: trace the autosave request path in the current working tree and identify likely latency-dominant steps.
- Goal: include concrete file references.
- Non-goal: propose or implement fixes.
- Non-goal: inspect unrelated admin/editor paths.

## Request Path Findings
1. Autosave always pays an extra browser-side session check before the blog update request.
   - `BlogNotionWorkspace` autosave calls `fetchWithCsrf` for the `PUT /api/admin/blogs/{id}` mutation at `src/components/admin/BlogNotionWorkspace.tsx:166`.
   - `fetchWithCsrf` performs `ensureBrowserAuthenticatedSession()` before any mutation and only then issues the actual fetch at `src/lib/api/auth.ts:102` and `src/lib/api/auth.ts:111`.
   - That session check is a separate `GET /api/auth/session` network round trip at `src/lib/api/auth.ts:28`.
   - Likely latency impact: this is one full extra request before the actual autosave `PUT`, so it is a primary candidate for perceived autosave delay.

2. The `PUT /api/admin/blogs/{id}` handler itself is fairly thin; the heavier work around it is auth/session validation plus a small number of DB operations.
   - The HTTP endpoint just forwards to MediatR at `backend/src/WoongBlog.Api/Modules/Content/Blogs/Api/UpdateBlog/UpdateBlogEndpoint.cs:12`.
   - The command handler does: load blog, extract text/excerpt, generate/check slug, update fields, and save at `backend/src/WoongBlog.Application/Modules/Content/Blogs/Application/UpdateBlog/UpdateBlogCommandHandler.cs:18` and `backend/src/WoongBlog.Application/Modules/Content/Blogs/Application/UpdateBlog/UpdateBlogCommandHandler.cs:42`.
   - Store operations are one `GetByIdForUpdateAsync`, at least one `SlugExistsAsync`, and one `SaveChangesAsync` at `backend/src/WoongBlog.Infrastructure/Modules/Content/Blogs/Persistence/BlogCommandStore.cs:17`, `backend/src/WoongBlog.Infrastructure/Modules/Content/Blogs/Persistence/BlogCommandStore.cs:10`, and `backend/src/WoongBlog.Infrastructure/Modules/Content/Blogs/Persistence/BlogCommandStore.cs:32`.
   - Protected requests also run cookie principal validation, which hits `AuthSessions` and `Profiles` and can write `LastSeenAt` every 30 seconds at `backend/src/WoongBlog.Infrastructure/Infrastructure/Auth/AppCookieAuthenticationEvents.cs:37`, `backend/src/WoongBlog.Infrastructure/Infrastructure/Auth/AuthRecorder.cs:169`, `backend/src/WoongBlog.Infrastructure/Infrastructure/Auth/AuthRecorder.cs:205`, `backend/src/WoongBlog.Infrastructure/Infrastructure/Auth/AuthRecorder.cs:221`, and `backend/src/WoongBlog.Infrastructure/Infrastructure/Auth/AuthOptions.cs:20`.
   - Likely latency impact: for the actual autosave `PUT`, the dominant backend cost is probably auth/session DB work plus the DB write, not the excerpt/search-text string processing.

3. Autosave-triggered public revalidation is decoupled from the "Saved" UI state and throttled to 25 seconds.
   - After a successful `PUT`, the autosave UI marks the save as complete before scheduling revalidation at `src/components/admin/BlogNotionWorkspace.tsx:195` and `src/components/admin/BlogNotionWorkspace.tsx:196`.
   - Revalidation is throttled by `AUTOSAVE_REVALIDATION_THROTTLE_MS = 25_000` at `src/components/admin/BlogNotionWorkspace.tsx:60`, with fire-and-forget scheduling at `src/components/admin/BlogNotionWorkspace.tsx:98` and `src/components/admin/BlogNotionWorkspace.tsx:113`.
   - Likely latency impact: autosave revalidation does not block the `Saved` badge for normal content autosave, so it is backend load but not the main perceived autosave latency for the editor status chip.

4. When autosave revalidation does run, the expensive-looking part is not `revalidatePath`/`revalidateTag`; it is the nested auth/session checks around the request.
   - Revalidation posts to the Next route `/revalidate-public` via `fetchWithCsrf` at `src/lib/public-revalidation-client.ts:13`, so it again pays the mutation preflight session check in `src/lib/api/auth.ts:111` and may pay a CSRF bootstrap at `src/lib/api/auth.ts:119`.
   - The Next route then performs its own admin check through `getPublicAdminAffordanceState()` at `src/app/revalidate-public/route.ts:33`, which calls `fetchServerSession()` and makes another internal `GET /api/auth/session` at `src/lib/auth/public-admin.ts:7` and `src/lib/api/server.ts:43`.
   - The actual invalidation loop is only over normalized paths/tags at `src/app/revalidate-public/route.ts:43` and `src/app/revalidate-public/route.ts:47`.
   - For a normal blog autosave slug, the path set is `/`, `/blog`, and `/blog/{slug}` from `src/lib/public-revalidation-paths.ts:18`, which expands to five unique tags through `src/lib/public-revalidation-paths.ts:71`.
   - Likely latency impact: the nested `/api/auth/session` fetches and auth validation are more likely to dominate than the small `revalidatePath`/`revalidateTag` loops themselves, because those calls mark cache entries stale rather than regenerate pages inline.

5. The first autosave or a retry can add another `/api/auth/csrf` request, and that also applies to `/revalidate-public` even though that route is outside the antiforgery middleware path filter.
   - CSRF bootstrap is fetched at `src/lib/api/auth.ts:82` and injected for all mutation methods at `src/lib/api/auth.ts:118`.
   - Server-side antiforgery validation only covers `/api/admin`, `/api/uploads`, and `/api/auth/logout` at `backend/src/WoongBlog.Infrastructure/Infrastructure/Security/AntiforgeryValidationMiddleware.cs:39`.
   - `/revalidate-public` is therefore paying the client wrapper's session/CSRF overhead without being in the middleware's protected path set.
   - Likely latency impact: this is intermittent, but it can dominate the first mutation after page load or any 400-triggered retry path.

## Likely Latency-Dominant Steps
1. Browser preflight `GET /api/auth/session` executed by `fetchWithCsrf` before each autosave `PUT`.
2. Protected `PUT /api/admin/blogs/{id}` auth principal validation against `AuthSessions` and `Profiles`, plus occasional `LastSeenAt` write.
3. The actual blog persistence path: load blog, slug existence check, and `SaveChangesAsync`.
4. When the 25-second throttle window allows it, the autosave-triggered `/revalidate-public` request, especially its nested auth/session fetches.
5. Cold-cache or retry-time `GET /api/auth/csrf` bootstrap.

## Validations Performed
- Static source trace with `rg` and `nl` over the current working tree.
- Verified autosave trigger, auth wrapper, blog API handler, auth validation, and revalidation route path.
- Verified no code edits were made outside audit artifacts.

## Risks / Yellow Flags / Deferred Follow-Up
- This audit is based on static inspection only; no live timings were captured in this pass.
- The inspected files are part of a dirty working tree and were reviewed as-is by explicit user instruction.
- Any real latency ranking can shift with DB/network conditions, but the repeated extra auth/session round trips are the clearest structural hotspot in the current path.

## Recommendation
Treat repeated auth/session checks around autosave and revalidation as the first suspects when profiling or optimizing this path. The business update handler is relatively small compared with the number of extra request/auth hops around it.
