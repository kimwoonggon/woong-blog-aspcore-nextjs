# UI Copy Surface Map 0415

이 문서는 사용자가 직접 코드로 문구를 수정할 수 있도록, 현재 노출되는 주요 문구의 위치와 노출 범위를 정리한 매핑 문서입니다.

구분 기준:
- `Public HTML`: 비로그인 방문자도 바로 보는 public 화면
- `Admin Auth Only`: 로그인한 관리자일 때만 public 화면에 추가로 보이는 편집 affordance
- `Admin Route`: `/admin/*` 경로에서만 보이는 운영자 전용 화면
- `Dynamic Value`: 문구 전체가 코드에 하드코딩되지 않고 설정/데이터에서 채워짐

## Global Chrome

### Navbar
- `Portfolio`
- `Works, writing, and experiments in one balanced shell.`
- 위치:
  - [Navbar.tsx](/mnt/d/woong-blog/woong-blog/src/components/layout/Navbar.tsx)
- 노출:
  - `Public HTML`
- 비고:
  - `Portfolio`는 정적 라벨
  - 그 아래 브랜드명/소유자명은 동적 값과 같이 섞여 렌더링됨

### Footer
- `Explore`
- `Selected work, writing, and ways to get in touch.`
- `Elsewhere`
- `© 2026 ... All rights reserved.`
- 위치:
  - [Footer.tsx](/mnt/d/woong-blog/woong-blog/src/components/layout/Footer.tsx)
- 노출:
  - `Public HTML`
- 비고:
  - footer의 소유자명 부분은 `Dynamic Value`
  - 연도는 `new Date().getFullYear()`라 `Dynamic Value`

### Dynamic Owner/Brand Name
- `홍길동! ...`
- 위치:
  - navbar / footer / 일부 public shell 전반
  - 주요 출처: [Navbar.tsx](/mnt/d/woong-blog/woong-blog/src/components/layout/Navbar.tsx), [Footer.tsx](/mnt/d/woong-blog/woong-blog/src/components/layout/Footer.tsx)
- 노출:
  - `Public HTML`
- 비고:
  - 운영자가 pages/settings에서 바꾸는 동적 값

## Home

### Explore Next Section
- `Explore next`
- `Move through the portfolio with intent`
- `Start with the project archive, read the longer notes, or jump into the more personal pages.`
- 카드:
  - `Works`
  - `Browse the complete archive of shipped work and experiments.`
  - `Blog`
  - `Read the rationale, process notes, and technical write-ups.`
  - `Introduction`
  - `Get the personal context behind the portfolio and current focus.`
- 위치:
  - [src/app/(public)/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/(public)/page.tsx)
- 노출:
  - `Public HTML`

### Featured Works Section Label
- `Selected work`
- 위치:
  - [src/app/(public)/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/(public)/page.tsx)
- 노출:
  - `Public HTML`

## Introduction

### Public Intro Shell
- `About the work`
- `Introduction`
- `A short framing of who I am, what kind of problems I like to solve, and how to read the projects collected across the site.`
- 위치:
  - [src/app/(public)/introduction/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/(public)/introduction/page.tsx)
- 노출:
  - `Public HTML`

### Inline Intro Editor
- `Introduction Inline Editor`
- `현재 페이지를 벗어나지 않고 소개글을 바로 수정합니다.`
- `소개글 수정`
- 위치:
  - [src/app/(public)/introduction/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/(public)/introduction/page.tsx)
- 노출:
  - `Admin Auth Only`
- 비고:
  - public introduction page 안에 로그인한 관리자에게만 추가로 렌더링됨

## Works Archive

### Public Works Hero
- `Project archive`
- `Works`
- `A curated archive of shipped interfaces, experiments, and platform work. Each card is meant to read quickly and still invite a deeper dive.`
- `Start a conversation`
- `Read the notes`
- 위치:
  - [src/app/(public)/works/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/(public)/works/page.tsx)
