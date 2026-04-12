# 개선 실행 지시서 — Admin Panel (Work & Blog) D+ → B+

> **생성일**: 2026-04-12  
> **참조**: `design-pattern-admin-0412.md` (디자인 패턴), `admin-design-review-0412.md` (냉정 리뷰)  
> **목표**: 13개 문제 전수 실행 → 프로페셔널 CMS 품질 달성  
> **UI/UX Pro Max 규칙 적용**: Priority 1 (Accessibility), 2 (Touch & Interaction), 4 (Style), 6 (Typography & Color), 8 (Forms & Feedback)

---

## Phase 1: CRITICAL — 오늘 바로 고쳐야 할 것 (1~2시간)

### Task 1. 개발자용 설명 텍스트 전면 교체

**우선순위**: 1 (최우선)  
**난이도**: S  
**영향 파일 수**: 4개  
**UI/UX Pro Max 규칙**: `consistency`, `primary-action`

#### 1.1 변경 맵

| 파일 | 현재 | 변경 |
|------|------|------|
| `src/app/admin/blog/page.tsx:27-28` | "Titles now act as primary edit links, and the new Notion view keeps document browsing beside the editor with local batch-selection scaffolding for future bulk actions." | `"Manage all blog posts. Click a title to edit."` |
| `src/app/admin/works/page.tsx:31-32` | "Click a title to edit directly, or create a new work and return to this list as soon as it saves." | `"Manage all portfolio works."` |

#### 1.2 구체적 코드 변경

**`src/app/admin/blog/page.tsx`**:

```tsx
// 변경 전
<p className="mt-2 text-sm text-muted-foreground">
    Titles now act as primary edit links, and the new Notion view keeps document browsing beside the editor with local batch-selection scaffolding for future bulk actions.
</p>

// 변경 후
<p className="mt-2 text-sm text-muted-foreground">
    Manage all blog posts. Click a title to edit.
</p>
```

**`src/app/admin/works/page.tsx`**:

```tsx
// 변경 전
<p className="mt-2 text-sm text-muted-foreground">
    Click a title to edit directly, or create a new work and return to this list as soon as it saves.
</p>

// 변경 후
<p className="mt-2 text-sm text-muted-foreground">
    Manage all portfolio works.
</p>
```

---

### Task 2. WorkEditor/BlogEditor 설명 텍스트 교체

**우선순위**: 1 (최우선)  
**난이도**: M  
**영향 파일 수**: 2개

#### 2.1 WorkEditor.tsx 변경 맵

| 위치 (대략) | 현재 | 변경 |
|------------|------|------|
| Media 섹션 설명문 (~1218행) | "Add thumbnail and icon assets with clear click-to-upload fields. Dragging a file onto the input still works, but it is no longer the only obvious path." | `"Upload thumbnail and icon images for this work."` |
| Videos 섹션 (editing 모드) | "You can add YouTube videos and MP4 uploads immediately. Ordering and removal update the work separately from the main form." | `"Add YouTube links or upload MP4 files. Video changes save immediately."` |
| Videos 섹션 (create 모드, non-inline) | "You can stage videos while creating a work. The app creates the work first, then attaches the staged videos in order." | `"Stage videos before saving. They'll be attached after the work is created."` |
| Videos 섹션 (create 모드, inline) | "You can stage videos while creating a work. Saving stays on this page, closes the create shell, and refreshes the works list after the videos attach." | `"Stage videos before saving. They'll be attached automatically."` |
| Content 힌트 박스 | "Write the public-facing project story here. New works save live immediately, so keep the summary and body ready before hitting create." | `"Write the project description shown on the public site."` |
| Footer 힌트 | "Saving creates a live work immediately, then returns you to the works list unless you choose the staged video flow." | **제거** |
| Thumbnail choose 힌트 | "Best for public cards. Click to browse, or drop an image onto the picker." | `"Recommended size: 800 × 600px"` |
| Icon choose 힌트 | "Use a square or simple mark for compact surfaces and metadata-driven sections." | `"Square image recommended."` |
| Category 힌트 | "New works default to Uncategorized so create is never blocked by categorization." | **제거** |

#### 2.2 BlogEditor.tsx 변경 맵

