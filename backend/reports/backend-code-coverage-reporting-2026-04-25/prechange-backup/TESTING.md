# Backend Testing

Backend tests live under `backend/tests` and use `Category` traits so local and CI runs can select the right cost level.

## Categories

| Category | Project | Meaning | When to run |
| --- | --- | --- | --- |
| `Unit` | `WoongBlog.Api.UnitTests` | Pure Application/support helper and validator tests. No ASP.NET host, EF provider, filesystem, process, network, or Infrastructure dependency. | Run during normal backend TDD loops and before every backend commit. |
| `Component` | `WoongBlog.Api.ComponentTests` | Application plus Infrastructure behavior using EF InMemory, fakes, temp filesystem, fake process seams, or direct service/store composition. No full ASP.NET test host. | Run after touching handlers, stores, storage services, AI runtime helpers, or persistence-adjacent code that does not need HTTP hosting. |
| `Architecture` | `WoongBlog.Api.ArchitectureTests` | Project reference, layer boundary, source-token, and composition-order rules. | Run after moving types, changing project references, adding dependencies, or changing startup/module composition. |
| `Integration` | `WoongBlog.Api.IntegrationTests` | Full ASP.NET `WebApplicationFactory` endpoint/startup behavior plus persistence integration contracts. | Run before backend PRs and any change affecting routes, auth, middleware, DI, persistence, uploads, or startup options. Requires Docker for Testcontainers-backed PostgreSQL tests. |
| `Contract` | `WoongBlog.Api.ContractTests` | Pact provider verification against pact files and a running provider URL. | Run when consumer contracts change or before release checks with pact files available. The test is skipped unless contract inputs are configured. |

## Local Commands

Use the runner scripts from the repository root:

```bash
./scripts/run-unit-tests.sh
./scripts/run-component-tests.sh
./scripts/run-architecture-tests.sh
./scripts/run-integration-tests.sh
./scripts/run-backend-tests.sh
```

Equivalent `dotnet` commands:

```bash
dotnet test backend/WoongBlog.sln --filter "Category=Unit"
dotnet test backend/WoongBlog.sln --filter "Category=Component"
dotnet test backend/WoongBlog.sln --filter "Category=Architecture"
dotnet test backend/WoongBlog.sln --filter "Category=Integration"
dotnet test backend/WoongBlog.sln
```

For a fast local backend loop, run `Unit`, then `Component`, then `Architecture`. Run `Integration` before pushing backend changes that affect HTTP, startup, auth, persistence, uploads, or database behavior. Run the full backend suite before release checks or broad refactors.

## CI Commands

CI should keep the category jobs separate so quick failures are visible and independent jobs can run in parallel:

```bash
./scripts/run-unit-tests.sh --blame-hang --blame-hang-timeout 5m -v minimal
./scripts/run-component-tests.sh --blame-hang --blame-hang-timeout 5m -v minimal
./scripts/run-architecture-tests.sh --blame-hang --blame-hang-timeout 5m -v minimal
docker version
./scripts/run-integration-tests.sh --blame-hang --blame-hang-timeout 5m -v minimal
```

The full command remains useful as a release gate:

```bash
./scripts/run-backend-tests.sh --blame-hang --blame-hang-timeout 5m -v minimal
```

## External Services

`Unit`, `Component`, and `Architecture` tests do not require external services. Component tests may use temp directories, fake process scripts, `HttpClient` test doubles, or EF InMemory, but they should not call real external AI, storage, or network services.

`Integration` tests include `PostgresPersistenceContractTests`, which uses Testcontainers with `postgres:16-alpine`; Docker must be available to run the full `Category=Integration` filter and full backend suite.

`Contract` tests require `PACT_PROVIDER_BASE_URL` and at least one pact file under `tests/contracts/pacts` or `PACT_FILE_DIRECTORY`. For provider verification with local provider startup, run:

```bash
./scripts/pact-provider-verify.sh
```

If pact inputs are missing, `WoongBlog.Api.ContractTests` reports an explicit skip rather than a false pass.
