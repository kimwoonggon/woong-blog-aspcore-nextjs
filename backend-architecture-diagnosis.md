# Backend Authentication/Login 코드 냉정 리뷰

## 총평: B+ (상위권이지만 프로덕션 배포 전 반드시 고쳐야 할 것이 있음)

전체적인 아키텍처 설계는 **수준급**입니다. Minimal API + 모듈 구조, OIDC + Cookie 인증, CSRF 보호, Rate Limiting, Security Headers, Audit Logging까지 갖춘 건 개인 프로젝트 수준을 넘어섭니다. 하지만 디테일에서 몇 가지 **실제 공격 벡터**가 존재하고, 성능/설계적으로 프로덕션에서 문제가 될 부분이 있습니다.

---

## CRITICAL - 반드시 고쳐야 함

### 1. Logout 엔드포인트 Open Redirect 취약점 (OWASP A01)

`LogoutEndpoint.cs`에서 `LogoutRequest.ReturnUrl`이 **아무런 검증 없이** 그대로 응답에 포함됩니다.

```csharp
// IdentityInteractionService.LogoutAsync
return string.IsNullOrWhiteSpace(returnUrl) ? _authOptions.SignedOutRedirectPath : returnUrl;
// → { redirectUrl: "https://evil-phishing-site.com" } 가 그대로 나감
```

Login 엔드포인트에는 `IsLocalReturnUrl()` 검증이 있는데, **Logout에는 없습니다**. 공격자가 `POST /api/auth/logout?ReturnUrl=https://evil.com`을 보내면 프론트엔드가 이걸 따라갈 수 있습니다.

**수정:** Login에 적용한 것과 동일한 `IsLocalReturnUrl` 검증을 Logout/TestLogin에도 적용해야 합니다.

**관련 파일:**
- `backend/src/WoongBlog.Api/Modules/Identity/Api/Logout/LogoutEndpoint.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Application/IdentityInteractionService.cs`

---

### 2. ValidateSessionAsync - 매 요청마다 2~3회 DB 호출 (성능 지뢰)

`AppCookieAuthenticationEvents.cs`의 `ValidatePrincipal`이 **모든 인증된 요청마다** 호출됩니다:

```csharp
public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
{
    var isValid = await recorder.ValidateSessionAsync(context.Principal!, ...);
```

`AuthRecorder.ValidateSessionAsync`는 내부에서:
1. `AuthSessions` 테이블 조회 (1회)
2. `Profiles` 테이블 조회 (1회)
3. `SaveChangesAsync` (1회, LastSeenAt 갱신)

**인증된 사용자의 모든 HTTP 요청마다 최소 2~3 DB roundtrip**. 블로그가 트래픽 받기 시작하면 DB가 먼저 죽습니다.

**수정 방안:**
- In-memory 캐시 (5~10분 TTL)로 세션 유효성 캐싱
- LastSeenAt 갱신을 배치/주기적으로 처리
- 또는 DB 조회를 1개 JOIN 쿼리로 축소

**관련 파일:**
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AppCookieAuthenticationEvents.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRecorder.cs`

---

## HIGH - 강력히 권장

### 3. AuthRecorder가 God Class

`AuthRecorder.cs` 하나가 **6개 역할**을 수행합니다:
- 로그인 기록 + Profile upsert
- 로그인 실패 기록
- 로그아웃 기록
- 접근 거부 기록
- 세션 유효성 검증
- 세션 취소

이건 **SRP(단일 책임 원칙) 위반**이고, 테스트도 어렵게 만듭니다. 특히 인터페이스 없이 concrete class로 주입되어 있어서 mocking이 불편합니다.

**수정 방안:** 최소한 `ISessionValidator`, `IAuditRecorder`, `IProfileManager`로 분리

**관련 파일:**
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRecorder.cs`

### 4. AuthRecorder에 인터페이스가 없음

```csharp
// IdentityInteractionService.cs
private readonly AuthRecorder _authRecorder; // concrete class 직접 의존
```

.NET 베스트 프랙티스의 핵심인 **Interface Segregation**과 **testability**에 어긋남. `AppCookieAuthenticationEvents`, `AppOpenIdConnectEvents`, `IdentityInteractionService` 모두 concrete `AuthRecorder`에 직접 의존합니다.

**관련 파일:**
- `backend/src/WoongBlog.Api/Modules/Identity/Application/IdentityInteractionService.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AppCookieAuthenticationEvents.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AppOpenIdConnectEvents.cs`

### 5. Profile Upsert 시 Race Condition 가능성

`AuthRecorder.RecordSuccessfulLoginAsync`에서:

```csharp
var profile = await _dbContext.Profiles.SingleOrDefaultAsync(x => x.Provider == "google" && x.ProviderSubject == providerSubject);
profile ??= await _dbContext.Profiles.SingleOrDefaultAsync(x => x.Email == email);
// ... 이후 Add 또는 Update
await _dbContext.SaveChangesAsync(cancellationToken);
```

