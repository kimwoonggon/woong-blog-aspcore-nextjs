# Blog / Work 검색 & 카테고리 필터 설계서

> **작성일**: 2026-04-12
> **범위**: Public Blog 페이지, Public Work 페이지에 검색 + 카테고리(태그) 필터 기능 추가
> **참고 UI**: HongLab Products 페이지 (카테고리 탭 + 검색바)

---

## 1. 현재 상태 분석

### 1.1 현재 API 지원 범위

| 엔드포인트 | 현재 파라미터 | 비고 |
|-----------|-------------|------|
| `GET /api/public/blogs` | `page`, `pageSize` | 검색/필터 없음 |
| `GET /api/public/works` | `page`, `pageSize` | 검색/필터 없음 |

### 1.2 현재 데이터 모델

```
Blog: Tags(string[]), Title, Excerpt, ContentJson
Work: Tags(string[]), Category(string), Title, Excerpt, ContentJson
```

- **Blog**: `Tags` 필드가 있지만, API에서 필터링 미지원
- **Work**: `Category` + `Tags` 필드가 있지만, API에서 필터링 미지원
- 검색 기능 전혀 없음

### 1.3 현재 프론트엔드 구조

- `src/app/(public)/blog/page.tsx` — 서버 컴포넌트, `fetchPublicBlogs(page, pageSize)` 호출
- `src/app/(public)/works/page.tsx` — 서버 컴포넌트, `fetchPublicWorks(page, pageSize)` 호출
- 페이지네이션은 URL 쿼리 기반 (`?page=2`)
- 반응형 페이지 사이즈: Desktop / Tablet / Mobile

---

## 2. 목표 기능

### 2.1 검색 기능

첨부 이미지처럼 페이지 우측 상단에 검색바를 배치하여:
- **Title** + **Excerpt** 를 대상으로 키워드 검색
- 입력 후 Enter 또는 검색 아이콘 클릭으로 검색 실행
- URL 쿼리 파라미터에 반영 (`?search=키워드`)
- 검색 시 페이지네이션 1페이지로 리셋
- 빈 검색어 → 전체 목록으로 복원

### 2.2 카테고리/태그 필터 탭

첨부 이미지처럼 페이지 타이틀 아래에 **카테고리 탭** 배치:

**Blog 페이지:**
- 탭 예시: `전체` | `C#` | `C++` | `Python` | `AI` | `Web` 등
- Tags 기반 필터링 (Blog 엔티티는 Category 필드가 없으므로 Tags 활용)
- "전체" 선택 시 필터 해제

**Work 페이지:**
- 탭 예시: `전체` | `Web` | `Game` | `AI` | `Graphics` 등
- Category 필드 기반 필터링 (Work 엔티티에 Category 존재)
- 선택적으로 Tags 기반 2차 필터 추가 가능

**공통:**
- URL 쿼리 파라미터에 반영 (`?tag=C%23` 또는 `?category=Web`)
- 카테고리/태그 변경 시 페이지네이션 1페이지로 리셋
- 검색과 카테고리 필터 동시 적용 가능

---

## 3. 백엔드 설계

### 3.1 API 엔드포인트 변경

#### `GET /api/public/blogs` 파라미터 확장

| 파라미터 | 타입 | 기본값 | 설명 |
|---------|------|-------|------|
| `page` | int? | 1 | 페이지 번호 |
| `pageSize` | int? | 10 | 페이지 크기 |
| **`search`** | string? | null | 제목+요약 검색 키워드 |
| **`tag`** | string? | null | 태그 필터 (정확 매칭) |

#### `GET /api/public/works` 파라미터 확장

| 파라미터 | 타입 | 기본값 | 설명 |
|---------|------|-------|------|
| `page` | int? | 1 | 페이지 번호 |
| `pageSize` | int? | 6 | 페이지 크기 |
| **`search`** | string? | null | 제목+요약 검색 키워드 |
| **`category`** | string? | null | 카테고리 필터 (정확 매칭) |
| **`tag`** | string? | null | 태그 필터 (정확 매칭) |

#### 추가 엔드포인트: 사용 가능한 태그/카테고리 목록 조회

| 엔드포인트 | 설명 |
|-----------|------|
| `GET /api/public/blogs/tags` | 발행된 블로그에서 사용되는 모든 태그 + 각 태그별 게시물 수 |
| `GET /api/public/works/categories` | 발행된 작업물에서 사용되는 모든 카테고리 + 각 카테고리별 게시물 수 |
| `GET /api/public/works/tags` | 발행된 작업물에서 사용되는 모든 태그 + 각 태그별 게시물 수 |

---

### 3.2 수정할 파일 목록 (백엔드)

#### A. GetBlogsRequest.cs 수정

```
파일: backend/src/WoongBlog.Api/Modules/Content/Blogs/Api/GetBlogs/GetBlogsRequest.cs
```

```csharp
internal sealed class GetBlogsRequest
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Search { get; init; }    // ← 추가
    public string? Tag { get; init; }       // ← 추가

    internal GetBlogsQuery ToQuery() => new(
        Page ?? 1,
        PageSize ?? 10,
        Search?.Trim(),        // ← 추가
        Tag?.Trim()            // ← 추가
    );
}
```