| 위치 | 현재 | 변경 |
|------|------|------|
| "New" 모드 publish 힌트 | "New posts publish immediately when you save. You can switch them back to draft later from the edit screen." | `"New posts go live immediately. Toggle 'Published' off to save as draft."` |
| Footer 힌트 | "Saving creates a live post immediately, then returns you to the blog list so you can keep editing the library." | **제거** |

#### 2.3 검증

```bash
# 개발자 어투 잔존 확인
grep -rn "scaffolding\|click-to-upload\|obvious path\|separately from\|staged video flow" src/components/admin/ src/app/admin/
# 기대: 0건
```

---

### Task 3. Raw Color → Semantic Token 전환

**우선순위**: 2  
**난이도**: M  
**영향 파일 수**: 4개  
**UI/UX Pro Max 규칙**: `color-semantic`, `color-contrast`, `color-dark-mode`

#### 3.1 일괄 치환 맵

| 찾기 | 바꾸기 | 파일 |
|------|--------|------|
| `text-gray-500` (단독, 날짜 셀) | `text-muted-foreground` | AdminBlogTableClient, AdminWorksTableClient |
| `text-sm text-gray-500` (미디어 설명) | `text-sm text-muted-foreground` | WorkEditor.tsx |
| `bg-white dark:bg-gray-950` (테이블 컨테이너) | `bg-background` | AdminBlogTableClient, AdminWorksTableClient |
| `border-gray-200 ... dark:border-gray-800` (테이블 border) | `border-border` | AdminBlogTableClient, AdminWorksTableClient |
| `bg-gray-100 dark:border-gray-800 dark:bg-gray-900` (이미지 placeholder) | `bg-muted` | WorkEditor.tsx |

#### 3.2 파일별 구체적 변경

**`src/components/admin/AdminBlogTableClient.tsx`**:

```
div 컨테이너: border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-950 → border-border bg-background
toolbar border-b: border-gray-200 dark:border-gray-800 → border-border
Date cell: text-sm text-gray-500 → text-sm text-muted-foreground
pagination border-t: border-gray-200 dark:border-gray-800 → border-border
```

**`src/components/admin/AdminWorksTableClient.tsx`**: (동일 패턴)

**`src/components/admin/WorkEditor.tsx`**:

```
이미지 placeholder: bg-gray-100 dark:border-gray-800 dark:bg-gray-900 → bg-muted dark:border-border
설명 텍스트: text-sm text-gray-500 → text-sm text-muted-foreground
Upload 상태: text-sm text-gray-500 → text-sm text-muted-foreground
```

#### 3.3 검증

```bash
grep -rn "text-gray-\|bg-gray-\|border-gray-" src/components/admin/AdminBlogTableClient.tsx src/components/admin/AdminWorksTableClient.tsx src/components/admin/WorkEditor.tsx src/components/admin/BlogEditor.tsx
# 기대: 0건 (Badge 내부 green/yellow 제외)
```

---

### Task 4. `window.alert()` → `toast.error()` 전환

**우선순위**: 3  
**난이도**: S  
**영향 파일 수**: 2개  
**UI/UX Pro Max 규칙**: `error-feedback`

#### 변경

**`src/components/admin/AdminBlogTableClient.tsx`** — `runDelete()` 내부:

```tsx
// 변경 전
window.alert(error instanceof Error ? error.message : 'Failed to delete blogs.')

// 변경 후
toast.error(error instanceof Error ? error.message : 'Failed to delete blogs.')
```

`import { toast } from 'sonner'` 추가 필요.

**`src/components/admin/AdminWorksTableClient.tsx`** — `runDelete()` 내부:

```tsx
// 변경 전
window.alert(error instanceof Error ? error.message : 'Failed to delete works.')

// 변경 후
toast.error(error instanceof Error ? error.message : 'Failed to delete works.')
```

`import { toast } from 'sonner'` 추가 필요.

#### 검증

```bash
grep -rn "window.alert" src/components/admin/
# 기대: 0건
```

---

## Phase 2: HIGH — UX 차이를 만드는 것 (3~4시간)

### Task 5. Callout 스타일 통일

**우선순위**: 4  
**난이도**: M  
**영향 파일 수**: 2개 (WorkEditor, BlogEditor)  
**UI/UX Pro Max 규칙**: `consistency`, `style-match`

