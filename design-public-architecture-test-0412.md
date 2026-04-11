# 냉정한 디자인 평가 — Public Works / Blog 페이지

> **평가일**: 2026-04-12  
> **대상 브랜치**: `feat/ui-improvement`  
> **평가 기준**: UI/UX Pro Max Skill (Priority 1–10), Vercel Web Interface Guidelines  
> **전체 등급**: **B+** (양호하지만 "프로 수준 포트폴리오"에는 부족)

---

## CRITICAL — 지금 바로 고쳐야 하는 것

### 1. `text-gray-500` (라이트 모드) — 여전히 WCAG FAIL 위험

`dark:text-gray-500`은 다 고쳤지만 **라이트 모드의 `text-gray-500`도 문제**다. `#6b7280` on `#fafafa` = **약 4.6:1**로 body text AA(4.5:1)를 겨우 통과하지만 **Works 카드 category 텍스트**(`src/app/(public)/works/page.tsx:120`)는 `text-xs` (12px 미만)이므로 **large text가 아닌 small text**에 해당하고, `text-gray-500`으로는 빠듯하다. 특히 `bg-background` (oklch 0.98)가 아닌 `bg-card` (oklch 1.0 = 순수 흰색) 위에 있을 때 더 밝아 보인다.

**제안**: 라이트 모드 보조 텍스트는 `text-gray-600` (= `#4b5563`, ~7:1)이 이미 다른 곳에서 쓰이고 있으니 통일하라. 또는 semantic token `text-muted-foreground`를 일관되게 쓰라.

**해당 파일**:
- `src/app/(public)/works/page.tsx:120` — category
- `src/app/(public)/page.tsx:129` — Featured Works category
- `src/app/(public)/blog/[slug]/page.tsx:84` — metadata wrapper
- `src/app/(public)/works/[slug]/page.tsx:75,85` — category, tags

### 2. 하드코딩된 색상 난무 — Design Token을 무시하는 코드

Vercel Guidelines와 UI/UX Pro Max 규칙 `color-semantic`에서 가장 크게 위반하는 부분:

```
text-gray-900 dark:text-gray-50  ← 제목 색상이 14곳에 하드코딩
text-gray-600 dark:text-gray-300  ← 본문 색상이 8곳에 하드코딩
text-gray-500 dark:text-gray-400  ← 보조 텍스트가 10곳에 하드코딩
bg-gray-100 dark:bg-gray-800     ← placeholder 배경이 5곳에 하드코딩
```

`--foreground`, `--muted-foreground`, `--muted` 등 **이미 정의된 semantic token이 있는데 raw Tailwind color를 직접 쓰고 있다.** 나중에 브랜드 컬러를 바꾸거나 다크 모드를 미세 조정하면 **14곳 이상을 일일이 수정해야 한다.**

| 현재 코드 | 써야 하는 토큰 |
|---|---|
| `text-gray-900 dark:text-gray-50` | `text-foreground` |
| `text-gray-600 dark:text-gray-300` | `text-foreground/80` 또는 새 토큰 |
| `text-gray-500 dark:text-gray-400` | `text-muted-foreground` |
| `bg-gray-100 dark:bg-gray-800` | `bg-muted` |

이건 "있으면 좋은" 수준이 아니라, 포트폴리오로 보여줄 코드 품질의 문제다.

---

## HIGH — 사용자 경험에 직접적 영향

### 3. Blog 카드 — 시각적 비대칭이 심하다

Works 카드는 **썸네일(시각 앵커) + Badge(날짜) + 카테고리 + 제목 + 발췌 + 태그** 구조로 풍부하다. 반면 Blog 카드는:

- 썸네일 **없음**
- Badge는 추가했지만 여전히 **텍스트만으로 구성**
- 데스크탑 `xl:grid-cols-4`로 12개 카드가 나열되면 → **텍스트 벽(Wall of Text)**

**문제**: Portfolio 사이트를 방문하는 사람은 시각적 사람이다. 글 목록이 모두 텍스트 카드면 **"이 사람은 디자인에 신경 안 쓰는구나"**라는 인상을 준다.

**제안**:
- Blog 카드에 최소한 **컬러 스트라이프**(상단 4px accent bar) 또는 **카테고리별 아이콘** 추가
- 또는 첫 번째(또는 pinned) 포스트를 **hero card** (2열 차지, 큰 발췌) 형태로 차별화
- `xl:grid-cols-4`는 과하다. 카드 내용이 텍스트 only인데 4열이면 각 카드가 너무 좁아서 `line-clamp-2`가 제목 1줄도 다 못 보여줄 수 있다. **`xl:grid-cols-3`이 적절**

**해당 파일**: `src/app/(public)/blog/page.tsx:86`

### 4. Works 카드 — `animationDelay` 잔재

