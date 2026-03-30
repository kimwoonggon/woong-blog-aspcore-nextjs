# Backend Refactoring Brief: MediatR/Application Boundary, Auth Hot Path, Upload Storage Boundary

## 문제 정의
현재 백엔드에는 세 가지 문제가 동시에 보인다.

1. MediatR 를 쓰고 있지만 Application 계층이 유스케이스 소유권을 거의 갖지 못한다.
2. `AuthRecorder` 가 인증 핫패스와 감사 책임을 함께 안고 있어 세션 검증 경로가 DB write hot path 로 이어진다.
3. `UploadsController` 가 저장 경계와 경로 안전성 규칙을 직접 처리해 보안과 응집도가 모두 약하다.

이 문서는 위 세 문제를 하나의 통합 브리프로 다루되, 실제 실행은 `3개 독립 lane` 으로 계획할 수 있게 만드는 것이 목적이다.

## 왜 지금 해야 하는가
- 현재 구조는 폴더 이름만 보면 Application / Infrastructure / CQRS 경계가 있는 것처럼 보이지만, 실제 정책은 다른 곳에 있다.
- 이 상태에서는 리팩토링이 아니라 단순 코드 이동으로 끝날 위험이 크다.
- 특히 auth/session 과 upload 경계는 성능, 보안, 운영 추적성에 직접 영향을 준다.
- 이 문서는 후속 planner/executor 가 추가 질문 없이 실제 리팩토링 계획으로 이어갈 수 있을 정도로 범위와 결정 표면을 닫아야 한다.

## 공통 제약
- 이 문서의 1차 근거는 반드시 코드여야 한다.
- Lane 간 직접 선행관계를 만들지 마라. 우선순위는 둘 수 있지만, 하나의 lane 이 다른 lane 완료를 전제로 하면 안 된다.
- 구현 클래스명보다 `ownership rule`, `boundary invariant`, `decision record` 를 우선해서 적어라.
- 고위험 영역은 방향성 문장만 적지 말고, 무엇을 명시적으로 결정해야 하는지까지 적어라.
- 이 문서는 리팩토링 브리프이지 MediatR 입문서가 아니다.

## 비목표
- 이번 문서에서 실제 코드 구현안을 확정하지 않는다.
- 런타임 public API 변경안을 설계하지 않는다.
- 새로운 패키지 도입을 전제로 하지 않는다.
- 특정 클래스명, 특정 폴더명, 특정 migration 시나리오를 미리 잠그지 않는다.

## 우선순위 표
| Lane | 주제 | 우선순위 | 이유 |
| --- | --- | --- | --- |
| B | AuthRecorder 세션/감사 핫패스 | 높음 | 인증 요청 경로와 DB write 경로가 직접 연결됨 |
| C | UploadsController 저장/경로 경계 | 높음 | 사용자 입력 기반 경로 해석과 파일 삭제가 직접 노출됨 |
| A | MediatR/Application 경계 | 중간 | 구조적 문제는 크지만 B/C 보다 즉시 위험도는 낮음 |

## 의존성 표
| Lane | 직접 선행조건 |
| --- | --- |
| A | 없음 |
| B | 없음 |
| C | 없음 |

## 후속 ralplan 입력 규칙
- 계획은 반드시 `공통 front matter + 3개 독립 lane` 구조를 유지하라.
- 각 lane 은 단독으로 추출해 별도 리팩토링 계획으로 전환 가능해야 한다.
- 각 lane 에 반드시 `현재 증거`, `문제의 성격`, `목표 상태`, `허용되는 설계 자유도`, `비목표`, `Decision record`, `검증/테스트`, `Open questions` 를 포함하라.
- 각 lane 에 evidence ledger 를 넣어라.
  형식: `claim | repo evidence | inferred risk | required invariant/decision | verification hook`

---

## Lane A. MediatR/Application 경계

### 현재 증거

