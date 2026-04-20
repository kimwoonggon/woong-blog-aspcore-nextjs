# Backend CQRS Refactor Notes - 2026-04-19

## Summary

이번 단계의 backend 변경 목표는 기존 Content 모듈이 의도했던 CQRS/Clean Architecture 방향을 더 명확하게 만드는 것이었다. 핵심은 `Admin*Service`/`Public*Service`처럼 actor 기준으로 너무 넓게 묶인 service를 제거하고, handler가 use-case orchestration을 소유하며, persistence는 좁은 command/query store로 분리하는 것이다.

대상 범위는 Content의 Blogs, Works, Pages가 중심이고, WorkVideo 관련 Domain dependency 정리와 public search 성능 개선도 포함됐다.

## Service 관점

### 제거한 broad service

다음 service interface/implementation은 제거 대상이 됐다.

- `IAdminBlogService`
- `IPublicBlogService`
- `AdminBlogService`
- `PublicBlogService`
- `IAdminWorkService`
- `IPublicWorkService`
- `AdminWorkService`
- `PublicWorkService`
- `IAdminPageService`
- `IPublicPageService`
- `AdminPageService`
- `PublicPageService`

이전 구조는 handler가 `IAdminBlogService.CreateAsync(command)`처럼 command 전체를 그대로 넘기고 service가 slug 생성, excerpt 생성, entity mutation, EF query, projection, SaveChanges까지 한 번에 담당했다. 이 구조는 service가 use-case와 persistence를 동시에 품는 문제가 있었다.

### 새로 만든 narrow store

actor service 대신 아래 store interface/implementation을 도입했다.

- `IBlogCommandStore` / `BlogCommandStore`
- `IBlogQueryStore` / `BlogQueryStore`
- `IWorkCommandStore` / `WorkCommandStore`
- `IWorkQueryStore` / `WorkQueryStore`
- `IPageCommandStore` / `PageCommandStore`
- `IPageQueryStore` / `PageQueryStore`

Command store는 mutation에 필요한 최소 연산만 가진다.

- slug 중복 확인
- update 대상 entity 로드
- add/remove
- 관련 child row 로드/삭제
- SaveChanges

Query store는 read model projection과 EF query만 담당한다.

- admin list/detail projection
- public list/detail projection
- pagination
- search column filtering
- asset/video lookup

### DI 위치 변경

기존 Content persistence service 등록은 `Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`에 있었다. 이번 변경 후 Content store 등록은 각 module extension으로 이동했다.

- `BlogsModuleServiceCollectionExtensions`
- `WorksModuleServiceCollectionExtensions`
- `PagesModuleServiceCollectionExtensions`

이렇게 해서 중앙 persistence registration이 Content module 내부 구현을 직접 알지 않게 했고, module별 ownership이 더 명확해졌다.

## CQRS 관점

### 이전 흐름

이전 흐름은 대체로 다음 형태였다.

```text
Endpoint -> MediatR Command/Query Handler -> Admin/Public Service -> DbContext
```

문제는 handler가 대부분 service에 그대로 위임하는 thin wrapper였다는 점이다. CQRS의 command/query type은 있었지만 실제 use-case 책임은 service에 있었다.

### 변경 후 흐름

변경 후 흐름은 다음 형태다.

```text
Endpoint -> MediatR Command/Query Handler -> Command/Query Store -> DbContext
```

handler는 이제 use-case orchestration을 맡는다.

- command validation 이후의 mutation 흐름
- slug 생성
- excerpt 생성
- publish timestamp 규칙
- search field 값 세팅
- not found/result mapping
- 삭제 시 관련 child cleanup orchestration

store는 DB read/write primitive를 제공한다.

### Command side

Blog command handlers:

- `CreateBlogCommandHandler`
- `UpdateBlogCommandHandler`
- `DeleteBlogCommandHandler`

Work command handlers:

- `CreateWorkCommandHandler`
- `UpdateWorkCommandHandler`
- `DeleteWorkCommandHandler`

Page command handler:

- `UpdatePageCommandHandler`

이 handler들은 더 이상 `IAdmin*Service`에 command를 통째로 넘기지 않는다. 필요한 entity를 command store에서 가져오고, handler 내부에서 use-case state transition을 수행한 뒤 store에 저장을 요청한다.

### Query side

Blog query handlers:

