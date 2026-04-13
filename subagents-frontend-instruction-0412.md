# Subagents Frontend Instruction — 2026-04-12

> 목적: 프론트엔드 작업에서 subagent들을 어떻게 나누고, main agent가 무엇을 직접 관리하며, 검증과 체크리스트를 어떤 순서로 처리하는지 고정한다.
> 범위: public / admin 프론트엔드 개선, 회귀 수정, Playwright 기반 검증, TODO 추적
> 기준일: 2026-04-12

---

## 1. 운영 원칙

1. main agent는 직접 모든 구현을 하지 않는다.
2. main agent는 **조정자(coordinator)** 이다.
3. 구현은 lane별 subagent가 맡는다.
4. Playwright 브라우저 실행은 한 번에 하나만 돌린다.
5. `todolist.md`, `admin-todolist.md` 같은 체크리스트는 **검증 통과 후에만** 체크한다.
6. subagent의 “완료했다”는 보고만으로는 완료 처리하지 않는다.
7. 실제 완료 판정은 **main 또는 전담 verifier agent의 검증 결과**로만 한다.

---

## 2. 역할 분리

### 2.1 main agent

main agent는 아래 업무만 전담한다.

- 사용자 요구 해석
- 작업 분해
- lane별 subagent 할당
- write scope 충돌 관리
- TODO 문서 생성 및 체크 상태 관리
- Playwright 실행 큐 관리
- 최종 pass/fail 판정
- 실패 시 재작업 지시
- 최종 결과 보고

main agent는 아래 행동을 피한다.

- 이미 lane이 배정된 파일을 main이 직접 수정하는 것
- 여러 agent가 동시에 Playwright를 돌리게 두는 것
- 검증 없이 TODO를 체크하는 것

### 2.2 implementation subagents

implementation subagent는 아래만 수행한다.

- 배정된 scope의 코드 수정
- 필요한 테스트 초안 작성
- 변경 이유와 위험 요약
- 검증에 필요한 재현 경로 전달

implementation subagent는 아래를 하지 않는다.

- 독단적인 완료 판정
- 다른 lane scope 수정
- 무거운 Playwright 브라우저 테스트 실행
- 체크리스트 직접 완료 처리

### 2.3 verifier subagent

verifier subagent는 아래만 수행한다.

- main이 큐에 넣은 Playwright 테스트 실행
- 실패 로그 / video / screenshot / trace 수집
- PASS / FAIL를 main에게 전달
- 필요 시 관련 spec 보강 제안

verifier subagent는 아래를 하지 않는다.

- 기능 구현
- TODO 문서 자체 수정
- 동시에 여러 Playwright job 실행

---

## 3. lane 설계 방식

프론트엔드 작업은 기능/영역 기준으로 lane을 나눈다.

예시:

- Lane A: home / landing / hero / featured section
- Lane B: blog list / blog detail / related content / pagination
- Lane C: works list / works detail / media / navigation
- Lane D: static public pages (`contact`, `introduction`, `resume`)
- Lane E: shared layout (`Header`, `Footer`, shared cards, pagination, tokens)
- Lane V: verifier lane

admin 작업일 때는 아래 식으로 나눈다.

- Lane A: sidebar / layout
- Lane B: notion workspace
- Lane C: editors
- Lane D: tables
- Lane E: tiptap
- Lane V: verifier lane

원칙:

- 각 lane은 **파일 write scope가 겹치지 않게** 나눈다.
- shared component를 건드려야 하면 main이 충돌 가능성을 먼저 판단한다.
- shared 파일은 가능하면 마지막에 별도 lane으로 처리한다.

---

## 4. 지시 템플릿

main agent가 implementation subagent에게 줄 지시는 아래 구조를 따른다.

### 4.1 implementation lane 지시 템플릿

```md
너의 담당 범위:
- <파일/모듈 범위>

이번 작업 목표:
- <구현 목표 1>
- <구현 목표 2>

필수 제약:
- 배정되지 않은 파일은 수정하지 말 것
- Playwright 브라우저 테스트는 직접 돌리지 말 것
- 필요한 경우 가벼운 정적 검증만 수행할 것
- 완료 시 변경 파일 목록과 검증 필요 포인트를 함께 보고할 것

완료 보고 형식:
- changed files
- what changed
- risks
- recommended verification command
```

### 4.2 verifier lane 지시 템플릿

```md
지금부터 너는 Playwright verifier다.

검증 대상:
- <todo item id>
- <관련 spec>
- <브라우저 프로젝트>

규칙:
- 한 번에 하나의 Playwright job만 실행
- 결과는 PASS / FAIL로만 먼저 요약
- 실패 시 assertion 핵심, 관련 artifact 경로, 재현 포인트를 보고
- 구현 수정은 하지 말 것
```

---

## 5. 실행 순서

### Step 1. main이 TODO를 먼저 정리

- `todolist.md` 또는 `admin-todolist.md`를 먼저 만든다.
- 문서 요구사항을 빠짐없이 옮긴다.
- 추가로 발견한 작업도 별도 섹션에 기록한다.

