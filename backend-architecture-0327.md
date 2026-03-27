# Backend Architecture Review

  작성일: 2026-03-27

  ## 1. 요약

  `backend/src/Portfolio.Api` 는 ASP.NET Core + EF Core + MediatR + FluentValidation 기반으로 구성되어 있고, 폴더 구조만 보면 `Application /
  Domain / Infrastructure / Controllers / Endpoints` 로 나뉘어 있어 기본 뼈대는 나쁘지 않다.

  하지만 실제 책임 배분을 보면 계층이 균등하게 유지되지는 않는다. 핵심 규칙과 운영 복잡도는 몇 군데로 다시 뭉쳐 있다.

  대표적으로 다음 다섯 지점이 구조적 집중 지점이다.

  - `Program.cs`
  - `Infrastructure/Notion/NotionImportService.cs`
  - `Infrastructure/Auth/AuthRecorder.cs`
  - `Controllers/UploadsController.cs`
  - `Endpoints/AdminAiEndpoints.cs`

  즉, 현재 구조는 "완전한 스파게티"라기보다 "계층은 나눴지만 실제 책임은 다시 특정 파일에 집중된 상태"로 보는 것이 정확하다.

  ## 2. 현재 아키텍처 흐름

  현재 요청 흐름은 대체로 아래와 같다.

  `Controller -> MediatR Handler -> Service -> PortfolioDbContext`

  공개 API와 관리자 CRUD는 이 흐름을 상당 부분 따른다. 예를 들면 `PublicController`, `AdminBlogsController`, `AdminWorksController` 는
  `ISender` 를 통해 Query/Command Handler 로 위임한다.

  반면 일부 기능은 이 규칙을 벗어난다.

  - `AdminAiEndpoints` 는 Minimal API가 직접 `PortfolioDbContext` 와 AI 서비스를 사용한다.
  - `AdminNotionImportEndpoints` 는 Application 계층 없이 `NotionImportService` 를 바로 호출한다.
  - `AuthController` 와 `UploadsController` 는 `DbContext` 를 직접 잡고 있다.

  결과적으로 코드베이스 안에 두 가지 스타일이 공존한다.

  - CQRS + Handler + Service
  - Endpoint/Controller 직결 + DbContext/Infra Service 직접 호출

  이 혼합이 유지보수 비용을 키우고 있다.

  ## 3. 강점

  현재 코드베이스의 좋은 점도 분명하다.

  - 폴더 구조와 네이밍은 전반적으로 읽기 쉽다.
  - `ValidationBehavior` 와 `ValidationExceptionFilter` 로 요청 검증 흐름이 일관적이다.
  - 인증, CSRF, 보안 헤더, rate limit 기본 골격이 이미 있다.
  - `tests/Portfolio.Api.Tests` 에 엔드포인트와 인증 주변 테스트가 꽤 넓게 깔려 있다.
  - `DbContext` 모델 설정과 기본 시드 데이터 흐름은 단순해서 이해가 쉽다.

  즉, 바닥부터 갈아엎을 수준은 아니다. 다만 "응집도 재조정"이 필요한 상태다.

  ## 4. 핵심 문제점

  ### 4.1 Composition Root 과밀

  `Program.cs` 에 설정 해석, 옵션 보정, 인증 구성, 쿠키 이벤트, DB 초기화, 미들웨어 배치, static files, OpenAPI, endpoint 매핑이 모두 들어 있
  다.

  문제는 단순히 길다는 것이 아니라, 운영 정책과 조립 코드가 한 파일에 섞여 있다는 점이다. 이 상태에서는 인증 정책이나 배포 정책 변경이 있을 때
  `Program.cs` 가 계속 비대해진다.

  개선 방향:

  - `ServiceCollection` 확장 메서드로 분리
  - 인증/보안/DB/HTTP client 등록을 별도 extension 으로 이동
  - startup bootstrap 도 별도 서비스로 이동

  ### 4.2 NotionImportService 의 과도한 책임 집중

  `NotionImportService.cs` 는 현재 다음 책임을 동시에 가진다.

  - Notion API 호출
  - 페이지/블록 탐색
  - HTML 렌더링
  - 이미지 다운로드
  - 로컬 파일 저장
  - Asset 엔티티 저장
  - Blog upsert
  - slug 생성
  - 기존 블로그 매칭

  이건 가장 명확한 스파게티 위험 지점이다. 클래스 하나가 외부 API 클라이언트, 변환기, 파일 저장기, 영속화 유스케이스를 모두 가진다.

  추가로 `FindExistingBlogAsync` 는 블로그 전체 `ContentJson` 을 메모리로 가져와 문자열 검색으로 매칭하고 있어 데이터가 늘면 비효율이 커진다.

  개선 방향:

  - `NotionClient`
  - `NotionBlockRenderer`
  - `NotionAssetImporter`
  - `NotionBlogImporter`

  로 분리하고, DB 저장은 유스케이스 계층으로 올리는 것이 좋다.

  ### 4.3 MediatR 계층의 얇은 래퍼화

  Admin/Public Query/Command Handler 대부분은 실제 로직이 거의 없다. 대부분 "서비스 호출 한 줄" 수준이다.

  즉 지금은 MediatR 를 쓰고 있지만 Application 계층이 정책을 갖지 못하고 있다. 실제 규칙은 `Infrastructure/Persistence/Admin/*Service`,
  `Infrastructure/Persistence/Public/*Service` 에 있다.

  이 구조는 다음 문제를 만든다.

  - Application 계층의 의미 약화
  - 테스트 초점 분산
  - Infra 계층이 사실상 use case 를 소유
  - CQRS 도입 비용 대비 이득 축소

  개선 방향:

  - Handler 가 유스케이스 규칙을 소유
  - Service 는 query/repository/infra adapter 역할로 축소
  - Command/Query DTO 를 Infra 서비스 메서드 시그니처에 직접 노출하지 않기

  ### 4.4 AuthRecorder 의 핫패스 집중

  `AuthRecorder` 는 로그인 기록, 실패 기록, 로그아웃, access denied, 세션 검증, 세션 revoke 를 모두 담당한다.

  특히 `ValidateSessionAsync` 는 인증 쿠키 검증 과정에서 매번 DB 조회와 `SaveChangesAsync` 를 유발한다. 트래픽이 늘면 인증 핫패스가 바로 DB
  write 경로가 된다.

  또 `SessionKey` 는 저장과 unique index 는 있으나 실제 인증 흐름에서는 읽지 않는다. 지금 기준으로는 유휴 필드에 가깝다.

  개선 방향:

  - `AuthAuditService` 와 `SessionService` 분리
  - 세션 last-seen 갱신 주기 제한
  - 매 요청 write 제거 또는 완화
  - 사용하지 않는 `SessionKey` 는 실제 사용처를 만들거나 제거 검토

  ### 4.5 UploadsController 의 보안/응집도 문제

  `UploadsController` 는 아래를 직접 처리한다.

  - multipart form 읽기
  - bucket 해석
  - 경로 계산
  - 파일 저장
  - Asset 엔티티 생성
  - 파일 삭제

  가장 위험한 부분은 `bucket` 값이 사용자 입력 기반인데 경로 정규화/화이트리스트 검증이 없다는 점이다. 현재 구조는 경로 이탈이나 의도치 않은 위
  치 쓰기/삭제 위험을 만든다.

  개선 방향:

  - `IAssetStorageService` 도입
  - bucket 을 enum 또는 허용 목록으로 제한
  - `MediaRoot` 하위 경로 강제 검증
  - 삭제 시에도 경로 정규화 검증 수행

  ## 5. 미사용 또는 반쯤 죽은 코드 후보

  완전히 안 쓰는 코드보다는 "반쯤 구현된 흔적"이 더 많다.

  ### 높은 우선순위 후보

  - `PageView`
    - 엔티티와 DBSet, 대시보드 집계는 존재
    - 실제 적재 코드가 없다
    - 지금은 사실상 죽은 기능이다

  - `NotionConfigResponse`
    - 선언만 있고 실제 사용처가 없다
    - 제거 후보다

  ### 구조적으로 애매한 후보

  - `AuthSession.SessionKey`
    - 생성하고 unique index 도 잡지만 실제 조회 경로가 없다
    - 보존할 이유가 약하다

  - `Blog.CoverAssetId`
    - 공개 조회에서는 사용된다
    - 하지만 관리자 create/update 경로에서는 다루지 않는다
    - 반쪽짜리 필드다

  - `AddHealthChecks()`
    - 서비스 등록은 있지만 `MapHealthChecks()` 는 없다
    - 실제 health 응답은 수동 `HealthController` 가 담당한다

  ## 6. 데이터 모델/표현 문제

  현재 `ContentJson`, `AllPropertiesJson` 은 문자열 JSON 으로 저장된다. 이것 자체가 반드시 나쁜 건 아니지만, 현재는 schema 검증이 거의 없고 파
  싱 실패를 조용히 무시하는 코드가 있다.

  예를 들어 `AdminContentJson` 은 잘못된 JSON 을 만나도 빈 값으로 처리한다. 이 방식은 운영 장애를 늦게 발견하게 만든다.

  개선 방향:

  - 최소한 입력 schema validation 추가
  - 가능하면 typed DTO 또는 value object 로 축소
  - 파싱 실패는 로깅 또는 validation failure 로 드러내기

  ## 7. 성능 및 운영 관점 이슈

  공개 조회 서비스들은 자산 테이블을 통째로 메모리로 가져오는 패턴이 반복된다.

  - `PublicHomeService`
  - `PublicWorkService`
  - `PublicBlogService`

  데이터가 작을 때는 버틸 수 있지만, 자산 수가 늘면 불필요한 메모리 사용과 조회 비용이 커진다.

  또한 `DatabaseBootstrapper` 는 재시도 로직이 있지만 실패 원인을 남기지 않고 넘기는 구간이 있어 운영 추적성이 떨어진다.

  개선 방향:

  - 필요한 Asset 만 projection/join 으로 가져오기
  - bootstrap 실패 원인 로깅
  - 무음 실패 제거

  ## 8. 스파게티 판정

  현재 backend 는 전체가 스파게티라고 보기는 어렵다.

  판정은 아래가 더 정확하다.

  - 전체 구조: 중간 수준 이상으로 정돈됨
  - 실제 책임 분배: 불균형
  - 특정 대형 클래스: 스파게티 위험 높음
  - 리팩터링 난이도: 중간
  - 전면 재작성 필요성: 낮음

  즉 "전체 붕괴"가 아니라 "핵심 몇 군데를 잘라내면 빠르게 좋아질 수 있는 구조"다.

  ## 9. 우선순위별 개선안

  ### P0
  - 업로드 경로 검증 추가
  - bucket 화이트리스트 도입
  - 파일 삭제 경로 안전성 보강

  ### P1
  - `NotionImportService` 분해
  - 업로드/Notion 이미지 저장을 `IAssetStorageService` 로 통합
  - AI/Notion 엔드포인트도 Application 계층 규칙에 맞추기

  ### P2
  - Handler 에 실제 유스케이스 로직 배치
  - Infra service 는 repository/query adapter 성격으로 축소
  - `ContentJson` 계열 validation 강화

  ### P3
  - `PageView` 를 실제 구현하거나 제거
  - `SessionKey` 정리
  - `NotionConfigResponse` 제거
  - health check 등록/노출 방식 정리
  - `CoverAssetId` 관리자 경로 완성 또는 필드 제거 검토

  ## 10. 테스트 및 검증 갭

  좋은 점은 현재 테스트가 꽤 넓다는 것이다. 하지만 가장 복잡한 지점에 대한 직접 테스트는 상대적으로 약하다.

  특히 다음 보강이 필요하다.

  - 업로드 경로 이탈 방지 테스트
  - Notion block 렌더링 단위 테스트
  - Notion 이미지 저장 실패 fallback 테스트
  - 기존 블로그 매칭 로직 테스트
  - 인증 세션 validate 핫패스 테스트
  - `PageView` 유지 시 적재 경로 테스트

  참고:
  이번 분석 세션에서는 현재 환경에 `dotnet` SDK 가 없어 `dotnet test backend/Portfolio.sln` 을 실행하지 못했다.

  ## 11. 결론

  이 backend 는 버려야 할 수준의 스파게티는 아니다. 다만 "레이어가 있어 보이는 구조"에 비해 실제 책임은 몇 군데에 과도하게 몰려 있고, 그 집중
  지점이 보안, 운영, 유지보수 리스크를 만든다.

  가장 먼저 손대야 할 것은 다음 세 가지다.

  - 업로드 경로 보안
  - `NotionImportService` 분해
  - 인증 세션 검증 핫패스 완화

  이 세 가지만 정리해도 코드베이스의 안정성과 설명 가능성이 크게 올라갈 가능성이 높다.