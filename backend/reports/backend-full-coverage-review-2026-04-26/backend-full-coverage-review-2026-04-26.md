# Backend Full Coverage Review - 2026-04-26

## Summary

This was an audit-only review of `coverage/backend/full`. Production code and test code were not modified.

The backend test suite is in a good baseline state for endpoint wiring and major happy paths, but the full report should not be read as "safe enough because line coverage is 90%." The useful signal is the branch and failure-path picture: full line coverage is `90.1%`, but branch coverage is `60.9%`. The main gaps are external-service seams, auth/cookie/OIDC event behavior, media/storage failure paths, AI provider failures, validation payload shape, and policy/parser edge cases.

## Coverage Snapshot

| Scope | Line coverage | Branch coverage | Method coverage |
|---|---:|---:|---:|
| Full backend | 90.1% (3,527 / 3,912) | 60.9% (405 / 665) | 93.9% |
| `WoongBlog.Api` | 99.3% | 69.5% | 100.0% |
| `WoongBlog.Application` | 93.7% | 60.2% | 93.5% |
| `WoongBlog.Domain` | 85.7% | 50.0% | 100.0% |
| `WoongBlog.Infrastructure` | 80.3% | 60.8% | 91.6% |

Coverage source files reviewed:

- `coverage/backend/full/report/index.html`
- `coverage/backend/full/report/Summary.json`
- `coverage/backend/full/report/SummaryGithub.md`
- selected per-class HTML files under `coverage/backend/full/report/`

## What Is Strong

- Most API endpoints are executed at least once; `WoongBlog.Api` is `99.3%` line-covered.
- Admin content mutation flows have meaningful integration coverage for create/update/delete and representative auth/CSRF behavior.
- Public page/blog/work/site DTOs, pagination, draft hiding, and projection behavior are well represented by component and integration tests.
- AI batch happy paths, auto-apply, partial failure, and state persistence have meaningful component/integration coverage.
- WorkVideo local upload, confirm, delete, ordering, cleanup scheduling, HLS success, and public projection are covered at integration/component levels.
- Test taxonomy is mostly right: endpoint/EF/auth/storage behavior is not being forced into unit tests.

## What The Number Hides

- `90.1%` line coverage is inflated by thin endpoints, DTOs, module registration, and broad host startup execution.
- Full coverage includes architecture tests and a skipped contract project, so it is not pure behavior coverage.
- Integration tests use `CustomWebApplicationFactory` and `TestAuthHandler`; this proves endpoint authorization outcomes but does not prove production cookie/OIDC event behavior.
- DI/startup tests can mark constructors and registration paths covered without proving runtime failure behavior.
- Branch coverage is the better signal right now. `60.9%` branch coverage shows that many failure, fallback, parsing, and external dependency paths are still thin.

## High-Risk Low-Coverage Areas

| Priority | Area | Coverage evidence | Why it matters | Recommended test level |
|---|---|---:|---|---|
| P1 | AI provider execution | `BlogAiFixService`: 62.8% line, 38.2% branch; `AiHttpResultMapper`: 70.0% line, 40.0% branch | External provider errors, malformed JSON, empty choices, model/provider fallback, and HTTP result mapping can break admin AI workflows. | Component + Integration |
| P1 | WorkVideo R2/object storage and storage selection | `R2VideoStorageService`: 9.0% line, 0.0% branch; `WorkVideoStorageSelector`: 78.2% line, 30.0% branch | Production video storage can fail differently from local storage; presigned upload, metadata, object reads, HLS prefix deletes, and fallback selection are under-tested. | Component + Integration |
| P1 | Auth/OIDC/cookie events | `AppOpenIdConnectEvents`: 0.0% line; `AppCookieAuthenticationEvents`: 11.1% line, 0.0% branch | Test auth covers endpoint outcomes, but production login callbacks, claim enrichment, non-admin revocation, remote failure, and API/browser redirect differences remain exposed. | Component + Integration |
| P1 | Validation exception contract | `ValidationExceptionFilter`: 0.0% line, 0.0% branch | A thrown FluentValidation exception currently has no direct test for stable `ValidationProblemDetails` shape or non-validation pass-through. | Unit or Component |
| P2 | WorkVideo policy and HLS failure paths | `WorkVideoPolicy`: 52.7% line, 31.6% branch; `FfmpegVideoTranscoder`: 61.7% line, 50.0% branch | Upload validation, YouTube parsing, MP4 sniffing, ffmpeg/ffprobe failures, timeouts, missing manifest, and preview generation branches are thin. | Unit + Component |
| P2 | Proxy/security configuration | `ForwardedHeadersOptionsFactory`: 55.1% line, 43.7% branch; `SecurityOptionsValidator`: 50.0% line, 64.2% branch | Trusted proxy parsing and security option validation affect production request origin/security behavior. | Unit + Integration |
| P2 | Content/search helpers | `ContentSearchText`: 60.0% line, 60.0% branch; `AdminContentJson`: 91.9% line, 60.7% branch; `AdminContentText`: 89.4% line, 50.0% branch | Search is user-visible and multilingual; helper branches are cheap to lock down with table-driven unit tests. | Unit |
| P2 | Media upload failure mapping | `UploadAssetEndpoint`: 94.1% line, 25.0% branch | Success is covered, but policy failure and storage failure status mapping are thin. | Integration |
| P3 | Design-time and low-runtime helpers | `WoongBlogDbContextDesignTimeFactory`: 0.0% line, 0.0% branch | Low production runtime risk; test only if migrations/design-time behavior becomes a recurring issue. | Component/manual |

