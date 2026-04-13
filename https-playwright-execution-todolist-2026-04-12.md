# HTTPS Playwright Execution TODO — 2026-04-12

## Goal

- Restore `https://localhost` to a healthy state
- Verify the HTTPS front door before any broad browser queue
- Run the Playwright integration plan serially against HTTPS
- Record evidence after each step

## Queue Rules

- Main agent owns the queue and checklist
- Browser Playwright runs only after the current prerequisite is verified
- No item is checked without command evidence

## Phase 1 — HTTPS Recovery

- [x] `HTTPS-1` Capture current nginx/frontend/backend status and logs
- [x] `HTTPS-2` Identify the root cause of `502 Bad Gateway`
- [x] `HTTPS-3` Apply the smallest fix needed for HTTPS front door health
- [x] `HTTPS-4` Verify `https://localhost/login` returns `200`
- [x] `HTTPS-5` Verify `https://localhost/admin/blog` returns frontend HTML

## Phase 2 — Integration Queue

- [x] `QUEUE-1` Functional integration sweep on HTTPS
- [x] `QUEUE-2` Web quality / visual sweep on HTTPS
- [x] `QUEUE-3` Runtime-auth sweep on HTTPS
- [x] `QUEUE-4` Record final pass/fail totals and artifact paths

## Evidence Log

- `NGINX_DEFAULT_CONF=./nginx/local-https.conf docker compose up --build -d nginx frontend backend` -> completed
- `curl -k -I https://localhost/login` -> `HTTP/2 200`
- `curl -k -s https://localhost/admin/blog` -> updated admin copy confirmed
- Full HTTPS sweep command:

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=https://localhost \
npx playwright test \
  tests/admin-*.spec.ts \
  tests/public-*.spec.ts \
  tests/ui-admin-*.spec.ts \
  tests/ui-improvement-*.spec.ts \
  tests/home.spec.ts \
  tests/introduction.spec.ts \
  tests/resume.spec.ts \
  tests/auth-login.spec.ts \
  tests/auth-security-browser.spec.ts \
  tests/dark-mode.spec.ts \
  tests/manual-qa-gap-coverage.spec.ts \
  tests/manual-qa-auth-gap.spec.ts \
  tests/work-*.spec.ts \
  --workers=1
```

- Final result: `252 passed (7.2m)`
