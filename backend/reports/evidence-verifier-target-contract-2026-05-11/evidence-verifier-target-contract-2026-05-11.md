# Evidence Verifier Target Contract - 2026-05-11

## Summary

Strengthened the production evidence bundle and verifier so returned Real Backend Test evidence must preserve realistic public target semantics.

## Changed

- `scripts/prod-runtime-evidence-verify.sh`
  - Allows Work/Study read targets as either public API paths or absolute public HTTPS URLs.
  - Still rejects backend/local direct targets.
  - Keeps strict list targets at `/api/public/works?page=1&pageSize=12` and `/api/public/blogs?page=1&pageSize=12`.
- `scripts/prod-runtime-evidence-bundle.sh`
  - Rejects backend-direct read target URLs before creating an evidence bundle.
  - Validates expected list target paths.
  - Validates Work/Study read targets are public detail paths or public HTTPS URLs.
- `src/test/prod-runtime-evidence-verify.test.ts`
  - Added coverage for absolute public HTTPS Work/Study read URLs.
- `src/test/prod-runtime-evidence-bundle.test.ts`
  - Added coverage for backend-direct read target rejection.

## Intentionally Not Changed

- No production SSH or remote server execution.
- No production `.env.prod` secrets were read or written.
- No Real Backend Test was executed against production.
- No performance slice was selected from load results because the required production result is still missing.

## Goal Verification

| Active goal requirement | Evidence after this slice |
| --- | --- |
| `pageSize=12` | Bundle and verifier enforce exact list target paths and list page size. |
| No seed/fixture targets | Existing bundle and verifier checks remain in place. |
| No backend-direct target | Bundle now rejects backend/local/127 direct read URLs; verifier accepts only public paths or HTTPS public read URLs. |
| Real Work/Study URL support | Verifier now accepts absolute public HTTPS Work/Study read URLs as valid evidence. |
| Production deploy/preflight/load | Still missing by design because remote production execution is excluded. |

## Validation

### TDD RED

- `prod-runtime-evidence-verify.test.ts`: absolute public HTTPS read URL failed before implementation with `step 1 work_read must be a public Work detail path`.
- `prod-runtime-evidence-bundle.test.ts`: backend-direct read target was incorrectly accepted before implementation.

### GREEN

Command:

```bash
PATH=/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/node_modules/.bin:$PATH vitest --pool=threads --maxWorkers=2 --reporter=dot --run src/test/prod-real-load-steps.test.ts src/test/prod-runtime-preflight.test.ts src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-runtime-evidence-verify.test.ts
```

Result:

- Test files: 4 passed
- Tests: 24 passed
- Duration: 34.01s

## Risks / Follow-Up

- This improves evidence quality but does not complete the active goal.
- The active goal still needs server-side pull/deploy, production preflight, production Real Backend Test, result-based slice selection, and after-slice full E2E/CI.

## Recommendation

Merge this guard improvement through `dev`, then promote to `main`. After the server operator runs the current `main` images and returns an evidence bundle, run `scripts/prod-runtime-evidence-verify.sh` before selecting the next backend slice.
