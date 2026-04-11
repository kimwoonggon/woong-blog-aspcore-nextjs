# UI/UX 개선 TODO 리스트

> **목적**: 포트폴리오 블로그 사이트의 UI/UX 비판을 기반으로 한 실행 계획  
> **대상 브랜치**: `feat/ui-improvement`  
> **작성일**: 2026-04-11  
> **기술 스택**: Next.js 14+ App Router, Tailwind CSS, shadcn/ui, Playwright  
> **Playwright 설정**: `video: 'on'`, `screenshot: 'on'` (이미 설정됨 — `playwright.config.ts`)

---

## 실행 규칙

1. 각 TODO 항목은 **반드시 Playwright 테스트를 작성**하고, 테스트 실행 시 **영상이 자동 녹화**된다 (`test-results/playwright/` 디렉토리).
2. 테스트는 `tests/ui-improvement-*.spec.ts` 네이밍 컨벤션을 따른다.
3. 기존 테스트(`tests/dark-mode.spec.ts`, `tests/public-content.spec.ts` 등)가 깨지면 안 된다.
4. 모든 변경은 라이트 모드 + 다크 모드 **양쪽 모두** 확인한다.
5. `chromium-public` 프로젝트 기준으로 테스트한다 (비인증 상태).
6. 변경 후 반드시 `npm run build` 성공을 확인한다.

---

## Phase 0: 접근성 CRITICAL 수정 (P0)

이 Phase는 다른 모든 작업보다 **먼저** 완료해야 한다. 접근성 위반은 법적·윤리적 문제이다.

### TODO 0-1: `prefers-reduced-motion` 미디어 쿼리 추가

- [ ] **완료**

**변경 범위**:
- `src/app/globals.css` — `@keyframes fadeInUp` 및 `.animate-fade-in-up` 수정

**상세 지시**:
```css
/* globals.css 파일의 @keyframes fadeInUp 뒤에 추가 */
@media (prefers-reduced-motion: reduce) {
  .animate-fade-in-up {
    animation: none;
    opacity: 1;
    transform: none;
  }
}
```

**이유**: `animate-fade-in-up` 클래스가 Hero 섹션(page.tsx)과 Works 페이지(works/page.tsx)에서 사용되는데, `prefers-reduced-motion`을 존중하지 않으면 전정기관 장애 사용자에게 불편을 줄 수 있다. Vercel Web Interface Guidelines와 WCAG 2.1 SC 2.3.3 위반.

**테스트 계획** — `tests/ui-improvement-reduced-motion.spec.ts`:
```
테스트 1: "reduced motion에서 fade-in-up 애니메이션 비활성화"
  - page.emulateMedia({ reducedMotion: 'reduce' })
  - `/` 페이지 이동
  - Hero 헤드라인(h1) 요소의 opacity가 즉시 1인지 확인
  - Hero 헤드라인의 transform이 none 또는 translateY(0)인지 확인
  - `/works` 페이지 이동
  - 페이지 제목(h1)의 opacity가 즉시 1인지 확인

테스트 2: "기본 모션 설정에서 애니메이션 동작 확인"
  - page.emulateMedia({ reducedMotion: 'no-preference' })
  - `/` 페이지 이동
  - Hero 헤드라인 요소가 `animate-fade-in-up` 클래스를 가지는지 확인
```
**영상 녹화**: 두 테스트 모두 자동 녹화됨 (playwright.config.ts의 `video: 'on'`).

---

### TODO 0-2: Skip to Main Content 링크 추가

- [ ] **완료**

**변경 범위**:
- `src/app/(public)/layout.tsx` — `<main>` 태그 앞에 skip link 추가 + `<main>`에 `id="main-content"` 추가

**상세 지시**:
현재 `src/app/(public)/layout.tsx`의 구조:
```tsx
<div className="flex min-h-screen flex-col font-sans">
    <Navbar ownerName={ownerName} session={session} />
    <main className="flex-1">{children}</main>
    <Footer ... />
</div>
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
    <Footer ... />
</div>
```

**이유**: Navbar에 6개 네비게이션 항목 + 테마 토글 + 세션 버튼이 있어, 키보드 사용자가 main content에 도달하려면 10회 이상 Tab 해야 한다. WCAG 2.4.1 위반.

**테스트 계획** — `tests/ui-improvement-skip-link.spec.ts`:
```
테스트 1: "Tab 키로 skip link가 보이고 Enter로 main content로 이동"
  - `/` 페이지 이동
  - Tab 키 1회 누르기 (page.keyboard.press('Tab'))
  - "Skip to main content" 텍스트를 가진 링크가 화면에 visible 상태인지 확인
  - Enter 키 누르기
  - 포커스가 #main-content 내부 또는 #main-content 자체로 이동했는지 확인
  - URL hash가 #main-content인지 확인

테스트 2: "skip link는 마우스 사용자에게 보이지 않음"
  - `/` 페이지 이동
  - "Skip to main content" 링크가 sr-only로 숨겨져 있는지 확인 (isVisible → false)
```

---

