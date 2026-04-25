# Backend Test Coverage Audit - 2026-04-24

## Scope And Method

Scope was backend production code and backend test projects only. The original audit did not modify production or test code; subsequent dated implementation updates in this report document test-only changes.

This is a strict, pessimistic coverage audit. A feature is not classified as `Covered` unless an existing test file can be named and the asserted behavior is described. Classifications are based on source/test inspection, not line coverage tooling.

Production inventory scanned:

- `backend/src/WoongBlog.Api`
- `backend/src/WoongBlog.Application`
- `backend/src/WoongBlog.Infrastructure`
- `backend/src/WoongBlog.Domain`

Test inventory scanned:

- `backend/tests/WoongBlog.Api.UnitTests`
- `backend/tests/WoongBlog.Api.ComponentTests`
- `backend/tests/WoongBlog.Api.IntegrationTests`
- `backend/tests/WoongBlog.Api.ArchitectureTests`
- `backend/tests/WoongBlog.Api.ContractTests`

## Existing Backend Test Suite Summary

| Test project | Intended taxonomy | Current files / tests observed | What is actually covered | Strict notes |
|---|---|---:|---|---|
| `WoongBlog.Api.UnitTests` | Pure Application/helper/validator tests only | 4 files, 12 facts/theories | Command/query validators, `AdminContentText.GenerateExcerpt`, `WorkVideoHlsJobPlan` | Taxonomy is preserved by `ArchitectureBoundaryTests.UnitTestProject_DoesNotReference_Infrastructure_AspNetCore_Or_EfInMemory`. Unit coverage is narrow. |
| `WoongBlog.Api.ComponentTests` | Application + Infrastructure + EF InMemory/fakes/filesystem/HttpClient style tests, no full HTTP host | 8 files, 76 facts | Auth recorder/session persistence including malformed/expired/missing-profile sessions, public query handlers/stores, WorkVideo storage/cleanup/ordering behavior, AI batch scheduler/store/runtime/options behavior, Codex fake-process runtime behavior, DbContext model metadata contracts | Public read/query, WorkVideo storage/service, AI batch/runtime/Codex, and EF model metadata coverage are stronger after the 2026-04-25 updates. Most non-WorkVideo admin command handlers and stores are not component-tested. |
| `WoongBlog.Api.IntegrationTests` | Full ASP.NET test host and endpoint behavior | 17 test files, 159 facts/theories; 194 expanded test cases | Auth/session/login/logout/CSRF/admin authorization slices, admin content mutation endpoints, public endpoint read/query behavior, media upload/delete, work videos, AI endpoints, startup/options/DI/middleware behavior, persistence bootstrapping, Postgres relational constraints | Auth, state-changing admin command coverage, public read endpoint coverage, WorkVideo endpoint coverage, representative AI endpoint coverage, persistence contracts, and startup composition coverage are stronger after the 2026-04-25 updates, but media/AI mutation matrices and several WorkVideo edge cases remain representative rather than exhaustive. |
| `WoongBlog.Api.ArchitectureTests` | Project/layer/dependency boundary tests | 1 file, 31 facts | Layer references, HTTP-agnostic Application, removed legacy controllers/services, module boundary checks, unit-test project dependency guard, Program composition order | Strong architecture regression net, but not behavior coverage. |
| `WoongBlog.Api.ContractTests` | Provider/contract verification tests | 1 file, 1 fact | Pact provider verification from pact files if `PACT_PROVIDER_BASE_URL` and pact files exist | Test self-skips when env or pact files are missing, so normal `dotnet test` can pass without contract verification. |

Observed source totals after the P1 auth, admin mutation, public read/query, WorkVideo, AI batch/runtime, and persistence/startup test updates: 31 backend test files containing facts/theories and 279 facts/theories. `dotnet test` executed 316 test cases because xUnit theories expand inline data. Production C# inventory remains unchanged for these updates.

## Coverage Classification Legend

| Classification | Meaning in this audit |
|---|---|
| `Covered` | Existing tests name the behavior and assert happy path plus representative failure/auth/persistence effects. This audit found no broad feature that qualifies as fully covered. |
| `Partially covered` | Some meaningful behavior is asserted, but important happy path, edge, auth, persistence, or side-effect cases are missing. |
| `Happy-path only` | Existing tests exercise success behavior only or mostly success behavior. |
| `Endpoint-only` | Existing tests use the ASP.NET host but do not isolate Application/Infrastructure behavior. |
| `Component-only` | Existing tests cover handler/store/service behavior without endpoint host coverage. |
| `Unit-only` | Existing tests cover pure helper/validator behavior only. |
| `Untested` | No named test evidence found for the feature. |
| `Unknown, needs manual review` | Source area exists but behavior intent or external contract cannot be verified from existing tests. |

## 1. Auth/session/CSRF/login/logout

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| Session endpoint | `backend/src/WoongBlog.Api/Modules/Identity/GetSession/GetSessionEndpoint.cs`; `backend/src/WoongBlog.Infrastructure/Auth/AuthRecorder.cs`; auth events/options | `AuthEndpointsTests.cs` asserts anonymous session returns `authenticated:false`, authenticated test header returns `authenticated:true` and `role:"admin"`; `AuthFlowIntegrationTests.cs` asserts full admin payload and authenticated non-admin `user` role payload; `AuthSecurityTests.cs` asserts security headers on session response | Integration | Partially covered | Unauthenticated response still does not assert null profile fields | Invalid/stale cookie path only indirectly covered at component level, not endpoint level | Non-admin authenticated role response is now tested through the test auth handler | Cookie/session validation side effects are not asserted through endpoint host | Integration | P1 |
| CSRF token endpoint and antiforgery middleware | `GetCsrfEndpoint.cs`; `AntiforgeryValidationMiddleware.cs`; `SecurityServiceCollectionExtensions.cs` | `AuthEndpointsTests.cs` asserts `/api/auth/csrf` returns request token/header; `AuthSecurityTests.cs` asserts `PUT /api/admin/site-settings` fails without CSRF and succeeds with CSRF; `AuthFlowIntegrationTests.cs` asserts invalid CSRF rejects without persisting, valid CSRF accepts, logout without CSRF rejects, and valid-CSRF anonymous/non-admin mutations reach 401/403 auth failures | Integration | Partially covered | Broader cookie/header interaction across multipart and all mutation families | Stale token, missing cookie, duplicate header, and multipart mutation paths remain untested | Representative admin mutation and logout CSRF behavior is now sampled, but not every admin POST/PUT/DELETE endpoint | Site-settings invalid-token rejection now asserts no DB side effect; other rejected mutations still lack side-effect assertions | Integration | P1 |
| Login and OIDC callback flow | `LoginEndpoint.cs`; `TestLoginEndpoint.cs`; `AppOpenIdConnectEvents.cs`; `PublicOriginUrlResolver.cs`; `IdentityInteractionService.cs`; `AuthRecorder.cs` | `AuthEndpointsTests.cs` asserts test-login is not application-rate-limited; `AuthRedirectUriResolverComponentTests.cs` asserts callback URI uses configured public origin or request origin; `AuthFlowIntegrationTests.cs` asserts `/api/auth/login` challenges a fake OIDC provider and external return URLs are not exposed | Integration, Component | Partially covered | OIDC callback success through the host and profile/session/audit creation through the callback endpoint | Disabled auth, provider failure, missing claims, and callback failure remain untested | Non-admin user login and admin email promotion through endpoint host remain untested | Profile/session/audit creation is component-tested only in `AuthRecorderComponentTests.cs`, not through callback endpoint | Integration + Component | P1 |
| Logout flow | `LogoutEndpoint.cs`; `AuthRecorder.cs`; cookie auth events | `AuthEndpointsTests.cs` asserts `GET /api/auth/logout` returns 405; `AuthFlowIntegrationTests.cs` asserts `POST /api/auth/logout` rejects missing CSRF, succeeds with valid CSRF, returns redirect payload, and clears the auth cookie; `AuthRecorderComponentTests.cs` asserts recorder revokes session and adds audit log | Integration, Component | Partially covered | Basic POST success, JSON redirect contract, cookie clear, and missing-CSRF failure are now tested | Anonymous logout, invalid return URL, and repeated logout remain untested | Logout POST is not an auth-required endpoint by current production mapping | Endpoint-level revocation/audit side effects are not asserted with a real cookie-authenticated principal | Integration | P1 |
| Auth session recorder | `AuthRecorder.cs`; `AuthOptions.cs`; `AuthClaimTypes.cs`; `Profile`, `AuthSession`, `AuthAuditLog` | `AuthRecorderComponentTests.cs` asserts successful login creates profile/session/audit, admin email grants admin, normal email keeps user, logout revokes, missing/malformed claims, revoked sessions, sliding expiration, absolute expiration, missing profile, role drift, valid refresh, and recently seen no-refresh behavior | Component | Partially covered | Existing profile login update and multiple concurrent sessions | Missing email/name/provider subject and audit failure reasons remain untested | Admin override changes over time and role downgrade semantics remain untested | IP address capture, user agent truncation, session key uniqueness collision handling | Component | P1 |
| Security headers | `SecurityHeadersMiddleware.cs`; `SecurityOptions.cs`; `SecurityOptionsValidator.cs` | `AuthSecurityTests.cs` asserts `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`, and CSP on `/api/auth/session` | Integration | Happy-path only | Headers on representative public/admin/media responses | Disabled/configured security option variants, HSTS/HTTPS behavior | Not applicable except ensuring headers on unauthorized responses | No side-effect concerns | Integration | P2 |

