# Prod Real Load Reject Seed Targets - 2026-05-11

## Goal

Advance the active load-test objective without remote server access by hardening the server-side Real Backend Test runner so standalone execution enforces the user's required target rules:

- list targets remain `pageSize=12`
- no seeded or fixture detail targets
- no cache shortcut result
- real Work/Study detail API paths are supplied explicitly

## Changed

- `scripts/prod-real-load-steps.sh`
  - Added `reject_seed_or_fixture_read_path`.
  - The script now fails before k6 execution when `WORK_READ_PATH` or `STUDY_READ_PATH` contains `seed`, `seeded`, or `fixture`.
  - Existing `LIST_PAGE_SIZE=12` guard and k6 `__k6Vu`/`__k6Iter` cache-busting query behavior remain intact.
- `src/test/prod-real-load-steps.test.ts`
  - Added behavior coverage that seeded Work and fixture Study paths are rejected before fake k6 is invoked.
- `docs/walkthroughs/main-server-setup.md`
  - Added post-deploy preflight command.
  - Added Real Backend Test command with explicit real Work/Study paths.
  - Documented `pageSize=12`, seed/fixture rejection, and cache-busting behavior.
- `todolist-2026-05-11.md`
  - Recorded the TDD slice, backups, and validations.

## Intentionally Not Changed

- No production SSH or remote server execution.
- No production `.env.prod` secrets read or modified.
- No runtime image, compose service, DB schema, or application API behavior changed.
- No Real Backend Test result was fabricated or inferred.
- No result-based backend optimization slice was selected yet, because that still requires a fresh post-deploy Real Backend Test result.

## Validation

- RED check:
  - `npm test -- src/test/prod-real-load-steps.test.ts`
  - Expected failure before implementation: seeded Work target was accepted and k6 was invoked.
- GREEN targeted script check:
  - `npm test -- src/test/prod-real-load-steps.test.ts`
  - Result: PASS, 1 file, 4 tests.
- Related production script check:
  - `npm test -- src/test/prod-runtime-preflight.test.ts src/test/prod-real-load-steps.test.ts`
  - Result: PASS, 2 files, 10 tests.
- Diff hygiene:
  - `git diff --check -- scripts/prod-real-load-steps.sh src/test/prod-real-load-steps.test.ts docs/walkthroughs/main-server-setup.md todolist-2026-05-11.md`
  - Result: PASS.
- Static evidence grep:
  - `scripts/prod-real-load-steps.sh` contains `reject_seed_or_fixture_read_path`, `LIST_PAGE_SIZE must remain 12`, and `__k6Vu`/`__k6Iter` cache-busting identity.
  - `docs/walkthroughs/main-server-setup.md` documents preflight, real-load command, `pageSize=12`, seed/fixture rejection, and cache-busting behavior.

## Risks / Yellow Flags

- The guard rejects any path containing `seed` or `fixture`. This is intentionally conservative for production load targets, but a legitimate article slug containing `seed` would be rejected and should be replaced by another real target.
- This does not prove production has deployed latest `main`; it only makes the script safer when the server operator runs it.
- The active goal remains blocked on actual server pull/up, production preflight output, and fresh Real Backend Test output.

## Recommendation

Promote this small hardening through `dev` and then `main`. After the server operator deploys `main`, run the documented preflight and `prod-real-load-steps.sh` commands with real current public Work/Study detail paths, then choose the next backend optimization slice from the generated summary.
