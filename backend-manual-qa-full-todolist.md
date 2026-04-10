# Backend Manual QA Full Todolist

## 사전 조건
- [ ] Docker 환경 실행 (`docker compose up`) — PostgreSQL + ASP.NET Core + Nginx 정상 기동
- [ ] `GET /api/health` 응답 `{ "status": "ok" }` 확인
- [ ] Admin 시드 계정 (`admin@example.com`) 로그인 가능 확인
- [ ] CSRF 토큰 획득 가능 확인 (`GET /api/auth/csrf`)
- [ ] HTTP 클라이언트 준비 (curl / Postman / Insomnia / codex-api-test.mjs)
- [ ] 테스트용 MP4 파일 (10MB 이하), JPG/PNG 이미지, PDF 파일 준비
- [ ] YouTube 영상 URL 2~3개 준비

## 각 테스트마다 필수적으로 해야 하는 것
### API 요청/응답 로그를 남긴다 (curl 기록 또는 Postman collection export)

---

## A. 인증 · 세션 · CSRF — Auth Endpoint 전체 흐름

- [ ] `A-1` 세션 조회 (비인증)
  `GET /api/auth/session` — 쿠키 없이 요청
  통과 기준: 200, `{ "authenticated": false }` 반환

- [ ] `A-2` CSRF 토큰 발급
  `GET /api/auth/csrf`
  통과 기준: 200, `{ "requestToken": "...", "headerName": "X-CSRF-TOKEN" }` 반환

- [ ] `A-3` 테스트 로그인 (개발 환경)
  `GET /api/auth/test-login?email=admin@example.com`
  통과 기준: 302 리다이렉트, Set-Cookie 세션 쿠키 발급

- [ ] `A-4` 세션 조회 (인증 후)
  로그인 쿠키로 `GET /api/auth/session`
  통과 기준: 200, `{ "authenticated": true, "role": "admin", "email": "admin@example.com" }` 포함

- [ ] `A-5` Google OAuth 로그인 리다이렉트
  `GET /api/auth/login` (OAuth 설정된 환경)
  통과 기준: 302, Google OIDC authorize URL로 리다이렉트

- [ ] `A-6` 로그아웃 (POST + CSRF)
  `POST /api/auth/logout` + `X-CSRF-TOKEN` 헤더 + 세션 쿠키
  통과 기준: 200, `{ "redirectUrl": "..." }`, 이후 세션 무효화

- [ ] `A-7` 로그아웃 — CSRF 없이 시도
  `POST /api/auth/logout` CSRF 토큰 없이
  통과 기준: 400 Bad Request

- [ ] `A-8` 로그아웃 — GET 메서드 거부
  `GET /api/auth/logout`
  통과 기준: 405, `{ "title": "Logout requires POST" }` 반환

- [ ] `A-9` 비인증 상태에서 admin 엔드포인트 접근
  쿠키 없이 `GET /api/admin/works`
  통과 기준: 401 또는 302 로그인 리다이렉트

- [ ] `A-10` CSRF 보호 — admin mutation에 토큰 없이 요청
  `POST /api/admin/works` CSRF 토큰 없이
  통과 기준: 400 Bad Request

- [ ] `A-11` CSRF 보호 — 잘못된 토큰으로 요청
  `POST /api/admin/works` + `X-CSRF-TOKEN: invalid_token`
  통과 기준: 400 Bad Request

- [ ] `A-12` Rate Limiting — test-login 반복 요청
  `GET /api/auth/test-login?email=...` 300회 이상 빠르게 반복
  통과 기준: 일정 횟수 이후 429 Too Many Requests

- [ ] `A-13` 세션 만료 후 admin 접근
  로그인 → 세션 만료 시간 경과 → `GET /api/admin/works`
  통과 기준: 401 또는 302 (세션 슬라이딩/절대 만료 동작)

---

## B. 보안 헤더 · 미들웨어