## Recommended Reinforcement Plan

### 1. AI Provider Failure Matrix

Target files/classes:

- `BlogAiFixService`
- `AiHttpResultMapper`
- `AiRuntimePolicy`
- runtime config handler/provider branches

Add tests:

- Fake `HttpMessageHandler` component tests for OpenAI success and Azure success.
- Non-2xx OpenAI/Azure payloads preserve useful error messages.
- Empty `choices`, malformed JSON, missing `message.content`, and markdown-fenced HTML cleanup.
- Missing API key/config fails clearly.
- Environment variable model/deployment/API-version fallback.
- Codex timeout, invalid command/start failure, unauthenticated home, and image artifact resolution limits.
- Endpoint or mapper tests for `NotFound`, `Conflict`, and fallback `500`.

Acceptance criteria:

- AI provider branches are covered without real network calls.
- Failure payloads are stable enough for operators/admin UI to diagnose.
- Runtime config reports the correct default provider/model for OpenAI, Azure, and Codex variants.

### 2. WorkVideo R2, Storage Selection, And HLS Failures

Target files/classes:

- `R2VideoStorageService`
- `WorkVideoStorageSelector`
- `WorkVideoPolicy`
- `FfmpegVideoTranscoder`
- WorkVideo confirm/delete/cleanup flows

Add tests:

- R2 unconfigured returns no playback URL and configured mode builds playback URL.
- Presigned upload target uses browser endpoint when configured.
- Object metadata NotFound returns null.
- Read prefix, single object delete, and HLS manifest-prefix delete behavior through a fake S3 seam or wrapper.
- Selector chooses R2 only when configured/forced appropriately and falls back to local otherwise.
- Confirm failure for missing object, wrong size, wrong MIME, invalid MP4 bytes, stale version, and repeated confirm.
- HLS start failure, non-zero stderr, timeout, missing manifest, invalid ffprobe duration, and preview VTT boundary behavior.
- `WorkVideoPolicy` table tests for file size, extension, MIME, MP4 prefix, YouTube watch/embed/shorts/youtu.be, malformed IDs, and foreign hosts.

Acceptance criteria:

- R2 behavior is covered without Cloudflare calls.
- R2 and local storage selection is explicit.
- WorkVideo failure paths assert DB/session/cleanup side effects, not only response codes.

### 3. Production Auth/OIDC/Cookie Behavior

Target files/classes:

- `AppOpenIdConnectEvents`
- `AppCookieAuthenticationEvents`
- `AuthOptionsValidator`
- `AuthServiceCollectionExtensions`

Add tests:

- OIDC token validation for admin: profile/session claims are added, expiry/refresh properties set.
- OIDC token validation for non-admin: denied audit is recorded, session is revoked, response redirects to `admin_only`, and response is handled.
- OIDC remote failure records login failure and redirects to `auth_failed`.
- Redirect-to-provider uses configured public origin callback.
- Cookie redirect to login returns `401` for `/api` and browser redirect otherwise.
- Access denied returns `403` for `/api` and browser redirect otherwise.
- Invalid/stale principal rejects and signs out.
- Startup/options tests for missing credentials, disabled auth, and production scheme configuration that are not masked by `TestAuthHandler`.