## 2. Admin content/page/blog/work management

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| Admin pages query/update | `Modules/Content/Pages/*`; `GetAdminPagesQueryHandler.cs`; `UpdatePageCommandHandler.cs`; `PageQueryStore.cs`; `PageCommandStore.cs` | `AdminContentEndpointsTests.cs` asserts slug-filtered admin pages, structured home content, bad request when id missing, not found when page missing; `AdminEndpointsTests.cs` asserts update page persists and public page reflects new HTML; `AdminMutationEndpointsTests.cs` asserts valid update response shape/persistence, invalid update no-write behavior, and anonymous/non-admin mutation rejection with valid CSRF | Integration | Partially covered | Get all admin pages; update each page type including home/introduction/contact payload shapes | Malformed `contentJson`, invalid slug filters, overly long title through endpoint, concurrency/last update | Admin page update now has anonymous 401 and non-admin 403 mutation coverage with valid CSRF | Store-level side effects are still not isolated in component tests; selected unrelated page preservation is now asserted through the endpoint | Integration + Component | P1 |
| Admin blog create/read/update/delete | `Modules/Content/Blogs/*`; `CreateBlogCommandHandler.cs`; `UpdateBlogCommandHandler.cs`; `DeleteBlogCommandHandler.cs`; `BlogCommandStore.cs`; `BlogQueryStore.cs` | `AdminContentEndpointsTests.cs` asserts missing title 400, create slug/excerpt, manual/blank/markdown/wrapped-markdown excerpts, duplicate slug, update missing 404; `AdminEndpointsTests.cs` asserts too-long title 400, create/delete removes blog, admin detail extracts HTML; `AdminMutationEndpointsTests.cs` asserts create/update/delete success and failure, response shape, DB persistence, publish/unpublish via `Published`, missing update/delete no side effects, invalid create no-write behavior, unrelated record preservation, and anonymous/non-admin mutation rejection with valid CSRF | Integration | Partially covered | Get admin blog list remains outside this destructive-command slice; cover asset behavior remains untested | Invalid JSON, duplicate slug under updates, tag boundaries beyond one case, markdown edge cases, empty content variants | Blog mutations now have representative anonymous 401 and non-admin 403 coverage with valid CSRF | DB `PublishedAt` on create, create/update/delete persistence, and unrelated-record preservation are now asserted; search field updates and asset links remain incomplete | Integration + Component | P1 |
| Admin work create/read/update/delete | `Modules/Content/Works/*`; `CreateWorkCommandHandler.cs`; `UpdateWorkCommandHandler.cs`; `DeleteWorkCommandHandler.cs`; `WorkCommandStore.cs`; `WorkQueryStore.cs` | `AdminEndpointsTests.cs` asserts invalid payload 400, create then get persists excerpt/category; `AdminContentEndpointsTests.cs` asserts missing category 400, malformed metadata falls back to `{}`, duplicate slug, update missing 404, thumbnail/icon asset persistence; `WorkVideoEndpointsTests.cs` asserts delete work removes videos/sessions and queues cleanup; `AdminMutationEndpointsTests.cs` asserts create/update/delete success and failure, response shape, DB persistence, publish/unpublish via `Published`, missing update/delete no side effects, invalid create no-write behavior, unrelated record preservation, and anonymous/non-admin mutation rejection with valid CSRF | Integration | Partially covered | Get admin work list remains outside this destructive-command slice; thumbnail/icon asset update semantics remain lightly covered elsewhere | Malformed content JSON, invalid asset ids, duplicate slug on update, stale video version interaction with work update | Work mutations now have representative anonymous 401 and non-admin 403 coverage with valid CSRF | DB `PublishedAt` on create, create/update/delete persistence, and unrelated-record preservation are now asserted; search field updates and thumbnail/icon delete semantics remain incomplete | Integration + Component | P1 |
| Request validators for admin content | API request validators and Application command validators for pages/blogs/works/site settings/work videos | `RequestValidatorTests.cs` asserts selected Application validators reject empty/too-long fields; endpoint tests assert selected 400 responses | Unit, Integration | Unit-only | Valid command cases for all validators | Missing validators for update blog/work request details, work-video request validators, API request validators directly | Not applicable | Not applicable | Unit | P2 |
| Admin content excerpt/text helpers | `AdminContentText.cs`; `AdminContentJson.cs`; `ContentSearchText.cs`; command handlers using generated excerpts | `AdminContentTextTests.cs` asserts mermaid-block removal and plain fence preservation; `AdminContentEndpointsTests.cs` asserts several blog excerpt paths | Unit, Integration | Partially covered | Excerpt generation across HTML, markdown, wrapped markdown, empty values as unit tests | XSS/script stripping expectations, images/alt text edge cases, malformed JSON helper behavior | Not applicable | Search-text side effects only indirectly covered in persistence tests | Unit + Component | P2 |

## 3. Admin members

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| Admin member list | `GetAdminMembersEndpoint.cs`; `GetAdminMembersQueryHandler.cs`; `AdminMemberQueryStore.cs`; `Profile`; `AuthSession` | `AdminMembersEndpointsTests.cs` asserts admin receives `admin@example.com`, `activeSessionCount`, and privacy-sensitive fields are absent; same file asserts anonymous gets 401 | Integration | Partially covered | Multiple profiles, active vs revoked/expired session counts, ordering | Empty member list, long names/emails, mixed roles | Non-admin authenticated user should be forbidden is not tested | Store-level count accuracy not component-tested | Integration + Component | P2 |

## 4. Admin dashboard

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| Dashboard summary | `GetDashboardSummaryEndpoint.cs`; `GetDashboardSummaryQueryHandler.cs`; `AdminDashboardQueryStore.cs`; `PageView`; `Work`; `Blog` | `AdminDashboardEndpointsTests.cs` asserts admin summary returns counts with works/blogs >= 1 and views >= 0; asserts anonymous gets 401 | Integration | Endpoint-only | Exact count computation with seeded and inserted data | Zero-data state, page view date/entity filters if intended | Non-admin authenticated user should be forbidden is not tested | `PageView` aggregation is not directly asserted; store has no component test | Component + Integration | P2 |

## 5. Site settings

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| Public site settings | `GetSiteSettingsEndpoint.cs`; `GetSiteSettingsQueryHandler.cs`; `SiteSettingsQueryStore.cs`; `SiteSetting` | `PublicEndpointsTests.cs` asserts seeded owner appears and typed public DTO serialization shape is anonymous-accessible, including `gitHubUrl`/`linkedInUrl` contract casing and no `resumeAssetId`; `PublicQueryHandlerComponentTests.cs` asserts handler returns null when settings are missing and maps all public social fields | Integration, Component | Partially covered | Endpoint-level coverage for non-empty Facebook/Instagram/Twitter fields | Missing settings returns 404 at endpoint level | Public endpoint anonymous access is covered by unauthenticated success tests | Resume asset is intentionally not part of `SiteSettingsDto`; resume asset join is covered through home/resume paths | Component + Integration | P2 |
| Admin get/update site settings | `GetAdminSiteSettingsEndpoint.cs`; `UpdateSiteSettingsEndpoint.cs`; `UpdateSiteSettingsCommandHandler.cs`; stores | `AdminEndpointsTests.cs` and `AdminContentEndpointsTests.cs` assert update owner/tagline persistence; `AdminContentEndpointsTests.cs` asserts resume asset explicit null clears, omitted id preserves, empty GUID 400; `AuthSecurityTests.cs` asserts CSRF on update; `AuthFlowIntegrationTests.cs` asserts invalid/valid CSRF for update; `AdminMutationEndpointsTests.cs` asserts partial update preserves omitted fields, invalid overlong owner no-write behavior, response shape, and anonymous/non-admin mutation rejection with valid CSRF | Integration | Partially covered | Admin get settings endpoint, all social fields update in one full DTO case, resume asset set to valid asset id | Missing singleton/settings not found and invalid resume asset id that is non-empty but absent remain untested | Admin site-settings update now has anonymous 401 and non-admin 403 mutation coverage with valid CSRF | Selected field preservation and invalid-request no-write behavior are now asserted; `UpdatedAt` and linked asset integrity remain incomplete | Integration + Component | P1 |
| Public resume | `GetResumeEndpoint.cs`; `GetResumeQueryHandler.cs`; `SiteSettingsQueryStore.cs`; `Asset` | `PublicEndpointsTests.cs` asserts seeded resume URL; `PublicQueryHandlerComponentTests.cs` asserts null when resume asset missing | Integration, Component | Partially covered | Endpoint 404 when resume missing | Invalid/missing asset, non-PDF asset if constrained | Public endpoint should remain anonymous | Asset join is component-tested but not failure through endpoint | Integration | P2 |

## 6. Public home/page/blog/work queries

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| Public home | `GetHomeEndpoint.cs`; `GetHomeQueryHandler.cs`; `HomeQueryStore.cs`; `SiteSetting`; `PageEntity`; `Work`; `Blog`; `Asset` | `PublicEndpointsTests.cs` asserts seeded home includes featured seed data and anonymous typed DTO serialization shape; `PublicQueryHandlerComponentTests.cs` asserts missing home and missing settings return null, maps home content/site fields/resume URL, filters drafts, maps work/blog assets, orders featured works/recent posts by published date, and limits recent posts to six | Integration, Component | Partially covered | Endpoint-level missing settings/home 404 paths | Missing resume asset behavior at endpoint level; malformed home content JSON remains untested | Anonymous access is covered by unauthenticated success tests | Store behavior covered with EF InMemory; no endpoint side-effect concerns | Component + Integration | P2 |
| Public pages by slug | `GetPageBySlugEndpoint.cs`; `GetPageBySlugQueryHandler.cs`; `PageQueryStore.cs`; validator | `PublicEndpointsTests.cs` directly asserts `/api/public/pages/introduction` returns anonymous typed page DTO shape and missing page returns 404; `AdminEndpointsTests.cs` indirectly asserts updated page is visible via `/api/public/pages/introduction`; `PublicQueryHandlerComponentTests.cs` asserts page content by slug and null for missing slug; `GetPageBySlugQueryValidatorTests.cs` asserts empty slug invalid and valid slug accepted | Integration, Component, Unit | Partially covered | Direct seeded success for all public page slugs | Empty/invalid route behavior and malformed content JSON | Anonymous access is covered by unauthenticated success tests | No persistence side effects expected | Integration | P2 |
| Public works list/detail/search | `GetWorksEndpoint.cs`; `GetWorkBySlugEndpoint.cs`; `GetWorksQueryHandler.cs`; `GetWorkBySlugQueryHandler.cs`; `WorkQueryStore.cs`; `WorkThumbnailUrlResolver.cs` | `PublicEndpointsTests.cs` asserts seeded detail, typed detail serialization with thumbnail/icon/videos, missing detail 404, draft detail 404, paged list shape, title/content/unified search, draft exclusion, published-date ordering, asset mapping, and stable empty list response; `PublicQueryHandlerComponentTests.cs` asserts stable empty page, draft filtering, asset URLs, page clamping, published-date ordering, title/normalized/content/unified search, social share message mapping, detail assets/videos, missing/draft detail null, and HLS timeline preview URLs | Integration, Component | Partially covered | Invalid `pageSize=0` normalization at endpoint level | Unknown `searchMode` fallback and malformed content/all-properties JSON endpoint path | Anonymous access is covered by unauthenticated success tests | Search fields covered by persistence tests, but query store uses EF InMemory and endpoint uses in-memory host only | Component + Integration | P1 |
| Public blogs list/detail/search | `GetBlogsEndpoint.cs`; `GetBlogBySlugEndpoint.cs`; `GetBlogsQueryHandler.cs`; `GetBlogBySlugQueryHandler.cs`; `BlogQueryStore.cs` | `PublicEndpointsTests.cs` asserts paged list shape, title/content/unified search, missing blog 404, typed seeded detail serialization with cover asset, draft detail 404, draft list exclusion, published-date ordering, cover mapping, and stable empty list response; admin create tests indirectly assert created public blog detail content/excerpt; `PublicQueryHandlerComponentTests.cs` asserts stable empty page, draft filtering, cover URL mapping, page clamping, published-date ordering, paging, normalized title search, unified search, typed detail with cover, and missing/draft detail null | Integration, Component | Partially covered | Invalid `pageSize=0` normalization at endpoint level | Unknown `searchMode` fallback and malformed content JSON endpoint path | Anonymous access is covered by unauthenticated success tests | Search fields covered by persistence tests, but not direct production PostgreSQL query behavior | Component + Integration | P1 |

