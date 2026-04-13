# Playwright 통합 테스트 플랜 (2026-04-12)

> **목적**: Admin/Public 프론트엔드 대규모 업데이트 이후, 기능(Functional) + 웹 품질(Web Quality) + UI 심미성(Visual/Aesthetic) 전체 영역에 대한 통합 테스트 수행
> **도구**: Playwright (chromium-public, chromium-authenticated, chromium-runtime-auth)
> **기준**: UI/UX Pro Max 가이드라인 + Vercel Web Interface Guidelines + WCAG 2.1 AA

---

## 테스트 구조

```
tests/
├── integration/
│   ├── admin/          # Admin 기능 통합 테스트
│   ├── public/         # Public 기능 통합 테스트
│   ├── auth/           # 인증/권한 통합 테스트
│   └── cross-cutting/  # 공통 관심사 (다크모드, 반응형 등)
├── visual/
│   ├── admin/          # Admin UI 심미성 테스트
│   ├── public/         # Public UI 심미성 테스트
│   └── responsive/     # 반응형 레이아웃 심미성
└── quality/
    ├── accessibility/  # 접근성 테스트
    ├── performance/    # 성능 관련 UI 테스트
    └── animation/      # 애니메이션/모션 테스트
```

---

## Part 1: 기능 통합 테스트 (Functional Integration)

### A. Admin 기능 테스트

#### A-1. 인증 & 라우팅
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| AF-001 | Admin 로그인 플로우 | 이메일/비밀번호 입력 → 로그인 → 대시보드 리다이렉트 | CRITICAL |
| AF-002 | 미인증 접근 차단 | `/admin/*` 경로 미인증 시 `/login` 으로 리다이렉트 | CRITICAL |
| AF-003 | 권한 없는 사용자 차단 | Admin 역할 아닌 사용자 → `/` 로 리다이렉트 | CRITICAL |
| AF-004 | 로그아웃 | 세션 제거 후 로그인 페이지로 이동, 쿠키 삭제 확인 | HIGH |
| AF-005 | 세션 만료 처리 | 만료된 토큰으로 API 호출 시 적절한 에러 처리 | HIGH |

#### A-2. 대시보드 (`/admin/dashboard`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| AF-010 | 통계 카드 데이터 로딩 | Total Views, Total Works, Total Blog Posts 정확한 표시 | HIGH |
| AF-011 | 퀵 내비게이션 | Members, Blog Notion View, Open Site 버튼 정상 이동 | MEDIUM |
| AF-012 | 최근 콘텐츠 표시 | 최근 Works, Blogs 컬렉션 정확한 렌더링 | MEDIUM |
| AF-013 | 데이터 로딩 실패 | API 에러 시 에러 패널 fallback 표시 | MEDIUM |

#### A-3. Blog 관리 (`/admin/blog`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| AF-020 | Blog 목록 조회 | 페이지네이션 포함 전체 목록 렌더링 | CRITICAL |
| AF-021 | Blog 검색 | 제목/태그 검색 → 실시간 필터링 | HIGH |
| AF-022 | Blog 생성 (new) | 제목, Excerpt, 태그, 콘텐츠 입력 → 저장 → 목록 반영 | CRITICAL |
| AF-023 | Blog 수정 (edit) | 기존 데이터 로딩 → 수정 → 저장 → 변경 반영 | CRITICAL |
| AF-024 | Blog 발행/비발행 토글 | Published 체크박스 토글 → API 반영 확인 | HIGH |
| AF-025 | Blog 삭제 | 확인 다이얼로그 → 삭제 → 목록에서 제거 | HIGH |
| AF-026 | Blog 일괄 삭제 | 체크박스 다중 선택 → 일괄 삭제 | HIGH |
| AF-027 | Blog 페이지네이션 | 12/8/6 페이지 사이즈 전환, 페이지 이동 | MEDIUM |
| AF-028 | TipTap 에디터 동작 | 리치 텍스트 편집, 링크 삽입, 서식 적용 | HIGH |
| AF-029 | Blog 이미지 업로드 | 에디터 내 이미지 삽입, S3 업로드 | HIGH |
| AF-030 | AI Fix 다이얼로그 | AI 콘텐츠 수정 요청 → 결과 적용 | MEDIUM |
| AF-031 | Blog 유효성 검사 | 빈 제목, 과도한 입력 등 에러 메시지 | HIGH |
| AF-032 | 미저장 경고 | 수정 중 페이지 이탈 시 경고 다이얼로그 | MEDIUM |

