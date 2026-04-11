# UI/UX 개선 TODO — Public (Works / Blog / 공통) 편

> **목적**: 사용자가 보는 공개 페이지의 UI/UX 비판을 기반으로 한 실행 계획  
> **대상 브랜치**: `feat/ui-improvement`  
> **작성일**: 2026-04-11  
> **기술 스택**: Next.js 14+ App Router, Tailwind CSS v4, shadcn/ui, Playwright  
> **Playwright 설정**: `video: 'on'`, `screenshot: 'on'` (이미 설정됨 — `playwright.config.ts`)  
> **범위**: `src/app/(public)/`, `src/components/layout/`, `src/components/content/`, `src/app/globals.css`, `src/app/layout.tsx`

---

## 실행 규칙

1. 각 TODO 항목은 **반드시 Playwright 테스트를 작성**하고, 테스트 실행 시 **영상이 자동 녹화**된다 (`test-results/playwright/`).
2. 테스트 파일명: `tests/ui-pub-*.spec.ts` 네이밍 컨벤션.
3. 기존 테스트(`tests/dark-mode.spec.ts`, `tests/public-content.spec.ts`, `tests/public-layout-stability.spec.ts` 등)가 **깨지면 안 된다**.
4. 모든 변경은 **라이트 모드 + 다크 모드** 양쪽 확인.
5. 공개 페이지 테스트는 `chromium-public` 프로젝트 기준 (비인증 상태).
6. 변경 후 반드시 `npm run build` 성공 확인.
7. 각 Phase를 **순서대로** 진행한다. Phase 0 완료 전에 Phase 1을 시작하지 않는다.

---

## Phase 0: 접근성 CRITICAL 수정

> 다른 모든 작업보다 **먼저** 완료해야 한다. 접근성 위반은 법적·윤리적 문제이다.

---

### PUB-0-1: `prefers-reduced-motion` 미디어 쿼리 추가

- [ ] 완료

**변경 파일**: `src/app/globals.css`

**현재 문제**:
`animate-fade-in-up` 클래스가 Hero 섹션(`src/app/(public)/page.tsx`), Works 페이지 제목(`src/app/(public)/works/page.tsx`)에서 사용되는데, `prefers-reduced-motion`을 존중하지 않는다. WCAG 2.1 SC 2.3.3 위반.

**변경 내용**:
`globals.css`의 `.animate-fade-in-up` 정의 바로 뒤에 추가:
```css
@media (prefers-reduced-motion: reduce) {
  .animate-fade-in-up {
    animation: none;
    opacity: 1;
    transform: none;
  }
}
```

**테스트 계획** — `tests/ui-pub-reduced-motion.spec.ts`:
```
테스트 1: "reduced motion 설정에서 fade-in-up 애니메이션 비활성화"
  - page.emulateMedia({ reducedMotion: 'reduce' })
  - `/` 페이지 이동
  - Hero 헤드라인(h1) 요소의 computed opacity === '1' 확인
  - computed transform이 'none' 또는 'matrix(1, 0, 0, 1, 0, 0)' 확인
  - `/works` 페이지 이동
  - 페이지 제목(h1)의 computed opacity === '1' 확인

테스트 2: "기본 모션 설정에서 애니메이션 클래스 존재 확인"
  - page.emulateMedia({ reducedMotion: 'no-preference' })
  - `/` 이동, h1에 'animate-fade-in-up' 클래스가 있는지 확인
```

---

### PUB-0-2: Skip to Main Content 링크 추가

- [ ] 완료

**변경 파일**: `src/app/(public)/layout.tsx`

**현재 문제**:
Navbar에 6개 네비게이션 항목 + 테마 토글 + 세션 버튼이 있어 키보드 사용자가 main content에 도달하려면 10회 이상 Tab해야 한다. WCAG 2.4.1 위반.

**변경 내용**:
현재 구조:
```tsx
<div className="flex min-h-screen flex-col font-sans">
    <Navbar ownerName={ownerName} session={session} />
    <main className="flex-1">{children}</main>
```
변경 후:
```tsx
<div className="flex min-h-screen flex-col font-sans">
    <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-[100] focus:rounded-md focus:bg-background focus:px-4 focus:py-2 focus:text-sm focus:font-medium focus:text-foreground focus:shadow-lg focus:ring-2 focus:ring-ring"
    >
        Skip to main content
    </a>
    <Navbar ownerName={ownerName} session={session} />
    <main id="main-content" className="flex-1">{children}</main>
```

**테스트 계획** — `tests/ui-pub-skip-link.spec.ts`:
```
테스트 1: "Tab 키로 skip link 등장 → Enter로 main content 이동"
  - `/` 이동
  - page.keyboard.press('Tab')
  - 'Skip to main content' 텍스트 링크가 visible 확인
  - page.keyboard.press('Enter')
  - URL hash가 #main-content 확인

테스트 2: "마우스 사용 시 skip link 숨김"
  - `/` 이동
  - 'Skip to main content' 링크가 visible이 아닌지 확인
```

---

### PUB-0-3: Hero 프로필 이미지 alt 텍스트 개선

- [ ] 완료

**변경 파일**: `src/app/(public)/page.tsx`

