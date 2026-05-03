# Public Article Code And Mermaid GitHub Styling Audit (2026-05-03)

## Scope

Requested change: update Study/blog and Work reading pages so rendered code blocks and Mermaid diagrams look closer to GitHub, stay readable in light and dark mode, and remove the mac-style floating chrome above code blocks.

## Changed

- Updated public prose code styles in `src/app/globals.css`:
  - Replaced the custom gradient/shadow/mac-dot chrome with a GitHub-like flat code surface.
  - Added GitHub-like light and dark variables for block code, inline code, borders, and highlight.js token colors.
  - Preserved horizontal scrolling and monospace formatting for long code.
- Updated Mermaid diagram styling in `src/app/globals.css`:
  - Added `mermaid-diagram-shell` surface variables for light/dark backgrounds, borders, text, and lines.
  - Added SVG text and edge color rules so rendered diagrams remain visible.
- Updated `src/components/content/MermaidRenderer.tsx`:
  - Switched Mermaid initialization to `theme: 'base'`.
  - Added explicit GitHub-readable light/dark Mermaid theme variables.
  - Re-renders diagrams when the document theme class changes.
- Updated Mermaid typing in `src/types/mermaid.d.ts` to include `themeVariables`.
- Added and updated tests:
  - `src/test/mermaid-renderer.test.tsx`
  - `tests/dark-mode.spec.ts`
- Updated `todolist-2026-05-03.md` with the requested instructions, plan, TDD notes, and verification results.

## Intentionally Not Changed

- Did not change article content storage, Markdown/HTML parsing, sanitization, or editor output.
- Did not change navigation, layout rails, TOC behavior, or card layouts.
- Did not install the low-install Mermaid skill found during skill discovery.
- Did not touch unrelated pre-existing local changes such as `image.png` or older untracked hotfix/report directories.

## Goal Verification

- Study/blog detail pages now render block code with a GitHub-like light/dark surface and no pseudo mac window dots.
- Work detail pages share the same public renderer and are covered by the new Mermaid Playwright fixture.
- Mermaid diagrams now use explicit light/dark theme variables instead of relying on Mermaid's default/dark presets.
- Light mode Mermaid surfaces are no longer plain card white with default Mermaid coloring; they use the same GitHub-like readable treatment.

## Validation

| Check | Result |
| --- | --- |
| `npx vitest run src/test/mermaid-renderer.test.tsx` before implementation | Failed as expected: old renderer used `default`/`dark` themes and lacked `mermaid-diagram-shell`. |
| Focused Playwright `DM-18` before implementation | Failed as expected on old 16px/mac-chrome code block styling and Mermaid light surface mismatch. |
| `BACKEND_PUBLISH_PORT=18080 NGINX_HTTPS_PORT=3002 docker compose -f docker-compose.dev.yml up -d --build frontend nginx` | Passed; frontend `next build` and TypeScript completed successfully. |
| `npx vitest run src/test/mermaid-renderer.test.tsx src/test/interactive-renderer.test.tsx` | Passed: 13/13. |
| `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_E2E_PROFILE=exhaustive npx playwright test tests/dark-mode.spec.ts --project=chromium-authenticated --grep "DM-18" --workers=1` | Passed: 2/2. |
| `npx eslint src/components/content/MermaidRenderer.tsx src/test/mermaid-renderer.test.tsx tests/dark-mode.spec.ts` | Passed. |
| `git diff --check` | Passed. |

## Risks And Follow-Up

- The highlight.js token overrides are intentionally scoped to `.prose` and `.content-code-block`, but future non-standard token classes may need extra mappings.
- Mermaid SVG output differs by diagram type; the new theme variables and shell rules cover common text, edge, node, cluster, and note colors, but unusual diagram syntaxes may warrant additional visual checks.
- Existing unrelated dirty files remain in the worktree and were not modified for this task.

## Recommendation

Keep the change. The requested Study/Work reading surfaces are covered by focused unit and Playwright tests, and the dev compose frontend was rebuilt successfully with the updated styling.
