# Backend Test Rebalancing Inventory

Generated: `2026-04-21`
Branch: `feat/phase-2-backend-discompostion`
Scope: test taxonomy inventory for the test-rebalancing PR.

## Before

| Test project/file | Category before | Reason |
| --- | --- | --- |
| `WoongBlog.Api.ArchitectureTests/ArchitectureBoundaryTests.cs` | architecture | Assembly/source boundary rules. |
| `WoongBlog.Api.ContractTests/ProviderContractVerificationTests.cs` | end-to-end contract | Pact provider verification against a real provider base URL when configured. |
| `WoongBlog.Api.IntegrationTests/*EndpointsTests.cs` | integration / component-style integration | Full ASP.NET test host, HTTP client, auth, DB, file/storage side effects depending on file. |
| `WoongBlog.Api.IntegrationTests/Startup*Tests.cs` | integration | Startup and composition behavior through the test host. |
| `WoongBlog.Api.IntegrationTests/PersistenceContractTests.cs` | integration | EF model/seeding contract with in-memory provider. |
| `WoongBlog.Api.IntegrationTests/PostgresPersistenceContractTests.cs` | integration | PostgreSQL/Testcontainers persistence contract. |
| `WoongBlog.Api.UnitTests/AdminContentTextTests.cs` | unit | Pure support helper behavior. |
| `WoongBlog.Api.UnitTests/GetPageBySlugQueryValidatorTests.cs` | unit | Application validator behavior only. |
| `WoongBlog.Api.UnitTests/RequestValidatorTests.cs` | unit | Application command/query validators only. |
| `WoongBlog.Api.UnitTests/PublicQueryHandlerTests.cs` | component-style integration | Uses Infrastructure stores, `WoongBlogDbContext`, and EF InMemory. |
| `WoongBlog.Api.UnitTests/AuthRecorderTests.cs` | component-style integration | Uses Infrastructure auth, `WoongBlogDbContext`, EF InMemory, and `DefaultHttpContext`. |
| `WoongBlog.Api.UnitTests/AuthRedirectUriResolverTests.cs` | component-style integration | Uses Infrastructure auth options/resolver and ASP.NET `DefaultHttpContext`. |
| `WoongBlog.Api.UnitTests/BlogAiFixServiceCodexRuntimeTests.cs` | component-style integration | Uses Infrastructure AI service, temp filesystem, fake shell process path, and `HttpClient`. |
| `WoongBlog.Api.UnitTests/CodexRuntimeEnvironmentTests.cs` | component-style integration | Uses Infrastructure AI service, temp filesystem, executable fake script, and `HttpClient`. |

## After

| Test project/file | Category after | Notes |
| --- | --- | --- |
| `WoongBlog.Api.ArchitectureTests/ArchitectureBoundaryTests.cs` | architecture | Unchanged; remains the boundary enforcement suite. |
| `WoongBlog.Api.ContractTests/ProviderContractVerificationTests.cs` | end-to-end contract | Unchanged. |
| `WoongBlog.Api.IntegrationTests/*EndpointsTests.cs` | integration / component-style integration | Unchanged; owns endpoint/full-host behavior. |
| `WoongBlog.Api.IntegrationTests/Startup*Tests.cs` | integration | Unchanged; includes composition root/DI sanity. |
| `WoongBlog.Api.IntegrationTests/PersistenceContractTests.cs` | integration | Unchanged. |
| `WoongBlog.Api.IntegrationTests/PostgresPersistenceContractTests.cs` | integration | Unchanged. |
| `WoongBlog.Api.ComponentTests/PublicQueryHandlerComponentTests.cs` | component-style integration | Moved from UnitTests; still uses Infrastructure stores and EF InMemory. |
| `WoongBlog.Api.ComponentTests/AuthRecorderComponentTests.cs` | component-style integration | Moved from UnitTests; still uses Infrastructure auth, EF InMemory, and `DefaultHttpContext`. |
| `WoongBlog.Api.ComponentTests/AuthRedirectUriResolverComponentTests.cs` | component-style integration | Moved from UnitTests; still uses Infrastructure auth and ASP.NET request objects. |
| `WoongBlog.Api.ComponentTests/BlogAiFixServiceCodexRuntimeComponentTests.cs` | component-style integration | Moved from UnitTests; still uses filesystem/process fake and `HttpClient`. |
| `WoongBlog.Api.ComponentTests/CodexRuntimeEnvironmentComponentTests.cs` | component-style integration | Moved from UnitTests; still uses filesystem/process fake and `HttpClient`. |
| `WoongBlog.Api.UnitTests/AdminContentTextTests.cs` | unit | Remains in UnitTests. |
| `WoongBlog.Api.UnitTests/GetPageBySlugQueryValidatorTests.cs` | unit | Remains in UnitTests. |
| `WoongBlog.Api.UnitTests/RequestValidatorTests.cs` | unit | Remains in UnitTests. |

## Representative Flow Coverage

| Flow requested | Coverage after rebalancing |
| --- | --- |
| Content command flow | Existing IntegrationTests cover admin content create/update/delete endpoints; ComponentTests cover public query handlers. |
| Content query flow | `PublicQueryHandlerComponentTests` covers home/site/work/blog query handlers with concrete stores and EF InMemory. |
| AI query or command flow | `AdminAiEndpointsTests` and Codex component tests cover runtime config/batch/Codex service behavior. |
| WorkVideo command or query flow | `WorkVideoEndpointsTests` and WorkVideo-related IntegrationTests cover upload, order, cleanup, and public rendering behavior. |
| Composition root / DI sanity | `StartupCompositionTests` covers representative Api/Application/Infrastructure service resolution. |

## Deferred

- Further split IntegrationTests into endpoint integration versus component-style integration only if the suite becomes hard to run selectively.
- Consolidate overlapping Codex runtime tests later; this PR only moves/relabels them.
- Keep production namespaces and module project structure unchanged.
