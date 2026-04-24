# Public Session + Blog Editor QoL Diff 해설

Date: 2026-04-21

## 한 줄 요약

이번 diff는 “공개 페이지에서 admin 전용 UI를 보여줄지 판단하는 흐름”을 한 곳으로 모으고, Contact 페이지가 작성자가 쓴 콘텐츠를 그대로 존중하게 만들고, BlogEditor에서 상단 저장/가로 리사이즈/Excerpt 보존을 개선한 PR이다.

백엔드 인증, 권한 정책, `/auth/session` 응답 구조, 공개 읽기 페이지 접근 정책은 바꾸지 않았다.

## 변경 묶음

### 1. 공개 레이아웃의 불필요한 세션 조회 제거

관련 파일:

- `src/app/(public)/layout.tsx`
- `src/components/layout/Navbar.tsx`

기존 흐름:

1. public layout이 매 요청마다 `fetchServerSession()`을 호출했다.
2. 그 결과를 `<Navbar session={session} />`로 넘겼다.
3. 그런데 `Navbar`는 실제로 `session` prop을 사용하지 않았다.

변경 후:

1. public layout은 사이트 설정만 가져온다.
2. `Navbar`에는 `ownerName`만 넘긴다.
3. `NavbarProps`에서 사용하지 않는 `session` 타입을 제거했다.

의미:

- 공개 페이지 전체 렌더링에서 죽은 세션 의존성이 사라졌다.
- Navbar는 계속 public-only navigation 역할만 한다.
- 로그인/관리자/로그아웃 같은 계정 UI는 public navbar에 추가하지 않았다.

리스크:

- 없음에 가깝다. 기존에도 Navbar가 session을 쓰지 않았기 때문에 동작 제거가 아니라 dead dependency 제거다.

## 2. public admin affordance gate 중앙화

관련 파일:

- `src/lib/auth/public-admin.ts`
- `src/components/admin/PublicAdminLink.tsx`
- `src/app/(public)/blog/page.tsx`
- `src/app/(public)/blog/[slug]/page.tsx`
- `src/app/(public)/works/page.tsx`
- `src/app/(public)/works/[slug]/page.tsx`
- `src/app/(public)/contact/page.tsx`
- `src/app/(public)/introduction/page.tsx`
- `src/app/(public)/resume/page.tsx`

새 helper:

```ts
export function canShowPublicAdminAffordances(session) {
  return session?.authenticated === true && session.role === 'admin'
}

export async function getPublicAdminAffordanceState() {
  const session = await fetchServerSession()
  return {
    session,
    canShowAdminAffordances: canShowPublicAdminAffordances(session),
  }
}
```

기존 흐름:

- 각 public page 또는 `PublicAdminLink`가 직접 `fetchServerSession()`을 호출했다.
- 여러 파일에 `session.authenticated && session.role === 'admin'` 조건이 흩어져 있었다.
- 특히 `PublicAdminLink`도 자체 세션 fetch를 하면서 page-level inline editor 조건과 별개로 동작했다.

변경 후:

- public page가 `getPublicAdminAffordanceState()`를 한 번 호출한다.
- 그 결과의 `canShowAdminAffordances` boolean으로 inline editor, create shell, manage link를 모두 제어한다.
- `PublicAdminLink`는 더 이상 async/server session fetch를 하지 않는다. 그냥 `canShow`를 받아 렌더링만 한다.

의미:

- 공개 페이지에서 admin 전용 UI를 보여줄지 판단하는 기준이 하나로 통일됐다.
- admin affordance visibility의 source of truth가 `src/lib/auth/public-admin.ts`로 모였다.
- anonymous public rendering은 그대로 가능하다. helper가 false를 반환하면 admin UI만 빠진다.
- 백엔드 보안은 여전히 backend/admin API 권한 체크와 CSRF가 담당한다. 프론트의 `canShow`는 “보여줄지”만 결정한다.

주의할 점:

- `getPublicAdminAffordanceState()`는 public page마다 한 번은 session을 조회한다. 완전 제거가 아니라 중복/산재 제거다.
- public layout의 dead session fetch는 제거했지만, admin affordance가 있는 개별 public page는 여전히 admin UI 표시 판단을 위해 session을 조회한다.