두 개의 별도 쿼리 → 판단 → 저장. 동일 사용자가 동시에 여러 탭에서 로그인하면 **중복 Profile이 생성**될 수 있습니다. `SaveChangesAsync`가 트랜잭션으로 감싸지만, SELECT 시점과 INSERT 시점 사이에 다른 트랜잭션이 INSERT할 수 있습니다.

**수정:** DB unique constraint (이미 있을 수 있음) + `try-catch DbUpdateException`으로 retry, 또는 `INSERT ... ON CONFLICT` 패턴.

**관련 파일:**
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRecorder.cs`

### 6. 구조화된 로깅 전무

전체 Auth 코드에 **`ILogger` 사용이 단 한 곳도 없습니다**. DB에 Audit Log를 쓰는 건 좋지만, 런타임 모니터링/알림을 위한 structured logging이 없으면:
- 로그인 실패 폭증을 감지할 수 없음
- 세션 만료 패턴을 파악할 수 없음
- DB 장애 시 감사 로그도 같이 소실

**수정:** 최소한 `RecordSuccessfulLoginAsync`, `RecordLoginFailureAsync`, `ValidateSessionAsync(실패 시)`에 `ILogger` 추가

**관련 파일:**
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRecorder.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AppCookieAuthenticationEvents.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AppOpenIdConnectEvents.cs`

---

## MEDIUM - 개선 권장

### 7. CSP에 `'unsafe-inline'` 허용

`SecurityOptions.cs`:
```
script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'
```
XSS 공격의 1차 방어벽인 CSP가 `unsafe-inline`으로 **사실상 무력화**. Next.js 때문에 어쩔 수 없는 부분이 있지만, `nonce` 기반으로 전환하면 크게 강화됩니다.

**관련 파일:**
- `backend/src/WoongBlog.Api/Infrastructure/Security/SecurityOptions.cs`

### 8. TestLogin에서 Claims 중복 생성

`IdentityInteractionService.CreateTestLoginAsync`:
```csharp
new Claim(ClaimTypes.Role, result.Role),      // ← 표준 Role claim
new Claim(AuthClaimTypes.Role, result.Role),   // ← 커스텀 Role claim
```
같은 값이 두 번 들어가는데, OIDC 로그인 흐름(`AppOpenIdConnectEvents.cs`)에서도 동일 패턴. 의도적인지 확인 필요. 표준 `ClaimTypes.Role`과 커스텀 `AuthClaimTypes.Role`이 항상 동기화된다는 보장이 없으면 보안 hole이 될 수 있습니다.

**관련 파일:**
- `backend/src/WoongBlog.Api/Modules/Identity/Application/IdentityInteractionService.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AppOpenIdConnectEvents.cs`

### 9. `AuthOptions.MediaRoot`가 Auth 설정에 있음

```csharp
public class AuthOptions
{
    public string MediaRoot { get; set; } = "/app/media"; // ← 여기 왜?
}
```
Media 파일 관리는 인증과 무관. 설정 응집도가 떨어집니다. 별도 `MediaOptions`로 분리해야 합니다.