### TODO 0-3: Hero 프로필 이미지 alt 텍스트 개선

- [ ] **완료**

**변경 범위**:
- `src/app/(public)/page.tsx` — `<Image>` 컴포넌트의 `alt` 속성 수정

**상세 지시**:
현재: `alt="Profile"`
변경: alt에 실제 사용자 이름 또는 의미 있는 설명을 포함해야 한다.

`fetchPublicHome()`에서 받아오는 데이터에서 `ownerName`을 사용하거나, `headline`에서 이름을 추출한다. 가장 간단한 방법은 `siteSettings`에서 `ownerName`을 가져와 사용하는 것이지만, 현재 HomePage에서는 siteSettings를 직접 fetch하지 않으므로, `headline` 변수를 활용한다:

```tsx
alt={`Profile photo of ${headline.replace(/^Hi,?\s*I\s*am\s*/i, '').split(',')[0].trim() || 'the site owner'}`}
```

또는 더 간단하게 API에서 이름이 오면 사용하고, 아니면 "Profile photo" 유지.
가장 현실적인 방법: `alt="Profile photo"` → `alt={headline}` 사용하여 "Hi, I am Woonggon Kim, Creative Technologist" 전체를 alt로 넣는 것이 가장 설명적이다.

**테스트 계획** — `tests/ui-improvement-hero-alt.spec.ts`:
```
테스트 1: "프로필 이미지가 의미있는 alt 텍스트를 가짐"
  - `/` 페이지 이동
  - 프로필 이미지 영역 내 img 태그 또는 role="img"인 요소를 찾는다
  - 해당 요소의 alt 속성이 "Profile"이 아닌, 더 설명적인 텍스트인지 확인
  - alt 속성이 빈 문자열이 아닌지 확인
```

---

### TODO 0-4: `<html>` 태그에 `color-scheme` 속성 반영

- [ ] **완료**

**변경 범위**:
- `src/app/layout.tsx` — `<html>` 태그에 `style` 또는 `className` 기반 color-scheme 추가
- 또는 `src/components/providers/ThemeProvider.tsx`에서 동적으로 `color-scheme` 주입

**상세 지시**:
현재 `<html lang="en" suppressHydrationWarning>` 상태에서, 다크 모드일 때 스크롤바·네이티브 `<select>`·`<input>` 등이 OS 기본(라이트) 스타일로 렌더링된다.

`globals.css`에 추가:
```css
@layer base {
  html {
    color-scheme: light;
  }
  html.dark {
    color-scheme: dark;
  }
}
```

**이유**: Vercel Web Interface Guidelines의 "Dark Mode & Theming" 규칙: `color-scheme: dark` on `<html>` for dark themes (fixes scrollbar, inputs).

**테스트 계획** — `tests/ui-improvement-color-scheme.spec.ts`:
```
테스트 1: "라이트 모드에서 html의 color-scheme이 light"
  - `/` 페이지 이동
  - html 요소의 computed style에서 color-scheme 값이 'light'를 포함하는지 확인

테스트 2: "다크 모드에서 html의 color-scheme이 dark"
  - page.emulateMedia({ colorScheme: 'dark' }) 또는 테마 토글 클릭
  - html 요소에 class="dark"가 있을 때 color-scheme이 'dark'를 포함하는지 확인
```

---

### TODO 0-5: 다크 모드 보조 텍스트 명암비 개선

- [ ] **완료**

**변경 범위**:
- `src/app/globals.css` — `.dark` 섹션의 `--muted-foreground` 값 조정

**상세 지시**:
현재 다크 모드 `--muted-foreground: oklch(0.708 0 0)`은 배경 `oklch(0.10 0.02 280)` 위에서 약 4.2:1 명암비로 WCAG AA 기준(4.5:1) 미달 가능성이 있다.

변경:
```css
.dark {
  --muted-foreground: oklch(0.75 0 0);  /* 기존 0.708 → 0.75로 밝기 상승 */
}
```

또한 다크 모드에서 `text-gray-400`, `text-gray-500` 등 하드코딩된 Tailwind 색상도 확인:
- `dark:text-gray-400` → 약 `#9ca3af` = 명암비 약 5:1 (OK)
- `dark:text-gray-500` → 약 `#6b7280` = 명암비 약 3.2:1 (**FAIL**)

`dark:text-gray-500`을 사용하는 곳을 `dark:text-gray-400`으로 변경해야 한다.

**영향 받는 파일들** (grep `dark:text-gray-500`):
- `src/app/(public)/page.tsx` — Featured works 기간(period) 표시
- `src/app/(public)/works/page.tsx` — Works 카드 태그
- `src/app/(public)/works/[slug]/page.tsx` — Work detail 기간