- [ ] `B-1` 보안 헤더 확인
  임의의 응답 헤더 검사
  통과 기준: `X-Content-Type-Options: nosniff`, `Referrer-Policy`, `X-Frame-Options`, `Permissions-Policy`, `Content-Security-Policy` 모두 존재

- [ ] `B-2` CSP — media 경로 SAMEORIGIN
  `/media/*` 응답의 `X-Frame-Options` 확인
  통과 기준: SAMEORIGIN (iframe 허용)

- [ ] `B-3` CSP — 일반 경로 DENY
  `/api/health` 등 non-media 경로 `X-Frame-Options` 확인
  통과 기준: DENY

---

## C. Health Check

- [ ] `C-1` Health 엔드포인트
  `GET /api/health`
  통과 기준: 200, `{ "status": "ok", "service": "portfolio-api", "timestamp": "..." }`

- [ ] `C-2` 루트 리다이렉트
  `GET /` 요청
  통과 기준: `/api/health`로 리다이렉트

---

## D. Works CRUD — Admin 전체 흐름

- [ ] `D-1` Work 목록 조회
  `GET /api/admin/works` (인증됨)
  통과 기준: 200, 배열 반환, 각 항목에 id/title/slug/category/published/createdAt

- [ ] `D-2` Work 생성 — 최소 필드
  `POST /api/admin/works` + CSRF, body: `{ "title": "Test Work", "category": "web" }`
  통과 기준: 200, `{ "id": "guid", "slug": "test-work" }` 반환

- [ ] `D-3` Work 생성 — 전체 필드
  title, category, period, tags, published, contentJson, allPropertiesJson, thumbnailAssetId, iconAssetId 모두 포함
  통과 기준: 200, 모든 필드 정상 저장, 조회 시 일치

- [ ] `D-4` Work 생성 — 한글+특수문자 제목
  title: `"테스트 프로젝트 #1 (한글/영문) @2026!"`
  통과 기준: 200, slug 자동 생성 정상

- [ ] `D-5` Work 생성 — 제목 누락 검증
  body에 title 없이 요청
  통과 기준: 400 Bad Request, 검증 에러 메시지

- [ ] `D-6` Work 생성 — 카테고리 누락 검증
  body에 category 없이 요청
  통과 기준: 400 Bad Request

- [ ] `D-7` Work 생성 — 태그 길이 초과 검증
  태그 하나를 51자 이상으로 설정
  통과 기준: 400 Bad Request

- [ ] `D-8` Work 상세 조회
  `GET /api/admin/works/{id}` (D-2에서 생성된 ID)
  통과 기준: 200, 생성 시 입력한 모든 필드 포함

- [ ] `D-9` Work 수정
  `PUT /api/admin/works/{id}` + CSRF, 제목/카테고리/태그 변경
  통과 기준: 200, slug 업데이트 반영

- [ ] `D-10` Work 수정 — 존재하지 않는 ID
  `PUT /api/admin/works/{random-guid}`
  통과 기준: 404 Not Found

- [ ] `D-11` Work 삭제
  `DELETE /api/admin/works/{id}` + CSRF
  통과 기준: 204 No Content, 이후 조회 시 404

- [ ] `D-12` Work 삭제 — 존재하지 않는 ID
  `DELETE /api/admin/works/{random-guid}`
  통과 기준: 404 Not Found

- [ ] `D-13` Work 삭제 — 비디오 연결된 Work
  비디오가 있는 work 삭제
  통과 기준: 204, 비디오 cleanup job이 큐에 등록됨 (DB 확인 또는 로그)

---

## E. Public Works 조회

- [ ] `E-1` Public Works 목록 (기본 페이지네이션)
  `GET /api/public/works`
  통과 기준: 200, `{ items: [...], page: 1, pageSize: 6, totalCount: N }`

- [ ] `E-2` Public Works 목록 (커스텀 페이지네이션)
  `GET /api/public/works?page=2&pageSize=3`
  통과 기준: 200, page=2, pageSize=3 반영, items 최대 3개