#### A-4. Blog Notion 워크스페이스 (`/admin/blog/notion`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| AF-040 | Notion 2패인 레이아웃 | 좌측 문서 목록 + 우측 에디터 정상 렌더링 | HIGH |
| AF-041 | 문서 선택 & 전환 | 좌측 목록 클릭 → 우측 에디터 로딩 | HIGH |
| AF-042 | 자동 저장 | 편집 후 자동 저장 트리거 → 상태 표시 (idle/saving/saved/error) | HIGH |
| AF-043 | 라이브러리 패널 검색 | 문서 검색 필터링 | MEDIUM |
| AF-044 | 라이브러리 시트 열기/닫기 | Sheet 컴포넌트 토글 | MEDIUM |
| AF-045 | 문서 정보 패널 | Updated At, Published At 타임스탬프 표시 | LOW |
| AF-046 | 힌트 dismiss | 신규 사용자 힌트 표시 → 닫기 | LOW |

#### A-5. Works 관리 (`/admin/works`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| AF-050 | Works 목록 조회 | 페이지네이션 포함 전체 목록 렌더링 | CRITICAL |
| AF-051 | Works 검색/필터 | 제목, 카테고리, 태그 기반 검색 | HIGH |
| AF-052 | Work 생성 (new) | General/Media/Content 탭 → 데이터 입력 → 저장 | CRITICAL |
| AF-053 | Work 수정 (edit) | 기존 데이터 로딩 → 수정 → 저장 | CRITICAL |
| AF-054 | Work 발행/비발행 | Published 토글 → API 반영 | HIGH |
| AF-055 | Work 삭제 (단건) | 확인 다이얼로그 → 삭제 → 토스트 알림 | HIGH |
| AF-056 | Work 일괄 삭제 | 다중 선택 → 일괄 삭제 | HIGH |
| AF-057 | Work 페이지네이션 | 12/8/6 반응형 사이즈 전환 | MEDIUM |
| AF-058 | 썸네일 이미지 업로드 | 이미지 파일 선택 → S3 업로드 → 미리보기 | HIGH |
| AF-059 | 비디오 업로드 (S3) | 프리사인 URL → S3 업로드 → 콘펌 | HIGH |
| AF-060 | YouTube 비디오 추가 | YouTube URL 입력 → 저장 | HIGH |
| AF-061 | 비디오 자동 썸네일 | 비디오 업로드 → 프레임 캡처 → 썸네일 생성 | MEDIUM |
| AF-062 | 비디오 순서 드래그 | 드래그앤드롭으로 비디오 순서 변경 → API 반영 | MEDIUM |
| AF-063 | 비디오 삭제 | 개별 비디오 삭제 | MEDIUM |
| AF-064 | 아이콘 업로드 | Work 아이콘 이미지 업로드 | LOW |
| AF-065 | Work 유효성 검사 | 필수 필드 누락, 특수 입력, 극단적 입력 | HIGH |
| AF-066 | 탭 전환 (General/Media/Content) | 탭 간 데이터 유지, 정상 전환 | MEDIUM |

#### A-6. 페이지 & 설정 (`/admin/pages`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| AF-070 | 사이트 설정 저장 | Owner Name, Tagline, 소셜 미디어 URL 저장 | HIGH |
| AF-071 | 홈페이지 편집 | Headline, Intro, 프로필 이미지 업로드 | HIGH |
| AF-072 | Introduction 페이지 편집 | 제목, 콘텐츠 저장 | MEDIUM |
| AF-073 | Contact 페이지 편집 | 제목, 콘텐츠 저장 | MEDIUM |
| AF-074 | Resume 업로드 | PDF 업로드 → 링크 표시 → 다운로드 확인 | HIGH |
| AF-075 | Resume 삭제 | 기존 PDF 삭제 → 연결 해제 | MEDIUM |
| AF-076 | Resume 파일 유효성 | non-PDF 파일 거부 | MEDIUM |
| AF-077 | 사이트 설정 극단적 입력 | 매우 긴 이름, 특수 문자, 빈 값 | MEDIUM |

#### A-7. 멤버 관리 (`/admin/members`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| AF-080 | 멤버 목록 표시 | 이름, 이메일, 역할(Admin/User) 표시 | MEDIUM |
| AF-081 | 읽기 전용 확인 | 편집/삭제 액션 없음 확인 | LOW |

#### A-8. AI 일괄 작업
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| AF-090 | AI Batch 실행 | 다중 선택 → AI Enhancement → 배치 패널 표시 | MEDIUM |
| AF-091 | AI Batch 상태 폴링 | 진행 상태 → 완료/실패 표시 | MEDIUM |
| AF-092 | AI Batch 결과 적용 | 배치 결과 → 개별 블로그 적용 | MEDIUM |
| AF-093 | AI Batch 취소 | 진행 중 취소 동작 | LOW |

