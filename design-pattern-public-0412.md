# Design Pattern — Public Portfolio Site

> **생성일**: 2026-04-12  
> **대상**: `src/app/(public)/**`, `src/components/layout/**`, `src/components/ui/**`  
> **목적**: 하드코딩 제거, 일관된 디자인 언어 수립, "프로 포트폴리오" 등급 달성

---

## 1. Color Token System

### 1.1 절대 규칙: Raw Tailwind Color 금지

컴포넌트에서 `text-gray-*`, `bg-gray-*`, `border-gray-*` 등 **raw Tailwind 색상 직접 사용을 전면 금지**한다.  
이미 `globals.css`에 정의된 semantic token을 반드시 사용한다.

### 1.2 Token Mapping Table

| 역할 | 사용할 Token | Tailwind Class | 대체 대상 (제거할 것) |
|------|-------------|----------------|----------------------|
| **제목 텍스트** | `--foreground` | `text-foreground` | `text-gray-900 dark:text-gray-50` |
| **본문 텍스트** | `--foreground` + opacity | `text-foreground/80` | `text-gray-600 dark:text-gray-300` |
| **보조 텍스트** | `--muted-foreground` | `text-muted-foreground` | `text-gray-500 dark:text-gray-400` |
| **비활성 텍스트** | `--muted-foreground` + opacity | `text-muted-foreground/70` | `text-gray-400 dark:text-gray-500` |
| **카드 배경** | `--card` | `bg-card` | `bg-white dark:bg-gray-900` |
| **뮤트 배경** | `--muted` | `bg-muted` | `bg-gray-100 dark:bg-gray-800` |
| **섹션 배경** | `--brand-section-bg` | `bg-brand-section-bg` | `bg-gray-50 dark:bg-gray-900` |
| **페이지 배경** | `--background` | `bg-background` | `bg-white dark:bg-gray-950` |
| **테두리** | `--border` | `border-border` | `border-gray-200 dark:border-gray-700` |

### 1.3 Brand Color Purpose

| Token | 용도 | 사용 가능 위치 |
|-------|------|---------------|
| `brand-accent` | 카드 제목 hover, CTA 강조 | `group-hover:text-brand-accent` |
| `brand-navy` | 날짜 Badge 배경 | `bg-brand-navy` |
| `brand-cyan` | 텍스트 링크 hover, "View all" | `text-brand-cyan`, `hover:text-brand-cyan` |
| `brand-orange` | 경고, 특별 라벨 | 필요 시만 사용 |
| `brand-section-bg` | 섹션 배경 구분 | `bg-brand-section-bg` |

### 1.4 Hover Color 규칙

| 요소 유형 | Hover 색상 | 규칙 |
|-----------|-----------|------|
| **카드 제목** (Works, Blog, Featured, Recent) | `group-hover:text-brand-accent` | 모든 카드 제목 동일 |
| **독립 텍스트 링크** ("View all", nav 링크) | `hover:text-brand-cyan` | 링크 전용 |
| **태그 hover** | `hover:text-brand-accent` | detail 페이지 tags |
| **소셜 아이콘** | `hover:text-brand-accent` | Footer |

**핵심**: 카드 제목과 독립 링크의 hover color가 다른 것은 의도적 체계. 하지만 **같은 계층은 반드시 같은 색**이어야 한다.

---

## 2. Typography Scale

### 2.1 Heading Hierarchy

| Level | 사용처 | 크기 |
|-------|-------|------|
| **Hero H1** | Home 메인 제목 | `text-4xl md:text-5xl lg:text-6xl` |
| **Page H1** | Works, Blog, Detail 페이지 제목 | `text-3xl md:text-4xl` |
| **Section H2** | Home 섹션 제목 ("Featured works", "Recent posts") | `text-xl md:text-2xl` |
| **Card Title** | 모든 피드 카드 제목 (통일) | `text-lg sm:text-xl` |

### 2.2 Body Text Scale

| 역할 | 크기 | 행간 |
|------|------|------|
| Hero 소개문 | `text-lg` | default |
| 카드 발췌문 | `text-sm sm:text-base` | `leading-relaxed` |
| 카드 카테고리/메타 | `text-xs` | default |
| 태그 | `text-xs` | default |
| Badge 텍스트 | `text-xs` | default |

### 2.3 Font Family 규칙

| 용도 | Font | Class |
|------|------|-------|
| 제목 (h1~h3) | Archivo | `font-heading` |
| 본문 | Space Grotesk | `font-sans` (default) |
| 코드, 태그 hash | Geist Mono | `font-mono` |

