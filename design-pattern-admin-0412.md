# Design Pattern — Admin Panel (Work & Blog)

> **생성일**: 2026-04-12  
> **대상**: `src/app/admin/**`, `src/components/admin/**`  
> **참조**: `admin-design-review-0412.md` (냉정 리뷰)  
> **목적**: 하드코딩 제거, Callout 체계화, 컴포넌트 분리, CMS 프로페셔널 등급 달성  
> **UI/UX Pro Max 규칙 적용**: Priority 1 (Accessibility), 2 (Touch & Interaction), 4 (Style), 6 (Typography & Color), 8 (Forms & Feedback)

---

## 1. Color Token System (Admin)

### 1.1 규칙: Raw Tailwind Color 금지

Public 페이지와 동일하게, admin 컴포넌트에서도 `text-gray-*`, `bg-gray-*`, `border-gray-*` 등 **raw Tailwind 색상 직접 사용을 전면 금지**한다.

### 1.2 Admin Token Mapping Table

| 역할 | 사용할 Token | Tailwind Class | 대체 대상 (제거할 것) |
|------|-------------|----------------|----------------------|
| **제목 텍스트** | `--foreground` | `text-foreground` | `text-gray-900 dark:text-gray-50` |
| **보조 텍스트** | `--muted-foreground` | `text-muted-foreground` | `text-gray-500`, `text-gray-500 dark:text-gray-400` |
| **테이블 메타** | `--muted-foreground` | `text-muted-foreground` | `text-sm text-gray-500` (날짜 셀 등) |
| **카드/패널 배경** | `--card` | `bg-card` | `bg-white dark:bg-gray-950` |
| **뮤트 배경** | `--muted` | `bg-muted` | `bg-gray-100 dark:bg-gray-800`, `bg-gray-100 dark:bg-gray-900` |
| **테이블 배경** | `--background` | `bg-background` | `bg-white dark:bg-gray-950` |
| **테두리** | `--border` | `border-border` | `border-gray-200 dark:border-gray-800` |
| **이미지 placeholder** | `--muted` | `bg-muted` | `bg-gray-100 dark:border-gray-800 dark:bg-gray-900` |

### 1.3 Callout 색상 체계 (Admin 전용)

현재 callout이 파일마다 인라인으로 색상을 조합한다. 의미 기반으로 통일한다:

| 의미 | 색상 세트 | 사용처 |
|------|----------|--------|
| **Info** (안내/설명) | `border-border/80 bg-muted/50 text-muted-foreground` | 에디터 힌트, 업로드 가이드, 일반 안내 |
| **Success** (성공/완료) | `border-emerald-200 bg-emerald-50 text-emerald-900 dark:border-emerald-900/60 dark:bg-emerald-950/30 dark:text-emerald-100` | "Videos saved", "Placed in body" |
| **Warning** (경고/주의) | `border-amber-200 bg-amber-50 text-amber-900 dark:border-amber-900/60 dark:bg-amber-950/30 dark:text-amber-100` | Orphan video, validation 경고 |
| **Destructive** (위험) | Dialog 사용 | 삭제 확인은 반드시 Dialog |

**핵심 변경**: `sky-*` 계열 callout → `Info` 스타일로 통일. `emerald`은 성공 전용으로 한정.

### 1.4 현재 → 변경 맵 (Callout)

| 현재 | 의미 | 변경 |
|------|------|------|
| `border-sky-300 bg-sky-50/70 ... dark:border-sky-900 dark:bg-sky-950/20` (업로드 가이드) | Info | `border-border/80 bg-muted/50 text-muted-foreground` |
| `border-sky-300 bg-sky-50 ... dark:border-sky-900/60 dark:bg-sky-950/20` (비디오 안내) | Info | `border-border/80 bg-muted/50 text-muted-foreground` |
| `border-emerald-200 bg-emerald-50 ...` ("Publish 즉시") | Info | `border-border/80 bg-muted/50 text-muted-foreground` |
| `border-emerald-300 bg-emerald-50 ...` ("Placed in body") | Success | 유지 |
| `border-emerald-300 bg-emerald-50 ...` ("Videos were saved. Continue...") | Success | 유지 |
| `border-amber-300 bg-amber-50 ...` (orphan video) | Warning | 유지 |

---