- [ ] `E-3` Public Works 목록 — 비발행 제외
  published=false인 work 생성 후 public 목록 조회
  통과 기준: 비발행 work이 items에 포함되지 않음

- [ ] `E-4` Public Work 상세 (slug)
  `GET /api/public/works/{slug}`
  통과 기준: 200, 전체 상세 (content, videos, tags, thumbnailUrl 포함)

- [ ] `E-5` Public Work 상세 — 존재하지 않는 slug
  `GET /api/public/works/nonexistent-slug-12345`
  통과 기준: 404 Not Found

- [ ] `E-6` Public Work 상세 — 비디오 포함 렌더
  비디오가 있는 work의 slug으로 조회
  통과 기준: videos 배열에 sourceType/sourceId/order 포함

---

## F. Work 비디오 — Upload · YouTube · Reorder · Delete

- [ ] `F-1` YouTube 비디오 추가
  `POST /api/admin/works/{id}/videos/youtube` + CSRF
  body: `{ "youtubeUrlOrId": "https://youtube.com/watch?v=dQw4w9WgXcQ", "expectedVideosVersion": 0 }`
  통과 기준: 200, videos 배열에 youtube sourceType 포함, videosVersion 증가

- [ ] `F-2` YouTube 비디오 추가 — ID만 입력
  body: `{ "youtubeUrlOrId": "dQw4w9WgXcQ", "expectedVideosVersion": N }`
  통과 기준: 200, sourceId에 정규화된 ID

- [ ] `F-3` YouTube 비디오 추가 — 유효하지 않은 URL
  body: `{ "youtubeUrlOrId": "not-a-url", "expectedVideosVersion": N }`
  통과 기준: 400 Bad Request

- [ ] `F-4` YouTube 비디오 추가 — 버전 충돌
  expectedVideosVersion을 실제보다 낮게 설정
  통과 기준: 409 Conflict

- [ ] `F-5` 비디오 업로드 URL 발급 (presigned)
  `POST /api/admin/works/{id}/videos/upload-url` + CSRF
  body: `{ "fileName": "test.mp4", "contentType": "video/mp4", "size": 1048576, "expectedVideosVersion": N }`
  통과 기준: 200, `{ "uploadSessionId": "guid", "uploadUrl": "..." }`

- [ ] `F-6` 비디오 업로드 URL 발급 — 파일명 누락
  fileName 빈 문자열
  통과 기준: 400 Bad Request

- [ ] `F-7` 비디오 업로드 URL 발급 — size 0
  size: 0
  통과 기준: 400 Bad Request

- [ ] `F-8` 로컬 비디오 업로드
  `POST /api/admin/works/{id}/videos/upload?uploadSessionId={sessionId}` multipart file
  통과 기준: 200, `{ "fileSize": N, "videosVersion": N }`

- [ ] `F-9` 비디오 업로드 확인 (confirm)
  `POST /api/admin/works/{id}/videos/confirm` + CSRF
  body: `{ "uploadSessionId": "guid", "expectedVideosVersion": N }`
  통과 기준: 200, videos 배열에 local sourceType 포함

- [ ] `F-10` 비디오 업로드 확인 — 버전 충돌
  expectedVideosVersion 불일치
  통과 기준: 409 Conflict

- [ ] `F-11` 비디오 순서 변경
  `PUT /api/admin/works/{id}/videos/order` + CSRF
  body: `{ "orderedVideoIds": ["guid-b", "guid-a"], "expectedVideosVersion": N }`
  통과 기준: 200, 순서 변경된 videos, videosVersion 증가

- [ ] `F-12` 비디오 순서 변경 — 버전 충돌
  expectedVideosVersion 불일치
  통과 기준: 409 Conflict

- [ ] `F-13` 비디오 순서 변경 — 누락된 video ID
  orderedVideoIds에 존재하지 않는 ID 포함
  통과 기준: 409 Conflict 또는 400

- [ ] `F-14` 비디오 삭제
  `DELETE /api/admin/works/{id}/videos/{videoId}?expectedVideosVersion=N` + CSRF
  통과 기준: 200, videos에서 제거, videosVersion 증가

