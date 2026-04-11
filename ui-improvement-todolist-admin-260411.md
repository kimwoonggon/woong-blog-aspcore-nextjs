# UI/UX 개선 TODO — Admin (관리 패널 / Notion View / 에디터) 편

> **목적**: 관리자 패널의 UI/UX 비판을 기반으로 한 실행 계획  
> **대상 브랜치**: `feat/ui-improvement`  
> **작성일**: 2026-04-11  
> **기술 스택**: Next.js 14+ App Router, Tailwind CSS v4, shadcn/ui, Tiptap, Playwright  
> **Playwright 설정**: `video: 'on'`, `screenshot: 'on'` (이미 설정됨 — `playwright.config.ts`)  
> **범위**: `src/app/admin/`, `src/components/admin/`, `src/components/admin/tiptap-editor/`

---

## 실행 규칙

1. 각 TODO 항목은 **반드시 Playwright 테스트를 작성**하고, 테스트 실행 시 **영상이 자동 녹화**된다 (`test-results/playwright/`).
2. 테스트 파일명: `tests/ui-admin-*.spec.ts` 네이밍 컨벤션.
3. Admin 테스트는 `chromium-authenticated` 프로젝트 기준 (인증 상태).
4. 기존 테스트(`tests/admin-*.spec.ts` 등)가 **깨지면 안 된다**.
5. 모든 변경은 **라이트 모드 + 다크 모드** 양쪽 확인.
6. 변경 후 반드시 `npm run build` 성공 확인.
7. 각 Phase를 **순서대로** 진행한다.

---

## Phase 0: Admin 사이드바 개선

> 사이드바는 모든 Admin 페이지에 영향 — 먼저 고친다.

---

### ADM-0-1: 사이드바 너비 축소 (`w-80` → `w-64`)

- [ ] 완료

**변경 파일**: `src/app/admin/layout.tsx`

**현재 문제**:
`w-80`(320px)은 사이드바에 과도하게 넓다. 메인 콘텐츠 영역이 1280px 모니터에서 960px밖에 안 남는다. 경쟁 대시보드(Vercel, Linear, Notion)는 240–256px이 표준.

**변경 내용**:
```tsx
{/* 기존 */}
<aside className="w-full border-b border-gray-200 bg-white p-6 md:w-80 md:border-b-0 md:border-r dark:border-gray-800 dark:bg-gray-950">
{/* 변경 */}
<aside className="w-full border-b border-gray-200 bg-white p-4 md:w-64 md:border-b-0 md:border-r dark:border-gray-800 dark:bg-gray-950">
```
- `md:w-80` → `md:w-64` (320px → 256px)
- `p-6` → `p-4` (padding 축소하여 실제 가용 폭 확보)

**테스트 계획** — `tests/ui-admin-sidebar-width.spec.ts`:
```
테스트 1: "사이드바 폭이 256px(w-64)"
  - /admin/dashboard 이동 (viewport 1920×1080)
  - aside 요소의 rendered width === 256 확인

테스트 2: "메인 콘텐츠 영역 폭이 1280px 모니터에서 ≥ 960px"
  - viewport 1280×900
  - main 요소의 rendered width ≥ 960 확인
```

---

### ADM-0-2: 사이드바 네비게이션에 Active State 추가

- [ ] 완료

**변경 파일**: `src/app/admin/layout.tsx`

**현재 문제**:
모든 nav 항목이 `variant="ghost"` — 현재 페이지를 식별할 수 없다. Nielsen Heuristic #1 "Visibility of system status" 위반.

**변경 내용**:
1. `layout.tsx`를 Server Component로 유지하면서, 현재 경로를 활용하기 위해 **nav 부분만 Client Component로 분리**:

`src/components/admin/AdminSidebarNav.tsx` — **신규 생성**:
```tsx
"use client"
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { Briefcase, FileText, LayoutDashboard, Settings, Users } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const navItems = [
  { href: '/admin/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/admin/works', label: 'Works', icon: Briefcase },
  { href: '/admin/blog', label: 'Blog', icon: FileText },
  { href: '/admin/blog/notion', label: 'Blog Notion View', icon: FileText },
  { href: '/admin/pages', label: 'Pages & Settings', icon: Settings },
  { href: '/admin/members', label: 'Members', icon: Users },
]

export function AdminSidebarNav() {
  const pathname = usePathname()

  return (
    <nav className="flex flex-col gap-1">
      {navItems.map(({ href, label, icon: Icon }) => {
        const isActive = pathname === href || (href !== '/admin/dashboard' && pathname.startsWith(href))
        return (
          <Link key={href} href={href}>
            <Button
              variant={isActive ? 'secondary' : 'ghost'}
              className={cn(
                'w-full justify-start gap-2 rounded-xl px-3 py-5',
                isActive && 'bg-accent font-semibold text-accent-foreground'
              )}
            >
              <Icon size={18} />
              {label}
            </Button>
          </Link>
        )
      })}
    </nav>
  )
}
```

2. `src/app/admin/layout.tsx`에서 기존 `<nav>...</nav>` 전체를 `<AdminSidebarNav />`로 교체.