---

### B. Public 기능 테스트

#### B-1. 홈페이지 (`/`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| PF-001 | Hero 섹션 렌더링 | Headline, Intro, 프로필 이미지, CTA 버튼 | CRITICAL |
| PF-002 | Featured Works 섹션 | 추천 Works 카드 렌더링, 이미지, 카테고리 배지 | HIGH |
| PF-003 | Recent Blog 섹션 | 최근 블로그 카드, gradient stripe, 태그, 날짜 | HIGH |
| PF-004 | CTA 섹션 | Works, Blog, Introduction 퀵 네비게이션 박스 | MEDIUM |
| PF-005 | CTA 버튼 동작 | "View My Works", "Read Blog" 클릭 → 올바른 라우팅 | HIGH |

#### B-2. Blog 페이지 (`/blog`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| PF-010 | Blog 목록 렌더링 | 3-column grid, 카드 데이터(날짜, 태그, 제목, excerpt) | CRITICAL |
| PF-011 | Blog 페이지네이션 | Edge nav + 하단 페이지 번호, 이전/다음/처음/마지막 | HIGH |
| PF-012 | 반응형 페이지 사이즈 | Desktop 12, Tablet 4, Mobile 2 자동 전환 | HIGH |
| PF-013 | URL 쿼리 연동 | `?page=2&pageSize=12` URL 파라미터 연동 | MEDIUM |
| PF-014 | 빈 상태 처리 | 게시글 없을 때 안내 메시지 | MEDIUM |

#### B-3. Blog 상세 (`/blog/[slug]`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| PF-020 | 블로그 콘텐츠 렌더링 | HTML 콘텐츠 블록 정상 렌더링 (h2, h3, p, code, img) | CRITICAL |
| PF-021 | Table of Contents | H2/H3 헤딩 자동 수집 → 목차 생성 → 클릭 네비게이션 | HIGH |
| PF-022 | TOC 스크롤 하이라이팅 | 스크롤 시 활성 헤딩 인디케이터 업데이트 | MEDIUM |
| PF-023 | 관련 포스트 네비게이션 | 이전/다음 블로그 카드, 멀티 페이지 네비게이션 | HIGH |
| PF-024 | Admin 인라인 편집 버튼 | 관리자 로그인 시 편집 버튼 표시, 비로그인 시 숨김 | HIGH |
| PF-025 | SEO 메타데이터 | title, description 태그 정확성 | MEDIUM |
| PF-026 | 인라인 편집 저장 | 관리자 인라인 편집 → 저장 → 반영 | HIGH |
| PF-027 | 미저장 경고 (인라인) | 인라인 편집 중 이탈 시 경고 | MEDIUM |

#### B-4. Works 페이지 (`/works`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| PF-030 | Works 목록 렌더링 | 4-column grid, 카드 데이터(썸네일, 날짜, 카테고리, 태그, 제목) | CRITICAL |
| PF-031 | Works 페이지네이션 | Edge nav + 하단 페이지 번호 | HIGH |
| PF-032 | 반응형 페이지 사이즈 | Desktop 8, Tablet 4, Mobile 2 자동 전환 | HIGH |
| PF-033 | 썸네일 없는 Works | placeholder 이미지 정상 표시 | MEDIUM |
| PF-034 | Description CTA | "Start a conversation", "Read the notes" 링크 동작 | LOW |

#### B-5. Work 상세 (`/works/[slug]`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| PF-040 | Work 콘텐츠 렌더링 | HTML 블록 + 메타데이터 (제목, 날짜, 카테고리, 기간) | CRITICAL |
| PF-041 | 비디오 재생 | Native 비디오 플레이어 재생/일시정지 | HIGH |
| PF-042 | YouTube 임베드 | youtube-nocookie 임베드 정상 로딩 | HIGH |
| PF-043 | 비디오 정렬 순서 | sortOrder 기준 비디오 렌더링 순서 확인 | MEDIUM |
| PF-044 | Table of Contents | 목차 생성 → 클릭 네비게이션 | HIGH |
| PF-045 | 관련 Works 네비게이션 | 이전/다음 Work 카드 | HIGH |
| PF-046 | Admin 인라인 편집 | 관리자 인라인 편집 → 저장 | HIGH |
| PF-047 | Work 인라인 생성 | 인라인 생성 → 리다이렉트 | MEDIUM |

