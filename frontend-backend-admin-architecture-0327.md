 현재 구조는 운영자만 admin page 접근에는 꽤 잘 맞습니다.
  반대로 일반 user는 works/blog 댓글만 가능이라는 요구는 아직 전혀 구현되어 있지 않고, auth/DB/route 관점에서도 그대로는 성립하지 않습니다.

  정확히 말하면:

  - admin-only 운영자 영역: 현재 구조로 가능하고, 이미 상당 부분 그렇게 되어 있음
  - 일반 user 참여 영역: 현재는 사실상 없음
  - 댓글만 허용하는 user 기능: 아직 모델, API, DB, 권한 정책, CSRF 범위 모두 없음
  - 로그인한 아무 일반 user가 comment 가능: 구조적으로는 추가 가능
  - 승인된 user만 comment 가능: 현재 구조로는 부족

  현재 auth 구조가 잘 되는 부분

  관리자 보호는 프론트와 백엔드 양쪽에서 이중으로 걸려 있습니다.

  - 백엔드에서 모든 admin 컨트롤러는 AdminOnly 정책을 강제합니다. 예: backend/src/Portfolio.Api/Controllers/AdminBlogsController.cs:13,
    backend/src/Portfolio.Api/Controllers/AdminWorksController.cs:13, backend/src/Portfolio.Api/Controllers/AdminPagesController.cs:10,
    backend/src/Portfolio.Api/Controllers/AdminSiteSettingsController.cs:10, backend/src/Portfolio.Api/Controllers/UploadsController.cs:10.
  - 그 정책 자체는 backend/src/Portfolio.Api/Program.cs:256 에서 AuthClaimTypes.Role == "admin" 으로 정의됩니다.
  - 로그인 성공 시 백엔드는 role/profile/session claim을 쿠키 principal에 직접 넣습니다. backend/src/Portfolio.Api/Program.cs:223
  - 그리고 매 요청 쿠키 검증 시 DB의 AuthSession 과 Profile.Role 을 다시 확인합니다. backend/src/Portfolio.Api/Program.cs:188, backend/src/
    Portfolio.Api/Infrastructure/Auth/AuthRecorder.cs:168
  - 프론트도 admin shell 진입 전에 세션을 읽고 비로그인 사용자는 /login, 비admin은 / 로 보냅니다. src/app/admin/layout.tsx:12

  즉, 일반 사용자가 /admin UI에 들어오더라도:

  1. 프론트에서 한 번 막고
  2. API 직접 호출을 해도 백엔드에서 다시 막습니다.

  이건 꽤 좋은 방어선입니다.

  현재 user 구조의 실제 의미

  문제는 user 역할이 “제품 기능 역할”이 아니라 거의 “admin이 아닌 로그인 사용자”라는 점입니다.

  - role은 backend/src/Portfolio.Api/Infrastructure/Auth/AuthRecorder.cs:249 에서 AuthOptions.AdminEmails 에 있으면 admin, 아니면 무조건 user
    로 정해집니다.
  - Profile 모델도 role 필드는 단순 문자열 하나뿐입니다. backend/src/Portfolio.Api/Domain/Entities/Profile.cs:3
  - 즉 현재는 Google 로그인만 성공하면 거의 자동으로 user 프로필이 생깁니다. backend/src/Portfolio.Api/Infrastructure/Auth/AuthRecorder.cs:42

  이 말은 곧:

  - “일반 user 개념”은 존재함
  - 하지만 그 user에게 허용된 제품 기능은 거의 없음
  - 사실상 admin이 아닌 로그인 사용자라는 표시만 있을 뿐임

  프론트에서도 이게 드러납니다.

  - public layout/navbar는 로그인 여부만 보고 signed-in 상태를 보여줍니다. src/app/(public)/layout.tsx:12, src/components/layout/Navbar.tsx:109
  - 하지만 non-admin user를 위한 별도 화면이나 API는 없습니다.
  - My Page 도 admin이 아니면 그냥 / 로 갑니다. src/components/layout/Navbar.tsx:68

  즉 지금 구조에서 user는 인증 상태는 있지만, 실제 권한 모델로 쓰이고 있지는 않습니다.

  댓글 요구사항 기준으로 보면 왜 아직 안 되는가

  현재는 댓글 기능이 없습니다.

  - 코드베이스 전체에서 comment/discussion/reply 관련 앱 코드가 사실상 없습니다.
  - public API는 backend/src/Portfolio.Api/Controllers/PublicController.cs:13 기준으로 site-settings, home, pages, works, blogs, resume 뿐입니
    다.
  - DB 엔티티도 Blog, Work, PageEntity, Profile, AuthSession, Asset 등만 있고 댓글 엔티티가 없습니다. backend/src/Portfolio.Api/Domain/
    Entities/Blog.cs:3, backend/src/Portfolio.Api/Domain/Entities/Work.cs:3

  즉 지금 구조는:

  - user 로그인: 가능
  - user 세션 유지: 가능
  - user role 구분: 가능
  - user가 comment 작성: 불가
  - user comment 조회/수정/삭제/신고/moderation: 전부 없음

  댓글 기능을 추가할 때 현재 구조가 가진 장점

  댓글을 “로그인한 user만 작성 가능”으로 만들려면, 현재 auth 기반은 꽤 재사용하기 좋습니다.

  장점은 이겁니다.

  - 이미 쿠키 세션 기반이라 브라우저에 access token을 둘 필요가 없습니다.
  - 백엔드 /api/auth/session 으로 로그인 상태와 role을 바로 확인할 수 있습니다. backend/src/Portfolio.Api/Controllers/AuthController.cs:63
  - comment 작성 API를 만들 때 ProfileId 는 claim에서 바로 얻을 수 있습니다. backend/src/Portfolio.Api/Controllers/AuthController.cs:72,
    backend/src/Portfolio.Api/Program.cs:231
  - admin-only 정책은 이미 있으니, comment는 반대로 그냥 RequireAuthenticatedUser 또는 일반 [Authorize] 로 분리하면 됩니다.

  즉 auth foundation은 나쁘지 않습니다.

  하지만 댓글 요구사항을 막는 핵심 부족분

  1. 댓글 엔티티/테이블이 없음

  - 최소한 CommentId, ProfileId, EntityType 또는 BlogId/WorkId, Body, Status, CreatedAt, UpdatedAt 가 필요합니다.
  - 지금 Blog/Work 는 author/user relation조차 없습니다. backend/src/Portfolio.Api/Domain/Entities/Blog.cs:3, backend/src/Portfolio.Api/Domain/
    Entities/Work.cs:3

  2. 댓글용 권한 정책이 없음

  - 현재 정책은 실질적으로 AdminOnly 하나입니다. backend/src/Portfolio.Api/Program.cs:256
  - 댓글은 AuthenticatedUser 같은 일반 로그인 정책이 필요합니다.

  3. CSRF 보호 범위가 댓글 API를 포함하지 않음

  - 현재 CSRF 미들웨어는 /api/admin, /api/uploads, /api/auth/logout 만 보호합니다. backend/src/Portfolio.Api/Infrastructure/Security/
    AntiforgeryValidationMiddleware.cs:39
  - 댓글 POST를 /api/comments 나 /api/public/blogs/{id}/comments 같은 경로로 만들면, 지금 상태로는 CSRF 보호를 자동으로 못 받습니다.
  - 쿠키 세션 기반으로 댓글을 쓸 거면 이건 꼭 확장해야 합니다.

  4. “승인된 user만 댓글” 모델은 없음

  - 지금은 admin email이 아니면 전부 user 로 자동 승격됩니다. backend/src/Portfolio.Api/Infrastructure/Auth/AuthRecorder.cs:249
  - 만약 “로그인한 아무 Google 계정”이 아니라 “승인된 일반 회원만 댓글 가능”이 요구라면,
      - Profile.Status
      - CanComment
      - IsApproved
        같은 추가 상태가 필요합니다.

  5. moderation / abuse / spam 설계가 없음

  - 댓글은 auth만으로 끝나지 않습니다.
  - 최소한 필요합니다:
      - soft delete
      - hidden/reported status
      - rate limiting
      - markdown/html sanitization
      - admin moderation 화면

  6. public page에 admin 기능이 섞여 있음

  - 현재 blog/work public detail은 admin이면 inline editor가 뜹니다. src/app/(public)/blog/[slug]/page.tsx:98, src/app/(public)/works/[slug]/
    page.tsx:102
  - 보안상 즉시 문제는 아니지만, “운영자 기능은 admin page에만” 이라는 경계 철학과는 충돌합니다.
  - 요구사항을 엄격히 적용하려면 public route에서 inline admin editor도 빼는 쪽이 더 맞습니다.

  요구사항에 대한 정확한 답

  질문:
  “auth가 중요한데 admin page는 운영자만 접근 가능하고 나머지는 user들은 works나 blog에 있는 댓글정도만 달 수 있게 만들려고 하는데 이런 구조로
  되어 있는지”

  정답은:

  - admin page는 운영자만 접근 가능
    -> 예, 현재 구조는 꽤 잘 그렇게 되어 있습니다.
  - 나머지 user는 works/blog 댓글 정도만 가능
    -> 아니요. 현재 구조는 그렇게 되어 있지 않습니다.
    -> 지금은 user가 존재하지만, 실제 user 기능 surface가 없습니다.
    -> 댓글 기능은 DB/API/UI/정책 전부 비어 있습니다.
  - 이 구조로 그 방향으로 확장 가능한가
    -> 예, auth 기초는 괜찮아서 확장 가능은 합니다.
    -> 다만 “바로 된다”가 아니라 권한 정책, CSRF, comment 모델, moderation을 새로 설계해야 합니다.

  가장 중요한 설계 판단

  여기서 제일 중요한 질문은 이것입니다.

  로그인한 아무 Google 사용자도 댓글을 달 수 있게 할 건가,
  아니면
  승인된 일반 사용자만 댓글을 달 수 있게 할 건가?

  현재 구조는 전자에 더 가깝습니다.
  후자를 원하면 지금 auth 모델은 부족합니다.

  실무적으로 추천하는 방향

  - admin 전용은 지금처럼 유지
  - 일반 사용자 댓글은 별도 policy 추가
  - 댓글 작성 API는 [Authorize] 또는 RequireAuthenticatedUser
  - comment 엔티티에 ProfileId 저장
  - CSRF middleware 범위를 comment mutation path까지 확장
  - public page의 admin inline editor는 제거해서 “운영자 기능은 admin shell에만” 원칙을 세우기
  - “아무 로그인 사용자 허용”인지 “승인 사용자만 허용”인지 먼저 결정

  한 줄 결론으로 정리하면:

  현재 구조는 admin-only 운영자 영역에는 적합하지만, 일반 user는 댓글만 가능한 구조는 아직 아니고, 현재의 user는 사실상 ‘admin이 아닌 로그인 사
  용자’라는 표시만 있을 뿐 제품 권한으로 구현되어 있지는 않습니다.