#### B. GetBlogsQuery.cs 수정

```
파일: backend/src/WoongBlog.Api/Modules/Content/Blogs/Application/GetBlogs/GetBlogsQuery.cs
```

```csharp
public sealed record GetBlogsQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,    // ← 추가
    string? Tag = null        // ← 추가
) : IRequest<PagedBlogsDto>;
```

#### C. PublicBlogService.cs - GetBlogsAsync 수정

```
파일: backend/src/WoongBlog.Api/Modules/Content/Blogs/Persistence/PublicBlogService.cs
```

```csharp
public async Task<PagedBlogsDto> GetBlogsAsync(GetBlogsQuery queryInput, CancellationToken cancellationToken)
{
    var assets = await _dbContext.Assets.AsNoTracking()
        .ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);
    var pageSize = Math.Max(1, queryInput.PageSize);
    var requestedPage = Math.Max(1, queryInput.Page);

    IQueryable<Blog> query = _dbContext.Blogs.AsNoTracking()
        .Where(x => x.Published);

    // ── 검색 필터 ──
    if (!string.IsNullOrWhiteSpace(queryInput.Search))
    {
        var searchLower = queryInput.Search.ToLowerInvariant();
        query = query.Where(x =>
            x.Title.ToLower().Contains(searchLower) ||
            x.Excerpt.ToLower().Contains(searchLower));
    }

    // ── 태그 필터 ──
    if (!string.IsNullOrWhiteSpace(queryInput.Tag))
    {
        query = query.Where(x => x.Tags.Contains(queryInput.Tag));
    }

    query = query.OrderByDescending(x => x.PublishedAt);

    var totalItems = await query.CountAsync(cancellationToken);
    var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
    var page = Math.Min(requestedPage, totalPages);

    var blogs = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    // ... (기존 매핑 로직 동일)
}
```

> **PostgreSQL 참고**: `string[].Contains()` 는 EF Core + Npgsql에서 `= ANY(tags)` 로 변환됨. 현재 Tags는 `text[]` 타입이므로 바로 사용 가능.

#### D. GetWorksRequest.cs 수정

```
파일: backend/src/WoongBlog.Api/Modules/Content/Works/Api/GetWorks/GetWorksRequest.cs
```

```csharp
internal sealed class GetWorksRequest
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Search { get; init; }     // ← 추가
    public string? Category { get; init; }   // ← 추가
    public string? Tag { get; init; }        // ← 추가

    internal GetWorksQuery ToQuery() => new(
        Page ?? 1,
        PageSize ?? 6,
        Search?.Trim(),        // ← 추가
        Category?.Trim(),      // ← 추가
        Tag?.Trim()            // ← 추가
    );
}
```

#### E. GetWorksQuery.cs 수정

```
파일: backend/src/WoongBlog.Api/Modules/Content/Works/Application/GetWorks/GetWorksQuery.cs
```

```csharp
public sealed record GetWorksQuery(
    int Page = 1,
    int PageSize = 6,
    string? Search = null,     // ← 추가
    string? Category = null,   // ← 추가
    string? Tag = null         // ← 추가
) : IRequest<PagedWorksDto>;
```

#### F. PublicWorkService.cs - GetWorksAsync 수정

```
파일: backend/src/WoongBlog.Api/Modules/Content/Works/Persistence/PublicWorkService.cs
```

```csharp
public async Task<PagedWorksDto> GetWorksAsync(GetWorksQuery queryInput, CancellationToken cancellationToken)
{
    var assets = await _dbContext.Assets.AsNoTracking()
        .ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);
    var pageSize = Math.Max(1, queryInput.PageSize);
    var requestedPage = Math.Max(1, queryInput.Page);

    IQueryable<Work> query = _dbContext.Works.AsNoTracking()
        .Where(x => x.Published);

    // ── 검색 필터 ──
    if (!string.IsNullOrWhiteSpace(queryInput.Search))
    {
        var searchLower = queryInput.Search.ToLowerInvariant();
        query = query.Where(x =>
            x.Title.ToLower().Contains(searchLower) ||
            x.Excerpt.ToLower().Contains(searchLower));
    }

    // ── 카테고리 필터 ──
    if (!string.IsNullOrWhiteSpace(queryInput.Category))
    {
        query = query.Where(x => x.Category == queryInput.Category);
    }

    // ── 태그 필터 ──
    if (!string.IsNullOrWhiteSpace(queryInput.Tag))
    {
        query = query.Where(x => x.Tags.Contains(queryInput.Tag));
    }

    query = query.OrderByDescending(x => x.PublishedAt);

    var totalItems = await query.CountAsync(cancellationToken);
    // ... (나머지 기존 로직 동일)
}
```

#### G. 새 엔드포인트: 태그/카테고리 목록 조회

**Blog Tags 엔드포인트:**

```
새 파일: backend/src/WoongBlog.Api/Modules/Content/Blogs/Api/GetBlogTags/GetBlogTagsEndpoint.cs
```