## 7. Assets/storage/files

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| Media asset upload/delete | `UploadAssetEndpoint.cs`; `DeleteAssetEndpoint.cs`; `UploadMediaAssetCommandHandler.cs`; `DeleteMediaAssetCommandHandler.cs`; `MediaAssetStorage.cs`; `MediaAssetUploadPolicy.cs`; `MediaAssetCommandStore.cs`; `Asset` | `UploadsControllerTests.cs` asserts missing file 400, PDF upload creates DB row/file then delete removes both, unsupported BMP 400, missing delete 404; `AdminContentEndpointsTests.cs` uses image upload for work thumbnail/icon | Integration | Partially covered | Supported image uploads for all intended buckets and inline/blog buckets | Path traversal filenames, size limits, MIME/content mismatch, invalid bucket, duplicate filenames, storage IO failures | Anonymous/non-admin and CSRF behavior for upload/delete are not tested | Delete failure rollback, orphan-file cleanup, `CreatedBy`, MIME/kind/size metadata not fully asserted | Component + Integration | P1 |
| Static media serving | `UseMediaStaticFiles` in auth/infrastructure; `MediaAssetStorage.cs`; `LocalVideoStorageService.cs`; Program middleware | `WorkVideoEndpointsTests.cs` asserts HLS manifest can be fetched by playback URL and confirmed local uploads leave a file under the configured media root; `WorkVideoComponentTests.cs` asserts local video storage save/read/delete side effects | Integration, Component | Partially covered | Fetch uploaded PDF/image public URL | Missing file 404, content type, cache headers/range requests, private bucket protection if intended | Anonymous/public serving expectations are only indirectly asserted through public video playback URLs | No DB side effects expected | Integration | P2 |
| Cloudflare/R2 and video object storage | `CloudflareR2Options.cs`; `R2VideoStorageService.cs`; `LocalVideoStorageService.cs`; `WorkVideoStorageSelector.cs`; `WorkVideoPlaybackUrlBuilder.cs` | `WorkVideoComponentTests.cs` directly covers local storage save/read/delete and HLS-manifest directory deletion; no direct tests for R2 | Component for local only | Partially covered | R2 upload/download/delete/playback URL paths with fake `HttpClient` | R2 credential failure, network errors, object missing, signed/public URL behavior | Admin-only operations using R2 are only covered for local/default storage | Cleanup side effects against R2 object storage remain unasserted | Component + Contract | P1 |

## 8. Work videos

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| YouTube work videos | `WorkVideoEndpoints.cs`; `AddYouTubeWorkVideoCommandHandler.cs`; `WorkVideoCommandStore.cs`; `WorkVideoQueryStore.cs`; `WorkVideoPolicy.cs` | `WorkVideoEndpointsTests.cs` asserts YouTube video persists, increments `videos_version`, appears in admin and public projections; public WorkVideo query test asserts draft work videos stay hidden with the draft work | Integration | Partially covered | Multiple YouTube URL/id normalization variants beyond the current direct ID and short URL samples | Invalid YouTube IDs, missing work for YouTube add, stale version for add, duplicate videos, max count if intended | Anonymous/non-admin is covered for upload-url only, not every WorkVideo mutation; invalid/missing CSRF remains unsampled for WorkVideo | Sort order and `VideosVersion` are asserted on success and reorder/delete paths, but max-count and duplicate semantics remain incomplete | Integration + Component | P1 |
| Local upload issue/upload/confirm | `IssueWorkVideoUploadCommandHandler.cs`; `UploadLocalWorkVideoCommandHandler.cs`; `ConfirmWorkVideoUploadCommandHandler.cs`; local storage/selector/file inspector | `WorkVideoEndpointsTests.cs` asserts upload-url auth rejection, upload to missing work, invalid file metadata, missing multipart file, upload target response shape, direct local upload success, file exists on disk, confirm persists metadata/session status/version, and confirmed local video appears in public projection; delete work removes rows/sessions and queues cleanup | Integration | Partially covered | R2-backed issue/confirm path with fake object storage | Invalid session, expired confirm session, wrong stored file size/MIME, missing stored object, stale version, repeated confirm/upload, max count | Anonymous/non-admin is covered for upload-url with valid CSRF; invalid/missing CSRF and non-admin for every WorkVideo route remain unsampled | Upload session confirmed status and local file side effect are now asserted; cleanup execution is component-tested for local files | Integration + Component | P1 |
| HLS transcoding/job | `StartWorkVideoHlsJobCommandHandler.cs`; `WorkVideoHlsJobPlan.cs`; `FfmpegVideoTranscoder.cs`; `WorkVideoHlsWorkspace.cs`; `WorkVideoHlsOutputPublisher.cs` | `WorkVideoHlsJobPlanTests.cs` asserts HLS keys/source key and optional timeline preview; `WorkVideoEndpointsTests.cs` asserts fake ffmpeg segments manifest/timeline files and HLS job stores manifest/playback/timeline URLs | Unit, Integration | Partially covered | HLS job with preview disabled and projection in admin/public | ffmpeg failure, ffprobe failure, timeout, malformed manifest, empty file, unsupported MIME, stale version | Anonymous/non-admin/CSRF not tested | Workspace cleanup and output publish rollback not asserted | Unit + Component + Integration | P1 |
| Reorder/delete individual videos | `ReorderWorkVideosCommandHandler.cs`; `DeleteWorkVideoCommandHandler.cs`; `WorkVideoCommandStore.cs`; cleanup store | `WorkVideoEndpointsTests.cs` asserts stale reorder conflict, successful reorder persists admin/public/DB order, invalid IDs return `400`, individual delete removes the video row, increments `videos_version`, empties public projection, queues cleanup, and missing video returns `404`; `WorkVideoComponentTests.cs` asserts reorder handler deterministic sort writes and delete handler HLS cleanup normalization/remaining-order compaction | Integration, Component | Partially covered | Missing-work behavior for reorder/delete routes | Duplicate ID reorder payload, stale version on delete, max-count interaction after delete | Anonymous/non-admin/CSRF is not sampled for reorder/delete specifically | Cleanup jobs for individual delete and `VideosVersion` increment are now asserted; actual delete execution is covered separately through local cleanup service component tests | Integration + Component | P1 |
| Video cleanup service/worker | `WorkVideoCleanupService.cs`; `VideoStorageCleanupWorker.cs`; `WorkVideoCleanupStore.cs`; `VideoStorageCleanupJob`; storage services | `WorkVideoEndpointsTests.cs` asserts expired upload sessions are marked expired and cleanup is enqueued; delete-work test asserts cleanup jobs exist; `WorkVideoComponentTests.cs` asserts local cleanup deletes files and marks success, missing storage marks failed, delete exceptions increment attempts and fail at max attempts, expired unconfirmed sessions enqueue cleanup, confirmed/active sessions are ignored, and local HLS manifest delete removes the directory | Integration, Component | Partially covered | Hosted worker loop lifecycle with scoped service resolution | R2 cleanup object listing/delete behavior, cancellation while processing, retry remains pending before final attempt, missing object semantics | Not endpoint auth-related | Local storage deletion and cleanup job status transitions are now asserted; R2 side effects remain untested | Component | P1 |

