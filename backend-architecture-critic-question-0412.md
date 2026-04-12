# Backend Authentication/Login 코드 리뷰 Q&A (2026-04-12)

이 문서는 `backend-architecture-diagnosis.md` 리뷰 이후 논의된 모든 질문과 답변을 기록합니다.

---

## Q1. Logout Open Redirect — returnUrl은 백엔드에서밖에 못 보내지 않나?

### 원래 진단 내용
```csharp
// IdentityInteractionService.LogoutAsync
return string.IsNullOrWhiteSpace(returnUrl) ? _authOptions.SignedOutRedirectPath : returnUrl;
// → { redirectUrl: "https://evil-phishing-site.com" } 가 그대로 나감
```

### 반론
returnUrl은 백엔드에서밖에 못 보내지 않느냐는 지적.

### 답변
"백엔드에서만 보낸다"는 건 **응답** 기준에서는 맞지만, 문제는 백엔드가 **요청에서 받은 값을 그대로 응답에 돌려준다**는 것입니다.

`[AsParameters] LogoutRequest request` — 이게 핵심. `[AsParameters]`는 **클라이언트가 보낸 query string에서 바인딩**합니다:

```
POST /api/auth/logout?ReturnUrl=https://evil.com
                       ↑ 클라이언트가 URL에 넣어서 전송
```

```csharp
// IdentityInteractionService
return string.IsNullOrWhiteSpace(returnUrl) ? _authOptions.SignedOutRedirectPath : returnUrl;
// 결과: { redirectUrl: "https://evil.com" }  ← 그대로 반환
```

프론트엔드가 이 `redirectUrl`을 받아서 `window.location = redirectUrl` 하는 순간 redirect 됩니다.

### 심각도 재조정
CSRF 보호가 `/api/auth/logout`에 걸려 있어서 **외부 사이트에서 직접 위조 POST는 불가**합니다. 따라서 공격 체인이 길어집니다:

1. **XSS 취약점이 있을 때** → 공격자가 CSRF 토큰 획득 후 malicious ReturnUrl로 logout API 호출
2. **프론트엔드가 `redirectUrl`을 blind follow 할 때** → 사용자가 피싱 사이트로 이동

즉, **XSS + Open Redirect 연쇄 공격**이어야 성립하므로 CRITICAL보다는 **MEDIUM**이 더 정확한 등급입니다.

단, 수정 자체는 5분짜리이고 프론트엔드도 `redirectUrl`을 맹목적으로 따르지 않도록 같이 방어해두는 게 좋습니다.

---

## Q2. Open Redirect를 어떻게 고쳐야 하나?

### 수정 방법

두 군데 고치면 됩니다. `IsLocalReturnUrl` 검증이 `LoginEndpoint.cs` private static 메서드로만 있어서 서비스에서 재사용이 안 됩니다.

#### Fix 1 — 공통 유틸로 추출

`Common/Api/Validation/LocalUrlValidator.cs` 신규 생성:

```csharp
namespace WoongBlog.Api.Common.Api.Validation;

internal static class LocalUrlValidator
{
    internal static bool IsLocalUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.StartsWith('/') && !value.StartsWith("//") && !value.StartsWith("/\\");
    }
}
```

#### Fix 2 — LoginEndpoint.cs 에서 private 메서드 제거 후 공통 유틸 사용

```csharp
// Before
var returnUrl = IsLocalReturnUrl(request.ReturnUrl) ? request.ReturnUrl! : "/admin";

// After
var returnUrl = LocalUrlValidator.IsLocalUrl(request.ReturnUrl) ? request.ReturnUrl! : "/admin";
```

#### Fix 3 — IdentityInteractionService.LogoutAsync에 검증 추가

```csharp
// Before (취약)
return string.IsNullOrWhiteSpace(returnUrl) ? _authOptions.SignedOutRedirectPath : returnUrl;

// After (안전)
return LocalUrlValidator.IsLocalUrl(returnUrl) ? returnUrl! : _authOptions.SignedOutRedirectPath;
```