## 2. UI Copy 규칙

### 2.1 절대 금지: 개발자용 설명문

Admin UI에 표시되는 모든 텍스트는 **최종 사용자(관리자)용**이어야 한다.  
다음은 **즉시 제거/교체** 대상:

| 파일 | 현재 텍스트 | 교체 |
|------|------------|------|
| `src/app/admin/blog/page.tsx` | "Titles now act as primary edit links, and the new Notion view keeps document browsing beside the editor with local batch-selection scaffolding for future bulk actions." | `"Manage all blog posts. Click a title to edit."` |
| `src/app/admin/works/page.tsx` | "Click a title to edit directly, or create a new work and return to this list as soon as it saves." | `"Manage all portfolio works."` |
| `WorkEditor.tsx` (Media) | "Add thumbnail and icon assets with clear click-to-upload fields. Dragging a file onto the input still works, but it is no longer the only obvious path." | `"Upload thumbnail and icon images for this work."` |
| `WorkEditor.tsx` (Video) | "You can add YouTube videos and MP4 uploads immediately. Ordering and removal update the work separately from the main form." | `"Add YouTube links or upload MP4 files. Video changes save immediately."` |
| `WorkEditor.tsx` (Video, create) | "You can stage videos while creating a work. The app creates the work first, then attaches the staged videos in order." | `"Stage videos before saving. They'll be attached after the work is created."` |
| `WorkEditor.tsx` (Content hint) | "Write the public-facing project story here. New works save live immediately, so keep the summary and body ready before hitting create." | `"Write the project description. This content is shown on the public site."` |
| `WorkEditor.tsx` (Footer hint) | "Saving creates a live work immediately, then returns you to the works list unless you choose the staged video flow." | 제거 (불필요) |
| `BlogEditor.tsx` (publish hint) | "New posts publish immediately when you save. You can switch them back to draft later from the edit screen." | `"New posts go live immediately. Toggle 'Published' off to save as draft."` |
| `BlogEditor.tsx` (footer hint) | "Saving creates a live post immediately, then returns you to the blog list so you can keep editing the library." | 제거 (불필요) |

### 2.2 설명문 스타일 규칙

- **최대 1문장** (20단어 이하)
- **동작 중심** ("Upload...", "Add...", "Manage...")
- **구현 디테일 노출 금지** ("scaffolding", "flow", "separately from the main form" 등)

---

## 3. WorkEditor 컴포넌트 구조 패턴

### 3.1 현재 문제

`WorkEditor.tsx`가 1,600줄 God Component. 다음 영역을 서브 컴포넌트로 분리한다:

### 3.2 목표 구조

```
WorkEditor.tsx (~300줄, 상태 관리 + 레이아웃)
├── WorkGeneralSection.tsx     (~100줄: title, category, period, tags, published, dates)
├── WorkMetadataEditor.tsx     (~80줄: flexible key-value metadata fields)
├── WorkMediaUploader.tsx      (~120줄: thumbnail/icon upload, preview, remove)
├── WorkVideoManager.tsx       (~250줄: YouTube/MP4 add, list, reorder, remove, insert/remove from body)
├── WorkContentSection.tsx     (~80줄: TiptapEditor wrapper, AI fix, hints)
└── WorkEditorActions.tsx      (~60줄: save/cancel/create buttons)
```

### 3.3 State 공유 패턴

상위 `WorkEditor`가 모든 state를 소유하고, props로 내려준다. Custom hooks로 추출 가능:

```tsx
// hooks/useWorkVideos.ts — 비디오 CRUD + 정렬 로직
// hooks/useWorkAssetUpload.ts — 썸네일/아이콘 업로드 로직
// hooks/useWorkAutoThumbnail.ts — 자동 썸네일 생성 로직
```

### 3.4 탭 → 실제 탭 전환

현재 3개 섹션 동시 렌더 + scrollIntoView → **조건부 렌더링으로 전환**:

```tsx
{activeTab === 'general' && <WorkGeneralSection ... />}
{activeTab === 'media' && (
  <>
    <WorkMetadataEditor ... />
    <WorkMediaUploader ... />
    <WorkVideoManager ... />
  </>
)}
{activeTab === 'content' && <WorkContentSection ... />}
```