## 9. AI/Codex/blog fix/batch jobs/runtime config

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| Blog fix endpoint | `BlogFixEndpoints.cs`; `FixBlogHtmlCommandHandler.cs`; `BlogAiFixService.cs`; `AiHttpResultMapper.cs`; `AiOptions` | `AdminAiEndpointsTests.cs` asserts anonymous blog-fix rejection with valid CSRF, missing HTML 400, provider payload, requested provider forwarding, custom prompt forwarding; component Codex tests cover Codex process env/argument/failure behavior | Integration, Component | Partially covered | OpenAI provider path with fake `HttpClient` and endpoint response shape | Provider exceptions, timeout, invalid provider/model/reasoning effort fallback details, empty AI response, HTML sanitization expectations | Non-admin and invalid/missing CSRF for blog-fix not tested | No persistence side effects expected for single fix | Component + Integration | P1 |
| Work enrich endpoint | `WorkEnrichEndpoint.cs`; `EnrichWorkHtmlCommandHandler.cs`; shared AI service | `AdminAiEndpointsTests.cs` asserts provider payload for `/api/admin/ai/work-enrich` | Integration | Happy-path only | Provider/model/custom prompt/title forwarding | Missing HTML/title, provider failure, invalid runtime config | Anonymous/non-admin/CSRF not tested | No persistence side effects expected | Integration | P2 |
| Legacy/direct blog fix batch endpoint | `BlogFixEndpoints.cs`; `FixBlogBatchCommandHandler.cs`; `IBlogAiFixService` | `AdminAiEndpointsTests.cs` asserts `blog-fix-batch` applies updated HTML/excerpt when requested | Integration | Happy-path only | Non-apply preview mode, `all=true`, multiple blog IDs | Missing/empty blog IDs, missing blogs, provider failure, partial failure | Anonymous/non-admin/CSRF not tested | Only apply success asserts DB update; failure no-write behavior missing | Integration + Component | P1 |
| Persistent AI batch jobs | `BatchJobEndpoints.cs`; `Create/List/Get/Apply/Cancel/CancelQueued/ClearCompleted/Remove*CommandHandler.cs`; `AiBatchJobRunner.cs`; `AiBatchJobScheduler.cs`; `AiBatchJobItemProcessor.cs`; `AiBlogFixBatchStore.cs`; `AiBatchJobProcessor.cs`; `AiBatchJobSignal.cs`; `AiBatchJobItemDispatcher.cs`; `AiBatchJob`, `AiBatchJobItem` | `AdminAiEndpointsTests.cs` asserts create/list/detail, no-target validation, repeated active selection returns existing job, custom prompt persistence and item fixed HTML, cancel single job response, apply completed results, remove completed job, auto-apply success; `AiBatchRuntimeComponentTests.cs` asserts selected/all target selection, queued job/item persistence, worker-count clamping, running reset, queued-to-running-to-completed success, full failure, partial failure, item error/fixed HTML persistence, and unrelated data preservation | Integration, Component | Partially covered | `cancel-queued` and `clear-completed` endpoints, endpoint-level `all=true`, list counts | Job not found for get/apply/cancel/delete, queued/running apply rejection, cancellation during execution, duplicate blog IDs edge beyond active-selection reuse | Anonymous/non-admin/CSRF not tested for batch endpoints | Store status transitions and failed/partial item persistence are now component-tested; cancellation persistence remains incomplete | Component + Integration | P1 |
| Runtime config | `RuntimeConfigEndpoint.cs`; `GetAiRuntimeConfigQueryHandler.cs`; `AiRuntimePolicy.cs`; `AiRuntimeCapabilities.cs`; `AiOptionsValidator.cs`; `AiOptionsPostConfigure.cs` | `AdminAiEndpointsTests.cs` asserts runtime config anonymous rejection and payload contains provider, available providers, codex model, batch concurrency, default prompt; `StartupOptionsValidationTests.cs` asserts invalid provider fails startup; `AiBatchRuntimeComponentTests.cs` asserts full runtime DTO defaults, provider fallback when configured provider is unavailable, env-style post-configure overrides/defaults, and invalid options validation failures | Integration, Component | Partially covered | Additional OpenAI vs Azure DTO variants | Prompt file missing/invalid JSON fallback, full env override precedence matrix, invalid model fallback details through endpoint | Non-admin runtime config access not tested | No persistence side effects expected | Component + Integration | P2 |
| Codex runtime integration | `BlogAiFixService.cs`; `CodexRuntimeEnvironmentComponentTests.cs` target behavior; `AiOptions` | `BlogAiFixServiceCodexRuntimeComponentTests.cs` asserts available providers include OpenAI/Codex, Codex home file fails clearly, configured OpenAI key exported, `CODEX_HOME` exported, fake process receives model/reasoning/workdir arguments, and non-zero fake process exit surfaces stderr; `CodexRuntimeEnvironmentComponentTests.cs` asserts Codex home exists and failure when file | Component | Component-only | Basic successful Codex invocation with env, args, and workdir is now covered | Timeout, output too large, auth file missing, invalid command path, remote image artifact handling | Endpoint auth is only covered for blog-fix/runtime-config, not Codex-specific provider selection | No persistence side effects expected | Component | P1 |
| Provider contract verification | Pact provider verification | `ProviderContractVerificationTests.cs` verifies pact files only when `PACT_PROVIDER_BASE_URL` and pact files exist; otherwise logs skip and returns | Contract | Unknown, needs manual review | Provider running against current pact set in CI | Missing pact directory, stale pacts, provider state setup | Authenticated provider states not visible in test | No persistence setup visible | Contract | P2 |

## 10. Persistence/EF model/seeding

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| EF model contracts | `WoongBlogDbContext.cs`; Domain entities; persistence configuration | `DbContextModelContractComponentTests.cs` asserts aggregate entity keys, required properties, JSON column types, indexes, uniqueness metadata, and WorkVideo cascade metadata; `PersistenceContractTests.cs` asserts existing JSON/search/index contracts; `PostgresPersistenceContractTests.cs` asserts relational NOT NULL, duplicate blog slug/session key unique violations, and DB-side WorkVideo/upload-session cascade | Component, Integration | Partially covered | Remaining asset/profile/session/job relationship/delete behavior and max lengths if configured | Provider differences beyond selected Postgres relational constraints and search schema | Not applicable | Cascade/delete behavior is covered for WorkVideo/upload sessions only; assets/sessions/jobs remain incomplete | Component + Integration | P1 |
| Content search fields | `ContentSearchFieldSynchronizer.cs`; `ContentSearchText.cs`; `WoongBlogDbContext.SaveChanges` | `PersistenceContractTests.cs` asserts save populates normalized blog/work `SearchTitle` and `SearchText`; `DbContextModelContractComponentTests.cs` asserts updates refresh search fields; public endpoint/component tests assert title/content/unified search | Integration, Component | Partially covered | Insert and update refresh behavior is now covered | Null/malformed JSON, tag/category inclusion expectations, delete behavior | Not applicable | Postgres trigram query behavior is schema-tested but search semantics run on test host/InMemory | Component + Integration | P1 |
| Seed data and bootstrapper | `SeedData.cs`; `DatabaseBootstrapper.cs`; `DatabaseInitializationExtensions.cs`; `SchemaPatch` | `PersistenceContractTests.cs` asserts seed data only once and core data; `DatabaseBootstrapperTests.cs` asserts initialization idempotence, rehydrates public detail seeds, reuses seeded work video slots; `PostgresPersistenceContractTests.cs` asserts Postgres extension/schema patch/indexes plus Postgres bootstrap idempotence and runtime blog preservation | Integration | Partially covered | Production provider bootstrap idempotence and runtime-row preservation are now sampled with Postgres | Partial existing data for every table, corrupt seed JSON, failed bootstrap rollback | Not applicable | Existing runtime-data preservation is sampled, not exhaustive | Integration | P1 |
| Page views/analytics persistence | `PageView.cs`; dashboard query store | Only dashboard count asserts `ViewsCount >= 0`; no direct page-view insertion/query tests found | Integration indirect | Untested | Insert and aggregate page views | Duplicate/session/date/entity boundaries | Not applicable | Page view retention, uniqueness, and aggregation side effects untested | Component + Integration | P2 |

## 11. Startup/DI/options/middleware

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| Startup composition and DI | `Program.cs`; module `*ServiceCollectionExtensions.cs`; `InfrastructureServiceCollectionExtensions.cs`; `ApplicationServiceCollectionExtensions.cs` | `StartupCompositionTests.cs` asserts root redirects to `/api/health`, broad Application handler/validator resolution, Infrastructure store/service resolution, hosted service registration, health-check service registration, options binding, runtime config startup without external services, and auth expiration defaults; `ArchitectureBoundaryTests.cs` asserts module registration extension types, forbids concrete infra in API module registrations, and pins Program composition order | Integration, Architecture | Partially covered | Important handlers/stores/hosted services are now sampled broadly | Missing optional provider/config combinations, duplicate registrations, and hosted worker lifecycle execution | Not directly auth-related | Hosted background service registration is covered, but lifecycle behavior remains untested | Integration + Architecture | P1 |
| Options validation | `StartupOptionsValidationExtensions.cs`; `AuthOptionsValidator.cs`; `ProxyOptionsValidator.cs`; `SecurityOptionsValidator.cs`; `AiOptionsValidator.cs`; `AiOptionsPostConfigure.cs` | `StartupOptionsValidationTests.cs` asserts testing config valid, invalid proxy CIDR, invalid auth public origin, invalid AI provider, invalid AI batch concurrency, missing auth media root, missing antiforgery header, and production auth missing credentials fail; `StartupCompositionTests.cs` asserts key options bind in the test host | Integration | Partially covered | Testing-host binding is now sampled for auth/security/proxy/AI/R2/HLS options | Invalid data-protection path, HTTPS/HSTS combos, Codex invalid model/effort, and R2/work-video option bounds remain incomplete | Not applicable | No side effects except startup fail/pass | Integration + Unit | P1 |
| Middleware pipeline | `Program.cs`; `SecurityHeadersMiddleware.cs`; `AntiforgeryValidationMiddleware.cs`; forwarded headers/security/static files | `AuthSecurityTests.cs` samples security headers and CSRF for site settings; `StartupCompositionTests.cs` root redirect, OpenAPI registration in Testing, runtime config startup, and anonymous unsafe admin mutation rejected by CSRF before authorization; uploads/HLS tests indirectly exercise static files; `ArchitectureBoundaryTests.cs` pins Program middleware order | Integration, Architecture | Partially covered | OpenAPI/testing registration and CSRF-before-authorization behavior are now asserted | Forwarded headers, HTTPS redirection/HSTS, static file edge cases, and request body multipart ordering edge cases remain incomplete | Admin endpoint auth coverage is representative but not exhaustive | Rejected mutation no-side-effect not broadly asserted | Integration | P1 |
| Request validation endpoint filter and exception filter | `RequestValidationEndpointFilter.cs`; `EndpointBuilderExtensions.cs`; `ValidationExceptionFilter.cs`; API request validators | Endpoint tests assert selected 400s for content/site/uploads/AI | Integration | Partially covered | Direct API request validator unit tests or representative endpoint 400s for every request type | Error payload schema, multiple errors, malformed JSON body | Not applicable | No side effects on validation failure not broadly asserted | Unit + Integration | P2 |

