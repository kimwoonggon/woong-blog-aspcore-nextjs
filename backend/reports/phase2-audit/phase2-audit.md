# Phase 2 Backend Physical Split Audit

Generated: `2026-04-21T00:27:49+09:00`
Branch: `feat/phase-2-backend-discompostion`
Commit SHA: `36c6fdfafd9600039bf2561070f73878e7220f3f`
Audit mode: current working tree inspection, no production/test/config changes made by this audit.

## Executive Summary

The current repository matches the intended Phase 2 physical split at the project-reference and source-boundary level.

No high-risk architectural violations were found. The production project graph is acyclic and follows the intended direction:

`Api -> Application + Infrastructure -> Application + Domain -> Domain`

Application has no direct Infrastructure project reference, no Infrastructure namespace imports, no ASP.NET HTTP result usage, and no service-locator/scope usage. Domain is dependency-free apart from framework/base runtime dependencies and contains only entity/value/constant style code. DbContext and concrete store implementations live in Infrastructure. HTTP result mapping remains in Api.

There are several yellow-flag cleanup items worth addressing after stabilization, mostly around composition polish and naming clarity rather than broken architecture.

Recommendation: **ready for stabilization PR**.

## PASS/FAIL Checklist

| Criterion | Status | Evidence |
| --- | --- | --- |
| Domain has no project references | PASS | `backend/src/WoongBlog.Domain/WoongBlog.Domain.csproj` has no `ProjectReference`. |
| Application references Domain only | PASS | `backend/src/WoongBlog.Application/WoongBlog.Application.csproj` references only `../WoongBlog.Domain/WoongBlog.Domain.csproj`. |
| Infrastructure references Application + Domain only | PASS | `backend/src/WoongBlog.Infrastructure/WoongBlog.Infrastructure.csproj` references Application and Domain only. |
| Api references Application + Infrastructure | PASS | `backend/src/WoongBlog.Api/WoongBlog.Api.csproj` references Application and Infrastructure only, no direct Domain reference. |
| No circular references | PASS | The project graph is one-way: Domain has no refs, Application -> Domain, Infrastructure -> Application/Domain, Api -> Application/Infrastructure. |
| No Application -> Infrastructure dependency | PASS | No `ProjectReference`; `rg` found no `WoongBlog.Api.Infrastructure` or `WoongBlog.Infrastructure` references under `backend/src/WoongBlog.Application`. |
| No Domain -> Application/Infrastructure dependency | PASS | `rg` found no higher-layer or framework references under `backend/src/WoongBlog.Domain`. |
| Application does not expose ASP.NET HTTP result types | PASS | `rg` found no `IResult`, `Results.`, or `StatusCodes.` in `backend/src/WoongBlog.Application`. |
| Application does not use `IServiceScopeFactory` / `CreateScope` / `GetRequiredService` | PASS | `rg` found no scope/service-locator tokens in Application. |
| Api owns HTTP result mapping | PASS | `AiHttpResultMapper` and `WorkVideoEndpoints.ToResult` are in Api; Application result types are HTTP-agnostic. Infrastructure middleware/auth events set response codes but do not map application results. |
| DbContext and store implementations live in Infrastructure | PASS | `WoongBlogDbContext` and all concrete `*Store` implementations are under `backend/src/WoongBlog.Infrastructure`. |
| Handlers and abstractions live in Application | PASS | 45 `*Handler.cs` files and 15 store/port abstraction files are under `backend/src/WoongBlog.Application`. |
| Domain only contains pure domain-owned code | PASS | Domain source contains entity and domain-constant files only; no ASP.NET, EF, MediatR, or higher-layer refs. |
| `WorkVideoResult<T>` and `AiActionResult<T>` remain HTTP-agnostic | PASS | Both are in Application and use domain/application enum statuses, not HTTP types/status codes. |
| Current Content CQRS direction remains intact | PASS | Content handlers are in Application, stores are interfaces in Application and concrete persistence implementations in Infrastructure. |
| Current AI batch boundary direction remains intact | PASS | AI handlers/contracts/policies are in Application; provider, signal/background host, dispatcher, and store implementation are in Infrastructure; HTTP mapping is in Api. |
| Current WorkVideo handler/store/support direction remains intact | PASS | WorkVideo handlers/ports/results are in Application; storage/transcoder/HLS/cleanup implementations and stores are in Infrastructure; endpoint result mapping is in Api. |

## Actual Project Reference Graph

```text
WoongBlog.Domain
  (no project references)

WoongBlog.Application
  -> WoongBlog.Domain

WoongBlog.Infrastructure
  -> WoongBlog.Application
  -> WoongBlog.Domain

WoongBlog.Api
  -> WoongBlog.Application
  -> WoongBlog.Infrastructure
```

## Important Package Reference Distribution