#### Evidence Ledger
| Claim | Repo evidence | Inferred risk | Required invariant/decision | Verification hook |
| --- | --- | --- | --- | --- |
| Handler 가 단순 전달자에 머문다 | `backend/src/WoongBlog.Api/Application/Admin/UpdatePage/UpdatePageCommandHandler.cs:16-18` | Application 이 껍데기로 남음 | Handler 가 어떤 정책을 가져와야 하는지 명시 | Handler/application 테스트 기준점 정의 |
| Command 타입이 infra service 입력 계약으로 직접 새어 나간다 | `backend/src/WoongBlog.Api/Application/Admin/Abstractions/IAdminBlogService.cs:13-14` | MediatR request 모델이 infra 계약이 됨 | MediatR request 타입을 infra 입력 계약으로 허용할지 여부 닫기 | service signature 검토 |
| 실제 규칙이 persistence service 쪽에 모여 있다 | `backend/src/WoongBlog.Api/Infrastructure/Persistence/Admin/AdminBlogService.cs:59-105` | use case ownership 이 infra 로 이동 | 유스케이스 규칙의 기본 소유자를 Application 으로 되돌릴지 명시 | handler/service 책임 재분배 기준 |
| Public 쪽도 query 타입이 service 계약으로 직접 연결된다 | `backend/src/WoongBlog.Api/Application/Public/Abstractions/IPublicBlogService.cs:8` | 조회 lane 도 동일한 leakage 패턴 유지 | thin-handler 예외 허용 조건을 명시 | projection-only read 구분 기준 |

### 문제의 성격
이 문제를 **"핸들러를 무조건 두껍게 만들어라"** 로 이해하면 안 된다. 핵심은 **유스케이스 소유권을 Application 으로 다시 가져오는 것**이다.

짧은 설명 사다리는 아래처럼 유지하라.

1. 증상: Handler 가 서비스 호출 한 줄이라 Application 이 껍데기처럼 보인다.
2. 왜 문제인가: 실제 유스케이스 규칙과 테스트 초점이 infra 서비스로 밀려 boundary 가 흐려진다.
3. Application 이 다시 가져와야 하는 것: 권한 검사, 존재 여부 검사, 상태 전이, 중복 검사, 작업 순서, dependency 호출 순서, 트랜잭션 경계, 실패 결정.
4. 여전히 얇게 남을 수 있는 것: projection-only read, 저장/조회 구현, 재사용 가능한 기술 보조 기능.

추가 설명이 필요하면 아래 공유 추론을 `보조 근거` 로만 써라.
- https://chatgpt.com/share/69c93c5f-2234-83a7-99fd-af869f911d1e

### 목표 상태
- Application layer 가 유스케이스 흐름과 정책을 소유한다.
- Infrastructure 는 persistence/query/adapter 구현에 집중한다.
- Handler 가 진짜로 얇아도 되는 경우와 안 되는 경우를 문서에서 명시적으로 구분한다.

### 허용되는 설계 자유도
- Handler 내부를 작은 application service 나 domain service 로 분해할지 여부
- Repository 와 adapter naming
- 단순 조회를 어디까지 thin-handler 로 허용할지에 대한 표현 방식

### 비목표
- 모든 handler 를 일괄적으로 두껍게 만드는 것
- CQRS 구조를 없애는 것
- MediatR pipeline 기본 개념을 길게 교육하는 것

### Decision record
- `infra 입력 계약에 MediatR request/command/query 타입을 직접 받지 않는다` 를 기본 규칙으로 둘지 결정하라.
- thin-handler 예외는 최소한 아래 조건을 모두 만족하는 조회에만 허용할지 결정하라.
  - projection-only
  - branching 없음
  - normalization 없음
  - cross-aggregate policy 없음
  - orchestration 없음
- 아래 기준표를 문서에 포함하라.

| Concern | Default owner | May stay thin? | Why |
| --- | --- | --- | --- |
| 권한 검사 | Application/Handler | 아니오 | 유스케이스 정책 |
| 존재 여부 검사 | Application/Handler | 아니오 | 실패 의미를 결정함 |
| 상태 전이 규칙 | Application/Handler or Entity | 아니오 | 도메인 정책 |
| 저장/조회 구현 | Repository/Infra | 예 | 기술 구현 |
| projection-only read | Query adapter/Infra | 조건부 예 | 정책이 없을 때만 |
| 슬러그 생성, 순서 결정, publish 처리 | Application/Handler | 아니오 | 흐름/정책 결정 |

