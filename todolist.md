# Public UI Improvement Todo

기준 문서:
- `ui-improvement-todolist-public-260411.md`
- `ui-improvement-todolist-admin-260411.md`
- `todolist-admin.md`

운영 원칙:
- `ui-ux-pro-max`
- Vercel `web-design-guidelines`
- `tdd`

테스트 규칙:
- `tests/ui-pub-*.spec.ts` 네이밍 유지
- 각 작업은 `https://localhost` + headed Playwright로 확인
- 기존 public/admin 회귀 테스트를 깨지지 않게 유지

## Current Sync
- [x] public 개선 관련 워킹트리 변경이 이미 존재함
- [x] 본 파일은 기존 완료 항목을 보존하면서 실제 구현 상태 기준으로 계속 동기화한다
- [x] static public shell 테스트 기대치와 구현을 다시 맞춘다
  - 대상: `tests/ui-improvement-static-public-pages.spec.ts`
  - 반영: `static-public-shell`, `resume-shell`

## Active Multi-Agent Queue
- [ ] `Q-1` lane A: token migration / home-works-static cleanup
- [ ] `Q-2` lane B: card system / footer / hover hierarchy
- [ ] `Q-3` lane C: detail pages / related content / prev-next
- [ ] `Q-4` lane D: creative audit beyond checklist
- [ ] `Q-5` verifier lane: single Playwright queue for public

## Public A+ Queue (2026-04-12)
- [x] `PAT-1` Raw color -> semantic token 전환
  - 기준: `design-pattern-public-0412.md`, `design-improvement-execution-0412.md` Task 1
- [x] `PAT-2` Hover 색상 체계 통일
  - 기준: execution Task 2
- [x] `PAT-3` Blog 카드 accent stripe + grid 3열 + `!important` 제거
  - 기준: execution Task 3
- [x] `PAT-4` Featured Works no-image 폴백 통일
  - 기준: execution Task 4
- [x] `PAT-5` Works detail prev/next 네비게이션 추가
  - 기준: execution Task 5
- [x] `PAT-6` Footer 네비게이션 확장
  - 기준: execution Task 6
- [x] `PAT-7` 태그 스타일 통일
  - 기준: execution Task 7
- [x] `PAT-8` 언어 혼용 정리
  - 기준: execution Task 8
- [x] `PAT-9` Works 카드 animationDelay dead code 제거
  - 기준: execution Task 9
- [x] `PAT-10` `Click to view details` 카피 제거
  - 기준: execution Task 10
- [x] `PAT-11` Avatar 폴백 아이콘 전환
  - 기준: execution Task 11
- [x] `PAT-12` 카드 제목 타이포 스케일 통일
  - 기준: execution Task 13
- [x] `PAT-V1` public 검증용 `ui-pub-*`/회귀 테스트 갱신
- [x] `PAT-V2` `NEXT_DIST_DIR=.next-build npm run build`
- [x] `PAT-V3` `tests/ui-pub-*.spec.ts --project=chromium-public`
- [x] `PAT-T-1` footer nav 검증 스펙 추가
  - `tests/ui-improvement-footer-nav.spec.ts`
- [x] `PAT-T-2` blog stripe/grid 검증 스펙 추가
  - `tests/ui-improvement-blog-stripe-grid.spec.ts`
- [x] `PAT-T-3` works prev/next 검증 스펙 추가
  - `tests/ui-improvement-work-prev-next.spec.ts`
- [x] `PAT-T-4` home fallback copy 제거 검증 스펙 추가
  - `tests/ui-improvement-home-copy-cleanup.spec.ts`

## Verification Snapshot
- [x] `npx eslint src tests next.config.ts playwright.config.ts`
- [x] `npm run typecheck`
- [x] `NEXT_DIST_DIR=.next-build npm run build`
- [x] `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=https://localhost PLAYWRIGHT_HEADED=1 npx playwright test tests/ui-pub-*.spec.ts --project=chromium-public --headed --workers=1`
- [x] `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=https://localhost npx playwright test tests/ui-pub-*.spec.ts tests/public-content.spec.ts tests/public-detail-pages.spec.ts tests/public-edge-nav.spec.ts tests/public-layout-stability.spec.ts tests/public-blog-pagination.spec.ts tests/public-works-pagination.spec.ts tests/public-work-videos.spec.ts tests/home.spec.ts tests/introduction.spec.ts tests/public-blog-detail-inline-edit.spec.ts tests/public-work-detail-inline-edit.spec.ts --workers=1`
  - 결과: `90 passed`

## Extra Ops
- [x] `ADD-0-1` Playwright/Next 런타임 충돌 해소
  - 변경: `next.config.ts`, `playwright.config.ts`
  - 내용: `NEXT_DIST_DIR`를 도입해 root 소유 `.next/dev/*`와 분리된 `.next-playwright`, `.next-build` 경로로 검증
- [x] `ADD-0-2` public 개선 마스터 체크리스트 작성
  - 결과: 이 파일
- [x] `ADD-0-2A` admin master 체크리스트 연동
  - 결과: `todolist-admin.md`