`src/app/(public)/works/page.tsx:97`에 `style={{ animationDelay: ... }}` 존재하지만 `animate-fade-in-up` 클래스가 **없다**. 이 `animationDelay`는 아무 효과도 없는 dead code다. 제거하라.

**해당 파일**: `src/app/(public)/works/page.tsx:97`

### 5. Featured Works "No Image" 폴백 불일치

- `src/app/(public)/works/page.tsx:111-117`: gradient + `BriefcaseBusiness` 아이콘 ✅
- `src/app/(public)/page.tsx:119-121` (Featured Works on Home): 단순 `"No Image"` 텍스트만 ❌

**같은 콘텐츠(Works)인데 메인 페이지와 Works 페이지에서 빈 이미지 처리가 다르다.** 방문자가 메인 → Works로 이동하면 시각적 불연속을 느낀다.

**해당 파일**: `src/app/(public)/page.tsx:119-121`

### 6. Recent Posts 섹션 — 카드 내부 구조가 Works와 완전히 다름

| 요소 | Featured Works 카드 | Recent Posts 카드 |
|---|---|---|
| 이미지 | `aspect-[4/3]` 썸네일 | 없음 |
| 날짜 | `bg-brand-navy` badge | plain text |
| 태그 | category label | `bg-muted` badge |
| 제목 위치 | CardContent 안 | CardHeader 안 (CardTitle) |
| padding | `p-4 sm:p-5` | shadcn default |
| hover | `group-hover:text-brand-accent` | `hover:text-brand-cyan` |

**hover 색상조차 통일되어 있지 않다** (`brand-accent` vs `brand-cyan`). 같은 페이지에 있는 두 섹션의 디자인 언어가 다르면 "기획된 디자인"이 아니라 "대충 갖다 붙인 느낌"을 준다.

**해당 파일**: `src/app/(public)/page.tsx:151-204`

### 7. Works 상세 — prev/next 네비게이션 없음

Blog 상세에는 `blog-prev-next` 네비게이션이 있는데 Works 상세에는 **없다**. 포트폴리오 사이트에서 작업물 간 탐색이 블로그보다 중요한데 이것이 빠져있다.

**해당 파일**: `src/app/(public)/works/[slug]/page.tsx`

---

## MEDIUM — 품질 인상에 영향

### 8. 타이포그래피 스케일 불일관

| 위치 | 제목 크기 |
|---|---|
| Home hero h1 | `text-4xl md:text-5xl lg:text-6xl` |
| Works page h1 | `text-3xl md:text-4xl` |
| Blog page h1 | `text-3xl md:text-4xl` |
| Blog detail h1 | `text-3xl md:text-4xl` |
| Works detail h1 | `text-3xl md:text-4xl` |
| Section h2 (Home) | `text-xl md:text-2xl` |

Hero는 크고 나머지는 같은 건 괜찮다. 그런데 **카드 제목의 크기가 파일마다 다르다**:

- Blog 카드: `text-lg sm:text-xl md:text-2xl` (반응형 3단계)
- Works 카드: `text-lg sm:text-xl` (2단계)
- Featured Works 카드: `text-lg` (고정)

동일 계층(피드 카드 제목)인데 스케일이 다르다.

### 9. Footer 디자인 — 너무 단순

```tsx
<footer className="w-full bg-white py-8 dark:bg-gray-950">
```

- `bg-white` 하드코딩 (토큰 미사용)
- 아이콘 + 코피라이트 한 줄이 전부
- **사이트 내비게이션 링크가 Footer에 없다** — SEO와 사용성 모두 손해
- 포트폴리오 사이트 Footer에 최소한 Works / Blog / Contact 링크, 이메일, 간략한 한 줄 소개 정도는 있어야 한다

**해당 파일**: `src/components/layout/Footer.tsx`

### 10. `brand-accent` hover가 일관되지 않음

- Works 카드 제목 hover: `group-hover:text-brand-accent` (빨간색)
- Blog 카드 제목 hover: `group-hover/card:text-brand-accent` (빨간색) ✅ 일치
- Recent Posts 제목 hover: `hover:text-brand-cyan` (파란색) ❌ **불일치**
- "View all" 링크: `text-brand-cyan hover:text-brand-cyan` (파란색)

**카드 제목 hover = accent, 링크 hover = cyan이라는 규칙인가?** Recent Posts의 제목은 `<Link>` 안에 있어서 링크처럼 동작하지만, 카드 전체가 클릭 가능한 Works 카드에서도 제목이 accent로 바뀐다. **통일하거나 명확한 체계를 만들어라.**

**해당 파일**: `src/app/(public)/page.tsx:186`

### 11. Works 카드 태그 스타일 — Blog과 다름

- Works 카드 태그: `rounded bg-gray-100 px-2 py-0.5 text-[10px]` (각진 모서리, raw 색상)
- Blog 카드 태그: `rounded-full bg-muted px-2 py-0.5 text-xs` (완전 라운드, 토큰 색상)