- `GetAdminBlogsQueryHandler`
- `GetAdminBlogByIdQueryHandler`
- `GetBlogsQueryHandler`
- `GetBlogBySlugQueryHandler`

Work query handlers:

- `GetAdminWorksQueryHandler`
- `GetAdminWorkByIdQueryHandler`
- `GetWorksQueryHandler`
- `GetWorkBySlugQueryHandler`

Page query handlers:

- `GetAdminPagesQueryHandler`
- `GetPageBySlugQueryHandler`

Query handler는 request parameter 정규화, search mode 결정, page/pageSize normalization을 담당한다. 실제 EF projection과 related lookup은 query store가 담당한다.

## Handler 관점

### Blog handlers

`CreateBlogCommandHandler`는 다음 책임을 갖게 됐다.

- title 기반 unique slug 생성
- content JSON에서 excerpt text 추출
- excerpt 생성
- `Blog` entity 생성
- `SearchTitle`, `SearchText` 값 계산
- `PublishedAt`, `CreatedAt`, `UpdatedAt` 설정
- `AdminMutationResult` 반환

`UpdateBlogCommandHandler`는 다음 책임을 갖게 됐다.

- update 대상 blog 로드
- 없으면 `null`
- title 변경에 따른 unique slug 재계산
- excerpt/search fields 갱신
- publish 상태 전환 시 최초 `PublishedAt` 설정
- `AdminMutationResult` 반환

`DeleteBlogCommandHandler`는 다음 책임을 갖게 됐다.

- 삭제 대상 blog 로드
- 없으면 `AdminActionResult(false)`
- remove/save
- 있으면 `AdminActionResult(true)`

### Work handlers

`CreateWorkCommandHandler`는 다음 책임을 갖게 됐다.

- title 기반 unique slug 생성
- excerpt 생성
- thumbnail/icon/category/period/tags/content/allProperties 설정
- search fields 계산
- publish timestamp와 audit timestamp 설정
- `AdminMutationResult` 반환

`UpdateWorkCommandHandler`는 다음 책임을 갖게 됐다.

- update 대상 work 로드
- 없으면 `null`
- slug/excerpt/search fields 갱신
- thumbnail/icon/category/period/tags/content/allProperties 갱신
- publish 상태 전환 처리

`DeleteWorkCommandHandler`는 다음 책임을 갖게 됐다.

- work 존재 여부 확인
- `IWorkVideoService.EnqueueCleanupForWorkAsync` 호출
- work videos/upload sessions 로드
- related rows remove
- work remove/save

### Page handlers

`UpdatePageCommandHandler`는 page update orchestration을 service에서 handler로 이동했다.

- page 로드
- 없으면 `AdminActionResult(false)`
- title/content/updatedAt 갱신
- save

`GetAdminPagesQueryHandler`, `GetPageBySlugQueryHandler`는 page query store로 projection 책임을 위임한다.

## DTO 관점

### 유지한 API DTO/response shape

HTTP API의 request/response 계약은 의도적으로 크게 바꾸지 않았다.

- `CreateBlogRequest`
- `UpdateBlogRequest`
- `CreateWorkRequest`
- `UpdateWorkRequest`
- `UpdatePageRequest`
- `UpdateSiteSettingsRequest`
- admin/public list/detail DTO들

프론트엔드와 API contract 변경을 최소화하기 위해 endpoint path와 주요 JSON shape는 유지했다.

### DTO ownership 개선

일부 카드 DTO의 위치가 정리됐다.

- `BlogCardDto`는 blog list/read model 쪽에 가까운 위치로 이동
- `WorkCardDto`는 work list/read model 쪽에 가까운 위치로 이동
- `HomeDto`는 composition 결과로서 해당 DTO들을 참조한다

이 변경의 목적은 Composition module이 card DTO를 소유하던 구조를 줄이고, Blog/Work의 public read model이 자기 DTO를 더 직접 소유하게 하는 것이다.

### Store interface가 command/query DTO를 직접 받지 않게 함

새 abstraction은 `CreateBlogCommand`, `UpdateWorkCommand` 같은 MediatR request type을 parameter로 받지 않는다. command/query object를 persistence layer로 흘리는 대신 handler가 primitive/entity state로 풀어 store를 호출한다.

예:

```text
Before: IAdminBlogService.CreateAsync(CreateBlogCommand command)
After:  IBlogCommandStore.Add(Blog blog)
```

이로써 Application request type과 persistence adapter 사이 coupling이 줄었다.