- [x] `ADD-0-3` `ui-pub-*` 엔트리 포인트 정리
  - 결과: `tests/ui-pub-*.spec.ts` 추가, 기존 `ui-improvement-*` 스펙 재사용 가능 상태로 정리
- [x] `ADD-4-3` public focus-visible 정교화
  - 변경: `src/app/globals.css`, `src/components/layout/Navbar.tsx`
  - 테스트: `tests/ui-pub-focus-visible.spec.ts`
- [x] `ADD-4-4` mobile overflow 방지 회귀 추가
  - 테스트: `tests/ui-pub-overflow.spec.ts`
- [x] `ADD-4-5` static public shell test hook 및 resume shell 규약 정리
  - 대상: `src/app/(public)/introduction/page.tsx`, `src/app/(public)/contact/page.tsx`, `src/app/(public)/resume/page.tsx`
  - 테스트: `tests/ui-improvement-static-public-pages.spec.ts`
- [x] `ADD-4-6` contact 페이지 직접 메일 CTA 안정화
  - 대상: `src/app/(public)/contact/page.tsx`
  - 테스트: `tests/ui-improvement-static-public-pages.spec.ts`
- [x] `ADD-4-7` featured works 반응형 breakpoint 정렬
  - 대상: `src/app/(public)/page.tsx`
  - 테스트: `tests/ui-improvement-featured-works-grid.spec.ts`
- [x] `ADD-4-8` blog related shell 폭 정렬 클래스 보강
  - 대상: `src/app/(public)/blog/[slug]/page.tsx`
  - 테스트: `tests/ui-improvement-related-content-width.spec.ts`
- [x] `ADD-4-9` `.next-build` 기반 type include 안정화
  - 대상: `tsconfig.json`
  - 배경: `NEXT_DIST_DIR=.next-build npm run build` 시 Next가 자동 제안한 include 반영

## In Progress Now
- [x] `PUB-X-1` static public shell 회귀 복구
  - 결과: `2 passed`
  - 테스트: `tests/ui-improvement-static-public-pages.spec.ts`
- [x] `PUB-X-2` public checklist와 실제 워킹트리 재동기화
  - 결과: 현재 파일 + `todolist-admin.md`

## Phase 0 — 접근성 Critical
- [x] `PUB-0-1` `prefers-reduced-motion` 지원
  - 파일: `src/app/globals.css`
  - 테스트: `tests/ui-pub-reduced-motion.spec.ts`
- [x] `PUB-0-2` Skip to Main Content 링크
  - 파일: `src/app/(public)/layout.tsx`, `src/components/layout/SkipToMainLink.tsx`
  - 테스트: `tests/ui-pub-skip-link.spec.ts`
- [x] `PUB-0-3` Hero 프로필 이미지 alt/fallback 개선
  - 파일: `src/app/(public)/page.tsx`
  - 내용: Hero 이미지 `alt={headline}`, fallback `role="img" aria-label={headline}`
  - 테스트: `tests/ui-pub-hero-alt.spec.ts`
- [x] `PUB-0-4` `<html>` `color-scheme` 반영
  - 파일: `src/app/globals.css`
  - 테스트: `tests/ui-pub-color-scheme.spec.ts`
- [x] `PUB-0-5` 다크 모드 보조 텍스트 대비 보정
  - 파일: `src/app/globals.css`, `src/app/(public)/page.tsx`, `src/app/(public)/works/page.tsx`, `src/app/(public)/works/[slug]/page.tsx`, `src/app/(public)/blog/page.tsx`
  - 테스트: `tests/ui-pub-contrast.spec.ts`
- [x] `PUB-0-6` heading anchor `scroll-margin-top`
  - 파일: `src/app/globals.css`
  - 테스트: `tests/ui-pub-scroll-margin.spec.ts`

## Phase 1 — 메인 페이지 시선 유도
- [x] `PUB-1-1` Hero 듀얼 CTA
  - 파일: `src/app/(public)/page.tsx`
  - 테스트: `tests/ui-pub-hero-cta.spec.ts`
- [x] `PUB-1-2` 메인 섹션 순서 `Featured Works` → `Recent Posts`
  - 파일: `src/app/(public)/page.tsx`
  - 테스트: `tests/ui-pub-section-order.spec.ts`
- [x] `PUB-1-3` Featured Works 카드 그리드 + `View all`
  - 파일: `src/app/(public)/page.tsx`
  - 테스트: `tests/ui-pub-featured-works-grid.spec.ts`
- [x] `PUB-1-4` Recent Posts 배경 제거 + heading weight 통일 + 카드 border/태그 pill
  - 파일: `src/app/(public)/page.tsx`
  - 테스트: `tests/ui-pub-recent-posts.spec.ts`
- [x] `PUB-1-5` Hero `text-wrap: balance`
  - 파일: `src/app/(public)/page.tsx`
  - 테스트: `tests/ui-pub-text-balance.spec.ts`
- [x] `PUB-1-6` 홈 container `max-w-7xl`
  - 파일: `src/app/(public)/page.tsx`
  - 테스트: `tests/ui-pub-container-width.spec.ts`
