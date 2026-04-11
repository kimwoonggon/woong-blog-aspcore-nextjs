# Work Inline Redirect Plan

## Goals
- Public `works` inline edit에서 본문을 안 바꾸고 영상만 수정한 경우에도 저장 완료 흐름을 탈 수 있게 한다.
- Public `works` create 후에는 새 상세가 아니라 `works` 목록 첫 페이지로 돌아가게 한다.
- Public `works` edit 후에는 진입 전 목록 페이지를 기억했다가 그 페이지로 복귀하게 한다.
- Public `works` 카드 높이를 일정하게 유지해 페이지마다 pagination 위치가 흔들리지 않게 한다.

## Approach
- `WorkEditor`에 persisted video mutation 상태를 추가해서 video-only 변경도 `Update Work`를 허용한다.
- `/works` 카드 링크에 현재 목록 위치를 담은 `returnTo`를 전달하고, inline save 완료 시 이를 우선 사용한다.
- Public inline create는 `/works`로 고정 복귀시키고, edit는 전달된 `returnTo`가 있으면 그 값으로 복귀시킨다.
- 카드 레이아웃은 이미지/본문/태그 영역 높이를 더 엄격하게 고정하고, E2E로 높이 편차를 검증한다.

## Verification
- `vitest`: `src/test/work-editor.test.tsx`
- `playwright`: public works inline redirect / detail inline edit / works pagination and layout stability 관련 spec