#### B-6. Introduction 페이지 (`/introduction`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| PF-050 | 콘텐츠 렌더링 | 블록 기반 콘텐츠 (헤딩, 문단, 리스트, 이미지, 코드) | HIGH |
| PF-051 | Admin 편집 버튼 | 관리자 로그인 시 편집 가능 | MEDIUM |
| PF-052 | 페이지 헤더 | "About the work" 라벨, 제목, 설명 | MEDIUM |

#### B-7. Contact 페이지 (`/contact`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| PF-060 | 콘텐츠 렌더링 | contact 콘텐츠 블록 표시 | HIGH |
| PF-061 | Fallback 이메일 표시 | mailto 링크 없을 때 이메일 주소 직접 표시 | MEDIUM |
| PF-062 | Admin 편집 버튼 | 관리자 편집 가능 | MEDIUM |

#### B-8. Resume 페이지 (`/resume`)
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| PF-070 | PDF 뷰어 표시 | PDF 이력서 뷰어 정상 렌더링 | HIGH |
| PF-071 | 다운로드 버튼 | PDF 다운로드 동작 | HIGH |
| PF-072 | PDF 없는 경우 | 미등록 상태 안내 메시지 | MEDIUM |

#### B-9. 네비게이션 & 레이아웃
| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| PF-080 | Navbar 링크 동작 | Home, Introduction, Works, Blog, Contact, Resume 모든 링크 | CRITICAL |
| PF-081 | 테마 토글 | Light/Dark/System 모드 전환 | HIGH |
| PF-082 | 모바일 햄버거 메뉴 | < 1024px 에서 메뉴 열기/닫기 | HIGH |
| PF-083 | Footer 링크 동작 | 모든 Footer 내비게이션 링크 | MEDIUM |
| PF-084 | Footer 소셜 아이콘 | 설정된 소셜 미디어만 조건부 표시 | MEDIUM |
| PF-085 | 로그인/로그아웃 상태 | 로그인 버튼 or signed-in 드롭다운 | HIGH |

---

### C. 크로스 커팅 기능 테스트

| ID | 테스트 항목 | 검증 내용 | 우선순위 |
|----|-----------|----------|---------|
| CF-001 | 다크 모드 전환 (Public) | 모든 페이지에서 Light ↔ Dark 전환 시 깨짐 없음 | HIGH |
| CF-002 | 다크 모드 전환 (Admin) | Admin 전체 페이지 다크 모드 정상 | HIGH |
| CF-003 | 인증 상태별 UI 분기 | Admin affordance (편집 버튼) 노출/비노출 | HIGH |
| CF-004 | 404 페이지 처리 | 존재하지 않는 slug 접근 시 404 | MEDIUM |
| CF-005 | API 에러 처리 | 서버 에러 시 사용자 친화적 에러 표시 | MEDIUM |
| CF-006 | 한국어 라벨 | 다음, 이전, 처음, 마지막 등 국제화 텍스트 | LOW |

---

## Part 2: 웹 품질 테스트 (Web Quality)

### D. 접근성 (Accessibility) — CRITICAL

| ID | 테스트 항목 | 검증 기준 (WCAG 2.1 AA) | 적용 범위 |
|----|-----------|------------------------|----------|
| WQ-001 | 색상 대비 (Color Contrast) | 일반 텍스트 4.5:1, 대형 텍스트 3:1 비율 충족 | 전 페이지 |
| WQ-002 | 포커스 링 (Focus States) | 모든 인터랙티브 요소에 2-4px 가시적 포커스 링 | 전 페이지 |
| WQ-003 | Alt 텍스트 (Alt Text) | 의미있는 이미지에 설명적 alt 텍스트 | 전 페이지 |
| WQ-004 | ARIA 라벨 | 아이콘 전용 버튼에 aria-label 부여 | 전 페이지 |
| WQ-005 | 키보드 내비게이션 | Tab 순서가 시각적 순서와 일치, 전체 키보드 접근 | 전 페이지 |
| WQ-006 | Skip Link | "Skip to main content" 링크 존재 및 동작 | 전 페이지 |
| WQ-007 | 헤딩 계층 구조 | h1→h2→h3 순차적, 레벨 건너뛰기 없음 | 전 페이지 |
| WQ-008 | 폼 라벨 연결 | label + for 속성 연결 | Admin 폼 전체 |
| WQ-009 | 에러 메시지 접근성 | aria-live 또는 role="alert" | Admin 폼 에러 |
| WQ-010 | SVG 아이콘 대체 텍스트 | 장식 SVG: aria-hidden, 의미 SVG: role="img" + title | 전 페이지 |
| WQ-011 | Reduced Motion 지원 | prefers-reduced-motion 시 애니메이션 최소화 | 전 페이지 |
| WQ-012 | 토스트 접근성 | aria-live="polite", 포커스 미탈취 | Admin 전체 |