- [ ] `F-15` 비디오 삭제 — cleanup job 확인
  삭제 후 DB에서 VideoStorageCleanupJob 레코드 존재 확인
  통과 기준: cleanup job 레코드 생성됨

- [ ] `F-16` 전체 비디오 파이프라인 E2E
  URL 발급 → 파일 업로드 → confirm → YouTube 추가 → 순서 변경 → 1개 삭제
  통과 기준: 모든 단계 성공, 최종 videos 상태 정확

---

## G. Blogs CRUD — Admin 전체 흐름

- [ ] `G-1` Blog 목록 조회
  `GET /api/admin/blogs` (인증됨)
  통과 기준: 200, 배열 반환, 각 항목에 id/title/slug/published/createdAt

- [ ] `G-2` Blog 생성 — 최소 필드
  `POST /api/admin/blogs` + CSRF, body: `{ "title": "Test Blog" }`
  통과 기준: 200, `{ "id": "guid", "slug": "test-blog" }`

- [ ] `G-3` Blog 생성 — 전체 필드
  title, tags, published, contentJson 모두 포함
  통과 기준: 200, 모든 필드 정상 저장

- [ ] `G-4` Blog 생성 — 한글+특수문자 제목
  title: `"블로그 포스트 #1 — 테스트 (한/영) @특수!"`
  통과 기준: 200, slug 자동 생성

- [ ] `G-5` Blog 생성 — 제목 누락 검증
  title 없이 요청
  통과 기준: 400 Bad Request

- [ ] `G-6` Blog 생성 — 태그 길이 초과
  태그 51자 이상
  통과 기준: 400 Bad Request

- [ ] `G-7` Blog 상세 조회
  `GET /api/admin/blogs/{id}`
  통과 기준: 200, 전체 상세 포함

- [ ] `G-8` Blog 수정
  `PUT /api/admin/blogs/{id}` + CSRF, 제목/태그/내용 변경
  통과 기준: 200, slug 업데이트

- [ ] `G-9` Blog 수정 — 존재하지 않는 ID
  `PUT /api/admin/blogs/{random-guid}`
  통과 기준: 404 Not Found

- [ ] `G-10` Blog 삭제
  `DELETE /api/admin/blogs/{id}` + CSRF
  통과 기준: 204, 이후 조회 시 404

- [ ] `G-11` Blog 삭제 — 존재하지 않는 ID
  통과 기준: 404

---

## H. Public Blogs 조회

- [ ] `H-1` Public Blogs 목록 (기본)
  `GET /api/public/blogs`
  통과 기준: 200, 페이지네이션 구조 반환

- [ ] `H-2` Public Blogs 목록 (커스텀 페이지네이션)
  `GET /api/public/blogs?page=2&pageSize=4`
  통과 기준: page/pageSize 반영

- [ ] `H-3` Public Blogs — 비발행 제외
  published=false blog 생성 후 public 조회
  통과 기준: 비발행 미포함

- [ ] `H-4` Public Blog 상세 (slug)
  `GET /api/public/blogs/{slug}`
  통과 기준: 200, content/tags/createdAt 포함

- [ ] `H-5` Public Blog 상세 — 존재하지 않는 slug
  통과 기준: 404

---

## I. Pages 관리

- [ ] `I-1` Admin 페이지 목록
  `GET /api/admin/pages`
  통과 기준: 200, 시드된 페이지 목록 (introduction, contact 등)

- [ ] `I-2` Admin 페이지 목록 — slug 필터
  `GET /api/admin/pages?slugs=introduction,contact`
  통과 기준: 필터된 결과만 반환

- [ ] `I-3` 페이지 수정
  `PUT /api/admin/pages` + CSRF, body: `{ "id": "guid", "title": "New Title", "contentJson": "{...}" }`
  통과 기준: 200

- [ ] `I-4` 페이지 수정 — 제목 누락
  title 없이 요청
  통과 기준: 400 Bad Request