---

## 3. Card Design Pattern

### 3.1 공통 카드 Shell

모든 피드 카드는 다음 기본 구조를 따른다:

```
┌─ Card ─────────────────────────────┐
│  [Visual Anchor: Image or Stripe]  │
│                                    │
│  [Badge: Date] [Category/Tags]     │
│  [Title: text-lg sm:text-xl]       │
│  [Excerpt: text-sm sm:text-base]   │
│  [Tags: text-xs, rounded-full]     │
└────────────────────────────────────┘
```

### 3.2 카드 CSS Class 규칙

```
Card shell:
  className="flex h-full flex-col overflow-hidden rounded-2xl border-border/80 bg-background py-0 shadow-sm transition hover:border-primary/30 hover:shadow-md"

Card content area:
  className="flex flex-1 flex-col p-4 sm:p-5"
```

- `py-0`을 Card 기본에 적용 (shadcn 기본 `py-6` override → `!py-0` 대신 Card variant 또는 일관된 `py-0`)
- `!important` 사용 금지 — Card 컴포넌트에 `data-variant` 또는 className 전달로 해결

### 3.3 Works 카드 vs Blog 카드

| 요소 | Works | Blog |
|------|-------|------|
| **이미지** | `aspect-[4/3]` 썸네일 | **상단 4px accent stripe** (카테고리별 색상 또는 고정 accent) |
| **No-image 폴백** | gradient + `BriefcaseBusiness` icon | N/A (stripe는 항상 표시) |
| **날짜 Badge** | `bg-brand-navy rounded-full text-xs text-white` | 동일 |
| **카테고리** | `text-xs font-medium uppercase text-muted-foreground` | 태그로 대체 |
| **제목** | `text-lg sm:text-xl font-heading font-bold text-foreground` | 동일 |
| **제목 hover** | `group-hover:text-brand-accent` (동일 group naming) | 동일 |
| **발췌문** | `text-sm sm:text-base text-foreground/80 line-clamp-3` | 동일 |
| **태그** | `text-xs rounded-full bg-muted text-muted-foreground` | 동일 |

### 3.4 이미지 Placeholder 폴백 — 통일 패턴

모든 "No Image" 상태는 동일한 컴포넌트/패턴을 사용한다:

```tsx
<div className="flex h-full w-full flex-col items-center justify-center gap-2 bg-gradient-to-br from-muted to-muted/80 text-muted-foreground">
  <IconComponent className="h-8 w-8" aria-hidden="true" />
  <span className="text-xs font-medium">No Image</span>
</div>
```

- Works 전용 아이콘: `BriefcaseBusiness`
- 프로필 전용 아이콘: `User` (lucide)
- "Avatar" 텍스트 표시 금지, "No Image" 텍스트만 표시 금지 → 반드시 아이콘 + 라벨

### 3.5 태그 스타일 — 단일 패턴

모든 태그(Works, Blog, Detail 공통):

```
className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground"
```

- `rounded` (각진) → `rounded-full` (통일)
- `text-[10px]` → `text-xs` (통일)
- `bg-gray-100 dark:bg-gray-800` → `bg-muted` (semantic token)
- `text-gray-600 dark:text-gray-400` → `text-muted-foreground` (semantic token)

---

## 4. Blog 카드 시각 앵커 — Accent Stripe Pattern

Blog 카드에는 이미지가 없으므로, **상단 accent bar**로 시각적 앵커를 제공한다.

```tsx
{/* Blog Card Top Accent Stripe */}
<div className="h-1 w-full bg-gradient-to-r from-brand-accent to-brand-cyan" />
```

- 높이: `h-1` (4px)
- 그라데이션: `from-brand-accent to-brand-cyan` → 브랜드 색상 활용
- 위치: Card 내부 최상단 (이미지 자리)

이를 통해:
- 텍스트 벽(Wall of Text) 방지
- 카드 간 시각적 분리 강화
- 브랜드 색상 노출 증가

---

## 5. Grid Layout 규칙

| 페이지 | 데스크탑 | 태블릿 | 모바일 |
|--------|---------|--------|--------|
| Works 목록 | `xl:grid-cols-4` | `md:grid-cols-2` | `grid-cols-1` |
| Blog 목록 | `xl:grid-cols-3` | `md:grid-cols-2` | `grid-cols-1` |
| Featured Works (Home) | `xl:grid-cols-3` | `md:grid-cols-2` | `grid-cols-1` |
| Recent Posts (Home) | `md:grid-cols-2` | `md:grid-cols-2` | `grid-cols-1` |

