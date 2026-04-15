너는 DevOps/Infra 시니어 엔지니어다.
내 프로젝트를 “로컬 개발용 compose”와 “운영 배포용 compose”로 분리해 설계해 줘.


내 프로젝트의 배포 구조를 로컬 개발용과 운영용으로 분리해서 설계해 줘.

현재 상황:
- frontend: Next.js
- backend: ASP.NET Core
- db: PostgreSQL
- reverse proxy: Nginx
- 현재는 docker compose에서 build: 로 직접 빌드해서 실행 중
- 로컬에서는 self-signed/local cert로 HTTPS 테스트 중
- 운영에서는 GitHub Actions로 이미지를 빌드해서 GHCR에 push하고, 서버에서는 build 없이 image pull만 해서 실행하고 싶음
- 운영 HTTPS는 Nginx + Let's Encrypt로 처리하고 싶음
- backend의 Auth 관련 설정으로는 운영에서 다음이 필요함:
  - Auth__SecureCookies=true
  - Auth__RequireHttpsMetadata=true
- backend는 Auth__DataProtectionKeysPath=/app/data-protection 를 사용하고 있고,
  data-protection key는 재배포 후에도 유지되어야 함
- media 파일 저장소와 postgres 데이터도 volume으로 영속화되어야 함
- 운영에서는 backend와 frontend를 외부에 직접 publish하지 않고, nginx만 80/443을 외부에 노출하고 싶음
- 내부 통신은 nginx -> frontend:3000, nginx -> backend:8080 구조로 하고 싶음
- 운영에서는 docker compose의 build: 대신 image: ghcr.io/... 형식으로 전환하고 싶음
- 서버에서는 docker compose pull && docker compose up -d 방식으로만 배포하고 싶음

원하는 결과물:
1. docker-compose.dev.yml
   - 로컬 개발용
   - build: 사용
   - local admin 접근 나옴
   - local cert 사용
   - 필요하면 backend 8080 publish 가능
   - ASPNETCORE_ENVIRONMENT=Development

2. docker-compose.prod.yml
   - 운영용
   - local admin 접근 안나옴
   - build: 금지, image: 사용
   - frontend/backend는 expose만 하고 ports는 nginx만 열기
   - postgres-data, data-protection-keys, media-storage volume 유지
   - ASPNETCORE_ENVIRONMENT=Production
   - backend에 운영용 Auth 환경변수 반영

3. nginx/local-https.conf
   - 로컬 self-signed 인증서용 설정

4. nginx/prod.conf
   - 운영용 설정
   - 80 -> 443 redirect
   - /.well-known/acme-challenge/ 처리 가능하게
   - 443 ssl http2
   - frontend와 backend로 reverse proxy
   - X-Forwarded-Proto, X-Forwarded-For, Host 헤더 적절히 전달

5. GitHub Actions workflow
   - main 브랜치 push 시 동작
   - frontend 이미지와 backend 이미지를 각각 빌드
   - GHCR 로그인
   - ghcr.io/<owner>/... 형태로 push
   - latest 태그와 가능하면 commit sha 태그도 함께 사용
   - permissions에 packages: write 포함

6. 운영 서버 배포 절차
   - 서버에서 GHCR 로그인 방법
   - docker compose pull / up -d 명령
   - 최초 HTTPS 발급 전 준비 절차
   - Let’s Encrypt 인증서 발급 후 Nginx 적용 절차
   - 갱신 시 주의사항

추가 요구사항:
- 왜 이렇게 분리하는지 이유도 설명해 줘
- 특히 아래 항목을 명확히 설명해 줘:
  - 왜 운영에서는 build가 아니라 image pull이 좋은지
  - 왜 HTTPS는 앱 이미지가 아니라 Nginx에서 처리하는지
  - data-protection-keys:/app/data-protection 가 왜 필요한지
  - 왜 backend 8080 포트를 운영에서 외부 publish하지 않는 게 좋은지
- 가능한 한 바로 복붙 가능한 예시 파일 전체 내용을 보여줘
- 민감정보는 .env 또는 GitHub Secrets로 분리해 줘
- 설명은 한국어로 해 줘

최종 출력 형식:
- 먼저 전체 아키텍처를 짧게 설명
- 그 다음 각 파일을 파일명 단위로 구분해서 전체 코드 제공
- 마지막에 적용 순서를 1, 2, 3 단계로 정리