`/`로 시작하고 `//`나 `/\`가 아닌 경우만 통과 — Login과 동일한 기준.

---

## Q3. ValidateSessionAsync는 언제 불리는가?

### 답변

`ValidatePrincipal`은 ASP.NET Core Cookie 인증의 **built-in 이벤트**입니다.

**인증 쿠키가 있는 모든 요청에서**, 쿠키를 복호화해서 Principal을 만들 때마다 호출됩니다.

```
클라이언트 요청 (쿠키 포함)
  → UseAuthentication() 미들웨어
    → 쿠키 복호화
      → ValidatePrincipal 이벤트 발생  ← 여기
        → ValidateSessionAsync 호출
          → DB 2~3회 쿼리
```

즉 `/api/admin/posts`, `/api/auth/session`, `/api/admin/dashboard` 등 **쿠키를 가진 사용자의 모든 요청마다** 실행됩니다.

### 대안 — throttle 방식

`ValidatePrincipal`을 **매 요청**이 아니라 **일정 주기(예: 5분)에만** 실행하도록 throttle할 수 있습니다:

```csharp
public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
{
    // 마지막 검증 시각을 쿠키 properties에 저장
    var lastValidated = context.Properties.GetString("last_validated");
    if (DateTimeOffset.TryParse(lastValidated, out var last)
        && DateTimeOffset.UtcNow - last < TimeSpan.FromMinutes(5))
    {
        return; // 5분 이내면 DB 조회 스킵
    }

    var isValid = await recorder.ValidateSessionAsync(...);
    // ...
    context.Properties.SetString("last_validated", DateTimeOffset.UtcNow.ToString("O"));
    context.ShouldRenew = true; // 갱신된 properties를 쿠키에 적음
}
```

이렇게 하면 DB 조회가 매 요청 → **5분에 1번**으로 줄어듭니다.

---

## Q4. 쿠키에는 어떤 정보가 들어있나? / 블로그 어드민 API 요청 개수 근거는?

### 쿠키에 들어있는 정보

ASP.NET Core Data Protection으로 **암호화된** ClaimsPrincipal 전체가 쿠키 안에 들어갑니다. 복호화하면 이 claims들:

```
NameIdentifier  (Google sub)
Email
Name
app:profile_id  (DB의 Profile.Id)
app:role        (admin / user)
app:session_id  (DB의 AuthSession.Id)
ClaimTypes.Role (admin / user)  ← 표준 role claim도 별도로 중복 저장
```

`AppOpenIdConnectEvents.TokenValidated`에서 직접 추가하는 것들입니다. 즉 **DB에서 조회할 수 있는 정보들이 쿠키에 이미 다 있습니다.**

`ValidatePrincipal`이 DB를 조회하는 이유는 "쿠키에 있는 sessionId가 취소/만료되지 않았는지 실시간 확인"하기 위함입니다.

### 어드민 API 요청 개수 — 실제 코드 기반 확인

"5~10개"는 과장이었습니다. 코드를 직접 확인하면:

**어드민 대시보드 한 번 열 때 Next.js 서버 → 백엔드 요청:**

| 파일 | 호출 | ValidatePrincipal 발동 |
|------|------|----------------------|
| `layout.tsx` | `fetchServerSession()` | 1회 |
| `dashboard/page.tsx` | `fetchAdminDashboardSummary()` | 1회 |
| `dashboard/page.tsx` | `fetchAdminWorks()` | 1회 |
| `dashboard/page.tsx` | `fetchAdminBlogs()` | 1회 |

= **4번의 백엔드 호출 → 4번의 ValidatePrincipal → 4 × (2~3 DB 쿼리) = 8~12번 DB 쿼리**

**어드민 members 페이지는:**
- layout(1) + fetchAdminMembers(1) = **2번 → 4~6 DB 쿼리**

"5~10개"는 과장이었고, 실제론 **페이지당 2~4 백엔드 요청**입니다. 하지만 매 요청마다 DB 2~3번 왕복한다는 사실 자체는 동일합니다.

---

## Q5. ClaimsPrincipal이 뭐고, ValidatePrincipal에서 일어나는 일이 뭔가?

### ClaimsPrincipal

"이 사람이 누구인가"를 나타내는 C# 객체입니다. 쿠키에서 복호화된 데이터가 이 형태로 메모리에 올라옵니다.

```csharp
ClaimsPrincipal
 └─ ClaimsIdentity (인증 방식: "Cookies")
     ├─ Claim("app:session_id", "3f2a1b...")   ← DB AuthSession.Id
     ├─ Claim("app:profile_id", "9c4e2d...")   ← DB Profile.Id
     ├─ Claim("app:role", "admin")
     ├─ Claim(ClaimTypes.Role, "admin")
     ├─ Claim(ClaimTypes.Email, "kim@...")
     ├─ Claim(ClaimTypes.Name, "웅곤")
     └─ Claim(ClaimTypes.NameIdentifier, "google_sub_xyz")