### E. 성능 관련 UI (Performance)

| ID | 테스트 항목 | 검증 기준 | 적용 범위 |
|----|-----------|----------|----------|
| WQ-020 | 이미지 dimension 선언 | width/height 또는 aspect-ratio 설정 (CLS 방지) | 전 페이지 |
| WQ-021 | Lazy 로딩 | Below-the-fold 이미지에 loading="lazy" | Blog/Works 목록 |
| WQ-022 | Hero LCP 최적화 | 프로필 이미지 priority/eager 로딩 | 홈페이지 |
| WQ-023 | 콘텐츠 점프 방지 | 비동기 콘텐츠에 공간 예약 (CLS < 0.1) | 전 페이지 |
| WQ-024 | 스켈레톤/로딩 상태 | 300ms 이상 로딩 시 스켈레톤 또는 스피너 | Admin 테이블, 에디터 |
| WQ-025 | 폰트 로딩 | font-display: swap/optional, FOIT 방지 | 전 페이지 |

### F. 반응형 레이아웃 (Responsive)

| ID | 테스트 항목 | 검증 기준 | 뷰포트 |
|----|-----------|----------|--------|
| WQ-030 | 모바일 가로 스크롤 없음 | 수평 스크롤 바 미발생 | 375px |
| WQ-031 | 태블릿 그리드 전환 | 카드 그리드 적절한 column 수 조정 | 768px |
| WQ-032 | 데스크톱 컨테이너 너비 | max-width 일관성 (max-w-6xl/7xl) | 1440px |
| WQ-033 | Admin 사이드바 반응형 | 모바일에서 사이드바 처리 (접기/오버레이) | 375px~1024px |
| WQ-034 | Viewport meta | width=device-width, initial-scale=1, zoom 비활성화 금지 | 전체 |
| WQ-035 | 본문 최소 폰트 | 모바일 body 텍스트 최소 16px | 375px |
| WQ-036 | Edge 페이지네이션 반응형 | 모바일에서 적절한 페이지네이션 축소 | 375px |

---

## Part 3: UI 심미성 테스트 (Visual / Aesthetic)

### G. 공통 비주얼 품질 (Cross-Page Visual Quality)

#### G-1. 색상 시스템 & 다크 모드
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-001 | Semantic Color Token 일관성 | primary, secondary, muted, destructive 토큰 전 페이지 동일 적용 | CSS 변수 값 비교 |
| VA-002 | 다크 모드 색상 조화 | 다크 모드에서 채도 낮은/밝은 톤 사용, 단순 반전 아님 | 스크린샷 비교 |
| VA-003 | 배지 색상 통일 | Blog/Works 카드의 배지 색상 팔레트 일관성 | 배지 computed style 비교 |
| VA-004 | 그라디언트 스트라이프 | Blog 카드 상단 gradient stripe 일관된 방향/색상 | computed background 확인 |
| VA-005 | 에러/성공 시맨틱 색상 | 에러=빨강, 성공=초록, 경고=노랑 일관 적용 + 아이콘/텍스트 보조 | 토스트/다이얼로그 검증 |

#### G-2. 타이포그래피 시스템
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-010 | 본문 line-height | 1.5~1.75 범위 | computed lineHeight |
| VA-011 | 본문 줄 길이 | 65-75자/줄 (max-width 제한) | 텍스트 컨테이너 너비 측정 |
| VA-012 | 폰트 스케일 일관성 | h1 > h2 > h3 > body 크기 계층 확인 | computed fontSize 비교 |
| VA-013 | font-weight 계층 | Heading 600-700, Body 400, Label 500 | computed fontWeight |
| VA-014 | text-balance 적용 | 제목 텍스트에 text-wrap: balance 적용 | CSS 속성 확인 |
| VA-015 | 말줄임 처리 | 카드 제목 2-line clamp, excerpt 3-line clamp | -webkit-line-clamp 확인 |

#### G-3. 간격 & 레이아웃 시스템
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-020 | 4/8px 간격 시스템 | 패딩/마진이 4px 또는 8px 배수 | computed 값 % 4 === 0 |
| VA-021 | 카드 높이 일관성 | 같은 그리드 내 카드 동일 높이 | getBoundingClientRect 비교 |
| VA-022 | 섹션 간 여백 일관성 | 홈페이지 섹션 간 동일한 vertical 여백 | margin/padding 비교 |
| VA-023 | 컨테이너 센터 정렬 | 메인 콘텐츠 수평 중앙 정렬 | margin-left ≈ margin-right |
| VA-024 | 그리드 gap 일관성 | 카드 그리드 gap 동일 값 | gap computed style |

