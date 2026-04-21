# Frontend CI Pact Fix Audit

- Generated: 2026-04-21T16:28:19+09:00
- Branch: `feat/phase-2-backend-discompostion`
- Commit SHA at generation: `224a8f896e1cc5165f4419d053979787c4b8b646`

## Executive Summary

Overall status: PASS

The failed PR check was not a backend architecture failure. The failing job was `Frontend lint, types, and unit tests`, and the single failing test was the Pact consumer test for public site settings.

Root cause: in the full Vitest suite, the Pact test could reach the real `getServerApiBaseUrl()` implementation, which calls Next.js `headers()`. That API is request-scoped and fails in plain Vitest/jsdom when no App Router request context exists.

## Failure Evidence

| Source | Evidence |
| --- | --- |
| GitHub Actions job | `Frontend lint, types, and unit tests` failed in run `24705473556`, job `72257705668`. |
| Failing test | `src/test/pact/public-api-consumer.pact.test.ts > contracts public site settings payload`. |
| Error | `headers was called outside a request scope` from `src/lib/api/server.ts:30`. |

## Fix Applied

| File | Change |
| --- | --- |
| `src/test/pact/public-api-consumer.pact.test.ts` | `withServerApi()` now mocks `next/headers` with the Pact mock server host/proto and an empty cookie store before importing public API clients. |
| `src/test/pact/public-api-consumer.pact.test.ts` | Removed unused Pact `string` matcher import, reducing one lint warning. |

## Behavior Preservation

No production code changed. Backend architecture, endpoint routes, DTO JSON shapes, Dockerfiles, CI files, and runtime configuration were not modified.

## Verification

| Command | Result |
| --- | --- |
| `npx vitest run src/test/pact/public-api-consumer.pact.test.ts --pool=threads` | PASS, 6 tests passed. |
| `npm run lint` | PASS, 0 errors and 5 existing warnings. |
| `npm run typecheck` | PASS. |
| `npm test -- --run` | PASS, 46 files and 239 tests passed. |

## Remaining Notes

Existing lint warnings remain outside this fix scope:

- `scripts/generate-test260419-flow-doc.mjs`
- `src/components/admin/WorkEditor.tsx`
- `src/test/work-editor.test.tsx`
- `tests/responsive-width-sweep.spec.ts`
- `tests/ui-quality-a11y-advanced.spec.ts`

These warnings are non-blocking for the current CI failure because `npm run lint` exits successfully.

## Recommendation

Push the fix commit to `origin/feat/phase-2-backend-discompostion` and let PR #13 rerun CI. The branch is ready to re-check the normal review/merge gate after the pushed commit.