## 3. Contact 페이지 fallback email UI 제거

관련 파일:

- `src/app/(public)/contact/page.tsx`
- `tests/public-contact-fallback-email.spec.ts`
- `tests/ui-improvement-static-public-pages.spec.ts`
- `src/test/public-admin-rendering.test.tsx`

기존 흐름:

1. contact content JSON을 파싱한다.
2. 파싱 결과에 `mailto:`가 없으면 별도의 “Direct email” fallback block을 추가한다.
3. local QA용 `__qaNoMailto=1` override로 강제로 mailto 없는 콘텐츠를 만들 수 있었다.

문제:

- 작성자가 Contact 페이지에 쓴 콘텐츠 외에 시스템이 임의 UI를 삽입했다.
- 즉, authored content fidelity가 깨졌다.

변경 후:

- `hasMailtoLink`, `fallbackEmail`, “Direct email” block 전체 제거.
- `__qaNoMailto` override 제거.
- 페이지는 `fetchPublicPageBySlug('contact')`에서 온 콘텐츠를 그대로 렌더링한다.
- admin inline edit entry point는 그대로 유지한다.

의미:

- Contact 페이지는 작성 콘텐츠를 있는 그대로 보여준다.
- 메일 링크가 필요하면 작성자가 contact page content 안에 직접 넣어야 한다.

테스트 변화:

- 기존 “fallback email이 보인다” 테스트는 “fallback email이 주입되지 않는다” 테스트로 바뀌었다.
- static public page layout 테스트도 contact에 mailto가 반드시 있어야 한다는 기대를 제거했다.

## 4. BlogEditor QoL 개선

관련 파일:

- `src/components/admin/BlogEditor.tsx`
- `src/test/blog-editor.test.tsx`
- `tests/ui-admin-blog-excerpt.spec.ts`
- `tests/manual-qa-auth-gap.spec.ts`

### 4.1 상단 quick save/back action bar 추가

기존:

- 저장 버튼이 하단에만 있었다.
- 편집 내용이 길어지면 저장하려고 아래까지 내려가야 했다.

변경 후:

- form 맨 위에 sticky action bar를 추가했다.
- 상단에 상태 텍스트가 나온다.
  - `Unsaved changes`
  - `No unsaved changes`
- 상단 Save 버튼은 submit button이다.
- 하단 `Create Post` / `Update Post` 버튼은 그대로 남겼다.

저장 가능 조건:

```ts
const isSaveDisabled = isSaving || !hasUnsavedChanges || !title.trim()
```

의미:

- 상단 Save와 하단 Create/Update가 같은 form submit 경로를 탄다.
- 저장 로직을 복제하지 않았다.
- 제목이 없거나 변경사항이 없으면 상단/하단 저장 모두 비활성화된다.

### 4.2 Back/Cancel 동작 통일

새 helper:

```ts
const requestBack = () => {
  if (hasUnsavedChanges) {
    setShowUnsavedDialog(true)
    return
  }

  router.back()
}
```

기존:

- 하단 Cancel 안에 unsaved check와 `router.back()`이 직접 들어 있었다.

변경 후:

- 상단 Back과 하단 Cancel이 같은 `requestBack()`을 쓴다.
- dirty 상태면 기존 unsaved dialog를 띄운다.
- clean 상태면 `router.back()` 한다.

의미:

- back/cancel semantics가 한 곳으로 모였다.
- inline mode에서는 상단 Back을 숨긴다. public inline shell의 `뒤로가기`/delete behavior는 유지된다.

### 4.3 편집 영역 가로 resize

변경:

```tsx
<div
  data-testid="blog-editor-workspace"
  className="min-w-0 max-w-full resize-x ... md:min-w-[42rem]"
>
```

의미:

- 브라우저 기본 `resize-x`를 사용해서 content editor 영역을 가로로 늘릴 수 있다.
- 테스트에서 `data-testid="blog-editor-workspace"`와 CSS `resize: horizontal`을 확인한다.

### 4.4 Excerpt 보존/업데이트 흐름 확인

기존에도 payload에는 `excerpt: excerpt.trim()`이 있었다.

이번 PR에서 추가로 보장한 것:

- 상단 quick-save로 update해도 excerpt가 PUT payload에 들어간다.
- inline detail edit에서도 excerpt가 PUT payload에 들어간다.
- inline update 후 public detail route replace/refresh 흐름이 유지된다.
- excerpt field는 계속 first-class editable field다.

테스트:

- `preserves excerpt when updating from the top quick-save action`
- `keeps inline update saves excerpt-aware while returning to the public detail route`
- `renders a horizontally resizable editor workspace while keeping the bottom submit action`

## 5. Browser API base를 same-origin `/api`로 변경

관련 파일:

- `src/lib/api/base.ts`
- `src/test/api-base.test.ts`
- `src/test/auth-csrf.test.ts`
- `src/test/auth-login-url.test.ts`
- `src/test/page-editor.test.tsx`

기존:

- 브라우저 origin이 `localhost:3000` 또는 `127.0.0.1:3000`이면 API base를 `http://localhost/api`로 바꿨다.

문제:

- Docker dev stack은 `http://127.0.0.1:3000`으로 접속한다.
- CSP가 `connect-src 'self' https:`라서 `http://localhost/api`는 same-origin이 아니다.
- 결과적으로 admin browser mutation 전에 session check가 CSP에 막혔다.
- Playwright에서 inline blog save가 클릭은 되었지만 POST/PUT이 발생하지 않는 원인이었다.

변경 후:

```ts
export function getApiBaseUrl() {
  if (process.env.NEXT_PUBLIC_API_BASE_URL) {
    return process.env.NEXT_PUBLIC_API_BASE_URL
  }

  if (typeof window !== 'undefined') {
    return '/api'
  }

  return '/api'
}
```

의미:

- 브라우저 기본 API 호출은 always same-origin `/api`다.
- Docker/nginx/Next rewrite/proxy 구조와 CSP가 맞아떨어진다.
- 절대 API URL이 필요한 배포는 기존처럼 `NEXT_PUBLIC_API_BASE_URL`로 override 가능하다.

리스크:

- 브라우저에서 “환경변수 없이 자동으로 `http://localhost/api`를 쓴다”는 과거 기대는 사라졌다.
- 하지만 dev/prod 모두 reverse proxy 아래의 `/api`가 더 안전한 기본값이다.

## 6. 테스트 변경 해석

### Unit/Vitest

추가/수정된 핵심 테스트:

- `src/test/public-admin-rendering.test.tsx`
  - public layout이 navbar 때문에 session을 fetch하지 않는지 확인.
  - shared admin gate가 admin/anonymous를 구분하는지 확인.
  - Contact page가 fallback direct email을 주입하지 않는지 확인.
  - Contact page inline editor가 admin에게만 보이는지 확인.

- `src/test/blog-editor.test.tsx`
  - top quick-save update payload에 excerpt가 포함되는지 확인.
  - inline update에서도 excerpt가 보존되는지 확인.
  - editor workspace가 resize-x인지 확인.
  - bottom Create/Update submit button이 계속 있는지 확인.

- `src/test/api-base.test.ts`, `src/test/auth-csrf.test.ts`, `src/test/auth-login-url.test.ts`, `src/test/page-editor.test.tsx`
  - browser default API base가 `/api`로 바뀐 기대값을 반영.

### Playwright

수정된 핵심 테스트:

- `tests/public-contact-fallback-email.spec.ts`
  - fallback email이 보이는 테스트에서 fallback email이 없어야 하는 테스트로 변경.

- `tests/ui-admin-blog-excerpt.spec.ts`
  - excerpt field 외에 top action save와 resizable workspace smoke check 추가.

- `tests/public-blog-detail-inline-edit.spec.ts`
  - `getByLabel('Title')`가 related card aria-label과 충돌할 수 있어 `input#title`로 좁힘.

- `tests/manual-qa-auth-gap.spec.ts`
  - 새 save-disabled 조건 때문에 dirty/enabled 상태를 먼저 확인하도록 안정화.

- `tests/ui-improvement-static-public-pages.spec.ts`
  - contact page가 mailto를 반드시 가져야 한다는 과거 기대 제거.

## 7. 검증 결과 해석

### 통과한 검증

- `npm run lint`
  - 통과.
  - 기존 warning 5개는 남아 있음.

- `npm run typecheck`
  - 통과.