**현재 문제**:
`<Image src={profileImageUrl} alt="Profile" .../>` — "Profile"은 비설명적. 스크린리더 사용자에게 무의미.

**변경 내용**:
`alt="Profile"` → `alt={headline}` 으로 변경. 이렇게 하면 "Hi, I am Woonggon Kim, Creative Technologist" 등 실제 의미 있는 설명이 전달된다.

fallback(프로필 이미지 없을 때)의 `<div>Avatar</div>` 텍스트도 `role="img" aria-label={headline}`로 보강.

**테스트 계획** — `tests/ui-pub-hero-alt.spec.ts`:
```
테스트 1: "프로필 이미지가 의미있는 alt 텍스트를 가짐"
  - `/` 이동
  - img[alt] 또는 [role="img"] 요소를 Hero 영역에서 찾기
  - alt 속성이 빈 문자열이 아니고, "Profile"과 같은 단어가 아닌지 확인
```

---

### PUB-0-4: `<html>`에 `color-scheme` 속성 반영

- [ ] 완료

**변경 파일**: `src/app/globals.css`

**현재 문제**:
다크 모드에서 스크롤바, `<select>`, `<input>` 등 네이티브 요소가 OS 기본(라이트) 스타일로 렌더링된다. Vercel Web Interface Guidelines "Dark Mode & Theming" 규칙 위반.

**변경 내용**:
`globals.css`의 `@layer base` 블록에 추가:
```css
@layer base {
  html {
    color-scheme: light;
  }
  html.dark {
    color-scheme: dark;
  }
  /* ...기존 코드 유지 */
}
```

**테스트 계획** — `tests/ui-pub-color-scheme.spec.ts`:
```
테스트 1: "라이트 모드에서 color-scheme이 light"
  - `/` 이동
  - html 요소 computed style의 color-scheme에 'light' 포함 확인

테스트 2: "다크 모드에서 color-scheme이 dark"
  - 테마 토글 클릭 또는 html에 class="dark" 강제 적용
  - html computed style의 color-scheme에 'dark' 포함 확인
```

---

### PUB-0-5: 다크 모드 보조 텍스트 명암비(Contrast Ratio) 개선

- [ ] 완료

**변경 파일**:
- `src/app/globals.css` — `--muted-foreground` 값 조정
- `src/app/(public)/page.tsx` — `dark:text-gray-500` → `dark:text-gray-400`
- `src/app/(public)/works/page.tsx` — `dark:text-gray-500` → `dark:text-gray-400`
- `src/app/(public)/works/[slug]/page.tsx` — `dark:text-gray-500` → `dark:text-gray-400`
- `src/app/(public)/blog/page.tsx` — 태그 영역 `text-gray-500 dark:text-gray-500` → `text-gray-500 dark:text-gray-400`

**현재 문제**:
- `.dark --muted-foreground: oklch(0.708 0 0)` → 배경 `oklch(0.10 ...)` 위에서 약 4.2:1. WCAG AA 4.5:1 미달 가능.
- `dark:text-gray-500` → Tailwind `#6b7280` → 명암비 약 3.2:1. **확실히 FAIL**.

**변경 내용**:
1. `globals.css` `.dark` 블록: `--muted-foreground: oklch(0.708 0 0)` → `--muted-foreground: oklch(0.75 0 0)`
2. 모든 `dark:text-gray-500` 인스턴스를 `dark:text-gray-400`으로 교체 (위 파일들).

**테스트 계획** — `tests/ui-pub-contrast.spec.ts`:
```
테스트 1: "다크 모드에서 muted-foreground의 명암비 ≥ 4.5"
  - 테마를 다크로 전환
  - `/` 이동
  - CSS custom property --muted-foreground와 --background의 실제 색상 추출 (기존 dark-mode.spec.ts의 getColorChannels 헬퍼 활용)
  - 두 색상의 relative luminance 계산 → contrast ratio ≥ 4.5 확인

테스트 2: "Works 카드의 보조 텍스트(카테고리/태그)가 다크 모드에서 명암비 충족"
  - `/works` 이동 (다크 모드)
  - 카드 내 보조 텍스트의 color 추출
  - 카드 배경과의 contrast ratio ≥ 4.5 확인
```

---

### PUB-0-6: `scroll-margin-top` 설정 (heading anchor)

- [ ] 완료

**변경 파일**: `src/app/globals.css`

**현재 문제**:
Navbar가 `sticky top-0 h-20`(80px)인데, heading anchor 클릭 시 heading이 navbar에 가려질 수 있다.

**변경 내용**:
`globals.css` `@layer base` 블록에 추가:
```css
h1, h2, h3, h4, h5, h6 {
  scroll-margin-top: 6rem; /* 96px — navbar 높이(80px) + 여유(16px) */
}
```

**테스트 계획** — `tests/ui-pub-scroll-margin.spec.ts`:
```
테스트 1: "heading 요소에 scroll-margin-top이 설정됨"
  - 블로그 상세 페이지 이동 (게시물이 있다고 가정)
  - h2 요소의 computed scroll-margin-top 값이 0이 아닌지 확인
```

---

## Phase 1: 메인 페이지 시선 유도 개선

> 이 Phase의 앞선 분석 리포트(메인 페이지 비판)에서 지적한 사항들을 실행한다.

