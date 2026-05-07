# Main Runtime Nginx Config Allowlist Audit - 2026-05-07

## Summary
- Fixed the main runtime promotion blocker caused by nginx architecture test coverage and runtime-only allowlist mismatch.
- Added architecture coverage that requires every nginx config checked by `NginxRuntimeConfigTests` to be present in `scripts/main-runtime-allowlist.txt`.
- Added `nginx/default.conf` and `nginx/local-https.conf` to the main runtime allowlist so `release/main-promote -> main` contains all files required by the architecture tests.

## Changed Files
- `backend/tests/WoongBlog.Api.ArchitectureTests/NginxRuntimeConfigTests.cs`
  - Added `MainRuntimeAllowlist_IncludesEveryNginxConfigUnderArchitectureCoverage`.
- `scripts/main-runtime-allowlist.txt`
  - Added `nginx/default.conf` and `nginx/local-https.conf`.
- `todolist-2026-05-07.md`
  - Recorded the interrupting promotion-fix slice.

## Intentionally Not Changed
- No backend runtime behavior changed.
- No nginx timeout directive values changed.
- No Docker compose, production environment, public API, database, or load-test target behavior changed.
- No direct push to `main` was performed.

## Failure Root Cause
- Open promotion PR #113 was generated from `dev@ed997d4` and failed `CI Main Runtime / Backend architecture tests`.
- The failure was `FileNotFoundException` for `nginx/default.conf` and `nginx/local-https.conf`.
- Those files are checked by `NginxRuntimeConfigTests`, but they were not copied into the runtime-only promotion tree because the allowlist omitted them.

## Validations
- RED: `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj --filter "FullyQualifiedName~MainRuntimeAllowlist_IncludesEveryNginxConfigUnderArchitectureCoverage" --no-restore --logger "console;verbosity=minimal"`
  - Failed before implementation because `nginx/default.conf` was missing from the allowlist.
- GREEN: same focused path passed after adding the missing nginx configs through the focused `NginxRuntimeConfigTests` run.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj --no-restore --logger "console;verbosity=minimal"`
  - Passed 40/40.
- PASS: `git diff --check`

## Risks And Yellow Flags
- This fixes the promotion tree shape; it does not by itself merge #113 or publish GHCR.
- The existing #113 failed checks must be refreshed by a new promotion branch update after this fix lands on `dev`.
- Existing GitHub Actions Node.js 20 deprecation warnings remain unrelated.

## Final Recommendation
- Merge this fix to `dev`, wait for `CI Dev`, then allow the Promote Main Runtime workflow to refresh `release/main-promote`.
- Verify `CI Main Runtime` on the refreshed promotion PR passes and then merge to `main`; confirm `Publish GHCR Main` succeeds afterward.