#### 5.1 변경 규칙

`design-pattern-admin-0412.md` §1.3~1.4 참조. 요약:

| 현재 스타일 | 의미 | 변경 |
|------------|------|------|
| `sky-*` 계열 (업로드, 안내, video hint) | Info | `rounded-xl border border-border/80 bg-muted/50 px-4 py-3 text-sm text-muted-foreground` |
| `emerald-*` (publish 즉시 안내) | Info (안내) | 위와 동일 |
| `emerald-*` ("Placed in body", "Videos saved") | Success | **유지** |
| `amber-*` (orphan video) | Warning | **유지** |

#### 5.2 구체적 변경 (WorkEditor.tsx)

**업로드 가이드 박스** (thumbnail, icon):

```tsx
// 변경 전
<div className="rounded-xl border border-dashed border-sky-300 bg-sky-50/70 p-4 dark:border-sky-900 dark:bg-sky-950/20">
  <p className="mb-2 text-sm font-medium text-sky-900 dark:text-sky-100">Choose a thumbnail image</p>
  <p className="mb-3 text-xs text-sky-900/80 dark:text-sky-100/80">...</p>

// 변경 후
<div className="rounded-xl border border-dashed border-border/80 bg-muted/50 p-4">
  <p className="mb-2 text-sm font-medium text-foreground">Thumbnail</p>
  <p className="mb-3 text-xs text-muted-foreground">Recommended size: 800 × 600px</p>
```

**비디오 안내 박스** (editing 모드):

```tsx
// 변경 전
<div className="rounded-xl border border-sky-300 bg-sky-50 px-4 py-3 text-sm text-sky-900 dark:border-sky-900/60 dark:bg-sky-950/20 dark:text-sky-100">

// 변경 후
<div className="rounded-xl border border-border/80 bg-muted/50 px-4 py-3 text-sm text-muted-foreground">
```

**Content 힌트 박스**:

```tsx
// 변경 전
<div className="rounded-xl border border-dashed border-sky-300 bg-sky-50/70 px-4 py-3 text-sm text-sky-900 dark:border-sky-900 dark:bg-sky-950/20 dark:text-sky-100">

// 변경 후
<div className="rounded-xl border border-border/80 bg-muted/50 px-4 py-3 text-sm text-muted-foreground">
```

**"Publish immediately" 안내** (create 모드):

```tsx
// 변경 전
<div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900 dark:border-emerald-900/60 dark:bg-emerald-950/30 dark:text-emerald-100 md:ml-auto">

// 변경 후
<div className="rounded-xl border border-border/80 bg-muted/50 px-4 py-3 text-sm text-muted-foreground md:ml-auto">
```

#### 5.3 검증

```bash
# sky 계열 callout 잔존 확인
grep -rn "border-sky-\|bg-sky-\|text-sky-" src/components/admin/WorkEditor.tsx src/components/admin/BlogEditor.tsx
# 기대: 0건
```

---

### Task 6. Works 테이블에 썸네일 컬럼 추가

**우선순위**: 5  
**난이도**: M  
**영향 파일 수**: 2개 (page, table client)  
**UI/UX Pro Max 규칙**: `image-dimension`, `content-jumping`

#### 6.1 데이터 확장

`WorkAdminItem` 타입에 `thumbnailUrl`이 이미 있는지 확인 필요. 없다면 API 응답에 추가.

#### 6.2 테이블 변경

**`src/components/admin/AdminWorksTableClient.tsx`**:

TableHeader에 Thumbnail 열 추가:

```tsx
<TableHead className="w-16">Thumbnail</TableHead>
```

TableBody에 이미지 셀 추가:

```tsx
<TableCell>
  <div className="relative h-10 w-14 overflow-hidden rounded-md bg-muted">
    {work.thumbnailUrl ? (
      <Image
        src={work.thumbnailUrl}
        alt=""
        fill
        unoptimized
        className="object-cover"
      />
    ) : (
      <div className="flex h-full w-full items-center justify-center text-muted-foreground">
        <Briefcase className="h-4 w-4" />
      </div>
    )}
  </div>
</TableCell>
```

`import Image from 'next/image'` 및 `import { Briefcase } from 'lucide-react'` 추가 필요.

