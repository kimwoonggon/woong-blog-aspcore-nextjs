# UI/UX 개선 TODO — Admin Execution Ledger

기준 문서:
- `ui-improvement-todolist-admin-260411.md`

운영 규칙:
- 모든 구현은 이 파일에 먼저 기록한다.
- 각 항목은 `테스트 작성/실행 -> 구현 -> headed Playwright 성공 -> 체크` 순서로 진행한다.
- Admin 대상 테스트 파일은 `tests/ui-admin-*.spec.ts` 네이밍을 사용한다.
- Admin 대상 Playwright 검증은 `chromium-authenticated` + `https://localhost` + headed 기준으로 한다.
- 기존 `tests/admin-*.spec.ts` 회귀를 깨지 않게 유지한다.

## Infra
- [x] `INFRA-0-1` `ui-admin-*.spec.ts`가 `chromium-authenticated`에 포함되도록 Playwright 매칭 확장
- [x] `INFRA-0-2` `https://localhost` headed admin 검증 명령 고정

## Phase 0 — Admin Sidebar
- [x] `ADM-0-1` 사이드바 너비 축소 (`w-80` -> `w-64`)
  - 테스트: `tests/ui-admin-sidebar-width.spec.ts`
- [x] `ADM-0-2` 사이드바 nav active state 추가
  - 파일: `src/components/admin/AdminSidebarNav.tsx` 신규
  - 테스트: `tests/ui-admin-sidebar-active.spec.ts`
- [x] `ADM-0-3` `Public Home` / `Open Site` 중복 링크 정리
  - 테스트: `tests/ui-admin-sidebar-links.spec.ts`
- [x] `ADM-0-4` 사이드바 하드코딩 색상 -> 시맨틱 토큰
  - 테스트: `tests/ui-admin-semantic-colors.spec.ts`
- [x] `ADM-0-5` 사이드바 설명 텍스트 간소화
  - 테스트: `tests/ui-admin-sidebar-text.spec.ts`

## Phase 1 — Notion View
- [x] `ADM-1-1` Blog Library를 Sheet로 전환
  - 테스트: `tests/ui-admin-notion-library-sheet.spec.ts`
- [x] `ADM-1-2` 문서 전환을 client-side로 변경
  - 테스트: `tests/ui-admin-notion-client-switch.spec.ts`
- [x] `ADM-1-3` Blog Library 검색 기능 추가
  - 테스트: `tests/ui-admin-notion-library-search.spec.ts`
- [x] `ADM-1-4` Capability hint dismiss 추가
  - 테스트: `tests/ui-admin-notion-hint-dismiss.spec.ts`
- [x] `ADM-1-5` Cmd+S / Ctrl+S 저장 단축키
  - 테스트: `tests/ui-admin-editor-keyboard-save.spec.ts`
- [x] `ADM-1-6` Doc Information 패널 토글
  - 테스트: `tests/ui-admin-notion-doc-info-toggle.spec.ts`

## Phase 2 — Blog / Works Editor
- [x] `ADM-2-1` BlogEditor excerpt 필드 추가
  - 테스트: `tests/ui-admin-blog-excerpt.spec.ts`
- [x] `ADM-2-2` Published 체크박스 위치 변경
  - 테스트: `tests/ui-admin-blog-published-position.spec.ts`
- [x] `ADM-2-3` Save 버튼 디자인 통일
  - 테스트: `tests/ui-admin-save-btn.spec.ts`
- [x] `ADM-2-4` 저장하지 않은 변경 경고 (`beforeunload`)
  - 테스트: `tests/ui-admin-unsaved-warning.spec.ts`
- [x] `ADM-2-5` WorkEditor 탭 레이아웃
  - 테스트: `tests/ui-admin-work-editor-tabs.spec.ts`
- [x] `ADM-2-6` Flexible Metadata 구조화 UI
  - 테스트: `tests/ui-admin-work-metadata-ui.spec.ts`

## Phase 3 — Admin Tables
- [x] `ADM-3-1` 삭제 확인 `AlertDialog` 전환
  - 테스트: `tests/ui-admin-delete-dialog.spec.ts`
- [x] `ADM-3-2` 제목 외 태그/카테고리 검색
  - 테스트: `tests/ui-admin-table-search.spec.ts`
- [x] `ADM-3-3` 테이블 UI 언어 영어 통일
  - 테스트: `tests/ui-admin-table-lang.spec.ts`

## Phase 4 — Tiptap
- [x] `ADM-4-1` 툴바 sticky 처리
  - 테스트: `tests/ui-admin-tiptap-sticky-toolbar.spec.ts`
- [x] `ADM-4-2` 링크 삽입 `Popover` 전환
  - 테스트: `tests/ui-admin-tiptap-link-popover.spec.ts`
- [x] `ADM-4-3` 에디터 하드코딩 색상 -> 시맨틱 토큰
  - 테스트: `tests/ui-admin-tiptap-semantic.spec.ts`

## Verification Log
- [x] 각 항목별 Playwright 성공 기록
- [x] `NEXT_DIST_DIR=.next-build npm run build` 성공 기록
- [x] 기존 `tests/admin-*.spec.ts --project=chromium-authenticated` 회귀 기록
- [x] 신규 `tests/ui-admin-*.spec.ts --project=chromium-authenticated` 전체 기록
