# 개선 실행 지시서 — Public Portfolio B+ → A+

> **생성일**: 2026-04-12  
> **참조**: `design-pattern-public-0412.md` (디자인 패턴), `design-public-architecture-test-0412.md` (평가서)  
> **목표**: 13개 항목 전수 실행 → 디자인 시스템 일관성 확보 → 포트폴리오 등급 A+  
> **UI/UX Pro Max 규칙 적용**: Priority 1 (Accessibility), 4 (Style), 6 (Typography & Color)

---

## Phase 1: CRITICAL — 코드 품질이 눈에 보이는 것 (1~2시간)

### Task 1. Raw Color → Semantic Token 전환

**우선순위**: 1 (최우선)  
**난이도**: M  
**영향 파일 수**: 6개  
**UI/UX Pro Max 규칙**: `color-semantic`, `color-contrast`, `color-dark-mode`

#### 1.1 일괄 치환 맵

아래 패턴을 모든 `src/app/(public)/**` 및 `src/components/layout/**` 파일에 적용한다.

| 찾기 (regex) | 바꾸기 | 비고 |
|---|---|---|
| `text-gray-900 dark:text-gray-50` | `text-foreground` | 제목 텍스트 14곳 |
| `text-gray-600 dark:text-gray-300` | `text-foreground/80` | 본문 텍스트 8곳 |
| `text-gray-600 dark:text-gray-400` | `text-muted-foreground` | 메타 텍스트 |
| `text-gray-500 dark:text-gray-400` | `text-muted-foreground` | 보조 텍스트 10곳 |
| `text-gray-500` (단독) | `text-muted-foreground` | empty state 등 |
| `text-gray-400 dark:text-gray-500` | `text-muted-foreground/70` | placeholder 텍스트 |
| `text-gray-400` (단독, 폴백 아이콘 등) | `text-muted-foreground` | avatar, no-image |
| `bg-gray-100 dark:bg-gray-800` | `bg-muted` | placeholder 배경 5곳 |
| `bg-gray-200 dark:bg-gray-800` | `bg-muted` | 프로필 원 배경 |
| `bg-gray-50 dark:bg-gray-900` | `bg-brand-section-bg` | excerpt 배경 (detail) |
| `bg-white dark:bg-gray-950` | `bg-background` | Footer 배경 |
| `from-gray-100 to-gray-200 ... dark:from-gray-800 dark:to-gray-900` | `from-muted to-muted/80` | Works no-image gradient |

#### 1.2 파일별 구체적 변경

**`src/app/(public)/page.tsx`**:
```
Line ~42: text-gray-900 ... dark:text-gray-50 → text-foreground
Line ~48: text-gray-600 dark:text-gray-400 → text-muted-foreground  
Line ~73: bg-gray-200 ... dark:bg-gray-800 → bg-muted
Line ~82: text-gray-400 → text-muted-foreground
Line ~93: text-gray-900 ... dark:text-gray-50 → text-foreground (Featured works h2)
Line ~112: bg-gray-100 dark:bg-gray-800 → bg-muted  
Line ~120: text-gray-400 → text-muted-foreground ("No Image")
Line ~129: text-gray-500 dark:text-gray-400 → text-muted-foreground (category)
Line ~132: text-gray-900 ... dark:text-gray-50 → text-foreground (card title)
Line ~135: text-gray-600 dark:text-gray-300 → text-foreground/80 (excerpt)
Line ~155: text-gray-900 ... dark:text-gray-50 → text-foreground (Recent posts h2)
Line ~188: text-gray-600 dark:text-gray-400 → text-muted-foreground (date wrapper)
Line ~199: text-gray-600 dark:text-gray-300 → text-foreground/80 (post excerpt)
```

