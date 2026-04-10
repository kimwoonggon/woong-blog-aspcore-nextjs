# Frontend Manual QA Full Todolist

## 사전 조건
- [x] 로컬 또는 스테이징 환경에서 `docker compose up` + `npm run dev` 실행 상태 확인
- [x] Admin 계정 로그인 가능 확인
- [x] 테스트용 MP4 파일 준비
- [x] 테스트용 JPG/PNG 이미지 파일 준비
- [x] 테스트용 PDF 파일 준비
- [x] YouTube 영상 URL 너가 하나 추천해서 3개정도 넣는다

## 각 테스트마다 필수적으로 해야 하는 것
### 테스트 폴더에 실험 결과를 영상으로 남긴다.

## A. Work (작업) — 생성 · 편집 · 삭제 · 비디오 전체 흐름
- [x] `A-1` Work 신규 생성 (텍스트만)
  제목(한글+영문+특수문자), 카테고리, 기간, 태그, 본문 작성 후 저장
  통과 기준: admin works 목록 표시, public `/works` 카드 노출, `/works/[slug]` 상세 정상 렌더
- [x] `A-2` Work 신규 생성 + YouTube 영상
  작업 생성 중 YouTube URL 2개 추가 후 `Create And Add Videos`
  통과 기준: 영상 2개 work detail 표시, YouTube iframe 재생 가능, 자동 썸네일이 YouTube에서 추출
- [x] `A-3` Work 신규 생성 + MP4 업로드
  작업 생성 중 MP4 파일 2개 선택 후 저장
  통과 기준: presigned URL 업로드 완료, 영상 플레이어 렌더, 자동 썸네일이 MP4 프레임에서 추출
- [x] `A-4` Work 신규 생성 + YouTube 2개 + MP4 2개 혼합
  YouTube 2개 + MP4 2개 동시 추가 후 저장
  통과 기준: 4개 영상 모두 표시, 정렬 순서 정확, 썸네일은 MP4 우선
- [x] `A-5` 비디오 본문 삽입 (videoInline)
  `A-4` 저장 후 리다이렉트된 편집 페이지에서 각 영상 `Insert Into Body` 클릭
  통과 기준: 본문에 비디오 임베드 블록 삽입, 저장 후 public에서 inline 위치에 영상 렌더
- [x] `A-6` 비디오 순서 변경 (reorder)
  편집 페이지에서 영상 드래그로 순서 변경 후 저장
  통과 기준: 변경된 순서가 public detail에 반영
- [x] `A-7` Work 편집 — 제목/내용 수정
  기존 work 편집 후 제목 변경, 본문 추가 후 저장
  통과 기준: slug 변경 반영, public에서 수정된 내용 표시
- [x] `A-8` Work 편집 중 영상 추가
  기존 work(영상 없음) 편집 후 YouTube + MP4 각 1개 추가 후 저장
  통과 기준: 기존 내용 유지 + 신규 영상 추가
- [x] `A-9` Work 썸네일 수동 업로드
  작업 편집 후 커스텀 썸네일 이미지 업로드
  통과 기준: 자동 썸네일 대신 수동 썸네일 표시, works 목록 카드 반영
- [x] `A-10` Work 아이콘 업로드
  작업 편집 후 아이콘 이미지 업로드 후 저장
  통과 기준: 아이콘 표시 정상
- [x] `A-11` Work 메타데이터 JSON
  유효 JSON 입력 후 저장 성공, 잘못된 JSON 입력 시 클라이언트 검증 에러
  통과 기준: 유효 JSON은 저장, 무효 JSON은 에러 메시지 + 저장 차단
- [ ] `A-12` Work 삭제 (단건)
  Works 목록에서 하나 선택 후 삭제
  통과 기준: 목록에서 제거, public에서도 제거
- [x] `A-13` Work 벌크 삭제
  Works 목록에서 3개 선택 후 일괄 삭제
  통과 기준: 3개 모두 제거
- [x] `A-14` Work 검색 + 페이지네이션
  Admin works에서 제목 검색 후 결과 확인, 첫/끝 페이지 이동
  통과 기준: 검색 필터 정상, 페이지네이션 번호/화살표 동작
- [x] `A-15` Public works 반응형 페이지네이션
  데스크톱(8개), 태블릿(3개), 모바일(2개) 뷰포트에서 works 목록 확인
  통과 기준: 각 뷰포트별 페이지 사이즈 정확
- [x] `A-16` Public work detail — related works
  Work 상세 하단 관련 작업 카드 확인, 페이지네이션 동작 확인
  통과 기준: 관련 작업 표시, 넘기기 가능

## B. Blog — 생성 · 편집 · 이미지 · AI Batch · 페이지네이션
- [x] `B-1` Blog 신규 작성 (텍스트)
  Admin → Blog → 새 글 → 제목, 태그, 본문 작성 후 저장
  통과 기준: 즉시 발행, public `/blog` 카드 노출, `/blog/[slug]` 정상