#### G-4. 그림자 & 깊이 (Elevation)
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-030 | 카드 그림자 일관성 | 모든 카드의 box-shadow 동일 | computed boxShadow 비교 |
| VA-031 | Hover 그림자 확대 | 카드 hover 시 그림자 강화 (elevation 상승 효과) | hover 상태 boxShadow |
| VA-032 | 모달 그림자 | 모달/다이얼로그 적절한 elevation | 모달 boxShadow 존재 확인 |
| VA-033 | z-index 계층 | navbar > modal > dropdown > base 계층 일관성 | z-index 비교 |

---

### H. Public 페이지 심미성

#### H-1. 홈페이지 (/)
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-100 | Hero 시각적 균형 | 프로필 이미지 + 텍스트 좌우/상하 균형 | 요소 정렬 비교 |
| VA-101 | CTA 버튼 시각적 계층 | Primary CTA 1개만 강조, Secondary 시각적으로 종속 | 색상/크기 차이 확인 |
| VA-102 | Featured Works 카드 이미지 비율 | 4:3 aspect-ratio 일관 적용 | aspect-ratio computed |
| VA-103 | 섹션 순서 시각적 흐름 | Hero → Featured → Recent → CTA 자연스러운 흐름 | DOM 순서 확인 |
| VA-104 | 빈 이미지 placeholder 품질 | placeholder가 레이아웃을 유지하고 시각적으로 깔끔 | placeholder 존재 + 크기 확인 |

#### H-2. Blog 목록 (/blog)
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-110 | 카드 그리드 정렬 | 3-column 균등 분배, 간격 일정 | grid-template-columns 확인 |
| VA-111 | 카드 내부 콘텐츠 정렬 | 날짜/태그/제목/excerpt 수직 정렬 일관 | 카드 내부 요소 상대 위치 |
| VA-112 | 페이지네이션 시각적 구분 | 현재 페이지 활성 상태 디자인 차별화 | 활성 버튼 스타일 비교 |
| VA-113 | 카드 앵커 영역 | 전체 카드가 클릭 가능 영역 (a 태그 래핑) | anchor 요소 크기 확인 |

#### H-3. Blog 상세 (/blog/[slug])
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-120 | TOC 시각적 분리 | 본문과 시각적으로 구분되는 사이드바 또는 영역 | TOC 컨테이너 스타일 |
| VA-121 | 콘텐츠 가독성 | 본문 줄 간격, 문단 간 여백, 코드 블록 구분 | lineHeight, margin 측정 |
| VA-122 | 관련 포스트 카드 디자인 | 이전/다음 카드 시각적 대칭/균형 | 좌우 카드 크기/스타일 비교 |
| VA-123 | scroll-margin-top | 헤딩 앵커 클릭 시 navbar 아래로 정확히 스크롤 | scroll-margin-top 값 확인 |

#### H-4. Works 목록 (/works)
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-130 | 4-column 그리드 균형 | 카드 균등 분배, 마지막 행 처리 깔끔 | grid 레이아웃 확인 |
| VA-131 | 썸네일 이미지 크롭 | object-fit: cover로 비율 유지 | computed objectFit |
| VA-132 | 카테고리 배지 디자인 | uppercase, 적절한 패딩, 배경색 | text-transform, padding |
| VA-133 | No-image 카드 | 이미지 없는 카드도 높이/레이아웃 유지 | placeholder 높이 확인 |

#### H-5. Work 상세 (/works/[slug])
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-140 | 비디오 플레이어 레이아웃 | 16:9 비율 유지, 컨테이너 내 정렬 | aspect-ratio 확인 |
| VA-141 | YouTube 임베드 크기 | 본문 너비에 맞는 반응형 크기 | iframe 크기 vs 컨테이너 |
| VA-142 | 메타데이터 시각적 구분 | 제목/날짜/카테고리 계층적 표현 | fontSize, color 계층 |

#### H-6. 정적 페이지 (Introduction, Contact, Resume)
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-150 | 페이지 헤더 일관성 | 모든 정적 페이지 헤더 동일 구조 (라벨 + 제목 + 설명) | 구조/스타일 비교 |
| VA-151 | 콘텐츠 영역 최대 너비 | 가독성 위한 max-width 제한 (65-75ch 또는 prose 클래스) | 컨테이너 max-width |
| VA-152 | PDF 뷰어 디자인 | Resume PDF 뷰어 깔끔한 프레이밍 | 뷰어 컨테이너 스타일 |

---