- **장점**: 페이지 길이 대폭 감소, 포커스 명확, 렌더 비용 절감
- **주의**: 탭 전환 시 unsaved state가 유실되지 않도록 상위에서 state 유지

### 3.5 Metadata 위치 이동

`Flexible Metadata`는 의미상 **General 탭**에 배치:

| 탭 | 포함 요소 |
|----|----------|
| **General** | Title, Category, Period, Tags, Published, Dates, Flexible Metadata |
| **Media & Videos** | Thumbnail, Icon, YouTube/MP4 Videos |
| **Content** | TiptapEditor, AI Enrich |

---

## 4. Table Design Pattern (List Pages)

### 4.1 Works 테이블 — 썸네일 컬럼 추가

현재 Works 리스트는 텍스트만. 포트폴리오 CMS답게 **썸네일 프리뷰** 필요:

```
┌──┬────────────┬────────────┬──────────┬──────────┬──────────┬──────────┐
│☐ │ Thumbnail  │ Title      │ Status   │ Date     │ Category │ Actions  │
├──┼────────────┼────────────┼──────────┼──────────┼──────────┼──────────┤
│☐ │ [40x30 img]│ My Work    │ Published│ Apr 12   │ Web      │ 👁 ✏️ 🗑 │
└──┴────────────┴────────────┴──────────┴──────────┴──────────┴──────────┘
```

**UI/UX Pro Max**: `image-dimension` — 이미지에 고정 크기를 선언하여 CLS 방지.

### 4.2 Blog 테이블 — Tags를 Badge로

현재 `blog.tags?.join(', ')` → 쉼표 구분 텍스트. Badge 컴포넌트로 교체:

```tsx
// 변경 전
<TableCell>{blog.tags?.join(', ')}</TableCell>

// 변경 후
<TableCell>
  <div className="flex flex-wrap gap-1">
    {blog.tags?.slice(0, 3).map((tag) => (
      <Badge key={tag} variant="secondary" className="text-xs">
        {tag}
      </Badge>
    ))}
    {(blog.tags?.length ?? 0) > 3 && (
      <Badge variant="outline" className="text-xs">+{(blog.tags?.length ?? 0) - 3}</Badge>
    )}
  </div>
</TableCell>
```

### 4.3 페이지네이션 간소화

```
// 변경 전: 5개 버튼
[First] [Previous] 1/5 [Next] [Last]

// 변경 후: 3개 버튼 (아이콘 기반)
[← Previous]  Page 1 of 5  [Next →]
```

- `First`/`Last` 제거 (콘텐츠 수가 적은 개인 포트폴리오에서 불필요)
- 아이콘 + 텍스트 조합: `ChevronLeft`/`ChevronRight`
- aria-label을 영어로 통일

---

## 5. Video Management UX Pattern

### 5.1 버튼 계층화

| 동작 | 중요도 | 스타일 |
|------|--------|--------|
| **Insert Into Body** | Primary (핵심 동작) | `variant="default"` (강조) |
| **Remove From Body** | Secondary | `variant="outline"` |
| **Move Up / Move Down** | Tertiary (순서 조정) | `variant="ghost" size="icon"` (`ChevronUp`/`ChevronDown` 아이콘) |
| **Remove** | Destructive | `variant="ghost" size="icon"` + `text-destructive` (`Trash2` 아이콘) |

### 5.2 비디오 카드 레이아웃

```
┌─ Video Card ───────────────────────────────────────────────┐
│ [▲] [▼]  YouTube · order 1 · Placed in body    [Insert] [🗑] │
│          [Video Player Preview]                              │
│          ✅ Placed in body                                   │
└──────────────────────────────────────────────────────────────┘
```

- 정렬 버튼: 왼쪽 아이콘으로 (드래그 앤 드롭 없이도 콤팩트)
- 주요 액션: 오른쪽으로
- 상태 뱃지: "Placed in body" → Success Badge (인라인 callout box 대체)

### 5.3 Create & Save 버튼 통합

```tsx
// 변경 전: 2개 분리
<Button variant="outline" disabled={hasStagedVideos}>Create Work</Button>
<Button disabled={!hasStagedVideos}>Create And Add Videos</Button>

// 변경 후: 1개 통합
<Button
  type="button"
  onClick={() => void saveWork(hasStagedVideos ? 'with-videos' : 'default')}
  disabled={isSaving || !isDirty || !title.trim()}
  className="px-8 font-medium"
>
  {isSaving ? 'Creating...' : hasStagedVideos ? 'Create with Videos' : 'Create Work'}
</Button>
```

