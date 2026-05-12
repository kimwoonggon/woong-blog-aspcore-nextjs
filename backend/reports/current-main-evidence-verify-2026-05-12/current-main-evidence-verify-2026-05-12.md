# Current Main Evidence Verify Audit 2026-05-12

## Objective

Continue the active goal by hardening the evidence path for server-side current-main deployment verification. The server operator must be able to return a `current-main` evidence bundle, and the repository verifier must prove that the bundle contains current runtime, production preflight, and Real Backend Test evidence without seed, cache, or page-size shortcuts.

## Changed

- Updated `scripts/prod-runtime-evidence-verify.sh` to support both legacy `production-runtime` evidence and newer `current-main` handoff evidence.
- Added extraction support for `current-main-preflight-load-evidence.tgz` as well as the legacy `production-runtime-evidence.tar.gz`.
- Added nested evidence root detection so bundles created as `tar -C <parent> <output-name>` can be verified after extraction.
- Added current-main manifest support for flat `backendDigest`, `frontendDigest`, `baseUrl`, `listPageSize`, and `preflightLog` fields.
- Kept existing real-load validation for public HTTPS base URL, `pageSize=12`, no seed/fixture targets, public Work/Study detail paths, failed-rate threshold, and dropped-iteration threshold.
- Updated `server-current-main-preflight-load-evidence.sh` to tee production preflight output into `current-main-preflight.log` inside the evidence directory before bundling.
- Updated the current-main handoff Markdown, HTML, and JSON docs to list `current-main-preflight.log` as required returned evidence.
- Added behavior-focused Vitest coverage for a current-main evidence directory and a nested current-main tarball bundle.
- Recorded the future Work/Study live-search UI slice in `todolist-2026-05-12.md` without implementing it in this verifier slice.

## Intentionally Not Changed

- No production SSH was used.
- No production workflow dispatch was run.
- No production Real Backend Test was executed from this workspace.
- No cache workaround, seed/fixture target, or `pageSize=12` criterion was weakened.
- No backend optimization slice was selected, because valid returned server evidence is still required first.
- Legacy `production-runtime-evidence-*` support was preserved.

## Goal Verification

- Requirement: returned current-main evidence can be verified.
  Evidence: focused Vitest includes current-main directory and nested tarball scenarios, and all 9 verifier tests pass.
- Requirement: evidence includes persisted preflight output, not console-only output.
  Evidence: handoff script writes `current-main-preflight.log` with `tee`, manifest includes `preflightLog`, and handoff docs list the file as required evidence.
- Requirement: no seed/cache/page-size shortcut.
  Evidence: verifier still rejects seed/fixture targets and requires `listPageSize=12` in summary and each step.
- Requirement: actual public Work/Study targets remain required.
  Evidence: verifier requires `/api/public/works?page=1&pageSize=12`, `/api/public/blogs?page=1&pageSize=12`, and public Work/Study detail paths or HTTPS URLs.
- Requirement: legacy evidence verification remains supported.
  Evidence: existing legacy verifier tests still pass in the same focused suite.

## Validations

- RED verification: `npx vitest run src/test/prod-runtime-evidence-verify.test.ts --pool=threads --maxWorkers=2` failed before implementation on the new current-main cases because `prod-runtime-preflight.log` was required at the input root.
- GREEN verification: `npx vitest run src/test/prod-runtime-evidence-verify.test.ts --pool=threads --maxWorkers=2` passed, 9 tests.
- Shell syntax: `bash -n scripts/prod-runtime-evidence-verify.sh` passed.
- Shell syntax: `bash -n backend/reports/current-main-server-evidence-handoff-2026-05-12/server-current-main-preflight-load-evidence.sh` passed.
- ESLint: `npx eslint src/test/prod-runtime-evidence-verify.test.ts` passed.
- JSON parse: current-main handoff JSON and active-goal current-state audit JSON parsed successfully.
- Diff hygiene: `git diff --check` passed.
- Skill check: `find-skills` search found only low-install shell-related skills, so no new skill was installed; local `tdd` skill was used.

## Risks And Follow-Up

- The active goal is still incomplete until the server-side script is run on the server and returned evidence is verified.
- The verifier now searches nested evidence roots to depth 3, which covers the current handoff bundle shape but should be revisited if artifact packaging changes again.
- Current-main preflight persistence is covered by static shell syntax and verifier fixture tests; actual server execution remains pending.

## Recommendation

- Commit this verifier hardening, promote it through `dev` and `main`, then have the server operator rerun the current-main handoff script and return the generated `current-main-preflight-load-evidence.tgz` for verification.