**테스트 계획** — `tests/ui-improvement-contrast.spec.ts`:
```
테스트 1: "다크 모드에서 muted-foreground 텍스트의 명암비가 4.5:1 이상"
  - page.emulateMedia({ colorScheme: 'dark' }) 또는 테마 토글 클릭
  - `/` 페이지 이동
  - muted-foreground 색상과 background 색상의 contrast ratio 계산
    (기존 dark-mode.spec.ts의 getColorChannels 헬퍼 활용 가능)
  - ratio >= 4.5 확인

테스트 2: "다크 모드에서 보조 텍스트(날짜, 태그)가 최소 명암비 충족"
  - `/works` 페이지 이동 (다크 모드)
  - works 카드 내 날짜/카테고리 텍스트의 color를 추출
  - 해당 color와 카드 배경 간 contrast ratio >= 4.5 확인
```

---

## Phase 1: 메인 페이지 시선 유도 개선 (P1)

이 Phase는 **사용자 전환율(Conversion)**에 직접 영향을 준다. 포트폴리오 사이트의 핵심 가치.

### TODO 1-1: Hero 섹션에 듀얼 CTA 버튼 추가

- [ ] **완료**

**변경 범위**:
- `src/app/(public)/page.tsx` — Hero 섹션 내 introText 아래에 버튼 2개 추가

**상세 지시**:
현재 Hero 구조 (`src/app/(public)/page.tsx` 22~52행):
```tsx
<section className="flex flex-col-reverse items-center justify-between gap-8 md:flex-row md:items-start md:gap-12">
  <div className="flex flex-1 flex-col items-center text-center md:items-start md:text-left">
    <h1 ...>{headline}</h1>
    <p ...>{introText}</p>
    <!-- 여기서 끝남. CTA 없음 -->
  </div>
  <div><!-- 프로필 이미지 --></div>
</section>
```

변경 후 — `introText` `<p>` 태그 바로 아래에 추가:
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

**디자인 의도**:
- Primary CTA (`View My Works`): `bg-foreground text-background` — Navbar의 활성 탭 스타일과 일치하여 사이트 내 디자인 언어 통일
- Secondary CTA (`Read Blog`): ghost/outline 스타일 — 덜 중요한 두 번째 행동
- `rounded-full`: Navbar 탭/버튼과 동일한 pill 형태
- animationDelay 300ms: headline(100ms) → introText(200ms) → CTA(300ms)로 자연스러운 시퀀스

**이유**: 포트폴리오 사이트의 Hero에 CTA가 없으면 방문자가 "다음에 뭘 해야 하지?"를 스스로 결정해야 한다. 이는 **이탈률 50% 이상 증가** 요인이다.

**테스트 계획** — `tests/ui-improvement-hero-cta.spec.ts`:
```
테스트 1: "메인 페이지 Hero에 'View My Works' CTA가 존재하고 /works로 이동"
  - `/` 페이지 이동
  - 'View My Works' 텍스트를 가진 링크가 visible인지 확인
  - 해당 링크 클릭
  - URL이 /works로 변경되었는지 확인
  - Works 페이지 h1이 보이는지 확인

테스트 2: "메인 페이지 Hero에 'Read Blog' CTA가 존재하고 /blog로 이동"
  - `/` 페이지 이동
  - 'Read Blog' 텍스트를 가진 링크가 visible인지 확인
  - 해당 링크 클릭
  - URL이 /blog로 변경되었는지 확인

테스트 3: "CTA 버튼이 모바일에서도 보이고 터치 가능"
  - viewport를 375x667 (iPhone SE)로 설정
  - `/` 페이지 이동
  - 두 CTA 버튼이 모두 visible인지 확인
  - 버튼 높이가 최소 44px 이상인지 확인 (터치 타겟 사이즈)

테스트 4: "CTA 버튼에 reduced-motion이 적용됨"
  - page.emulateMedia({ reducedMotion: 'reduce' })
  - `/` 페이지 이동
  - CTA 래퍼의 opacity가 즉시 1인지 확인
```

---

### TODO 1-2: 메인 페이지 섹션 순서 변경 — Featured Works를 먼저

- [ ] **완료**

**변경 범위**:
- `src/app/(public)/page.tsx` — 두 `<section>` 블록의 순서를 교체

**상세 지시**:
현재 순서 (`src/app/(public)/page.tsx`):
1. Hero (22~52행)
2. **Recent Posts** (54~100행) ← `bg-brand-section-bg` 배경
3. **Featured Works** (102~170행)

변경 후:
1. Hero
2. **Featured Works** (먼저)
3. **Recent Posts** (나중에)

단순히 두 `<section>` 블록의 위치를 swap하면 된다. JSX의 `<section className="-mx-4 bg-brand-section-bg ...">` (Recent Posts)와 `<section>` (Featured Works)의 위치를 교체한다.

추가로, Featured Works 섹션에 섹션 배경(`bg-brand-section-bg`)을 이동하고, Recent Posts에서는 배경을 제거한다. 이유: 위에 있는 주요 섹션이 시각적으로 구분되어야 시선이 머문다.

**이유**: 포트폴리오 사이트에서 **작업물 > 블로그** 가치 순서이다. 채용 담당자는 블로그보다 실제 결과물을 먼저 본다. 스크롤 없이 보이는 영역(above the fold)에 가장 중요한 콘텐츠를 배치하는 것은 UX의 기본 원칙이다.