### I. Admin 페이지 심미성

#### I-1. Admin 레이아웃 & 사이드바
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-200 | 사이드바 너비 일관성 | 고정 폭 사이드바 (예: 240-280px) | computed width |
| VA-201 | 사이드바 활성 상태 | 현재 경로 메뉴 아이템 활성 표시 (배경색/font-weight) | active 상태 스타일 |
| VA-202 | 사이드바 텍스트 | 메뉴 아이템 크기, 간격, 아이콘+텍스트 정렬 | 세로 정렬 확인 |
| VA-203 | "View Site" 버튼 위치 | 사이드바 하단 또는 논리적 위치에 배치 | DOM 위치 + 스타일 |

#### I-2. Admin 테이블
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-210 | 테이블 행 간격 | 적절한 행 높이, 터치 타겟 44px+ | 행 높이 측정 |
| VA-211 | 테이블 칼럼 정렬 | 텍스트 좌측, 상태 중앙, 액션 우측 | text-align 확인 |
| VA-212 | 테이블 검색바 디자인 | 검색 입력 필드 적절한 크기, 아이콘 위치 | 입력 레이아웃 |
| VA-213 | 체크박스 셀렉션 시각 | 선택된 행 배경 강조 | 선택 상태 배경색 |

#### I-3. Admin 에디터
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-220 | TipTap 툴바 고정 | 스크롤 시 에디터 툴바 sticky 유지 | position: sticky 확인 |
| VA-221 | 에디터 영역 시각적 분리 | 입력 필드와 에디터 영역 명확한 경계 | border/background 구분 |
| VA-222 | 탭 전환 (Work 에디터) | General/Media/Content 탭 활성 상태 디자인 | 활성 탭 스타일 |
| VA-223 | 저장 버튼 시각적 계층 | Primary action(저장)이 시각적으로 가장 두드러짐 | 색상/크기 비교 |

#### I-4. Admin 다이얼로그 & 모달
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-230 | 삭제 다이얼로그 디자인 | destructive 색상(빨강), 위험 액션 시각 분리 | 삭제 버튼 색상 |
| VA-231 | 모달 오버레이 | 반투명 오버레이 배경, 콘텐츠 블러 | overlay 스타일 |
| VA-232 | 모달 진입 애니메이션 | 부드러운 fade+scale 진입 | 애니메이션 존재 확인 |
| VA-233 | 미저장 경고 다이얼로그 | 경고 아이콘 + 취소/확인 버튼 명확한 계층 | 아이콘 존재, 버튼 스타일 |

#### I-5. Notion 워크스페이스
| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-240 | 2-pane 분할 비율 | 좌측 리스트 / 우측 에디터 적절한 비율 | 컬럼 너비 비율 |
| VA-241 | 문서 선택 하이라이트 | 선택된 문서 시각적 강조 | 선택 상태 배경색 |
| VA-242 | 저장 상태 인디케이터 | idle/saving/saved/error 시각적 구분 | 상태별 색상/아이콘 |

---

### J. 네비게이션 & 인터랙션 심미성

| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-300 | Navbar 고정 & 배경 | sticky navbar, 스크롤 시 배경 처리 (투명→불투명 또는 blur) | position, backdrop-filter |
| VA-301 | 모바일 메뉴 애니메이션 | 햄버거→메뉴 부드러운 전환 | 트랜지션 확인 |
| VA-302 | 드롭다운 메뉴 디자인 | 테마 토글, 로그인 드롭다운 그림자/경계 | dropdown 스타일 |
| VA-303 | Footer 시각적 분리 | 콘텐츠와 Footer 명확한 분리선 또는 배경 대비 | 배경색 차이 |
| VA-304 | 소셜 아이콘 크기 | 터치 타겟 44px+, 균등 간격 | 아이콘 크기/gap |
| VA-305 | 페이지네이션 버튼 크기 | 터치 타겟 최소 44px, 명확한 active 상태 | 버튼 크기/스타일 |

---

### K. 애니메이션 & 모션 심미성

| ID | 테스트 항목 | 심미성 기준 | 검증 방법 |
|----|-----------|-----------|----------|
| VA-400 | 트랜지션 지속 시간 | 마이크로 인터랙션 150-300ms, 복합 전환 ≤ 400ms | transition-duration 확인 |
| VA-401 | Easing 함수 | ease-out(진입), ease-in(퇴장), linear 미사용 | transition-timing-function |
| VA-402 | 카드 Hover 효과 | 부드러운 scale 또는 shadow 변화 (transform 기반) | hover 트랜지션 |
| VA-403 | 로딩 상태 전환 | 콘텐츠 로딩 → 렌더링 부드러운 전환 | opacity/skeleton 전환 |
| VA-404 | 모달 enter/exit | exit 애니메이션이 enter의 60-70% 속도 | enter vs exit duration |
| VA-405 | 페이드 인 효과 | 홈페이지 섹션 순차적 fade-in | animation 존재 + 순서 |
| VA-406 | Reduced motion 대응 | prefers-reduced-motion 시 모든 애니메이션 최소화/제거 | 미디어 쿼리 적용 확인 |

