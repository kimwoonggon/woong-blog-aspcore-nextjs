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

## Coverage Reports

Backend coverage uses the existing `dotnet test` flow with `coverlet.collector` and `XPlat Code Coverage`; readable reports are generated with the backend-local ReportGenerator tool manifest in `backend/.config/dotnet-tools.json`.

Run coverage from the repository root:

```bash
./scripts/run-backend-coverage.sh unit
./scripts/run-backend-coverage.sh component
./scripts/run-backend-coverage.sh integration
./scripts/run-backend-coverage.sh full
```

The same script accepts extra `dotnet test` arguments after the suite name:

```bash
./scripts/run-backend-coverage.sh component --blame-hang --blame-hang-timeout 5m -v minimal
```

Coverage output is local build/test output and is not intended for commits:

| Suite | Raw collector output | Readable report |
| --- | --- | --- |
| UnitTests | `coverage/backend/unit/raw/` | `coverage/backend/unit/report/index.html` |
| ComponentTests | `coverage/backend/component/raw/` | `coverage/backend/component/report/index.html` |
| IntegrationTests | `coverage/backend/integration/raw/` | `coverage/backend/integration/report/index.html` |
| Full backend solution | `coverage/backend/full/raw/` | `coverage/backend/full/report/index.html` |

Each readable report also includes `SummaryGithub.md`, `Summary.txt`, `Summary.json`, and a merged `Cobertura.xml` file in the suite's `report` directory.

Coverage percentage is useful as a gap-finding signal, not a quality target by itself. A high line percentage can still miss authorization failures, persistence side effects, concurrency paths, invalid input, startup composition, and real database behavior. Prefer coverage that proves important behavior through the right test level. No strict high coverage threshold is enforced yet; if a threshold is added later, it should start from the current backend baseline or a conservative lower baseline and be ratcheted only after meaningful tests are added.

Some backend areas are intentionally covered outside `UnitTests`:

| Area | Primary coverage level | Reason |
| --- | --- | --- |
| HTTP endpoints, auth/session/CSRF/authorization, middleware, routing, startup, options validation, health/OpenAPI wiring | Integration | Behavior depends on the ASP.NET host, middleware order, DI, filters, and auth schemes. |
| PostgreSQL constraints, schema bootstrap, relational cascade behavior, and Testcontainers-backed persistence contracts | Integration | EF InMemory cannot prove relational database semantics. |
| Application handlers with Infrastructure stores, EF InMemory, filesystem/process fakes, cleanup services, public query stores, WorkVideo storage/order behavior, AI batch runtime, and Codex process seams | Component | These tests need real composition of Application plus Infrastructure seams but not the full HTTP host. |
| Validators, pure helper logic, small DTO algorithms, and deterministic planning logic | Unit | These behaviors are isolated and do not need ASP.NET, EF, filesystem, process, or network dependencies. |

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