**테스트 계획** — `tests/ui-admin-sidebar-active.spec.ts`:
```
테스트 1: "/admin/dashboard에서 Dashboard 항목이 active 스타일"
  - /admin/dashboard 이동
  - 'Dashboard' 버튼에 'bg-accent' (또는 secondary variant 스타일) 확인

테스트 2: "/admin/blog에서 Blog 항목이 active 스타일"
  - /admin/blog 이동
  - 'Blog' 버튼에 active 스타일 확인

테스트 3: "non-active 항목은 ghost 스타일"
  - /admin/dashboard 이동
  - 'Works' 버튼에 active 스타일이 없는 것 확인
```

---

### ADM-0-3: "Public Home"과 "Open Site" 중복 링크 정리

- [ ] 완료

**변경 파일**: `src/app/admin/layout.tsx`

**현재 문제**:
"Public Home" → `/` (같은 탭)과 "Open Site" → `/` (`target="_blank"`)가 병렬 표시. 둘 다 `/`로 이동하여 중복.

**변경 내용**:
두 개를 하나로 통합:
```tsx
<div className="mb-6">
  <Link href="/" target="_blank">
    <Button variant="outline" size="sm" className="gap-2">
      <ArrowUpRight size={14} />
      View Site
    </Button>
  </Link>
</div>
```

**테스트 계획** — `tests/ui-admin-sidebar-links.spec.ts`:
```
테스트 1: "사이드바에 'View Site' 링크 1개만 존재"
  - /admin/dashboard 이동
  - aside 내 'View Site' 또는 'Public Home' 또는 'Open Site' 관련 링크가 정확히 1개

테스트 2: "View Site가 새 탭으로 열림"
  - 해당 링크의 target === '_blank' 확인
```

---

### ADM-0-4: 사이드바 하드코딩 색상 → 시맨틱 토큰으로 교체

- [ ] 완료

**변경 파일**: `src/app/admin/layout.tsx`

**현재 문제**:
`bg-gray-50 dark:bg-gray-900` (래퍼), `bg-white dark:bg-gray-950` (사이드바), `border-gray-200 dark:border-gray-800` — 모두 하드코딩. 테마 변경 시 일일이 수정해야 한다. Tailwind v4 시맨틱 토큰 미활용.

**변경 내용**:
```tsx
{/* 기존 */}
<div className="flex min-h-screen flex-col bg-gray-50 md:flex-row dark:bg-gray-900">
  <aside className="w-full border-b border-gray-200 bg-white p-4 md:w-64 md:border-b-0 md:border-r dark:border-gray-800 dark:bg-gray-950">
  ...
  <main className="flex-1 bg-gray-50 p-6 md:p-12 dark:bg-gray-900">

{/* 변경 */}
<div className="flex min-h-screen flex-col bg-muted/30 md:flex-row">
  <aside className="w-full border-b border-border bg-background p-4 md:w-64 md:border-b-0 md:border-r">
  ...
  <main className="flex-1 bg-muted/30 p-6 md:p-12">
```
- `bg-gray-50 dark:bg-gray-900` → `bg-muted/30`
- `bg-white dark:bg-gray-950` → `bg-background`
- `border-gray-200 dark:border-gray-800` → `border-border`

**테스트 계획** — `tests/ui-admin-semantic-colors.spec.ts`:
```
테스트 1: "다크 모드 전환 후 사이드바 배경이 semantic background 색상"
  - /admin/dashboard 이동 (다크 모드)
  - aside background-color가 CSS variable --background와 일치 확인
```

---

### ADM-0-5: 사이드바 설명 텍스트 간소화

- [ ] 완료

**변경 파일**: `src/app/admin/layout.tsx`

**현재 문제**:
```
"Modernized shortcuts keep the public site, list views, and blog Notion workspace within one click."
```
이 설명 텍스트는 관리자에게 불필요한 마케팅 문구. 사이드바 공간 낭비.

**변경 내용**:
전체 `<p className="mt-2 text-sm text-muted-foreground">Modernized shortcuts...</p>` 블록 삭제.

제목 영역도 간소화:
```tsx
<div className="mb-6">
  <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Admin</p>
  <h1 className="mt-1 text-lg font-semibold">{/* 사용자 이름 또는 "Dashboard" */}</h1>
</div>
```

**테스트 계획** — `tests/ui-admin-sidebar-text.spec.ts`:
```
테스트 1: "'Modernized shortcuts' 텍스트 미존재"
  - /admin/dashboard 이동
  - page.getByText('Modernized shortcuts') → toHaveCount(0)
```

---

## Phase 1: Notion View 개선

> Notion View(`BlogNotionWorkspace.tsx`)는 핵심 편집 UX. 더블 사이드바 문제가 가장 심각함.

---

### ADM-1-1: Notion View 더블 사이드바 문제 해결 — Blog Library를 Sheet로 전환

- [ ] 완료

**변경 파일**: `src/components/admin/BlogNotionWorkspace.tsx`

**현재 문제**:
Admin 사이드바(개선 후 256px) + Blog Library 사이드바(320px) = 576px가 항상 표시. 1280px 모니터에서 에디터에 704px만 남음. 1024px 태블릿에서는 에디터 공간 448px → 사용 불가.

**변경 내용**:
Blog Library를 **항상 표시하는 사이드 패널**에서 **Sheet(서랍)** 패턴으로 전환:

1. 좌측 상단에 "Library" 토글 버튼 추가 (`<Sheet>` 컴포넌트 활용)
2. `grid lg:grid-cols-[320px_minmax(0,1fr)]` → 단순 `<div className="flex-1">`
3. Sheet 내부에 기존 Blog Library 컨텐츠 배치
4. 에디터가 **전체 폭** 사용 가능