**`src/app/(public)/works/page.tsx`**:
```
Line ~80: text-gray-900 dark:text-gray-50 → text-foreground (h1)
Line ~100: bg-gray-100 dark:bg-gray-800 → bg-muted (image placeholder bg)
Line ~112-114: from-gray-100 to-gray-200 text-gray-400 dark:from-gray-800 dark:to-gray-900 → from-muted to-muted/80 text-muted-foreground
Line ~120: text-gray-500 dark:text-gray-400 → text-muted-foreground (category)
Line ~124: text-gray-900 ... dark:text-gray-50 → text-foreground (card title)
Line ~127: text-gray-600 dark:text-gray-300 → text-foreground/80 (excerpt)
Line ~133: bg-gray-100 ... text-gray-600 dark:bg-gray-800 dark:text-gray-400 → bg-muted text-muted-foreground (tags)
Line ~empty: text-gray-500 → text-muted-foreground (empty state)
```

**`src/app/(public)/blog/page.tsx`**:
```
Line ~82: text-gray-900 dark:text-gray-50 → text-foreground (h1)
Line ~110: text-gray-900 ... dark:text-gray-50 → text-foreground (card title)
Line ~115: text-gray-600 dark:text-gray-300 → text-foreground/80 (excerpt)
```

**`src/app/(public)/blog/[slug]/page.tsx`**:
```
Line ~83: text-gray-900 dark:text-gray-50 → text-foreground (h1)
Line ~84: text-gray-500 dark:text-gray-400 → text-muted-foreground (meta wrapper)
Line ~95: bg-gray-50 ... text-gray-600 dark:bg-gray-900 dark:text-gray-300 → bg-brand-section-bg text-foreground/80 (excerpt blockquote)
```

**`src/app/(public)/works/[slug]/page.tsx`**:
```
Line ~75: text-gray-900 dark:text-gray-50 → text-foreground (h1)
Line ~79: text-gray-500 dark:text-gray-400 → text-muted-foreground (category)
Line ~82: text-gray-400 dark:text-gray-400 → text-muted-foreground/70 (period)
Line ~85: bg-gray-50 ... text-gray-600 dark:bg-gray-900 dark:text-gray-300 → bg-brand-section-bg text-foreground/80 (excerpt)
Line ~88: text-gray-500 dark:text-gray-400 → text-muted-foreground (tags wrapper)
```

**`src/components/layout/Footer.tsx`**:
```
Line ~33: bg-white ... dark:bg-gray-950 → bg-background
Line ~45: text-gray-600 ... dark:text-gray-400 → text-muted-foreground
Line ~55: text-gray-500 dark:text-gray-400 → text-muted-foreground
```

#### 1.3 검증

```bash
# 1. raw gray color가 public 페이지에서 완전히 제거되었는지 확인
grep -rn "text-gray-\|bg-gray-" src/app/\(public\)/ src/components/layout/Footer.tsx

# 2. 기대 결과: 0건 (gradient 전용 제외)
```

---

### Task 2. Hover 색상 체계 통일

**우선순위**: 2  
**난이도**: S  
**UI/UX Pro Max 규칙**: `consistency`, `state-clarity`

#### 변경 사항

| 파일 | 현재 | 변경 후 |
|------|------|---------|
| `page.tsx:186` (Recent Posts 제목) | `hover:text-brand-cyan` | `group-hover:text-brand-accent` |
| `page.tsx:171` (Recent Posts Card) | className에 `group` 없음 | Card에 `group` 추가 |

#### 구체적 코드 변경

**`src/app/(public)/page.tsx` — Recent Posts 카드**:

현재:
```tsx
<Card key={post.id} data-testid="recent-post-card" className="overflow-hidden rounded-2xl ...">
  ...
  <CardTitle className="text-xl font-bold">
    <Link href={...} className="transition-colors hover:text-brand-cyan">
```

변경:
```tsx
<Link key={post.id} href={`/blog/${post.slug}?relatedPage=${page}`} className="group block h-full" data-testid="recent-post-card">
  <Card className="flex h-full flex-col overflow-hidden rounded-2xl ...">
    ...
    <CardTitle className="text-lg font-heading font-bold leading-tight text-foreground transition-colors group-hover:text-brand-accent sm:text-xl">
      {post.title}
    </CardTitle>
```

**규칙**: Recent Posts도 Works/Blog과 동일한 구조 — `Link`가 카드 전체를 감싸고 `group-hover`로 제목 색상 변경.

---

## Phase 2: HIGH — 사용자 경험 직결 (2~3시간)

### Task 3. Blog 카드 시각 앵커 + Grid 조정