## Database 관점

### Search columns 추가

`Blog`와 `Work` entity에 다음 컬럼을 추가했다.

- `SearchTitle`
- `SearchText`

목적은 public list search에서 `ContentJson`/excerpt/title 전체 row를 메모리로 가져와 C#에서 필터링하던 구조를 DB filtering 구조로 바꾸는 것이다.

### DbContext search field synchronization

`WoongBlogDbContext.SaveChanges`와 `SaveChangesAsync`를 override했다.

저장 전 `Blog`, `Work`의 added/modified entries를 확인해 다음을 동기화한다.

- `SearchTitle = ContentSearchText.Normalize(Title)`
- `SearchText = ContentSearchText.BuildIndex(Excerpt, ExtractExcerptText(ContentJson))`

이렇게 해서 handler가 값을 세팅하더라도 seed나 다른 update path에서 누락될 가능성을 줄였다.

### EF model indexes

`WoongBlogDbContext.OnModelCreating`에 다음 index를 추가했다.

- `Blog(Published, PublishedAt)`
- `Blog(SearchTitle)`
- `Work(Published, PublishedAt)`
- `Work(SearchTitle)`

EF model contract test에서 이 index 존재를 검증하도록 했다.

### Schema patch 추가

`DatabaseBootstrapper`에 schema patch를 추가했다.

Patch ID:

```text
20260419_content_search_fields
```

Patch 내용:

- `pg_trgm` extension 생성
- `Blogs.SearchTitle`, `Blogs.SearchText` 추가
- `Works.SearchTitle`, `Works.SearchText` 추가
- 기존 rows backfill
- `Published, PublishedAt` index 생성
- `SearchTitle`, `SearchText` GIN trigram index 생성

생성되는 DB index:

- `IX_Blogs_Published_PublishedAt`
- `IX_Works_Published_PublishedAt`
- `IX_Blogs_SearchTitle_Trgm`
- `IX_Blogs_SearchText_Trgm`
- `IX_Works_SearchTitle_Trgm`
- `IX_Works_SearchText_Trgm`

### Public search query 개선

이전에는 검색어가 있으면 published rows를 모두 가져온 뒤 C#에서 `ContentSearchText`로 filtering했다.

변경 후:

- `GetBlogsQueryHandler`가 search query를 normalize
- `BlogQueryStore`가 `SearchTitle.Contains(normalizedQuery)` 또는 `SearchText.Contains(normalizedQuery)`로 DB query 구성
- `GetWorksQueryHandler`와 `WorkQueryStore`도 동일한 방식 적용

이로써 large row set을 application memory에 올리는 부하를 줄였다.

## Domain 관점

### Blog/Work domain entity 변경

`Blog`, `Work`에 검색 전용 필드를 추가했다.

- `SearchTitle`
- `SearchText`

이 필드는 user-facing DTO가 아니라 persistence/search optimization을 위한 domain entity field다.

### WorkVideo constants 이동

기존 `WorkVideoConstants.cs`는 `Modules.Content.Works.Application.WorkVideos` namespace에 있었고, Domain entity가 Application namespace를 using했다.

변경 후:

- `WorkVideoConstants.cs`를 `Domain.Entities` namespace로 이동
- `WorkVideoUploadSession`
- `VideoStorageCleanupJob`

위 entity들이 더 이상 Application namespace를 참조하지 않는다.

포함된 constants:

- `WorkVideoSourceTypes`
- `WorkVideoUploadSessionStatuses`
- `VideoStorageCleanupJobStatuses`
- `WorkVideoHlsSourceKey`

### Thumbnail resolver 위치 조정

`WorkThumbnailUrlResolver`는 persistence 폴더에서 `Modules.Content.Works.Application.Support`로 이동했다. 이 resolver는 public/admin read model projection에서 thumbnail URL을 결정하는 application support logic에 가깝다.

역할:

- explicit thumbnail asset 우선
- non-YouTube video 우선
- YouTube thumbnail fallback
- content HTML 첫 image fallback

## Architecture boundary tests 관점

`ArchitectureBoundaryTests`를 강화했다.

추가/강화한 관점:

- Content handler가 제거된 actor facade service에 의존하지 않음
- Content application abstraction이 MediatR request type을 parameter로 받지 않음
- Domain source가 `WoongBlog.Api.Modules.*`, `Application`, `Infrastructure` using을 갖지 않음
- Public content query persistence가 in-memory search filtering으로 회귀하지 않음