## 12. Observability/health/metrics

| Feature | Production files involved | Existing test files | Existing test level | Current coverage classification | Missing happy-path tests | Missing failure/edge-case tests | Missing auth/authorization tests | Missing persistence/side-effect tests | Recommended test project | Priority |
|---|---|---|---|---|---|---|---|---|---|---|
| Health endpoint | `Controllers/HealthController.cs`; `Program.cs`; `ApiServiceCollectionExtensions.cs` | `PublicEndpointsTests.cs` asserts `GET /api/health` returns OK; `StartupCompositionTests.cs` asserts `/` redirects to `/api/health`, `HealthCheckService` resolves, and `/api/health` payload includes `status`, `service`, and timestamp | Integration | Partially covered | Health payload shape now covered | Failure/degraded health if health checks become meaningful | Endpoint should remain anonymous; no explicit auth regression beyond unauthenticated client success | No persistence side effects expected | Integration | P3 |
| Metrics/observability | `AddHealthChecks()` appears in API core, but no mapped health-check endpoint or metrics/OpenTelemetry source was found | `StartupCompositionTests.cs` asserts `HealthCheckService` registration; no metrics endpoint exists | Integration | Partially covered | Health-check service registration is covered; no metrics endpoint was found to test | Misconfiguration/no-op detection; metrics exposure if intended | Auth policy for metrics endpoint if added | No persistence side effects expected | Integration + Architecture | P3 |

## Cross-Cutting Gaps

| Gap | Evidence | Recommended project | Priority |
|---|---|---|---|
| Admin authorization is not consistently asserted | `AuthFlowIntegrationTests.cs` samples representative admin GET endpoints; `AdminMutationEndpointsTests.cs` now samples anonymous 401 and non-admin 403 with valid CSRF across page update, blog create/update/delete, work create/update/delete, and site-settings update; `AdminAiEndpointsTests.cs` samples anonymous AI blog-fix/runtime-config rejection. Media, AI batch, work-video, and upload/delete mutation families still lack endpoint-specific matrices. | Integration | P1 |
| CSRF is only sampled against site settings | `AuthFlowIntegrationTests.cs` adds invalid-token/no-persist, valid-token, logout missing-token, and valid-token auth-failure samples; `AdminMutationEndpointsTests.cs` uses valid CSRF for admin mutation auth checks; `AdminAiEndpointsTests.cs` uses valid CSRF for anonymous AI blog-fix rejection. Invalid/missing/stale CSRF behavior for blog/work/page/media/AI/work-video mutations still relies on middleware inference. | Integration | P1 |
| Component coverage is thin for admin command handlers/stores | Most admin content behavior is endpoint-only; no component tests for `BlogCommandStore`, `WorkCommandStore`, `PageCommandStore`, AI batch store failure paths, media store | Component | P1 |
| Contract tests can silently skip | `ProviderContractVerificationTests.cs` returns when env or pact files are missing | Contract | P2 |
| Postgres behavior is only partly covered | Postgres test checks schema/search indexes, but most query/store behavior runs on EF InMemory/test host | Integration | P1 |
| Failure side-effect guarantees are weak | Validation/failure tests rarely assert no DB/file/object-storage changes | Integration + Component | P1 |

## Recommended Prioritization

P1 direct additions should focus on risk boundaries:

- Continue the admin auth/authorization and CSRF matrix for media, AI, and work-video mutations beyond the page/blog/work/site mutation samples added on 2026-04-25.
- Component tests for admin command handlers/stores and AI batch state transitions.
- Media/work-video storage failure and cleanup behavior, especially R2/object-storage abstractions.
- Postgres-backed query behavior for search/pagination if CI can support Testcontainers reliably.

P2 additions should deepen edge cases:

- Site settings admin-get and full social/resume field behavior.
- Dashboard/member query store component tests.
- Remaining runtime config prompt-file fallback and Codex timeout/auth/invalid-command failure modes.
- Request validation payload schema and every request validator.

P3 additions should cover low-risk observability gaps:

- Health payload shape and future mapped health checks/metrics if intentionally exposed.

## Priority 1 Auth Test Implementation Update - 2026-04-25

This update implemented only the requested backend Priority 1 auth/session/login/logout/CSRF/admin-authorization tests. No production auth architecture was rewritten.

### Tests Added

- `AuthFlowIntegrationTests.cs`
  - Full session payload for authenticated admin.
  - Authenticated non-admin session role payload.
  - Representative admin GET endpoints return 401 for anonymous requests.
  - Representative admin GET endpoints return 403 for authenticated non-admin requests.
  - Representative admin GET endpoints return success for authenticated admin requests.
  - `/api/auth/login` challenges a fake OIDC provider without real Google OAuth.
  - External login return URLs are not exposed in the auth redirect.
  - `POST /api/auth/logout` rejects missing CSRF.
  - `POST /api/auth/logout` succeeds with valid CSRF, returns redirect payload, and clears the auth cookie.
  - Admin site-settings mutation rejects invalid CSRF and preserves existing DB state.
  - Admin site-settings mutation accepts valid CSRF and persists.
  - Valid-CSRF admin mutation returns 401 for anonymous and 403 for non-admin principals.
- `AuthRecorderComponentTests.cs`
  - Malformed session-id claim does not authenticate.
  - Absolute-expired session does not authenticate and is revoked.
  - Session with missing profile does not authenticate and is revoked.

### Files Changed

- `backend/tests/WoongBlog.Api.IntegrationTests/AuthFlowIntegrationTests.cs` added.
- `backend/tests/WoongBlog.Api.IntegrationTests/TestOidcChallengeHandler.cs` added as fake test auth infrastructure for OIDC challenge behavior.
- `backend/tests/WoongBlog.Api.IntegrationTests/TestAuthHandler.cs` updated to include a non-admin `user` identity and fuller admin/user claims.
- `backend/tests/WoongBlog.Api.IntegrationTests/CustomWebApplicationFactory.cs` updated to register the fake OIDC challenge handler under the production OIDC scheme name.
- `backend/tests/WoongBlog.Api.ComponentTests/AuthRecorderComponentTests.cs` updated with three session invalidation edge tests.
- `todolist-2026-04-25.md` updated with the required implementation TODO, scope mapping, backups, and verification results.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.md` updated with this implementation report and validation results.

### Behavior Covered

- Authenticated admin, authenticated non-admin, and anonymous identity paths through the session endpoint and representative admin HTTP endpoints.
- API unauthorized behavior returns 401 without browser redirect for anonymous admin GET requests.
- API forbidden behavior returns 403 for authenticated non-admin admin GET requests.
- Login challenge redirect behavior is covered through a fake OIDC challenge handler, not real Google OAuth.
- Logout POST is covered for missing CSRF and valid-CSRF success including cookie clearing.
- CSRF missing/invalid/valid behavior is covered for logout and site-settings mutation, including a no-persist assertion on invalid token rejection.
- Invalid session claim, absolute expiration, and missing-profile invalidation are covered at component level through `AuthRecorder`.

### Remaining Auth/Session/CSRF Gaps

- OIDC callback success/failure, provider failure, disabled-auth login, missing claims in callback, and non-admin login rejection through the full callback host remain untested.
- Endpoint-level revocation/audit side effects for logout with a real cookie-authenticated session remain untested because the integration host still uses the test auth handler for request identity.
- Anonymous/non-admin and CSRF matrices are still representative, not exhaustive, for every admin mutation, media upload/delete, AI batch, and work-video endpoint.
- CSRF stale token, missing cookie, duplicate header, multipart upload token behavior, and rejected-mutation side effects outside site settings remain untested.
- Existing-profile login update, multiple concurrent auth sessions, admin override changes over time, role downgrade semantics, IP capture, user-agent truncation, and session-key collision behavior remain component-test gaps.

### Backup

Backups were prepared before test/report edits under `.agent-backups/priority1-auth-tests-2026-04-25/`.

## Priority 1 Admin Mutation Test Implementation Update - 2026-04-25

This update implemented only the requested backend Priority 1 destructive/state-changing admin command tests. No production code was changed. No public read tests were added, and no WorkVideo-specific endpoint tests were added.

### Tests Added

- `AdminMutationEndpointsTests.cs`
  - Page update rejects anonymous and non-admin principals when CSRF is valid.
  - Page update success returns `{ success: true }`, persists target page changes, and leaves an unrelated page unchanged.
  - Page update invalid body returns `400 Bad Request` and does not persist.
  - Blog create/update/delete reject anonymous and non-admin principals when CSRF is valid.
  - Blog create success returns `{ id, slug }` and persists title, excerpt, tags, content JSON, `Published`, and `PublishedAt`.
  - Blog invalid create returns `400 Bad Request` and does not create a row.
  - Blog update success returns `{ id, slug }`, changes expected fields, can unpublish via `Published = false`, preserves `CreatedAt`, and leaves an unrelated blog unchanged.
  - Blog missing update/delete return `404 Not Found` and do not affect existing blogs.
  - Blog delete success hard-deletes only the target blog.
  - Work create/update/delete reject anonymous and non-admin principals when CSRF is valid.
  - Work create success returns `{ id, slug }` and persists title, category, period, tags, content JSON, all-properties JSON, `Published`, and `PublishedAt`.
  - Work invalid create returns `400 Bad Request` and does not create a row.
  - Work update success returns `{ id, slug }`, changes expected fields, can unpublish via `Published = false`, preserves `CreatedAt`, and leaves an unrelated work unchanged.
  - Work missing update/delete return `404 Not Found` and do not affect existing works.
  - Work delete success hard-deletes only the target work.
  - Site settings update rejects anonymous and non-admin principals when CSRF is valid.
  - Site settings partial update returns `{ success: true }`, persists supplied fields, and preserves omitted fields.
  - Site settings invalid body returns `400 Bad Request` and does not persist.

### Files Changed

- `backend/tests/WoongBlog.Api.IntegrationTests/AdminMutationEndpointsTests.cs` added.
- `todolist-2026-04-25.md` updated with this admin mutation test TODO, backups, and verification results.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.md` updated with this implementation report and validation results.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.html` updated.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.json` updated.

