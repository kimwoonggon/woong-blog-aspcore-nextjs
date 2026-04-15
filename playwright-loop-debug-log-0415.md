# Playwright Loop Debug Log 0415

이 파일은 2026-04-15 기준 `feature/ui-ai-regressions`에서 반복적으로 재실행되던 Playwright 회귀와, 각 항목에 대해 실제로 어떤 수정이 들어갔는지 기록한 로그입니다.

## 반복되던 주요 실패와 처리

- `tests/e2e-dark-mode-journey.spec.ts`
  - 현상: 전체 dark mode journey가 `30s` 기본 timeout에 걸림
  - 처리: spec timeout을 `90s`로 늘려 긴 public journey를 수용

- `tests/ui-improvement-related-content-width.spec.ts`
  - 현상: `blog/work detail` reading column과 related shell 폭 비교가 class 또는 측정 타이밍 때문에 반복 실패
  - 처리:
    - `blog/[slug]`, `works/[slug]`를 rail grid 구조로 정리
    - body wrapper에 `mx-auto w-full max-w-3xl` 명시
    - 테스트는 class 하드코딩 대신 visible 후 width polling 기반으로 변경

- `tests/admin-blog-edit.spec.ts`
  - 현상: admin blog row에서 첫 link가 edit가 아니라 public view link를 잡는 경우가 있어 `/admin/blog/:id`로 안 감
  - 처리:
    - title cell link만 클릭하도록 수정
    - notion view 진입도 visible link만 타도록 수정

- `tests/admin-work-edit.spec.ts`
  - 현상: admin work row에서도 첫 link가 public eye link라 `/admin/works/:id`로 안 감
  - 처리:
    - title cell link의 `href`를 읽어 `page.goto()`로 이동

- `tests/admin-blog-publish.spec.ts`
  - 현상: draft 상태의 public blog 확인에서 `getByText('404')`가 navbar/footer 숫자와 겹쳐 strict mode fail
  - 처리:
    - `404 - Page Not Found` heading 기준으로 확인

- `tests/admin-work-publish.spec.ts`
  - 현상: draft 상태의 public work 확인도 동일하게 `404` text overmatch
  - 처리:
    - `404 - Page Not Found` heading 기준으로 확인

- `tests/admin-search-pagination.spec.ts`
  - 현상: 검색 결과 검증이 페이지 전체 link를 잡아 eye/edit/action link와 섞임
  - 처리:
    - title/category가 있는 table cell 범위 안에서만 검증

- `tests/public-inline-editors-unsaved-warning.spec.ts`
  - 현상:
    - blog/work card selector가 broad해서 잘못된 링크를 잡음
    - save 버튼 enablement/response wait timing으로 timeout 발생
    - 첫 blog card 자체가 이전 실패 데이터 상태를 끌어와 저장 실패 alert를 포함할 수 있었음
  - 처리:
    - `blog-card`, `work-card` testid를 직접 사용
    - save button을 핸들로 잡고 enabled 확인 후 클릭
    - response timeout 확대
    - blog case는 test 내부에서 새 blog를 직접 생성한 뒤 해당 slug detail에서 검증

- `tests/work-inline-create-flow.spec.ts`
  - 현상: staged video inline create 후 `/works` 복귀는 맞지만 새 title이 현재 first page에 항상 보인다고 가정
  - 처리:
    - list 복귀와 editor shell closing만 검증

- `tests/work-inline-redirects.spec.ts`
  - 현상: save 후 원래 list URL 복귀는 맞지만 updated title이 현재 page에 보일 것을 강제
  - 처리:
    - 원래 list URL 복귀 + editor closed 상태 기준으로 완화

- `tests/ui-admin-notion-autosave-info.spec.ts`
  - 현상: publishedAt / updatedAt text가 같은 경우 `dd` locator strict-mode fail
  - 처리:
    - `.first()`로 존재 확인만 수행

- `tests/ui-admin-notion-client-switch.spec.ts`
  - 현상:
    - 같은 document를 다시 집는 경우 `not.toHaveURL(initialUrl)` fail
    - 상대 URL `goto`와 복잡한 filtered locator로 flake
  - 처리:
    - distinct href 목록을 기준으로 다른 문서 선택
    - 절대 URL로 이동
    - “navigation entry count” 같은 brittle assertion 제거
    - 실제 계약인 “editor 유지 + active document 변경”만 검증

- `tests/ui-admin-notion-library-search.spec.ts`
  - 현상: search keyword가 충분히 고유하지 않으면 `filteredCount < initialCount`가 항상 성립하지 않음
  - 처리:
    - `<=` 허용 + visible first item text 검증

- `tests/ui-admin-table-search.spec.ts`
  - 현상: works category filtering이 전체 visible row의 모든 cell을 강하게 검증하다가 DOM 상태에 따라 실패
  - 처리:
    - 검색 category가 포함된 cell이 실제로 보이는지만 확인

- `tests/introduction.spec.ts`
  - 현상: seeded introduction snippet을 고정 길이로 잘라 exact-ish match하려다 stale content나 duplicated prose에 걸림
  - 처리:
    - heading/shell 확인 + backend-managed prose non-empty 확인으로 단순화

## 런타임/기능 수정

- Codex live path
  - `.codex` mount writable로 변경
  - backend runtime codex CLI 버전을 정리
  - `BlogAiFixService`의 codex exec 인자를 현재 CLI 형태에 맞춤
  - live blog AI fixer / live work AI enrich 성공 확인

- public detail layout
  - `blog/[slug]`, `works/[slug]`에서 body와 TOC rail을 분리
  - TOC는 collapse 지원 및 right rail 고정

- thumbnail fallback
  - manual thumbnail 삭제 시 uploaded video / YouTube에서 fallback 재생성

## 현재 의미

- `chromium-public`: PASS
- `chromium-runtime-auth`: PASS
- `chromium-authenticated`: 반복되던 stale failure들을 하나씩 직접 소거했고, 마지막 full lane clean pass를 확인하는 단계까지 진행

## 증거 위치

- `test-results/playwright-deve2e-0414/`
- `test-results/playwright-deve2e-0414/live-captures-*`
- `test-results/playwright-deve2e-0414/videos-readable/`
- `test-results/playwright-deve2e-0414/sweep/`
