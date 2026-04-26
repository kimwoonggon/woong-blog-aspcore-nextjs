# Backend WorkVideo Policy Coverage Reinforcement - 2026-04-26

## Summary

Implemented backend-only pure unit coverage for `WorkVideoPolicy` without changing production code.

The prior full backend coverage report identified `WorkVideoPolicy` as a WorkVideo policy/parser hotspot at 52.7% line coverage and 31.6% branch coverage. After this task, both the unit and full backend coverage reports show `WorkVideoPolicy` at 100.0% line coverage and 95.0% branch coverage.

## Changes Made

- Added `backend/tests/WoongBlog.Api.UnitTests/WorkVideoPolicyTests.cs`.
- Preserved the existing unit test category convention with `[Trait(TestCategories.Key, TestCategories.Unit)]`.
- Added 36 table-driven unit cases:
  - 9 valid `NormalizeYouTubeVideoId` cases for raw IDs, `youtu.be`, `youtube.com`, `m.youtube.com`, `embed`, and `shorts` inputs.
  - 13 invalid `NormalizeYouTubeVideoId` cases for whitespace, non-absolute URLs, unsupported hosts, missing or empty `v`, malformed IDs, malformed `embed` and `shorts` paths, and `youtu.be` without a path.
  - 6 `ValidateVideoFile` cases for non-positive size, oversized files, unsupported extension, unsupported MIME type, uppercase `.MP4`, and valid `.mp4`.
  - 4 `LooksLikeMp4` cases for short prefixes, valid `ftyp`, missing `ftyp`, and later `ftyp`.
  - 4 `SanitizeOriginalFileName` cases for normal names, traversal-looking paths, trimmed names, and 120-character truncation.
- Updated the dated TODO file with planning, instruction mapping, and verification results.
- Added pre-change backup artifacts under `backend/reports/backend-workvideo-policy-coverage-reinforcement-2026-04-26/prechange-backup/`.

## Intentionally Not Changed

- No production code was modified.
- `backend/src/WoongBlog.Application/Modules/Content/Works/WorkVideos/WorkVideoPolicy.cs` was left unchanged because the added tests did not expose a real bug.
- R2, HLS, storage services, and storage selection were not touched.
- No integration tests were added; the policy surface is pure and covered directly through unit tests.
- `NormalizeYouTubeVideoId(null)` was not tested because the current public contract is non-nullable `string rawValue`; whitespace-only input was covered.

## Coverage Results

| Scope | Line coverage | Branch coverage | Source |
| --- | ---: | ---: | --- |
| `WorkVideoPolicy`, previous full | 52.7% | 31.6% | Previous `coverage/backend/full/report/Summary.json` before this task |
| `WorkVideoPolicy`, current unit | 100.0% | 95.0% | `coverage/backend/unit/report/Summary.json` |
| `WorkVideoPolicy`, current full | 100.0% | 95.0% | `coverage/backend/full/report/Summary.json` |
| Full backend, current | 92.2% | 70.5% | `coverage/backend/full/report/Summary.json` |

## Validation

| Command | Result |
| --- | --- |
| `npx skills find dotnet testing` | Completed; no external skill installed because returned .NET testing skills had low install counts and local `tdd` plus .NET guidance was sufficient. |
| `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj --filter WorkVideoPolicy` | Passed; 36 passed, 0 failed. |
| `./scripts/run-backend-coverage.sh unit -v minimal` | Passed; 55 unit tests passed and unit coverage report regenerated. |
| `./scripts/run-backend-coverage.sh full -v minimal` | Passed; contract verification skipped as configured, 96 component tests passed, 55 unit tests passed, 35 architecture tests passed, and 194 integration tests passed. |
| `dotnet test backend/WoongBlog.sln` | Passed; contract verification skipped as configured, 96 component tests passed, 55 unit tests passed, 35 architecture tests passed, and 194 integration tests passed. |
| `git diff --check` | Passed. |

Existing warning observed during .NET commands: `NU1901` for `AWSSDK.Core` 4.0.0.17 low-severity vulnerability.

## Remaining WorkVideo/R2/HLS Gaps

- `R2VideoStorageService`: 9.0% line coverage and 0.0% branch coverage. Remaining gaps include presigned upload behavior, metadata/object reads, object deletion, HLS prefix deletion, and failure mapping without real Cloudflare calls.
- `WorkVideoStorageSelector`: 78.2% line coverage and 30.0% branch coverage. Remaining gaps include configured versus unconfigured R2 selection, forced mode behavior, and local fallback decisions.
- `FfmpegVideoTranscoder`: 61.7% line coverage and 50.0% branch coverage. Remaining gaps include ffprobe failures, ffmpeg non-zero exits, timeout paths, missing manifest handling, and preview generation edge cases.
- `LocalVideoStorageService`: 91.4% line coverage and 70.0% branch coverage. Remaining gaps are mostly filesystem edge/failure paths.
- `WorkVideoPlaybackUrlBuilder`: line coverage is complete, but branch coverage remains 66.6%; URL-building variants can be folded into the R2/storage selector follow-up if needed.

## Risks And Yellow Flags

- `WorkVideoPolicy` still has three uncovered branch slots in coverage tooling, but the requested public behavior matrix is now covered without asserting private implementation details.
- Coverage reports were regenerated locally and may include timestamp or history updates under ignored coverage output.
- The existing `AWSSDK.Core` NU1901 warning remains outside the scope of this task.

## Final Recommendation

Treat the `WorkVideoPolicy` hotspot as addressed. The next coverage step should move to the planned WorkVideo R2/storage/HLS failure matrix, starting with `R2VideoStorageService`, `WorkVideoStorageSelector`, and `FfmpegVideoTranscoder` seams.
