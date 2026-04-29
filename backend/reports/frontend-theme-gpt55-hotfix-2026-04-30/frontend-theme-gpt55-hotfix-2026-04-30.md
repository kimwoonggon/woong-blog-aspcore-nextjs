# Audit Report: Theme + AI Hotfix (System theme + AI fix dialog + gpt-5.5)
Date: 2026-04-30

## 1) What changed
- System theme default behavior
  - `src/components/providers/ThemeProvider.tsx`
    - `defaultTheme` from `light` to `system`
    - `enableSystem` enabled
  - `src/components/ui/ThemeToggle.tsx`
    - toggle display now uses `resolvedTheme` when current theme is `system`
- AI dialog UX stability
  - `src/components/admin/AIFixDialog.tsx`
    - Added reset of transient dialog state (`loading`, `fixedContent`) on close
    - Ensures `Start AI Fix` path remains available on reopen
- AI model config (`gpt-5.5`)
  - `backend/src/WoongBlog.Api/appsettings.json`
    - `CodexModel` set to `gpt-5.5`
    - `CodexAllowedModels` includes `gpt-5.5`
  - `backend/src/WoongBlog.Infrastructure/Ai/AiOptionsPostConfigure.cs`
    - default fallback model set to `gpt-5.5`
    - allowed list fallback includes `gpt-5.5`
  - `backend/src/WoongBlog.Application/Modules/AI/AiOptions.cs`
    - default `CodexModel` set to `gpt-5.5`
  - `src/components/admin/AIFixDialog.tsx`
    - fallback model defaults to `gpt-5.5`
  - `src/components/admin/AdminBlogBatchAiPanel.tsx`
    - fallback model defaults to `gpt-5.5`

## 2) What was intentionally not changed
- No backend API routes, DTOs, DB schema, auth, or permission logic were changed.
- No UI redesign outside the affected components.
- No test suite changes beyond necessary execution.

## 3) Validation against goals
- Browser OS theme as default source: applied in provider and toggle.
- AI dialog start action visibility issue addressed by state reset on close.
- `gpt-5.5` introduced in runtime config defaults and frontend fallback defaults.

## 4) Validations performed
- Frontend tests
  - `npm run test -- src/test/admin-ai-fix-dialog.test.tsx src/test/admin-blog-batch-ai-panel.test.tsx`
  - Result: **2 test files passed, 28 tests passed**
- Backend tests
  - `dotnet test backend/WoongBlog.sln`
  - Result: pass on full suite
    - ComponentTests: 114 passed
    - UnitTests: 56 passed
    - ArchitectureTests: 35 passed
    - IntegrationTests: 196 passed
    - ContractTests: skipped (no pact input configured)
    - Warnings only: low severity `NU1901` (AWSSDK.Core) observed during restore

## 5) Risks / yellow flags / deferred follow-ups
- Two low-priority warnings from `NU1901` remain unchanged.
- Playwright/e2e browser validation was not executed in this pass.
- Backup directories generated during this hotfix are kept locally and should not be committed.

## 6) Final recommendation
- Push verified changes to `dev` branch.
- Merge into `main` only after this commit lands on `dev` without regressions.