변경 후 구조:
```tsx
<div className="flex flex-col h-full">
  {/* 상단 툴바 */}
  <div className="flex items-center gap-3 border-b border-border px-4 py-3">
    <Sheet>
      <SheetTrigger asChild>
        <Button variant="outline" size="sm" className="gap-2">
          <FileText size={16} />
          Library
        </Button>
      </SheetTrigger>
      <SheetContent side="left" className="w-80 p-0">
        {/* 기존 Blog Library 내용 전체 이동 */}
      </SheetContent>
    </Sheet>
    <span className="text-sm font-medium text-muted-foreground">{currentDocTitle || '새 글 작성'}</span>
  </div>

  {/* 에디터 영역 — 전체 폭 */}
  <div className="flex-1 overflow-y-auto">
    {selectedBlogId ? (
      <div className="mx-auto max-w-4xl">
        {/* 기존 에디터 + 문서정보 영역 */}
      </div>
    ) : (
      <EmptyState />
    )}
  </div>
</div>
```

`Sheet`, `SheetContent`, `SheetTrigger` import 필요: `import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet'`

**테스트 계획** — `tests/ui-admin-notion-library-sheet.spec.ts`:
```
테스트 1: "Notion View 초기상태에서 Library 패널이 자동 표시되지 않음"
  - /admin/blog/notion 이동
  - 왼쪽에 320px 사이드바가 자동으로 보이지 않는지 확인

테스트 2: "Library 버튼 클릭 → Sheet 오픈 → 문서 목록 표시"
  - 'Library' 버튼 클릭
  - Sheet 내에 blog 목록이 보이는지 확인

테스트 3: "문서 선택 → Sheet 닫힘 → 에디터 표시"
  - Sheet에서 문서 클릭
  - Sheet가 닫히는지 확인
  - 에디터 영역에 선택한 문서 로드 확인

테스트 4: "에디터 영역 폭이 viewport의 80% 이상"
  - viewport 1280×900
  - 에디터 컨테이너 width ≥ 1024 (256px sidebar 제외 나머지) 확인
```

---

### ADM-1-2: Notion View 문서 전환을 Client-Side로 변경

- [ ] 완료

**변경 파일**: `src/components/admin/BlogNotionWorkspace.tsx`

**현재 문제**:
문서를 클릭할 때 `<Link href="/admin/blog/notion?docId=...">`로 **서버 라운드트립**이 발생한다. 에디터가 매번 재마운트되어 TiptapEditor가 초기화되고 UX가 매우 느리다.

**변경 내용**:
1. `<Link href="...">` → `<button onClick={() => setSelectedBlogId(id)}>` 또는 `router.push` 대신 **state만 변경**
2. `selectedBlogId`를 URL search params에서도 읽되, 전환은 `setState`로만 처리 → `router.replace(url, { scroll: false })` 사용하여 URL만 업데이트 (브라우저 새로고침 없이)

현재 코드 패턴 (추정):
```tsx
// 기존: <Link href={`/admin/blog/notion?docId=${blog.id}`}> ...
// → 서버 컴포넌트 재평가 → 전체 리렌더

// 변경: Client-side state
const [activeBlogId, setActiveBlogId] = useState<string | null>(initialDocId)

const handleDocSelect = (id: string) => {
  setActiveBlogId(id)
  window.history.replaceState(null, '', `/admin/blog/notion?docId=${id}`)
}
```

3. `BlogEditor`는 이미 Client Component이므로 `activeBlogId` prop 변경만으로 데이터를 다시 fetch하면 된다.

**테스트 계획** — `tests/ui-admin-notion-client-switch.spec.ts`:
```
테스트 1: "문서 전환 시 페이지 새로고침 없음"
  - /admin/blog/notion 이동
  - Library에서 문서 A 선택 → 에디터 로드
  - page.evaluate(() => performance.getEntriesByType('navigation').length) → 1 (초기 로드만)
  - Library에서 문서 B 선택
  - 위 navigation 개수 여전히 1 확인 (새 navigation 없음)
  - URL에 docId가 업데이트된 것 확인

테스트 2: "브라우저 새로고침 → 마지막 선택 문서 유지"
  - 문서 선택 → URL에 docId 포함 확인
  - page.reload()
  - 동일 문서가 에디터에 로드됨 확인
```

---

### ADM-1-3: Blog Library에 검색 기능 추가

- [ ] 완료

**변경 파일**: `src/components/admin/BlogNotionWorkspace.tsx`

**현재 문제**:
문서가 많아지면 스크롤로만 찾아야 한다. 제목 기반 검색이 없다. Notion 본가의 Quick Find(Cmd+K)와 비교하면 현저히 부족.

**변경 내용**:
Sheet Library 상단에 검색 입력 추가:
```tsx
<div className="sticky top-0 z-10 border-b border-border bg-background/95 p-3 backdrop-blur-sm">
  <Input
    placeholder="Search posts..."
    value={librarySearch}
    onChange={(e) => setLibrarySearch(e.target.value)}
    className="h-8 text-sm"
  />
</div>
```

문서 목록을 `librarySearch`로 filter:
```tsx
const filteredBlogs = blogs.filter(b =>
  b.title.toLowerCase().includes(librarySearch.toLowerCase())
)
```