- `npm test -- --run`
  - 47 files passed.
  - 247 tests passed.

- focused Playwright subset
  - 14 passed.
  - 1 existing skipped.

### full e2e with 4 workers

명령:

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible npx playwright test --workers=4
```

결과:

- 552 passed
- 10 failed
- 9 skipped

이후 실패한 line들을 serial로 재실행:

- 29 passed

해석:

- 4-worker full e2e 자체는 clean하지 않았다.
- 하지만 실패 line들이 1 worker serial rerun에서 전부 통과했다.
- 따라서 이번 PR의 명확한 제품 회귀라기보다는 병렬 실행 시 공유 데이터/세션/테스트 상태 충돌 문제로 보는 것이 맞다.

## 8. DOMMatrix 에러 해석

로그:

```text
ReferenceError: DOMMatrix is not defined
```

진단:

- `/resume` 경로에서 `ResumePdfViewer`가 `react-pdf`를 import한다.
- `react-pdf`는 내부적으로 `pdfjs-dist`를 사용한다.
- 현재 import 구조상 Next.js SSR bundle에서 `pdfjs-dist`가 평가된다.
- `pdfjs-dist`는 브라우저 API인 `DOMMatrix`를 기대하는데 Node SSR 환경에는 없다.

관련 파일:

- `src/app/(public)/resume/page.tsx`
- `src/components/content/ResumePdfViewer.tsx`

이번 PR과의 관계:

- 이번 diff가 직접 만든 에러는 아니다.
- full e2e에서 `/resume` 접근이 많아지며 로그가 눈에 띄게 반복됐다.
- 해결하려면 별도 PR에서 `ResumePdfViewer`의 `react-pdf` import를 SSR에서 평가되지 않도록 client-only dynamic import로 분리하는 것이 맞다.

## 9. 리뷰할 때 보면 좋은 포인트

### 꼭 봐야 할 핵심 파일

1. `src/lib/auth/public-admin.ts`
   - admin affordance source of truth.

2. `src/app/(public)/contact/page.tsx`
   - fallback email 제거가 정확히 요구사항과 맞는지.

3. `src/components/admin/BlogEditor.tsx`
   - top action bar가 과하지 않은지.
   - bottom submit 유지 여부.
   - save disabled 조건이 UX 요구에 맞는지.
   - resize-x wrapper가 충분한지.

4. `src/lib/api/base.ts`
   - browser API default를 `/api`로 바꾼 것이 배포 구조와 맞는지.

5. `src/test/blog-editor.test.tsx`
   - excerpt-preserving behavior를 제대로 잡는지.

### 의도적으로 안 봐도 되는 것

- backend auth endpoint 구현.
- mobile infinite scroll/feed redesign.
- related content deep refactor.
- autosave debounce redesign.
- rendered content snippet cards.

## 10. 후속 추천

1. `ResumePdfViewer` SSR 문제 수정
   - `react-pdf` import를 client-only dynamic boundary 뒤로 이동.
   - 목표: `/resume`에서 `DOMMatrix is not defined` 로그 제거.

2. 4-worker e2e isolation 개선
   - Notion autosave, generated baseline content, media thumbnail persistence 계열이 병렬 실행에서 서로 영향을 받는다.
   - 테스트 데이터 namespace 또는 worker별 seed isolation이 필요하다.

3. Contact content authoring 정책 문서화
   - 앞으로 contact direct email이 필요하면 authored content 안에 명시적으로 넣어야 한다.

## 11. 결론

이번 diff는 public/admin rendering consistency와 editor QoL 개선을 작은 범위로 묶은 변경이다. 가장 중요한 behavioral change는 “Contact 페이지가 더 이상 임의 fallback email UI를 주입하지 않는다”는 점이고, 가장 중요한 infrastructure-adjacent change는 “브라우저 API 기본값을 same-origin `/api`로 바꿔 CSP와 Docker dev stack에서 admin mutation이 정상 동작하게 했다”는 점이다.

현재 남은 큰 리스크는 제품 코드보다 테스트/런타임 주변부다. 즉, 4-worker full e2e isolation 문제와 resume PDF SSR `DOMMatrix` 문제는 별도 후속 PR로 다루는 것이 맞다.
