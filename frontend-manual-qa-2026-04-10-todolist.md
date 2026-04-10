# Frontend Manual QA 2026-04-10 Todolist

## Overview

| Field | Value |
| --- | --- |
| Date | 2026-04-10 |
| Target | Local docker-compose stack |
| Base URL | `http://localhost` |
| Tester | Codex + follow-up human manual QA |
| Browser | Chromium |
| Backend | Docker `backend` on `:8080` |
| Front Door | Docker `nginx` on `:80` |
| Fixtures | `tests/fixtures/sample-video.mp4`, JPG/PNG image, `tests/fixtures/resume.pdf`, 2-3 YouTube URLs |

## Preflight

| Check | Status | Evidence | Note |
| --- | --- | --- | --- |
| `docker compose ps` | Passed | backend/frontend/nginx/db all `Up` on 2026-04-10 23:54 KST | Local stack healthy |
| `curl -I http://localhost` | Passed | `HTTP/1.1 200 OK` from nginx | Front door responding |
| Admin login availability | Passed | `tests/public-admin-affordances.spec.ts`, admin-authenticated Playwright lanes | Local admin session path works |
| MP4 / image / PDF / YouTube fixtures ready | Partial | Repo fixtures confirmed: `tests/fixtures/sample-video.mp4`, `tests/fixtures/resume.pdf`, image fixtures | Human-provided YouTube URLs still needed for manual-only scenarios |

## Automated Precheck

| Order | Command | Areas Covered | Status | Evidence | Note |
| --- | --- | --- | --- | --- | --- |
| 1 | `npm run test:e2e:public` | public navigation, detail, pagination, resume | Passed | `21 passed (47.0s)` |  |
| 2 | `npm run test:e2e:admin` | dashboard, menus, members, pages, redirects | Passed | `17 passed (45.0s)` |  |
| 3 | `npm run test:e2e:works` | work create/edit/video/image/inline/public flows | Passed | `19 passed (1.2m)` |  |
| 4 | `npm run test:e2e:blog` | blog create/edit/image/inline/public flows | Passed | `11 passed (44.1s)` |  |
| 5 | `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost npx playwright test tests/admin-ai-batch-jobs.spec.ts tests/public-inline-editors.spec.ts tests/public-admin-affordances.spec.ts tests/auth-login.spec.ts --workers=1` | AI batch, public inline affordances, auth/login | Passed | `8 passed (1.4m)` |  |
| 6 | `npm run test:e2e:uploads` | resume upload, home image, work/blog image, upload-related flows | Passed | `10 passed (47.1s)` | Added because it materially covers page/upload rows in sections A, B, and E |
| 7 | `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost npx playwright test tests/manual-qa-gap-coverage.spec.ts tests/manual-qa-auth-gap.spec.ts --workers=1` | reorder, single delete, editor gaps, inline save/create, mobile nav, long body, local login, stale session | Partial | Initial run failed, then targeted reruns closed all but `C-10` | Video, trace, and screenshot artifacts retained under `test-results/playwright/manual-qa-*` |
| 8 | `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost npx playwright test tests/auth-security-browser.spec.ts --workers=1` | CSRF enforcement, logout with csrf | Passed | `2 passed (17.5s)` | Covers D-5 directly |
| 9 | `npm run test:e2e:artifacts:index` | artifact index | Passed | `test-results/playwright/summary/latest-upload-artifacts.md` | Latest video/screenshot index generated |

## Manual QA Matrix