colSpan 업데이트: `colSpan={6}` → `colSpan={7}` (empty state)

---

### Task 7. 페이지네이션 간소화

**우선순위**: 6  
**난이도**: S  
**영향 파일 수**: 2개  
**UI/UX Pro Max 규칙**: `primary-action`, `touch-target-size`

#### 변경 (AdminBlogTableClient, AdminWorksTableClient 동일)

```tsx
// 변경 전: 5개 버튼
<Button ... aria-label="처음">First</Button>
<Button ... aria-label="이전">Previous</Button>
<span>1 / 5</span>
<Button ... aria-label="다음">Next</Button>
<Button ... aria-label="끝">Last</Button>

// 변경 후: 3개 버튼
<Button
  type="button"
  variant="outline"
  size="sm"
  aria-label="Previous page"
  disabled={currentPage <= 1}
  onClick={() => setPage((p) => Math.max(1, p - 1))}
>
  <ChevronLeft className="mr-1 h-4 w-4" />
  Previous
</Button>
<span className="text-sm text-muted-foreground">
  Page {currentPage} of {totalPages}
</span>
<Button
  type="button"
  variant="outline"
  size="sm"
  aria-label="Next page"
  disabled={currentPage >= totalPages}
  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
>
  Next
  <ChevronRight className="ml-1 h-4 w-4" />
</Button>
```

`import { ChevronLeft, ChevronRight } from 'lucide-react'` 추가 필요.

---

### Task 8. 비디오 버튼 계층화

**우선순위**: 7  
**난이도**: M  
**영향 파일 수**: 1개 (WorkEditor.tsx)  
**UI/UX Pro Max 규칙**: `primary-action`, `state-clarity`, `touch-target-size`

#### 8.1 저장된 비디오 카드 액션 버튼 변경

```tsx
// 변경 전: 5개 outline 버튼 동일 가중치
<Button variant="outline" ...>Insert Into Body</Button>
<Button variant="outline" ...>Remove From Body</Button>
<Button variant="outline" ...>Move Up</Button>
<Button variant="outline" ...>Move Down</Button>
<Button variant="outline" ...>Remove</Button>

// 변경 후: 계층화
<div className="flex flex-wrap items-center gap-1">
  {/* 정렬: 아이콘 버튼 */}
  <Button type="button" variant="ghost" size="icon"
    onClick={() => void reorderSavedVideo(video.id, -1)}
    disabled={isVideoBusy || index === 0}
    aria-label="Move up">
    <ChevronUp className="h-4 w-4" />
  </Button>
  <Button type="button" variant="ghost" size="icon"
    onClick={() => void reorderSavedVideo(video.id, 1)}
    disabled={isVideoBusy || index === videos.length - 1}
    aria-label="Move down">
    <ChevronDown className="h-4 w-4" />
  </Button>

  <div className="mx-2 h-4 w-px bg-border" /> {/* separator */}

  {/* 핵심 동작: 강조 */}
  {embeddedVideoIdSet.has(video.id) ? (
    <Button type="button" variant="outline" size="sm"
      onClick={() => removeSavedVideoFromBody(video.id)}
      disabled={isVideoBusy}>
      Remove from Body
    </Button>
  ) : (
    <Button type="button" variant="default" size="sm"
      onClick={() => insertSavedVideoIntoBody(video.id)}
      disabled={isVideoBusy}>
      Insert into Body
    </Button>
  )}

  {/* 삭제: destructive 아이콘 */}
  <Button type="button" variant="ghost" size="icon"
    className="text-destructive hover:text-destructive"
    onClick={() => void removeSavedVideo(video.id)}
    disabled={isVideoBusy}
    aria-label="Remove video">
    <Trash2 className="h-4 w-4" />
  </Button>
</div>
```

#### 8.2 "Placed in body" 상태 표시 변경

```tsx
// 변경 전: 인라인 callout box
<div className="rounded-xl border border-emerald-300 bg-emerald-50 px-3 py-2 text-xs ...">
  Placed in body. Remove it from the body before deleting the saved video.
</div>

// 변경 후: 인라인 Badge
<Badge variant="secondary" className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300">
  In body
</Badge>
```

---

### Task 9. Create 버튼 통합