| Project | Important package/framework references |
| --- | --- |
| `WoongBlog.Domain` | No package references. |
| `WoongBlog.Application` | `FluentValidation`, `MediatR`, `Microsoft.Extensions.Options`. |
| `WoongBlog.Infrastructure` | `FrameworkReference Microsoft.AspNetCore.App`, `AWSSDK.S3`, `Microsoft.AspNetCore.Authentication.OpenIdConnect`, `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.InMemory`, `Npgsql.EntityFrameworkCore.PostgreSQL`. |
| `WoongBlog.Api` | Web SDK, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `MediatR`, `Microsoft.AspNetCore.Authentication.OpenIdConnect`, `Microsoft.AspNetCore.OpenApi`. |
| `WoongBlog.Api.ArchitectureTests` | References all four production projects and includes `NetArchTest.Rules`, xUnit v3, `SonarAnalyzer.CSharp`. |
| `WoongBlog.Api.IntegrationTests` | References all four production projects; includes `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers, xUnit v3, `SonarAnalyzer.CSharp`. |
| `WoongBlog.Api.UnitTests` | References Application, Domain, Infrastructure; has `FrameworkReference Microsoft.AspNetCore.App` for HTTP/auth test helpers; no Api project reference. |
| `WoongBlog.Api.ContractTests` | No production project references; includes Pact.NET packages. |

## High-Risk Violations

No high-risk Phase 2 boundary violations were found.

| Severity | File | Finding |
| --- | --- | --- |
| None | N/A | No hard violations found by project graph inspection or forbidden dependency `rg` checks. |

## Yellow-Flag Items

| Area | File(s) | Why it matters | Suggested cleanup |
| --- | --- | --- | --- |
| Namespace clarity | Project-wide under `backend/src/WoongBlog.Application`, `WoongBlog.Domain`, `WoongBlog.Infrastructure` | Namespaces mostly remain `WoongBlog.Api.*`, even in non-Api assemblies. This is intentional minimal churn, but it makes ownership and grep-based dependency checks harder to read. | Later namespace normalization or a documented convention. |
| Application registration composition | `backend/src/WoongBlog.Api/Common/ApiServiceCollectionExtensions.cs` | Api currently scans Application assemblies and registers `ValidationBehavior`. This is valid composition-root behavior, but a dedicated `AddApplication()` would make the physical boundary clearer. | Add `AddApplication()` in Application later, keep Api as caller. |
| Infrastructure depends on Application content support from DbContext | `backend/src/WoongBlog.Infrastructure/Infrastructure/Persistence/WoongBlogDbContext.cs` | DbContext calls `ContentSearchText` and `AdminContentJson` from Application support. This is legal by reference direction, but persistence is coupled to app support helpers. | Move these low-level text/index helpers to Domain or Infrastructure support if they become persistence-owned. |
| AI options placement | `backend/src/WoongBlog.Application/Modules/AI/Application/AiOptions.cs` | `AiOptions` includes provider secrets/runtime settings such as API keys, Codex command, and Codex home. This avoids Application -> Infrastructure but keeps infrastructure-shaped config in Application. | Split into Application runtime policy options and Infrastructure provider options later. |
| Infrastructure contains ASP.NET middleware/auth status handling | `backend/src/WoongBlog.Infrastructure/Infrastructure/Security/AntiforgeryValidationMiddleware.cs`, `backend/src/WoongBlog.Infrastructure/Infrastructure/Auth/AppCookieAuthenticationEvents.cs`, `backend/src/WoongBlog.Infrastructure/Infrastructure/Auth/AuthRuntimeExtensions.cs`, `backend/src/WoongBlog.Infrastructure/Infrastructure/Security/SecurityHeadersMiddleware.cs` | Not an Application leak and not endpoint result mapping, but Infrastructure is not purely non-HTTP. | Accept for now as host infrastructure; revisit only if an Api-only HTTP-boundary split is desired. |
| EF migrations absent | `backend/src/WoongBlog.Infrastructure/Infrastructure/Persistence/WoongBlogDbContextDesignTimeFactory.cs` | Design-time factory and migrations assembly are configured, but there is no migrations folder; current behavior still relies on bootstrap/schema patches. | No action unless EF migrations become part of the workflow. |
| README stale paths | `README.md` | Some README references still point at old single-project paths such as `backend/src/WoongBlog.Api/Infrastructure/Persistence/...` and old test project names. | Update docs after the physical split stabilizes. |
| Unit test layer reaches Infrastructure | `backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj` | UnitTests directly reference Infrastructure because some tests instantiate concrete stores/auth/AI adapters. This is acceptable for current coverage but blurs unit vs integration taxonomy. | Gradually move concrete infrastructure tests to IntegrationTests or rename as component tests. |

## Specific Repo Checks

| Check | Result |
| --- | --- |
| Search direct Infrastructure references from Application | PASS: no matches for `WoongBlog.Api.Infrastructure` or `WoongBlog.Infrastructure`. |
| Search ASP.NET HTTP abstractions from Application | PASS: no `Microsoft.AspNetCore`, `IResult`, `StatusCodes`, `Results.`, `HttpContext`, or `IFormFile` hits. Only the substring `Endpoint` appears in `AzureOpenAiEndpoint`, which is not an ASP.NET endpoint abstraction. |
| Search service locator/scope usage outside Infrastructure | PASS: no `IServiceScopeFactory`, `CreateScope`, `CreateAsyncScope`, or `GetRequiredService` hits outside Infrastructure. |
| Search DbContext usage outside Infrastructure | PASS: no `WoongBlogDbContext`, `DbContext`, or `Microsoft.EntityFrameworkCore` hits outside Infrastructure. |
| Verify Program.cs composition | PASS: `Program.cs` delegates to module/common registration extensions and maps endpoint modules; it does not contain DbContext/store/provider implementation logic. |
| Verify tests target correct layers | PASS: Architecture and Integration tests reference all four projects; Unit tests reference Application/Domain/Infrastructure and not Api; Contract tests reference no production project. |
| Verify Docker/backend build entry point | PASS: `backend/Dockerfile` copies all four backend projects before `dotnet restore ../../WoongBlog.sln` and publishes `WoongBlog.Api`. |

## Recommended Next Step

**ready for stabilization PR**

Proceed with stabilization and review. Do not do another large physical split pass unless you want to clean yellow-flag polish items before review. The highest-value follow-up cleanup is to add an `AddApplication()` registration extension and decide whether `AiOptions` should be split between Application runtime policy and Infrastructure provider configuration.