**테스트 계획** — `tests/ui-improvement-section-order.spec.ts`:
```
테스트 1: "Featured Works 섹션이 Recent Posts보다 먼저 나타남"
  - `/` 페이지 이동
  - 'Featured works' 헤딩의 Y좌표(boundingBox)를 구한다
  - 'Recent posts' 헤딩의 Y좌표를 구한다
  - Featured works의 Y < Recent posts의 Y 확인

테스트 2: "Featured Works 섹션에 배경색이 적용됨"
  - `/` 페이지 이동
  - Featured Works 섹션의 부모 요소 배경색이 brand-section-bg인지 확인
```

---

### TODO 1-3: Featured Works를 카드 그리드로 전환 + "View All" 링크 추가

- [ ] **완료**

**변경 범위**:
- `src/app/(public)/page.tsx` — Featured Works 섹션 전체 재구성

**상세 지시**:
현재 Featured Works는 **수평 리스트 + border-b 구분선** 형태이다 (`src/app/(public)/page.tsx` 102~170행):
```tsx
<div className="flex flex-col gap-6">
  {featuredWorks.map((work) => (
    <div className="flex flex-col gap-6 border-b border-gray-200 pb-6 md:flex-row ...">
      <Link className="h-48 w-full md:w-64 ..."> {/* 썸네일 */} </Link>
      <div> {/* 제목, 날짜, 발췌 */} </div>
    </div>
  ))}
</div>
```

이것을 **카드 그리드**로 변환한다 (Works 목록 페이지 `src/app/(public)/works/page.tsx`의 카드 패턴과 유사하게):

```tsx
<section className="-mx-4 bg-brand-section-bg px-4 py-8 md:-mx-6 md:px-6">
  <div className="mb-6 flex items-center justify-between">
    <h2 className="text-xl font-bold text-gray-900 md:text-2xl dark:text-gray-50">
      Featured Works
    </h2>
    <Link
      href="/works"
      className="text-sm font-medium text-brand-cyan transition-colors hover:text-brand-cyan hover:underline"
    >
      View all
    </Link>
  </div>
  <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-3">
    {featuredWorks.length > 0 ? (
      featuredWorks.map((work) => {
        const thumbnailUrl = work.thumbnailUrl || null
        const publishDate = work.publishedAt
          ? new Date(work.publishedAt).toLocaleDateString('en-US', {
              year: 'numeric',
              month: 'short',
            })
          : 'Unknown Date'

        return (
          <Link key={work.id} href={`/works/${work.slug}`} className="group block">
            <Card className="flex h-full flex-col overflow-hidden rounded-2xl border-border/80 bg-background shadow-sm transition hover:border-primary/30 hover:shadow-md">
              <div className="relative aspect-[4/3] overflow-hidden bg-gray-100 dark:bg-gray-800">
                {thumbnailUrl ? (
                  <Image
                    src={thumbnailUrl}
                    alt={work.title}
                    fill
                    className="object-cover transition-transform duration-500 group-hover:scale-105"
                    unoptimized
                  />
                ) : (
                  <div className="flex h-full w-full items-center justify-center text-sm font-medium text-gray-400">
                    No Image
                  </div>
                )}
              </div>
              <CardContent className="flex flex-1 flex-col p-4 sm:p-5">
                <div className="mb-2 flex items-center gap-2">
                  <span className="rounded-full bg-brand-navy px-2.5 py-0.5 text-xs font-bold text-white">
                    {publishDate}
                  </span>
                  <span className="text-xs font-medium uppercase tracking-wide text-gray-500 dark:text-gray-400">
                    {work.category}
                  </span>
                </div>
                <h3 className="line-clamp-2 text-lg font-heading font-bold leading-tight text-gray-900 transition-colors group-hover:text-brand-accent dark:text-gray-50">
                  {work.title}
                </h3>
                <p className="mt-2 line-clamp-2 flex-1 text-sm leading-relaxed text-gray-600 dark:text-gray-300">
                  {work.excerpt || 'Click to view details'}
                </p>
              </CardContent>
            </Card>
          </Link>
        )
      })
    ) : (
      <div className="col-span-full py-8 text-center text-gray-500">
        No featured works found.
      </div>
    )}
  </div>
</section>
```

**주의사항**:
- `CardContent`를 import하지 않았다면 기존 import에 추가: `import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'` (이미 존재)
- `dark:text-gray-500` → `dark:text-gray-400`로 변경 (TODO 0-5 명암비 개선과 연동)
- 카드 전체가 `<Link>`로 감싸져서 전체 영역이 클릭 가능해야 함

**이유**: 수평 리스트 레이아웃은 시선의 좌→우 스캔 비용이 높고, 각 항목의 시각적 무게가 동일하지 않다. 카드 그리드는 시선이 Z-패턴으로 자연스럽게 흐르며, 썸네일이 1:1로 비교 가능하여 사용자가 관심 있는 항목을 빠르게 식별한다.