---

### PUB-1-1: Hero 섹션에 듀얼 CTA 버튼 추가

- [ ] 완료

**변경 파일**: `src/app/(public)/page.tsx`

**현재 문제**:
Hero 섹션에 headline + introText 뒤 CTA가 전혀 없다. 방문자가 "다음에 뭘 해야 하지?" 결정을 스스로 내려야 한다. 이탈률 증가 요인.

**변경 내용**:
`src/app/(public)/page.tsx`에서 `<p>{introText}</p>` 아래(약 37행 뒤)에 추가:
```tsx
<div
  className="flex flex-wrap gap-4 opacity-0 animate-fade-in-up"
  style={{ animationDelay: '300ms' }}
>
  <Link
    href="/works"
    className="inline-flex items-center rounded-full bg-foreground px-6 py-3 text-sm font-semibold text-background transition-colors hover:bg-foreground/90"
  >
    View My Works
  </Link>
  <Link
    href="/blog"
    className="inline-flex items-center rounded-full border border-border px-6 py-3 text-sm font-semibold text-foreground transition-colors hover:bg-muted"
  >
    Read Blog
  </Link>
</div>
```

- Primary CTA: `View My Works` → `bg-foreground text-background` (Navbar 활성 탭과 동일 디자인 언어)
- Secondary CTA: `Read Blog` → ghost/outline 스타일
- `animationDelay: 300ms` → headline(100ms) → introText(200ms) → CTA(300ms) 자연스러운 시퀀스

**테스트 계획** — `tests/ui-pub-hero-cta.spec.ts`:
```
테스트 1: "'View My Works' CTA → /works 이동"
  - `/` 이동
  - 'View My Works' 링크 visible 확인 → 클릭 → URL /works 확인

테스트 2: "'Read Blog' CTA → /blog 이동"
  - `/` 이동
  - 'Read Blog' 링크 visible 확인 → 클릭 → URL /blog 확인

테스트 3: "모바일(375px)에서 CTA 터치타겟 ≥ 44px"
  - viewport 375×667
  - 두 CTA의 boundingBox height ≥ 44 확인

테스트 4: "reduced motion에서 CTA가 즉시 visible"
  - page.emulateMedia({ reducedMotion: 'reduce' })
  - CTA 래퍼 opacity === '1' 확인
```

---

### PUB-1-2: 메인 페이지 섹션 순서 변경 — Featured Works를 먼저

- [ ] 완료

**변경 파일**: `src/app/(public)/page.tsx`

**현재 문제**:
포트폴리오 사이트의 핵심 가치는 **작업물**인데, "Recent Posts"가 "Featured Works"보다 위에 있다. 채용 담당자는 블로그보다 실제 결과물을 먼저 본다.

**변경 내용**:
`src/app/(public)/page.tsx`에서 두 `<section>` 블록의 **순서를 swap**한다:
- 현재: Hero → **Recent Posts**(bg-brand-section-bg) → **Featured Works**
- 변경: Hero → **Featured Works**(bg-brand-section-bg로 이동) → **Recent Posts**(배경 제거)

구체적으로:
1. Featured Works `<section>` 블록을 Recent Posts 위로 올린다.
2. Featured Works 섹션에 `className="-mx-4 bg-brand-section-bg px-4 py-8 md:-mx-6 md:px-6"` 적용.
3. Recent Posts에서 `bg-brand-section-bg` 관련 클래스를 제거하고 단순 `<section>`으로 변경.

**테스트 계획** — `tests/ui-pub-section-order.spec.ts`:
```
테스트 1: "Featured Works 섹션이 Recent Posts보다 위에 위치"
  - `/` 이동
  - 'Featured works' 또는 'Featured Works' 헤딩의 boundingBox.y 값 기록
  - 'Recent posts' 헤딩의 boundingBox.y 값 기록
  - Featured works Y < Recent posts Y 확인
```

---

### PUB-1-3: Featured Works를 카드 그리드로 전환 + "View All" 링크 추가

- [ ] 완료

**변경 파일**: `src/app/(public)/page.tsx`

**현재 문제**:
수평 리스트(border-b 구분선) 형태로 시선의 좌→우 스캔 비용이 높다. 카드 그리드와 달리 각 항목의 시각적 무게가 동일하지 않고, "View all" 진입점도 없다.

**변경 내용**:
Featured Works 섹션의 `<div className="flex flex-col gap-6">...</div>` 전체를 카드 그리드로 교체:

```tsx
<section className="-mx-4 bg-brand-section-bg px-4 py-8 md:-mx-6 md:px-6">
  <div className="mb-6 flex items-center justify-between">
    <h2 className="text-xl font-bold text-gray-900 md:text-2xl dark:text-gray-50">
      Featured Works
    </h2>
    <Link href="/works" className="text-sm font-medium text-brand-cyan transition-colors hover:text-brand-cyan hover:underline">
      View all
    </Link>
  </div>
  <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-3">
    {featuredWorks.length > 0 ? (
      featuredWorks.map((work) => (
        <Link key={work.id} href={`/works/${work.slug}`} className="group block">
          <Card className="flex h-full flex-col overflow-hidden rounded-2xl border-border/80 bg-background shadow-sm transition hover:border-primary/30 hover:shadow-md">
            <div className="relative aspect-[4/3] overflow-hidden bg-gray-100 dark:bg-gray-800">
              {work.thumbnailUrl ? (
                <Image src={work.thumbnailUrl} alt={work.title} fill className="object-cover transition-transform duration-500 group-hover:scale-105" unoptimized />
              ) : (
                <div className="flex h-full w-full items-center justify-center text-sm font-medium text-gray-400">No Image</div>
              )}
            </div>
            <CardContent className="flex flex-1 flex-col p-4 sm:p-5">
              <div className="mb-2 flex items-center gap-2">
                <span className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs font-bold text-white">
                  {work.publishedAt ? new Date(work.publishedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short' }) : 'Unknown Date'}
                </span>
                <span className="text-xs font-medium uppercase tracking-wide text-gray-500 dark:text-gray-400">{work.category}</span>
              </div>
              <h3 className="line-clamp-2 text-lg font-heading font-bold leading-tight text-gray-900 transition-colors group-hover:text-brand-accent dark:text-gray-50">{work.title}</h3>
              <p className="mt-2 line-clamp-2 flex-1 text-sm leading-relaxed text-gray-600 dark:text-gray-300">{work.excerpt || 'Click to view details'}</p>
            </CardContent>
          </Card>
        </Link>
      ))
    ) : (
      <div className="col-span-full py-8 text-center text-gray-500">No featured works found.</div>
    )}
  </div>
</section>
```

**주의**: `Card`, `CardContent`는 이미 import되어 있다.

**테스트 계획** — `tests/ui-pub-featured-works-grid.spec.ts`:
```
테스트 1: "Featured Works가 그리드 레이아웃"
  - `/` 이동 → Featured Works 영역의 grid container가 존재하는지 확인

테스트 2: "카드 클릭 → /works/[slug]로 이동"
  - 첫 번째 Featured Works 카드 클릭 → URL이 /works/로 시작하는지 확인

테스트 3: "'View all' → /works 이동"
  - 'View all' 링크 클릭 → URL이 /works인지 확인

테스트 4: "모바일(375px) 1열, 태블릿(768px) 2열, 데스크탑(1280px) 3열"
  - 각 viewport에서 grid-template-columns computed 값으로 열 수 확인
```

---

### PUB-1-4: Recent Posts 섹션 개선 — 배경 제거 + 제목 weight 통일 + 카드 border 추가

- [ ] 완료

**변경 파일**: `src/app/(public)/page.tsx`

**현재 문제**:
1. Featured Works가 `bg-brand-section-bg`를 가져갈 것이므로 Recent Posts에서 제거 필요.
2. `font-medium`(Recent Posts) vs `font-bold`(Featured Works) — 같은 계층 제목인데 weight 불일관.
3. 카드가 `border-none` — 카드 구분 불분명.
4. 태그가 `border-l` 세퍼레이터로만 표시 — badge 스타일이 아님.

**변경 내용**:
1. Recent Posts `<section>`: `-mx-4 bg-brand-section-bg px-4 py-8 md:-mx-6 md:px-6` → 단순 `<section>`
2. 제목: `font-medium` → `font-bold`
3. `<Card className="border-none shadow-sm">` → `<Card className="overflow-hidden rounded-2xl border-border/80 bg-background shadow-sm transition hover:border-primary/30 hover:shadow-md">`
4. 태그 표시: `<span className="border-l border-gray-400 pl-4">` → `<span className="rounded-full bg-muted px-2.5 py-0.5 text-xs font-medium text-muted-foreground">`

**테스트 계획** — `tests/ui-pub-recent-posts.spec.ts`:
```
테스트 1: "Recent Posts 제목 font-weight === 700"
  - `/` 이동 → 'Recent posts' 헤딩 computed fontWeight === '700'

테스트 2: "Recent Posts 카드에 border 존재"
  - 첫 번째 카드의 border-width > 0 확인

테스트 3: "태그가 badge 스타일(rounded background)"
  - 카드 내 태그 요소의 borderRadius > 0 확인
```

---

### PUB-1-5: 헤드라인에 `text-wrap: balance` 추가

- [ ] 완료

**변경 파일**: `src/app/(public)/page.tsx`

**현재 문제**:
긴 헤드라인에서 마지막 줄에 단어 하나만 남는 "widow" 현상 발생 가능. Vercel Guidelines Typography 규칙 위반.

**변경 내용**:
Hero `<h1>` 클래스에 `[text-wrap:balance]` 추가.

**테스트 계획** — `tests/ui-pub-text-balance.spec.ts`:
```
테스트 1: "Hero h1에 text-wrap: balance"
  - `/` 이동 → h1 computed textWrap === 'balance' 확인
```

---

### PUB-1-6: 메인 페이지 `container`에 `max-w-7xl` 추가

- [ ] 완료

**변경 파일**: `src/app/(public)/page.tsx`

**현재 문제**:
메인 페이지만 `max-width` 제한 없음. 2560px 모니터에서 과도하게 펼쳐지고, Blog/Works에서 돌아왔을 때 콘텐츠 폭이 달라져 시각적 불연속.

**변경 내용**:
`<div className="container mx-auto flex flex-col gap-16 ...">` → `<div className="container mx-auto max-w-7xl flex flex-col gap-16 ...">`

