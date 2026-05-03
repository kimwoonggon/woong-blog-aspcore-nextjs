# Audit: Pact 세션 계약 테스트 방어 처리 (2026-05-04)

## 요약
- CI `main`에서 `frontend lint, types, and unit tests`가 실패한 이유가 된 Pact 소비자 테스트의 `GET /api/auth/session` 요청 미수신 이슈를 해결하기 위해,
  `src/test/pact/public-api-consumer.pact.test.ts`에 각 테스트 시작 시 글로벌 상태 정리를 추가함.
- 목적은 테스트 간 `fetch` 스텁/환경 오염을 제거해 Pact 테스트가 외부 모듈의 목 환경에서 벗어나
  실제 mock 서버 호출을 수행하도록 만드는 것.

## 변경 사항
1. **Pact 테스트 디스크리트화**
   - 파일: `src/test/pact/public-api-consumer.pact.test.ts`
   - 변경: `describe('public API consumer Pact contracts', () => { ... })` 블록에 `beforeEach`를 추가하여
     - `vi.unstubAllGlobals()`
     - `vi.unstubAllEnvs()`
   를 매 케이스 실행 전에 수행.
   - 의도: 이전 테스트가 남겨둔 `global.fetch` 스텁, 환경 변수 스텁을 제거해 `/api/auth/session` 요청이 Pact mock server로 실제 송신되도록 보장.

2. **실행 계획(TODO) 갱신**
   - 파일: `todolist-2026-05-04.md`
   - `CI Pact 계약 테스트 안정화` 항목 추가 및 재검증 내역 업데이트.

## 의도적으로 변경하지 않은 항목
- 본 이슈의 범위를 Pact 테스트 안정화에만 한정.
- 대시보드/로드 테스트 기능 코드, backend API 동작, Playwright 시나리오, 배포 파이프라인 자체는 변경 없음.

## 검증
- `npx vitest run --pool=threads --maxWorkers=1 src/test/pact/public-api-consumer.pact.test.ts`
  - 1 파일, 6 tests 통과.
- `npx vitest run --pool=threads --maxWorkers=1 src/test/public-responsive-feed.test.tsx src/test/pact/public-api-consumer.pact.test.ts`
  - 2 파일, 23 tests 통과.
- `npx vitest run --pool=forks --maxWorkers=1 src/test/pact/public-api-consumer.pact.test.ts`
  - 1 파일, 6 tests 통과.
- `npx eslint src/test/pact/public-api-consumer.pact.test.ts`
  - 오류 없음.

## 리스크/주의점
- `npm run test` 기본 설정(`--pool=threads`)에서의 Pact 단일 실행은 로컬 환경에서 간헐적으로 워커 시작 타임아웃이 보고되었음.
  (CI 환경에서는 `npm run test -- --run` 파이프라인으로 재검증 필요)
- 테스트 스위트에서 여전히 `vi.stubGlobal('fetch')`를 정리하지 않는 파일이 존재해,
  다른 파일 실행 순서에 따라 상호 영향이 날 가능성은 남아 있음.
  이번 수정은 해당 Pact 블록 내부에서 국소적으로 이를 방지.

## 최종 권고
1. `dev` 브랜치에서 CI 실행으로 전체 테스트 통과 확인.
2. 동일 결과가 확인되면 `main`으로 PR/머지 후 동일 워크플로우 재확인.
