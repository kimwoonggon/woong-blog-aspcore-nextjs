# More UX Plan TODO 0416

Execution ledger for the second 2026-04-16 UX pass. Mark `[x]` only after focused verification passes.

## Prev / Next

- [x] Move detail top navigation to left `Next`, right `Previous`.
- [x] Keep `Next` linked to the newer/front item.
- [x] Keep `Previous` linked to the older/back item.
- [x] Preserve `relatedPage` and `returnTo` query state.

## Related Lists

- [x] Keep related pagination semantics: `Previous` lowers `relatedPage`, `Next` raises it.
- [x] Show up to 9 related cards on desktop detail pages.
- [x] Make the related visible-count badge show the actual visible item count.
- [x] Avoid artificial blank related-card slots.

## Study Search

- [x] Add public Study search UI near the `/blog` heading.
- [x] Support title search via URL query state.
- [x] Support content search via URL query state.
- [x] Reset to page 1 when applying a new search.
- [x] Provide a clear search action.

## Dark Mode

- [x] Derive an Editorial Ink dark-mode token system.
- [x] Improve surface hierarchy with semantic dark tokens.
- [x] Keep dark body and muted text contrast at or above 4.5:1.
- [x] Keep default theme light and system option hidden.

## Verification

- [x] Backend public blog search tests.
- [x] Frontend API client search tests.
- [x] Related navigation unit tests.
- [x] Focused Playwright for detail nav, related count/pagination, Study search, and dark mode.
