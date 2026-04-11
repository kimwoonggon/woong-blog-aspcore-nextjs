# 포트폴리오 블로그 UI/UX 심층 비판 및 개선 제안

## 1. 웹사이트 특성 이해

이 웹사이트는 **개인 포트폴리오 + 블로그 + 이력서** 하이브리드 사이트입니다:
- **기술 스택**: Next.js App Router + Tailwind CSS + shadcn/ui + ASP.NET 백엔드
- **폰트**: Archivo (heading) + Space Grotesk (body) — 테크 포트폴리오에 적합한 조합
- **컬러 시스템**: oklch 기반 커스텀 브랜드 컬러 (accent-red, navy, cyan, orange)
- **6개 네비게이션**: Home, Introduction, Works, Blog, Contact, Resume
- **타겟 사용자**: 채용 담당자, 기술 동료, 잠재 클라이언트

---

## 2. CRITICAL 문제점 — 시선 동선(Visual Hierarchy)과 메인 페이지

### 2.1 메인 페이지의 가장 심각한 문제: **"무엇을 하는 사람인지" 3초 안에 전달 실패**

현재 page.tsx/page.tsx)의 Hero 섹션:

```
[프로필 이미지(원형 240x240)] ← → [헤드라인 + 소개 텍스트]
```

**비판**:
- **헤드라인 기본값이 `"Hi, I am John, Creative Technologist"`** — 이것은 CMS fallback이지만, 실제 데이터가 이걸 어떻게 대체하는지와 무관하게 **구조적 문제**가 있습니다
- 헤드라인 아래에 **CTA(Call to Action)가 전혀 없습니다**. 소개 텍스트 후 바로 공백 → "Recent posts" 섹션으로 넘어감
- 사용자가 *"이 사람에게 연락해야겠다"* 또는 *"작업물을 봐야겠다"* 라는 다음 행동을 유도하는 장치가 **0개**
- `animate-fade-in-up` 애니메이션이 `opacity: 0`에서 시작 → **LCP(Largest Contentful Paint)에 부정적 영향** + 첫 로드 시 0.8초 동안 텍스트가 안 보임

### 2.2 시선 흐름(F-Pattern/Z-Pattern) 위반

포트폴리오 사이트 방문자의 시선 패턴은 보통 **Z-Pattern**입니다:

```
[로고/이름] ──────→ [네비게이션]
      ↘
   [Hero CTA]  ←───── [비주얼]
      ↓
[대표 작업물] ──→ [블로그]
```

현재 구조의 문제:
1. **Hero에서 CTA 부재**: Z자의 하단 왼쪽 앵커가 비어있음
2. **"Recent posts"가 "Featured works"보다 먼저 배치됨**: 포트폴리오 사이트에서 **작업물이 블로그보다 중요**한데, 시선의 골든 존에 블로그가 위치
3. **Recent posts 섹션의 `bg-brand-section-bg` 배경 구분**: 이 부분만 배경색이 다른데, 이것이 오히려 "Featured works"를 시각적으로 격하시킴

### 2.3 Recent Posts 카드의 문제

```tsx
<Card className="border-none shadow-sm">
```
- `border-none`으로 카드 경계가 사라짐 → **배경 위에 떠있는 느낌 부재**
- 2컬럼 그리드인데 **이미지/썸네일이 전혀 없음** → 텍스트만의 카드가 시각적 관심을 끌지 못함
- "View all" 링크가 너무 작고 `text-sm`으로 존재감 부재

### 2.4 Featured Works 섹션의 문제

- **수평 리스트(border-b 구분선)** 형태 → 카드 기반 그리드보다 스캔 효율이 낮음
- 썸네일이 `h-48 w-full md:w-64`로 고정 → 모바일에서 이미지 비율이 왜곡될 가능성
- **"View all" 링크 없음** → Works 전체 목록으로의 진입점 부재
- 날짜 배지가 `bg-brand-navy` 진한 배경 → 너무 시각적으로 강하면서 실제 중요도는 낮음 (날짜보다 작업 제목이 중요)

---

## 3. 컬러 시스템 비판

### 3.1 브랜드 컬러 일관성 부재

현재 4개의 브랜드 컬러가 있지만 **역할이 불명확**합니다:

| 컬러 | 변수 | 사용처 | 문제 |
|-------|------|--------|------|
| Accent Red | `--brand-accent` | Blog 날짜 배지, hover | Primary와 동일값 — 중복 |
| Navy | `--brand-navy` | Works 날짜 배지 | Blog에선 사용 안 함 — 비일관적 |
| Cyan | `--brand-cyan` | "View all" 링크, Recent posts hover | 한정적 사용 |
| Orange | `--brand-orange` | Work detail 배지/태그 hover | Works 전용 — 비일관적 |