**테스트 계획** — `tests/ui-admin-notion-library-search.spec.ts`:
```
테스트 1: "Library 검색으로 문서 필터링"
  - /admin/blog/notion 이동 → Library 오픈
  - 검색 input에 특정 키워드 입력
  - 표시되는 문서 수 감소 확인

테스트 2: "검색어 비우면 전체 목록 복원"
  - 검색 input 비움 → 원래 문서 수 복원 확인
```

---

### ADM-1-4: Capability Hint 닫기 가능하게 변경

- [ ] 완료

**변경 파일**: `src/components/admin/BlogNotionWorkspace.tsx`

**현재 문제**:
`<p className="text-sm italic text-amber-600 dark:text-amber-400">\n 💡 Blog Notion Workspace에서는 ...\n</p>` — 한 번 읽으면 더 이상 필요 없는데 항상 표시된다. 에디터 공간 침해.

**변경 내용**:
1. `localStorage`에 `notionCapabilityHintDismissed` 키로 닫기 상태 저장
2. 닫기 버튼(X) 추가
3. 한 번 닫으면 다시 표시하지 않음

```tsx
const [showCapabilityHint, setShowCapabilityHint] = useState(() => {
  if (typeof window === 'undefined') return true
  return localStorage.getItem('notionCapabilityHintDismissed') !== 'true'
})

{showCapabilityHint && (
  <div className="flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-700 dark:border-amber-800 dark:bg-amber-950/30 dark:text-amber-400">
    <span className="flex-1">💡 Blog Notion Workspace에서는 ...</span>
    <button
      onClick={() => {
        setShowCapabilityHint(false)
        localStorage.setItem('notionCapabilityHintDismissed', 'true')
      }}
      className="rounded p-0.5 hover:bg-amber-200/50 dark:hover:bg-amber-800/50"
      aria-label="Close hint"
    >
      <X size={14} />
    </button>
  </div>
)}
```

**테스트 계획** — `tests/ui-admin-notion-hint-dismiss.spec.ts`:
```
테스트 1: "Capability hint 닫기 → 새로고침해도 안 보임"
  - /admin/blog/notion 이동
  - hint 영역 visible 확인
  - close 버튼 클릭
  - hint 영역 not visible 확인
  - 페이지 새로고침
  - hint 여전히 not visible 확인

테스트 2: "localStorage 초기화 → hint 다시 표시"
  - localStorage에서 키 삭제
  - 페이지 새로고침
  - hint 다시 visible 확인
```

---

### ADM-1-5: Cmd+S / Ctrl+S 키보드 단축키로 저장

- [ ] 완료

**변경 파일**: `src/components/admin/BlogEditor.tsx`

**현재 문제**:
에디터에서 저장은 하단 Save 버튼 클릭만 가능. 텍스트 에디터 사용자의 가장 기본적인 근육 기억(Cmd+S)을 무시.

**변경 내용**:
`BlogEditor` 컴포넌트에 `useEffect`로 키보드 단축키 등록:
```tsx
useEffect(() => {
  const handleKeyDown = (e: KeyboardEvent) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 's') {
      e.preventDefault()
      handleSave() // 기존 save 함수 호출
    }
  }
  document.addEventListener('keydown', handleKeyDown)
  return () => document.removeEventListener('keydown', handleKeyDown)
}, [handleSave])
```

`handleSave`를 `useCallback`으로 래핑하여 의존성 안정화.

동일한 단축키를 `WorkEditor.tsx`에도 적용.

**테스트 계획** — `tests/ui-admin-editor-keyboard-save.spec.ts`:
```
테스트 1: "Cmd+S로 블로그 저장 트리거"
  - /admin/blog/notion 이동 → 문서 선택
  - 에디터 수정
  - page.keyboard.press('Meta+s') (Mac) 또는 'Control+s' (Linux/Win)
  - 저장 완료 toast 또는 상태 변경 확인

테스트 2: "브라우저 저장 대화상자가 뜨지 않음"
  - Cmd+S 후 파일 저장 다이얼로그가 나타나지 않는 것을 확인 (e.preventDefault 동작)
```

---

### ADM-1-6: 문서 정보 패널을 접을 수 있게 변경

- [ ] 완료

**변경 파일**: `src/components/admin/BlogNotionWorkspace.tsx`

**현재 문제**:
에디터 옆 Doc Information 패널(`xl:grid-cols-[minmax(0,1fr)_260px]`)이 항상 표시. 편집에 집중할 때 불필요한 공간 소비.

**변경 내용**:
1. Doc Info 패널을 토글 가능하게 변경
2. 에디터 상단 툴바에 "Info" 토글 버튼 추가
3. 닫으면 에디터가 전체 폭 사용

```tsx
const [showDocInfo, setShowDocInfo] = useState(true)

// 에디터 영역
<div className={cn(
  'grid gap-4',
  showDocInfo ? 'xl:grid-cols-[minmax(0,1fr)_260px]' : ''
)}>
  <div>{/* 에디터 */}</div>
  {showDocInfo && <aside>{/* Doc Info */}</aside>}
</div>
```

**테스트 계획** — `tests/ui-admin-notion-doc-info-toggle.spec.ts`:
```
테스트 1: "Doc Info 토글 off → 에디터 폭 확장"
  - viewport 1920×1080
  - 문서 선택 → Info 토글 off
  - 에디터 영역 폭이 Doc Info 닫기 전보다 넓은지 확인

테스트 2: "Doc Info 토글 on → 패널 재표시"
  - Info 토글 on
  - Doc Info 패널 visible 확인
```

