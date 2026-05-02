# Mobile Content Layout and Code Rendering Hotfix Audit - 2026-05-02

## Changed
- Study/Works detail shells now use explicit full-width/min-width containment.
- Public prose renders full-width at the renderer level, while paragraphs retain `72ch` readable line length.
- Code blocks now use a stronger code-window visual treatment and contained horizontal scrolling.
- Inline code no longer renders as filled gray badges.
- Detail-page anchor scrolling reserves sticky-navbar space without globally affecting list-page click scrolling.
- Mobile search opens in a viewport-fixed overlay and preserves scroll while focusing the search field.
- Theme default is explicit light; dark/light toggles remain direct and persistent.
- Admin dashboard all-collections-failed state restores the expected specific fallback copy.

## Intentionally not changed
- Resume page was kept as the layout reference.
- Backend GPT-5.5 config was not changed because it already included `gpt-5.5` as default/allowed.
- Existing unrelated dirty files/directories were not staged.

## Goal verification
- Mobile Study/Works detail spill: fixed with width containment and code-block internal scrolling.
- Code visual quality: fixed with code-window styling.
- Unexpected shaded inline text: fixed by reducing inline code styling.
- Sticky TOC anchor behavior: fixed and verified.
- Mobile search scroll stability: fixed and verified.
- GPT-5.5 support: confirmed already present.

## Validations performed
- `BACKEND_PUBLISH_PORT=18080 ./scripts/dev-up.sh`: passed.
- Focused e2e failure set: 34 passed.
- Mobile/search + scroll-margin focused e2e: 4 passed.
- Response-time focused e2e: 9 passed.
- Latest full e2e: 423 passed, 4 skipped, 1 latency-budget failure under full-suite load.

## Risks and yellow flags
- The latest full e2e run had one transient response-time budget failure: `Public nav click to Works` took 2145.15ms against a 1500ms hard budget.
- The same response-time spec passed in focused rerun, so this was treated as suite-load latency flake rather than a functional regression.

## Final recommendation
- Proceed with push after staging only the scoped files. Monitor response-time budget behavior in CI because the full local run exposed a transient latency spike.