**비판**: 블로그에선 red/cyan, 작업물에선 navy/orange — 같은 사이트인데 **컬러 언어가 분리**됩니다. 이것은 하나의 브랜드가 아니라 두 개의 사이트처럼 느껴질 수 있습니다.

### 3.2 다크 모드 대비(Contrast) 우려

- `--muted-foreground: oklch(0.708 0 0)` (dark) → 밝은 회색 텍스트 on `oklch(0.10 0.02 280)` 배경
- 이것은 약 **4.2:1** 수준으로 WCAG AA의 4.5:1 기준에 **간신히 미달** 가능성
- 날짜/태그 등 보조 텍스트(`text-gray-400 dark:text-gray-500`)는 **확실히 명암비 부족**

---

## 4. 타이포그래피 비판

### 4.1 강점
- `clamp()` 기반 반응형 heading 크기 — 좋은 패턴
- `line-height: 1.8` 본문 — 한국어/영어 혼합에 적합
- 별도 heading/body 폰트 페어링

### 4.2 약점
- **Hero 헤드라인이 `text-4xl md:text-5xl lg:text-6xl`로 점프** — `clamp()` 대신 브레이크포인트별 하드코딩, 중간 사이즈에서 어색할 수 있음
- `text-wrap: balance` 또는 `text-pretty` **미사용** → 헤드라인에서 과부(widow) 발생
- "Recent posts" `text-xl font-medium` vs "Featured works" `text-xl font-bold` → **같은 수준의 섹션 제목인데 weight 불일관**

---

## 5. 인터랙션 & 접근성 비판

### 5.1 Web Interface Guidelines 위반 사항

| 파일 | 위치 | 문제 |
|------|------|------|
| page.tsx/page.tsx) | Hero Image | `alt="Profile"` → 비설명적. 사람 이름을 포함해야 함 |
| page.tsx/page.tsx) | Card links | `<Card>` 내부 `<Link>`가 제목만 감싸고, 카드 전체가 clickable하지 않음 |
| Navbar.tsx | Skip link | **Skip to main content 링크 없음** |
| page.tsx/page.tsx) | Images | `unoptimized` prop → Next.js 이미지 최적화 비활성화 |
| Footer.tsx | 소셜 링크 | `aria-label` 있어 좋으나, `rel="noopener noreferrer"` 잘 설정됨 ✓ |
| globals.css | 애니메이션 | `prefers-reduced-motion` 미지원 → **CRITICAL 접근성 위반** |
| globals.css | 전환 | `transition: all` 패턴 없음 ✓ |
| Navbar.tsx | 모바일 메뉴 | Sheet 컴포넌트 사용 — `overscroll-behavior: contain` 설정 확인 필요 |

### 5.2 `<html>` 태그에 `color-scheme` 미설정
```tsx
<html lang="en" suppressHydrationWarning>
```
다크 모드 사용 중이나 `color-scheme: dark` 미설정 → 스크롤바, `<select>`, `<input>` 등 네이티브 요소가 라이트 모드로 렌더링될 수 있음.

---

## 6. 레이아웃 & 반응형 비판

### 6.1 Navbar 과적재
6개 항목 + "Latest writing" 링크 + 테마 토글 + 세션 버튼 = **총 9개 요소**가 한 줄에.
- "Latest writing"은 Blog과 중복 → 혼란 유발
- 모바일 Sheet 메뉴는 잘 구현됨 ✓

### 6.2 `container` 불일관
- 메인 페이지: `container mx-auto px-4` (max-width 제한 없음)
- 블로그: `container mx-auto max-w-7xl`
- 소개/연락처: `container mx-auto max-w-3xl`
- **페이지마다 콘텐츠 폭이 다름** → 사이트 내 이동 시 콘텐츠 영역이 "흔들리는" 느낌

### 6.3 Works 피드 카드 고정 높이
```css
.works-feed-card { height: 30rem; }
```
고정 높이는 **콘텐츠 길이가 다를 때 하단 빈 공간 또는 잘림** 발생. `min-height` + flex가 더 적합.

---

## 7. 핵심 개선 제안 — 메인 페이지 재구성

### 7.1 Hero 섹션 개선

**현재**: 프로필 + 텍스트만
**제안**: 프로필 + 텍스트 + **듀얼 CTA**

```
┌─────────────────────────────────────────────────────┐
│  [프로필 이미지]                                      │
│                                                       │
│   Hi, I'm Woonggon Kim                               │
│   Creative Technologist                               │
│                                                       │
│   짧은 소개 (2줄 이내)                                │
│                                                       │
│   [ View My Works ]  [ Read Blog →]                  │
│         (Primary)        (Ghost/Outline)              │
└─────────────────────────────────────────────────────┘
```