- [x] `PUB-1-7` Navbar `/blog` 중복 링크 제거
  - 파일: `src/components/layout/Navbar.tsx`
  - 테스트: `tests/ui-pub-navbar-dedup.spec.ts`

## Phase 2 — Blog 개선
- [x] `PUB-2-1` Blog 카드 시각적 앵커 추가
  - 파일: `src/app/(public)/blog/page.tsx`
  - 내용: 날짜 badge, 태그 pill, localhost 전용 `__qaTagged=1` deterministic path 추가
  - 테스트: `tests/ui-pub-blog-card-anchors.spec.ts`
- [x] `PUB-2-2` Blog 상세 날짜/발췌 accent를 `brand-navy`로 통일
  - 파일: `src/app/(public)/blog/[slug]/page.tsx`
  - 테스트: `tests/ui-pub-badge-color-unified.spec.ts`
- [x] `PUB-2-3` Blog 상세 TOC 사이드바 추가
  - 파일: `src/app/(public)/blog/[slug]/page.tsx`, `src/components/content/TableOfContents.tsx`
  - 참고: seeded blog content도 heading 구조로 보강
  - 테스트: `tests/ui-pub-blog-toc.spec.ts`
- [x] `PUB-2-4` Blog 상세 이전글/다음글 네비게이션
  - 파일: `src/app/(public)/blog/[slug]/page.tsx`
  - 테스트: `tests/ui-pub-blog-prev-next.spec.ts`

## Phase 3 — Works 개선
- [x] `PUB-3-1` Works 제목 fade 제거
  - 파일: `src/app/(public)/works/page.tsx`
  - 테스트: `tests/ui-pub-works-no-fade.spec.ts`
- [x] `PUB-3-2` Works 카드 고정 높이 제거
  - 파일: `src/app/globals.css`
  - 테스트: `tests/ui-pub-works-card-height.spec.ts`
- [x] `PUB-3-3` Works `No Image` 폴백 개선
  - 파일: `src/app/(public)/works/page.tsx`
  - 내용: gradient fallback + deterministic `__qaNoImage=1`
  - 테스트: `tests/ui-pub-works-no-image.spec.ts`
- [x] `PUB-3-4` Works 상세 날짜/발췌/태그 hover 색 통일
  - 파일: `src/app/(public)/works/[slug]/page.tsx`
  - 테스트: `tests/ui-pub-badge-color-unified.spec.ts`
- [x] `PUB-3-5` Works 상세 비디오 배치 개선
  - 파일: `src/app/(public)/works/[slug]/page.tsx`
  - 내용: 첫 비디오 우선 + 나머지 `More videos`
  - 테스트: `tests/ui-pub-works-video-layout.spec.ts`

## Phase 4 — 공통 정교화
- [x] `PUB-4-1` height 기반 반응형에 width fallback 추가
  - 파일: `src/app/globals.css`
  - 테스트: `tests/ui-pub-responsive-fallback.spec.ts`
- [x] `PUB-4-2` Hero animation LCP fallback
  - 파일: `src/app/globals.css`
  - 테스트: `tests/ui-pub-hero-lcp.spec.ts`

## Phase 5 — Related Content / Pagination
- [x] `PUB-5-1` Related Content 폭 정렬
  - 파일: `src/app/(public)/blog/[slug]/page.tsx`, `src/app/(public)/works/[slug]/page.tsx`
  - 테스트: `tests/ui-pub-related-content-width.spec.ts`

## Seed / Test Data Support
- [x] seeded blog 본문 heading 구조 보강
  - 파일: `backend/src/WoongBlog.Api/Infrastructure/Persistence/Seeding/SeedData.cs`
  - 목적: TOC / scroll-margin 검증 가능화
- [x] seeded work 멀티 비디오 보강
  - 파일: `backend/src/WoongBlog.Api/Infrastructure/Persistence/Seeding/SeedData.cs`
  - 목적: lead video / folded videos 검증 가능화

## Changed Areas
- [x] UI/CSS
  - `src/app/globals.css`
  - `src/app/(public)/page.tsx`
  - `src/app/(public)/blog/page.tsx`
  - `src/app/(public)/blog/[slug]/page.tsx`
  - `src/app/(public)/works/page.tsx`
  - `src/app/(public)/works/[slug]/page.tsx`
  - `src/components/content/TableOfContents.tsx`
  - `src/components/layout/Navbar.tsx`
- [x] Test/runtime
  - `next.config.ts`
  - `playwright.config.ts`
  - `tests/ui-pub-*.spec.ts`
  - `tests/ui-improvement-*.spec.ts` 일부 안정화
- [x] Seed data
  - `backend/src/WoongBlog.Api/Infrastructure/Persistence/Seeding/SeedData.cs`

## Remaining Risks
- [ ] 기존 `tests/ui-improvement-*.spec.ts` 원본 파일은 보존되어 있다. 현재는 `ui-pub-*` 엔트리에서 재사용 중이므로, 추후 테스트 정리 시 중복 정리 여부를 따로 결정해야 한다.
- [ ] `.next-build/` 산출물은 이번 검증에서 다시 생성됐다. 커밋 전 정리/ignore 상태는 별도 운영 판단이 필요하다.
