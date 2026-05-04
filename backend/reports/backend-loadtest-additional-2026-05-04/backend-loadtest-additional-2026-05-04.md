# Backend Loadtest Additional Audit (2026-05-04)

- Task slug: `backend-loadtest-additional-2026-05-04`
- Date: `2026-05-05`
- Branch: `dev`
- Objective source: `2026-05-04-backend-loadtest-additional.md`

## 실행 요약

- Playwright full core run was executed and completed with `434` tests.
- Focused replay was executed for the requested subset.
- 결과는 **`full: 416 / 14 / 4`**, **`repro: 30 / 7 / 0`**으로 0 failed 목표는 미달.
- CI 체인은 `CI Dev -> Promote Main Runtime -> release/main-promote -> CI Main Runtime`에서 확인됨.

## 실행 항목 이행

### 1) Playwright full core 실행

- Command
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible PLAYWRIGHT_E2E_PROFILE=core node scripts/run-e2e-latency.mjs -- --workers=1`
- Result
  - Failed (`exit code 1`)
  - Pass/Fail/Skipped: `416 / 14 / 4`
- Failure count: 14
  - `tests/public-detail-toc-fallback.spec.ts:6`
  - `tests/ui-improvement-featured-works-grid.spec.ts:141`
  - `tests/ui-improvement-featured-works-grid.spec.ts:200`
  - `tests/admin-blog-ai-dialog.spec.ts:29`
  - `tests/admin-blog-ai-dialog.spec.ts:90`
  - `tests/admin-blog-ai-dialog.spec.ts:156`
  - `tests/admin-search-pagination.spec.ts:344`
  - `tests/admin-work-special-input.spec.ts:6`
  - `tests/dark-mode.spec.ts:446`
  - `tests/ui-admin-keyboard-accessibility.spec.ts:45`
  - `tests/ui-admin-keyboard-accessibility.spec.ts:66`
  - `tests/ui-quality-layout-rhythm.spec.ts:7`
  - `tests/ui-quality-visual-metrics.spec.ts:61`
  - `tests/public-admin-affordances.spec.ts:64`
- Evidence
  - `backend/reports/backend-loadtest-additional-2026-05-04/reruns/full-core-latest.log`
  - `backend/reports/backend-loadtest-additional-2026-05-04/reruns/full-core-20260504T155539Z.log`
  - `backend/reports/backend-loadtest-additional-2026-05-04/reruns/full-core-20260504T155539Z-summary.json`
  - `backend/reports/backend-loadtest-additional-2026-05-04/reruns/full-core-20260504T155539Z-summary.md`
  - `backend/reports/backend-loadtest-additional-2026-05-04/reruns/full-core-20260504T155539Z.exitcode`

### 2) 실패 케이스 코드스캔 및 분류

- Full run 14건을 기준으로 재현 집합에서 재실행
- 재현 실패 7건
  - `tests/ui-improvement-featured-works-grid.spec.ts:141`
  - `tests/ui-improvement-featured-works-grid.spec.ts:200`
  - `tests/admin-blog-ai-dialog.spec.ts:29`
  - `tests/admin-blog-ai-dialog.spec.ts:90`
  - `tests/admin-blog-ai-dialog.spec.ts:156`
  - `tests/ui-admin-keyboard-accessibility.spec.ts:45`
  - `tests/ui-admin-keyboard-accessibility.spec.ts:66`
- 비재현 후보 7건
  - `tests/public-detail-toc-fallback.spec.ts:6`
  - `tests/admin-search-pagination.spec.ts:344`
  - `tests/admin-work-special-input.spec.ts:6`
  - `tests/dark-mode.spec.ts:446`
  - `tests/ui-quality-layout-rhythm.spec.ts:7`
  - `tests/ui-quality-visual-metrics.spec.ts:61`
  - `tests/public-admin-affordances.spec.ts:64`

### 3) 재현 루프 실행 (`spec` 집합)

- Command
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible PLAYWRIGHT_E2E_PROFILE=core node scripts/run-e2e-latency.mjs -- --workers=1 tests/public-blog-inline-redirects.spec.ts tests/ui-improvement-featured-works-grid.spec.ts tests/admin-blog-ai-dialog.spec.ts tests/admin-blog-edit.spec.ts tests/ui-admin-keyboard-accessibility.spec.ts tests/ui-admin-tiptap-link-popover.spec.ts tests/ui-quality-visual-metrics.spec.ts tests/work-inline-create-flow.spec.ts`
- Result
  - Failed (`exit code 1`)
  - Pass/Fail/Skipped: `30 / 7 / 0`
- Evidence
  - `backend/reports/backend-loadtest-additional-2026-05-04/reruns/repro-latest.log`
  - `backend/reports/backend-loadtest-additional-2026-05-04/reruns/repro-core-20260504T161519Z.log`
  - `backend/reports/backend-loadtest-additional-2026-05-04/reruns/repro-core-20260504T161519Z.exitcode`

### 4) CI chain 재확인

- `CI Dev`
  - `25322888983` → `https://github.com/kimwoonggon/woong-blog-aspcore-nextjs/actions/runs/25322888983` (`success`)
- `Promote Main Runtime`
  - `25323185860` (`workflow_run`) → `https://github.com/kimwoonggon/woong-blog-aspcore-nextjs/actions/runs/25323185860` (`success`)
- `release/main-promote -> main` PR
  - `#35` (`Promote runtime-only tree: dev -> main`) → `https://github.com/kimwoonggon/woong-blog-aspcore-nextjs/pull/35` (`MERGED`, `2026-05-04T14:03:11Z`)
- `CI Main Runtime`
  - `25323196924` (PR) → `https://github.com/kimwoonggon/woong-blog-aspcore-nextjs/actions/runs/25323196924` (`success`)
  - `25323508573` (main push) → `https://github.com/kimwoonggon/woong-blog-aspcore-nextjs/actions/runs/25323508573` (`success`)

## 목표 대비 검증 여부

- 완료: Playwright full 실행 + 재현 루프 + full/repro 실패 분류 + CI 체인 점검 + 산출물 정합성 반영.
- 미완료: full core suite green (`0 failed`) 목표.

## 의도치 않거나 미해결 리스크

- `admin-blog-ai-dialog` 3건은 재현성이 확인되어 strict selector/다이얼로그 버튼 경합 정리가 필요.
- `ui-admin-keyboard-accessibility` 2건은 포커스 이동 경로/키보드 순서 안정화가 필요.
- full run 추가 7건은 환경/타이밍/타입 동작 의존성으로 보이는 비재현군.
- 1개 budget failure, 39개의 warning은 full run에서 검증 대상은 아니지만 성능 여력 포인트로 유지 관리 필요.

## 다음 액션

1. 재현 실패 7건의 테스트 픽스 범위를 좁혀 selector·포커스 경로 안정화.
2. 동일 대상의 focused rerun 재실행으로 우선순위 정합성 재확인.
3. full core 재실행 후 감소 폭을 측정해 다음 추가 TODO/이행 계획 반영.

## 산출물 경로

- `backend/reports/backend-loadtest-additional-2026-05-04/backend-loadtest-additional-2026-05-04.md`
- `backend/reports/backend-loadtest-additional-2026-05-04/backend-loadtest-additional-2026-05-04.html`
- `backend/reports/backend-loadtest-additional-2026-05-04/backend-loadtest-additional-2026-05-04.json`