### Admin Mutation Coverage Improved

- Page update now has endpoint-level auth rejection, success response-shape, DB persistence, invalid-request no-write, and unrelated-record preservation coverage.
- Blog create/update/delete now have endpoint-level auth rejection, validation failure, missing-entity, hard-delete, publish/unpublish, response-shape, DB persistence, and unrelated-record preservation coverage.
- Work create/update/delete now have endpoint-level auth rejection, validation failure, missing-entity, hard-delete, publish/unpublish, response-shape, DB persistence, and unrelated-record preservation coverage, without invoking WorkVideo-specific endpoints.
- Site settings update now has endpoint-level auth rejection, response-shape, partial update, omitted-field preservation, and invalid-request no-write coverage.

### Remaining Admin Mutation Gaps

- Page create/delete endpoints do not exist in the current API surface; only page update was testable.
- Admin member create/update/delete/update-role endpoints do not exist in the current API surface; only member listing exists and remains outside this destructive-command slice.
- Invalid/missing/stale CSRF behavior is still not sampled for every page/blog/work mutation; this slice used valid CSRF for auth rejection and focused invalid-CSRF coverage remains in the auth slice.
- Blog/work duplicate slug on update, invalid JSON semantics, tag boundaries beyond existing validator samples, search-field updates, asset link/update semantics, and detailed `UpdatedAt` behavior remain incomplete.
- Media upload/delete, AI batch mutations, and WorkVideo mutation endpoints remain outside this step and should be handled in later focused slices.

### Backup

Backups were prepared before test/report edits under `.agent-backups/priority1-admin-mutation-tests-2026-04-25/`.

## Public Read Query Test Implementation Update - 2026-04-25

This update implemented only the requested backend public read/query tests. No production code was changed. No admin mutation tests were added, and no WorkVideo upload/order/delete tests were added.

### Tests Added

- `PublicQueryHandlerComponentTests.cs`
  - Public home returns null when site settings are missing.
  - Public home maps home page content, site settings, resume URL, work/blog asset URLs, published-only summaries, and published-date ordering.
  - Public site settings maps all public social fields.
  - Public page-by-slug returns page content when present and null when missing.
  - Public work list returns a stable empty page when no works are published.
  - Public work list excludes drafts, orders by `PublishedAt`, maps thumbnail/icon asset URLs, and clamps an over-large requested page to the last page.
  - Public work detail returns assets, social share message, `videos_version`, and public video DTOs, and returns null for draft or missing slugs.
  - Public blog list returns a stable empty page when no blogs are published.
  - Public blog list excludes drafts, orders by `PublishedAt`, maps cover asset URLs, and clamps an over-large requested page to the last page.
  - Public blog detail returns cover asset DTO fields and returns null for draft or missing slugs.
- `PublicEndpointsTests.cs`
  - Public site settings returns typed DTO shape to an anonymous client, including current `gitHubUrl`/`linkedInUrl` JSON contract casing and excluding `resumeAssetId`.
  - Public home returns typed DTO shape to an anonymous client.
  - Public page-by-slug route returns typed DTO shape to an anonymous client and missing slugs return `404 Not Found`.
  - Public work detail returns typed DTO shape with thumbnail/icon/video media fields to an anonymous client.
  - Draft work detail returns `404 Not Found`.
  - Public works list excludes drafts, orders by published date, maps assets, and returns typed paging data.
  - Public blog detail returns typed DTO shape with cover asset field to an anonymous client.
  - Draft blog detail returns `404 Not Found`.
  - Public blogs list excludes drafts, orders by published date, maps cover assets, and returns typed paging data.
  - Public work/blog list endpoints return stable empty paged responses when the content tables are empty.

### Files Changed

- `backend/tests/WoongBlog.Api.ComponentTests/PublicQueryHandlerComponentTests.cs` updated.
- `backend/tests/WoongBlog.Api.IntegrationTests/PublicEndpointsTests.cs` updated.
- `todolist-2026-04-25.md` updated with this public read/query TODO, backups, and verification results.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.md` updated with this implementation report and validation results.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.html` updated.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.json` updated.

### Public Query Coverage Improved

- Public home now has component coverage for missing prerequisites, DTO field mapping, resume URL mapping, draft filtering, asset projection, and deterministic published-date ordering, plus endpoint-level typed DTO shape coverage.
- Public site settings now has component coverage for all social fields and endpoint-level typed DTO shape coverage for the current public JSON contract.
- Public pages now have direct component and endpoint coverage for slug success and missing-slug not found behavior.
- Public blog/work list queries now have component and endpoint coverage for draft exclusion, published-date ordering, asset URL mapping, pagination boundary clamping or stable empty pagination, and anonymous endpoint access.
- Public blog/work detail queries now have component and endpoint coverage for draft/missing slug exclusion and related asset/media DTO fields.

### Remaining Public Read Gaps

- Endpoint-level missing singleton/site settings and missing home page `404` behavior remain untested because the default integration host is seeded; those paths are covered at component level only.
- Invalid `pageSize=0` normalization and unknown `searchMode` fallback are covered by handler logic patterns but not explicit endpoint tests.
- Malformed `ContentJson`/`AllPropertiesJson` behavior for public projections remains only partially covered.
- Public query semantics still run against EF InMemory/test-host storage; direct PostgreSQL query behavior for ordering/search remains a broader persistence gap.
- Public resume endpoint still lacks endpoint-level 404 coverage when the resume asset is absent.

### Backup

Backups were prepared before test/report edits under `.agent-backups/public-read-query-tests-2026-04-25/`.

## WorkVideo Test Implementation Update - 2026-04-25

This update implemented only the requested backend WorkVideo tests. No production code was changed. No frontend tests, real external storage, or R2 network calls were added.

### Tests Added

- `WorkVideoEndpointsTests.cs`
  - Added `Category=Integration` trait so WorkVideo endpoint tests are selected by `--filter "Category=Integration"`.
  - Upload-url endpoint rejects anonymous and non-admin callers when CSRF is valid.
  - Upload-url to a missing work returns `404 Not Found` with the expected error body.
  - Invalid upload metadata returns `400 Bad Request`.
  - Valid multipart upload without a `file` part returns `400 Bad Request`.
  - Successful local upload/confirm returns upload target and mutation response shapes, persists metadata, marks the upload session confirmed, increments `videos_version`, stores the file under the configured media root, and projects the video through the public work detail endpoint.
  - Individual video delete removes the row, increments `videos_version`, queues local storage cleanup, and removes the video from public projection.
  - Missing video delete returns `404 Not Found`.
  - Successful reorder now also asserts deterministic persisted DB sort order.
  - Invalid reorder IDs return `400 Bad Request` and leave persisted order unchanged.
  - Public work detail returns published video data while a draft work with a video stays hidden behind `404 Not Found`.
- `WorkVideoComponentTests.cs`
  - Local video storage saves, reads metadata/prefix, produces upload target shape, and deletes a temp-root file.
  - Local HLS manifest delete removes the HLS directory.
  - Cleanup service deletes a local file and marks the job succeeded.
  - Cleanup service marks a job failed when no storage backend exists.
  - Cleanup service increments attempts and marks a job failed on the fifth delete failure.
  - Expired unconfirmed upload sessions are marked expired and enqueue cleanup; confirmed and active sessions are ignored.
  - Reorder handler rewrites sort order deterministically through EF-backed stores.
  - Delete handler removes the video, compacts remaining sort order, and normalizes HLS cleanup to the underlying local manifest key.

### Files Changed

- `backend/tests/WoongBlog.Api.IntegrationTests/WorkVideoEndpointsTests.cs` updated.
- `backend/tests/WoongBlog.Api.ComponentTests/WorkVideoComponentTests.cs` added.
- `todolist-2026-04-25.md` updated with this WorkVideo test TODO, backups, and verification results.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.md` updated with this implementation report and validation results.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.html` updated.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.json` updated.

### WorkVideo Coverage Improved

- WorkVideo upload now has endpoint-level auth rejection, missing-work not found, invalid metadata, missing multipart file, success response-shape, DB persistence, upload-session status, local file side-effect, and public projection coverage.
- WorkVideo metadata persistence now verifies source type/key, original file name, MIME type, file size, sort order, and `videos_version`.
- Reorder/delete coverage now includes deterministic persisted sort order, invalid IDs, missing video, individual delete DB removal, cleanup scheduling, and public projection after delete.
- Component coverage now exercises local storage save/read/delete, HLS-directory delete semantics, cleanup service success/failure/max-attempt behavior, expired-session cleanup, reorder handler/store ordering, and delete handler cleanup normalization.
- Public query coverage now explicitly confirms published work videos are returned and draft work videos are not publicly visible.

### Remaining WorkVideo And Storage Gaps

- R2/object-storage behavior remains untested without real external storage: presigned upload URL generation, object metadata, byte-range prefix reads, object delete, and HLS prefix listing/delete should be covered with a fake S3/R2 client or adapter seam before enabling R2-heavy changes.
- Confirm-upload failure paths still need direct coverage for invalid session, expired session, missing stored object, wrong stored size/MIME, invalid MP4 bytes after storage, stale version, and repeated confirm/upload attempts.
- HLS job failure behavior remains incomplete: ffmpeg/ffprobe failure, timeout, malformed manifest, unsupported MIME, empty file, stale version, workspace cleanup, and output-publish rollback are still only partially covered.
- WorkVideo CSRF/auth coverage is representative, not exhaustive; upload-url has anonymous/non-admin coverage, but invalid/missing CSRF and auth matrices for upload, confirm, HLS job, YouTube add, reorder, and delete remain broader middleware-inference gaps.
- Hosted `VideoStorageCleanupWorker` lifecycle behavior and cleanup cancellation behavior remain untested.
- PostgreSQL-backed WorkVideo ordering/query behavior remains unverified; current query/store behavior is exercised with EF InMemory and the ASP.NET test host.

### Backup

