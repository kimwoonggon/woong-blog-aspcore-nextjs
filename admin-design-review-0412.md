# Admin Page 디자인 냉정 리뷰: Work & Blog

---

## S등급 문제 (지금 바로 고쳐야 할 것)

### 1. UI에 개발자 커밋 메시지가 그대로 노출됨

포트폴리오 사이트를 방문한 채용 담당자나 클라이언트가 admin에 들어왔을 때 이런 문구를 봅니다:

- `src/app/admin/blog/page.tsx:26`: *"Titles now act as primary edit links, and the new Notion view keeps document browsing beside the editor with local batch-selection scaffolding for future bulk actions."*
- `src/app/admin/works/page.tsx:31`: *"Click a title to edit directly, or create a new work and return to this list as soon as it saves."*
- `src/components/admin/WorkEditor.tsx:1218`: *"Add thumbnail and icon assets with clear click-to-upload fields. Dragging a file onto the input still works, but it is no longer the only obvious path."*

**이건 admin이 아니라 PR 설명문입니다.** 프로페셔널한 CMS에서 이런 문구는 제품 미완성의 느낌을 줍니다. 설명 텍스트가 도움이 되는 게 아니라, 신뢰를 깎아먹습니다.

### 2. WorkEditor가 1,600줄짜리 God Component

`src/components/admin/WorkEditor.tsx` 파일 하나에:
- state 변수 **20개 이상**
- async 함수 **15개 이상**
- 비디오 업로드/정렬/삭제/YouTube 연동
- 썸네일 자동생성
- 메타데이터 key-value 관리
- 이미지 업로드
- 탭 네비게이션
- 인라인 모드 분기

**한 컴포넌트가 모든 걸 다 합니다.** 이건 디자인 문제이기도 하지만 유지보수 불가능 상태입니다. 비디오 하나 추가하려고 1,600줄 파일을 열어야 합니다.

### 3. "탭"이 탭이 아님

General / Media / Content 탭이 있지만, 실제로는 **세 섹션이 전부 동시에 렌더링**되고 `scrollIntoView`로 스크롤만 합니다:

```tsx
// 실제로는 탭 전환이 아니라 스크롤
nextSection?.scrollIntoView({ behavior: 'smooth', block: 'start' })
```

결과: 사용자가 끝없는 스크롤 페이지를 봅니다. 포트폴리오 작품 하나 편집하는 화면이 **고층빌딩처럼 길어집니다.** sticky 탭 바는 시각적 노이즈만 더합니다.

---

## A등급 문제 (사용 경험을 확실히 깎는 것)

### 4. 리스트 페이지에 썸네일 프리뷰가 없음

**포트폴리오 CMS인데 작품 목록에 이미지가 하나도 없습니다.** Blog도 마찬가지입니다. 제목과 텍스트만 나열된 테이블은 2015년 WordPress 느낌입니다. Notion, Linear, Framer 같은 도구들은 리스트 자체가 시각적입니다.

### 5. 비디오 관리 UX가 버튼 지옥

비디오 하나당 버튼이 **5개**:

```
[Insert Into Body] [Remove From Body] [Move Up] [Move Down] [Remove]
```

전부 `variant="outline"`으로 동일한 시각 가중치. 어떤 게 위험한 동작이고 어떤 게 일상 동작인지 구분이 안 됩니다. 드래그 앤 드롭이 없고 텍스트 버튼으로만 정렬합니다.

### 6. 정보 박스(callout) 스타일이 난립

한 화면 안에서 색상 시스템이 파편화:

| 색상 | 용도 |
|------|------|
| `emerald` | 성공 힌트, "videos saved" 알림 |
| `sky` | 업로드 영역, 안내 문구 |
| `amber` | 경고 (orphan video) |
| `green` | Published 뱃지 |
| `yellow` | Draft 뱃지 |
| `red` | 삭제 버튼 |

**통일된 Alert/Callout 컴포넌트**가 없고 매번 인라인으로 색상을 조합합니다. 같은 "안내" 역할을 하는 박스가 emerald일 때도 있고 sky일 때도 있습니다.

### 7. 색상 토큰과 하드코딩 혼용