**우선순위**: 3  
**난이도**: M  
**UI/UX Pro Max 규칙**: `style-match`, `visual-hierarchy`, `line-length-control`

#### 3.1 Accent Stripe 추가

**`src/app/(public)/blog/page.tsx`** — Blog 카드 내부:

```tsx
<Card className="... py-0">
  {/* Accent stripe — visual anchor for text-only cards */}
  <div className="h-1 w-full rounded-t-2xl bg-gradient-to-r from-brand-accent to-brand-cyan" />
  <CardHeader className="px-4 pt-4 pb-0 sm:px-5 sm:pt-5">
    ...
```

#### 3.2 Grid 변경

```tsx
{/* 변경 전 */}
<div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">

{/* 변경 후 */}
<div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
```

#### 3.3 `!important` 제거

Card 컴포넌트의 기본 `py-6`과 `gap-6`을 override하는 방식 변경:

```tsx
{/* 변경 전 */}
<Card className="responsive-feed-card !gap-0 !py-0 flex h-full flex-col ...">

{/* 변경 후 — py-0과 gap-0은 !important 없이 적용 */}
<Card className="responsive-feed-card flex h-full flex-col gap-0 py-0 ...">
```

> **참고**: shadcn v4에서는 `className` merge가 `cn()` (tailwind-merge)로 처리되므로 마지막에 선언된 클래스가 우선한다. `!important`는 불필요.

---

### Task 4. Featured Works "No Image" 폴백 통일

**우선순위**: 4  
**난이도**: S

#### 변경

**`src/app/(public)/page.tsx:119-121`** — Featured Works 카드:

현재:
```tsx
<div className="flex h-full w-full items-center justify-center text-sm font-medium text-gray-400">
  No Image
</div>
```

변경 (Works 페이지와 동일 패턴):
```tsx
<div className="flex h-full w-full flex-col items-center justify-center gap-2 bg-gradient-to-br from-muted to-muted/80 text-muted-foreground">
  <BriefcaseBusiness className="h-8 w-8" aria-hidden="true" />
  <span className="text-xs font-medium">No Image</span>
</div>
```

**필요**: `import { BriefcaseBusiness } from 'lucide-react'` 추가

---

### Task 5. Works 상세 — Prev/Next 네비게이션 추가

**우선순위**: 5  
**난이도**: M  
**UI/UX Pro Max 규칙**: `predictable-back`, `escape-routes`

#### 5.1 데이터 준비

**`src/app/(public)/works/[slug]/page.tsx`** — Blog detail과 동일한 패턴으로 정렬 + index 계산 추가:

```tsx
const allWorks = await fetchAllPublicWorks()
const sortedWorks = [...allWorks].sort((left, right) => {
  const leftTime = left.publishedAt ? new Date(left.publishedAt).getTime() : 0
  const rightTime = right.publishedAt ? new Date(right.publishedAt).getTime() : 0
  if (leftTime !== rightTime) return rightTime - leftTime
  return left.title.localeCompare(right.title)
})
const currentIndex = sortedWorks.findIndex((item) => item.id === work.id)
const newerWork = currentIndex > 0 ? sortedWorks[currentIndex - 1] : null
const olderWork = currentIndex >= 0 && currentIndex < sortedWorks.length - 1 ? sortedWorks[currentIndex + 1] : null
```

#### 5.2 네비게이션 UI

Blog detail의 prev/next를 그대로 미러링:

```tsx
{(olderWork || newerWork) && (
  <nav
    aria-label="Work navigation"
    data-testid="work-prev-next"
    className="mt-12 grid gap-3 border-t border-border/70 pt-8 sm:grid-cols-2"
  >
    {olderWork ? (
      <Link
        href={`/works/${olderWork.slug}`}
        className="rounded-2xl border border-border/80 bg-background p-4 transition hover:border-primary/30 hover:shadow-sm"
      >
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Previous Work</p>
        <p className="mt-2 text-base font-semibold text-foreground">{olderWork.title}</p>
      </Link>
    ) : <div />}
    {newerWork ? (
      <Link
        href={`/works/${newerWork.slug}`}
        className="rounded-2xl border border-border/80 bg-background p-4 text-left transition hover:border-primary/30 hover:shadow-sm sm:justify-self-end"
      >
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Next Work</p>
        <p className="mt-2 text-base font-semibold text-foreground">{newerWork.title}</p>
      </Link>
    ) : null}
  </nav>
)}
```