---

## Phase 2: Blog / Works Editor 개선

---

### ADM-2-1: BlogEditor에 excerpt(발췌문) 필드 추가

- [ ] 완료

**변경 파일**: `src/components/admin/BlogEditor.tsx`

**현재 문제**:
발췌문(excerpt)이 에디터에 없어서 블로그 목록에서 본문 일부가 잘려서 표시된다. SEO의 `meta description`에도 영향.

**변경 내용**:
Title 입력 필드 아래, Tags 위에 Textarea 추가:
```tsx
<div className="space-y-2">
  <Label htmlFor="excerpt">Excerpt</Label>
  <Textarea
    id="excerpt"
    placeholder="A brief summary of the post (used in previews and SEO)..."
    value={excerpt}
    onChange={(e) => setExcerpt(e.target.value)}
    className="resize-none"
    rows={2}
    maxLength={200}
  />
  <p className="text-xs text-muted-foreground">{excerpt.length}/200</p>
</div>
```

API 호출에 `excerpt` 필드 포함 (백엔드에 해당 필드가 이미 있다고 가정).

**테스트 계획** — `tests/ui-admin-blog-excerpt.spec.ts`:
```
테스트 1: "BlogEditor에 excerpt 필드 존재"
  - 블로그 편집 페이지 이동
  - label 'Excerpt' + textarea 존재 확인

테스트 2: "글자 수 카운터 작동"
  - textarea에 50자 입력
  - '50/200' 텍스트 표시 확인
```

---

### ADM-2-2: Published 체크박스 위치 변경 — Content 섹션에서 상단 Visibility로

- [ ] 완료

**변경 파일**: `src/components/admin/BlogEditor.tsx`

**현재 문제**:
Published 체크박스가 Content 섹션 내부에 있어 에디터 본문과 섞인다. Publish 상태는 **메타데이터**이지 콘텐츠가 아니다.

**변경 내용**:
Published 체크박스를 Tags 필드 옆 또는 바로 아래 Visibility 영역으로 이동:
```tsx
<div className="flex items-center gap-6">
  <div className="space-y-2 flex-1">
    <Label htmlFor="tags">Tags</Label>
    {/* ... tag input */}
  </div>
  <div className="flex items-center gap-2 pt-6">
    <Checkbox
      id="published"
      checked={isPublished}
      onCheckedChange={(checked) => setIsPublished(!!checked)}
    />
    <Label htmlFor="published" className="text-sm font-medium">Published</Label>
  </div>
</div>
```

**테스트 계획** — `tests/ui-admin-blog-published-position.spec.ts`:
```
테스트 1: "Published 체크박스가 에디터 본문 위에 위치"
  - 블로그 편집 페이지 이동
  - Published 체크박스의 boundingBox.y < TiptapEditor의 boundingBox.y 확인
```

---

### ADM-2-3: Save 버튼 디자인 통일 (`bg-brand-navy` → `bg-primary`)

- [ ] 완료

**변경 파일**: `src/components/admin/BlogEditor.tsx`

**현재 문제**:
`<Button className="bg-brand-navy text-white ... hover:scale-[1.02]">`
- `bg-brand-navy`는 날짜 뱃지와 동일 색상 → 의미 혼동
- `hover:scale-[1.02]`은 불필요한 장식적 애니메이션

**변경 내용**:
```tsx
{/* 기존 */}
<Button className="bg-brand-navy text-white ... hover:scale-[1.02] ...">
{/* 변경 */}
<Button className="bg-primary text-primary-foreground hover:bg-primary/90">
```
- `hover:scale-[1.02]` 삭제
- shadcn/ui의 `default` variant과 동일한 디자인 언어 사용

동일 변경을 `WorkEditor.tsx`에도 적용.

**테스트 계획** — `tests/ui-admin-save-btn.spec.ts`:
```
테스트 1: "Save 버튼에 hover scale 애니메이션 없음"
  - 에디터 이동
  - Save 버튼의 computed transform이 hover 시 scale(1.02)가 아닌지 확인
```

---

### ADM-2-4: 저장하지 않은 변경 경고 (`beforeunload`)

- [ ] 완료

**변경 파일**: `src/components/admin/BlogEditor.tsx`, `src/components/admin/WorkEditor.tsx`

**현재 문제**:
편집 중 실수로 브라우저를 닫거나 다른 페이지로 이동하면 변경 내용이 유실된다. 경고 없음.

**변경 내용**:
두 에디터 모두에 `beforeunload` 핸들러 추가:
```tsx
const [isDirty, setIsDirty] = useState(false)

useEffect(() => {
  const handler = (e: BeforeUnloadEvent) => {
    if (isDirty) {
      e.preventDefault()
      // Modern browsers ignore custom messages, but this triggers the dialog
    }
  }
  window.addEventListener('beforeunload', handler)
  return () => window.removeEventListener('beforeunload', handler)
}, [isDirty])
```

`isDirty`는 title/content/tags 등 어느 필드든 변경되면 `true`, 저장 성공 후 `false`로 리셋.