이 테스트들은 이번 구조 변경의 회귀 방지 역할을 한다.

## Persistence contract tests 관점

`PersistenceContractTests`에 search schema contract를 추가했다.

검증 대상:

- `Blog.SearchTitle`
- `Blog.SearchText`
- `Work.SearchTitle`
- `Work.SearchText`
- `Blog(Published, PublishedAt)` index
- `Work(Published, PublishedAt)` index
- `Blog.SearchTitle` index
- `Work.SearchTitle` index

기존 jsonb, slug unique, profile/session uniqueness contract는 유지했다.

## Public query tests 관점

`PublicQueryHandlerTests`는 기존 service helper 대신 새 query store를 사용하도록 갱신했다.

변경:

- `PublicBlogService` 대신 `BlogQueryStore`
- `PublicWorkService` 대신 `WorkQueryStore`

테스트 목적:

- published filtering
- pagination
- normalized title search
- content search
- asset/video URL resolution

## Module registration 관점

Content module 등록 책임이 module extension으로 이동했다.

Blog:

```text
AddBlogsModule()
  IBlogCommandStore -> BlogCommandStore
  IBlogQueryStore -> BlogQueryStore
```

Works:

```text
AddWorksModule()
  IWorkCommandStore -> WorkCommandStore
  IWorkQueryStore -> WorkQueryStore
  IWorkVideoService -> WorkVideoService
  IVideoObjectStorage -> Local/R2 storage
  IWorkVideoPlaybackUrlBuilder -> WorkVideoPlaybackUrlBuilder
```

Pages:

```text
AddPagesModule()
  IPageCommandStore -> PageCommandStore
  IPageQueryStore -> PageQueryStore
```

## Files added

### Content abstractions

- `IBlogCommandStore.cs`
- `IBlogQueryStore.cs`
- `IWorkCommandStore.cs`
- `IWorkQueryStore.cs`
- `IPageCommandStore.cs`
- `IPageQueryStore.cs`

### Persistence adapters

- `BlogCommandStore.cs`
- `BlogQueryStore.cs`
- `WorkCommandStore.cs`
- `WorkQueryStore.cs`
- `PageCommandStore.cs`
- `PageQueryStore.cs`

### Domain/support

- `Domain/Entities/WorkVideoConstants.cs`
- `Content/Common/Application/Support/ContentSearchMode.cs`
- `Content/Works/Application/Support/WorkThumbnailUrlResolver.cs`

## Files removed

- `IAdminBlogService.cs`
- `IPublicBlogService.cs`
- `AdminBlogService.cs`
- `PublicBlogService.cs`
- `IAdminWorkService.cs`
- `IPublicWorkService.cs`
- `AdminWorkService.cs`
- `PublicWorkService.cs`
- `IAdminPageService.cs`
- `IPublicPageService.cs`
- `AdminPageService.cs`
- `PublicPageService.cs`
- old `WorkVideoConstants.cs` under Application WorkVideos namespace
- old `WorkThumbnailUrlResolver.cs` under Persistence namespace

## Behavior preserved

The refactor aimed to preserve external behavior.

Preserved:

- admin blog/work/page endpoint paths
- public blog/work/page endpoint paths
- request DTO names and JSON shape
- admin mutation result shape
- public list/detail DTO shape as consumed by frontend
- Work video upload/playback behavior
- existing seed behavior

## Known tradeoffs

- `SearchTitle` and `SearchText` are denormalized fields. This adds write-side synchronization complexity but reduces read-side search cost.
- `WoongBlogDbContext` now references Content support helpers for search synchronization. This is pragmatic in a single-project architecture but would need a domain/application service if the codebase later becomes multi-project Clean Architecture.
- Raw SQL schema patch remains consistent with the repository's current `SchemaPatches` approach instead of introducing EF migrations.

## Verification performed

Backend:

```text
dotnet test backend/WoongBlog.sln
139 passed
```

Frontend/browser smoke after backend change:

```text
npm run typecheck
npm run lint
npx vitest run src/test/admin-bulk-table.test.tsx
focused Docker-backed Playwright subset
```

Docker/Postgres schema smoke:

- backend health OK
- nginx health OK
- `SchemaPatches` contains `20260419_content_search_fields`
- `Blogs`/`Works` contain `SearchTitle` and `SearchText`
- trigram indexes exist for Blog/Work search fields