```csharp
internal static class GetBlogTagsEndpoint
{
    internal static void MapGetBlogTags(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public/blogs/tags", async (
                WoongBlogDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var tags = await dbContext.Blogs.AsNoTracking()
                    .Where(x => x.Published)
                    .SelectMany(x => x.Tags)
                    .GroupBy(tag => tag)
                    .Select(g => new TagCountDto(g.Key, g.Count()))
                    .OrderByDescending(x => x.Count)
                    .ToListAsync(cancellationToken);

                return Results.Ok(tags);
            })
            .WithTags("Public Blogs")
            .WithName("GetBlogTags")
            .Produces<List<TagCountDto>>(StatusCodes.Status200OK);
    }
}

public sealed record TagCountDto(string Name, int Count);
```

**Work Categories 엔드포인트:**

```
새 파일: backend/src/WoongBlog.Api/Modules/Content/Works/Api/GetWorkCategories/GetWorkCategoriesEndpoint.cs
```

```csharp
internal static class GetWorkCategoriesEndpoint
{
    internal static void MapGetWorkCategories(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public/works/categories", async (
                WoongBlogDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var categories = await dbContext.Works.AsNoTracking()
                    .Where(x => x.Published)
                    .Where(x => x.Category != "")
                    .GroupBy(x => x.Category)
                    .Select(g => new CategoryCountDto(g.Key, g.Count()))
                    .OrderByDescending(x => x.Count)
                    .ToListAsync(cancellationToken);

                return Results.Ok(categories);
            })
            .WithTags("Public Works")
            .WithName("GetWorkCategories")
            .Produces<List<CategoryCountDto>>(StatusCodes.Status200OK);
    }
}

public sealed record CategoryCountDto(string Name, int Count);
```

**Work Tags 엔드포인트:**

```
새 파일: backend/src/WoongBlog.Api/Modules/Content/Works/Api/GetWorkTags/GetWorkTagsEndpoint.cs
```

```csharp
internal static class GetWorkTagsEndpoint
{
    internal static void MapGetWorkTags(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public/works/tags", async (
                WoongBlogDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var tags = await dbContext.Works.AsNoTracking()
                    .Where(x => x.Published)
                    .SelectMany(x => x.Tags)
                    .GroupBy(tag => tag)
                    .Select(g => new TagCountDto(g.Key, g.Count()))
                    .OrderByDescending(x => x.Count)
                    .ToListAsync(cancellationToken);

                return Results.Ok(tags);
            })
            .WithTags("Public Works")
            .WithName("GetWorkTags")
            .Produces<List<TagCountDto>>(StatusCodes.Status200OK);
    }
}
```

#### H. Program.cs 라우트 등록

새 엔드포인트들을 `Program.cs`의 기존 라우트 매핑 부분에 추가:

```csharp
// 기존
app.MapGetBlogs();
app.MapGetBlogBySlug();
// 추가
app.MapGetBlogTags();

// 기존
app.MapGetWorks();
app.MapGetWorkBySlug();
// 추가
app.MapGetWorkCategories();
app.MapGetWorkTags();
```

---

### 3.3 DB 인덱스 (선택 사항, 성능 최적화)

데이터가 수천~수만 건 이상이 되면 추가를 고려:

```sql
-- 검색 성능 향상 (Full-text search 미적용 시)
CREATE INDEX idx_blogs_published_title_excerpt
ON "Blogs" USING gin (to_tsvector('simple', "Title" || ' ' || "Excerpt"))
WHERE "Published" = true;

-- 태그 검색 성능 향상
CREATE INDEX idx_blogs_tags_gin ON "Blogs" USING gin ("Tags")
WHERE "Published" = true;

-- Work 카테고리 인덱스
CREATE INDEX idx_works_category ON "Works" ("Category")
WHERE "Published" = true;

CREATE INDEX idx_works_tags_gin ON "Works" USING gin ("Tags")
WHERE "Published" = true;
```

> **참고**: 현재 데이터 양이 적다면 인덱스 없이도 충분합니다. 나중에 느려지면 추가.

---

### 3.4 API 응답 예시

#### `GET /api/public/blogs?search=python&tag=AI&page=1&pageSize=10`

```json
{
  "items": [
    {
      "id": "...",
      "slug": "python-ai-tutorial",
      "title": "Python AI 튜토리얼",
      "excerpt": "Python을 활용한 AI 기초...",
      "tags": ["Python", "AI"],
      "coverUrl": "",
      "publishedAt": "2026-04-10T09:00:00+00:00"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalItems": 1,
  "totalPages": 1
}
```

#### `GET /api/public/blogs/tags`

```json
[
  { "name": "C#", "count": 15 },
  { "name": "Python", "count": 12 },
  { "name": "C++", "count": 8 },
  { "name": "AI", "count": 7 },
  { "name": "Web", "count": 5 }
]
```

#### `GET /api/public/works/categories`

```json
[
  { "name": "Web", "count": 10 },
  { "name": "Game", "count": 7 },
  { "name": "AI", "count": 5 },
  { "name": "Graphics", "count": 3 }
]
```