**우선순위**: 8  
**난이도**: S  
**영향 파일 수**: 1개 (WorkEditor.tsx)

#### 변경

```tsx
// 변경 전: 2개 분리 버튼
<Button variant="outline" onClick={() => void saveWork('default')}
  disabled={isSaving || !isDirty || !title.trim() || hasStagedVideos}>
  {isSaving ? 'Saving...' : 'Create Work'}
</Button>
<Button onClick={() => void saveWork('with-videos')}
  disabled={isSaving || !isDirty || !title.trim() || !hasStagedVideos}
  className="px-8 font-medium">
  {isSaving ? 'Creating...' : 'Create And Add Videos'}
</Button>

// 변경 후: 1개 통합 버튼
<Button
  type="button"
  onClick={() => void saveWork(hasStagedVideos ? 'with-videos' : 'default')}
  disabled={isSaving || !isDirty || !title.trim()}
  className="px-8 font-medium"
>
  {isSaving
    ? 'Creating...'
    : hasStagedVideos
      ? `Create with ${stagedVideos.length} Video${stagedVideos.length > 1 ? 's' : ''}`
      : 'Create Work'}
</Button>
```

---

## Phase 3: MEDIUM — 구조적 품질 (향후)

### Task 10. WorkEditor 컴포넌트 분리

**우선순위**: 9  
**난이도**: L (대규모)  
**영향 파일 수**: 6~8개 생성/수정

> ⚠️ 이 작업은 리팩토링이므로, **Phase 1~2 완료 후** 진행.  
> 반드시 기존 동작을 regression test로 보호한 후 실행.

#### 10.1 분리 계획

| 서브 컴포넌트 | 추출 대상 | 예상 줄 수 |
|--------------|----------|-----------|
| `WorkGeneralSection.tsx` | title, category, period, tags, published, dates, metadata | ~180줄 |
| `WorkMediaUploader.tsx` | thumbnail/icon upload, preview, remove | ~120줄 |
| `WorkVideoManager.tsx` | YouTube/MP4 add, list, reorder, remove, insert/remove body | ~250줄 |
| `WorkContentSection.tsx` | TiptapEditor, AI fix, hints | ~80줄 |
| `WorkEditorActions.tsx` | save/cancel buttons | ~60줄 |

#### 10.2 Custom Hook 추출

| Hook | 추출 대상 |
|------|----------|
| `useWorkVideos.ts` | addYouTube, uploadVideo, removeVideo, reorderVideo, insertIntoBody, removeFromBody, stagedVideos 관리 |
| `useWorkAssetUpload.ts` | uploadAssetFile, uploadWorkImage, removeWorkImage |
| `useWorkAutoThumbnail.ts` | maybeApplyAutoThumbnailForCandidate, thumbnail resolution 로직 |

---

### Task 11. 탭을 실제 탭으로 전환

**우선순위**: 10  
**난이도**: M  
**영향 파일 수**: 1개 (WorkEditor.tsx → 분리 후 반영)

#### 변경

```tsx
// 변경 전: 3개 섹션 동시 렌더링 + scrollIntoView
<div ref={generalSectionRef} className={cn(..., activeTab === 'general' && 'ring-2 ...')}>
  {/* general 항상 렌더 */}
</div>
<div ref={mediaSectionRef} className={cn(..., activeTab === 'media' && 'ring-2 ...')}>
  {/* media 항상 렌더 */}
</div>
<div ref={contentSectionRef} className={cn(..., activeTab === 'content' && 'ring-2 ...')}>
  {/* content 항상 렌더 */}
</div>

// 변경 후: 조건부 렌더링
{activeTab === 'general' && <WorkGeneralSection ... />}
{activeTab === 'media' && (
  <>
    <WorkMediaUploader ... />
    <WorkVideoManager ... />
  </>
)}
{activeTab === 'content' && <WorkContentSection ... />}
```

**주의사항**:
- 모든 state는 상위 `WorkEditor`에서 관리 (탭 전환 시 유실 방지)
- `TiptapEditor`는 unmount/remount 시 상태 복원 확인 필요
- `ref` 기반 scrollIntoView 코드 제거

---

### Task 12. Metadata 위치 이동

**우선순위**: 11  
**난이도**: S