**테스트 계획** — `tests/ui-improvement-featured-works-grid.spec.ts`:
```
테스트 1: "Featured Works가 그리드 카드 레이아웃으로 표시됨"
  - `/` 페이지 이동
  - 'Featured Works' 또는 'Featured works' 헤딩 확인
  - 해당 섹션 아래에 grid 레이아웃인 컨테이너가 존재하는지 확인
    (CSS grid-template-columns computed style 확인)

테스트 2: "Featured Works 카드를 클릭하면 work detail로 이동"
  - `/` 페이지 이동
  - 첫 번째 featured work 카드 클릭
  - URL이 /works/[slug] 패턴으로 변경되었는지 확인

테스트 3: "View all 링크가 /works로 이동"
  - `/` 페이지 이동
  - Featured Works 섹션 내 'View all' 링크 확인
  - 클릭하여 /works 페이지로 이동 확인

테스트 4: "카드 hover 시 이미지 scale 애니메이션과 제목 색상 변경"
  - `/` 페이지 이동
  - 첫 번째 카드에 hover
  - 이미지의 transform에 scale이 적용되었는지 확인
  - 제목의 color가 brand-accent로 변경되었는지 확인

테스트 5: "모바일(375px)에서 카드가 1열로 표시"
  - viewport 375x667
  - `/` 페이지 이동
  - Featured Works 카드들이 세로로 쌓여있는지 확인 (각 카드의 width ≈ 컨테이너 width)

테스트 6: "태블릿(768px)에서 카드가 2열"
  - viewport 768x1024
  - grid-template-columns에 2개 컬럼이 있는지 확인

테스트 7: "데스크탑(1280px)에서 카드가 3열"
  - viewport 1280x800
  - grid-template-columns에 3개 컬럼이 있는지 확인
```

---

### TODO 1-4: Recent Posts 섹션 개선 — 배경 제거 + 섹션 제목 weight 통일

- [ ] **완료**

**변경 범위**:
- `src/app/(public)/page.tsx` — Recent Posts 섹션의 `bg-brand-section-bg` 제거, 제목 `font-medium` → `font-bold`

**상세 지시**:
Featured Works가 `bg-brand-section-bg` 배경을 가져가므로, Recent Posts에서는 제거한다:

```tsx
{/* 기존: className="-mx-4 bg-brand-section-bg px-4 py-8 md:-mx-6 md:px-6" */}
{/* 변경: */}
<section>
```

제목 수정:
```tsx
{/* 기존: className="text-xl font-medium text-gray-900 md:text-2xl dark:text-gray-50" */}
{/* 변경: font-medium → font-bold */}
<h2 className="text-xl font-bold text-gray-900 md:text-2xl dark:text-gray-50">
```

**이유**: 같은 수준의 섹션 제목인데 font-weight가 다르면 시각적 계층이 혼란스럽다. "Recent Posts"가 "Featured Works"보다 덜 중요해 보이는 것은 의도적이지만, 같은 계층 level에서 weight가 다르면 디자인 언어가 깨진다.

**테스트 계획** — `tests/ui-improvement-recent-posts.spec.ts`:
```
테스트 1: "Recent Posts 섹션이 background-section-bg 없이 렌더링됨"
  - `/` 페이지 이동
  - 'Recent posts' 헤딩의 가장 가까운 section 부모의 background-color가 기본 배경색(--background)인지 확인

테스트 2: "Recent Posts 제목의 font-weight가 bold"
  - `/` 페이지 이동
  - 'Recent posts' 헤딩의 computed font-weight가 700(bold)인지 확인
```

---

### TODO 1-5: 헤드라인에 `text-wrap: balance` 추가

- [ ] **완료**

**변경 범위**:
- `src/app/(public)/page.tsx` — Hero `<h1>` 태그에 `text-wrap: balance` 추가
- 선택적으로 `src/app/globals.css`에 유틸리티 클래스 추가

**상세 지시**:
Hero의 `<h1>` 클래스에 `[text-wrap:balance]` 추가:
```tsx
<h1
  className="mb-4 text-4xl font-heading font-bold tracking-tight text-gray-900 md:text-5xl lg:text-6xl dark:text-gray-50 opacity-0 animate-fade-in-up [text-wrap:balance]"
  style={{ animationDelay: '100ms' }}
>
```

**이유**: Vercel Web Interface Guidelines Typography 규칙: `text-wrap: balance` or `text-pretty` on headings (prevents widows). 긴 헤드라인에서 마지막 줄에 단어 하나만 남는 "과부"(widow) 현상 방지.

**테스트 계획** — `tests/ui-improvement-text-balance.spec.ts`:
```
테스트 1: "Hero 헤드라인에 text-wrap: balance가 적용됨"
  - `/` 페이지 이동
  - h1 요소의 computed style text-wrap 값이 'balance'인지 확인
```

---

## Phase 2: 디자인 일관성 개선 (P2)

### TODO 2-1: `container` max-width 통일

- [ ] **완료**

**변경 범위**:
- `src/app/(public)/page.tsx` — 메인 페이지 container에 `max-w-7xl` 추가

