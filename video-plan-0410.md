# Video Plan 0410

## Current Status
- [x] `videos[]` 기반 work video 모델 도입
- [x] create 화면 staged videos 지원
- [x] edit 화면 YouTube/add/upload/reorder/remove 지원
- [x] public work detail video 렌더링
- [x] backend lifecycle integration test 통과
- [x] frontend unit/api parser tests 통과
- [x] Playwright local stack video flows 통과
- [ ] S3-compatible lane 안정화
- [x] real R2 smoke verification
- [x] real browser R2 upload flow verification

## Notes
- Development/Testing에서는 local video storage를 강제해 authoring과 Playwright 흐름을 안정화했다.
- Production에서는 R2 설정이 있을 때 direct upload 경로를 사용한다.
- cleanup job, upload session expiry, strict parser, `VideosVersion` conflict control을 모두 반영했다.
- S3-compatible lane는 MinIO 기준으로만 아직 미완료다.
- 실제 Cloudflare R2 smoke는 성공했다. `work 생성 -> presigned PUT -> confirm -> public JSON/readback -> playback HEAD 200`까지 확인했다.
- 실제 브라우저 R2 업로드도 CORS 수정 후 통과했다.
- 현재 real R2 기준으로는 `글 + 업로드 영상 + YouTube`가 모두 공개 페이지에서 렌더된다.
- 필요한 최소 CORS 예시:
  ```json
  [
    {
      "AllowedOrigins": ["http://localhost", "http://localhost:3000", "https://YOUR_PROD_DOMAIN"],
      "AllowedMethods": ["GET", "PUT", "HEAD"],
      "AllowedHeaders": ["Content-Type"],
      "ExposeHeaders": ["ETag"],
      "MaxAgeSeconds": 3600
    }
  ]
  ```