**Blog은 `xl:grid-cols-3`**: 텍스트 전용 카드에 4열은 과밀. 3열이 가독성 최적.

---

## 6. Navigation & Footer Pattern

### 6.1 Footer 구조

```
┌─ Footer ────────────────────────────────┐
│  ┌─ Top ─────────────────────────────┐  │
│  │  Navigation Links (3-column)      │  │
│  │  Works | Blog | Contact           │  │
│  └───────────────────────────────────┘  │
│  ┌─ Middle ──────────────────────────┐  │
│  │  Social Icons                     │  │
│  └───────────────────────────────────┘  │
│  ┌─ Bottom ──────────────────────────┐  │
│  │  © 2026 Name. All rights reserved.│  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

- 배경: `bg-background` (semantic token, `bg-white` 하드코딩 제거)
- 구분선: `border-t border-border`
- 네비게이션 링크 추가 (Works, Blog, Home 최소)

### 6.2 Prev/Next Navigation — 모든 Detail 공통

Blog과 Works 상세 모두 prev/next 네비게이션을 동일 패턴으로 제공:

```tsx
<nav aria-label="{type} navigation" className="mt-12 grid gap-3 border-t border-border/70 pt-8 sm:grid-cols-2">
  {olderItem && (
    <Link href={`/${type}/${olderItem.slug}`} className="rounded-2xl border border-border/80 bg-background p-4 transition hover:border-primary/30 hover:shadow-sm">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Previous</p>
      <p className="mt-2 text-base font-semibold text-foreground">{olderItem.title}</p>
    </Link>
  )}
  {newerItem && (
    <Link href={`/${type}/${newerItem.slug}`} className="rounded-2xl border border-border/80 bg-background p-4 transition hover:border-primary/30 hover:shadow-sm sm:justify-self-end">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Next</p>
      <p className="mt-2 text-base font-semibold text-foreground">{newerItem.title}</p>
    </Link>
  )}
</nav>
```

---

## 7. Language 규칙

| 대상 | 언어 |
|------|------|
| 모든 UI text (heading, label, button, nav) | **영어** |
| Admin-only 라벨 (로그인 필수) | 한국어 허용 |
| RelatedContentList heading | **영어** ("More Posts", "More Works") |
| "Click to view details" | 제거 → 빈 발췌문은 표시하지 않음 |

---

## 8. Dead Code 규칙

- `animationDelay`는 대응하는 `animate-*` 클래스가 있을 때만 사용
- 사용되지 않는 `style` 속성 즉시 제거
- `!important`는 원칙적으로 금지 — 컴포넌트 기본값 수정으로 해결

---

## 9. Accessibility Checklist (Public 페이지)

| 항목 | 기준 | 현재 상태 |
|------|------|----------|
| 색상 대비 (본문) | ≥ 4.5:1 AA | `text-muted-foreground`로 통일 필요 |
| 색상 대비 (대형 텍스트) | ≥ 3:1 AA | ✅ OK |
| Focus ring | 모든 인터랙티브 요소 | ✅ OK |
| Skip link | 메인 콘텐츠 건너뛰기 | ✅ OK |
| 이미지 alt | 모든 의미 있는 이미지 | ✅ OK |
| reduced-motion | `prefers-reduced-motion` 대응 | ✅ OK |
| 프로필 폴백 | `User` 아이콘 (텍스트 "Avatar" 금지) | 수정 필요 |

---

## 10. Quick Reference — 파일별 적용 규칙

| 파일 | 적용할 패턴 |
|------|------------|
| `page.tsx` (Home) | Token 전환, hover 통일, Featured placeholder 통일, Avatar 아이콘, UX 카피 |
| `works/page.tsx` | Token 전환, animationDelay 제거, tag 스타일 통일 |
| `blog/page.tsx` | Token 전환, accent stripe 추가, `xl:grid-cols-3`, `!important` 제거, tag 통일 |
| `blog/[slug]/page.tsx` | Token 전환, heading 영어, card title scale 통일 |
| `works/[slug]/page.tsx` | Token 전환, heading 영어, prev/next 추가 |
| `Footer.tsx` | Token 전환, nav links 추가, 구조 확장 |
| `card.tsx` | 필요 시 variant 추가 (compact) |