### Step 2. main이 작업을 lane으로 분해

- 구현 scope를 나눈다.
- 충돌 가능성이 큰 shared scope는 별도 관리한다.
- 테스트 우선순위를 정한다.

### Step 3. implementation subagents가 코드 작업

- 각 lane은 자기 범위만 수정한다.
- 테스트가 필요한 경우 spec 초안을 같이 넣는다.
- 작업 완료 후 main에게 보고한다.

### Step 4. verifier lane이 단일 큐로 검증

- main은 검증 큐를 만든다.
- verifier는 큐 순서대로만 Playwright를 실행한다.
- 동시에 2개 이상 브라우저 세션을 열지 않는다.

### Step 5. main이 체크리스트 상태 반영

- PASS면 체크
- FAIL이면 담당 lane에 재작업 요청
- 재검증 통과 전에는 완료 처리 금지

---

## 6. Playwright 운영 규칙

### 절대 규칙

1. 여러 subagent가 동시에 Playwright를 돌리지 않는다.
2. Playwright는 verifier lane 또는 main만 실행한다.
3. `--workers=1`을 기본값으로 본다.
4. 사용자가 보는 테스트면 headed, 아니면 headless를 기본으로 한다.
5. HTTPS 환경이 요구되면 `PLAYWRIGHT_BASE_URL=https://localhost`를 명시한다.

### 권장 실행 예시

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=https://localhost npx playwright test tests/ui-pub-*.spec.ts --project=chromium-public --workers=1
```

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=https://localhost PLAYWRIGHT_HEADED=1 npx playwright test tests/ui-admin-unsaved-warning.spec.ts --project=chromium-authenticated --headed --workers=1
```

### artifact 관리

- video
- screenshot
- trace
- `test-results/playwright/summary/latest-upload-artifacts.md`

검증 보고에는 최소 아래가 포함되어야 한다.

- 어떤 spec을 돌렸는지
- 어떤 project를 썼는지
- pass/fail
- artifact 경로

---

## 7. TODO 체크 규칙

TODO는 아래 조건을 모두 만족할 때만 체크한다.

1. 코드가 실제로 반영됨
2. 관련 테스트가 존재함
3. verifier가 테스트를 실행함
4. PASS가 확인됨
5. 회귀 우려가 있으면 관련 회귀 테스트도 통과함

다음 경우에는 체크 금지:

- subagent가 “완료”라고만 말한 경우
- 로컬 추정만 있고 실행 증거가 없는 경우
- 테스트가 flaky해서 우연히 한 번 통과한 경우
- 관련 사용자 재현 경로가 아직 검증되지 않은 경우

---

## 8. 실패 처리 규칙

테스트가 실패하면 main은 아래 순서로 처리한다.

1. 실패를 발생시킨 TODO item과 lane을 식별
2. 실패 원인이 구현 결함인지, 테스트 결함인지 분류
3. 원인 lane에 재작업 지시
4. 같은 Playwright job을 동시에 다시 여러 번 돌리지 않음
5. 수정 후 동일 spec으로 재검증
6. 통과 후에만 체크리스트 반영

실패 보고 형식:

- failing item
- failing spec
- error summary
- likely owner lane
- next action

---

## 9. public 작업에서의 추가 규칙

public 개선에서는 문서 요구사항을 그대로 복붙하지 않고 아래를 함께 본다.

- 실제 현재 UI 흐름
- deep-link 가능성
- page / pageSize / relatedPage / returnTo 같은 URL 상태 유지
- inline edit 후 현재 위치 유지 여부
- save 후 beforeunload 경고 해제 여부
- prev / next와 related list pagination의 일관성
- 모바일/데스크톱에서 폭과 클릭 흐름의 안정성

즉, public lane은 “문서 준수”만이 아니라 **실제 사용자 흐름 일관성**까지 책임진다.

---

## 10. admin 작업에서의 추가 규칙

admin 개선에서는 아래를 함께 본다.

- 편집 도중 dirty state 정확도
- save 후 dirty clear
- `beforeunload` 경고가 실제 저장 이후 사라지는지
- list / detail / inline mode 간 이동 흐름
- notion workspace에서 server roundtrip 과다 여부
- 테이블 검색, 삭제, 다이얼로그의 일관성

---

## 11. main agent 최종 보고 규칙

main agent는 최종 보고에서 아래를 분리해서 적는다.

- 실제 구현된 항목
- 실제 검증된 항목
- 아직 남은 위험
- 미검증 또는 보류 항목

금지:

- 구현과 검증을 섞어서 과장 보고
- “다 끝났다”라고 말하면서 unchecked TODO가 남아 있는 상태
- 영상/테스트를 실행하지 않았는데 실행한 것처럼 말하는 것

---

## 12. 요약 운영 문장

이 방식의 핵심은 아래 한 문장으로 정리한다.

> subagent는 구현을 분담하고, main은 충돌과 TODO를 관리하며, Playwright는 단일 verifier 큐에서만 실행하고, PASS된 항목만 체크한다.