**테스트 계획** — `tests/ui-pub-container-width.spec.ts`:
```
테스트 1: "메인 페이지 container maxWidth ≤ 1280px"
  - viewport 1920×1080
  - `/` container의 computed maxWidth가 1280px(또는 80rem) 이하 확인

테스트 2: "/blog와 동일 폭"
  - metadata: `/` container width === `/blog` container width
```

---

### PUB-1-7: Navbar에서 "Latest writing" 중복 링크 제거

- [ ] 완료

**변경 파일**: `src/components/layout/Navbar.tsx`

**현재 문제**:
네비게이션 메뉴의 "Blog" → `/blog`와 우측 "Latest writing" → `/blog`가 **완전 중복**. 키보드 Tab 수 불필요하게 증가.

**변경 내용**:
`Navbar.tsx`의 다음 코드 블록을 삭제:
```tsx
<Link
    href="/blog"
    className="hidden rounded-full border border-border/80 px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:border-primary/30 hover:text-foreground xl:inline-flex"
>
    Latest writing
</Link>
```

**테스트 계획** — `tests/ui-pub-navbar-dedup.spec.ts`:
```
테스트 1: "Navbar에 'Latest writing'이 없음"
  - `/` 이동 (viewport 1920×1080)
  - page.getByText('Latest writing') → toHaveCount(0)

테스트 2: "Blog 네비게이션 링크는 존재"
  - nav 영역 내 'Blog' 링크 visible + href /blog 확인
```

---

## Phase 2: Blog 페이지 개선

---

### PUB-2-1: Blog 카드에 시각적 앵커 추가 — 태그 뱃지 + 날짜 뱃지

- [ ] 완료

**변경 파일**: `src/app/(public)/blog/page.tsx`

**현재 문제**:
12개 카드가 모두 텍스트만으로 구성되어 시각적 앵커(Visual Anchor)가 전혀 없다. 눈이 어디를 봐야 할지 모른다. Works 카드(썸네일+뱃지)와 완전히 다른 디자인 언어.

**변경 내용**:
Blog 카드의 `<CardHeader>` 내부를 다음과 같이 개선:
1. 날짜를 `<Badge>`로 변경: `<Badge variant="secondary" className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs text-white">`
2. 태그를 `rounded-full bg-muted` badge로 변경 (현재 `text-gray-500 dark:text-gray-500` → WCAG 미달 해소)

변경 후 카드 구조 예시:
```tsx
<CardHeader className="px-4 pt-4 pb-0 sm:px-5 sm:pt-5">
  <div className="mb-2 flex flex-wrap items-center gap-2">
    <Badge variant="secondary" className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs text-white hover:bg-brand-navy/90">
      {formatPublishedDate(blog.publishedAt)}
    </Badge>
    {blog.tags?.slice(0, 2).map((tag) => (
      <span key={tag} className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
        {tag}
      </span>
    ))}
  </div>
  <CardTitle className="responsive-feed-title line-clamp-2 ...">
    {blog.title}
  </CardTitle>
</CardHeader>
```

`Badge` import 추가 필요: `import { Badge } from '@/components/ui/badge'`

**테스트 계획** — `tests/ui-pub-blog-card-anchors.spec.ts`:
```
테스트 1: "Blog 카드에 날짜 badge 존재"
  - `/blog` 이동
  - 첫 번째 blog-card 내 [data-slot="badge"] 또는 Badge 요소 존재 확인

테스트 2: "다크 모드에서 태그 텍스트 명암비 충족"
  - 다크 모드 전환
  - 태그 요소의 color와 배경(muted) 간 contrast ratio ≥ 4.5
```

---

### PUB-2-2: Blog 상세 날짜 배지 색상 통일 (`brand-accent` → `brand-navy`)

- [ ] 완료

**변경 파일**: `src/app/(public)/blog/[slug]/page.tsx`

**현재 문제**:
Blog detail 날짜: `bg-brand-accent`(빨간색), Works detail: `bg-brand-orange`(주황색), Works 카드: `bg-brand-navy`(네이비). 같은 역할(날짜 표시)인데 컬러가 제각각.

**변경 내용**:
```tsx
{/* 기존 */}
<Badge variant="secondary" className="rounded-full bg-brand-accent px-3 text-white hover:bg-brand-accent/90">

{/* 변경 */}
<Badge variant="secondary" className="rounded-full bg-brand-navy px-3 text-white hover:bg-brand-navy/90">
```

발췌문 `border-l-4`도 통일: `border-brand-accent` → `border-brand-navy`

**테스트 계획** — `tests/ui-pub-badge-color-unified.spec.ts`:
```
테스트 1: "Blog detail 날짜 배지가 dark navy 계열"
  - 블로그 상세 페이지 이동
  - 날짜 Badge의 background-color 추출 → navy 계열(blue hue, low lightness) 확인
```

---

### PUB-2-3: Blog 상세에 TOC(Table of Contents) 사이드바 추가

- [ ] 완료

**변경 파일**:
- `src/components/content/TableOfContents.tsx` — **신규 생성**
- `src/app/(public)/blog/[slug]/page.tsx` — TOC 배치