---

## 6. Error Feedback Pattern

### 6.1 `window.alert()` 전면 금지

모든 에러를 `toast.error()`로 통일:

```tsx
// 변경 전
window.alert(error instanceof Error ? error.message : 'Failed to delete blogs.')

// 변경 후
toast.error(error instanceof Error ? error.message : 'Failed to delete blogs.')
```

**UI/UX Pro Max**: `error-feedback` — Clear error messages near the problem. `window.alert()`은 접근성과 UX 모두 위반.

### 6.2 에러 피드백 규칙

| 에러 유형 | 피드백 방식 |
|-----------|-----------|
| API 호출 실패 | `toast.error()` |
| 유효성 검사 실패 | `toast.error()` 또는 필드 인라인 에러 |
| 삭제 확인 | `Dialog` (이미 구현됨) |
| 네트워크 에러 | `toast.error()` |

---

## 7. Accessibility Rules (Admin)

| 항목 | 규칙 | 현재 상태 | 조치 |
|------|------|----------|------|
| **aria-label 언어** | 버튼 텍스트와 동일 언어 (영어) | `aria-label="처음"` + `>First<` 혼용 | 영어로 통일 |
| **icon-only 버튼** | 반드시 `title` 또는 `aria-label` | ✅ OK (View/Edit/Delete에 title 있음) | — |
| **색상 대비** | ≥ 4.5:1 AA | `text-gray-500`은 배경에 따라 위반 가능 | semantic token 전환 |
| **Focus ring** | 모든 인터랙티브 요소 | ✅ OK (shadcn 기본) | — |
| **Form labels** | 모든 Input에 Label 연결 | ✅ OK | — |
| **keyboard nav** | Tab 순서가 시각적 순서와 일치 | ✅ OK | — |

### 7.1 aria-label 변경 맵

| 현재 | 변경 |
|------|------|
| `aria-label="처음"` | `aria-label="First page"` |
| `aria-label="이전"` | `aria-label="Previous page"` |
| `aria-label="다음"` | `aria-label="Next page"` |
| `aria-label="끝"` | `aria-label="Last page"` |

---

## 8. Admin Layout Pattern

### 8.1 사이드바

| 속성 | 현재 | 목표 |
|------|------|------|
| 데스크탑 너비 | `md:w-64` (고정) | 유지 (CMS에 적절) |
| 모바일 | 전체 nav 노출 | 햄버거 메뉴 + Sheet (향후) |
| 콘텐츠 max-width | `max-w-4xl` (에디터만) | 테이블은 `max-w-6xl` 또는 제한 없음 |

### 8.2 콘텐츠 영역 규칙

| 페이지 유형 | max-width | 이유 |
|------------|-----------|------|
| **리스트 (table)** | 제한 없음 (`w-full`) | 테이블은 넓을수록 좋음 |
| **에디터 (form)** | `max-w-4xl` | 폼 입력은 좁은 게 가독성 좋음 |
| **대시보드** | `max-w-6xl` | 카드 그리드에 적절 |

---

## 9. File-Level Pattern Application

| 파일 | 적용할 패턴 |
|------|------------|
| `AdminBlogTableClient.tsx` | Token 전환, 페이지네이션 간소화, Tags Badge화, alert→toast, aria-label 영어 |
| `AdminWorksTableClient.tsx` | Token 전환, 썸네일 컬럼 추가, 페이지네이션 간소화, alert→toast, aria-label 영어 |
| `BlogEditor.tsx` | Token 전환, 설명 텍스트 교체, Callout 통일 |
| `WorkEditor.tsx` | 컴포넌트 분리, 탭→실제 탭, Metadata 위치 이동, 설명 텍스트 교체, Callout 통일, 버튼 통합, Token 전환 |
| `AdminSidebarNav.tsx` | 현재 OK (유지) |
| `admin/blog/page.tsx` | 설명 텍스트 교체 |
| `admin/works/page.tsx` | 설명 텍스트 교체 |
| `admin/layout.tsx` | 현재 OK (향후 모바일 햄버거 메뉴 추가) |