---

## 4. 프론트엔드 설계

### 4.1 URL 쿼리 파라미터 설계

**Blog 페이지** (`/blog`):
```
/blog                                  → 전체 목록, 1페이지
/blog?page=2                           → 전체 목록, 2페이지
/blog?tag=C%23                         → C# 태그 필터, 1페이지
/blog?tag=Python&page=2                → Python 태그 필터, 2페이지
/blog?search=튜토리얼                    → "튜토리얼" 검색, 1페이지
/blog?search=AI&tag=Python             → "AI" 검색 + Python 태그 필터
/blog?search=AI&tag=Python&page=2      → 검색 + 필터 + 2페이지
```

**Work 페이지** (`/works`):
```
/works                                 → 전체 목록, 1페이지
/works?category=Web                    → Web 카테고리, 1페이지
/works?category=AI&search=머신러닝      → AI 카테고리 + 검색
/works?category=Game&page=2            → Game 카테고리, 2페이지
```

### 4.2 수정/추가할 프론트엔드 파일 목록

```
수정: src/lib/api/blogs.ts           ← fetchPublicBlogs에 search, tag 파라미터 추가
수정: src/lib/api/works.ts           ← fetchPublicWorks에 search, category, tag 파라미터 추가
추가: src/lib/api/blogs.ts           ← fetchBlogTags() 함수 추가
추가: src/lib/api/works.ts           ← fetchWorkCategories(), fetchWorkTags() 함수 추가
추가: src/components/content/ContentSearch.tsx       ← 검색바 컴포넌트
추가: src/components/content/CategoryTabs.tsx        ← 카테고리/태그 탭 컴포넌트
수정: src/app/(public)/blog/page.tsx               ← 검색바 + 태그 탭 통합
수정: src/app/(public)/works/page.tsx              ← 검색바 + 카테고리 탭 통합
```

---

### 4.3 프론트엔드 API 함수 수정

#### `src/lib/api/blogs.ts` 수정

```typescript
// ── 기존 fetchPublicBlogs 시그니처 확장 ──
export interface FetchBlogsOptions {
  page?: number
  pageSize?: number
  search?: string
  tag?: string
}

export async function fetchPublicBlogs(options: FetchBlogsOptions = {}) {
  const { page = 1, pageSize = 10, search, tag } = options
  const apiBaseUrl = await getServerApiBaseUrl()

  const params = new URLSearchParams()
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))
  if (search) params.set('search', search)
  if (tag) params.set('tag', tag)

  const response = await fetch(
    `${apiBaseUrl}/public/blogs?${params.toString()}`,
    { cache: 'no-store' }
  )
  if (!response.ok) {
    return {
      items: [],
      page,
      pageSize,
      totalItems: 0,
      totalPages: 1,
    } satisfies PagedBlogsPayload
  }
  return response.json() as Promise<PagedBlogsPayload>
}

// ── 새 함수: 태그 목록 조회 ──
export interface TagCount {
  name: string
  count: number
}

export async function fetchBlogTags(): Promise<TagCount[]> {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/blogs/tags`, {
    cache: 'no-store',
  })
  if (!response.ok) return []
  return response.json() as Promise<TagCount[]>
}
```

> **주의**: `fetchPublicBlogs`의 시그니처가 바뀌므로, 기존 호출부 (`blog/page.tsx`, `fetchAllPublicBlogs`)도 함께 수정해야 합니다.

#### `src/lib/api/works.ts` 수정

```typescript
// ── 기존 fetchPublicWorks 시그니처 확장 ──
export interface FetchWorksOptions {
  page?: number
  pageSize?: number
  search?: string
  category?: string
  tag?: string
}

export async function fetchPublicWorks(options: FetchWorksOptions = {}) {
  const { page = 1, pageSize = 6, search, category, tag } = options
  const apiBaseUrl = await getServerApiBaseUrl()

  const params = new URLSearchParams()
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))
  if (search) params.set('search', search)
  if (category) params.set('category', category)
  if (tag) params.set('tag', tag)

  const response = await fetch(
    `${apiBaseUrl}/public/works?${params.toString()}`,
    { cache: 'no-store' }
  )
  if (!response.ok) {
    return {
      items: [],
      page,
      pageSize,
      totalItems: 0,
      totalPages: 1,
    } satisfies PagedWorksPayload
  }
  return response.json() as Promise<PagedWorksPayload>
}

// ── 새 함수: 카테고리 목록 조회 ──
export interface CategoryCount {
  name: string
  count: number
}

export async function fetchWorkCategories(): Promise<CategoryCount[]> {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/works/categories`, {
    cache: 'no-store',
  })
  if (!response.ok) return []
  return response.json() as Promise<CategoryCount[]>
}

// ── 새 함수: 태그 목록 조회 ──
export interface TagCount {
  name: string
  count: number
}

export async function fetchWorkTags(): Promise<TagCount[]> {
  const apiBaseUrl = await getServerApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/public/works/tags`, {
    cache: 'no-store',
  })
  if (!response.ok) return []
  return response.json() as Promise<TagCount[]>
}
```

---

### 4.4 새 컴포넌트 설계

#### A. `ContentSearch.tsx` — 검색바 컴포넌트

```
파일: src/components/content/ContentSearch.tsx
타입: Client Component ("use client")
```

```tsx
"use client"

