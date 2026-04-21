# Phase 2 Stabilization Architecture Audit

Generated: `2026-04-21T10:37:19+09:00`
Branch: `feat/phase-2-backend-discompostion`
Commit SHA: `066c48c1ed04d096d6e2a2cd9322c4b967c6aed1`
Audit mode: source/project/test inspection only. No production code, test code, project files, Dockerfiles, CI files, or configuration files were modified by this audit.

## 1. Executive Summary

Overall status: **PASS with yellow flags**

High-risk violations: **0**

Yellow flags: **8**

Recommendation: **ready for test-rebalancing PR**

The backend physical split remains aligned with the intended Phase 2 project graph. `WoongBlog.Domain` has no project references, `WoongBlog.Application` references only Domain, `WoongBlog.Infrastructure` references Application and Domain, and `WoongBlog.Api` references Application and Infrastructure. No circular references were found from project files.

The stabilization work preserved the main architecture boundaries. Application has no direct Infrastructure project reference or Infrastructure namespace usage, Domain does not reference Application/Infrastructure, Application does not expose ASP.NET HTTP result abstractions, and Application does not use `IServiceScopeFactory`, `CreateScope`, or `GetRequiredService`. HTTP result mapping for AI and WorkVideo flows remains in Api. DbContext and concrete store implementations live in Infrastructure. Handlers and primary application abstractions live in Application.

The remaining items are not blockers for the next phase. They are mostly naming/composition/test-taxonomy cleanup: stale `WoongBlog.Api.*` namespaces in split assemblies, Api module service extensions still registering concrete Infrastructure adapters, Infrastructure still depending on Application content support helpers for search synchronization, Infrastructure-shaped AI options in Application, and unit test project contents that behave more like component/infrastructure tests.

## 2. PASS/FAIL Checklist

| Criterion | Status | Evidence |
| --- | --- | --- |
| Domain is dependency-free apart from runtime framework basics | PASS | `backend/src/WoongBlog.Domain/WoongBlog.Domain.csproj` has no `PackageReference`, `FrameworkReference`, or `ProjectReference`. Source search found no ASP.NET, EF Core, MediatR, Application, or Infrastructure references. |
| Application depends on Domain only | PASS | `backend/src/WoongBlog.Application/WoongBlog.Application.csproj` has a single project reference: `../WoongBlog.Domain/WoongBlog.Domain.csproj`. |
| Infrastructure depends on Application + Domain only | PASS | `backend/src/WoongBlog.Infrastructure/WoongBlog.Infrastructure.csproj` references only Application and Domain. |
| Api depends on Application + Infrastructure | PASS | `backend/src/WoongBlog.Api/WoongBlog.Api.csproj` references Application and Infrastructure, with no direct Domain project reference. |
| No circular references | PASS | Actual project graph is acyclic: Domain -> none; Application -> Domain; Infrastructure -> Application + Domain; Api -> Application + Infrastructure. |
| No Application -> Infrastructure dependency | PASS | Project graph has no such reference. Source search under `backend/src/WoongBlog.Application` found no `WoongBlog.Api.Infrastructure`, `WoongBlog.Infrastructure`, or `Infrastructure.` matches. |
| No Domain -> Application/Infrastructure dependency | PASS | Source search under `backend/src/WoongBlog.Domain` found no Application/Infrastructure namespace or project-layer references. |
| Application must not expose ASP.NET HTTP result abstractions | PASS | Source search under `backend/src/WoongBlog.Application` found no `Microsoft.AspNetCore`, `IResult`, `TypedResults`, `Results.`, `StatusCodes.`, `HttpContext`, or `IFormFile`. Architecture tests also inspect reflected type surfaces. |
| Application must not use `IServiceScopeFactory` / `CreateScope` / `GetRequiredService` | PASS | Source search under `backend/src/WoongBlog.Application` found no `IServiceScopeFactory`, `CreateScope`, `CreateAsyncScope`, or `GetRequiredService`. |
| Api owns HTTP result mapping | PASS | AI HTTP mapping is in `backend/src/WoongBlog.Api/Modules/AI/Api/AiHttpResultMapper.cs`. WorkVideo HTTP mapping is in `backend/src/WoongBlog.Api/Modules/Content/Works/Api/WorkVideos/WorkVideoEndpoints.cs`. |
| DbContext and concrete store implementations live in Infrastructure | PASS | `WoongBlogDbContext` and concrete `*Store` implementations are under `backend/src/WoongBlog.Infrastructure`. Production source search found no DbContext usage outside Infrastructure. |
| Handlers and abstraction interfaces live in Application | PASS with yellow flag | MediatR `*Handler` classes and store/port abstractions live under `backend/src/WoongBlog.Application`. One framework-facing identity interaction interface remains under Infrastructure with an `Application` namespace segment; this is a naming/classification yellow flag, not a direction violation. |
| `AiActionResult<T>` remains HTTP-agnostic | PASS | `backend/src/WoongBlog.Application/Modules/AI/Application/AiActionResult.cs` uses application enum statuses only. HTTP mapping occurs in Api. |
| `WorkVideoResult<T>` remains HTTP-agnostic | PASS | `backend/src/WoongBlog.Application/Modules/Content/Works/Application/WorkVideos/WorkVideoDto.cs` uses `WorkVideoResultStatus` only. HTTP mapping occurs in Api. |
| Content CQRS direction remains intact | PASS | Content handlers and store interfaces are in Application. Concrete persistence stores are in Infrastructure. Api endpoints map requests and call MediatR. |
| AI batch boundary remains intact | PASS | AI commands/queries/policies/contracts are in Application; provider implementation, hosted processor, dispatcher, signal, and batch store implementation are in Infrastructure; endpoint/result mapping is in Api. |
| WorkVideo handler/store/support boundary remains intact | PASS | WorkVideo handlers, result types, policy, and ports are in Application. Storage selector, HLS workspace/publisher, transcoder, cleanup worker/service, and concrete stores are in Infrastructure. Endpoint mapping is in Api. |
| Cleaner composition boundaries exist | PASS with yellow flag | `AddApplication()` exists in Application and `AddInfrastructure()` exists in Infrastructure. Api module service extensions still register many concrete Infrastructure adapters directly. |
| EF design-time/runtime setup remains viable | PASS with yellow flag | `WoongBlogDbContext` and `WoongBlogDbContextDesignTimeFactory` are in Infrastructure, with Npgsql migrations assembly pointed at Infrastructure. No migrations folder exists; runtime still relies on bootstrap/schema patches. |
| Active docs were updated for split paths | PASS | README and quality verification matrix now reference split backend projects and `backend/WoongBlog.sln`. |
| Test taxonomy supports next test-rebalancing phase | PASS with yellow flag | Test projects are physically split, but `WoongBlog.Api.UnitTests` still references Infrastructure and ASP.NET and contains component-style tests. |

