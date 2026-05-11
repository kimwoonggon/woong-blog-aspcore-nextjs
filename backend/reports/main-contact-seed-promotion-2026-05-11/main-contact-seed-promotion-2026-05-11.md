# Main Contact Seed Promotion - 2026-05-11

## Summary

Promoted the runtime-safe part of the dev E2E stability fix to `main`: the seeded Contact page no longer exposes the forbidden `woong@example.com` mailto link.

## Changed

- `backend/src/WoongBlog.Infrastructure/Persistence/Seeding/SeedData.cs`
  - Replaced the seeded Contact page direct email with neutral contact-link copy.
- `backend/tests/WoongBlog.Api.IntegrationTests/PersistenceContractTests.cs`
  - Added assertions that seeded Contact content does not contain `woong@example.com` or `mailto:woong@example.com`.
- `todolist-2026-05-11.md`
  - Recorded the main-specific promotion plan and status.

## Intentionally Not Changed

- No production SSH or remote server action.
- No broad `dev` to `main` merge, because `origin/main..origin/dev` contains a large unrelated history set.
- No dev-only Playwright edge-navigation fixture was added to `main`; main currently has only a minimal browser smoke suite.
- No production compose/env files were changed.

## Validation Plan

- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PersistenceContractTests.SeedData_SeedsCoreContractData_OnlyOnce" --verbosity minimal`: passed, 1 test.
- `git diff --check`: passed.
- Main-compatible frontend/backend CI will run through the PR to `main`.
- Let successful `CI Main Runtime` trigger `Publish GHCR Main` so server users can pull updated runtime images.

## Risks

- This PR only handles the runtime seed mismatch. The dev-only E2E deterministic fixture fix remains on `dev` because main does not carry that E2E suite.
- Runtime images are not available until `CI Main Runtime` and `Publish GHCR Main` complete on `main`.

## Recommendation

Merge this targeted main promotion after CI passes, then use the published `main` GHCR images for server pull/up.
