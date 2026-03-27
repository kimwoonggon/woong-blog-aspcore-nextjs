
  # Frontend Architecture Review Proposal (frontend_architecture-0327.md)

  ## Summary

  - 대상은 src/app, src/components, src/lib, src/hooks 전체이며, 현재 구조는 Next.js App Router 기반 public/admin 분리는 되어 있지만 public
    SEO 경계와 admin 편집 경계가 강하게 섞여 있다.
  - 가장 큰 결론은 “SEO를 극대화할 수 있는 Next.js 구조”라기보다 “admin 편의와 public 페이지가 서로를 침범하는 상태”라는 점이다.
  - 검증 결과 npm run typecheck 는 통과했고, npm run lint -- src 는 5 errors / 4 warnings 로 실패했다. 핵심 오류는 admin collection/table
    client 들의 effect 내부 동기 setState 패턴이다. src/components/admin/AdminBlogTableClient.tsx:47, src/components/admin/
    AdminWorksTableClient.tsx:46, src/components/admin/AdminDashboardCollections.tsx:69.
  - next build 검증 중 Next가 tsconfig.json 에 .next/dev/types/**/*.ts 를 자동 추가하는 tool-generated 변경을 만들었다. 이번 요청의 의도된 코
    드 수정은 아니다.

  ## Key Findings

  - CRITICAL SEO: public 레이아웃 자체가 세션을 읽는다. src/app/(public)/layout.tsx:12 의 fetchServerSession() 때문에 public 전체가 개인화 경
    로로 기울고, 공개 셸이 정적/ISR 기반으로 분리되지 못한다.
  - CRITICAL SEO: public 데이터 fetch 가 전부 cache: 'no-store' 다. src/lib/api/home.ts:46, src/lib/api/blogs.ts:52, src/lib/api/works.ts:43,
    src/lib/api/site-settings.ts:18. 여기에 src/app/(public)/page.tsx:8, src/app/(public)/blog/page.tsx:13, src/app/(public)/works/
    page.tsx:14, src/app/(public)/blog/[slug]/page.tsx:13 의 force-dynamic 이 겹쳐 SEO 친화적인 정적화 여지가 거의 없다.
  - HIGH SEO: metadata 가 루트와 detail 두 종류에만 있다. src/app/layout.tsx:20, src/app/(public)/blog/[slug]/page.tsx:19, src/app/(public)/
    works/[slug]/page.tsx:19. robots, sitemap, canonical, openGraph, twitter, structured data 경로가 없다.
  - HIGH SEO: src/app/layout.tsx:39 의 lang="en" 과 placeholder fallback (John Doe) 는 실제 다국어/한국어 운영과 맞지 않을 수 있고, 백엔드 실
    패 시 빈약한 메타가 인덱싱될 수 있다.
  - HIGH ARCH: public 페이지가 admin 편집기를 직접 import 한다. src/app/(public)/blog/page.tsx:3, src/app/(public)/works/page.tsx:4, src/app/
    (public)/blog/[slug]/page.tsx:3, src/app/(public)/works/[slug]/page.tsx:4. public 경로가 admin/editor/Tiptap surface 를 함께 끌고 있어 번
    들 분리와 책임 분리가 약하다.
  - HIGH SEO/UX: src/components/layout/ResponsivePageSizeSync.tsx:22 가 viewport 에 따라 pageSize 쿼리를 강제로 바꾼다. 같은 목록 콘텐츠가 ?
    pageSize=2, 4, 8, 12 등 여러 URL 로 퍼져 crawl budget 과 canonical 정책에 불리하다.
  - HIGH CLEANUP: mutation 경로가 이중화돼 있다. client editor 는 fetchWithCsrf 로 직접 백엔드를 치는데, 동시에 src/app/admin/blog/
    actions.ts:9, src/app/admin/works/actions.ts:14 server action 도 남아 있다. 현재 검색 기준 runtime 사용 흔적은 거의 없다.
  - HIGH CLEANUP: src/app/api/ai/fix-blog/route.ts:53, src/app/api/ai/enrich-work/route.ts:69 는 존재하지만 src 내부 호출 흔적이 없다. 백엔드
    의 /api/admin/ai/* 와 중복된 dead surface 후보다.
  - MEDIUM: 콘텐츠 파싱이 일관되지 않다. src/lib/content/page-content.ts:25 는 raw JSON.parse 를 던지고, detail page 는 직접
    JSON.parse(blog.contentJson) 를 쓴다. src/app/(public)/blog/[slug]/page.tsx:93, src/app/(public)/works/[slug]/page.tsx:97. malformed
    payload 에 취약하다.
  - MEDIUM: src/components/content/InteractiveRenderer.tsx:71 는 여러 갈래의 HTML 특수 처리와 dangerouslySetInnerHTML 를 동시에 담당한다. 렌
    더러/파서/특수 블록 해석이 한 파일에 몰려 있다.
  - LOW: 빈 src/lib/supabase/ 디렉터리, 미사용 UI 프리미티브 scroll-area, skeleton, 그리고 direct import 흔적이 희박한 dependency 들은 정리
    후보다. direct import 검색 기준 @tanstack/react-query, zustand, react-hook-form, react-pdf, recharts, react-draggable, uuid, use-
    debounce, html-react-parser 는 현재 src 사용 흔적이 없다.

  ## Implementation Changes

  - frontend_architecture-0327.md 는 다음 결론을 고정한다.
  - Public SEO surface 와 admin authoring surface 를 분리한다. 공개 페이지에서는 세션 fetch 와 inline editor import 를 제거하고, admin
    affordance 는 별도 admin shell 또는 lazy client island 로 이동한다.
  - Public fetch 레이어는 no-store 기본값을 버리고 페이지별로 revalidate, generateStaticParams, on-demand revalidation 을 적용한다. slug
  - pageSize 를 URL canonical 축에서 제거한다. viewport 기반 UI 변화는 client presentation 상태로만 처리하고, indexable URL 은 page 중심으로
  - Public page content parsing 은 safeParsePageContent 같은 단일 경로로 통합하고, raw JSON.parse 호출을 제거한다.
  - AI route 와 server action 은 하나의 경로만 남긴다. 권장 기본은 “백엔드 API를 단일 진실 공급원으로 두고, 프론트는 thin client/server fetch
    adapter 만 유지”다.
  - Dead surface cleanup 후보를 명시한다: unused Next AI routes, 미사용 server actions, 빈 src/lib/supabase, 미사용 UI primitives, direct
    import 없는 dependency 후보.
  - Admin client collection/table 컴포넌트는 effect 내부 동기 setState 제거 기준으로 정리한다.

  ## Test Plan

  - npm run typecheck 는 계속 통과해야 한다.
  - npm run lint -- src 는 현재 실패를 해소해 clean 상태로 만들어야 한다.
  - public route 별 metadata snapshot 테스트를 추가한다.
  - robots/sitemap/canonical 생성 결과를 테스트한다.
  - public list/detail page 가 기대한 revalidate/static 전략으로 동작하는지 빌드 기준으로 확인한다.
  - malformed contentJson 입력 시 public detail page 가 깨지지 않는 테스트를 추가한다.
  - public 경로에서 admin editor bundle 이 직접 import 되지 않는지 구조 검사를 추가한다.
  - responsive pagination 이 crawlable URL 을 추가 생성하지 않는지 검증한다.

  ## Assumptions

  - 문서는 한국어로 작성한다.
  - 산출물 경로는 /mnt/d/woong-blog/woong-blog/frontend_architecture-0327.md 이다.
  - 이번 단계는 분석/제안 문서만 대상으로 하고, 코드 수정은 포함하지 않는다.
  - 의도된 코드 변경은 없었고, 검증 과정에서 Next가 tsconfig.json 을 자동 수정한 것은 별도 리뷰 대상으로 남긴다