## 3. Project Reference Graph

Actual graph discovered from production `.csproj` files:

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

Relevant production package/framework distribution:

| Project | Package/framework references |
| --- | --- |
| `WoongBlog.Domain` | none |
| `WoongBlog.Application` | `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `MediatR`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Options` |
| `WoongBlog.Infrastructure` | `FrameworkReference Microsoft.AspNetCore.App`, `AWSSDK.S3`, `Microsoft.AspNetCore.Authentication.OpenIdConnect`, `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.InMemory`, `Npgsql.EntityFrameworkCore.PostgreSQL` |
| `WoongBlog.Api` | Web SDK, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `MediatR`, `Microsoft.AspNetCore.Authentication.OpenIdConnect`, `Microsoft.AspNetCore.OpenApi` |

Test project reference distribution:

| Test project | Production references | Notes |
| --- | --- | --- |
| `WoongBlog.Api.ArchitectureTests` | Api, Application, Domain, Infrastructure | Owns assembly/source dependency rules. |
| `WoongBlog.Api.IntegrationTests` | Api, Application, Domain, Infrastructure | Owns WebApplicationFactory, endpoint, auth, EF, storage, and Testcontainers coverage. |
| `WoongBlog.Api.UnitTests` | Application, Domain, Infrastructure + ASP.NET framework reference | Contains true unit tests plus component/infrastructure-style tests. |
| `WoongBlog.Api.ContractTests` | none | Pact provider verification shell; no production project references. |

## 4. High-Risk Violations

| Severity | File path(s) | Finding | Why it matters |
| --- | --- | --- | --- |
| None | N/A | No high-risk Phase 2 boundary violations found. | The intended physical split and stabilization boundaries remain intact. |

## 5. Yellow-Flag Items

