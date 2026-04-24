# Notion Editor Save Test Impact Audit (2026-04-24)

## Summary
- Scope: audit-only inspection of frontend test impact for the admin Notion editor if content autosave moves from `10_000ms` to about `1_000ms` and a `Ctrl+S`/`Meta+S` manual save shortcut is added.
- Current implementation anchor: `AUTOSAVE_DEBOUNCE_MS = 10_000` in `src/components/admin/BlogNotionWorkspace.tsx:60`.
- Current implementation anchor: there is no Notion workspace keyboard save listener today; save shortcut patterns exist in the full blog/work editors, not in `BlogNotionWorkspace`.
- Result: one Vitest file and one Playwright file need direct updates; two additional Playwright files are good extension points for visual/manual-save coverage; several current selectors and fixed sleeps are brittle under a 1-second debounce.

## Intentionally Not Changed
- No production code changed.
- No test code changed.
- No local or Docker test runs were performed; this pass was repository inspection only.

## Goals And Non-Goals
- Goal: identify existing Vitest and Playwright coverage to modify or extend for the save-behavior change.
- Goal: recommend concise new assertions.
- Goal: call out brittle selectors and timing assumptions likely to break.
- Non-goal: implement the feature or rewrite tests.

## Recommended Test Updates

### 1. Highest-priority Vitest update
- File: `src/test/blog-notion-workspace.test.tsx:168`
- Why it changes: the existing test hard-codes the current `10s` debounce with `9_500ms` then `500ms` fake-timer advances.
- Recommended assertions:
  - Keep the pre-threshold assertion, but move it to roughly `900-950ms`.
  - Assert no `fetchWithCsrf` call before the threshold.
  - After crossing roughly `1_000ms`, assert one `PUT` to `/admin/blogs/:id`.
  - Tighten the save-state expectation from `/Saving...|Saved/` to an ordered transition when possible: `Waiting` before threshold, then `Saving...`, then `Saved` after the mocked response resolves.
- Recommended extension in the same file:
  - Add a manual-save test that dispatches `keydown` with `ctrlKey/metaKey + s` after dirtying content.
  - Assert `preventDefault()` is honored.
  - Assert the save request fires immediately without advancing the autosave timer to `1_000ms`.
  - After that manual save, advance timers past the autosave window and assert no duplicate second `PUT` is triggered for unchanged content.

### 2. Highest-priority Playwright update
- File: `tests/ui-admin-notion-autosave-info.spec.ts:37`
- Why it changes: this file encodes the old debounce model in both its title and its timing assumptions.
- Recommended assertions for the first test:
  - Rename the test so it no longer implies a long autosave delay.
  - Keep the initial `Waiting` assertion before edits.
  - After editing, wait for the targeted `PUT` whose request body contains the new text.
  - Reduce the `Saving...` timeout from `15_000ms` to something aligned with a `~1s` debounce plus CI slack, such as `3_000-5_000ms`.
  - Assert `Saved` after the `PUT` completes.
- Recommended new Playwright test in the same file:
  - Use the existing `replaceEditorContent(...)` helper, then press `Control+S` or `Meta+S`.
  - Assert the `PUT` starts before the autosave debounce would naturally expire.
  - Assert `notion-save-state` transitions through `Saving...` to `Saved`.
  - Reload the page and verify the edited content persisted, so the shortcut is not only changing the badge.
- Recommended refinement for the throttled revalidation test at `tests/ui-admin-notion-autosave-info.spec.ts:81`:
  - Replace the `waitForTimeout(1_500)`-style inference with request-count assertions tied to the second autosave `PUT` and the absence of a second `/revalidate-public` `POST` before explicit metadata save.

### 3. Secondary Playwright smoke extension
- File: `tests/admin-blog-edit.spec.ts:51`
- Why it is relevant: it already covers the Notion workspace after document switching and content editing.
- Recommended assertions:
  - After switching documents and editing content, assert the save chip reaches `Saved` quickly instead of only waiting on the `PUT`.
  - Optional: add a keyboard-save branch here after the document switch to ensure `Ctrl+S` saves the currently selected Notion document, not the previously opened one.
- Priority: medium; the primary save-behavior coverage belongs in `ui-admin-notion-autosave-info.spec.ts`.