**테스트 계획** — `tests/ui-admin-unsaved-warning.spec.ts`:
```
테스트 1: "변경 없이 페이지 이탈 → 경고 없음"
  - 편집 페이지 이동 → 다른 페이지 이동 → 정상 이동 확인

테스트 2: "변경 후 beforeunload 이벤트 리스너 등록됨"
  - 편집 페이지 이동 → 제목 수정
  - page.evaluate로 beforeunload 리스너가 등록되었는지 확인
  (Playwright는 실제 beforeunload 다이얼로그를 차단할 수 있으므로, 리스너 등록 확인에 초점)
```

---

### ADM-2-5: WorkEditor 탭/섹션 분리 (단일 스크롤 → 탭 레이아웃)

- [ ] 완료

**변경 파일**: `src/components/admin/WorkEditor.tsx`

**현재 문제**:
~1100줄 컴포넌트가 단일 스크롤 폼으로 렌더링. Title → Category → Period → Tags → Flexible Metadata(JSON) → Media → Videos → Content → Save. 사용자가 원하는 섹션을 찾으려면 긴 스크롤 필요.

**변경 내용**:
shadcn/ui `Tabs` 컴포넌트로 분리:
```tsx
<Tabs defaultValue="general" className="w-full">
  <TabsList className="grid w-full grid-cols-3">
    <TabsTrigger value="general">General</TabsTrigger>
    <TabsTrigger value="media">Media & Videos</TabsTrigger>
    <TabsTrigger value="content">Content</TabsTrigger>
  </TabsList>

  <TabsContent value="general">
    {/* Title, Category, Period, Tags, Excerpt */}
  </TabsContent>

  <TabsContent value="media">
    {/* Thumbnail, Icon, work Videos, Flexible Metadata */}
  </TabsContent>

  <TabsContent value="content">
    {/* TiptapEditor + Published */}
  </TabsContent>
</Tabs>
```

- General 탭: 기본 정보 (자주 수정)
- Media 탭: 미디어/비디오/메타데이터 (가끔 수정)
- Content 탭: 본문 편집 (집중 모드)

**테스트 계획** — `tests/ui-admin-work-editor-tabs.spec.ts`:
```
테스트 1: "WorkEditor에 3개 탭 존재"
  - Work 편집 페이지 이동
  - 'General', 'Media & Videos', 'Content' 탭이 존재 확인

테스트 2: "탭 전환 시 해당 섹션 표시"
  - 'Media & Videos' 탭 클릭
  - Thumbnail 업로드 영역 visible 확인
  - 'Content' 탭 클릭
  - TiptapEditor visible 확인

테스트 3: "탭 전환 시 이전 탭 데이터 유지"
  - General 탭에서 제목 입력
  - Content 탭으로 전환 후 다시 General 탭
  - 입력한 제목 유지 확인
```

---

### ADM-2-6: Flexible Metadata — JSON 직접 입력 → 구조화 UI

- [ ] 완료

**변경 파일**: `src/components/admin/WorkEditor.tsx`

**현재 문제**:
```tsx
<Textarea
    value={JSON.stringify(flexibleMetadata, null, 2)}
    onChange={(e) => setFlexibleMetadata(JSON.parse(e.target.value))}
    ...
/>
```
JSON 직접 편집은 관리자가 아닌 개발자 도구. 작은 실수로 `JSON.parse` 에러 발생.

**변경 내용**:
Key-Value 동적 폼으로 교체:
```tsx
<div className="space-y-2">
  <Label>Metadata</Label>
  {Object.entries(flexibleMetadata).map(([key, value], i) => (
    <div key={i} className="flex gap-2">
      <Input
        placeholder="Key"
        value={key}
        onChange={(e) => updateMetadataKey(i, e.target.value)}
        className="w-1/3"
      />
      <Input
        placeholder="Value"
        value={String(value)}
        onChange={(e) => updateMetadataValue(key, e.target.value)}
        className="flex-1"
      />
      <Button variant="ghost" size="icon" onClick={() => removeMetadataEntry(key)}>
        <Trash2 size={14} />
      </Button>
    </div>
  ))}
  <Button variant="outline" size="sm" onClick={addMetadataEntry} className="gap-2">
    <Plus size={14} /> Add Field
  </Button>
</div>
```

**테스트 계획** — `tests/ui-admin-work-metadata-ui.spec.ts`:
```
테스트 1: "메타데이터 필드 추가/삭제"
  - Work 편집 → Media 탭
  - 'Add Field' 클릭 → 입력 행 추가 확인
  - key/value 입력 → 삭제 버튼 클릭 → 행 제거 확인

테스트 2: "JSON textarea가 더 이상 존재하지 않음"
  - textarea[value*='{'] 요소가 Media 탭에 없는지 확인
```

---

## Phase 3: Admin 테이블 개선

---

### ADM-3-1: 삭제 확인 — `window.confirm` → Dialog 컴포넌트

- [ ] 완료

**변경 파일**:
- `src/components/admin/AdminBlogTableClient.tsx`
- `src/components/admin/AdminWorksTableClient.tsx`

**현재 문제**:
`window.confirm('정말 삭제하시겠습니까?')` → 기본 브라우저 alert은 스타일링 불가, 사이트 디자인과 완전히 단절.