- 노출:
  - `Public HTML`

### Inline Work Create Shell
- `Works Inline Create`
- `navbar를 유지한 채 현재 페이지 아래에서 새 작업을 작성합니다.`
- `새 작업 쓰기`
- 위치:
  - [PublicWorksInlineCreateShell.tsx](/mnt/d/woong-blog/woong-blog/src/components/admin/PublicWorksInlineCreateShell.tsx)
  - 사용 위치: [src/app/(public)/works/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/(public)/works/page.tsx)
- 노출:
  - `Admin Auth Only`
- 비고:
  - `/works` public 페이지 안에서 관리자에게만 보임

## Blog Detail

### Inline Blog Editor Shell
- `Blog Inline Editor`
- `현재 게시물 뷰를 유지한 채 바로 수정합니다.`
- `글 수정`
- 위치:
  - [InlineBlogEditorSection.tsx](/mnt/d/woong-blog/woong-blog/src/components/admin/InlineBlogEditorSection.tsx)
  - 사용 위치: [src/app/(public)/blog/[slug]/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/(public)/blog/[slug]/page.tsx)
- 노출:
  - `Admin Auth Only`
- 비고:
  - blog slug detail page에서 로그인한 관리자에게만 보임

## Contact

### Inline Contact Editor
- `문의글 수정`
- `Contact Inline Editor`
- `현재 페이지에서 바로 문의 페이지 내용을 수정합니다.`
- 위치:
  - [src/app/(public)/contact/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/(public)/contact/page.tsx)
- 노출:
  - `Admin Auth Only`

## Blog Archive Create Shell

### Public Blog Create
- `새 글 쓰기`
- `Blog Inline Create`
- `Create a new post inline without leaving the current public page.`
- 위치:
  - [src/app/(public)/blog/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/(public)/blog/page.tsx)
- 노출:
  - `Admin Auth Only`
- 비고:
  - 이 부분은 public `/blog` 화면 안에서 관리자에게만 보임

## Resume

### Resume Inline Upload
- `이력서 PDF 업로드`
- `Resume Inline Upload`
- `현재 페이지에서 바로 PDF를 업로드하거나 교체합니다.`
- 위치:
  - [src/app/(public)/resume/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/(public)/resume/page.tsx)
- 노출:
  - `Admin Auth Only`

## Admin Route Only References

아래는 public HTML이 아니라 `/admin/*`에서만 보이는 운영자 전용 문구입니다.

### Admin Dashboard
- `Works`
- `Blog Posts`
- `Blog Notion View`
- 위치:
  - [src/app/admin/dashboard/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/admin/dashboard/page.tsx)
- 노출:
  - `Admin Route`

### Admin Works Page
- `Works`
- 위치:
  - [src/app/admin/works/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/admin/works/page.tsx)
- 노출:
  - `Admin Route`

### Admin Blog Notion
- `Blog Notion View`
- 위치:
  - [src/app/admin/blog/notion/page.tsx](/mnt/d/woong-blog/woong-blog/src/app/admin/blog/notion/page.tsx)
  - [BlogNotionWorkspace.tsx](/mnt/d/woong-blog/woong-blog/src/components/admin/BlogNotionWorkspace.tsx)
- 노출:
  - `Admin Route`

## 빠른 판단 기준

- public 방문자도 바로 보는 문구를 바꾸고 싶다:
  - `Navbar.tsx`
  - `Footer.tsx`
  - `src/app/(public)/*/page.tsx`

- 로그인한 관리자에게만 public 화면 위에 추가로 보이는 편집 UI 문구를 바꾸고 싶다:
  - `InlineBlogEditorSection.tsx`
  - `PublicWorksInlineCreateShell.tsx`
  - 각 public page의 `InlineAdminEditorShell` 사용부

- 운영자 전용 `/admin/*` 문구를 바꾸고 싶다:
  - `src/app/admin/*`
  - `src/components/admin/*`
