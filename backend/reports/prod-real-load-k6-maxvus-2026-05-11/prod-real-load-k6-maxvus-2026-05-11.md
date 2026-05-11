# Prod Real Load K6 MaxVUs Fix - 2026-05-11

## Problem

The local prod-like real-load smoke exposed a real k6 runtime failure:

```text
ReferenceError: maxVUs is not defined
```

Root cause:
- `scripts/prod-real-load-steps.sh` generated k6 JavaScript declaring `const maxVus = ...`.
- The generated `options.scenarios.public_api_real_mix` object used shorthand `maxVUs`.
- `handleSummary` also referenced shorthand `maxVUs`.
- JavaScript is case-sensitive, so k6 failed before issuing load.

## Changed

- Updated generated k6 options to use `maxVUs: maxVus`.
- Updated generated k6 summary to use `maxVUs: maxVus`.
- Added a regression test asserting the generated script uses the declared variable and does not emit bare `maxVUs,`.

Changed files:
- `scripts/prod-real-load-steps.sh`
- `src/test/prod-real-load-steps.test.ts`

## Intentionally Not Changed

- No load target semantics changed.
- `pageSize=12` remains enforced.
- Seed/fixture targets remain rejected.
- k6 identity query values `__k6Vu` and `__k6Iter` remain in place.
- No cache behavior was added.
- No production server was touched.

## Validations

RED:

```bash
npm test -- src/test/prod-real-load-steps.test.ts
```

Result:
- Failed as expected because generated script did not contain `maxVUs: maxVus`.

GREEN:

```bash
npm test -- src/test/prod-real-load-steps.test.ts
```

Result:
- 1 test file passed
- 5 tests passed

Related tests:

```bash
npm test -- src/test/prod-real-load-steps.test.ts src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-runtime-redeploy-workflow.test.ts
```

Result:
- 3 test files passed
- 9 tests passed

Shell syntax:

```bash
bash -n scripts/prod-real-load-steps.sh scripts/prod-runtime-evidence-bundle.sh scripts/prod-runtime-preflight.sh scripts/prod-public-origin-preflight.sh
```

Result: PASS

Local real-load smoke after fix:
- Stack: local prod-like `main` runtime images through `docker-compose.prod.yml`
- Runner: backend container `k6 v1.0.0` via evidence wrapper
- Rate: 20 rps
- Duration: 10 seconds
- Max VUs: 50
- Requests: 201
- Failed: 0
- p95: 2.8 ms
- `nextFocus`: `increase-rate-or-extend-soak`

## Evidence

- `evidence/red-prod-real-load-steps.log`
- `evidence/green-prod-real-load-steps.log`
- `evidence/related-vitest.log`
- `evidence/bash-syntax.log`
- `../main-runtime-local-prodlike-2026-05-11/evidence/local-prod-real-load.log`
- `../main-runtime-local-prodlike-2026-05-11/evidence/local-prod-real-load-after-fix.log`
- `../main-runtime-local-prodlike-2026-05-11/evidence/local-real-load/prod-real-load-steps-summary.json`
- `../main-runtime-local-prodlike-2026-05-11/evidence/local-real-load/prod-real-load-steps-summary.md`

## Completion Impact

This fixes a blocker that would prevent `prod-real-load-steps.sh` from executing with real k6.

The active goal is still not complete because:
- Production server pull/deploy evidence is absent.
- Production preflight evidence is absent.
- Production Real Backend Test evidence is absent.
- No production-result-driven optimization slice has been selected.

Do not call `update_goal complete`.
