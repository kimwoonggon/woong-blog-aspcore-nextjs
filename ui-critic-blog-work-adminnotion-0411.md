Created 5 todos

Read [](file:///mnt/d/woong-blog/woong-blog/src/app/admin)

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin)

Read [](file:///mnt/d/woong-blog/woong-blog/src/app/admin/page.tsx)

Read [](file:///mnt/d/woong-blog/woong-blog/src/app/admin/layout.tsx#1-1), lines 1 to 80

Read [](file:///mnt/d/woong-blog/woong-blog/src/app/admin/dashboard/page.tsx#1-1), lines 1 to 100

Read [](file:///mnt/d/woong-blog/woong-blog/src/app/admin/blog)

Read [](file:///mnt/d/woong-blog/woong-blog/src/app/admin/works)

Read [](file:///mnt/d/woong-blog/woong-blog/src/app/admin/layout.tsx#80-80), lines 80 to 96

Read [](file:///mnt/d/woong-blog/woong-blog/src/app/admin/blog/notion)

Read [](file:///mnt/d/woong-blog/woong-blog/src/app/admin/works/page.tsx)

Read [](file:///mnt/d/woong-blog/woong-blog/src/app/admin/blog/page.tsx)

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/BlogNotionWorkspace.tsx#1-1), lines 1 to 200

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/BlogEditor.tsx#1-1), lines 1 to 150

Read [](file:///mnt/d/woong-blog/woong-blog/src/app/admin/blog/notion/page.tsx)

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/BlogNotionWorkspace.tsx#200-200), lines 200 to 350

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/BlogEditor.tsx#150-150), lines 150 to 244

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/WorkEditor.tsx#1-1), lines 1 to 200

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/WorkEditor.tsx#200-200), lines 200 to 500

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/TiptapEditor.tsx#1-1), lines 1 to 150

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/AdminBlogTableClient.tsx#1-1), lines 1 to 150

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/AdminBlogTableClient.tsx#150-150), lines 150 to 270

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/BlogNotionWorkspace.tsx#350-350), lines 350 to 369

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/AdminWorksTableClient.tsx#1-1), lines 1 to 200

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/tiptap-editor)

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/tiptap)

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/TiptapEditor.tsx#145-145), lines 145 to 180

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/WorkEditor.tsx#500-500), lines 500 to 800

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/tiptap-editor/toolbar.tsx#1-1), lines 1 to 150

Read [](file:///mnt/d/woong-blog/woong-blog/src/components/admin/WorkEditor.tsx#800-800), lines 800 to 1100

Starting: *Deep critique and improvement plan* (5/5)

Now I have comprehensive understanding of all three areas. Let me compile the deep analysis.

---

# Works / Blog / Admin Notion View — 심층 UI/UX 비판 및 개선 계획

30개 이상의 소스 파일을 분석한 결과입니다. UI/UX Pro Max Priority 1~10 룰과 Vercel Web Interface Guidelines 기준으로 비판합니다.

---

## PART A: Blog 목록 + 상세 페이지 비판

### A-1. Blog 목록 페이지 (`/blog`)

**현재 구조**: 4컬럼 카드 그리드, 텍스트 온리 (썸네일 없음)

#### 치명적 문제: **시각적 앵커(Visual Anchor) 부재**

blog/page.tsx/blog/page.tsx)의 카드 구조:
```
┌─────────────────┐
│  Title (텍스트)   │
│  날짜 · 태그      │
│  발췌 (텍스트)    │
└─────────────────┘  × 4열 × N행
```

- **모든 카드가 텍스트 덩어리** — 이미지도, 아이콘도, 컬러 포인트도 없다. 12개 카드가 한 화면에 나오면 **눈이 어디를 봐야 할지 모른다**
- Works 카드는 썸네일+날짜뱃지+카테고리 구조인데, Blog 카드는 완전히 다른 디자인 언어 → **사이트 내 디자인 일관성이 깨짐**
- 태그가 `text-gray-500 dark:text-gray-500` — 다크 모드에서 **WCAG AA 명암비 미달** (약 3.2:1)

#### 구조적 문제: **카드 높이 불일관**

- `!gap-0 !py-0` + `responsive-feed-card`로 카드 패딩을 리셋하지만, 카드 안 콘텐츠(title, excerpt) 길이 차이로 **그리드 내 카드 높이가 달라짐**
- `line-clamp-2` (제목) + `line-clamp-3` (발췌) 만으로는 카드 사이 시각적 리듬이 보장되지 않음

#### 인터랙션 문제

- 카드 전체를 `<Link>`로 감싸고 있어 클릭 영역은 좋으나, **hover 시 카드 전체의 시각적 상승(elevation) 변화가 미약** — `hover:shadow-md`만으로는 부족
- **active/pressed 상태가 없음** — 모바일에서 탭 시 피드백 없음

### A-2. Blog 상세 페이지 (`/blog/[slug]`)

#### 강점
- `max-w-3xl` 콘텐츠 폭 — 읽기 최적화에 적합 ✓
- `border-l-4 border-brand-accent` 발췌문 인용 스타일 — 시각적 구분 좋음 ✓
- 태그에 `#` prefix + `rounded-full border` — 깔끔한 인터랙티브 느낌 ✓

#### 비판

1. **읽기 진행 표시(Reading Progress) 없음** — 긴 글에서 사용자가 얼마나 읽었는지 알 수 없음. 기술 블로그에서 필수적인 기능
2. **목차(Table of Contents) 없음** — `prose h1, h2, h3`이 정의되어 있는데 TOC가 없으면 긴 기술 글을 스캔하기 어렵다
3. **날짜 배지 색상**: `bg-brand-accent` (빨간색) — Works 상세의 `bg-brand-orange`와 다름. 같은 "날짜" 정보인데 **색상 언어가 불일치**
4. **Related Content 섹션**: `RelatedContentList`가 `max-w-6xl` 컨테이너에 있는데, 본문은 `max-w-3xl`에 있어서 **폭이 갑자기 넓어지는 시각적 불연속**
5. **글 본문 하단에 CTA 없음** — 글을 다 읽은 사용자에게 "다음 행동"을 제안하지 않음 (예: "이 글이 유용했나요?", SNS 공유, 다음글/이전글)
6. **`scroll-margin-top` 미설정** — heading anchor가 있다면, sticky navbar(h-20)에 의해 가려질 수 있음

### A-3. Works 목록 페이지 (`/works`)

#### 강점
- 썸네일 이미지 기반 카드 — Blog보다 시각적 매력 높음 ✓
- `aspect-[4/3]` 이미지 비율 고정 — CLS 방지 ✓
- `group-hover/card:scale-105` 이미지 줌 효과 — 인터랙티브 ✓
- 태그 뱃지 `text-[10px] uppercase tracking-wider` — 미니멀 ✓

#### 비판

1. **고정 높이 카드 문제 재확인**: `.works-feed-card { height: 30rem }` — 콘텐츠가 짧으면 하단 빈 공간, 길면 잘림
2. **카테고리 필터 없음** — Works가 10개 이상이면 카테고리별 필터가 필수인데, 현재는 전체 목록만 표시
3. **페이지 제목에 불필요한 애니메이션**: `opacity-0 animate-fade-in-up` — "Works" 텍스트에 fade-in 필요 없음. Hero 이외의 페이지 제목에 입장 애니메이션은 **과잉 연출**이고 LCP를 해친다
4. **`responsive-feed-card` 클래스는 height-based 미디어 쿼리(`max-height`)에 의존** — 이것은 매우 비표준적. 뷰포트 높이 기반 반응형은 예측 불가능하고, 가로 모드 태블릿에서 엉뚱한 결과를 낸다
5. **"No Image" 폴백이 시각적으로 빈약** — 순수 텍스트 `"No Image"`만으로는 placeholder 역할이 불충분. 아이콘이나 브랜드 그라데이션 배경이 필요

### A-4. Works 상세 페이지 (`/works/[slug]`)

#### 강점
- 비디오 플레이어 지원 (YouTube + MP4) ✓
- 인라인 비디오 임베드와 자동 썸네일 생성 ✓
- 발췌문 `border-l-4 border-brand-orange` — Blog과 다른 색이지만 구조는 일관 ✓

#### 비판

1. **Blog과 Works 상세의 레이아웃이 거의 동일한데 색상만 다름** — 이것은 "일관성"이 아니라 **"구분 부재"**. Works는 시각적 포트폴리오이므로 **이미지/비디오를 히어로 영역에 크게 보여줘야 한다**. 현재 구조는 텍스트 기반 블로그 글과 동일
2. **`dark:text-gray-500` (period 텍스트)** — 명암비 미달 (이미 P0에서 지적)
3. **태그가 헤더 하단에 분리되어 있음**: `mt-8`으로 태그가 본문 시작점과 떨어져 있어서 태그가 콘텐츠의 일부인지 헤더의 일부인지 모호
4. **비디오가 본문 위에 무조건 배치** (`!hasInlineVideoEmbeds`일 때) — 비디오가 여러 개면 스크롤 없이 본문에 도달 불가

---

## PART B: Admin 패널 / 글 작성 / Notion View 비판

### B-1. Admin Layout (사이드바)

#### 현재 구조
```
┌────────────────┬──────────────────────────┐
│  sidebar       │                          │
│  (w-80, 고정)  │     main content         │
│  ── nav items  │     (flex-1)             │
│                │                          │
└────────────────┴──────────────────────────┘
```

#### 비판

1. **사이드바 `w-80` (320px)가 과도하게 넓음** — 6개 nav 항목에 320px는 낭비. 240px면 충분. 특히 Notion View처럼 콘텐츠 영역이 넓을수록 사이드바가 공간을 빼앗음
2. **nav 항목의 `py-6` (48px 높이)가 과도** — 6개 항목이 약 300px 세로 공간 차지. 모바일에서 스크롤 없이 보기 어려움
3. **활성 상태(active state) 표시 없음** — 모든 nav 버튼이 `variant="ghost"`로 동일하게 보임. 현재 어디에 있는지 시각적 피드백이 **0**. 이것은 **네비게이션 UX의 기본 원칙 위반**
4. **"Public Home"과 "Open Site" 버튼이 중복** — 둘 다 `/`로 이동. `target="_blank"`만 다름. 하나로 합치거나 아이콘으로 구분 필요
5. **모바일에서 사이드바가 `w-full border-b`로 변환** — 수평으로 모든 nav를 나열하면 작은 화면에서 `overflow-x`가 발생할 수 있음. 드롭다운이나 시트로 변환해야 함
6. **다크 모드에서 `bg-gray-50` / `bg-gray-900` 하드코딩** — 시맨틱 토큰(`bg-background`, `bg-card`) 대신 하드코딩된 색상을 사용. 테마 변경 시 불일치 발생

### B-2. Blog Editor (전체 에디터)

#### 현재 구조
BlogEditor.tsx:
```
┌──────────────────────────────────────┐
│  Title |  Tags                       │  ← 메타 영역
│  Visibility | Last Modified          │
├──────────────────────────────────────┤
│  Content                             │
│  ┌──────────────────────────────┐    │
│  │ Tiptap Toolbar               │    │
│  │ Editor Body (min-h 500px)    │    │
│  └──────────────────────────────┘    │
├──────────────────────────────────────┤
│       Cancel  |  Create/Update Post  │
└──────────────────────────────────────┘
```

#### 비판

1. **`max-w-4xl` 제한** — 에디터 자체가 좁은데, admin layout의 사이드바 w-80까지 빼면 실제 에디터 작업 영역이 **600px 미만**. 글을 쓸 때 답답함
2. **Excerpt(발췌) 입력 필드가 없음** — `blog.excerpt`가 공개 페이지에서 사용되는데, 에디터에서 직접 입력할 수 없음. 자동 생성인지 확인 필요하지만, 수동 제어가 불가능하면 SEO와 카드 미리보기에 치명적
3. **Published 체크박스가 Content 영역 안에 숨겨져 있음** — `isEditing`일 때만 보이고, 위치가 "Content" 제목 옆. 이것은 **"내용"과 "게시 상태"라는 전혀 다른 관심사를 한 영역에 섞은 것**
4. **저장 버튼이 `bg-brand-navy`로 브랜드 accent와 다른 색** — primary action 버튼은 사이트의 primary color를 써야 하는데, 여기서만 navy 사용. 디자인 토큰 불일치
5. **`hover:scale-[1.02]` 미묘한 스케일 애니메이션** — 관리자 UI에서 "장식적" 애니메이션은 불필요. 빈번한 반복 작업에서 시각적 노이즈
6. **변경사항 없을 때 "unsaved changes" 경고 없음** — `isDirty` 상태는 추적하지만, **브라우저 뒤로가기나 다른 링크 클릭 시 경고가 없음**. Vercel Guidelines: "Warn before navigation with unsaved changes"

### B-3. Blog Notion View (핵심 작업 환경)

#### 현재 구조
BlogNotionWorkspace.tsx:
```
┌─────────────┬──────────────────────────────────────┐
│ Blog Library │  Blog Notion View                    │
│ (320px)      │  ┌────────────────┬──────────┐      │
│              │  │ Title / Tags   │ Doc Info │      │
│ [글 목록]    │  │ Published CB   │ Slug     │      │
│              │  ├────────────────┤ Updated  │      │
│              │  │ Capability Hint│ Save btn │      │
│              │  │ Tiptap Editor  │          │      │
│              │  └────────────────┴──────────┘      │
└─────────────┴──────────────────────────────────────┘
```

#### 이것은 이 웹사이트의 **가장 중요한 도구**이다. 수시로 글을 쓰고 관리하는 공간이므로 가장 가혹하게 비판한다.

**치명적 문제들:**

1. **이중 사이드바 문제**: Admin layout의 `w-80` 사이드바 + Notion View의 `320px` Blog library = **총 640px가 사이드바에 소비**. 1920px 모니터에서 에디터 영역은 **1280px - 패딩**, 1440px 모니터에서는 **800px 미만**. 이것은 글 쓰기에 **답답한 폭**이다
2. **Capability Hint 배너가 매번 보임** — "Type `/` for commands, insert code blocks..." 안내가 영구적으로 표시됨. **처음 한 번만 보여주고 dismiss 가능하게** 해야 함. 매번 글 열 때마다 이 안내를 보면 인지 피로
3. **Document info 사이드바(260px)가 에디터 옆에 항상 열려있음** — Slug, Published date, Updated date는 **참조 정보**이지 항상 봐야 하는 정보가 아닌데, 에디터 폭을 260px 더 빼앗음
4. **Autosave 상태 표시가 헤더 우측 상단에만 있음** — `"Saving..."`, `"Saved"`, `"Error"` 텍스트가 작고 눈에 안 띔. **명확한 시각적 피드백 필요** (예: 에디터 하단 고정 바, 또는 제목 옆에 동기화 아이콘)
5. **Blog library에서 글 선택 시 전체 페이지가 서버에서 다시 로드됨** — 현재 `<Link href="/admin/blog/notion?id=...">` 방식이라 **SSR 왕복**. Notion처럼 빠르게 글 사이를 전환하려면 클라이언트 사이드 전환이 필요
6. **글 검색/필터 없음 (Library 패널)** — 글이 20개 이상이면 스크롤로 찾기 불가능. 검색 입력필드가 없음
7. **"Save Post Settings" 버튼이 disabled 상태가 명확하지 않음** — `metaDirty`가 false일 때 disabled이지만, 시각적으로 disabled 상태가 충분히 구분되지 않을 수 있음
8. **Keyboard shortcut 없음** — Notion의 핵심 장점은 `Cmd+S`, `Cmd+Enter`, `/` 커맨드 등의 키보드 워크플로우. 현재 `/` 커맨드는 Tiptap에서 지원하지만, **메타데이터 저장(`Cmd+S`), 게시 토글 등의 단축키가 없음**

### B-4. Work Editor

#### 비판

1. **폼이 압도적으로 길다** — Title, Category, Period, Tags, Flexible Metadata(JSON), Work Media (Thumbnail + Icon), Work Videos (YouTube + MP4), Content, Visibility, Save... **하나의 스크롤 페이지에 모든 것이 있다**. 이것은 Notion이 아니라 **구글 시트**
2. **"Flexible Metadata (JSON)"** — 일반 사용자(당신 자신 포함)가 JSON을 직접 입력하도록 강요. `min-h-[120px]` textarea에 `{"key": "value"}` 플레이스홀더. 이것은 **개발자 인터페이스지 콘텐츠 관리 인터페이스가 아님**
3. **비디오 관리 UX가 복잡** — YouTube URL 입력 + MP4 업로드 + 정렬 + 인라인 배치 + 삭제가 모두 한 섹션에. 초보 사용자는 물론 **매일 쓰는 당신조차도** 실수할 수 있는 복잡도
4. **이미지 업로드 영역의 file input이 OS 기본 스타일** — `<Input type="file">` 그대로. 드래그앤드롭 영역이 별도 표시되지 않음. Notion/Medium 수준의 드롭존이 아님
5. **Works에는 Notion View가 없음** — Blog에만 Notion View가 있고, Works는 전통적인 폼. 둘 다 같은 종류의 콘텐츠인데 편집 경험이 완전히 다름

### B-5. Admin Table (Blog/Works 목록)

#### 비판

1. **테이블이 좁은 화면에서 읽기 어려움** — Title, Status, Published Date, Tags/Category, Actions가 한 줄에. 1024px 이하에서 컬럼이 겹침
2. **삭제 확인이 `window.prompt`/`window.confirm`** — 네이티브 브라우저 다이얼로그는 **관리자 UX에 부적합**. shadcn/ui의 `AlertDialog` 사용 필요
3. **검색이 제목만 지원** — 태그, 카테고리, 게시 상태로 필터링 불가
4. **페이지네이션이 한국어("처음", "이전", "다음", "끝")인데 나머지 UI는 영어** — 언어 혼용

### B-6. Tiptap Editor (공통 에디터 컴포넌트)

#### 강점
- Slash command (`/`) 지원 ✓
- 이미지 드래그앤드롭, 붙여넣기 지원 ✓
- Bubble menu(선택 시 서식 팝업) ✓
- HTML/3D 블록 커스텀 확장 ✓

#### 비판

1. **Toolbar가 스크롤에 따라 사라짐** — `border-b` 상단 고정이 아니라 에디터 컨테이너 내부에 위치. 긴 글 편집 시 **서식 도구에 접근하려면 스크롤 업 필요**. `sticky top-0`이어야 함
2. **에디터 배경색 하드코딩**: `bg-white dark:bg-gray-950` — 시맨틱 토큰 미사용
3. **링크 삽입이 `window.prompt`** — `const url = window.prompt('URL', previousUrl)`. 이것은 2020년대의 에디터가 아님. 인라인 팝오버 또는 `Dialog` 컴포넌트 사용 필요
4. **이미지 리사이즈 불가** — 업로드된 이미지의 크기를 에디터 내에서 조절할 수 없음
5. **에디터 영역 `min-h-[500px]`** — 500px은 Notion View에서는 충분하지만, Notion처럼 화면 전체를 채우는 느낌이 아님. **에디터가 남은 뷰포트 높이를 채우도록** `flex-1`이어야 함
6. **Slash command 목록에 접근성 부족** — 키보드 네비게이션, 포커스 관리가 미흡할 가능성

---

## PART C: 통합 개선 계획

### Phase 0: CRITICAL (관리자 생산성 직결)

| ID | 항목 | 영향 범위 |
|----|------|-----------|
| C-0-1 | Notion View 이중 사이드바 해소: admin sidebar를 축소 가능하게 (collapse) | `admin/layout.tsx` |
| C-0-2 | Notion View에서 글 전환을 클라이언트 사이드로 변경 | BlogNotionWorkspace.tsx |
| C-0-3 | Toolbar를 `sticky`로 변경하여 스크롤 중에도 접근 가능 | `tiptap-editor/toolbar.tsx` |
| C-0-4 | 링크 삽입을 `Dialog` 컴포넌트로 변경 | TiptapEditor.tsx |
| C-0-5 | Unsaved changes 경고 추가 (`beforeunload`) | BlogEditor.tsx, WorkEditor.tsx |
| C-0-6 | `Cmd+S`/`Ctrl+S` 단축키로 저장 | BlogNotionWorkspace.tsx, editors |

### Phase 1: 공개 페이지 시각적 개선

| ID | 항목 | 영향 범위 |
|----|------|-----------|
| C-1-1 | Blog 카드에 시각적 앵커 추가 (태그 뱃지 컬러, 첫 글 featured) | `blog/page.tsx` |
| C-1-2 | Blog/Works 상세 날짜 뱃지 색상 통일 (`brand-navy`) | `blog/[slug]/page.tsx`, `works/[slug]/page.tsx` |
| C-1-3 | Works 상세 히어로에 이미지/비디오를 풀 와이드로 강조 | `works/[slug]/page.tsx` |
| C-1-4 | Blog 상세에 TOC(Table of Contents) 사이드바 추가 | `blog/[slug]/page.tsx` 신규 컴포넌트 |
| C-1-5 | Works 목록 페이지 제목에서 fade-in 애니메이션 제거 | `works/page.tsx` |
| C-1-6 | `scroll-margin-top: 6rem` heading anchor 설정 | globals.css |
| C-1-7 | Works "No Image" 폴백을 그라데이션+아이콘으로 개선 | `works/page.tsx` |

### Phase 2: Admin 효율성 개선

| ID | 항목 | 영향 범위 |
|----|------|-----------|
| C-2-1 | Admin sidebar에 active state 표시 추가 | `admin/layout.tsx` |
| C-2-2 | Admin sidebar 하드코딩 색상 → 시맨틱 토큰 | `admin/layout.tsx` |
| C-2-3 | Notion View: Capability Hint를 dismissible로 변경 | BlogNotionWorkspace.tsx |
| C-2-4 | Notion View: Document Info를 collapsible로 변경 | BlogNotionWorkspace.tsx |
| C-2-5 | Blog Library 패널에 검색 필드 추가 | BlogNotionWorkspace.tsx |
| C-2-6 | BlogEditor에 excerpt(발췌) 입력 필드 추가 | BlogEditor.tsx |
| C-2-7 | `window.prompt` 삭제 확인 → `AlertDialog` 교체 | AdminBlogTableClient.tsx, AdminWorksTableClient.tsx |
| C-2-8 | Autosave 상태 표시 개선 (에디터 하단 바) | BlogNotionWorkspace.tsx |

### Phase 3: 디자인 일관성 & 정교화

| ID | 항목 | 영향 범위 |
|----|------|-----------|
| C-3-1 | Work Editor를 탭/섹션 기반으로 재구성 (Basic Info / Media / Content) | WorkEditor.tsx |
| C-3-2 | Works에도 Notion View 제공 또는 에디터 통합 | `admin/works/` 신규 |
| C-3-3 | height 기반 미디어 쿼리를 width 기반으로 전환 | globals.css |
| C-3-4 | Blog/Works 카드 디자인 언어 통일 | `blog/page.tsx` 카드 구조 |
| C-3-5 | 에디터 배경색 시맨틱 토큰화 | TiptapEditor.tsx |
| C-3-6 | 공개 페이지-관리자 UI 언어 통일 (한/영 혼합 해소) | 전역 |