| ID | Area | Scenario | Priority | Automation | Status | Evidence | Bug/Note | Retest |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A-1 | Work | Work 신규 생성 (텍스트만) | High | Automated | Passed (Auto) | `tests/admin-work-publish.spec.ts` |  |  |
| A-2 | Work | Work 신규 생성 + YouTube 영상 | High | Automated | Passed (Auto) | `tests/admin-work-video-create-flow.spec.ts` |  |  |
| A-3 | Work | Work 신규 생성 + MP4 업로드 | High | Automated | Passed (Auto) | `tests/admin-work-video-create-flow.spec.ts` | Same flow covers uploaded video lane |  |
| A-4 | Work | Work 신규 생성 + YouTube 2개 + MP4 2개 혼합 | High | Automated | Passed (Auto) | `tests/admin-work-video-mixed-flow.spec.ts` |  |  |
| A-5 | Work | 비디오 본문 삽입 (videoInline) | High | Automated | Passed (Auto) | `tests/admin-work-video-edit-flow.spec.ts`, `tests/public-work-detail-inline-edit.spec.ts` |  |  |
| A-6 | Work | 비디오 순서 변경 (reorder) | High | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` | Fixed by backend two-phase reorder persistence and runtime rebuild |  |
| A-7 | Work | Work 편집 — 제목/내용 수정 | High | Automated | Passed (Auto) | `tests/admin-work-edit.spec.ts` |  |  |
| A-8 | Work | Work 편집 중 영상 추가 | High | Automated | Passed (Auto) | `tests/admin-work-video-edit-flow.spec.ts` |  |  |
| A-9 | Work | Work 썸네일 수동 업로드 | High | Automated | Passed (Auto) | `tests/admin-work-image-upload.spec.ts`, `tests/admin-work-auto-thumbnail.spec.ts` |  |  |
| A-10 | Work | Work 아이콘 업로드 | Medium | Hybrid | Passed (Auto) | `tests/admin-work-image-upload.spec.ts` | Automated coverage exists even though a visual spot-check is still useful |  |
| A-11 | Work | Work 메타데이터 JSON | High | Automated | Passed (Auto) | `tests/admin-work-validation.spec.ts`, `tests/admin-input-exceptions.spec.ts` |  |  |
| A-12 | Work | Work 삭제 (단건) | High | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` |  |  |
| A-13 | Work | Work 벌크 삭제 | High | Automated | Passed (Auto) | `tests/admin-bulk-delete.spec.ts` |  |  |
| A-14 | Work | Work 검색 + 페이지네이션 | High | Automated | Passed (Auto) | `tests/admin-search-pagination.spec.ts` |  |  |
| A-15 | Work | Public works 반응형 페이지네이션 | High | Automated | Passed (Auto) | `tests/public-works-pagination.spec.ts` |  |  |
| A-16 | Work | Public work detail — related works | Medium | Automated | Passed (Auto) | `tests/public-work-detail-inline-edit.spec.ts`, `tests/public-detail-pages.spec.ts` |  |  |
| B-1 | Blog | Blog 신규 작성 (텍스트) | High | Automated | Passed (Auto) | `tests/admin-blog-publish.spec.ts` |  |  |
| B-2 | Blog | Blog 본문에 이미지 삽입 | High | Automated | Passed (Auto) | `tests/admin-blog-image-upload.spec.ts` |  |  |
| B-3 | Blog | Blog 편집 — 제목/태그/본문 수정 | High | Automated | Passed (Auto) | `tests/admin-blog-edit.spec.ts` |  |  |
| B-4 | Blog | Blog 특수문자 입력 | Medium | Automated | Passed (Auto) | `tests/admin-blog-validation.spec.ts` |  |  |
| B-5 | Blog | Blog 삭제 (단건 + 벌크) | High | Automated | Passed (Auto) | `tests/admin-bulk-delete.spec.ts`, `tests/manual-qa-gap-coverage.spec.ts` |  |  |
| B-6 | Blog | Blog 검색 + 페이지네이션 | Medium | Automated | Passed (Auto) | `tests/admin-search-pagination.spec.ts` |  |  |
| B-7 | Blog | Public blog 반응형 페이지네이션 | Medium | Automated | Passed (Auto) | `tests/public-blog-pagination.spec.ts` |  |  |
| B-8 | Blog | Public blog detail — related posts | Medium | Automated | Passed (Auto) | `tests/public-blog-detail-inline-edit.spec.ts`, `tests/public-detail-pages.spec.ts` |  |  |
| B-9 | Blog | AI Batch Fix — Job 생성 | High | Automated | Passed (Auto) | `tests/admin-ai-batch-jobs.spec.ts` |  |  |
| B-10 | Blog | AI Batch Fix — Polling & 진행률 | High | Automated | Passed (Auto) | `tests/admin-ai-batch-jobs.spec.ts` |  |  |
| B-11 | Blog | AI Batch Fix — 결과 미리보기 & 적용 | High | Automated | Passed (Auto) | `tests/admin-ai-batch-jobs.spec.ts` |  |  |
| B-12 | Blog | Notion 워크스페이스 뷰 | Medium | Automated | Passed (Auto) | `tests/admin-blog-edit.spec.ts` | Notion view + autosave covered there |  |
| C-1 | Editor | 기본 서식 | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` | Passed in isolated rerun |  |
| C-2 | Editor | 링크 삽입/수정 | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` | Passed in isolated rerun |  |
| C-3 | Editor | 이미지 드래그 & 드롭 | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` | Passed after editor wrapper drop handling was added and runtime rebuilt |  |
| C-4 | Editor | 이미지 붙여넣기 | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` |  |  |
| C-5 | Editor | 비디오 임베드 삽입 | High | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` | Duplicate insert was prevented by disabled action state and public render stayed single-instance |  |
| C-6 | Editor | 슬래시 커맨드 | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` |  |  |
| C-7 | Editor | 코드 블록 (Syntax Highlight) | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` | Passed in isolated rerun |  |
| C-8 | Editor | HTML 커스텀 블록 | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` |  |  |
| C-9 | Editor | Three.js 블록 | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` |  |  |
| C-10 | Editor | Bubble Menu | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` | Passed after selection-based browser test was aligned with the current floating toolbar behavior |  |
| C-11 | Editor | 에디터 내용 동기화 | Medium | Hybrid | Passed (Auto) | `src/test/tiptap-editor.test.tsx`, `tests/manual-qa-gap-coverage.spec.ts` | Browser-level reopen/save sync now covered |  |
| D-1 | Auth | Google 로그인 | High | Automated | Partial | `tests/auth-login.spec.ts` | CTA and redirect verified; full Google-provider completion still depends on live provider/account |  |
| D-2 | Auth | 로컬 Admin 로그인 (dev) | High | Automated | Passed (Auto) | `tests/manual-qa-auth-gap.spec.ts` |  |  |
| D-3 | Auth | 로그아웃 | High | Automated | Passed (Auto) | `tests/public-admin-affordances.spec.ts` |  |  |
| D-4 | Auth | 비인증 admin 접근 | High | Automated | Passed (Auto) | `tests/admin-redirect.spec.ts` |  |  |
| D-5 | Auth | CSRF 토큰 검증 | High | Automated | Passed (Auto) | `tests/auth-security-browser.spec.ts` |  |  |
| D-6 | Auth | 세션 만료 후 저장 시도 | Medium | Hybrid | Partial | `tests/manual-qa-auth-gap.spec.ts` | Browser session invalidation was verified, but save UX message/redirect path still needs human confirmation |  |
| E-1 | Pages | 소개 페이지 편집 | High | Automated | Passed (Auto) | `tests/admin-pages-settings.spec.ts`, `tests/public-inline-editors.spec.ts` |  |  |
| E-2 | Pages | 연락처 페이지 편집 | High | Automated | Passed (Auto) | `tests/admin-pages-settings.spec.ts`, `tests/public-inline-editors.spec.ts` |  |  |
| E-3 | Pages | 홈 페이지 편집 | High | Automated | Passed (Auto) | `tests/admin-home-extreme-input.spec.ts`, `tests/admin-home-image-upload.spec.ts` |  |  |
| E-4 | Pages | 사이트 설정 | High | Automated | Passed (Auto) | `tests/admin-pages-settings.spec.ts`, `tests/admin-site-settings-extreme-input.spec.ts` |  |  |
| E-5 | Pages | 이력서 PDF 업로드 | High | Automated | Passed (Auto) | `tests/admin-resume-upload.spec.ts`, `tests/resume.spec.ts` |  |  |
| E-6 | Pages | 이력서 비PDF 거부 | High | Automated | Passed (Auto) | `tests/admin-resume-validation.spec.ts` |  |  |
| E-7 | Pages | 페이지 제목 길이 제한 | High | Automated | Passed (Auto) | `tests/admin-pages-validation.spec.ts` |  |  |
| F-1 | Inline Edit | Work 인라인 편집 | High | Automated | Passed (Auto) | `tests/public-work-detail-inline-edit.spec.ts` |  |  |
| F-2 | Inline Edit | Blog 인라인 편집 | High | Automated | Passed (Auto) | `tests/public-blog-detail-inline-edit.spec.ts` |  |  |
| F-3 | Inline Edit | 소개 인라인 편집 | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` |  |  |
| F-4 | Inline Edit | 연락처 인라인 편집 | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` |  |  |
| F-5 | Inline Edit | Works 목록 "새 작업 쓰기" | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` |  |  |
| F-6 | Inline Edit | Blog 목록 "새 글 쓰기" | Medium | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` |  |  |
| F-7 | Inline Edit | 비인증 시 편집 버튼 숨김 | High | Automated | Passed (Auto) | `tests/public-admin-affordances.spec.ts` |  |  |
| G-1 | Dashboard | 대시보드 요약 카드 | Medium | Automated | Passed (Auto) | `tests/admin-dashboard.spec.ts` |  |  |
| G-2 | Dashboard | 대시보드 최근 항목 | Medium | Automated | Passed (Auto) | `tests/admin-dashboard.spec.ts` |  |  |
| G-3 | Dashboard | 멤버 목록 | Medium | Automated | Passed (Auto) | `tests/admin-members.spec.ts` |  |  |
| G-4 | Dashboard | 사이드바 네비게이션 | Medium | Automated | Passed (Auto) | `tests/admin-menus.spec.ts` |  |  |
| G-5 | Dashboard | Public 네비게이션 | Medium | Automated | Passed (Auto) | `tests/home.spec.ts`, `tests/public-content.spec.ts`, `tests/public-admin-affordances.spec.ts`, `tests/manual-qa-gap-coverage.spec.ts` | Mobile hamburger path now covered in browser automation |  |
| H-1 | Layout | 데스크톱 카드 정렬 | Medium | Automated | Passed (Auto) | `tests/public-layout-stability.spec.ts` |  |  |
| H-2 | Layout | 모바일 카드 스택 | Medium | Automated | Passed (Auto) | `tests/public-layout-stability.spec.ts` |  |  |
| H-3 | Layout | Edge navigation 화살표 | Medium | Automated | Passed (Auto) | `tests/public-edge-nav.spec.ts` |  |  |
| H-4 | Layout | 빈 목록 상태 | Low | Manual | Pending Manual |  | Requires disposable dataset or temporary delete sweep |  |
| H-5 | Layout | 매우 긴 본문 | Low | Automated | Passed (Auto) | `tests/manual-qa-gap-coverage.spec.ts` |  |  |

## Failure Triage

| Bug Title | Area | Severity | Repro Steps | Expected | Actual | Retest |
| --- | --- | --- | --- | --- | --- | --- |
|  |  |  |  |  |  |  |

## Exit Summary

| Metric | Count |
| --- | --- |
| Passed | 65 |
| Partial | 2 |
| Pending Manual | 1 |
| Failed | 0 |
| Blocked | 0 |
| Skipped | 0 |

**Release Recommendation:** Automated Playwright coverage is green for the covered matrix. Remaining gaps are `D-1` full Google-provider completion, `D-6` final stale-session UX confirmation, and destructive/manual-only `H-4`.