### 검증/테스트
- 후속 planner 가 Handler/Application vs Service/Repository 경계를 추가 질문 없이 사용할 수 있어야 한다.
- 문서가 “핸들러를 두껍게”라는 슬로건으로 축소되지 않아야 한다.
- service signature 와 handler 책임 사이의 경계가 실제 파일 근거와 연결돼 있어야 한다.

### Open questions
- Public read 쪽은 어디까지 thin-handler 예외로 남겨도 되는가?
- Entity 가 가져가야 할 상태 규칙과 Application 이 가져가야 할 흐름 규칙의 기준을 얼마나 강하게 적을 것인가?

---

## Lane B. AuthRecorder 세션/감사 핫패스

### 현재 증거

#### Evidence Ledger
| Claim | Repo evidence | Inferred risk | Required invariant/decision | Verification hook |
| --- | --- | --- | --- | --- |
| 쿠키 검증마다 세션 검증이 호출된다 | `backend/src/WoongBlog.Api/Infrastructure/Auth/AppCookieAuthenticationEvents.cs:37-44` | 인증 핫패스가 DB 경로로 연결됨 | valid-session write policy 명시 | cookie validation 흐름 검토 |
| 세션 검증 성공 시에도 `LastSeenAt` 갱신과 저장이 일어난다 | `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRecorder.cs:220-221` | 모든 정상 요청이 write 후보가 됨 | 정상 경로 write 허용 여부 결정 | auth/session 테스트 확장 |
| invalid path 에서 revoke write 가 반복된다 | `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRecorder.cs:190-217` | invalid path side effect 가 불명확함 | 어떤 invalid path 에 write 를 허용할지 결정 | invalid path matrix 정의 |
| `SessionKey` 는 세션에 저장되고 index 까지 있으나 실제 검증 흐름은 `SessionId` 기반이다 | `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRecorder.cs:170-183`, `backend/src/WoongBlog.Api/Infrastructure/Persistence/WoongBlogDbContext.cs:75-79` | 유휴 필드 또는 반쯤 죽은 설계 가능성 | `SessionKey disposition` 결정 | persistence contract 검토 |

### 문제의 성격
현재 `AuthRecorder` 는 로그인 기록, 실패 기록, 로그아웃, access denied, 세션 검증, 세션 revoke 를 모두 담당한다. 문제는 단순히 클래스가 길다는 것이 아니라, **정상 인증 경로의 세션 검증과 감사/운영 기록 책임이 한 곳에 섞여 있고 write side effect 가 지나치게 크다**는 점이다.

### 목표 상태
- 세션 검증 경로와 감사 기록 경로의 책임을 구분한다.
- 정상 인증 요청이 매번 DB write 를 강제하지 않는 방향을 명시한다.
- invalid path 에서 어떤 side effect 를 허용할지 분명히 닫는다.

### 허용되는 설계 자유도
- 클래스명
- throttling 방식
- persistence 구현 위치
- audit 기록을 어디서 남길지에 대한 구체 구현

### 비목표
- 특정 throttling 공식 확정
- 특정 migration 방식 확정
- 세션 만료 정책 자체를 다시 설계하는 것

### Decision record
- 아래 항목을 문서에서 명시적으로 닫게 하라.
  - `valid-session write policy`
  - `invalid-path side effects`
  - `single revoke write 허용 path`
  - `SessionKey disposition`
- 권장 방향은 아래처럼 적어라.
  - `session validation concern` 과 `audit concern` 을 분리하는 방향
  - 정상 요청의 매 요청 write 제거 또는 완화
  - 구현 클래스명과 구현 메커니즘은 열어 두기

### 검증/테스트
- 후속 planner 가 `valid-session policy`, `invalid-path matrix`, `SessionKey disposition` 을 빠짐없이 계획으로 이어갈 수 있어야 한다.
- 정상/비정상 세션 경로의 write side effect 를 구분하는 테스트 항목이 문서에 드러나야 한다.
- 기존 [AuthRecorderTests] 기준점과 연결 가능해야 한다.

### Open questions
- invalid path 중 어떤 경우에 revoke write 가 필요하고, 어떤 경우에는 reject-only 로 충분한가?
- `SessionKey` 는 제거 후보인가, 실제 사용처를 만들 후보인가?

---

## Lane C. UploadsController 저장/경로 경계

### 현재 증거

