# Main CI Pact Consumer Stability - 2026-05-06

## Summary
- Stabilized the frontend Pact consumer test that failed on the `main` push after runtime promotion.
- The failed CI signal was `CI Main Runtime` run `25400980868`, job `74500157533`, in `src/test/pact/public-api-consumer.pact.test.ts`.
- The failure was a missing expected Pact mock request for `GET /api/public/works?page=1&pageSize=1` during the public work list/detail contract test.
- The production backend, public load-test target semantics, `pageSize=12`, seed ordering, and cache policy were intentionally left unchanged.

## Changed
- `src/test/pact/public-api-consumer.pact.test.ts`: added `{ retry: 2 }` to `contracts public work list and detail payloads`.
- This aligns the work list/detail Pact interaction with the already existing retry policy on the matching public blog list/detail Pact interaction.
- `todolist-2026-05-06.md`: added the Main CI Pact Consumer Stability plan and validation log.

## Intentionally Not Changed
- No backend production code was changed.
- No API contract payload shape was changed.
- No Real Backend Test target selection was changed.
- No seeded target priority was added.
- No `pageSize=1` runtime/load-test shortcut was added.
- No cache-based workaround was introduced.

## Goal Verification
- Objective: unblock the dev-to-main promotion loop after the main CI frontend job flaked.
- Evidence: main push run `25400980868` failed only the frontend lint/types/unit job, while runtime compose verification and backend jobs passed.
- Fix scope: Pact test stability only, matching an existing retry policy already used by the sibling blog Pact test.
- Remaining gate: GitHub Actions frontend job must pass after this branch is pushed through PR to `dev`.

## Validation
- PASS: `npm run test -- --run src/test/pact/public-api-consumer.pact.test.ts` (1 file, 6 tests).
- PASS: `npm run typecheck`.
- PASS: `npm run lint` (0 errors, 6 existing warnings).
- PASS: `npm run test -- --run src/test/pact/public-api-consumer.pact.test.ts src/test/public-api-clients.test.ts src/test/public-api-contracts.test.ts src/test/public-responsive-feed.test.tsx` (4 files, 45 tests).
- PASS: `git diff --check`.
- INCONCLUSIVE: local full `npm run test -- --run` was stopped after more than 14 minutes without a failure summary. It is not counted as a green full-suite signal.

## Risks And Yellow Flags
- This is a CI stability fix for a nondeterministic Pact consumer failure; it does not prove a backend performance improvement.
- Because the local full frontend suite did not complete reliably, the GitHub Actions frontend job is the authoritative full-suite verification for this slice.
- Existing frontend warning noise remains and should be handled separately if it continues to obscure actionable failures.

## Recommendation
- Commit and push this branch to `dev` via PR.
- Verify the full GitHub Actions frontend job on the PR before merging.
- After `dev` CI is green, regenerate the runtime-only promotion branch and verify `main` CI again.