- [ ] `I-5` 페이지 수정 — 존재하지 않는 ID
  통과 기준: 404

- [ ] `I-6` 페이지 수정 — 제목 200자 초과
  통과 기준: 400 Bad Request

- [ ] `I-7` Public 페이지 조회 (slug)
  `GET /api/public/pages/introduction`
  통과 기준: 200, title/content/contentJson 포함

- [ ] `I-8` Public 페이지 — 존재하지 않는 slug
  통과 기준: 404

---

## J. Site Settings · Resume

- [ ] `J-1` Public Site Settings 조회
  `GET /api/public/site-settings`
  통과 기준: 200, ownerName/tagline/description 포함

- [ ] `J-2` Admin Site Settings 조회
  `GET /api/admin/site-settings`
  통과 기준: 200, admin 전용 필드 포함

- [ ] `J-3` Site Settings 수정
  `PUT /api/admin/site-settings` + CSRF, ownerName/tagline 변경
  통과 기준: 200, 이후 public 조회 시 변경 반영

- [ ] `J-4` Site Settings 수정 — resume asset 연결
  thumbnailAssetId로 PDF asset 연결
  통과 기준: resume 조회 시 해당 asset URL 반환

- [ ] `J-5` Site Settings 수정 — resume asset 해제
  resume asset을 null로 설정
  통과 기준: resume 조회 시 404 또는 빈 응답

- [ ] `J-6` Public Resume 조회
  `GET /api/public/resume`
  통과 기준: 200 (PDF asset 있을 때) 또는 404 (없을 때)

---

## K. 미디어 업로드 · 삭제

- [ ] `K-1` 이미지 업로드
  `POST /api/uploads` multipart, bucket: "content", file: JPG
  통과 기준: 200, `{ "id": "guid", "url": "/media/...", "path": "..." }`

- [ ] `K-2` PDF 업로드
  `POST /api/uploads` multipart, bucket: "thumbnails", file: PDF
  통과 기준: 200, asset 생성

- [ ] `K-3` 업로드된 파일 접근
  K-1에서 반환된 URL로 `GET /media/...`
  통과 기준: 200, 파일 정상 다운로드

- [ ] `K-4` Asset 삭제
  `DELETE /api/uploads?id={assetId}` + CSRF
  통과 기준: 200, 이후 URL 접근 시 404

- [ ] `K-5` Asset 삭제 — 존재하지 않는 ID
  통과 기준: 404, `{ "error": "Asset not found" }`

- [ ] `K-6` 업로드 — 비인증 시도
  쿠키 없이 `POST /api/uploads`
  통과 기준: 401

---

## L. Dashboard · Members · Home

- [ ] `L-1` Dashboard 요약
  `GET /api/admin/dashboard`
  통과 기준: 200, totalWorks/totalBlogs/totalPages/recentWorks/recentBlogs 포함

- [ ] `L-2` Dashboard — 수치 정확성
  Work 1개 생성 후 dashboard 재조회
  통과 기준: totalWorks 1 증가

- [ ] `L-3` Members 목록
  `GET /api/admin/members`
  통과 기준: 200, 각 멤버에 id/email/name/role, sessionKey/providerSubject/ipAddress 미포함

- [ ] `L-4` Public Home
  `GET /api/public/home`
  통과 기준: 200, featuredWorks/featuredBlogs 구조

- [ ] `L-5` Public Home — 데이터 정확성
  Works/Blogs 존재 시 featuredWorks/featuredBlogs에 항목 포함
  통과 기준: 비어있지 않은 배열

---

## M. AI Batch Processing

- [ ] `M-1` AI Runtime Config 조회
  `GET /api/admin/ai/runtime-config`
  통과 기준: 200, isConfigured/modelName 등 포함

- [ ] `M-2` 단일 Blog AI Fix
  `POST /api/admin/ai/blog-fix`, body: `{ "blogId": "guid", "fixType": "grammar" }`
  통과 기준: 200, 수정 결과 반환