**변경 내용**:
shadcn/ui `AlertDialog` 사용:
```tsx
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'

// 삭제 버튼 위치:
<AlertDialog>
  <AlertDialogTrigger asChild>
    <Button variant="destructive" size="sm">Delete</Button>
  </AlertDialogTrigger>
  <AlertDialogContent>
    <AlertDialogHeader>
      <AlertDialogTitle>Delete {itemTitle}?</AlertDialogTitle>
      <AlertDialogDescription>
        This action cannot be undone. The post will be permanently deleted.
      </AlertDialogDescription>
    </AlertDialogHeader>
    <AlertDialogFooter>
      <AlertDialogCancel>Cancel</AlertDialogCancel>
      <AlertDialogAction onClick={handleDelete} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
        Delete
      </AlertDialogAction>
    </AlertDialogFooter>
  </AlertDialogContent>
</AlertDialog>
```

**테스트 계획** — `tests/ui-admin-delete-dialog.spec.ts`:
```
테스트 1: "삭제 버튼 → AlertDialog 표시"
  - /admin/blog 이동
  - 첫 항목의 삭제 버튼 클릭
  - AlertDialog가 visible 확인
  - 'Cancel' 클릭 → Dialog 닫힘 → 항목 유지 확인

테스트 2: "window.confirm이 호출되지 않음"
  - page.on('dialog') 이벤트가 발생하지 않는 것 확인
```

---

### ADM-3-2: 검색 기능 확장 — 제목 외 태그/카테고리 검색

- [ ] 완료

**변경 파일**:
- `src/components/admin/AdminBlogTableClient.tsx`
- `src/components/admin/AdminWorksTableClient.tsx`

**현재 문제**:
현재 검색이 `title`만 매칭. 태그나 카테고리로 필터링 불가.

**변경 내용**:
검색 로직 밀터를 확장:
```tsx
// 기존
const filtered = items.filter(item =>
  item.title.toLowerCase().includes(search.toLowerCase())
)

// 변경
const filtered = items.filter(item => {
  const q = search.toLowerCase()
  return (
    item.title.toLowerCase().includes(q) ||
    item.tags?.some(t => t.toLowerCase().includes(q)) ||
    ('category' in item && item.category?.toLowerCase().includes(q))
  )
})
```

검색 Input placeholder도 업데이트: `"Search by title..."` → `"Search by title, tags, or category..."`

**테스트 계획** — `tests/ui-admin-table-search.spec.ts`:
```
테스트 1: "태그로 검색 가능"
  - /admin/blog 이동
  - 검색창에 특정 태그명 입력
  - 해당 태그를 가진 항목만 필터링되는지 확인

테스트 2: "카테고리로 Works 검색 가능"
  - /admin/works 이동
  - 검색창에 카테고리명 입력
  - 필터링 확인
```

---

### ADM-3-3: 혼합 언어(한국어/영어) 정리

- [ ] 완료

**변경 파일**:
- `src/components/admin/AdminBlogTableClient.tsx`
- `src/components/admin/AdminWorksTableClient.tsx`

**현재 문제**:
일부 UI 텍스트가 한국어(`"정말 삭제하시겠습니까?"`, `"검색"`) 일부는 영어(`"Delete"`, `"Search"`). 혼합 사용은 비전문적.

**변경 내용**:
**모든 Admin UI 텍스트를 영어로 통일** (admin은 개발자/관리자가 사용하므로):
- `"정말 삭제하시겠습니까?"` → AlertDialog으로 대체 (ADM-3-1에서 해결)
- `"새글 작성"` → `"New Post"`
- `"검색"` → `"Search"`
- 기타 한국어 레이블 → 영어로 변경

**테스트 계획** — `tests/ui-admin-table-lang.spec.ts`:
```
테스트 1: "Admin 테이블에 한국어 텍스트 없음"
  - /admin/blog 이동
  - 페이지 전체 textContent에서 한국어 문자(유니코드 범위 \uAC00-\uD7A3) 검색
  - 0개 확인 (데이터 내용은 제외 — 테이블 UI 레이블만)
```

---

## Phase 4: TiptapEditor 개선

---

### ADM-4-1: 에디터 툴바 Sticky 처리

- [ ] 완료

**변경 파일**: `src/components/admin/tiptap-editor/toolbar.tsx`

**현재 문제**:
에디터가 `min-h-[500px]`인데 툴바가 스크롤과 함께 사라진다. 긴 글 편집 시 서식 변경을 위해 맨 위로 스크롤해야 한다.

**변경 내용**:
```tsx
{/* 기존 */}
<div className="flex flex-wrap items-center gap-1 border-b border-gray-200 p-2 dark:border-gray-800">
{/* 변경 */}
<div className="sticky top-0 z-20 flex flex-wrap items-center gap-1 border-b border-border bg-background/95 p-2 backdrop-blur-sm">
```
- `sticky top-0 z-20` — 스크롤 시 상단 고정
- `bg-background/95 backdrop-blur-sm` — 반투명 배경으로 아래 콘텐츠 비침 방지
- `border-gray-200 dark:border-gray-800` → `border-border` (시맨틱)

**테스트 계획** — `tests/ui-admin-tiptap-sticky-toolbar.spec.ts`:
```
테스트 1: "에디터 스크롤 시 툴바가 상단에 고정"
  - 에디터에 긴 콘텐츠 입력 (또는 기존 긴 글 편집)
  - 에디터 영역 500px 스크롤
  - 툴바의 isIntersecting 또는 sticky 위치 확인
  - 굵기(Bold) 버튼이 여전히 visible 확인
```

---

### ADM-4-2: 링크 삽입 — `window.prompt` → Popover

- [ ] 완료

**변경 파일**: `src/components/admin/tiptap-editor/toolbar.tsx`