---

## Part 4: End-to-End 시나리오 테스트

### L. 사용자 시나리오 (User Journey)

| ID | 시나리오 | 흐름 | 우선순위 |
|----|---------|------|---------|
| E2E-001 | 관리자 블로그 작성→발행→확인 | 로그인 → Blog 생성 → 콘텐츠 작성 → 발행 → Public 페이지 확인 | CRITICAL |
| E2E-002 | 관리자 Work 작성→비디오→발행 | 로그인 → Work 생성 → 비디오 업로드 → 발행 → Public 상세 확인 | CRITICAL |
| E2E-003 | 방문자 콘텐츠 탐색 | 홈 → Works 목록 → Work 상세 → 관련 Works → Blog → Blog 상세 | HIGH |
| E2E-004 | 방문자 검색→필터→페이지네이션 | Blog 목록 → 검색 → 카테고리 필터 → 페이지네이션 → 상세 | HIGH |
| E2E-005 | 관리자 사이트 설정 전체 | 사이트 설정 수정 → 홈 페이지 편집 → Resume 업로드 → Public 확인 | HIGH |
| E2E-006 | 인라인 편집 전체 플로우 | 관리자 Public 페이지 → 인라인 편집 → 저장 → 새로고침 확인 | HIGH |
| E2E-007 | 다크 모드 전체 여정 | Light → 전 페이지 순회 → Dark 전환 → 전 페이지 순회 → 깨짐 없음 | MEDIUM |
| E2E-008 | Notion 워크스페이스 편집 | Notion 뷰 → 문서 선택 → 편집 → 자동 저장 → 다른 문서 전환 | MEDIUM |
| E2E-009 | 일괄 관리 워크플로우 | Blog 목록 → 다중 선택 → AI 일괄 수정 → 결과 확인 → 일괄 삭제 | MEDIUM |

---

## 실행 우선순위 요약

| 등급 | 테스트 수 | 설명 |
|------|---------|------|
| **CRITICAL** | ~25 | 인증, 핵심 CRUD, 페이지 렌더링 — 실패 시 서비스 불능 |
| **HIGH** | ~60 | 주요 기능, 접근성, 반응형, 핵심 심미성 — 실패 시 UX 저하 |
| **MEDIUM** | ~50 | 부가 기능, 세부 심미성, 애니메이션 — 실패 시 품질 감소 |
| **LOW** | ~10 | 엣지 케이스, 장식적 요소 — 실패 시 미미한 영향 |

## 테스트 수행 전략

1. **Phase 1 (CRITICAL)**: 인증 → CRUD 전체 → 페이지 렌더링 → 접근성 핵심
2. **Phase 2 (HIGH)**: 반응형 → 페이지네이션 → 인라인 편집 → 색상/타이포 심미성
3. **Phase 3 (MEDIUM)**: 애니메이션 → 디테일 심미성 → E2E 시나리오 → AI 기능
4. **Phase 4 (LOW)**: 엣지 케이스 → 장식적 요소 → 최종 확인

## 기존 테스트와 겹치는 영역 (참고)

> 현재 **140+ 테스트 파일**이 존재하며, 상당수의 기능/UI 테스트가 이미 있음.
> 이 플랜은 **기존 테스트를 보완하는 통합 관점**에서 작성됨:
> - 기존: 개별 컴포넌트/페이지 단위 테스트 위주
> - 추가: E2E 사용자 여정, 크로스 페이지 일관성, 심미성 정량 검증

### 기존 테스트 갭 (이 플랜에서 새로 커버):
- 크로스 페이지 색상/타이포 일관성 검증 (VA-001~VA-015)
- 간격 시스템 4/8px 정량 검증 (VA-020~VA-024)
- 그림자/elevation 일관성 (VA-030~VA-033)
- 애니메이션 duration/easing 정량 검증 (VA-400~VA-406)
- E2E 사용자 여정 시나리오 (E2E-001~E2E-009)
- Admin Notion 워크스페이스 통합 테스트 (AF-040~AF-046)
- 검색/카테고리 필터 통합 테스트 (신규 기능, PF-014 관련)