```

`principal.FindFirstValue(AuthClaimTypes.SessionId)` — 이게 저 Claim 목록에서 값 꺼내는 겁니다.

### ValidatePrincipal에서 실제로 일어나는 일

코드 흐름을 직선으로 펴면:

```
요청 도착 (쿠키 포함)
    ↓
UseAuthentication()
    ↓ Data Protection: HMAC 검증 → AES 복호화
    ↓ ClaimsPrincipal 생성 (위의 객체)
    ↓
ValidatePrincipal(context) 호출
    ↓
    [1] 쿠키에서 session_id 꺼냄
        principal.FindFirstValue("app:session_id") → "3f2a1b..."
    ↓
    [2] DB에서 AuthSession 조회
        SELECT * FROM AuthSessions WHERE Id = '3f2a1b...'
    ↓
    [3] 세 가지 실패 조건 체크
        - session이 없거나 RevokedAt이 있으면 → false
        - ExpiresAt 지났으면 → RevokedAt 기록 후 false
        - LastSeenAt + SlidingExpiration 지났으면 → RevokedAt 기록 후 false
    ↓
    [4] DB에서 Profile 조회 (role 변경 감지용)
        SELECT * FROM Profiles WHERE Id = '9c4e2d...'
    ↓
    [5] 쿠키의 role claim vs DB의 profile.Role 비교
        "admin" == "admin" → OK
        (관리자 권한이 중간에 취소됐을 때 즉시 차단하는 용도)
    ↓
    [6] session.LastSeenAt = now → SaveChanges (활성 확인 기록)
    ↓
    return true → 요청 통과
```

실패(`false`) 반환 시:
```csharp
context.RejectPrincipal();  // Principal 무효화
await context.HttpContext.SignOutAsync(...);  // 쿠키 삭제
// → 다음 요청에서 401 → /login 리다이렉트
```

### 한 줄 요약

> 쿠키 위조 여부는 Data Protection이 보장하고, `ValidatePrincipal`은 "위조는 안 됐는데 이 세션이 중간에 강제 종료되거나 권한 변경됐냐?"를 DB에서 확인하는 단계입니다.

---

## Q6. 코드에서는 서명 관련 로직이 안 보이는데, 실제로 있는 과정인가?

### 질문 내용
ValidatePrincipal 이벤트 이전에 Data Protection API가 HMAC 서명 확인 → AES 복호화 → 객체 생성 순서로 동작한다는 설명이 실제 코드에 존재하는지.

### 답변

해당 작업은 ASP.NET Core 프레임워크 내부에서 자동으로 처리됩니다. 코드에서 이를 셋업하는 부분:

```csharp
// AuthServiceCollectionExtensions.cs
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(authOptions.DataProtectionKeysPath))
    .SetApplicationName("WoongBlog.Api");