Backups were prepared before test/report edits under `.agent-backups/workvideo-tests-2026-04-25/`.

## AI Batch Runtime Test Implementation Update - 2026-04-25

This update implemented only the requested backend AI/Codex/batch/runtime config tests. No production code was changed. No frontend tests were added. No real external AI service, real Codex binary, or shell tool was invoked outside the existing fake-script component-test pattern.

### Tests Added

- `AdminAiEndpointsTests.cs`
  - Admin AI blog-fix rejects an anonymous request when a valid CSRF token is supplied.
  - Runtime config rejects an anonymous request.
  - Persistent batch-job creation returns `400 Bad Request` when neither `blogIds` nor `all=true` is supplied.
  - Repeated active batch-job creation with the same selected blog set returns the existing queued job instead of creating duplicate job/item records; the test disables only the AI batch hosted worker to keep the active queued state deterministic.
- `AiBatchRuntimeComponentTests.cs`
  - `CreateBlogFixBatchJobCommandHandler` persists a queued job and pending items for selected targets, trims custom prompts, clamps `workerCount`, records provider/model/reasoning metadata, and signals work.
  - `AiBlogFixBatchStore` selects requested targets or all targets deterministically by `UpdatedAt`.
  - `AiBatchJobScheduler.ResetRunningJobsAsync` requeues interrupted running jobs.
  - `AiBatchJobScheduler.ProcessQueuedJobsUntilEmptyAsync` moves queued work through running to completed, persists fixed HTML/provider/model data, and saves running state before invoking the fake runtime.
  - Full runtime failure marks the job `failed`, records failed counts, stores the item error, and leaves `FixedHtml` unset.
  - Partial failure with `AutoApply=true` leaves the job `completed`, represents one applied item and one failed item, updates only the successful blog, and preserves failed/unrelated blog content.
  - Runtime config handler returns stable default DTO values and prompt metadata.
  - Runtime config handler falls back safely when the configured provider is unavailable.
  - `AiOptionsPostConfigure` applies environment-style overrides and stable defaults.
  - `AiOptionsValidator` fails safely for invalid provider, timeout, batch concurrency, retention, endpoint, allowed-model, and reasoning-effort configuration.
- `BlogAiFixServiceCodexRuntimeComponentTests.cs`
  - Fake Codex process receives model, reasoning effort, and workdir arguments and returns cleaned HTML.
  - Non-zero fake Codex process exit surfaces stderr in the thrown exception.

### Files Changed