import { useRouter, useSearchParams } from "next/navigation"
import { useState, useCallback } from "react"
import { Search } from "lucide-react"

interface ContentSearchProps {
  /** 기본 경로 (예: "/blog" 또는 "/works") */
  basePath: string
  /** 검색 placeholder 텍스트 */
  placeholder?: string
}

export default function ContentSearch({ basePath, placeholder = "Search" }: ContentSearchProps) {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [value, setValue] = useState(searchParams.get("search") ?? "")

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault()
      const params = new URLSearchParams(searchParams.toString())

      if (value.trim()) {
        params.set("search", value.trim())
      } else {
        params.delete("search")
      }
      // 검색 시 항상 1페이지로 리셋
      params.delete("page")

      router.push(`${basePath}?${params.toString()}`)
    },
    [basePath, router, searchParams, value]
  )

  return (
    <form onSubmit={handleSubmit} className="relative w-full max-w-sm">
      <input
        type="text"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        placeholder={placeholder}
        className="w-full rounded-md border border-muted-foreground/30 bg-background
                   px-4 py-2 pr-10 text-sm
                   focus:border-brand-accent focus:outline-none focus:ring-1 focus:ring-brand-accent"
      />
      <button
        type="submit"
        className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground
                   hover:text-brand-accent transition-colors"
        aria-label="검색"
      >
        <Search className="h-4 w-4" />
      </button>
    </form>
  )
}
```

#### B. `CategoryTabs.tsx` — 카테고리/태그 필터 탭

```
파일: src/components/content/CategoryTabs.tsx
타입: Client Component ("use client")
```

```tsx
"use client"

import { useRouter, useSearchParams } from "next/navigation"
import { useCallback } from "react"
import { cn } from "@/lib/utils"

interface CategoryTabsProps {
  /** 기본 경로 (예: "/blog" 또는 "/works") */
  basePath: string
  /** 탭에 표시할 항목 목록 */
  items: { name: string; count: number }[]
  /** URL 쿼리 파라미터 키 (예: "tag" 또는 "category") */
  paramKey: string
  /** "전체" 탭 라벨 */
  allLabel?: string
}

export default function CategoryTabs({
  basePath,
  items,
  paramKey,
  allLabel = "전체",
}: CategoryTabsProps) {
  const router = useRouter()
  const searchParams = useSearchParams()
  const activeValue = searchParams.get(paramKey)

  const handleSelect = useCallback(
    (value: string | null) => {
      const params = new URLSearchParams(searchParams.toString())

      if (value) {
        params.set(paramKey, value)
      } else {
        params.delete(paramKey)
      }
      // 카테고리 변경 시 1페이지로 리셋
      params.delete("page")

      router.push(`${basePath}?${params.toString()}`)
    },
    [basePath, paramKey, router, searchParams]
  )

  return (
    <nav className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm">
      {/* "전체" 탭 */}
      <button
        type="button"
        onClick={() => handleSelect(null)}
        className={cn(
          "pb-0.5 transition-colors hover:text-brand-accent",
          !activeValue
            ? "border-b-2 border-brand-accent font-semibold text-brand-accent"
            : "text-muted-foreground"
        )}
      >
        {allLabel}
      </button>

      {/* 개별 카테고리/태그 탭 */}
      {items.map((item) => (
        <button
          key={item.name}
          type="button"
          onClick={() => handleSelect(item.name)}
          className={cn(
            "pb-0.5 transition-colors hover:text-brand-accent",
            activeValue === item.name
              ? "border-b-2 border-brand-accent font-semibold text-brand-accent"
              : "text-muted-foreground"
          )}
        >
          {item.name}
        </button>
      ))}
    </nav>
  )
}
```

---

### 4.5 페이지 컴포넌트 수정

#### A. Blog 리스팅 페이지 수정

```
파일: src/app/(public)/blog/page.tsx
```

**핵심 변경 사항:**

```tsx
import { Suspense } from "react"
import ContentSearch from "@/components/content/ContentSearch"
import CategoryTabs from "@/components/content/CategoryTabs"
import { fetchPublicBlogs, fetchBlogTags } from "@/lib/api/blogs"

interface BlogPageProps {
  searchParams: Promise<{
    page?: string
    search?: string
    tag?: string
  }>
}