- [ ] `M-3` Batch Blog AI Fix — Job 생성
  `POST /api/admin/ai/blog-fix-batch`, body: `{ "blogIds": ["guid1", "guid2"], "fixType": "style" }`
  통과 기준: 200, job 생성 확인

- [ ] `M-4` Batch Job — 상태 전이 관찰
  Job 생성 후 반복 조회
  통과 기준: Queued → Running → Completed 순서로 전이

- [ ] `M-5` Batch Job — 결과 적용
  Completed job의 결과를 apply
  통과 기준: 대상 blog 내용이 AI 수정 결과로 업데이트

- [ ] `M-6` Batch Job — 취소
  Queued 상태 job 취소 요청
  통과 기준: 상태 Cancelled로 변경, 더 이상 처리되지 않음

- [ ] `M-7` Batch Job — 완료 후 삭제
  Completed job 삭제 요청
  통과 기준: job 목록에서 제거

- [ ] `M-8` Work Enrich
  `POST /api/admin/ai/work-enrich`, body: `{ "workId": "guid", "enrichType": "description" }`
  통과 기준: 200, 보강된 결과 반환

- [ ] `M-9` AI — 비인증 접근
  쿠키 없이 AI 엔드포인트 접근
  통과 기준: 401

---

## N. 비디오 저장소 Cleanup (Background Worker)

- [ ] `N-1` Upload Session 만료
  presigned URL 발급 후 confirm 없이 대기 (5분 후)
  통과 기준: 만료된 session이 자동 정리됨 (DB에서 제거 또는 만료 상태)

- [ ] `N-2` 비디오 삭제 후 Cleanup Job 처리
  비디오 삭제 → VideoStorageCleanupJob 생성 확인 → Worker 처리 대기
  통과 기준: cleanup job이 처리되고, 실제 저장소에서 파일 제거

- [ ] `N-3` Stale Running Job 복구
  서버 재시작 시 Running 상태 AI job → Queued로 복원
  통과 기준: 서버 재시작 후 stale job이 Queued로 돌아감

---

## O. Optimistic Concurrency · 충돌 처리

- [ ] `O-1` 비디오 추가 — 동시 요청 충돌
  같은 work에 두 개의 YouTube 추가 요청을 동일 expectedVideosVersion으로 전송
  통과 기준: 첫 번째 성공(200), 두 번째 409 Conflict

- [ ] `O-2` 비디오 순서 변경 — 동시 충돌
  같은 work에 두 개의 reorder 요청을 동일 version으로 전송
  통과 기준: 하나만 성공, 나머지 409

- [ ] `O-3` 비디오 삭제 — version 불일치
  잘못된 expectedVideosVersion으로 삭제 시도
  통과 기준: 409 Conflict

---

## P. 입력 검증 · Edge Case

- [ ] `P-1` Work 생성 — 매우 긴 제목 (1000자)
  통과 기준: 적절한 에러 또는 truncation (서버 검증)

- [ ] `P-2` Blog 생성 — 빈 contentJson
  body: `{ "title": "Test", "contentJson": "" }`
  통과 기준: 200 (빈 내용 허용) 또는 400 (검증 규칙에 따라)

- [ ] `P-3` Work 생성 — 유효하지 않은 JSON metadata
  allPropertiesJson: `"not valid json"`
  통과 기준: 400 Bad Request

- [ ] `P-4` 페이지네이션 — 음수 page
  `GET /api/public/works?page=-1`
  통과 기준: 400 또는 page=1로 정규화

- [ ] `P-5` 페이지네이션 — pageSize 0
  `GET /api/public/works?pageSize=0`
  통과 기준: 400 또는 기본값 적용

- [ ] `P-6` 페이지네이션 — 매우 큰 pageSize
  `GET /api/public/works?pageSize=10000`
  통과 기준: 상한값 적용 또는 적절한 에러

- [ ] `P-7` Slug 특수문자 처리
  `/api/public/works/한글-슬러그` 또는 `/api/public/works/slug%20with%20spaces`
  통과 기준: 404 (존재하지 않으면) 또는 200 (존재하면), 500이 아닐 것