```

그리고 실제 파이프라인:

```csharp
// Program.cs — 순서가 핵심
app.UseAuthentication();                         // ← Data Protection 복호화+검증 여기서 끝
app.UseMiddleware<AntiforgeryValidationMiddleware>();
app.UseAuthorization();
```

`UseAuthentication()` 안에서 일어나는 일:
1. `CookieAuthenticationDefaults.AuthenticationScheme` 핸들러 실행
2. 쿠키 문자열 → Data Protection API (HMAC 검증 → AES 복호화)
3. → `ClaimsPrincipal` 생성
4. → `AppCookieAuthenticationEvents.ValidatePrincipal` 호출

**핵심:** `ValidatePrincipal`에 도달했다는 것 자체가 이미 "위조되지 않은 쿠키"임을 보장합니다. 그래서 코드에 서명 검증 로직이 없는 겁니다. 이미 프레임워크 레벨에서 끝났습니다.

`ValidatePrincipal`에서 하는 DB 조회는 서명 검증이 아니라, **"쿠키는 진짜인데 이 세션이 그 사이에 취소됐냐?"** 를 확인하는 추가 비즈니스 로직입니다.

---

## Q7. session_id, profile_id, role은 누가 정하나?

### 답변

**백엔드가 정합니다** — 클라이언트는 개입 불가.

#### 1단계 — role 결정 (`AuthRecorder.ResolveRole`)
```csharp
private string ResolveRole(string email)
    => _authOptions.AdminEmails.Any(x => string.Equals(x, email, StringComparison.OrdinalIgnoreCase))
        ? "admin"
        : "user";
```
`appsettings.json`의 `Auth:AdminEmails` 배열에 이메일이 있으면 `"admin"`, 없으면 `"user"`.
**구글이 준 이메일 claim을 서버가 직접 조회해서 판단합니다.**

#### 2단계 — session_id, profile_id 생성 (`AuthRecorder.RecordSuccessfulLoginAsync`)
```csharp
// profile_id = DB에 upsert된 Profile의 PK
profile = new Profile { Id = Guid.NewGuid(), ... };

// session_id = 새로 생성한 AuthSession의 PK  
var session = new AuthSession { ProfileId = profile.Id, ... };
_dbContext.AuthSessions.Add(session);

await _dbContext.SaveChangesAsync(cancellationToken);
return new AuthRecordResult(profile.Id, profile.Email, profile.Role, session.Id, ...);
```

#### 3단계 — 쿠키에 claim으로 기록 (`AppOpenIdConnectEvents.TokenValidated`)
```csharp
var result = await recorder.RecordSuccessfulLoginAsync(...);

identity.AddClaim(new Claim(AuthClaimTypes.ProfileId, result.ProfileId.ToString()));
identity.AddClaim(new Claim(AuthClaimTypes.Role, result.Role));
identity.AddClaim(new Claim(AuthClaimTypes.SessionId, result.SessionId.ToString()));
```

#### 요약

| 값 | 누가 | 기준 |
|---|---|---|
| `role` | `AuthRecorder.ResolveRole()` | `AdminEmails` 배열에 있으면 admin |
| `profile_id` | DB `Profiles` 테이블 | 첫 로그인 시 `Guid.NewGuid()` 생성, 이후 재사용 |
| `session_id` | DB `AuthSessions` 테이블 | 로그인할 때마다 `Guid.NewGuid()` 신규 생성 |

클라이언트(브라우저)는 이 값들을 **읽지도, 바꾸지도 못합니다** — 암호화된 쿠키 안에 있고, Data Protection 키 없이는 조작 불가능합니다.

---

## Q8. `app.UseAuthentication()` 여기서 검증하는 데이터는 프론트엔드가 넘기는 게 맞나?

### 답변

맞습니다. 정확히는 **브라우저가 자동으로 넘깁니다.**

```
브라우저 → 요청 헤더에 쿠키 자동 포함
──────────────────────────────────────
GET /api/admin/dashboard
Cookie: portfolio_auth=CfDJ8Kx3mN... (암호화된 긴 문자열)
──────────────────────────────────────
           ↓
