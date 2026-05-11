# Prod Public Origin Preflight Guard - 2026-05-11

## Summary

Added a Docker/SSH-free public-origin preflight script that verifies whether the public site is safe to use for Real Backend Test interpretation after a `main` runtime image pull/deploy.

The guard fails before Real Backend Test when the public origin is stale or incomplete, including:

- missing `X-Nginx-Request-Time` timing header
- missing `X-App-Elapsed-Ms` app timing header
- missing gzip on public JSON responses
- Work list exposing stale card fields such as `iconUrl` or `period`
- Work detail exposing stale `iconUrl`, `contentJson`, or video admin fields such as `originalFileName`, `fileSize`, `createdAt`
- Blog/Study detail exposing stale `contentJson`
- `WORK_READ_PATH` or `STUDY_READ_PATH` using `seed`/`fixture` targets
- `WORK_READ_PATH` and `STUDY_READ_PATH` pointing to the wrong public API type or a non-public absolute URL
- list target not using `pageSize=12`

## Changed

- Added `scripts/prod-public-origin-preflight.sh`.
- Added Vitest coverage in `src/test/prod-public-origin-preflight.test.ts`.
- Added the script to `scripts/main-runtime-allowlist.txt` so it is carried into the promoted `main` runtime tree.
- Updated `docs/walkthroughs/main-server-setup.md` to run public-origin preflight before Real Backend Test interpretation.
- Updated `todolist-2026-05-11.md` with this slice and verification plan.

## Intentionally Not Changed

- No production SSH or server-side command execution was performed.
- No cache optimization was added.
- No Real Backend Test result was interpreted from the current public origin.
- `prod-real-load-steps.sh` still owns k6 execution; this new script is a preflight gate.

## Validation

- RED: `npm test -- src/test/prod-public-origin-preflight.test.ts` failed because `scripts/prod-public-origin-preflight.sh` did not exist.
- RED: `npm test -- src/test/prod-public-origin-preflight.test.ts` also failed when swapped Work/Study public target types were accepted.
- GREEN: `npm test -- src/test/prod-public-origin-preflight.test.ts` passed 6/6.
- PASS: `npm test -- src/test/prod-public-origin-preflight.test.ts src/test/prod-runtime-preflight.test.ts src/test/prod-real-load-steps.test.ts` passed 16/16.
- PASS: `bash -n scripts/prod-public-origin-preflight.sh scripts/prod-runtime-preflight.sh scripts/prod-real-load-steps.sh`.
- LIVE BLOCKER CONFIRMED: running the new preflight against `https://woonglab.com` with real non-seed targets failed at `public Work list missing X-Nginx-Request-Time`.

## Live Public Origin Probe

Command used:

```bash
OUTPUT_DIR=backend/reports/prod-public-origin-preflight-2026-05-11/live-probe \
BASE_URL=https://woonglab.com \
WORK_READ_PATH=/api/public/works/smoke-fluid-simulation \
STUDY_READ_PATH=/api/public/blogs/%EC%96%B4%EC%85%88%EB%B8%94%EB%A6%AC-%EC%83%9D%EA%B8%B0%EC%B4%88-stack-pointer-ebp-esp-eip \
./scripts/prod-public-origin-preflight.sh
```

Result:

```text
[prod-public-origin-preflight] FAIL: public Work list missing X-Nginx-Request-Time
```

This confirms the current public origin still should not be used as the basis for the next HLS/body/DB slice decision.

## Risks And Follow-Up

- The script is intentionally strict for Real Backend Test interpretation. If nginx timing headers are intentionally removed from public traffic, the performance attribution model must be changed before using load results.
- The script proves public-origin readiness, not server-side container resource settings. Server-side `prod-runtime-preflight.sh` is still needed after actual pull/up.
- Current public origin is still blocked for Real Backend Test interpretation until latest `:main` images are actually deployed and the preflight passes.

## Recommendation

After the server operator pulls and starts the latest `:main` images, run:

```bash
BASE_URL=https://woonglab.com \
WORK_READ_PATH=/api/public/works/<real-work-slug> \
STUDY_READ_PATH=/api/public/blogs/<real-study-slug> \
./scripts/prod-public-origin-preflight.sh
```

Only run and interpret Real Backend Test after this public-origin guard and the compose-based production preflight both pass.