export default async function BlogPage({ searchParams }: BlogPageProps) {
  const params = await searchParams
  const page = Number(params.page) || 1
  const search = params.search || undefined
  const tag = params.tag || undefined

  // 병렬 fetch: 블로그 목록 + 태그 목록
  const [blogsData, tags] = await Promise.all([
    fetchPublicBlogs({ page, pageSize: responsivePageSize, search, tag }),
    fetchBlogTags(),
  ])

  return (
    <div className="mx-auto max-w-6xl px-4 py-12">
      {/* ── 헤더 영역: 타이틀 + 검색바 ── */}
      <div className="flex flex-col gap-6 sm:flex-row sm:items-start sm:justify-between">
        <h1 className="font-serif text-3xl font-bold text-brand-navy sm:text-4xl">
          Blog
        </h1>
        <Suspense fallback={null}>
          <ContentSearch basePath="/blog" placeholder="블로그 검색..." />
        </Suspense>
      </div>

      {/* ── 태그 필터 탭 ── */}
      <div className="mt-6 mb-8">
        <Suspense fallback={null}>
          <CategoryTabs
            basePath="/blog"
            items={tags}
            paramKey="tag"
            allLabel="전체"
          />
        </Suspense>
      </div>

      {/* ── 검색 결과 상태 표시 ── */}
      {(search || tag) && (
        <div className="mb-4 text-sm text-muted-foreground">
          {search && <span>"{search}" 검색 결과</span>}
          {search && tag && <span> · </span>}
          {tag && <span>태그: {tag}</span>}
          <span> — 총 {blogsData.totalItems}건</span>
        </div>
      )}

      {/* ── 블로그 카드 그리드 (기존 로직 유지) ── */}
      {blogsData.items.length === 0 ? (
        <div className="py-20 text-center text-muted-foreground">
          {search || tag ? "검색 결과가 없습니다." : "아직 게시물이 없습니다."}
        </div>
      ) : (
        <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
          {/* 기존 카드 렌더링 로직 */}
        </div>
      )}

      {/* ── 페이지네이션 (기존 로직 유지) ── */}
    </div>
  )
}
```

#### B. Works 리스팅 페이지 수정

```
파일: src/app/(public)/works/page.tsx
```

**핵심 변경 사항:**

```tsx
import { Suspense } from "react"
import ContentSearch from "@/components/content/ContentSearch"
import CategoryTabs from "@/components/content/CategoryTabs"
import { fetchPublicWorks, fetchWorkCategories } from "@/lib/api/works"

interface WorksPageProps {
  searchParams: Promise<{
    page?: string
    search?: string
    category?: string
  }>
}

export default async function WorksPage({ searchParams }: WorksPageProps) {
  const params = await searchParams
  const page = Number(params.page) || 1
  const search = params.search || undefined
  const category = params.category || undefined

  // 병렬 fetch: 작업 목록 + 카테고리 목록
  const [worksData, categories] = await Promise.all([
    fetchPublicWorks({ page, pageSize: responsivePageSize, search, category }),
    fetchWorkCategories(),
  ])

  return (
    <div className="mx-auto max-w-7xl px-4 py-12">
      {/* ── 헤더 영역: 타이틀 + 검색바 ── */}
      <div className="flex flex-col gap-6 sm:flex-row sm:items-start sm:justify-between">
        <h1 className="font-serif text-3xl font-bold text-brand-navy sm:text-4xl">
          Works
        </h1>
        <Suspense fallback={null}>
          <ContentSearch basePath="/works" placeholder="작업물 검색..." />
        </Suspense>
      </div>

      {/* ── 카테고리 필터 탭 ── */}
      <div className="mt-6 mb-8">
        <Suspense fallback={null}>
          <CategoryTabs
            basePath="/works"
            items={categories}
            paramKey="category"
            allLabel="전체"
          />
        </Suspense>
      </div>

      {/* ── 검색 결과 상태 표시 ── */}
      {(search || category) && (
        <div className="mb-4 text-sm text-muted-foreground">
          {search && <span>"{search}" 검색 결과</span>}
          {search && category && <span> · </span>}
          {category && <span>카테고리: {category}</span>}
          <span> — 총 {worksData.totalItems}건</span>
        </div>
      )}

      {/* ── 작업물 카드 그리드 (기존 로직 유지) ── */}
      {worksData.items.length === 0 ? (
        <div className="py-20 text-center text-muted-foreground">
          {search || category ? "검색 결과가 없습니다." : "아직 작업물이 없습니다."}
        </div>
      ) : (
        <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-4">
          {/* 기존 카드 렌더링 로직 */}
        </div>
      )}

      {/* ── 페이지네이션 (기존 로직 유지) ── */}
    </div>
  )
}
```

---

### 4.6 UI 레이아웃 설계 (와이어프레임)

```
┌───────────────────────────────────────────────────────────────┐
│  Navbar                                                       │
├───────────────────────────────────────────────────────────────┤
│                                                               │
│  Blog (또는 Works)                      ┌──────────────────┐  │
│  ═══════════════                       │ Search         🔍 │  │
│                                        └──────────────────┘  │
│  ┌─────────────────────────────────────────────────┐         │
│  │ 전체   C#   C++   Python   AI   Web   Graphics  │         │
│  │  ───                                             │         │
│  └─────────────────────────────────────────────────┘         │
│                                                               │
│  "AI" 검색 결과 · 태그: Python — 총 5건                        │
│                                                               │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                    │
│  │  Card 1  │  │  Card 2  │  │  Card 3  │                    │
│  │          │  │          │  │          │                    │
│  └──────────┘  └──────────┘  └──────────┘                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                    │
│  │  Card 4  │  │  Card 5  │  │  Card 6  │                    │
│  │          │  │          │  │          │                    │
│  └──────────┘  └──────────┘  └──────────┘                    │
│                                                               │
│           < 1  2  3  4  5 >                                  │
│                                                               │
├───────────────────────────────────────────────────────────────┤
│  Footer                                                       │
└───────────────────────────────────────────────────────────────┘
```

**반응형:**
- **Desktop (xl)**: 타이틀 왼쪽 / 검색바 오른쪽 (flex-row), 탭 한 줄
- **Tablet (md)**: 타이틀 위 / 검색바 아래 (flex-col), 탭 2줄까지 wrap
- **Mobile**: 풀 너비 검색바, 탭 스크롤 또는 wrap

---

### 4.7 페이지네이션 URL 유지 전략

검색/필터 상태를 페이지네이션에서도 유지해야 합니다.

**현재 `PublicPagination` 컴포넌트 수정 필요:**

```tsx
// 현재: buildHref = (p) => `?page=${p}`
// 수정: 기존 search params를 유지하면서 page만 변경