app.UseAuthentication()  ← 이 쿠키를 받아서 처리
```

**브라우저가 자동으로** 합니다. JS 코드가 쿠키를 직접 꺼내서 헤더에 붙이는 게 아니라, 브라우저가 해당 도메인의 쿠키를 요청마다 알아서 첨부합니다.

#### 프론트엔드가 관여하는 것 vs 안 하는 것

| | 누가 | 방법 |
|---|---|---|
| 쿠키 **설정** | 백엔드 `Set-Cookie` 응답 헤더 | 로그인 성공 시 서버가 씁니다 |
| 쿠키 **전송** | 브라우저 자동 | 매 요청마다 `Cookie` 헤더에 자동 첨부 |
| 쿠키 **읽기/수정** | ❌ 불가 | `HttpOnly=true`라서 JS에서 접근 자체가 막힙니다 |
| 쿠키 **검증** | 백엔드 `UseAuthentication()` | Data Protection으로 복호화+검증 |

#### 핵심

```csharp
options.Cookie.HttpOnly = true;  // AuthServiceCollectionExtensions.cs
```

이 한 줄 때문에 프론트엔드 JS는 쿠키 값을 **볼 수도, 건드릴 수도 없습니다.** 브라우저가 자동으로 넘기기만 할 뿐입니다.

---

## Q9. 쿠키 암호화 문자열은 백엔드가 넘겨주는 것이고 이걸 대상으로 HMAC을 수행하는 건가?

### 답변

맞습니다. 순서대로:

#### 쿠키 값이 만들어지는 순서 (백엔드 → 브라우저)

로그인 성공 직후, 백엔드가 쿠키를 만들 때:

```
1. ClaimsPrincipal 직렬화 (JSON/Binary)
   { session_id, profile_id, role, email, ... }
          ↓
2. AES 암호화 (Data Protection)
   → 알아볼 수 없는 바이트 덩어리
          ↓
3. HMAC-SHA256 서명 추가
   → 암호화된 데이터 + 서명값 합체
          ↓
4. Base64URL 인코딩
   → CfDJ8Kx3mN...  (이 문자열)
          ↓
Set-Cookie: portfolio_auth=CfDJ8Kx3mN...; HttpOnly; SameSite=Lax
```

#### 다음 요청에서 검증할 때 (브라우저 → 백엔드)

```
Cookie: portfolio_auth=CfDJ8Kx3mN...
          ↓
1. Base64URL 디코딩
          ↓
2. HMAC 서명 검증  ← 조작됐으면 여기서 즉시 reject
   (서버의 비밀키로 서명 재계산 → 쿠키 안의 서명과 비교)
          ↓
3. AES 복호화
          ↓
4. ClaimsPrincipal 역직렬화
   → { session_id: "3f2a...", role: "admin", ... }
          ↓
ValidatePrincipal 호출 (DB 세션 확인)
```

#### HMAC의 역할

**"이 쿠키를 우리 서버가 만들었다"** 를 증명합니다.

```
공격자가 쿠키를 조작 시도:
CfDJ8Kx3mN[...role=user...] → CfDJ8Kx3mN[...role=admin...]
                                          ↑
                               HMAC 서명이 맞지 않음 → 즉시 폐기
```

비밀키(`DataProtectionKeysPath`에 저장된 키)가 없으면 올바른 서명을 만들 수 없으니까, 서버만이 유효한 쿠키를 발급할 수 있습니다.

---

## Q10. ValidateSessionAsync의 DB 3회 호출 증거와 SQL 테이블 위치

### DB 3회 호출 증거 — 코드 라인 추적

`ValidateSessionAsync` (line 176~230)의 **happy path** (세션 유효한 경우)를 따라가면:

```csharp
// [DB 1회차] line 193 — AuthSessions 테이블 SELECT
var session = await _dbContext.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

// ... 만료/취소 체크 통과 ...

// [DB 2회차] line 213 — Profiles 테이블 SELECT  
var profile = await _dbContext.Profiles.SingleOrDefaultAsync(x => x.Id == profileId, cancellationToken);

// ... role 비교 통과 ...

// [DB 3회차] line 228~229 — LastSeenAt UPDATE
session.LastSeenAt = now;
await _dbContext.SaveChangesAsync(cancellationToken);  // ← EF가 UPDATE SQL 실행
```

실패 경로에서는 더 많이 호출될 수도 있습니다 — 만료/취소 시마다 `SaveChangesAsync`로 `RevokedAt`을 갱신합니다.

### SQL 테이블 위치

`WoongBlogDbContext.cs`에서 정의:

```csharp
public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
public DbSet<AuthAuditLog> AuthAuditLogs => Set<AuthAuditLog>();
public DbSet<Profile> Profiles => Set<Profile>();
```

#### 테이블 3개와 매핑

| DbSet | 엔티티 | PostgreSQL 테이블 | ValidateSessionAsync에서 |
|---|---|---|---|
| `_dbContext.AuthSessions` | `AuthSession` | `AuthSessions` | line 193: `SingleOrDefaultAsync` → **SELECT 1회** |
| `_dbContext.Profiles` | `Profile` | `Profiles` | line 213: `SingleOrDefaultAsync` → **SELECT 1회** |
| (같은 DbContext) | | | line 229: `SaveChangesAsync` → **UPDATE 1회** |

#### DB 인덱스 (OnModelCreating에서 확인)

```csharp
// AuthSession → PK(Id)로 조회하니까 인덱스 탐. 추가로:
entity.HasIndex(x => x.ProfileId);
entity.HasIndex(x => x.SessionKey).IsUnique();