#### Evidence Ledger
| Claim | Repo evidence | Inferred risk | Required invariant/decision | Verification hook |
| --- | --- | --- | --- | --- |
| 사용자 입력 `bucket` 으로 상대 경로를 만든다 | `backend/src/WoongBlog.Api/Controllers/UploadsController.cs:30-41` | 경로 이탈, 위치 혼동 위험 | bucket allowlist 결정 | upload validation 테스트 |
| upload 와 delete 가 동일한 boundary 규칙을 공유하지 않는다 | `backend/src/WoongBlog.Api/Controllers/UploadsController.cs:40-45`, `:91-95` | 저장/삭제 대칭성 깨짐 | upload/delete symmetry 결정 | delete path 테스트 |
| 현재 테스트는 missing file, upload/delete happy path, missing asset 만 본다 | `backend/tests/WoongBlog.Api.Tests/UploadsControllerTests.cs:18-68` | traversal, allowlist 실패, out-of-root delete 공백 | fail-closed 규칙 명시 | 보안 테스트 추가 |
| 정적 파일 루트는 `MediaRoot` 아래로 노출된다 | `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRuntimeExtensions.cs:17-25` | 저장 경계 규칙이 미약하면 노출면도 같이 커짐 | in-root normalization invariant | static files 경계 검토 |

### 문제의 성격
현재 `UploadsController` 는 multipart form 읽기, bucket 해석, 경로 계산, 파일 저장, `Asset` 생성, 파일 삭제를 모두 직접 처리한다. 가장 위험한 부분은 **사용자 입력 기반 bucket/path 해석과 파일시스템 write/delete 경계가 하나의 컨트롤러 액션에 직접 열려 있다**는 점이다.

### 목표 상태
- 저장 경계 규칙을 컨트롤러 밖의 명시적 경계로 밀어낸다.
- upload 와 delete 가 동일한 normalization / boundary rule 을 사용한다.
- 허용되지 않은 입력은 fail-closed 로 처리한다.

### 허용되는 설계 자유도
- `IAssetStorageService` 같은 이름을 실제로 쓸지 여부
- allowlist 표현 방식
- compatibility 처리 구현 방식

### 비목표
- 특정 compatibility window 확정
- 특정 cleanup workflow 확정
- 파일 스토리지를 외부 서비스로 옮긴다고 전제하는 것

### Decision record
- 아래 invariant 를 문서에서 고정하라.
  - `bucket allowlist`
  - `upload/delete 동일 normalization`
  - `MediaRoot 하위 경로 강제`
  - `invalid path fail-closed`
- 아래 항목은 compatibility question 으로 남겨라.
  - 기존 persisted `Asset.Path` 를 어떻게 처리할 것인가
  - 기존 request bucket 값을 어떻게 흡수할 것인가

### 검증/테스트
- `allowlist`, `in-root normalization`, `delete symmetry`, `existing path/bucket compatibility question` 이 모두 문서에 있어야 한다.
- traversal-style input, out-of-root delete path, unknown bucket 에 대한 테스트 공백이 명시돼야 한다.
- 기존 [UploadsControllerTests] 기준점과 연결 가능해야 한다.

### Open questions
- 기존 `Asset.Path` 데이터 중 normalization 기준을 어길 가능성이 있는가?
- legacy caller 의 bucket 값 분포를 먼저 조사해야 하는가?

---

## 공통 수용 기준
- 문서가 메모가 아니라 실행 가능한 브리프 구조를 가져야 한다.
- 핵심 주장 대부분이 코드 근거 또는 명시된 보조 근거에 연결돼야 한다.
- 각 lane 이 `decision record` 와 `verification hook` 를 포함해야 한다.
- 후속 `ralplan` 이 추가 질문 없이 실제 코드 리팩토링 계획으로 이어질 수 있어야 한다.

## 최종 지시
이 문서를 입력으로 받아 리팩토링 계획을 먼저 세워라.

요구사항:
- 계획은 lane 단위로도, 통합 단위로도 실행 가능해야 한다.
- 설치된 skill 들과 네 판단을 활용하되, 코드 근거를 1차 기준으로 삼아라.
- 구현으로 바로 들어가지 말고, 먼저 테스트 가능한 계획과 결정 표면을 닫아라.