- [x] `B-2` Blog 본문에 이미지 삽입
  에디터에서 이미지 업로드 버튼 → 파일 선택 → 본문 삽입 → 저장
  통과 기준: 이미지 업로드 완료, 본문 표시, public에서 이미지 렌더
- [x] `B-3` Blog 편집 — 제목/태그/본문 수정
  기존 글 편집 후 제목 변경, 태그 추가/삭제, 본문 수정 후 저장
  통과 기준: 수정 내용 반영, slug 변경 시 새 URL 접근 가능
- [x] `B-4` Blog 특수문자 입력
  제목에 한글+영문+특수문자 혼합 입력 후 저장
  통과 기준: 저장 성공, 표시 깨짐 없음
- [x] `B-5` Blog 삭제 (단건 + 벌크)
  단건 삭제 1회, 벌크 삭제(3개 선택) 1회
  통과 기준: 모두 정상 제거
- [x] `B-6` Blog 검색 + 페이지네이션
  Admin blog 제목 검색, 첫/끝 페이지 이동
  통과 기준: 동작 정상
- [x] `B-7` Public blog 반응형 페이지네이션
  데스크톱(12개), 태블릿(4개), 모바일(2개) 확인
  통과 기준: 뷰포트별 사이즈 정확
- [x] `B-8` Public blog detail — related posts
  블로그 상세 하단 관련 글 확인, 페이지네이션 확인
  통과 기준: 표시 및 넘기기 정상
- [x] `B-9` AI Batch Fix — Job 생성
  Admin Blog → Batch AI 패널 열기 → 블로그 선택 → 모델/effort 설정 → Job 생성
  통과 기준: Job 생성 성공, 상태 `queued` 표시
- [x] `B-10` AI Batch Fix — Polling & 진행률
  Job 생성 후 `queued → running → completed` 상태 전환 관찰
  통과 기준: 2초 간격 폴링 업데이트, completed 시 결과 표시
- [x] `B-11` AI Batch Fix — 결과 미리보기 & 적용
  Completed job → 각 블로그 preview 확인 → Apply
  통과 기준: 수정된 내용이 블로그에 반영
- [x] `B-12` Notion 워크스페이스 뷰
  Admin → Blog → Notion 탭 → 멀티 블로그 네비게이션
  통과 기준: Notion 형태 UI 정상, autosave 동작

## C. 에디터 (TiptapEditor) — 리치 텍스트 · 임베드 · 슬래시 커맨드
- [x] `C-1` 기본 서식
  Bold, Italic, Heading 1-3, 리스트, 인용, 코드블록 적용
  통과 기준: 각 서식 토글 정상, 저장 후 public 렌더 일치
- [x] `C-2` 링크 삽입/수정
  텍스트 선택 → 링크 추가 → URL 입력 → 저장
  통과 기준: 링크 클릭 시 정상 이동
- [x] `C-3` 이미지 드래그 & 드롭
  이미지 파일을 에디터 영역에 드롭
  통과 기준: 업로드 후 본문에 이미지 삽입
- [x] `C-4` 이미지 붙여넣기
  클립보드에서 이미지 `Ctrl+V`
  통과 기준: 업로드 후 본문에 이미지 삽입
- [x] `C-5` 비디오 임베드 삽입
  Work 편집 시 비디오 임베드 블록 삽입 (`Insert Into Body`)
  통과 기준: 에디터에 비디오 프리뷰 표시, 중복 삽입 방지(`nonce`)
- [x] `C-6` 슬래시 커맨드
  본문에 `/` 입력 → 커맨드 목록 표시 → 항목 선택
  통과 기준: 커맨드 팔레트 열림, 선택한 블록 삽입
- [x] `C-7` 코드 블록 (Syntax Highlight)
  코드 블록 삽입 → 언어 선택 → 코드 입력
  통과 기준: 하이라이팅 표시, public에서 동일 렌더
- [x] `C-8` HTML 커스텀 블록
  에디터에서 HTML 블록 삽입 → 커스텀 HTML 작성
  통과 기준: 에디터 내 렌더, public InteractiveRenderer에서 정상 표시
- [x] `C-9` Three.js 블록
  에디터에서 Three.js 임베드 블록 삽입
  통과 기준: 3D 뷰어 정상 렌더
- [x] `C-10` Bubble Menu
  텍스트 선택 시 버블 메뉴 표시 → 서식 적용
  통과 기준: 메뉴 7+ 버튼 정상 동작
- [x] `C-11` 에디터 내용 동기화
  외부 prop으로 content 변경 시 editor 반영, editor 변경 시 onUpdate 콜백 확인
  통과 기준: 양방향 sync 정상

## D. 인증 · 보안 · CSRF
- [ ] `D-1` Google 로그인
  로그인 페이지 → Google 로그인 → 리다이렉트
  통과 기준: 세션 생성, admin 접근 가능
- [x] `D-2` 로컬 Admin 로그인 (dev)
  개발 환경에서 로컬 admin 로그인
  통과 기준: 세션 정상
- [x] `D-3` 로그아웃
  로그인 상태 → 로그아웃 → admin 접근 시도
  통과 기준: 세션 파기, `/login` 리다이렉트