function buildHref(targetPage: number): string {
  const params = new URLSearchParams(currentSearchParams)
  if (targetPage <= 1) {
    params.delete("page")
  } else {
    params.set("page", String(targetPage))
  }
  return `?${params.toString()}`
}
```

이렇게 해야 `?search=AI&tag=Python&page=2` 같은 URL이 올바르게 생성됩니다.

**`EdgePaginationNav` (좌우 화살표)도 동일하게 수정.**

---

## 5. 전체 구현 순서 (추천)

### Phase 1: 백엔드 API 확장

| 순서 | 작업 | 파일 |
|-----|------|------|
| 1-1 | GetBlogsRequest, GetBlogsQuery에 search/tag 파라미터 추가 | `GetBlogsRequest.cs`, `GetBlogsQuery.cs` |
| 1-2 | PublicBlogService.GetBlogsAsync에 필터 로직 추가 | `PublicBlogService.cs` |
| 1-3 | GetWorksRequest, GetWorksQuery에 search/category/tag 파라미터 추가 | `GetWorksRequest.cs`, `GetWorksQuery.cs` |
| 1-4 | PublicWorkService.GetWorksAsync에 필터 로직 추가 | `PublicWorkService.cs` |
| 1-5 | Blog Tags 엔드포인트 생성 + 라우트 등록 | 새 파일 + `Program.cs` |
| 1-6 | Work Categories 엔드포인트 생성 + 라우트 등록 | 새 파일 + `Program.cs` |
| 1-7 | (선택) Work Tags 엔드포인트 생성 | 새 파일 + `Program.cs` |
| 1-8 | 백엔드 빌드 & 테스트 | `dotnet build`, API 호출 검증 |

### Phase 2: 프론트엔드 API 레이어

| 순서 | 작업 | 파일 |
|-----|------|------|
| 2-1 | `fetchPublicBlogs` 시그니처 변경 (options 객체) | `blogs.ts` |
| 2-2 | `fetchBlogTags` 함수 추가 | `blogs.ts` |
| 2-3 | `fetchPublicWorks` 시그니처 변경 (options 객체) | `works.ts` |
| 2-4 | `fetchWorkCategories` 함수 추가 | `works.ts` |
| 2-5 | 기존 호출부 마이그레이션 (`fetchAllPublicBlogs` 등) | `blogs.ts`, `works.ts` |

### Phase 3: 프론트엔드 UI 컴포넌트

| 순서 | 작업 | 파일 |
|-----|------|------|
| 3-1 | `ContentSearch` 컴포넌트 생성 | 새 파일 |
| 3-2 | `CategoryTabs` 컴포넌트 생성 | 새 파일 |
| 3-3 | Blog 페이지에 검색바 + 태그 탭 통합 | `blog/page.tsx` |
| 3-4 | Works 페이지에 검색바 + 카테고리 탭 통합 | `works/page.tsx` |
| 3-5 | `PublicPagination` 수정 — search params 유지 | `PublicPagination.tsx` |
| 3-6 | `EdgePaginationNav` 수정 — search params 유지 | `EdgePaginationNav.tsx` |
| 3-7 | 빈 상태 / 검색 결과 없음 UI 처리 | `blog/page.tsx`, `works/page.tsx` |

### Phase 4: 검증 & 마무리

| 순서 | 작업 |
|-----|------|
| 4-1 | 검색 + 필터 + 페이지네이션 조합 수동 테스트 |
| 4-2 | 반응형 레이아웃 확인 (Desktop / Tablet / Mobile) |
| 4-3 | 빈 데이터, 특수 문자 검색, URL 직접 접근 테스트 |
| 4-4 | (선택) 프론트엔드 유닛 테스트 추가 |

---

## 6. 기술 결정 & 트레이드오프

### 6.1 서버사이드 검색 vs 클라이언트 검색

| | 서버사이드 (선택) | 클라이언트사이드 |
|---|---|---|
| **장점** | 대량 데이터 처리 가능, DB 인덱스 활용, SEO 친화적 | 즉각적 응답, 서버 부하 없음 |
| **단점** | 네트워크 왕복 필요 | 전체 데이터 로드 필요, 대량 데이터 불가 |
| **적합** | 데이터 증가 예상, 페이지네이션과 자연스러운 통합 | 데이터가 매우 적은 경우 |

→ **서버사이드 검색 선택**: 기존 페이지네이션 구조와 일관되고, 데이터가 늘어나도 확장 가능

### 6.2 URL 기반 상태 관리 vs React State

| | URL searchParams (선택) | React useState |
|---|---|---|
| **장점** | 북마크 가능, 새로고침 유지, 공유 가능, SSR 지원 | 구현 단순 |
| **단점** | 네비게이션 시 서버 리렌더링 | 새로고침 시 초기화, 공유 불가, SSR 불가 |

→ **URL searchParams 선택**: 현재 page도 URL 기반이므로 일관성 유지

### 6.3 Blog 태그 vs Blog 카테고리

- Blog 엔티티에는 `Category` 필드가 **없음** (Work에만 있음)
- Blog의 `Tags` 배열을 카테고리 탭처럼 활용
- 향후 Blog에도 Category 단일 필드가 필요하면 DB 마이그레이션으로 추가 가능
- 현 시점에서는 **Tags 기반 필터로 충분**

### 6.4 검색 방식: LIKE vs Full-Text Search

| | LIKE / Contains (선택) | PostgreSQL Full-Text Search |
|---|---|---|
| **장점** | 구현 간단, 별도 설정 불필요 | 형태소 분석, 랭킹, 한국어 지원 |
| **단점** | 인덱스 활용 어려움, 형태소 분석 없음 | 설정 복잡, 한국어 추가 설정 필요 |

→ **LIKE 방식으로 시작**: 현재 데이터 규모에서 충분. 나중에 성능 이슈 시 FTS 도입 고려

---

## 7. 보안 고려사항

1. **SQL Injection 방지**: EF Core의 LINQ가 파라미터화된 쿼리를 생성하므로 안전
2. **XSS 방지**: React의 기본 이스케이핑 + URL 인코딩으로 보호
3. **검색어 길이 제한**: 백엔드에서 search 파라미터 최대 길이 검증 권장 (예: 200자)
4. **Rate limiting**: 검색 API에 rate limiting 적용 고려 (DoS 방지)

```csharp
// 검증 예시 (GetBlogsRequest.cs)
internal GetBlogsQuery ToQuery() => new(
    Page ?? 1,
    Math.Min(PageSize ?? 10, 100),  // 최대 100개
    Search?.Trim().Length > 200 ? Search.Trim()[..200] : Search?.Trim(),
    Tag?.Trim()
);
```

---

## 8. 영향받는 기존 코드 체크리스트

기존 코드 중 `fetchPublicBlogs`, `fetchPublicWorks` 를 호출하는 부분을 모두 업데이트해야 합니다:

| 파일 | 호출 | 변경 내용 |
|------|------|----------|
| `src/app/(public)/blog/page.tsx` | `fetchPublicBlogs(page, pageSize)` | `fetchPublicBlogs({ page, pageSize, search, tag })` |
| `src/app/(public)/blog/[slug]/page.tsx` | `fetchAllPublicBlogs()` | 시그니처 변경 없으면 유지, 아니면 수정 |
| `src/app/(public)/works/page.tsx` | `fetchPublicWorks(page, pageSize)` | `fetchPublicWorks({ page, pageSize, search, category })` |
| `src/app/(public)/works/[slug]/page.tsx` | `fetchAllPublicWorks()` | 시그니처 변경 없으면 유지, 아니면 수정 |
| `src/lib/api/blogs.ts` | `fetchAllPublicBlogs` 내부에서 `fetchPublicBlogs` 호출 | 인자 형태 수정 |
| `src/lib/api/works.ts` | `fetchAllPublicWorks` 내부에서 `fetchPublicWorks` 호출 | 인자 형태 수정 |
| Home page composition (있다면) | 블로그/작업 목록 fetch | 확인 필요 |

---

## 9. 요약

### 백엔드 변경
- **수정 6개 파일**: Request, Query, Service (Blog/Work 각각)
- **새 파일 2~3개**: GetBlogTags, GetWorkCategories, (선택) GetWorkTags 엔드포인트
- **Program.cs**: 라우트 등록 추가

### 프론트엔드 변경
- **수정 2개 API 파일**: `blogs.ts`, `works.ts`
- **새 컴포넌트 2개**: `ContentSearch.tsx`, `CategoryTabs.tsx`
- **수정 2개 페이지**: `blog/page.tsx`, `works/page.tsx`
- **수정 2개 레이아웃 컴포넌트**: `PublicPagination.tsx`, `EdgePaginationNav.tsx`

### 추정 난이도
- 백엔드: **낮음~중간** (기존 패턴을 그대로 확장)
- 프론트엔드: **중간** (Client Component 추가 + URL 상태 관리)
