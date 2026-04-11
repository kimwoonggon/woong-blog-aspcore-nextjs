# UI Improvement Admin Master Todo

기준 문서:
- `ui-improvement-todolist-admin-260411.md`
- `ui-improvement-todolist-public-260411.md`
- `todolist.md`

운영 원칙:
- 항상 이 파일을 먼저 갱신한다.
- public 세부 구현/테스트 체크는 `todolist.md`에 기록한다.
- 문서 원문에 있는 항목과 구현 중 새로 발견한 항목을 모두 기록한다.
- 각 작업은 TDD 순서로 진행한다: 대상 테스트 확인 또는 작성 → 최소 수정 → HTTPS headed 검증 → 체크리스트 갱신.

## Execution Rules
- [x] `todolist-admin.md`와 `todolist.md`를 현재 워킹트리 기준으로 동기화
- [x] 각 public 작업은 관련 테스트 파일과 검증 명령을 함께 기록
- [x] 각 public 작업은 `https://localhost` + headed Playwright로 확인
- [ ] 각 Phase 종료 후 체크리스트와 실제 상태를 다시 맞춤
- [x] 최종 검증에 `lint`, `typecheck`, `build`, public HTTPS headed 배치 테스트 포함

## Current Baseline
- [x] 기존 public 개선 작업이 워킹트리에 다수 존재함
- [x] `todolist.md`가 이미 존재하며 public 작업 기록 일부 포함
- [x] HTTPS 실행 표준 경로 확인
  - `./scripts/run-local-https.sh`
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=https://localhost`
- [x] `web-design-guidelines` 기준 fresh source 확인
- [x] `ui-ux-pro-max`, `tdd` 지침 확인

## Discovered Gaps
- [x] `tests/ui-improvement-static-public-pages.spec.ts` 기대사항과 현재 구현 불일치
  - 반영: `data-testid="static-public-shell"`
  - 반영: `data-testid="resume-shell"`
- [x] `todolist.md`와 실제 public 변경 상태 재동기화
- [ ] public 날짜 포맷 로직이 여러 페이지에 분산되어 있음
- [x] static public / resume shell 규약이 코드 레벨에서 명시됨
- [x] HTTPS runtime bootstrap blocker
  - 원인: `SeedData`가 기존 `WorkVideos` slot을 재사용하지 못해 startup 중복 예외 발생
  - 조치: 기존 slot 우선 재사용 + bootstrap 테스트 추가

## Phase Ledger
- [x] P0 체크리스트 정리
  - [x] `todolist-admin.md` 생성
  - [x] `todolist.md` 재정렬
- [x] P1 static public shell 회귀 수습
  - [x] 관련 테스트 red 확인
  - [x] 구현 수정
  - [x] HTTPS headed 검증
- [x] P2 public UI guideline 갭 정리
  - [x] 연락 CTA 안정화
  - [x] related shell width 정렬
  - [x] featured works breakpoint 정렬
  - [x] 필요한 테스트 추가/재검증
  - [x] HTTPS headed 검증
- [x] P3 전체 public 검증
  - [x] source eslint
  - [x] `npm run typecheck`
  - [x] `NEXT_DIST_DIR=.next-build npm run build`
  - [x] public 대상 HTTPS headed Playwright 배치

## Evidence Log
- [x] HTTPS stack bootstrap 결과 기록
  - `./scripts/run-local-https.sh`
  - `curl -k https://localhost/api/health` => `{"status":"ok",...}`
- [x] per-task Playwright 명령과 결과 기록
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=https://localhost PLAYWRIGHT_HEADED=1 npx playwright test tests/ui-improvement-static-public-pages.spec.ts --project=chromium-public --headed --workers=1`
    - 결과: `2 passed`
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=https://localhost PLAYWRIGHT_HEADED=1 npx playwright test tests/ui-improvement-featured-works-grid.spec.ts tests/ui-improvement-related-content-width.spec.ts --project=chromium-public --headed --workers=1`
    - 결과: `8 passed`
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=https://localhost PLAYWRIGHT_HEADED=1 npx playwright test tests/ui-pub-*.spec.ts --project=chromium-public --headed --workers=1`
    - 결과: `56 passed`
- [x] 최종 artifact index 갱신 여부 기록
  - `npm run test:e2e:artifacts:index`
  - 결과: `test-results/playwright/summary/latest-upload-artifacts.md` 갱신