| Area | File path(s) | Why it matters | Suggested cleanup |
| --- | --- | --- | --- |
| Namespace clarity | Broadly under `backend/src/WoongBlog.Application`, `backend/src/WoongBlog.Domain`, and `backend/src/WoongBlog.Infrastructure` | Many split assemblies still use `WoongBlog.Api.*` namespaces. This is not a project-reference violation, but it weakens grep/readability and can obscure ownership. | Defer namespace normalization to a dedicated low-risk PR after test rebalancing or explicitly document the convention. |
| Api composition knows concrete Infrastructure adapters | `backend/src/WoongBlog.Api/Modules/*/*ServiceCollectionExtensions.cs` | Module service registration extensions in Api import and register concrete stores/storage/providers from Infrastructure. This is acceptable for a composition root, but Api still knows many concrete implementation types. | In a later stabilization pass, consider moving module infrastructure registrations behind Infrastructure-owned module registration helpers while keeping Api as the caller. |
| DbContext search synchronization depends on Application support helpers | `backend/src/WoongBlog.Infrastructure/Infrastructure/Persistence/ContentSearchFieldSynchronizer.cs` | Infrastructure legally references Application, but persistence still uses Application content helpers (`ContentSearchText`, `AdminContentJson`) during SaveChanges. | Defer until ownership is clearer. If search normalization becomes persistence-owned, move the helper logic to Domain or Infrastructure support with focused tests. |
| Infrastructure-shaped AI options live in Application | `backend/src/WoongBlog.Application/Modules/AI/Application/AiOptions.cs` | `AiOptions` includes provider/runtime details such as API keys, Codex command, Codex home, and deployment-specific settings. This keeps Application independent from Infrastructure but blurs option ownership. | Later split Application runtime policy options from Infrastructure provider options. |
| Infrastructure includes ASP.NET host/middleware concerns | `backend/src/WoongBlog.Infrastructure/Infrastructure/Auth/*`, `backend/src/WoongBlog.Infrastructure/Infrastructure/Security/*`, `backend/src/WoongBlog.Infrastructure/Infrastructure/Validation/ValidationExceptionFilter.cs` | Infrastructure has ASP.NET framework dependencies and HTTP middleware/event logic. This is acceptable as host infrastructure, but not a pure non-HTTP infrastructure layer. | Defer unless a stricter Api-only HTTP boundary is desired. |
| EF migration workflow remains bootstrap/schema-patch based | `backend/src/WoongBlog.Infrastructure/Infrastructure/Persistence/WoongBlogDbContextDesignTimeFactory.cs`, `backend/src/WoongBlog.Infrastructure/Infrastructure/Persistence/DatabaseBootstrapper.cs` | Design-time factory exists and points migrations to Infrastructure, but no checked-in EF migrations folder is present. Runtime relies on `EnsureCreatedAsync` plus bootstrap/schema patches. | No immediate blocker. Decide separately whether EF migrations should become the deployment workflow. |
| Unit test project includes component/infrastructure tests | `backend/tests/WoongBlog.Api.UnitTests/PublicQueryHandlerTests.cs`, `AuthRecorderTests.cs`, `BlogAiFixServiceCodexRuntimeTests.cs`, `CodexRuntimeEnvironmentTests.cs`, `AuthRedirectUriResolverTests.cs` | These tests use Infrastructure types, EF InMemory, ASP.NET `DefaultHttpContext`, filesystem/process fakes, or `HttpClient`. They are valuable but not pure unit tests. | Next PR should rebalance or relabel these into component/integration-oriented coverage. |
| Identity infrastructure namespace/path classification | `backend/src/WoongBlog.Infrastructure/Modules/Identity/Application/IIdentityInteractionService.cs`, `IdentityInteractionService.cs` | These live in Infrastructure but use an `Application` namespace segment. They wrap ASP.NET authentication and are not core Application handlers, but naming can confuse boundary scans. | Rename/repath to Infrastructure identity interaction naming in a later low-risk cleanup. |

## 6. Follow-Up Recommendation

The repository is **ready for the next test-rebalancing PR**.

No additional stabilization pass is required before moving to test rebalancing. The high-risk project and source boundaries are correct. The most valuable next PR should focus on test taxonomy:

- Move or relabel `WoongBlog.Api.UnitTests` tests that instantiate Infrastructure, EF InMemory, ASP.NET contexts, filesystem/process fakes, or `HttpClient`.
- Decide whether to introduce a separate component test project or move those tests into IntegrationTests with narrower traits.
- Keep ArchitectureTests focused on dependency/source boundary enforcement and avoid expanding them into behavioral integration assertions.

One optional stabilization cleanup can happen before or during test rebalancing if desired: move module concrete Infrastructure registration behind Infrastructure-owned registration methods so Api module service extensions stop importing concrete store/storage/provider types. This is not blocking.

## 7. Deferred Items

- Namespace normalization from `WoongBlog.Api.*` to assembly-aligned namespaces.
- Moving `ContentSearchText` and `AdminContentJson` out of Application support if persistence/search indexing becomes their main owner.
- Splitting AI configuration into Application runtime policy options and Infrastructure provider options.
- Deciding whether ASP.NET middleware/auth/validation filters should remain in Infrastructure or move closer to Api.
- Introducing an EF migrations workflow, if the project later chooses migrations over bootstrap/schema patches.
- Per-module physical splits.
- Broad handler/store redesign.
- Reclassifying component-style tests currently under `WoongBlog.Api.UnitTests`.