`Flexible Metadata` 섹션을 Media 탭 → **General 탭** 하단으로 이동.

Task 10, 11과 함께 실행.

---

### Task 13. Blog 테이블 Tags Badge화

**우선순위**: 12  
**난이도**: S

```tsx
// 변경 전
<TableCell>{blog.tags?.join(', ')}</TableCell>

// 변경 후
<TableCell>
  <div className="flex flex-wrap gap-1">
    {blog.tags?.slice(0, 3).map((tag) => (
      <Badge key={tag} variant="secondary" className="text-xs font-normal">
        {tag}
      </Badge>
    ))}
    {(blog.tags?.length ?? 0) > 3 && (
      <Badge variant="outline" className="text-xs font-normal">
        +{(blog.tags?.length ?? 0) - 3}
      </Badge>
    )}
  </div>
</TableCell>
```

---

## 실행 후 검증 체크리스트

### 자동 검증

```bash
# 1. 개발자 어투 잔존
grep -rn "scaffolding\|click-to-upload\|obvious path\|separately from\|staged video flow\|batch-selection" src/components/admin/ src/app/admin/
# 기대: 0건

# 2. window.alert 잔존
grep -rn "window.alert" src/components/admin/
# 기대: 0건

# 3. raw gray color 잔존 (Badge 제외)
grep -rn "text-gray-\|bg-gray-\|border-gray-" src/components/admin/AdminBlogTableClient.tsx src/components/admin/AdminWorksTableClient.tsx | grep -v "bg-green\|bg-yellow"
# 기대: 0건

# 4. sky callout 잔존
grep -rn "border-sky-\|bg-sky-\|text-sky-" src/components/admin/WorkEditor.tsx src/components/admin/BlogEditor.tsx
# 기대: 0건

# 5. 한국어 aria-label 잔존
grep -rn 'aria-label="처음\|aria-label="이전\|aria-label="다음\|aria-label="끝' src/components/admin/
# 기대: 0건

# 6. TypeScript 빌드
npx tsc --noEmit

# 7. 린트
npx next lint
```

### 수동(시각) 검증

| 항목 | 확인 방법 |
|------|----------|
| 라이트/다크 모드 전환 | 모든 admin 텍스트가 semantic token으로 동작하는지 |
| 설명 텍스트 | 개발자 어투가 완전히 사라졌는지 |
| Callout 색상 | 안내 = muted, 성공 = emerald, 경고 = amber로 통일되었는지 |
| 삭제 에러 | toast로 표시되는지 (window.alert 아닌지) |
| 페이지네이션 | Previous / Page X of Y / Next 3개만 있는지 |
| Works 리스트 | 썸네일 컬럼이 보이는지 (Phase 2 이후) |
| 비디오 버튼 | Insert가 강조, Move가 아이콘, Remove가 destructive인지 |
| Create Work | 버튼 1개로 통합되었는지 |

---

## 예상 결과

| 항목 | Before | After (Phase 1+2) | After (All) |
|------|--------|-------------------|-------------|
| 프로페셔널함 | **3/10** | **6/10** | **8/10** |
| 시각 디자인 | **5/10** | **7/10** | **8/10** |
| 정보 구조 | **4/10** | **5/10** | **7/10** |
| UX 흐름 | **5/10** | **7/10** | **8/10** |
| 코드 퀄리티 | **4/10** | **6/10** | **8/10** |

---

## 타임라인

| Phase | 작업 | Tasks | 예상 |
|-------|------|-------|------|
| **Phase 1** | CRITICAL — 텍스트 교체, Token 전환, alert 제거 | Task 1, 2, 3, 4 | 1~2시간 |
| **Phase 2** | HIGH — Callout 통일, 썸네일, 페이지네이션, 비디오 UX, 버튼 통합 | Task 5, 6, 7, 8, 9 | 3~4시간 |
| **Phase 3** | MEDIUM — 컴포넌트 분리, 실제 탭, Metadata 이동, Tags Badge | Task 10, 11, 12, 13 | 별도 세션 |
| **검증** | 자동 + 수동 테스트 | 체크리스트 전수 | 각 Phase 후 |

**실행 순서 원칙**: Phase 1 완료 → 검증 → Phase 2 진입. Phase 3은 regression test 보호 후 진행.