**상세 지시**:
현재 페이지별 container 폭:
| 페이지 | 현재 | 변경 |
|--------|------|------|
| 메인(`/`) | `container mx-auto` (제한없음) | `container mx-auto max-w-7xl` |
| Blog, Works | `container mx-auto max-w-7xl` | 유지 |
| Introduction, Contact | `container mx-auto max-w-3xl` | 유지 (콘텐츠 페이지는 좁은 폭이 적절) |
| Resume | `container mx-auto` | 유지 (PDF 뷰어이므로 넓은 폭 적절) |

변경:
```tsx
{/* 기존: className="container mx-auto flex flex-col gap-16 px-4 py-8 md:px-6 md:py-12" */}
<div className="container mx-auto max-w-7xl flex flex-col gap-16 px-4 py-8 md:px-6 md:py-12">
```

**이유**: 메인 페이지만 max-width 제한이 없어서, 2560px 이상 초광폭 모니터에서 콘텐츠가 과도하게 펼쳐진다. Blog/Works 목록 페이지에서 돌아왔을 때 콘텐츠 영역 폭이 갑자기 달라져서 시각적 불연속이 발생한다.

**테스트 계획** — `tests/ui-improvement-container-width.spec.ts`:
```
테스트 1: "메인 페이지 container가 max-w-7xl 이내"
  - viewport 1920x1080
  - `/` 페이지 이동
  - 최상위 container 요소의 maxWidth computed style이 80rem(1280px) 이하인지 확인

테스트 2: "블로그 페이지와 메인 페이지의 container 폭이 동일"
  - viewport 1920x1080
  - `/` 이동 → container width 기록
  - `/blog` 이동 → container width 기록
  - 두 값이 동일한지 확인
```

---

### TODO 2-2: Navbar에서 "Latest writing" 중복 링크 제거

- [ ] **완료**

**변경 범위**:
- `src/components/layout/Navbar.tsx` — "Latest writing" 링크 제거 (약 162~166행)

**상세 지시**:
현재 Navbar에 다음이 공존한다:
1. 네비게이션 메뉴의 "Blog" 링크 → `/blog`
2. 우측 영역의 "Latest writing" 링크 → `/blog`

둘 다 `/blog`로 가므로 **완전 중복**이다. "Latest writing"을 제거한다.

삭제할 코드 (`src/components/layout/Navbar.tsx` 약 162~166행):
```tsx
<Link
    href="/blog"
    className="hidden rounded-full border border-border/80 px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:border-primary/30 hover:text-foreground xl:inline-flex"
>
    Latest writing
</Link>
```

**이유**: 동일한 목적지를 가리키는 중복 링크는 인지 부하를 높이고, 키보드 사용자의 Tab 수를 불필요하게 증가시킨다. Vercel Web Interface Guidelines의 Navigation 원칙 위반.

**테스트 계획** — `tests/ui-improvement-navbar-dedup.spec.ts`:
```
테스트 1: "Navbar에 'Latest writing' 링크가 없음"
  - `/` 페이지 이동 (viewport 1920x1080)
  - 'Latest writing' 텍스트를 가진 링크가 존재하지 않는지 확인

테스트 2: "Blog 네비게이션 링크는 여전히 존재"
  - `/` 페이지 이동
  - 네비게이션 내 'Blog' 링크가 visible이고 /blog로 이동하는지 확인
```

---

### TODO 2-3: 브랜드 컬러 역할 정리 — 날짜 배지 색상 통일

- [ ] **완료**

**변경 범위**:
- `src/app/(public)/page.tsx` — Featured Works 날짜 배지 (이미 TODO 1-3에서 재구성)
- `src/app/(public)/blog/page.tsx` — 변경 없음 (날짜 배지 없음, 텍스트만)
- `src/app/(public)/blog/[slug]/page.tsx` — Blog detail 날짜 배지: `bg-brand-accent` 유지
- `src/app/(public)/works/[slug]/page.tsx` — Work detail 날짜 배지: `bg-brand-orange` → `bg-brand-navy`로 통일

**상세 지시**:
현재 컬러 사용이 혼재:
- Blog detail: `bg-brand-accent` (빨간색)
- Work detail: `bg-brand-orange` (주황색)
- Works 목록 카드: `bg-brand-navy` (네이비)
- Home Featured Works: `bg-brand-navy` (네이비)

통일 규칙:
- **날짜 배지는 모든 곳에서 `bg-brand-navy`** 사용 → 날짜는 중립적 정보이므로 차분한 색상이 적합
- **hover/interactive accent**: `brand-accent` (빨간색) → 행동 유도 색상으로 유지
- `brand-orange`는 Work detail에서만 border-accent로 유지 가능하나, 배지에서는 제거

변경:
`src/app/(public)/blog/[slug]/page.tsx`:
```tsx
{/* 기존: bg-brand-accent */}
<Badge variant="secondary" className="rounded-full bg-brand-navy px-3 text-white hover:bg-brand-navy/90">
```