같은 역할인데 반경(`rounded` vs `rounded-full`), 사이즈(`text-[10px]` vs `text-xs`), 색상(`bg-gray-100` vs `bg-muted`)이 모두 다르다.

**해당 파일**:
- `src/app/(public)/works/page.tsx:133` — Works 태그
- `src/app/(public)/blog/page.tsx:103` — Blog 태그

### 12. `!important`와 `!py-0 !gap-0` — 코드 스멜

`src/app/(public)/blog/page.tsx:96`:
```tsx
<Card className="responsive-feed-card !gap-0 !py-0 flex h-full ...">
```

shadcn Card의 기본 padding을 `!important`로 덮어쓰고 있다. 이건 **Card 컴포넌트의 기본값이 이 프로젝트에 맞지 않다는 의미**이므로, Card 컴포넌트 자체의 기본값을 수정하는 게 맞다. `!important` 남용은 유지보수의 적이다.

**해당 파일**: `src/app/(public)/blog/page.tsx:96`

---

## LOW — 디테일

### 13. Blog 상세 — "다른 게시물" heading이 한국어

```tsx
<RelatedContentList heading="다른 게시물" ... />
<RelatedContentList heading="다른 작업" ... />
```

사이트 전체가 영어(Works, Blog, View all, Recent posts)인데 여기만 한국어. **언어 혼용**은 비전문적으로 보인다.

**해당 파일**:
- `src/app/(public)/blog/[slug]/page.tsx` — `heading="다른 게시물"`
- `src/app/(public)/works/[slug]/page.tsx` — `heading="다른 작업"`

### 14. `'Click to view details'` — 나쁜 UX 카피

`src/app/(public)/page.tsx:137`:
```tsx
{work.excerpt || 'Click to view details'}
```

Vercel Guidelines의 Content & Copy 규칙 "Specific button labels" 위반. 발췌문이 없으면 비워두거나 `"No description"` 정도가 적절. "Click to view details"는 2010년대 웹사이트 느낌이다.

### 15. 프로필 이미지 없을 때 "Avatar" 텍스트

```tsx
<div role="img" aria-label={headline} className="...">Avatar</div>
```

`aria-label`은 잘 설정되어 있지만, 시각적으로 **"Avatar"라는 문자가 보인다**. 대신 사용자 이니셜이나 placeholder 아이콘(lucide `User`)을 쓰는 것이 프로페셔널하다.

**해당 파일**: `src/app/(public)/page.tsx:80-84`

---

## 잘 된 점 (인정)

1. **접근성 기초**: skip link, focus-visible, scroll-margin-top, reduced-motion, color-scheme 모두 구현
2. **touch-action: manipulation + tap-highlight**: 모바일 UX 기본기 OK
3. **반응형 pageSize 동기화**: 디바이스별 `pageSize` 자동 조절은 좋은 UX 패턴
4. **TOC 구현**: IntersectionObserver 기반 활성 헤딩 추적 — 기술적으로 견고
5. **비디오 접기 패턴**: `<details>` 활용한 progressive disclosure — 정확한 판단
6. **QA testing 인프라**: `__qaEmpty`, `__qaNoImage`, `__qaTagged` 파라미터 — 테스트 가능한 설계

---

## 실행 우선순위

| 우선순위 | 작업 | 이유 | 난이도 |
|---|---|---|---|
| **1** | raw color → semantic token 전환 | 코드 품질이 눈에 보이는 포트폴리오에서 하드코딩은 치명적 | M |
| **2** | hover 색상 체계 통일 | accent vs cyan 혼용은 "체계 없음" 신호 | S |
| **3** | Blog 카드 시각 앵커 강화 | 4열 텍스트 벽은 포트폴리오 킬러 | M |
| **4** | Featured Works "No Image" 폴백 통일 | 같은 콘텐츠, 다른 표현 = 완성도 의심 | S |
| **5** | Works 상세에 prev/next 추가 | Blog과의 기능 대칭 | M |
| **6** | Footer에 네비게이션 추가 | SEO + 전문성 | M |
| **7** | 태그 스타일 통일 | 디자인 시스템의 신뢰도 | S |
| **8** | 언어 혼용 정리 | 세부 완성도 | S |
| **9** | Works 카드 animationDelay 잔재 제거 | dead code | S |
| **10** | "Click to view details" 카피 수정 | 디테일 | S |
| **11** | Avatar 폴백 아이콘 전환 | 디테일 | S |
| **12** | Blog 카드 `!important` 제거 | 코드 품질 | S |
| **13** | 카드 제목 타이포 스케일 통일 | 디자인 시스템 | S |

---

## 최종 한 줄

기술적 접근성과 반응형은 평균 이상이지만, **디자인 시스템의 일관성**이 부족해서 "개발자가 만든 포트폴리오"라는 인상에서 "프로 수준의 포트폴리오"로 넘어가지 못하고 있다. 위 13개를 고치면 확실히 넘어간다.