- **Primary CTA**: Works로 유도 (포트폴리오의 핵심)
- **Secondary CTA**: Blog으로 유도
- `text-wrap: balance` 추가
- `animate-fade-in-up`에 `prefers-reduced-motion` fallback 추가

### 7.2 섹션 순서 재배치

**현재**: Hero → Recent Posts → Featured Works
**제안**: Hero → **Featured Works** → Recent Posts → **(NEW) Skills/Tech Stack 요약**

이유:
- 포트폴리오 사이트의 핵심 가치는 **작업물**
- 채용 담당자는 블로그보다 작업물을 먼저 봄
- 기술 스택 요약 섹션은 **Social Proof** 역할

### 7.3 Featured Works를 카드 그리드로 전환

**현재**: 수평 리스트 + 구분선
**제안**: 2~3컬럼 카드 그리드 (Works 목록 페이지와 동일 패턴)

```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  [썸네일]     │  │  [썸네일]     │  │  [썸네일]     │
│  제목         │  │  제목         │  │  제목         │
│  카테고리     │  │  카테고리     │  │  카테고리     │
│  발췌         │  │  발췌         │  │  발췌         │
└──────────────┘  └──────────────┘  └──────────────┘
            [ View All Works → ]
```

### 7.4 Recent Posts에 비주얼 앵커 추가

- 카드에 **카테고리/태그 컬러 뱃지** 추가
- 첫 번째 포스트를 **Featured Post**로 크게 표시 (2컬럼 스팬)
- "View all" → **"Browse All Posts →"** 로 더 강한 CTA

### 7.5 컬러 시스템 통합

| 역할 | 제안 컬러 | 적용 |
|------|-----------|------|
| Primary Action | `--brand-accent` (red) | CTA, 활성 상태 |
| Content Accent | `--brand-cyan` | 링크, 태그, 하이라이트 |
| Section Bg | `--brand-section-bg` | 섹션 구분 (유지) |
| Muted Info | `--brand-navy` | 날짜 배지 (Works/Blog 통합) |

**Orange는 제거하거나 한정적 사용으로 변경** → 브랜드 컬러를 3개 이하로 줄여 일관성 확보.

---

## 8. 우선순위별 액션 아이템

### P0 (즉시 수정 — 접근성/성능)
1. `prefers-reduced-motion` 미디어 쿼리 추가 (globals.css)
2. `<html>`에 `color-scheme` 속성 동적 반영
3. Skip to main content 링크 추가
4. Hero 이미지 `alt`에 실제 이름 포함
5. 다크 모드 보조 텍스트 명암비 4.5:1 이상 확보

### P1 (높은 기대효과 — 시선 유도)
6. Hero에 듀얼 CTA 버튼 추가
7. **섹션 순서**: Featured Works → Recent Posts로 변경
8. Featured Works를 카드 그리드로 전환 + "View All" 링크 추가
9. 헤드라인에 `text-wrap: balance` 추가

### P2 (디자인 일관성)
10. `container` max-width 통일 (메인도 `max-w-7xl` 권장)
11. 섹션 제목 font-weight 통일 (`font-bold`)
12. 브랜드 컬러 역할 정리 (orange 축소, 3색 체계)
13. Navbar에서 "Latest writing" 중복 링크 제거

### P3 (정교화)
14. Works 카드 고정 높이 → `min-height` + flex
15. Hero 애니메이션을 `opacity: 0` 시작이 아닌 SSR-safe 방식으로 변경 (LCP 개선)
16. Recent Posts 첫 번째 카드를 Featured로 강조
17. 메인 페이지 하단에 기술 스택/툴 뱃지 섹션 추가 고려

--- 

Completed: *Deep analysis and critique report* (6/6)

이 분석은 13개 파일을 읽고, UI/UX Pro Max의 Priority 1~6 룰과 Vercel Web Interface Guidelines를 기준으로 수행했습니다.

**가장 큰 한 마디 요약**: 이 사이트는 기술적 기반(shadcn/ui, oklch 컬러, 반응형 레이아웃)은 견고하지만, **메인 페이지가 방문자에게 "다음에 무엇을 해야 하는지" 알려주지 못합니다**. Hero에 CTA가 없고, 포트폴리오의 핵심인 Works가 두 번째로 밀려 있으며, 브랜드 컬러가 4색으로 분산되어 시각적 일관성을 해치고 있습니다.

구현을 시작할 항목이 있으면 알려주세요.