# TOC Rail Readability And Collapse Audit - 2026-05-12

## Summary
- Improved public detail `On This Page` / `On This Work` desktop TOC rail readability.
- Increased the desktop TOC rail from 22rem to 24rem and reduced the `xl` grid gap from 3rem to 2.5rem so the wider rail still fits the desktop viewport.
- Changed the TOC header from a truncating flex row to a non-overlapping grid row so the collapse button does not squeeze title text.
- Added `aria-controls` linkage from the collapse button to the TOC list.
- Added collapsed-state summary copy so the card does not become visually empty after collapse.
- Strengthened unit and Playwright coverage around rail width, long heading link width, non-overlap, viewport fit, and collapsed summary visibility.

## Changed Files
- `src/components/content/TableOfContents.tsx`
- `src/app/(public)/blog/[slug]/page.tsx`
- `src/app/(public)/works/[slug]/page.tsx`
- `src/test/table-of-contents.test.tsx`
- `tests/public-blog-toc-layout.spec.ts`
- `tests/public-work-toc.spec.ts`
- `todolist-2026-05-12.md`

## Intentionally Not Changed
- No backend, database, load test, production deploy, or remote server changes.
- No article body max width change.
- No mobile TOC behavior change; the desktop rail remains hidden below `xl` as before.
- No visual theme/color system rewrite.

## Goal Verification
- User concern: collapse control and text area felt narrow.
- Result: rail width requirement is now at least 360px in browser tests, long heading link width is at least 336px, and the collapse state keeps visible summary copy.
- Non-goal check: production/remote server work was not performed.

## Validations
- RED: `vitest --pool=threads --maxWorkers=2 --reporter=dot --run src/test/table-of-contents.test.tsx` failed before implementation because `aria-controls`/list id were missing.
- GREEN: `vitest --pool=threads --maxWorkers=2 --reporter=dot --run src/test/table-of-contents.test.tsx` passed, 9 tests.
- Lint: `eslint src/components/content/TableOfContents.tsx src/test/table-of-contents.test.tsx tests/public-blog-toc-layout.spec.ts tests/public-work-toc.spec.ts 'src/app/(public)/blog/[slug]/page.tsx' 'src/app/(public)/works/[slug]/page.tsx'` passed.
- Build/type coverage: `docker compose -f docker-compose.dev.yml up -d --build` completed frontend Next production build and TypeScript stage successfully.
- Dev stack health: `curl http://127.0.0.1:13000/api/health` returned HTTP 200 after running the stack on non-conflicting ports.
- Playwright: `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:13000 ... playwright test tests/public-blog-toc-layout.spec.ts tests/public-work-toc.spec.ts --workers=1` passed, 2 tests.
- Diff hygiene: `git diff --check` passed.

## Environment Notes
- Initial compose attempt failed because `127.0.0.1:8080` was already in use.
- The worktree lacked `.env`; `.env.example` was copied to local ignored `.env` for dev-only validation.
- The worktree lacked local HTTPS certs and Postgres bind mount on the Windows path could not chmod, so focused browser validation used `NGINX_DEFAULT_CONF=./nginx/default.conf`, `POSTGRES_DATA_DIR=/tmp/toc-rail-readable-collapse-postgres-20260512`, `BACKEND_PUBLISH_PORT=18080`, `NGINX_HTTP_PORT=13000`, `NGINX_HTTPS_PORT=13001`.

## Risks And Follow-Up
- The wider 24rem rail shifts desktop content slightly left at `xl`; Playwright verifies viewport fit at 1440/1600 widths for the covered pages.
- If design wants more article centering at exactly 1280px, a future slice can introduce a `2xl`-only wider rail and keep 22rem at `xl`.
- Full E2E was not run because this was a targeted UI/layout fix; focused browser coverage was run for the affected pages.

## Recommendation
- Ship this slice if the desired behavior is a more readable desktop TOC rail without changing mobile or article body width.