// Profile → PK(Id)로 조회하니까 인덱스 탐. 추가로:
entity.HasIndex(x => x.Email);
entity.HasIndex(x => new { x.Provider, x.ProviderSubject }).IsUnique();
```

둘 다 **PK 조회**라서 쿼리 자체는 빠릅니다. 문제는 횟수 × 빈도입니다.

#### 실제 SQL

```sql
-- 1. SELECT (AuthSessions)
SELECT * FROM "AuthSessions" WHERE "Id" = @sessionId

-- 2. SELECT (Profiles)
SELECT * FROM "Profiles" WHERE "Id" = @profileId

-- 3. UPDATE (LastSeenAt)
UPDATE "AuthSessions" SET "LastSeenAt" = @now WHERE ...
```

PK 조회라 각각 1ms 미만이겠지만, **모든 인증 요청마다 3번**입니다. 대시보드 한 번 열면 4 API × 3 DB = **12 DB roundtrip**이 세션 검증만으로 발생합니다.

---

## Q11. DB 캐싱/JOIN/배치가 왜 해결책인가?

### 현재 문제

```
매 인증 요청마다:
  SELECT AuthSessions   (1 roundtrip)
  SELECT Profiles        (1 roundtrip)  
  UPDATE LastSeenAt      (1 roundtrip)
  = 3 DB roundtrip × 요청 수
```

### 해결책 1: In-memory 캐시 (5분 TTL)

```
첫 번째 요청:  DB 3회 → 결과를 메모리에 저장 (key: session_id)
2번째~N번째:   메모리에서 꺼냄 → DB 0회
5분 후:        캐시 만료 → 다시 DB 3회 → 캐시 갱신
```

**왜 해결이 되냐:** 5분 동안 관리자가 API를 20번 호출해도 DB는 3번만 맞습니다. 현재는 60번 맞습니다.

**트레이드오프:** 세션을 강제 취소해도 최대 5분간 유효할 수 있음. 개인 블로그에서 관리자 1~2명이면 허용 가능한 수준.

### 해결책 2: LastSeenAt 배치 처리

현재 문제의 **1/3이 UPDATE**입니다:

```csharp
session.LastSeenAt = now;
await _dbContext.SaveChangesAsync(cancellationToken);  // ← 매번 UPDATE 쿼리
```

"이 사용자가 마지막으로 활동한 시각"을 **매 요청마다 DB에 쓸 필요가 있냐?** 라는 질문입니다.

```
현재: 요청 올 때마다   → UPDATE AuthSessions SET LastSeenAt = now
개선: 메모리에 임시 저장 → 1분마다 한 번에 UPDATE (BackgroundService)
```

**왜 해결이 되냐:** 1분에 20번 UPDATE → 1번 UPDATE. 쓰기 부하가 1/20로 줄어듭니다.

### 해결책 3: JOIN 쿼리로 축소

현재:
```sql
-- roundtrip 1
SELECT * FROM "AuthSessions" WHERE "Id" = @sid;
-- roundtrip 2  
SELECT * FROM "Profiles" WHERE "Id" = @pid;
```

개선:
```sql
-- roundtrip 1 (한 번에 끝)
SELECT s.*, p."Role" 
FROM "AuthSessions" s
JOIN "Profiles" p ON p."Id" = s."ProfileId"
WHERE s."Id" = @sid;
```

**왜 해결이 되냐:** DB 연결 → 쿼리 전송 → 응답 대기 → 수신이 **네트워크 왕복 1번**으로 줄어듭니다. 쿼리 2개 따로 보내면 이 왕복이 2번입니다.

### 효과 비교

| 방법 | 요청 20번 기준 DB roundtrip | 현재 대비 |
|---|---|---|
| 현재 | 60회 (20 × 3) | — |
| JOIN만 적용 | 40회 (20 × 2) | 33% 감소 |
| 캐시(5분) | 3회 | **95% 감소** |
| 캐시 + 배치 LastSeenAt | 2회 (SELECT만) + 배치 1회 | **95%+ 감소** |

개인 블로그에서 가장 현실적인 건 **캐시 한 줄 추가**입니다. JOIN이나 배치는 트래픽이 실제로 문제될 때 하면 됩니다.

---

## Q12. `[AsParameters]` — LogoutRequest가 Query String 바인딩이라는 게 무슨 내용인가?

### 답변

`[AsParameters]`가 Minimal API에서 파라미터 바인딩 소스를 지정하는 어트리뷰트입니다. `[AsParameters]`는 클래스 프로퍼티들을 **query string에서 바인딩**합니다.

```
POST /api/auth/logout?ReturnUrl=/admin
                      ↑
                      query string에서 LogoutRequest.ReturnUrl로 바인딩
