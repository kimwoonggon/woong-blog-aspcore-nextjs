# Load Test: Max Users 10,000 Audit

Generated: 2026-05-04

## Summary

Expanded load test dashboard user upper bound from 1,000 to 10,000 while keeping existing dashboard behavior and UI layout unchanged.

- `MAX_USERS` is now `10_000` in `src/lib/load-test-dashboard.ts`.
- UI number inputs for start/max/step users now reference the shared `MAX_USERS` constant.
- Unit coverage updated to assert clamping at the new upper bound and acceptance at 10,000.

## Intentionally Not Changed

- Concurrency cap (still 100) and default scenario values (`start=100`, `max=1000`) are intentionally kept.
- Existing load-test scenario generation algorithm and request execution flow unchanged.
- Existing dashboard styling and responsive behavior were left untouched.

## Goal Validation

- Upper bound for users is now 10,000 (`sanitizeLoadTestConfig`).
- Maximum input allowed in UI for start/max/step users now matches backend cap.
- Unit tests pass for both clamping and valid boundary acceptance around 10,000.

## Validations Performed

- `npx vitest run src/test/load-test-dashboard.test.ts`
- `npx eslint src/lib/load-test-dashboard.ts src/components/admin/LoadTestDashboard.tsx src/test/load-test-dashboard.test.ts`
- `npm run typecheck`
- `git diff -- src/lib/load-test-dashboard.ts src/components/admin/LoadTestDashboard.tsx src/test/load-test-dashboard.test.ts todolist-2026-05-04.md`

## Risks / Yellow Flags

- Browser-driven load test still limited by runtime/resources and is not a substitute for distributed load tools.
- Large step values may create large scenario counts if `startUsers`/`stepUsers` are adjusted heavily.

## Recommendation / Next Step

Merge to `dev` first and confirm CI. If passed, promote to `main` as requested in prior flow.