**관련 파일:**
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthOptions.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRuntimeExtensions.cs`

### 10. Session 정리 로직 부재

만료/취소된 세션이 **영원히 DB에 남습니다**. `AuthSessions` 테이블이 계속 커지면 `ValidateSessionAsync`의 쿼리도 느려집니다. Background cleanup job이 필요합니다.

**관련 파일:**
- `backend/src/WoongBlog.Api/Domain/Entities/AuthSession.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRecorder.cs`

### 11. LogoutRequest가 Query String 바인딩

```csharp
app.MapPost(IdentityApiPaths.Logout, async (
    [AsParameters] LogoutRequest request, ...
```
POST 엔드포인트인데 `[AsParameters]`로 query string에서 바인딩. ReturnUrl은 request body(`[FromBody]`)에서 받는 게 보안상 더 안전합니다 (URL에 민감 정보가 노출되지 않도록).

**관련 파일:**
- `backend/src/WoongBlog.Api/Modules/Identity/Api/Logout/LogoutEndpoint.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/Logout/LogoutRequest.cs`

---

## LOW - 참고 사항

| 항목 | 파일 | 설명 |
|------|------|------|
| XML 문서화 없음 | AuthRecorder, AuthOptions 등 | public 클래스에 XML doc 없음 |
| `IsConfigured()` 매직 스트링 | `AuthOptions.cs` | `"your-"` 문자열 검사는 fragile |
| 접근 제한자 비일관 | 전체 | AuthRecorder는 public, Endpoints는 internal — 전략이 보이지 않음 |
| `AdminMemberService.GetAllAsync` | `AdminMemberService.cs` | ALL sessions + ALL profiles 메모리 로드. 소규모엔 OK, 스케일하면 문제 |

---

## 잘한 부분 (공정하게)

| 항목 | 평가 |
|------|------|
| Login Open Redirect 방어 | `IsLocalReturnUrl()`로 `//`, `/\` 패턴까지 차단 - **훌륭** |
| CSRF 보호 | Antiforgery middleware가 POST/PUT/DELETE/PATCH 전부 커버 |
| Rate Limiting | auth 엔드포인트에 Fixed Window 적용 |
| Security Headers | CSP, X-Content-Type-Options, Referrer-Policy 등 모두 설정 |
| Cookie 설정 | HttpOnly, environment별 Secure 분기, SameSite=Lax |
| 세션 유효성 실시간 검증 | 쿠키 이벤트에서 DB 기반 세션 검증 (보안적으로 매우 강력) |
| Audit Logging | 로그인/로그아웃/실패/접근거부 전부 기록 |
| Options Validation | `ValidateOnStart()` + custom validator 패턴 |
| TestLogin 환경 격리 | Development/Testing에서만 활성화 |
| 테스트 커버리지 | 세션 검증, CSRF, Rate Limit, 역할 변경 감지까지 테스트 있음 |

---

## 우선순위 정리

| 순위 | 이슈 | 난이도 | 보안 영향 |
|------|------|--------|----------|
| **1** | Logout Open Redirect | 10분 | HIGH |
| **2** | ValidateSessionAsync 성능 | 2~4시간 | MEDIUM (가용성) |
| **3** | AuthRecorder 인터페이스 추출 | 1~2시간 | - (유지보수성) |
| **4** | Structured Logging 추가 | 1시간 | MEDIUM (모니터링) |
| **5** | Profile upsert race condition | 30분 | LOW~MEDIUM |
| **6** | Session cleanup job | 1~2시간 | LOW (장기 가용성) |

---

## 리뷰 대상 파일 전체 목록

### Infrastructure / Auth (Core Authentication Setup)
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthServiceCollectionExtensions.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRuntimeExtensions.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthOptions.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthOptionsValidator.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthClaimTypes.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRecorder.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AuthRecordResult.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AppCookieAuthenticationEvents.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Auth/AppOpenIdConnectEvents.cs`

### Security / Middleware
- `backend/src/WoongBlog.Api/Infrastructure/Security/SecurityServiceCollectionExtensions.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Security/SecurityOptions.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Security/SecurityOptionsValidator.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Security/AntiforgeryValidationMiddleware.cs`
- `backend/src/WoongBlog.Api/Infrastructure/Security/SecurityHeadersMiddleware.cs`

### Identity Endpoints (API)
- `backend/src/WoongBlog.Api/Modules/Identity/Api/Login/LoginEndpoint.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/Login/LoginRequest.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/Logout/LogoutEndpoint.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/Logout/LogoutRequest.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/TestLogin/TestLoginEndpoint.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/TestLogin/TestLoginRequest.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/GetSession/GetSessionEndpoint.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/GetCsrf/GetCsrfEndpoint.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/GetAdminMembers/GetAdminMembersEndpoint.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/IdentityApiPaths.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/IdentityEndpoints.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Api/IdentityModule.cs`

### Identity Application Services
- `backend/src/WoongBlog.Api/Modules/Identity/Application/IdentityInteractionService.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Application/IIdentityInteractionService.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Application/Abstractions/IAdminMemberService.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Application/GetAdminMembers/GetAdminMembersQuery.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Application/GetAdminMembers/GetAdminMembersQueryHandler.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/Application/GetAdminMembers/AdminMemberListItemDto.cs`

### Identity Persistence
- `backend/src/WoongBlog.Api/Modules/Identity/Persistence/AdminMemberService.cs`
- `backend/src/WoongBlog.Api/Modules/Identity/IdentityModuleServiceCollectionExtensions.cs`

### Domain Entities
- `backend/src/WoongBlog.Api/Domain/Entities/AuthSession.cs`
- `backend/src/WoongBlog.Api/Domain/Entities/AuthAuditLog.cs`

### Configuration
- `backend/src/WoongBlog.Api/appsettings.json`
- `backend/src/WoongBlog.Api/appsettings.Development.json`
- `backend/src/WoongBlog.Api/Program.cs`

### 테스트 파일
- `backend/tests/WoongBlog.Api.Tests/AuthSecurityTests.cs`
- `backend/tests/WoongBlog.Api.Tests/AuthRecorderTests.cs`
- `backend/tests/WoongBlog.Api.Tests/AuthEndpointsTests.cs`
- `backend/tests/WoongBlog.Api.Tests/TestAuthHandler.cs`
- `backend/tests/WoongBlog.Api.Tests/AdminMembersEndpointsTests.cs`
- `backend/tests/WoongBlog.Api.Tests/CustomWebApplicationFactory.cs`