- [x] `D-4` 비인증 admin 접근
  로그아웃 상태에서 `/admin/dashboard` 직접 접근
  통과 기준: `/login` 리다이렉트
- [x] `D-5` CSRF 토큰 검증
  mutation 요청 시 CSRF 토큰 없이 전송
  통과 기준: `400` 에러 반환
- [ ] `D-6` 세션 만료 후 저장 시도
  오래 열어둔 편집 페이지에서 저장
  통과 기준: 에러 처리 또는 재로그인 안내

## E. 페이지 관리 · 사이트 설정 · 이력서
- [x] `E-1` 소개 페이지 편집
  Admin Pages → Introduction 내용 수정 → 저장
  통과 기준: public `/introduction` 반영
- [x] `E-2` 연락처 페이지 편집
  Admin Pages → Contact 내용 수정 → 저장
  통과 기준: public `/contact` 반영
- [x] `E-3` 홈 페이지 편집
  Admin → 프로필 이미지 업로드, 헤드라인 수정, 소개 텍스트 수정 → 저장
  통과 기준: public `/` 반영, 이미지 표시
- [x] `E-4` 사이트 설정
  Owner name, tagline, 소셜 링크 수정 → 저장
  통과 기준: footer, 타이틀에 반영
- [x] `E-5` 이력서 PDF 업로드
  Resume PDF 업로드 → 저장
  통과 기준: public `/resume` 다운로드 링크 정상
- [x] `E-6` 이력서 비PDF 거부
  JPG 파일을 resume로 업로드 시도
  통과 기준: 거부 에러 메시지
- [x] `E-7` 페이지 제목 길이 제한
  200자 초과 제목 입력 시도
  통과 기준: 거부 에러 메시지

## F. Public 인라인 편집 (Admin 인증 상태)
- [x] `F-1` Work 인라인 편집
  Public `/works/[slug]` → `작업 수정` 클릭 → 인라인 에디터에서 수정 → 저장
  통과 기준: 모달/오버레이 열림, 수정 반영, 닫기 후 페이지 갱신
- [x] `F-2` Blog 인라인 편집
  Public `/blog/[slug]` → `글 수정` 클릭 → 인라인 수정 → 저장
  통과 기준: 동일
- [x] `F-3` 소개 인라인 편집
  `/introduction` → `소개글 수정` 클릭 → 수정 → 저장
  통과 기준: 동일
- [x] `F-4` 연락처 인라인 편집
  `/contact` → `문의글 수정` 클릭 → 수정 → 저장
  통과 기준: 동일
- [x] `F-5` Works 목록 `새 작업 쓰기`
  `/works` → `새 작업 쓰기` → 인라인 생성
  통과 기준: 생성 후 목록에 추가
- [x] `F-6` Blog 목록 `새 글 쓰기`
  `/blog` → `새 글 쓰기` → 인라인 생성
  통과 기준: 생성 후 목록에 추가
- [x] `F-7` 비인증 시 편집 버튼 숨김
  로그아웃 상태에서 동일 페이지 확인
  통과 기준: 편집/생성 버튼 없음

## G. 대시보드 · 멤버 · 네비게이션
- [x] `G-1` 대시보드 요약 카드
  Admin Dashboard → 총 조회수, Works 수, Blog 수 카드 확인
  통과 기준: 숫자 표시 정상, 실제 데이터와 일치
- [x] `G-2` 대시보드 최근 항목
  최근 Works/Blogs 목록 → 클릭 시 편집 페이지 이동
  통과 기준: 리스트 정상, 링크 동작
- [x] `G-3` 멤버 목록
  Admin → Members → 사용자 목록 확인
  통과 기준: email, provider 표시, sessionKey/IP 등 민감 필드 숨김
- [x] `G-4` 사이드바 네비게이션
  Admin 모든 메뉴 이동 (Dashboard, Works, Blog, Members, Pages)
  통과 기준: 각 메뉴 정상 로드
- [x] `G-5` Public 네비게이션
  Home → Works → Blog → Resume → Introduction → Contact
  통과 기준: 모든 링크 정상, 모바일 햄버거 메뉴 동작

## H. 반응형 · 레이아웃 · Edge Case
- [x] `H-1` 데스크톱 카드 정렬
  Works/Blog 목록 데스크톱에서 행 정렬 확인
  통과 기준: 카드 가로 정렬, 균등 간격
- [x] `H-2` 모바일 카드 스택
  같은 페이지 모바일 뷰포트 확인
  통과 기준: 카드 세로 스택
- [x] `H-3` Edge navigation 화살표
  Works/Blog에서 좌우 페이지 화살표 확인
  통과 기준: 화살표 표시, 클릭 시 페이지 이동, Introduction에는 화살표 없음
- [ ] `H-4` 빈 목록 상태
  Works 모두 삭제 후 public `/works`
  통과 기준: 빈 상태 표시, 에러 아님
- [x] `H-5` 매우 긴 본문
  5000자 이상 본문 작성 → 저장 → public 확인
  통과 기준: 저장 성공, 렌더 정상, 스크롤 동작