`src/app/(public)/works/[slug]/page.tsx`:
```tsx
{/* 기존: bg-brand-orange */}
<Badge variant="secondary" className="rounded-full bg-brand-navy px-3 text-white hover:bg-brand-navy/90">
```

**이유**: 같은 역할(날짜 표시)의 UI 요소가 페이지마다 다른 색상이면, 사용자는 무의식적으로 "이건 다른 종류의 정보인가?"라고 혼란을 느낀다. 디자인 토큰의 Semantic 일관성 원칙.

**테스트 계획** — `tests/ui-improvement-badge-color.spec.ts`:
```
테스트 1: "Blog detail 날짜 배지가 brand-navy 색상"
  - 블로그 글 하나가 존재한다고 가정
  - `/blog/[첫번째-slug]` 이동
  - time 요소를 감싸는 Badge의 background-color를 추출
  - 해당 색상이 brand-navy(oklch(0.25 0.08 260) ≈ 어두운 네이비)와 유사한지 확인

테스트 2: "Works detail 날짜 배지가 brand-navy 색상"
  - `/works/[첫번째-slug]` 이동
  - 날짜 Badge의 background-color가 blog detail과 동일한지 확인

테스트 3: "다크 모드에서 배지 텍스트(흰색)가 배경(navy) 위에서 충분한 명암비"
  - 다크 모드 활성화
  - 배지의 text-color(white)와 배지 background 간 contrast ratio >= 4.5
```

---

## Phase 3: 정교화 (P3)

### TODO 3-1: Works 카드 고정 높이를 유연한 레이아웃으로 변경

- [ ] **완료**

**변경 범위**:
- `src/app/globals.css` — `.works-feed-card` 관련 고정 height 규칙 수정

**상세 지시**:
현재:
```css
.works-feed-card { height: 30rem; }
/* + max-height 860px, 720px breakpoints에서 27rem, 24rem */
```

변경:
```css
.works-feed-card {
  min-height: 24rem;
  /* height: 30rem; 제거 — 콘텐츠에 맞게 자연 확장 */
}

@media (max-height: 860px) {
  .works-feed-card { min-height: 22rem; }
}

@media (max-height: 720px) {
  .works-feed-card { min-height: 20rem; }
}
```

**이유**: 고정 높이는 콘텐츠 길이가 다른 카드에서 하단 빈 공간(underfill) 또는 오버플로우(잘림)를 유발한다. `min-height`로 변경하면 최소 높이를 보장하면서 긴 콘텐츠도 수용한다.

**테스트 계획** — `tests/ui-improvement-works-card-height.spec.ts`:
```
테스트 1: "Works 카드 높이가 콘텐츠에 따라 유연하게 조절됨"
  - `/works` 페이지 이동
  - 첫 번째 works-feed-card 요소의 height >= min-height 확인
  - height가 정확히 30rem(480px)이 아닌지 확인 (고정값이 아님)

테스트 2: "짧은 뷰포트(720px 높이)에서 카드가 적절한 min-height 유지"
  - viewport 1280x720
  - `/works` 페이지 이동
  - 카드 높이가 최소 20rem(320px) 이상인지 확인
```

---

### TODO 3-2: Hero 애니메이션 LCP 개선 — opacity:0 시작 문제 해결

- [ ] **완료**

**변경 범위**:
- `src/app/(public)/page.tsx` — Hero 요소들의 inline `opacity-0` 클래스 처리 방식 변경
- `src/app/globals.css` — 애니메이션 키프레임 및 클래스 수정

**상세 지시**:
현재 문제: Hero의 `<h1>`, `<p>`, 프로필 이미지 컨테이너 모두 `opacity-0` 클래스가 적용되어 있고, CSS 애니메이션(0.8초)이 끝난 후에야 `opacity: 1`이 된다. 이는:
1. JavaScript가 로드되지 않으면 콘텐츠가 영구적으로 보이지 않음
2. LCP 요소(h1 텍스트)가 0.8초 지연됨 → Core Web Vitals 점수 하락

해결책: SSR에서는 콘텐츠가 즉시 보이고, 클라이언트에서만 애니메이션 실행.

방법 1 (CSS만으로 해결 — 권장):
```css
.animate-fade-in-up {
  animation: fadeInUp 0.8s ease-out forwards;
}

/* JS가 로드되지 않은 상태에서는 콘텐츠를 보여줌 */
@media (scripting: none) {
  .animate-fade-in-up {
    opacity: 1 !important;
    transform: none !important;
    animation: none;
  }
}
```

그리고 `page.tsx`에서 `opacity-0` 클래스는 유지하되, `@media (scripting: none)` fallback이 이를 override한다.

**테스트 계획** — `tests/ui-improvement-hero-lcp.spec.ts`:
```
테스트 1: "Hero 헤드라인이 1초 이내에 visible 상태"
  - `/` 페이지 이동
  - page.waitForTimeout(1000) (애니메이션 완료 대기)
  - h1 요소가 visible이고 opacity > 0인지 확인

테스트 2: "reduced-motion에서 Hero 콘텐츠가 즉시 visible"
  - page.emulateMedia({ reducedMotion: 'reduce' })
  - `/` 페이지 이동
  - h1의 opacity가 즉시(100ms 이내) 1인지 확인
```