### 4. Visual/manual-save extension point
- File: `tests/ui-admin-notion-visual-state.spec.ts:33`
- Why it is relevant: this file already validates the save-chip styling for `Saved` and `Error`.
- Recommended assertions:
  - Extend it to capture the `Saving...` visual class before success/failure.
  - Prefer using the new manual-save shortcut path for one branch, so the keyboard-triggered save shares the same visual treatment as autosave.
- Priority: medium.

## Brittle Selectors And Timing Assumptions
- `tests/ui-admin-notion-autosave-info.spec.ts:27`, `tests/admin-blog-edit.spec.ts:74`, `tests/ui-admin-notion-visual-state.spec.ts:61`, `tests/ui-admin-semantic-colors.spec.ts:50`
  - These rely on `.tiptap.ProseMirror`. That is implementation-coupled class selection, not a stable testing surface.
  - Risk: editor DOM/class refactors break tests even if behavior is unchanged.
- `tests/ui-admin-notion-autosave-info.spec.ts:59` and `:153`
  - `page.waitForTimeout(900)` is being used to force a visible `Saving...` state.
  - Risk: this slows the suite and couples behavior verification to an arbitrary sleep rather than request boundaries.
- `tests/ui-admin-notion-autosave-info.spec.ts:114`
  - `page.waitForTimeout(1_500)` is a weak proxy for “revalidation did not fire again.”
  - Risk: it can pass or fail for scheduler reasons unrelated to the intended throttle behavior.
- `tests/ui-admin-notion-autosave-info.spec.ts:34`, `tests/admin-blog-edit.spec.ts:80`, `tests/ui-admin-notion-visual-state.spec.ts:63`, `tests/ui-admin-semantic-colors.spec.ts:52`
  - These type into the editor character-by-character with `page.keyboard.type(...)`.
  - Risk under a `~1s` debounce: slow CI or longer strings can accidentally let autosave trigger mid-entry. Matching the `PUT` request body to the final text becomes more important.
- `src/test/blog-notion-workspace.test.tsx:189` and `:196`
  - Fake-timer steps are currently precise to the old `10_000ms` value.
  - Risk: they will fail immediately once the debounce is shortened.

## Open Questions
- The manual-save shortcut needs a test-visible contract: does `Ctrl+S` save content only, or content plus dirty metadata fields (`title`, `tags`, `published`) in the Notion workspace?
- If shortcut save is intended to be “immediate autosave,” the tests should assert the content mutation path. If it is intended to behave like the metadata button, the tests should assert immediate revalidation too.

## Validation Performed
- Reviewed current Notion workspace implementation in `src/components/admin/BlogNotionWorkspace.tsx`.
- Reviewed current Vitest coverage in `src/test/blog-notion-workspace.test.tsx`.
- Reviewed current Playwright coverage in:
  - `tests/ui-admin-notion-autosave-info.spec.ts`
  - `tests/admin-blog-edit.spec.ts`
  - `tests/ui-admin-notion-visual-state.spec.ts`
  - `tests/ui-admin-semantic-colors.spec.ts`
  - `tests/ui-admin-notion-client-switch.spec.ts`
  - `tests/ui-admin-notion-library-sheet.spec.ts`
- Searched repo-wide for Notion/autosave/save-state/keyboard-save related coverage and selectors.
- Verified plugin cache absolute-path prerequisites before inspection work.

## Risks / Deferred Follow-up
- If the implementation adds `Ctrl+S` without a stable editor test id, Playwright will still depend on `.tiptap.ProseMirror`.
- If the debounce becomes `~1s` but tests still use `page.keyboard.type(...)` with longer strings, flakes can appear as partial-content saves.
- If `Saving...` becomes too transient at the faster cadence, state assertions should be tied to intercepted request timing instead of raw visible duration.

## Final Recommendation
- Update `src/test/blog-notion-workspace.test.tsx` and `tests/ui-admin-notion-autosave-info.spec.ts` first.
- Extend `tests/ui-admin-notion-visual-state.spec.ts` for the `Saving...` keyboard-save path if visual parity matters.
- Treat fixed sleeps and `.tiptap.ProseMirror` selectors as the first cleanup targets while making the save-behavior test changes.
