 Plan Mode라 frontend-architecture-test-0327.md 를 직접 저장할 수는 없습니다. 아래가 그대로 넣을 초안입니다.

  # Frontend Architecture And Test Review

  작성일: 2026-03-27

  ## 1. 요약

  현재 프론트엔드는 Next.js App Router 기반으로 public/admin 분리가 되어 있고, 테스트 자산도 적지 않다.

  - Vitest 테스트 파일: 25개
  - Playwright 스펙 파일: 41개
  - Playwright 인식 테스트 수: 67개
  - Playwright 프로젝트 분리:
    - `chromium-public`
    - `chromium-authenticated`
    - `chromium-runtime-auth`

  즉 “테스트가 없는 프로젝트”는 아니다.
  오히려 admin authoring, public 기본 UX, inline editing, auth 보안 흐름은 꽤 넓게 커버되어 있다.

  하지만 SEO 극대화를 목표로 한 Next.js 프로젝트 기준으로 보면, 현재 테스트와 아키텍처는 기능 회귀 방지 쪽에 많이 치우쳐 있고, SEO/정적화/인덱
  싱 품질을 보장하는 체계는 아직 부족하다.

  핵심 결론은 다음과 같다.

  - 기능 회귀 방지: 보통 이상
  - admin UX 회귀 방지: 강함
  - SEO 품질 보장: 약함
  - public 렌더링 전략 검증: 약함
  - 테스트 구조 일관성: 보통
  - 장기 유지보수 관점 완성도: 아직 개선 필요

  ## 2. 현재 아키텍처 상태

  ### 2.1 구조적으로 괜찮은 점
  - `src/app/(public)` 와 `src/app/admin` 이 분리되어 있다.
  - `src/lib/api/*` 로 fetch helper 가 정리되어 있다.
  - 공용 UI 컴포넌트와 admin 컴포넌트가 물리적으로 분리되어 있다.
  - App Router 기반 서버 컴포넌트 사용이 기본이다.

  ### 2.2 구조적으로 큰 문제
  현재 public SEO surface 와 admin authoring surface 가 강하게 섞여 있다.

  대표 예시:
  - public layout 이 세션을 읽는다.
  - public list/detail page 가 admin editor 를 직접 import 한다.
  - public 데이터 fetch helper 가 전부 `cache: 'no-store'` 다.
  - public 주요 페이지에 `dynamic = 'force-dynamic'` 이 붙어 있다.
  - pagination 이 viewport 기반 `pageSize` 쿼리로 indexable URL 을 늘린다.

  이 구조는 SEO 관점에서 불리하다.

  ### 2.3 대표 구조 문제
  - `src/app/(public)/layout.tsx`
    - `fetchServerSession()` 호출로 public shell 전체가 개인화 경로에 기울어짐
  - `src/app/(public)/page.tsx`
    - `force-dynamic`
  - `src/app/(public)/blog/page.tsx`
    - `force-dynamic`
    - `ResponsivePageSizeSync`
    - `PublicAdminLink`
    - `InlineAdminEditorShell`
    - `BlogEditor` 직접 포함
  - `src/app/(public)/works/page.tsx`
    - `force-dynamic`
    - `ResponsivePageSizeSync`
    - `InlineAdminEditorShell`
    - `WorkEditor` 직접 포함
  - `src/app/(public)/blog/[slug]/page.tsx`
    - admin editor 직접 import
    - `fetchAllPublicBlogs()` 로 전체 목록 로드
    - 직접 `JSON.parse(blog.contentJson)` 사용
  - `src/app/(public)/works/[slug]/page.tsx`
    - admin editor 직접 import
    - `fetchAllPublicWorks()` 로 전체 목록 로드
    - 직접 `JSON.parse(work.contentJson)` 사용

  ## 3. 현재 테스트 구조

  ## 3.1 Vitest
  역할:
  - API helper contract 테스트
  - server/browser helper 테스트
  - form/data parser 테스트
  - admin client component 테스트
  - page content parser 테스트
  - 일부 script helper 테스트

  강한 영역:
  - `src/lib/api/*`
  - `src/lib/api/server.ts`
  - `src/lib/api/auth.ts`
  - admin editor component 동작
  - 일부 UI primitive
  - responsive page size 로직

  약한 영역:
  - SEO metadata
  - App Router route-level behavior
  - 실제 렌더링 전략
  - structured data
  - robots/sitemap/canonical

  주의점:
  - `src/test/notion-*.test.ts` 는 실제 앱 코드가 아니라 `scripts/*.mjs` 를 검증한다.
  - 즉 frontend app 품질을 직접 보장하는 테스트와 스크립트 테스트가 섞여 있다.

  ## 3.2 Playwright
  역할:
  - 실제 사용자 흐름 검증
  - public page smoke
  - admin CRUD
  - auth browser flow
  - inline editor flow
  - upload/validation flow
  - layout stability

  강한 영역:
  - admin create/edit/publish/upload/delete
  - public page 진입과 seeded content 노출
  - auth + csrf browser behavior
  - inline editor 회귀
  - pagination/nav UI 노출
  - layout alignment

  약한 영역:
  - SEO 메타 검증
  - HTML source/indexability
  - crawler-friendly URL 정책
  - static/ISR 여부
  - sitemap/robots
  - canonical/alternates
  - JSON-LD
  - social preview metadata

  ## 4. 현재 테스트가 잘 잡는 것

  ### 4.1 Admin 기능
  Playwright 기준으로 아래는 강하다.

  - blog edit
  - blog publish
  - blog image upload/validation
  - work edit
  - work publish
  - work image upload/validation
  - pages/settings 수정
  - resume upload/validation
  - members page
  - admin menus
  - bulk delete
  - admin search/pagination
  - dashboard navigation
  - public inline editors

  이건 “관리 툴 회귀 방지” 측면에서 꽤 좋다.

  ### 4.2 Public 기본 UX
  - home smoke
  - introduction content
  - works/blog heading
  - detail page seeded content
  - edge navigation
  - pagination UI
  - layout stability
  - related content stability
  - mobile collapse

  즉 화면이 뜨고, seeded content 가 보이고, 관리자 인라인 편집이 유지되는지는 잘 잡는다.

  ### 4.3 Auth/보안
  - login CTA
  - local admin shortcut
  - csrf required flow
  - storage token 미보관
  - logout
  - public admin affordance visibility

  ## 5. 현재 테스트가 못 잡는 것

  ### 5.1 SEO 핵심 공백
  현재 가장 큰 공백이다.

  테스트 부재 항목:
  - `generateMetadata` 실제 반환값 검증
  - route별 title/description uniqueness
  - canonical URL
  - `openGraph`
  - `twitter`
  - `robots.ts`
  - `sitemap.ts`
  - structured data / JSON-LD
  - `lang` 설정 적절성
  - 404/Not Found indexing policy
  - pagination canonical policy
  - noindex 정책
  - social share preview metadata

  즉 “검색엔진 친화적인 프론트”를 보장하는 테스트가 거의 없다.

  ### 5.2 Next.js 정적화 전략 공백
  현재 public fetch helper 가 `no-store` 위주이고, public page 에 `force-dynamic` 이 많다.

  그런데 테스트는 다음을 전혀 보지 않는다.
  - 어떤 라우트가 static 인가
  - 어떤 라우트가 ISR 인가
  - 어떤 라우트가 dynamic 인가
  - `generateStaticParams` 존재 여부
  - `revalidate` 전략 일관성
  - build output 기준 route mode 변화

  즉 SEO에 가장 중요한 “렌더링 전략”이 테스트에서 빠져 있다.

  ### 5.3 HTML source 기준 검증 부재
  지금 Playwright는 브라우저 렌더 후 보이는 텍스트를 많이 본다.
  하지만 SEO는 실제 initial HTML source 도 중요하다.

  빠진 것:
  - 서버가 준 HTML 안에 핵심 콘텐츠가 있는가
  - metadata tag 가 들어갔는가
  - structured data script 가 있는가
  - canonical link 가 있는가
  - robots meta 가 있는가

  ### 5.4 아키텍처 냄새를 테스트가 못 잡음
  현재 lint가 잡는 문제:
  - effect 안 동기 `setState`
  - unused import

  하지만 테스트는 이런 구조 문제를 잡지 못한다.

  즉 “동작은 한다”와 “구조가 건전하다”가 분리되어 있다.

  ### 5.5 실제 외부 인증 흐름 공백
  현재 Playwright auth 는 `/api/auth/test-login` 기반이다.
  이건 안정성은 좋지만 다음은 검증하지 못한다.

  - 실제 OIDC redirect 성공
  - provider callback 실패 처리
  - secure cookie production behavior
  - cross-origin auth edge cases

  ## 6. 테스트 스위트 품질 평가

  ### 6.1 장점
  - 테스트 자산 수가 충분하다.
  - Playwright 프로젝트 분리가 명확하다.
  - global setup 이 storage state 를 잘 준비한다.
  - admin 회귀 검출 능력이 높다.
  - public layout/spacing regression 도 일부 잡는다.
  - API helper unit test 밀도가 꽤 좋다.

  ### 6.2 단점
  - SEO 검증이 거의 없다.
  - App code 테스트와 script test 가 섞여 있다.
  - public 쪽 테스트도 SEO보다 “보여지는 UI”에 치우쳐 있다.
  - viewport 기반 `pageSize` URL 정책을 오히려 테스트가 고정시킨다.
  - 일부 Playwright는 구현 디테일 결합도가 높다.
  - 전체 Vitest 실행은 이번 세션에서 완료 로그를 확인하지 못해 실행 안정성 점검이 추가로 필요하다.

  ## 7. SEO 중심 완성도 높이기 위한 우선순위

  ### P0: 아키텍처 정리
  가장 먼저 해야 할 것:
  - public layout 에서 `fetchServerSession()` 제거
  - public page 에서 admin editor 직접 import 제거
  - public list/detail 은 public-only server component 로 분리
  - admin affordance 는 lazy client island 또는 별도 admin-only 진입점으로 이동
  - `pageSize` 를 indexable URL 축에서 제거
  - public route 의 `force-dynamic` 남발 중단
  - fetch helper 기본값을 `no-store` 에서 route별 `revalidate` 전략으로 전환

  ### P1: SEO 기능 자체 추가
  - `robots.ts`
  - `sitemap.ts`
  - route-level metadata 정리
  - canonical
  - OG/Twitter metadata
  - JSON-LD
  - 404/noindex 정책
  - public detail page structured data

  ### P2: SEO 테스트 추가
  추가해야 할 테스트:
  - metadata snapshot 테스트
  - route별 title/description/canonical 테스트
  - robots/sitemap 응답 테스트
  - initial HTML source 에 핵심 텍스트 포함 여부 테스트
  - JSON-LD 스크립트 존재/shape 테스트
  - pagination canonical 정책 테스트
  - public route cache/revalidate 정책 테스트
  - `next build` 결과 기준 route mode 회귀 검사

  ### P3: 테스트 구조 정리
  - `src/test` 안의 app test 와 script helper test 분리
  - Playwright에서 purely visual/layout spec 과 core user-flow spec 분리
  - manual-auth 는 기본 목록에서 명시적으로 제외하거나 별도 프로젝트로 분리
  - admin 회귀 테스트의 중복 케이스 압축

  ## 8. 추천 테스트 전략

  ### 8.1 Vitest에서 해야 할 것
  - `generateMetadata()` 직접 테스트
  - metadata builder helper 도입 후 snapshot 테스트
  - JSON-LD helper 테스트
  - canonical URL builder 테스트
  - pagination URL normalization 테스트
  - content parser safe parse 테스트

  ### 8.2 Playwright에서 해야 할 것
  - `page.content()` 기준 metadata 확인
  - `locator('head meta[name="description"]')`
  - `link[rel="canonical"]`
  - `meta[property="og:title"]`
  - `meta[name="twitter:card"]`
  - `script[type="application/ld+json"]`
  - `/robots.txt`
  - `/sitemap.xml`
  - public detail page initial HTML 에 title/excerpt/body 일부 존재 확인
  - query noise 제거 정책 확인

  ### 8.3 CI에서 해야 할 것
  - `npm run lint`
  - `npm run typecheck`
  - `npm test -- --run`
  - `npx playwright test`
  - `npm run build`
  - build artifact 기준 route rendering mode 검사

  ## 9. 현재 상태 최종 판정

  현재 프론트엔드 테스트는 다음 질문에는 꽤 잘 답한다.

  - 관리자가 글/작업/페이지/이력서를 수정할 수 있는가?
  - 공개 페이지가 기본적으로 렌더되는가?
  - 인라인 에디터와 인증 흐름이 동작하는가?
  - 주요 UI 레이아웃이 크게 무너지지 않는가?

  하지만 다음 질문에는 거의 답하지 못한다.

  - 검색엔진이 이 페이지를 좋은 문서로 인식할 수 있는가?
  - 이 앱은 정적화/ISR/metadata 관점에서 SEO 친화적인가?
  - public 경로가 admin 개인화 때문에 불필요하게 dynamic 해지지 않는가?
  - canonical/sitemap/robots/structured data 가 완성되어 있는가?

  정리하면 현재 테스트는 “기능 회귀 방지”에는 꽤 유효하지만, “SEO 극대화를 위한 Next.js 프론트 검증 체계”로는 아직 완성도가 높지 않다.

  ## 10. 최종 한 줄 결론

  **지금 프론트엔드 테스트는 admin/product 기능은 잘 지키지만, SEO 중심 품질을 보장하는 테스트 체계는 아직 본격적으로 만들어지지 않았다.**