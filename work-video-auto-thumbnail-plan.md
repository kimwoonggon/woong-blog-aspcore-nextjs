# Work Video Auto-Thumbnail Plan

## Summary
- `works`에서 수동 썸네일이 없을 때 자동 썸네일을 채운다.
- 우선순위는 `manual > uploaded-video > youtube > content-image > none` 으로 고정한다.
- MP4/비YouTube 영상은 브라우저 `Canvas API`로 첫 프레임을 추출해서 `work-thumbnails` asset로 저장한다.
- YouTube는 `img.youtube.com` 썸네일을 가져와 같은 업로드 API로 asset 저장을 시도한다.
- 영상이 없고 본문 이미지가 있으면 첫 `<img>`를 썸네일 fallback으로 사용한다.

## Implementation
- `work-thumbnail-resolution` helper로 우선순위 분기와 content-image 추출을 분리한다.
- `work-auto-thumbnail` helper로 video frame 추출과 remote image fetch를 분리한다.
- `WorkEditor`는 영상 추가/create-with-videos 완료 후 자동 썸네일 생성과 persisted thumbnail update를 처리한다.
- `PublicWorkService`, `AdminWorkService`, `PublicHomeService`는 동일 우선순위 helper로 `thumbnailUrl` fallback을 계산한다.
- `works` Playwright는 auto-thumbnail create/edit/mixed/photo-only를 포함해 녹화와 screenshot을 남긴다.

## Verification
- Frontend unit: thumbnail priority helpers, editor auto-thumbnail behavior
- Backend test compile/run: work-related tests
- Playwright: `npm run test:e2e:works`
- Archive: `test-results/playwright-archives/work-auto-thumbnail-*`

## Defaults
- 수동 썸네일은 자동 썸네일이 절대 덮어쓰지 않는다.
- 같은 세션에서 auto-generated YouTube thumbnail은 uploaded-video가 뒤에 들어오면 교체될 수 있다.
- YouTube fetch 실패는 영상 저장을 막지 않고, thumbnail fallback만 downgrade 된다.