```

#### 뭐가 문제냐

**기능상 문제는 없습니다.** 그래서 LOW 항목입니다.

다만 관행상 POST 요청에서 데이터를 넘길 때는 body에 담는 게 일반적입니다:

```
# 현재 방식 (query string)
POST /api/auth/logout?ReturnUrl=/admin

# 일반적 방식 (request body)
POST /api/auth/logout
Content-Type: application/json
{ "returnUrl": "/admin" }
```

**query string 방식의 단점:**
1. URL에 값이 노출됨 → 서버 access log에 `ReturnUrl` 값이 그대로 기록됨
2. 브라우저 히스토리에 URL이 남을 수 있음
3. ReturnUrl이 민감한 경로라면 로그에서 추적 가능

**실제 영향도:** 이 프로젝트에서 `ReturnUrl`은 `/admin` 같은 경로라서 민감한 정보가 아닙니다. 그래서 LOW로 분류.

---

## Q13. Session이 계속 쌓여서 메모리가 터질 수도 있다는 말인가?

### 답변

아니요. **메모리가 아니라 DB 디스크**입니다.

로그인할 때마다 `AuthSession` 행이 하나씩 **INSERT**됩니다:

```csharp
// AuthRecorder.RecordSuccessfulLoginAsync
var session = new AuthSession { ... };
_dbContext.AuthSessions.Add(session);
await _dbContext.SaveChangesAsync(cancellationToken);
```

그런데 만료되거나 취소된 세션을 **DELETE하는 코드가 어디에도 없습니다.**

`RevokedAt`을 설정하긴 하지만 그 행을 지우지는 않습니다:

```csharp
session.RevokedAt = now;  // UPDATE만 함, DELETE 없음
await _dbContext.SaveChangesAsync(cancellationToken);
```

#### 결과

```
로그인 100회 = AuthSessions에 100개 행 쌓임
로그인 10000회 = 10000개 행, 99% 이상이 만료/취소 상태
```

**메모리는 관계없습니다.** DB 테이블(PostgreSQL 디스크)에 쌓입니다.

#### 성능 영향

- `ValidateSessionAsync`의 `WHERE Id = @sessionId` **PK 조회**라서 행이 늘어도 느려지지 않습니다.
- 실제로 느려질 수 있는 건 `AdminMemberService.GetAllAsync`입니다 — 거기서 전체 세션을 메모리로 다 불러옵니다:

```csharp
// AdminMemberService.cs
var activeSessions = await _dbContext.AuthSessions
    .AsNoTracking()
    .Where(session => session.RevokedAt == null && ...)
    .ToListAsync(cancellationToken);  // ← 조건부지만 여전히 전수 스캔
```

개인 블로그 수준에서는 실질적 문제가 될 만큼 쌓이기 어렵지만, 정리 job이 없다는 사실은 맞습니다.