```tsx
// 이 두 가지가 같은 파일에서 섞임
className="text-gray-500"           // 하드코딩
className="text-muted-foreground"   // 시맨틱 토큰
```

다크 모드에서 `text-gray-500`은 배경과 대비가 불충분할 수 있고, 디자인 시스템 일관성이 깨집니다.

### 8. 페이지네이션 디자인이 구식

`First / Previous / 1/5 / Next / Last` 텍스트 버튼은 복잡합니다. 콘텐츠 수가 많지 않은 개인 포트폴리오에서 이 방식은 과잉이면서 동시에 모던하지 않습니다.

---

## B등급 문제 (개선하면 품질이 올라가는 것)

### 9. 생성 모드에 버튼이 2개

```
[Create Work]  [Create And Add Videos]
```

비디오가 staged 되면 `Create Work`은 disabled되고, staged 안 하면 `Create And Add Videos`가 disabled. **사용자 입장에서 왜 버튼이 두 개인지 즉시 이해 불가.** 하나의 버튼이 상태에 따라 동작하면 됩니다.

### 10. Flexible Metadata가 "Media & Videos" 탭 안에 있음

메타데이터는 의미상 **General** 섹션입니다. Media 탭에 들어가서 메타데이터를 찾으라는 건 직관적이지 않습니다.

### 11. `window.alert()` 폴백

```tsx
window.alert(error instanceof Error ? error.message : 'Failed to delete blogs.')
```

`toast`도 쓰고 있는데 에러 시에는 `window.alert()`. 브라우저 기본 alert은 모던 UI를 한 방에 깨뜨립니다.

### 12. aria-label은 한국어, 버튼 텍스트는 영어

```tsx
aria-label="처음"  // 한국어
>First</Button>    // 영어
```

접근성과 일관성 둘 다 어중간합니다.

### 13. 사이드바에 여백 비율 문제

`md:w-64` 고정 사이드바 + 콘텐츠 `max-w-4xl`. 대형 모니터에서는 콘텐츠 양쪽에 거대한 빈 공간이 생깁니다. 모바일에서는 사이드바가 상단 바로 변하는데 햄버거 메뉴 없이 전체 nav가 노출됩니다.

---

## 잘한 점 (솔직하게)

- **BlogEditor**는 비교적 깔끔합니다. 카드 기반 레이아웃, excerpt 글자수 카운터, published 토글 pill — 적절합니다.
- **shadcn/ui 컴포넌트** 기반으로 일관된 Button/Input/Dialog/Table을 씁니다. 기반은 좋습니다.
- **Ctrl+S 저장, beforeunload 경고, dirty state 감지** — 기능적으로 CMS에 필요한 건 다 있습니다.
- **Delete confirmation dialog** — 파괴적 동작에 확인을 받습니다.
- **Batch AI Fix** — 차별화 기능으로 좋은 방향입니다.

---

## 총평

| 영역 | 점수 | 한줄 |
|------|------|------|
| 시각 디자인 | **5/10** | shadcn 기본기는 있지만 칼라 시스템 파편화, 리스트에 시각 요소 부재 |
| 정보 구조 | **4/10** | WorkEditor 탭 구조가 가짜, 메타데이터 위치 오류, 설명 텍스트 과잉 |
| UX 흐름 | **5/10** | 기능은 다 있지만 버튼 과다, 스크롤 과다, 직관적이지 않은 분기 |
| 코드 퀄리티 | **4/10** | 1,600줄 God component, 스타일 혼용, alert 폴백 |
| 프로페셔널함 | **3/10** | 커밋 메시지 노출이 치명적. "미완성 사이드프로젝트" 느낌 |

---

## 현실적 우선순위

1. **즉시**: 모든 개발자용 설명 텍스트 제거/교체
2. **이번 주**: WorkEditor를 서브 컴포넌트로 분리 (VideoManager, MetadataEditor, MediaUploader)
3. **이번 주**: 리스트 페이지에 썸네일 프리뷰 추가
4. **다음**: 탭을 실제 탭으로 전환 (조건부 렌더링)
5. **다음**: 칼라 시스템 통일 (하드코딩 제거, 공통 Callout 컴포넌트)
