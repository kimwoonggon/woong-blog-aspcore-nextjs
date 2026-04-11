# Work Inline Redirect TODO

## Plan
- [x] 실행계획 문서 작성
- [x] TODO 문서 작성

## Redirect And Save
- [x] Public `works` 목록 링크에 복귀 경로 전달
- [x] Public inline create 저장 후 `/works` 첫 페이지 복귀
- [x] Public inline edit 저장 후 진입 전 목록 페이지 복귀
- [x] Video-only edit도 저장 완료 흐름 허용

## Layout Stability
- [x] `works` 카드 높이/내부 영역 길이 고정
- [x] pagination 위치 흔들림 방지 검증 추가

## Verification
- [ ] `src/test/work-editor.test.tsx`
  현재 Vitest worker가 테스트 파일을 `queued` 상태로만 두고 실행하지 못해 결과 확보 실패
- [ ] 관련 Playwright spec 실행
  compose 빌드 후 직접 실행까지는 진행했지만 Playwright runner가 브라우저 프로세스 없이 무출력 대기 상태여서 결과 확보 실패