- `backend/tests/WoongBlog.Api.IntegrationTests/AdminAiEndpointsTests.cs` updated.
- `backend/tests/WoongBlog.Api.ComponentTests/AiBatchRuntimeComponentTests.cs` added.
- `backend/tests/WoongBlog.Api.ComponentTests/BlogAiFixServiceCodexRuntimeComponentTests.cs` updated.
- `todolist-2026-04-25.md` updated with this AI batch/runtime TODO, backups, and verification results.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.md` updated with this implementation report and validation results.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.html` updated.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.json` updated.

### AI Batch Runtime Coverage Improved

- Admin AI endpoint coverage now includes anonymous rejection for representative AI routes and validation for invalid persistent batch-job creation.
- Batch creation now has component coverage for selected/all target selection, queued job/item persistence, custom prompt normalization, provider/model/reasoning metadata, worker-count clamping, and batch signal emission.
- Batch status coverage now includes running-job reset, queued-to-running-to-completed success, all-failed final `failed`, partial failure represented as completed with failed item counts, and persisted item `FixedHtml`/`Error` results.
- Failure handling now asserts runtime exceptions are stored on failed items without corrupting unrelated blog content.
- Runtime config coverage now asserts stable DTO defaults, provider fallback, post-configure environment-style loading, and invalid options fail safely.
- Codex runtime coverage now asserts fake-process argument construction for model/reasoning/workdir and non-zero process stderr handling.

### Remaining AI Batch Runtime Gaps

- Batch endpoint auth remains representative: anonymous blog-fix/runtime-config is covered, but anonymous/non-admin/invalid-CSRF matrices for every batch endpoint are still incomplete.
- `cancel-queued`, `clear-completed`, not-found responses for get/apply/cancel/delete, queued/running apply rejection, and cancellation during execution still need focused endpoint/component tests.
- `all=true` batch creation is covered at store level but not yet through the endpoint.
- Runtime prompt-file missing/invalid JSON fallback and full environment override precedence remain incomplete.
- Codex timeout, output-size handling, missing auth file, invalid command path, and remote image artifact behavior remain untested.
- OpenAI/Azure HTTP provider paths still need fake `HttpClient` coverage for success and provider failure payloads.

### Backup

Backups were prepared before test/report edits under `.agent-backups/ai-batch-runtime-tests-2026-04-25/`.

## Persistence And Startup Test Implementation Update - 2026-04-25

This update implemented only the requested backend persistence, EF model, startup composition, DI, options, middleware, and health/observability tests. No production code was changed.

### Tests Added

- `DbContextModelContractComponentTests.cs`
  - DbContext model includes all expected aggregate entity types and primary keys.
  - Required EF model properties are non-nullable for important page/blog/work fields.
  - JSON column contracts remain `jsonb` for page/blog/work content fields.
  - Unique/index metadata remains present for page/blog/work slugs, published-date indexes, search-title indexes, profile provider subject, auth session key, AI batch job item entity identity, and WorkVideo sort order.
  - WorkVideo and upload-session relationships remain required cascade foreign keys to `Work`.
  - Blog/work search fields refresh when content changes on update, not only insert.
- `PostgresPersistenceContractTests.cs`
  - Added `Category=Integration` trait.
  - Existing Postgres schema/search extension test now runs from a reset Testcontainers database.
  - Postgres bootstrap is idempotent and preserves runtime blog data across repeated initialization.
  - Relational NOT NULL enforcement rejects a blog with a missing title.
  - Relational unique constraints reject duplicate blog slugs and duplicate auth session keys.
  - Database-side cascade deletes WorkVideos and WorkVideoUploadSessions when a Work is deleted with dependents not tracked by EF.
- `PersistenceContractTests.cs` and `DatabaseBootstrapperTests.cs`
  - Added `Category=Integration` traits so these existing persistence/bootstrap tests participate in the requested integration filter.
- `StartupCompositionTests.cs`
  - Added `Category=Integration` trait.
  - Resolves representative Application handlers/validators across content, site, composition, identity, media, AI batch, AI runtime, and WorkVideo flows.
  - Resolves important Infrastructure stores/services, `WoongBlogDbContext`, `HealthCheckService`, `IBlogAiFixService`, WorkVideo services, both local/R2 video storage implementations, and hosted service registrations.
  - Verifies test-host options binding for auth, security, proxy, AI, R2, and HLS options.
  - Verifies health, Testing OpenAPI, and admin AI runtime-config endpoints start without unavailable external services.
  - Verifies unsafe admin mutation without CSRF returns the CSRF failure before authorization.
- `StartupOptionsValidationTests.cs`
  - Added `Category=Integration` trait.
  - Invalid AI batch concurrency fails startup validation.
  - Missing required auth media root fails startup validation.
  - Missing antiforgery header name fails startup validation.
- `ArchitectureBoundaryTests.cs`
  - Program service composition, middleware, and endpoint mapping order is pinned as an Architecture test.

### Files Changed

- `backend/tests/WoongBlog.Api.ComponentTests/DbContextModelContractComponentTests.cs` added.
- `backend/tests/WoongBlog.Api.IntegrationTests/PostgresPersistenceContractTests.cs` updated.
- `backend/tests/WoongBlog.Api.IntegrationTests/PersistenceContractTests.cs` updated.
- `backend/tests/WoongBlog.Api.IntegrationTests/DatabaseBootstrapperTests.cs` updated.
- `backend/tests/WoongBlog.Api.IntegrationTests/StartupCompositionTests.cs` updated.
- `backend/tests/WoongBlog.Api.IntegrationTests/StartupOptionsValidationTests.cs` updated.
- `backend/tests/WoongBlog.Api.ArchitectureTests/ArchitectureBoundaryTests.cs` updated.
- `todolist-2026-04-25.md` updated with this persistence/startup TODO, backups, and verification results.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.md` updated with this implementation report and validation results.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.html` updated.
- `backend/reports/backend-test-coverage-audit-2026-04-24/backend-test-coverage-audit-2026-04-24.json` updated.

### Persistence And Startup Coverage Improved

- EF model metadata now has component coverage for aggregate keys, required fields, JSON column contracts, index/unique metadata, and WorkVideo cascade relationship metadata without a full host.
- Relational persistence contracts now use Postgres/Testcontainers for NOT NULL, unique slug/key, and database-side cascade behavior instead of EF InMemory.
- Bootstrap coverage now includes Postgres idempotence and runtime data preservation, not only EF InMemory seeding.
- Content search synchronization now covers updates to existing blog/work rows.
- Startup composition now resolves a broad set of important Application handlers, Infrastructure stores/services, hosted services, health checks, and options through the full ASP.NET test host.
- Options validation now covers additional missing/invalid required options for AI, auth storage, and antiforgery configuration.
- Middleware/endpoint registration now has observable coverage for Testing OpenAPI, health payload shape, runtime config startup without external services, and CSRF-before-authorization behavior.
- Architecture coverage now pins the centralized Program composition order.

### Remaining Persistence And Startup Gaps

- PostgreSQL-backed public search/query semantics remain incomplete; current public query behavior is still mostly covered through EF InMemory/test-host storage.
- Asset/profile/session/job relationship delete behavior remains incomplete beyond WorkVideo and upload-session cascade.
- EF max length contracts are not covered because the current model does not configure explicit max lengths for the tested entities.
- Partial existing-data bootstrap is still sampled, not exhaustive, across every seeded table and every corrupt seed JSON variant.
- Hosted service lifecycle behavior is still not exercised; the new tests assert registration and startup compatibility, not long-running worker loops.
- Optional provider/config combinations remain incomplete, especially R2 configuration, HLS tool path bounds, HTTPS/HSTS behavior, forwarded headers, and Codex model/effort invalid values at startup.
- No metrics endpoint or OpenTelemetry source was found; only health-check service registration and `/api/health` payload shape are tested.

### Backup

Backups were prepared before test/report edits under `.agent-backups/persistence-startup-tests-2026-04-25/`.

## 8-Step Direct Origin Push Plan

This is a direct origin push plan, not a PR plan.

1. Confirm the target branch for direct push and ensure `git status --short` has only the audit artifacts intended for commit.
2. Run `git fetch origin` to refresh the remote branch state.
3. Rebase or merge the local branch on top of the matching `origin/<branch>` and resolve conflicts without changing production or test code.
4. Run `dotnet test backend/WoongBlog.sln` and record the exact result in this report.
5. Review `git diff -- backend/tests backend/reports/backend-test-coverage-audit-2026-04-24 todolist-2026-04-25.md` to verify only backend test, audit, and TODO artifacts changed.
6. Commit with a targeted subject such as `Cover persistence startup backend tests`.
7. Push directly with `git push origin HEAD:<branch>`.
8. Verify the pushed commit on origin and inspect CI results for the direct push.

## Validation Log

| Command | Result | Notes |
|---|---|---|
| `rg --files backend/src backend/tests` | Passed | Used to inventory backend production and test files. |
| `rg -n "\[Fact\]\|\[Theory\]" backend/tests` | Passed | Used to enumerate existing test evidence. |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter FullyQualifiedName~AuthFlowIntegrationTests` | Passed, exit code 0 | 31 integration test cases passed after adding the fake OIDC challenge handler. Initial red run failed because the test host had no `OpenIdConnect` challenge scheme registered. |
| `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter FullyQualifiedName~AuthRecorderComponentTests` | Passed, exit code 0 | 12 component tests passed, including 3 new session invalidation cases. |
| `dotnet test backend/WoongBlog.sln` | Passed, exit code 0 | All five backend test projects passed after the P1 auth update: Contract 1, Unit 14, Component 37, Architecture 30, Integration 129. Restore/build emitted NU1901 warnings for `AWSSDK.Core` 4.0.0.17 low-severity advisory `GHSA-9cvc-h2w8-phrp`. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Unit"` | Passed, exit code 0 | Unit filter passed: 14 tests. Other projects reported no tests matching the filter. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Component"` | Passed, exit code 0 | Component filter passed: 37 tests. Other projects reported no tests matching the filter. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Integration"` | Passed, exit code 0 | Integration category filter passed: 73 tests. The full integration project has 129 test cases because pre-existing integration classes without `Category=Integration` traits are not selected by this filter. |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter FullyQualifiedName~AdminMutationEndpointsTests` | Passed, exit code 0 | 32 integration test cases passed for the P1 admin mutation slice. |
| `dotnet test backend/WoongBlog.sln` | Passed, exit code 0 | All five backend test projects passed after the P1 admin mutation update: Contract 1, Unit 14, Component 37, Architecture 30, Integration 161. Restore/build emitted NU1901 warnings for `AWSSDK.Core` 4.0.0.17 low-severity advisory `GHSA-9cvc-h2w8-phrp`. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Integration"` | Passed, exit code 0 | Integration category filter passed: 105 tests. The full integration project has 161 test cases because pre-existing integration classes without `Category=Integration` traits are not selected by this filter. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Component"` | Passed, exit code 0 | Component filter passed: 37 tests. Other projects reported no tests matching the filter. |
| `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter FullyQualifiedName~PublicQueryHandlerComponentTests` | Passed, exit code 0 | 32 component tests passed for the public query handler/store slice. |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter FullyQualifiedName~PublicEndpointsTests` | Passed, exit code 0 | 27 integration tests passed for public endpoints. Initial red run exposed the current `gitHubUrl`/`linkedInUrl` JSON casing; the test now documents that public contract. |
| `dotnet test backend/WoongBlog.sln` | Passed, exit code 0 | All five backend test projects passed after the public read/query update: Contract 1, Unit 14, Component 52, Architecture 30, Integration 172. Restore/build emitted NU1901 warnings for `AWSSDK.Core` 4.0.0.17 low-severity advisory `GHSA-9cvc-h2w8-phrp`. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Component"` | Passed, exit code 0 | Component filter passed: 52 tests. Other projects reported no tests matching the filter. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Integration"` | Passed, exit code 0 | Integration category filter passed: 116 tests. The full integration project has 172 test cases because pre-existing integration classes without `Category=Integration` traits are not selected by this filter. |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter FullyQualifiedName~WorkVideoEndpointsTests` | Passed, exit code 0 | 17 integration tests passed for WorkVideo endpoints. Initial red run exposed an invalid empty multipart test request; the test now sends a valid multipart form without a `file` part to exercise endpoint validation. |
| `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter FullyQualifiedName~WorkVideoComponentTests` | Passed, exit code 0 | 8 component tests passed for WorkVideo local storage, cleanup service, reorder handler, and delete handler behavior. |
| `dotnet test backend/WoongBlog.sln` | Passed, exit code 0 | All five backend test projects passed after the WorkVideo update: Contract 1, Unit 14, Component 60, Architecture 30, Integration 182. Restore/build emitted NU1901 warnings for `AWSSDK.Core` 4.0.0.17 low-severity advisory `GHSA-9cvc-h2w8-phrp`. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Component"` | Passed, exit code 0 | Component filter passed: 60 tests. Other projects reported no tests matching the filter. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Integration"` | Passed, exit code 0 | Integration category filter passed: 133 tests. The full integration project has 182 test cases because pre-existing integration classes without `Category=Integration` traits are not selected by this filter. |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter FullyQualifiedName~AdminAiEndpointsTests` | Passed, exit code 0 | 17 integration tests passed for Admin AI endpoints, including new anonymous rejection, invalid batch request, and deterministic active-selection reuse tests. |
| `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter FullyQualifiedName~AiBatchRuntimeComponentTests` | Passed, exit code 0 | 10 component tests passed for AI batch target selection, job persistence, status transitions, partial/full failure persistence, runtime config, and options validation. Initial red run exposed test harness scope resolution; fixed by resolving the scheduler from a DI scope. |
| `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~AiBatchRuntimeComponentTests\|FullyQualifiedName~BlogAiFixServiceCodexRuntimeComponentTests"` | Passed, exit code 0 | 16 component tests passed for AI batch/runtime config plus Codex fake-process env/argument/failure behavior. |
| `dotnet test backend/WoongBlog.sln` | Passed, exit code 0 | All five backend test projects passed after the AI batch/runtime update: Contract 1, Unit 14, Component 72, Architecture 30, Integration 185. Restore/build emitted NU1901 warnings for `AWSSDK.Core` 4.0.0.17 low-severity advisory `GHSA-9cvc-h2w8-phrp`. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Component"` | Passed, exit code 0 | Component filter passed: 72 tests. Other projects reported no tests matching the filter. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Integration"` | Passed, exit code 0 | Integration category filter passed: 136 tests. The full integration project has 185 test cases because pre-existing integration classes without `Category=Integration` traits are not selected by this filter. |
| `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter FullyQualifiedName~DbContextModelContractComponentTests` | Passed, exit code 0 | 4 component tests passed for DbContext model metadata and search-field update behavior. |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter FullyQualifiedName~PostgresPersistenceContractTests` | Passed, exit code 0 | 3 Postgres/Testcontainers integration tests passed for schema/search patching, bootstrap idempotence/runtime preservation, relational NOT NULL/unique violations, and database-side WorkVideo cascade. |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter FullyQualifiedName~StartupCompositionTests` | Passed, exit code 0 | 7 startup composition integration tests passed for DI, handlers, stores/services, hosted services, options binding, health/OpenAPI/runtime-config registration, and CSRF-before-authorization behavior. |
| `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter FullyQualifiedName~StartupOptionsValidationTests` | Passed, exit code 0 | 8 startup options validation tests passed, including invalid AI batch concurrency, missing auth media root, and missing antiforgery header name. |
| `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj --filter FullyQualifiedName~Program_ComposesServicesMiddlewareAndEndpoints_InExpectedBoundaryOrder` | Passed, exit code 0 | 1 architecture test passed for centralized Program service, middleware, and endpoint composition order. |
| `dotnet test backend/WoongBlog.sln` | Passed, exit code 0 | All five backend test projects passed after the persistence/startup update: Contract 1, Unit 14, Component 76, Architecture 31, Integration 194. Restore/build emitted NU1901 warnings for `AWSSDK.Core` 4.0.0.17 low-severity advisory `GHSA-9cvc-h2w8-phrp`. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Architecture"` | Passed, exit code 0 | Architecture filter passed: 31 tests. Other projects reported no tests matching the filter. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Component"` | Passed, exit code 0 | Component filter passed: 76 tests. Other projects reported no tests matching the filter. |
| `dotnet test backend/WoongBlog.sln --filter "Category=Integration"` | Passed, exit code 0 | Integration category filter passed: 160 tests. The full integration project has 194 test cases because some pre-existing integration classes without `Category=Integration` traits are still not selected by this filter. |
| `node -e "JSON.parse(...)"` | Passed, exit code 0 | Machine-readable audit JSON parsed successfully after the persistence/startup update. |
| `git diff --check` | Passed, exit code 0 | No whitespace errors after the persistence/startup update. |

## Final Audit Recommendation

The Priority 1 auth/session/login/logout/CSRF, admin mutation, public read/query, WorkVideo, AI batch/runtime, and persistence/startup slices are materially stronger after these updates and should be kept. Do not treat backend coverage as strict feature-complete yet: remaining risk is concentrated in media upload/delete, exhaustive AI batch endpoint matrices, endpoint-level cookie-session side effects, WorkVideo R2/HLS failure paths, confirm-upload edge cases, remaining Codex timeout/auth/invalid-command paths, OpenAI/Azure fake-HTTP provider behavior, hosted worker lifecycle behavior, optional startup provider/config matrices, and PostgreSQL-backed query/search semantics. The next backend test work should continue with dedicated media tests, remaining AI endpoint matrices, and deeper WorkVideo R2/HLS/R2 failure coverage when a fake object-storage seam is available.