**현재 문제**:
긴 기술 블로그 글에서 목차 없이 스크롤만으로 내용을 파악해야 한다. 사용자가 원하는 섹션으로 바로 이동할 수 없다.

**변경 내용**:
1. `TableOfContents.tsx` 신규 생성:
   - Client component (`"use client"`)
   - 렌더된 HTML 콘텐츠에서 h2, h3 요소를 파싱하여 목차 생성
   - `IntersectionObserver`로 현재 읽고 있는 섹션 하이라이트
   - `sticky top-24` 배치 (navbar 80px + 여유)
   - 모바일에서는 숨김 (`hidden xl:block`)

2. `blog/[slug]/page.tsx`에서 `max-w-6xl` 컨테이너 활용:
   - 본문(`max-w-3xl`) 옆에 TOC를 `xl:` breakpoint에서 표시:
```tsx
<div className="mx-auto max-w-3xl xl:grid xl:max-w-none xl:grid-cols-[minmax(0,48rem)_200px] xl:gap-8">
  <div> {/* 기존 본문 */} </div>
  <aside className="hidden xl:block">
    <TableOfContents />
  </aside>
</div>
```

**테스트 계획** — `tests/ui-pub-blog-toc.spec.ts`:
```
테스트 1: "데스크탑에서 TOC 사이드바 visible"
  - viewport 1440×900
  - 블로그 상세 이동
  - aside 내 TOC 요소가 visible 확인

테스트 2: "모바일에서 TOC 숨김"
  - viewport 375×667
  - TOC 요소가 visible이 아닌지 확인

테스트 3: "TOC 링크 클릭 → 해당 heading으로 스크롤"
  - TOC 내 첫 번째 링크 클릭
  - 해당 heading이 viewport 내에 있는지 확인
```

---

### PUB-2-4: Blog 상세 하단에 이전글/다음글 네비게이션 추가

- [ ] 완료

**변경 파일**: `src/app/(public)/blog/[slug]/page.tsx`

**현재 문제**:
글을 다 읽은 사용자에게 "다음 행동"을 제안하지 않는다. Related Content List가 있지만 "이전글/다음글" 패턴이 없어서 시리즈 글이나 시간순 탐색이 불편.

**변경 내용**:
`RelatedContentList` 바로 위에 이전/다음 글 네비게이션 블록 추가:
```tsx
{(() => {
  const allBlogs = relatedBlogs // 이미 현재 글을 제외한 목록
  // publishedAt 기준 정렬하여 이전/다음 결정 로직
  // 간단한 prev/next 카드 2개 (flex justify-between)
})()}
```

실제 구현은 `relatedBlogs` 배열을 publishedAt 기준으로 정렬하여 현재 글의 앞뒤를 찾는 방식. 또는 API에서 prev/next를 별도로 제공하는 게 이상적이지만, 현재 구조에서는 클라이언트 사이드에서 처리.

**테스트 계획** — `tests/ui-pub-blog-prev-next.spec.ts`:
```
테스트 1: "블로그 상세에 이전/다음 네비게이션 존재"
  - 블로그 글이 2개 이상 있을 때
  - 상세 페이지 이동
  - '이전' 또는 '다음' 관련 링크가 하나 이상 존재 확인
```

---

## Phase 3: Works 페이지 개선

---

### PUB-3-1: Works 페이지 제목에서 불필요한 fade-in 애니메이션 제거

- [ ] 완료

**변경 파일**: `src/app/(public)/works/page.tsx`

**현재 문제**:
`<h1 className="... opacity-0 animate-fade-in-up" style={{ animationDelay: '100ms' }}>Works</h1>` — "Works" 한 단어에 fade-in은 과잉 연출이고 LCP를 해친다. Hero 이외의 페이지 제목에 입장 애니메이션은 불필요.

**변경 내용**:
`opacity-0 animate-fade-in-up` 제거, `style={{ animationDelay: '100ms' }}` 제거:
```tsx
<h1 className="text-3xl font-heading font-bold md:text-4xl text-gray-900 dark:text-gray-50">Works</h1>
```

**테스트 계획** — `tests/ui-pub-works-no-fade.spec.ts`:
```
테스트 1: "Works 제목이 즉시 visible (opacity: 1, 애니메이션 없음)"
  - `/works` 이동
  - h1 computed opacity === '1' 확인 (pageload 직후)
  - h1에 'animate-fade-in-up' 클래스가 없는지 확인
```

---

### PUB-3-2: Works 카드 고정 높이 → 유연 높이로 변경

- [ ] 완료

**변경 파일**: `src/app/globals.css`

**현재 문제**:
```css
.works-feed-card { height: 30rem; }
```
콘텐츠 길이가 다른 카드에서 하단 빈 공간(underfill) 또는 overflow(잘림) 발생.

**변경 내용**:
```css
/* 기존 */
.works-feed-card { height: 30rem; }
/* 변경 */
.works-feed-card { min-height: 24rem; }

/* max-height: 860px */
/* 기존: height: 27rem; → 변경: min-height: 22rem; */

/* max-height: 720px */
/* 기존: height: 24rem; → 변경: min-height: 20rem; */
```