---

### TODO 3-3: Recent Posts 카드에 태그 뱃지 추가

- [ ] **완료**

**변경 범위**:
- `src/app/(public)/page.tsx` — Recent Posts 카드에 태그 뱃지 시각 요소 보강

**상세 지시**:
현재 Recent Posts 카드에는 태그가 `border-l` 구분선 뒤 텍스트로만 표시된다:
```tsx
{post.tags?.[0] && (
  <span className="border-l border-gray-400 pl-4">{post.tags[0]}</span>
)}
```

변경 — 첫 번째 태그를 styled badge로:
```tsx
{post.tags?.[0] && (
  <span className="rounded-full bg-muted px-2.5 py-0.5 text-xs font-medium text-muted-foreground">
    {post.tags[0]}
  </span>
)}
```

또한 카드에 `border` 추가하여 경계를 명확히:
```tsx
{/* 기존: className="border-none shadow-sm" */}
<Card className="overflow-hidden rounded-2xl border-border/80 bg-background shadow-sm transition hover:border-primary/30 hover:shadow-md">
```

**이유**: `border-none`으로 카드 경계가 완전히 사라진 현재 상태에서는 카드 간 구분이 불분명하다. 특히 `bg-brand-section-bg` 배경이 제거된 후(TODO 1-4) 카드가 더 묻힐 수 있다. 배경 대신 border로 경계를 표현하고, 태그를 badge로 강조하면 시각적 anchor point가 늘어난다.

**테스트 계획** — `tests/ui-improvement-recent-posts-cards.spec.ts`:
```
테스트 1: "Recent Posts 카드에 border가 있음"
  - `/` 페이지 이동
  - Recent Posts 첫 번째 카드의 border-width가 > 0인지 확인

테스트 2: "태그가 badge 스타일로 표시됨"
  - `/` 페이지 이동
  - Recent Posts 카드 내 태그 요소가 rounded background를 가지는지 확인

테스트 3: "카드 hover 시 shadow 변화"
  - `/` 페이지 이동
  - 첫 번째 Recent Posts 카드에 hover
  - box-shadow computed value가 변경되었는지 확인
```

---

## 전체 회귀 테스트 계획

모든 Phase 완료 후, 기존 테스트가 깨지지 않았는지 확인하는 최종 단계.

### 기존 테스트 실행 목록
```bash
npx playwright test tests/dark-mode.spec.ts
npx playwright test tests/public-content.spec.ts
npx playwright test tests/public-layout-stability.spec.ts
npx playwright test tests/public-detail-pages.spec.ts
npx playwright test tests/public-works-pagination.spec.ts
npx playwright test tests/public-blog-pagination.spec.ts
npx playwright test tests/public-edge-nav.spec.ts
```

### 새 테스트 전체 실행
```bash
npx playwright test tests/ui-improvement-*.spec.ts --project=chromium-public
```

### 영상 결과 확인
모든 테스트 영상은 `test-results/playwright/` 디렉토리에 자동 저장된다.
Playwright config에 `video: 'on'`이 설정되어 있으므로 별도 설정 불필요.

---

## 변경 파일 요약

| Phase | 파일 | 변경 유형 |
|-------|------|-----------|
| P0 | `src/app/globals.css` | 수정 (reduced-motion, color-scheme, muted-foreground) |
| P0 | `src/app/(public)/layout.tsx` | 수정 (skip link, main id) |
| P0 | `src/app/(public)/page.tsx` | 수정 (alt 텍스트, dark:text-gray-500) |
| P0 | `src/app/(public)/works/page.tsx` | 수정 (dark:text-gray-500) |
| P0 | `src/app/(public)/works/[slug]/page.tsx` | 수정 (dark:text-gray-500) |
| P1 | `src/app/(public)/page.tsx` | 대규모 수정 (CTA, 섹션 순서, Works 카드 그리드, container) |
| P1 | `src/components/layout/Navbar.tsx` | 수정 (Latest writing 제거) |
| P2 | `src/app/(public)/blog/[slug]/page.tsx` | 수정 (배지 색상) |
| P2 | `src/app/(public)/works/[slug]/page.tsx` | 수정 (배지 색상) |
| P3 | `src/app/globals.css` | 수정 (works-feed-card, 애니메이션) |
| P3 | `src/app/(public)/page.tsx` | 수정 (카드 border, 태그 badge) |
| 테스트 | `tests/ui-improvement-reduced-motion.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-skip-link.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-hero-alt.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-color-scheme.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-contrast.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-hero-cta.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-section-order.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-featured-works-grid.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-recent-posts.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-text-balance.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-container-width.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-navbar-dedup.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-badge-color.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-works-card-height.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-hero-lcp.spec.ts` | 신규 |
| 테스트 | `tests/ui-improvement-recent-posts-cards.spec.ts` | 신규 |

총 변경 파일: **소스 8개, 테스트 16개 신규**