Acceptance criteria:

- Production auth events are tested directly or through a host that does not replace the relevant event type.
- API and browser paths are asserted separately.

### 4. Proxy/Security/Validation Contracts

Target files/classes:

- `ValidationExceptionFilter`
- `ForwardedHeadersOptionsFactory`
- `ProxyOptionsValidator`
- `SecurityOptionsValidator`
- `SecurityHeadersMiddleware`

Add tests:

- Validation exception filter groups duplicate failures by property and returns `ValidationProblemDetails`.
- Non-validation exception passes through unhandled.
- Forwarded header options default to loopback when no proxies/networks are configured.
- Known proxy and CIDR parsing valid/invalid cases.
- Security options validator invalid CSP/header/HSTS combinations.
- Representative `/api` and `/media` responses keep expected headers.

Acceptance criteria:

- Error payload shape is pinned.
- Proxy/security config failures are caught before deployment.

### 5. Media Upload And Search Helpers

Target files/classes:

- `UploadAssetEndpoint`
- `MediaAssetUploadPolicy`
- `ContentSearchText`
- `AdminContentJson`
- `AdminContentText`

Add tests:

- Upload endpoint returns `400` for policy failure and `500` for storage failure.
- Storage failure does not leave orphan DB rows/files.
- MIME/extension mismatch, path traversal filename, size limits, and bucket/kind boundaries.
- Search normalization for Korean/English, punctuation, whitespace, HTML/JSON content, blank query, null/empty values, and multiple values.
- Malformed JSON, non-object JSON, missing `html`/`markdown`, wrapped markdown, and plain text extraction.

Acceptance criteria:

- Search helpers are stable through realistic multilingual content.
- Upload failures assert response and side effects.

## What Not To Chase

- Do not try to raise `UnitTests` coverage just for the number. The repo intentionally keeps unit tests for pure logic.
- Do not unit-test EF query behavior, auth/CSRF middleware, routing, or storage side effects. Keep those in component/integration tests.
- Do not add a strict high coverage threshold yet. If CI needs a guardrail, use only a conservative anti-regression floor, for example full line coverage around `88%` and branch coverage around `58%`, then ratchet after the P1/P2 work lands.
- Do not treat `WoongBlog.Api` at `99.3%` as endpoint completeness. It mostly says the endpoints are hit, not that every status/failure branch is proved.

## Subagent Review Inputs

Three subagents were used:

- Infrastructure/external seam review: identified R2, HLS, storage selector, auth events, proxy/security, validation filter, and AI provider gaps.
- API/Application review: identified `AiHttpResultMapper`, `UploadAssetEndpoint`, `WorkVideoPolicy`, content/search helpers, AI runtime policy, and batch edge gaps.
- Executive prioritization review: confirmed the current suite is directionally healthy but recommended prioritizing branch/failure paths over line chasing.

Their findings agreed with the local `Summary.json`/HTML parsing: branch coverage and external/failure paths are the current bottleneck.

## Validation Performed

| Check | Result |
|---|---|
| Plugin cache absolute paths | Verified. |
| `npx skills find dotnet coverage test audit` | Completed; no external skill installed due low install counts. |
| `coverage/backend/full/report/Summary.json` parse | Passed; metrics extracted. |
| Per-class HTML inspection | Completed for high-risk classes listed above. |
| Subagent review | Completed with three independent slices. |

## Risks And Caveats

- This review did not rerun coverage; it audits the existing `coverage/backend/full` artifacts.
- Existing uncommitted coverage-tooling changes from the previous task are present and were intentionally not touched.
- Some source classes not visible as separate classes in `Summary.json` may still need behavior review before assuming coverage.
- Coverage line percentages can move when generated-code/exclusion rules change; the recommendations above are behavior-oriented and should survive percentage drift.

## Final Recommendation

Treat the current suite as a solid baseline, not a finish line. Implement coverage reinforcement in this order:

1. AI provider failure matrix.
2. WorkVideo R2/storage/HLS failure paths.
3. Production auth/OIDC/cookie event behavior.
4. Validation/proxy/security contracts.
5. Media upload failure mapping and content/search helper edge cases.

Do not add a high coverage threshold until the P1 and most P2 targets are covered. Use branch coverage and named behavior gaps as the planning metric.