**위치**: `relatedWorks` 섹션 바로 위, admin editor 아래.

---

### Task 6. Footer 네비게이션 확장

**우선순위**: 6  
**난이도**: M  
**UI/UX Pro Max 규칙**: `predictable-back`, `content-priority`

#### 변경 (Footer.tsx)

```tsx
<footer className="w-full border-t border-border bg-background py-8">
  <div className="container mx-auto flex flex-col items-center gap-6 px-4">
    {/* Navigation Links */}
    <nav aria-label="Footer navigation" className="flex flex-wrap items-center gap-6 text-sm">
      <Link href="/" className="text-muted-foreground transition-colors hover:text-foreground">Home</Link>
      <Link href="/works" className="text-muted-foreground transition-colors hover:text-foreground">Works</Link>
      <Link href="/blog" className="text-muted-foreground transition-colors hover:text-foreground">Blog</Link>
    </nav>

    {/* Social Icons */}
    {socialLinks.length > 0 && (
      <div className="flex items-center gap-6">
        {socialLinks.map(({ url, icon: Icon, label }) => (
          <Link key={label} href={url} target="_blank" rel="noopener noreferrer"
            className="text-muted-foreground transition-colors hover:text-brand-accent"
            aria-label={label}>
            <Icon size={24} />
          </Link>
        ))}
      </div>
    )}

    {/* Copyright */}
    <p className="text-center text-sm text-muted-foreground">
      &copy; {new Date().getFullYear()} {ownerName}. All rights reserved.
    </p>
  </div>
</footer>
```

---

## Phase 3: MEDIUM — 품질 인상 (1~2시간)

### Task 7. 태그 스타일 통일

**우선순위**: 7  
**난이도**: S

**`src/app/(public)/works/page.tsx:133`** — Works 카드 태그:

현재:
```tsx
<span key={tag} className="rounded bg-gray-100 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wider text-gray-600 dark:bg-gray-800 dark:text-gray-400">
```

변경:
```tsx
<span key={tag} className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
```

- `rounded` → `rounded-full`
- `text-[10px]` → `text-xs`
- `uppercase tracking-wider` → 제거 (Blog 태그와 일치)
- `bg-gray-100 dark:bg-gray-800` → `bg-muted`
- `text-gray-600 dark:text-gray-400` → `text-muted-foreground`

---

### Task 8. 언어 혼용 정리

**우선순위**: 8  
**난이도**: S

| 파일 | 현재 | 변경 |
|------|------|------|
| `blog/[slug]/page.tsx` | `heading="다른 게시물"` | `heading="More Posts"` |
| `works/[slug]/page.tsx` | `heading="다른 작업"` | `heading="More Works"` |

---

### Task 9. Works 카드 animationDelay 잔재 제거

**우선순위**: 9  
**난이도**: S

**`src/app/(public)/works/page.tsx:97`**:

현재:
```tsx
<article
  className="responsive-feed-card works-feed-card flex h-full flex-col ..."
  style={{ animationDelay: `${(index * 100) + 200}ms` }}
>
```

변경:
```tsx
<article
  className="responsive-feed-card works-feed-card flex h-full flex-col ..."
>
```

`style` prop 전체 제거. `animate-fade-in-up` 클래스가 없으므로 이 delay는 dead code다.

---

### Task 10. "Click to view details" 카피 수정

**우선순위**: 10  
**난이도**: S

**`src/app/(public)/page.tsx:137`**:

현재:
```tsx
{work.excerpt || 'Click to view details'}
```

변경:
```tsx
{work.excerpt}
```

발췌문이 없으면 표시하지 않는다. 빈 공간은 `flex-1`에 의해 자연스럽게 처리됨.

---

### Task 11. Avatar 폴백 아이콘 전환

**우선순위**: 11  
**난이도**: S

**`src/app/(public)/page.tsx:80-84`**:

현재:
```tsx
<div role="img" aria-label={headline} className="flex h-full w-full items-center justify-center text-gray-400">
  Avatar
</div>
```

변경:
```tsx
<div role="img" aria-label={headline} className="flex h-full w-full items-center justify-center text-muted-foreground">
  <User className="h-16 w-16" aria-hidden="true" />
</div>
```

**필요**: `import { User } from 'lucide-react'` 추가

---

### Task 12. Blog 카드 `!important` 제거

(Task 3.3에서 이미 처리됨 — 중복 확인)

---

### Task 13. 카드 제목 타이포 스케일 통일

**우선순위**: 13  
**난이도**: S

모든 피드 카드 제목을 동일하게 맞춘다:

| 파일 | 현재 | 변경 |
|------|------|------|
| Blog 카드 제목 | `text-lg sm:text-xl md:text-2xl` | `text-lg sm:text-xl` |
| Works 카드 제목 | `text-lg sm:text-xl` | 유지 |
| Featured Works 카드 제목 | `text-lg` | `text-lg sm:text-xl` |
| Recent Posts 제목 | `text-xl` | `text-lg sm:text-xl` |

**통일 값**: `text-lg sm:text-xl font-heading font-bold leading-tight`

---

## 실행 후 검증 체크리스트

### 자동 검증

```bash
# 1. raw gray color 잔존 확인
grep -rn "text-gray-\|bg-gray-" src/app/\(public\)/ src/components/layout/Footer.tsx | grep -v "// " | wc -l
# 기대: 0

# 2. !important 잔존 확인
grep -rn "!" src/app/\(public\)/blog/page.tsx | grep -E "!py|!gap|!p-"
# 기대: 0

# 3. 한국어 heading 잔존
grep -rn "다른 게시물\|다른 작업" src/app/\(public\)/
# 기대: 0

# 4. "Click to view details" 잔존
grep -rn "Click to view details" src/app/\(public\)/
# 기대: 0

# 5. "Avatar" 텍스트 잔존
grep -rn ">Avatar<" src/app/\(public\)/
# 기대: 0

# 6. dead animationDelay
grep -rn "animationDelay" src/app/\(public\)/works/page.tsx
# 기대: 0

# 7. TypeScript 빌드 확인
npx tsc --noEmit

# 8. 린트
npx next lint
```

### 수동(시각) 검증

| 항목 | 확인 방법 |
|------|----------|
| 라이트/다크 모드 전환 | 모든 텍스트가 semantic token으로 동작하는지 확인 |
| Blog 카드 accent stripe | 상단 4px gradient bar 표시 확인 |
| Blog grid 3열 | xl viewport에서 3열 확인 |
| Works no-image 폴백 | Home과 Works 페이지에서 동일한 gradient+icon 패턴 확인 |
| Card hover 색상 | 모든 카드 제목 hover가 `brand-accent`(빨강) 확인 |
| Footer 네비게이션 | Home/Works/Blog 링크 존재 확인 |
| Works detail prev/next | 이전/다음 작업 네비게이션 표시 확인 |

---

## 예상 결과

| 항목 | Before | After |
|------|--------|-------|
| 전체 등급 | B+ | **A+** |
| Semantic token 사용률 | ~30% | 100% |
| Hover 색상 일관성 | 혼용 | 체계적 |
| Blog 카드 인상 | 텍스트 벽 | 시각적 앵커 |
| Design system 신뢰도 | 낮음 | 높음 |
| Footer 완성도 | 최소 | 표준 |
| 코드 품질 (portfolio 관점) | "개발자 포폴" | **"프로 포폴"** |

---

## 타임라인

| Phase | 작업 | Tasks |
|-------|------|-------|
| **Phase 1** | CRITICAL — Token 전환 + Hover 통일 | Task 1, 2 |
| **Phase 2** | HIGH — UX 개선 | Task 3, 4, 5, 6 |
| **Phase 3** | MEDIUM/LOW — 디테일 마무리 | Task 7, 8, 9, 10, 11, 12, 13 |
| **검증** | 자동 + 수동 테스트 | 체크리스트 전수 |

**실행 순서 원칙**: 한 Phase 완료 후 검증 → 다음 Phase 진입. Phase 내에서는 Task 번호순.