**테스트 계획** — `tests/ui-pub-works-card-height.spec.ts`:
```
테스트 1: "Works 카드가 정확히 30rem(480px)이 아닌 유연 높이"
  - `/works` 이동
  - 첫 번째 .works-feed-card의 height가 정확히 480px이 아닌지 확인
  - height ≥ 384px (24rem) 확인
```

---

### PUB-3-3: Works "No Image" 폴백 개선

- [ ] 완료

**변경 파일**: `src/app/(public)/works/page.tsx`

**현재 문제**:
이미지 없을 때 `<div className="flex h-full w-full items-center justify-center text-sm font-medium text-gray-400">No Image</div>` — 순수 텍스트만으로 placeholder 역할 불충분.

**변경 내용**:
폴백을 브랜드 그라데이션 + 아이콘으로 교체:
```tsx
<div className="flex h-full w-full flex-col items-center justify-center gap-2 bg-gradient-to-br from-gray-100 to-gray-200 dark:from-gray-800 dark:to-gray-900">
  <Briefcase className="h-8 w-8 text-gray-400" />
  <span className="text-xs font-medium text-gray-400">No Image</span>
</div>
```
(`Briefcase` 아이콘은 `lucide-react`에서 import. Works 페이지에서 이미 `lucide-react`가 쓸 수 있으나, 현재 직접 import는 없을 수 있으므로 추가 필요.)

**테스트 계획** — `tests/ui-pub-works-no-image.spec.ts`:
```
테스트 1: "No Image 폴백에 그라데이션 배경과 아이콘 존재"
  - `/works?__qaEmpty=1` (QA 빈 상태 가능하면) 또는 이미지 없는 카드를
    확인할 수 있는 경우
  - 해당 영역에 gradient background와 svg 아이콘이 존재하는지 확인
```

---

### PUB-3-4: Works 상세 날짜 배지 색상 통일 (`brand-orange` → `brand-navy`)

- [ ] 완료

**변경 파일**: `src/app/(public)/works/[slug]/page.tsx`

**현재 문제**:
`bg-brand-orange`(주황색) 사용. Blog과 역할이 같은 날짜 배지인데 색이 다르다.

**변경 내용**:
```tsx
{/* 기존 */}
<Badge ... className="rounded-full bg-brand-orange px-3 text-white hover:bg-brand-orange/90">
{/* 변경 */}
<Badge ... className="rounded-full bg-brand-navy px-3 text-white hover:bg-brand-navy/90">
```

발췌문 `border-l-4`도: `border-brand-orange` → `border-brand-navy`

태그 hover도: `hover:text-brand-orange` → `hover:text-brand-accent` (사이트 전역 hover 색상과 통일)

**테스트 계획** — 위 PUB-2-2의 `tests/ui-pub-badge-color-unified.spec.ts`에 테스트 추가:
```
테스트 2: "Works detail 날짜 배지가 Blog detail과 동일 색상"
  - Works 상세 이동 → 날짜 Badge background-color 추출
  - Blog 상세 이동 → 날짜 Badge background-color 추출
  - 두 색상이 동일한지 확인
```

---

### PUB-3-5: Works 상세에서 비디오 배치 개선

- [ ] 완료

**변경 파일**: `src/app/(public)/works/[slug]/page.tsx`

**현재 문제**:
인라인 임베드가 아닌 비디오가 본문 위에 무조건 배치(`!hasInlineVideoEmbeds`)되어, 비디오가 여러 개면 스크롤 없이 본문 도달 불가.

**변경 내용**:
비디오가 2개 이상이면 첫 번째만 본문 위에 표시하고, 나머지는 본문 아래 "More Videos" 섹션으로 분리:
```tsx
{orderedVideos.length > 0 && !hasInlineVideoEmbeds && (
  <div className="mb-8 space-y-4">
    <WorkVideoPlayer key={orderedVideos[0].id} video={orderedVideos[0]} />
    {orderedVideos.length > 1 && (
      <details className="rounded-2xl border border-border/80 p-4">
        <summary className="cursor-pointer text-sm font-medium text-muted-foreground">
          More videos ({orderedVideos.length - 1})
        </summary>
        <div className="mt-4 space-y-4">
          {orderedVideos.slice(1).map((video) => (
            <WorkVideoPlayer key={video.id} video={video} />
          ))}
        </div>
      </details>
    )}
  </div>
)}
```

**테스트 계획** — `tests/ui-pub-works-video-layout.spec.ts`:
```
테스트 1: "비디오가 있을 때 첫 번째 비디오가 본문 위에 표시"
  - 비디오가 있는 Works 상세 이동
  - video 또는 iframe 요소가 article 내에 존재 확인
```

---

## Phase 4: 공통 정교화

---

### PUB-4-1: height 기반 미디어 쿼리를 width 기반으로 보완

- [ ] 완료

**변경 파일**: `src/app/globals.css`

**현재 문제**:
`@media (max-height: 860px)`, `@media (max-height: 720px)` — 뷰포트 높이 기반 반응형은 비표준적이고, 가로 모드 태블릿에서 엉뚱한 결과를 낸다.

**변경 내용**:
기존 `max-height` 미디어 쿼리를 유지하되, `max-width` 폴백을 추가:
```css
@media (max-height: 860px), (max-width: 640px) {
  /* ... */
}
@media (max-height: 720px), (max-width: 480px) {
  /* ... */
}
```
이렇게 하면 좁은 화면(모바일)에서도 height와 무관하게 축소 스타일이 적용된다.