- [ ] `P-8` GUID 형식이 아닌 ID로 admin 조회
  `GET /api/admin/works/not-a-guid`
  통과 기준: 400 또는 404, 500이 아닐 것

---

## Q. 데이터 무결성 · Cascade 삭제

- [ ] `Q-1` Work 삭제 → 관련 비디오 cascade
  비디오 2개 있는 work 삭제
  통과 기준: work + videos 모두 삭제, cleanup job 생성

- [ ] `Q-2` Asset 삭제 — 참조 중인 asset
  work의 thumbnailAssetId로 사용 중인 asset 삭제 시도
  통과 기준: 삭제 성공 또는 적절한 에러 (orphan 처리 정책에 따라)

- [ ] `Q-3` 시드 데이터 무결성
  서버 최초 기동 후 DB 확인
  통과 기준: 2 Profiles, 3 Pages, 2 Works, 2 Blogs, 6 Assets, 1 SiteSetting 시드됨

- [ ] `Q-4` 시드 데이터 멱등성
  서버 재시작 2회
  통과 기준: 시드 데이터 중복 없이 동일 수량 유지

---

## R. Docker · 배포 · 인프라

- [ ] `R-1` Docker 빌드
  `docker build -t woong-blog-backend backend/`
  통과 기준: 빌드 성공, 이미지 생성

- [ ] `R-2` Docker Compose 기동
  `docker compose up`
  통과 기준: PostgreSQL + Backend + Nginx 모두 기동, health 200

- [ ] `R-3` 볼륨 persist 확인
  데이터 작성 → `docker compose down` → `docker compose up`
  통과 기준: 이전에 작성한 데이터 유지

- [ ] `R-4` 환경변수 설정 검증
  프로덕션 필수 환경변수 (Auth:ClientId, Auth:ClientSecret 등) 누락 시 기동
  통과 기준: 명확한 에러 메시지 또는 기동 실패 (silent fail 아님)

---

## 실행 순서 권장

```
1일차: A (인증 13개) → B (보안 3개) → C (Health 2개)
        ⤷ 모든 후속 테스트의 전제조건, 토큰/세션 확보

2일차: D (Works CRUD 13개) → E (Public Works 6개) → F-1~F-10 (비디오 기본)
        ⤷ 핵심 엔티티 CRUD + 비디오 업로드 기본

3일차: F-11~F-16 (비디오 고급) → G (Blogs 11개) → H (Public Blogs 5개)
        ⤷ 비디오 reorder/delete + Blog CRUD

4일차: I (Pages 8개) → J (Settings 6개) → K (Media 6개) → L (Dashboard 5개)
        ⤷ 부가 엔티티 전체

5일차: M (AI 9개) → N (Background 3개) → O (Concurrency 3개)
        ⤷ 비동기/백그라운드 처리

6일차: P (Edge Case 8개) → Q (Cascade 4개) → R (Docker 4개)
        ⤷ 경계값/인프라 최종 확인
```

---

## 요약

| 영역 | 시나리오 수 |
|------|-----------|
| A. 인증/세션/CSRF | 13 |
| B. 보안 헤더 | 3 |
| C. Health Check | 2 |
| D. Works CRUD (Admin) | 13 |
| E. Public Works | 6 |
| F. 비디오 파이프라인 | 16 |
| G. Blogs CRUD (Admin) | 11 |
| H. Public Blogs | 5 |
| I. Pages 관리 | 8 |
| J. Site Settings/Resume | 6 |
| K. 미디어 업로드 | 6 |
| L. Dashboard/Members/Home | 5 |
| M. AI Batch Processing | 9 |
| N. Background Worker | 3 |
| O. Concurrency 충돌 | 3 |
| P. 입력 검증/Edge Case | 8 |
| Q. Cascade/무결성 | 4 |
| R. Docker/인프라 | 4 |
| **합계** | **125** |