**현재 문제**:
`const url = window.prompt('Enter URL')` — 브라우저 기본 prompt는 스타일링 불가, UX 단절.

**변경 내용**:
shadcn/ui `Popover` 컴포넌트로 교체:
```tsx
<Popover>
  <PopoverTrigger asChild>
    <Button variant="ghost" size="icon" className={cn('h-8 w-8', editor.isActive('link') && 'bg-accent')}>
      <Link2 size={16} />
    </Button>
  </PopoverTrigger>
  <PopoverContent className="w-72 p-3" align="start">
    <div className="space-y-2">
      <Label htmlFor="link-url" className="text-xs">URL</Label>
      <Input
        id="link-url"
        placeholder="https://..."
        value={linkUrl}
        onChange={(e) => setLinkUrl(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') {
            applyLink()
          }
        }}
        className="h-8 text-sm"
      />
      <div className="flex justify-end gap-2">
        {editor.isActive('link') && (
          <Button variant="ghost" size="sm" onClick={removeLink}>Remove</Button>
        )}
        <Button size="sm" onClick={applyLink}>Apply</Button>
      </div>
    </div>
  </PopoverContent>
</Popover>
```

**테스트 계획** — `tests/ui-admin-tiptap-link-popover.spec.ts`:
```
테스트 1: "링크 버튼 클릭 → Popover 표시"
  - 에디터 이동
  - 텍스트 선택 → 링크 버튼 클릭
  - Popover 내 URL input visible 확인

테스트 2: "URL 입력 + Enter → 링크 적용"
  - input에 'https://example.com' 입력
  - Enter 키
  - 선택 텍스트에 링크가 적용되었는지 확인

테스트 3: "window.prompt가 호출되지 않음"
  - page.on('dialog') 이벤트 미발생 확인
```

---

### ADM-4-3: 에디터 하드코딩 색상 → 시맨틱 토큰

- [ ] 완료

**변경 파일**: `src/components/admin/TiptapEditor.tsx`, `src/components/admin/tiptap-editor/toolbar.tsx`

**현재 문제**:
`bg-white dark:bg-gray-950`, `border-gray-200 dark:border-gray-800` 등 하드코딩 색상이 전체 에디터에 산포.

**변경 내용**:
- `bg-white dark:bg-gray-950` → `bg-background`
- `dark:bg-gray-800` (toolbar active) → `bg-accent`
- `border-gray-200 dark:border-gray-800` → `border-border`
- `text-gray-400 dark:text-gray-600` → `text-muted-foreground`

**테스트 계획** — `tests/ui-admin-tiptap-semantic.spec.ts`:
```
테스트 1: "다크 모드에서 에디터 배경이 --background와 일치"
  - 다크 모드 전환
  - 에디터 이동
  - EditorContent 래퍼의 background-color와 CSS variable --background가 일치 확인
```

---

## 변경 파일 총 요약

| Phase | 파일 | 변경 유형 |
|-------|------|-----------|
| P0 | `src/app/admin/layout.tsx` | 대규모 수정 (sidebar 폭, padding, 색상, 중복 링크, 설명문) |
| P0 | `src/components/admin/AdminSidebarNav.tsx` | **신규** (active state용 Client Component) |
| P1 | `src/components/admin/BlogNotionWorkspace.tsx` | 대규모 수정 (Library→Sheet, client-side switching, 검색, hint 닫기, doc info 토글) |
| P1 | `src/components/admin/BlogEditor.tsx` | 수정 (Cmd+S) |
| P2 | `src/components/admin/BlogEditor.tsx` | 수정 (excerpt, published 위치, save 디자인, beforeunload) |
| P2 | `src/components/admin/WorkEditor.tsx` | 대규모 수정 (탭 레이아웃, 메타데이터 UI, Cmd+S, beforeunload, save 디자인) |
| P3 | `src/components/admin/AdminBlogTableClient.tsx` | 수정 (AlertDialog, 검색 확장, 언어 통일) |
| P3 | `src/components/admin/AdminWorksTableClient.tsx` | 수정 (AlertDialog, 검색 확장, 언어 통일) |
| P4 | `src/components/admin/tiptap-editor/toolbar.tsx` | 수정 (sticky, link popover, 색상) |
| P4 | `src/components/admin/TiptapEditor.tsx` | 수정 (색상) |
| 테스트 | `tests/ui-admin-*.spec.ts` | **신규 19개** |

**총: 소스 9개 수정 + 1개 신규, 테스트 19개 신규**

---

## 회귀 테스트

모든 Phase 완료 후:
```bash
# 기존 admin 테스트 회귀 확인
npx playwright test tests/admin-*.spec.ts --project=chromium-authenticated

# 신규 admin 테스트 전체 실행
npx playwright test tests/ui-admin-*.spec.ts --project=chromium-authenticated
```

영상: `test-results/playwright/` 자동 저장 (playwright.config.ts `video: 'on'`).

---

## 기존 파일과의 관계

| 파일 | 범위 |
|------|------|
| `ui-improvement-todolist-main-260411.md` | 메인(Home) 페이지 전용 |
| `ui-improvement-todolist-public-260411.md` | Blog/Works 공개 페이지 + 공통 접근성 |
| **이 파일** (`ui-improvement-todolist-admin-260411.md`) | Admin 전체 (사이드바, Notion View, 에디터, 테이블, Tiptap) |
