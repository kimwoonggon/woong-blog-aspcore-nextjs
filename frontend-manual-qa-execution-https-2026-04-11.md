# Frontend Manual QA Execution Log HTTPS 2026-04-11

## Environment
- Base URL: `https://localhost`
- Stack start: `./scripts/run-local-https.sh`
- Compose overlay: `docker-compose.yml` + `docker-compose.https.yml`
- Browser lane: Playwright Chromium
- Recording: Playwright per-test `video.webm`

## Stack Verification
- `docker compose -f docker-compose.yml -f docker-compose.https.yml ps`
  Result: `backend`, `frontend`, `nginx`, `db` up; nginx exposes `443`
- `curl -k -I https://localhost`
  Result: `HTTP/2 200`
- `curl -k https://localhost/api/health`
  Result: `{"status":"ok"}`

## HTTPS Playwright Aggregate
- Command:
  `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=https://localhost npx playwright test ... --workers=1`
- Result:
  `83 passed`, `10 failed`

## HTTPS Failures Observed
- `A-12` single work delete removes the work from admin and public lists
  Result: deletion reaches a 404 page, but the current assertion expects no `404` text at all
- `C-1/C-2/C-7` toolbar formatting, link, and code block render publicly
  Result: combined formatting assertions fail in the aggregate run
- `contact page renders heading and contact content`
  Result: seeded contact content expectation no longer matches after prior mutation tests
- `public-layout-stability` related content cards on detail pages
  Result: related card count/layout assertion fails after prior state mutations
- `admin-home-image-upload`
  Result: HTTPS upload request did not complete in time
- `admin-home-image-validation`
  Result: expected upload-failure alert was not observed
- `admin-input-exceptions` resume non-PDF reject path
  Result: resume input locator did not become ready in time
- `admin-resume-upload`
  Result: HTTPS upload request did not complete in time
- `admin-resume-validation`
  Result: resume input locator did not become ready in time
- `public-admin-affordances` admin session sees navbar status and public edit affordances
  Result: runtime-auth helper hit an execution-context-destroyed error during HTTPS login bootstrap

## Interpretation
- HTTP-mode fixes for `A-6`, `C-3`, and `C-10` remain good, but HTTPS exposes separate issues:
  - upload flow regressions on home/resume editors
  - runtime-auth bootstrap instability on HTTPS
  - stateful test ordering problems for seeded public assertions after content mutation tests

## Artifact Notes
- Videos and failure traces are under `test-results/playwright/*`
- Refresh the summary file after each HTTPS batch:
  `npm run test:e2e:artifacts:index`