**테스트 계획** — `tests/ui-pub-responsive-fallback.spec.ts`:
```
테스트 1: "좁은 viewport(480px width, 1024px height)에서 카드 축소 스타일 적용"
  - viewport 480×1024
  - `.responsive-feed-title` computed fontSize ≤ 18px (1.125rem) 확인
```

---

### PUB-4-2: Hero 애니메이션 LCP 개선

- [ ] 완료

**변경 파일**: `src/app/globals.css`

**현재 문제**:
Hero 요소들이 `opacity-0 animate-fade-in-up`으로 시작 → CSS 애니메이션 끝날 때까지 콘텐츠 안 보임 → LCP(Largest Contentful Paint) 0.8초 지연.

**변경 내용**:
JS 비활성 환경 fallback 추가:
```css
@media (scripting: none) {
  .animate-fade-in-up {
    opacity: 1 !important;
    transform: none !important;
    animation: none;
  }
}
```
(PUB-0-1의 `prefers-reduced-motion` 규칙과 함께 적용)

**테스트 계획** — `tests/ui-pub-hero-lcp.spec.ts`:
```
테스트 1: "Hero 헤드라인이 1초 이내에 visible"
  - `/` 이동, 1초 대기
  - h1 visible + opacity > 0 확인
```

---

## Phase 5: Related Content & Pagination 개선

---

### PUB-5-1: Related Content 섹션의 폭 불연속 해소

- [ ] 완료

**변경 파일**: `src/app/(public)/blog/[slug]/page.tsx`, `src/app/(public)/works/[slug]/page.tsx`

**현재 문제**:
본문은 `max-w-3xl`인데 `RelatedContentList`는 `max-w-6xl` 컨테이너에 있어 폭이 갑자기 넓어진다.

**변경 내용**:
`RelatedContentList`를 `max-w-3xl` 영역 안으로 이동하거나, 부드러운 전환(full-bleed 섹션)으로 처리:
```tsx
{/* RelatedContentList 앞에 구분선 + 설명 텍스트 추가 */}
<div className="mx-auto max-w-3xl mt-16 border-t pt-12">
  <RelatedContentList ... />
</div>
```

**테스트 계획** — `tests/ui-pub-related-content-width.spec.ts`:
```
테스트 1: "Related content 섹션 폭이 본문 폭과 유사"
  - 블로그 상세 이동
  - 본문 영역 width와 related content 영역 width 차이가 100px 이내 확인
```

---

## 변경 파일 총 요약

| Phase | 파일 | 변경 유형 |
|-------|------|-----------|
| P0 | `src/app/globals.css` | 수정 (reduced-motion, color-scheme, muted-foreground, scroll-margin-top) |
| P0 | `src/app/(public)/layout.tsx` | 수정 (skip link + main id) |
| P0 | `src/app/(public)/page.tsx` | 수정 (alt text, dark:text-gray-500) |
| P0 | `src/app/(public)/works/page.tsx` | 수정 (dark:text-gray-500) |
| P0 | `src/app/(public)/works/[slug]/page.tsx` | 수정 (dark:text-gray-500) |
| P0 | `src/app/(public)/blog/page.tsx` | 수정 (dark:text-gray-500) |
| P1 | `src/app/(public)/page.tsx` | 대규모 수정 (CTA, 섹션 순서, Works그리드, container, text-balance) |
| P1 | `src/components/layout/Navbar.tsx` | 수정 (Latest writing 삭제) |
| P2 | `src/app/(public)/blog/page.tsx` | 수정 (카드 시각 앵커) |
| P2 | `src/app/(public)/blog/[slug]/page.tsx` | 수정 (배지 색상, TOC 배치, prev/next) |
| P2 | `src/components/content/TableOfContents.tsx` | **신규** |
| P3 | `src/app/(public)/works/page.tsx` | 수정 (애니메이션 제거, No Image 폴백) |
| P3 | `src/app/(public)/works/[slug]/page.tsx` | 수정 (배지 색상, 비디오 배치) |
| P4 | `src/app/globals.css` | 수정 (height→width 폴백, LCP, 카드 높이) |
| P5 | `src/app/(public)/blog/[slug]/page.tsx` | 수정 (related content 폭) |
| P5 | `src/app/(public)/works/[slug]/page.tsx` | 수정 (related content 폭) |
| 테스트 | `tests/ui-pub-*.spec.ts` | **신규 16개** |

**총: 소스 11개 수정 + 1개 신규, 테스트 16개 신규**

---

## 회귀 테스트

모든 Phase 완료 후:
```bash
# 기존 테스트 회귀 확인
npx playwright test tests/dark-mode.spec.ts tests/public-content.spec.ts tests/public-layout-stability.spec.ts tests/public-detail-pages.spec.ts tests/public-works-pagination.spec.ts tests/public-blog-pagination.spec.ts tests/public-edge-nav.spec.ts --project=chromium-public

# 신규 테스트 전체 실행
npx playwright test tests/ui-pub-*.spec.ts --project=chromium-public
```

영상: `test-results/playwright/` 자동 저장 (playwright.config.ts `video: 'on'`).
