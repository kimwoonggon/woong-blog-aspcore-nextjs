 # GHCR 기반 Dev/Prod 분리 배포 재설계 계획

  ## Summary

  subagent 검토와 현재 repo 상태를 합치면, 지금 구조는 docker-compose.yml + local HTTPS override 중심의 개발용 런타임이며, devops-plan-0413.md가 요구한 “명시적 dev/prod compose 분리”, “운영용
  nginx/Let’s Encrypt”, “서버는 build 없이 GHCR image pull만 수행”을 아직 만족하지 않습니다.

  이번 구현은 아래를 목표로 고정합니다.

  - dev는 source-build 개발 스택
  - main은 runtime-only 운영 스택
  - main CI가 prod shape를 검증한 뒤에만 GHCR publish
  - 서버는 docker compose pull && docker compose up -d만 수행
  - 운영 HTTPS는 nginx가 종료하고, frontend/backend는 외부에 직접 publish하지 않음
  - 현재 repo의 red 테스트는 모두 통과 상태로 끌어올려야 CI green으로 인정

  ## Key Changes

  ### 1. Compose를 명시적으로 분리

  - 새 파일:
      - docker-compose.dev.yml
      - docker-compose.prod.yml
  - docker-compose.dev.yml
      - frontend, backend는 build: 사용
      - db는 postgres:16-alpine
      - nginx는 nginx/local-https.conf 사용
      - ENABLE_LOCAL_ADMIN_SHORTCUT=true
      - Auth__EnableTestLoginEndpoint=true
      - ASPNETCORE_ENVIRONMENT=Development
      - backend는 필요 시 8080:8080 publish 허용
      - postgres-data, data-protection-keys, media-storage named volume 유지
  - docker-compose.prod.yml
      - frontend, backend는 build: 금지, image:만 사용
      - FRONTEND_IMAGE, BACKEND_IMAGE env로 이미지 주입
      - frontend, backend는 expose만, ports 금지
      - 외부 노출은 nginx의 80:80, 443:443만 허용
      - ASPNETCORE_ENVIRONMENT=Production
      - ENABLE_LOCAL_ADMIN_SHORTCUT=false
      - Auth__EnableTestLoginEndpoint=false
      - Auth__SecureCookies=true
      - Auth__RequireHttpsMetadata=true
      - Auth__DataProtectionKeysPath=/app/data-protection
      - Auth__MediaRoot=/app/media
      - postgres-data, data-protection-keys, media-storage 동일 유지

  ### 2. nginx를 로컬/운영으로 분리

  - 유지:
      - nginx/local-https.conf
  - 새 파일:
      - nginx/prod-bootstrap.conf
      - nginx/prod.conf
  - nginx/prod-bootstrap.conf
      - 최초 인증서 발급 전용
      - 80만 리슨
      - /.well-known/acme-challenge/ 서빙
      - 나머지는 frontend/backend reverse proxy 허용
      - SSL cert 없이 기동 가능해야 함
  - nginx/prod.conf
      - 80 -> 443 redirect
      - 단 /.well-known/acme-challenge/는 예외
      - 443 ssl, http2 on
      - /api/와 /media/는 backend
      - 나머지는 frontend
      - Host, X-Forwarded-For, X-Forwarded-Proto, X-Forwarded-Host 전달
      - 업로드 한도와 timeout은 현재 nginx 설정 기준 유지
  - 기본 운영 방향:
      - nginx 이미지는 별도 빌드하지 않고 nginx:alpine 사용
      - 인증서는 서버 마운트 또는 certbot webroot 방식으로 관리

  ### 3. GHCR/CI/CD를 prod artifact 중심으로 재구성

  - 새 workflow 구조:
      - ci-dev.yml
      - ci-main-runtime.yml
      - publish-ghcr-main.yml
      - 필요 시 promote-main-runtime.yml는 유지하되 prod artifact 기준으로 수정
  - ci-dev.yml
      - trigger: feature/**, dev, pull_request -> dev
      - 실행:
          - npm ci
          - npm run lint
          - npm run typecheck
          - npm run test -- --run
          - backend dotnet test
          - docker compose -f docker-compose.dev.yml up -d --build
          - dev smoke
          - Playwright dev smoke
              - public smoke
              - runtime-auth smoke
  - ci-main-runtime.yml
      - trigger: main, pull_request -> main, release/main-promote -> main
      - 실행:
          - frontend/backend 테스트 동일
          - frontend/backend 이미지를 local tag로 빌드
          - docker-compose.prod.yml를 local tag로 기동
          - NGINX_DEFAULT_CONF=./nginx/prod-bootstrap.conf로 cert 없는 CI 환경 검증
          - main-mode smoke
          - Playwright public smoke
              - /
              - /login
              - /blog
              - /works
              - local admin hidden
              - /api/auth/test-login 404
  - publish-ghcr-main.yml
      - trigger: workflow_run on CI Main Runtime success for main
      - workflow_dispatch는 수동 재실행용으로만 유지
      - frontend/backend 각각 GHCR push
      - 태그:
          - :main
          - :latest
          - :sha-<12>
      - permissions:
          - contents: read
          - packages: write
  - 중요한 운영 계약:
      - main push만으로 곧바로 publish하지 않음
      - main CI green 이후에만 GHCR publish
      - 서버는 source build 금지

  ### 4. main promotion surface를 운영 자산 중심으로 재정렬

  - scripts/promote-main-runtime.sh 유지
  - scripts/main-runtime-allowlist.txt를 prod 기준으로 수정
  - main에 포함:
      - docker-compose.prod.yml
      - nginx/prod.conf
      - nginx/prod-bootstrap.conf
      - .github/workflows/ci-main-runtime.yml
      - .github/workflows/publish-ghcr-main.yml
      - DEPLOYMENT.md
      - .env.prod.example
      - runtime source (src, backend, public, Dockerfiles 등)
  - main에서 제외:
      - docker-compose.dev.yml
      - nginx/local-https.conf
      - .local-certs
      - tests
      - .codex, .agents
      - planning/todo markdown
  - README.md는 branch strategy와 high-level 설명만 남기고, 실제 서버 절차는 DEPLOYMENT.md로 분리

  ### 5. 운영 env / 배포 runbook 추가

  - 새 파일:
      - .env.prod.example
      - DEPLOYMENT.md
  - .env.prod.example 필수 항목:
      - FRONTEND_IMAGE
      - BACKEND_IMAGE
      - POSTGRES_DB
      - POSTGRES_USER
      - POSTGRES_PASSWORD
      - Auth__Enabled=true
      - Auth__ClientId
      - Auth__ClientSecret
      - Auth__AdminEmails__0
      - Auth__CookieName
      - Auth__DataProtectionKeysPath=/app/data-protection
      - Auth__MediaRoot=/app/media
      - Auth__SecureCookies=true
      - Auth__RequireHttpsMetadata=true
      - ConnectionStrings__Postgres=Host=db;Port=5432;...
      - Proxy__KnownNetworks__0 또는 Proxy__KnownProxies__*
      - Security__UseHttpsRedirection=true
      - Security__UseHsts=true
  - DEPLOYMENT.md에 포함할 절차:
      1. 서버에서 GHCR 로그인
      2. docker-compose.prod.yml + prod-bootstrap.conf로 최초 기동
      3. certbot webroot 방식으로 인증서 발급
      4. nginx/prod.conf로 전환
      5. 이후 배포는 docker compose pull && docker compose up -d
      6. 갱신 후 nginx -s reload 또는 container reload

  ## Interfaces / Contracts

  - 새 운영 진입 파일:
      - docker-compose.dev.yml
      - docker-compose.prod.yml
      - nginx/prod-bootstrap.conf
      - nginx/prod.conf
      - .env.prod.example
      - DEPLOYMENT.md
  - 이미지 계약:
      - FRONTEND_IMAGE=ghcr.io/<owner>/<repo>-frontend:main
      - BACKEND_IMAGE=ghcr.io/<owner>/<repo>-backend:main
      - 롤백 시 sha-<12> 태그 override 허용
  - CI 계약:
      - dev CI는 source-build 검증
      - main CI는 prod compose/image-based 검증
      - GHCR publish는 main runtime CI success 이후만 허용

  ## Test Plan

  - Dev compose
      - docker compose -f docker-compose.dev.yml config
      - docker compose -f docker-compose.dev.yml up -d --build
      - /login에 Continue as Local Admin visible
      - /api/auth/test-login enabled
      - local HTTPS 정상
      - build: 없음
      - nginx만 80/443 노출
      - /login에서 local admin hidden
      - /api/auth/test-login 404
      - /api/health, /blog, /works 정상
  - CI
      - full frontend test gate
      - full backend test gate
      - dev/main 각각 compose smoke
      - main에서는 public Playwright smoke
      - GHCR publish는 main runtime CI success 이후만
  - Server rollout
      - bootstrap nginx로 ACME challenge 통과
      - prod nginx 전환 후 HTTPS 정상
      - 재배포 후 session/data-protection/media/postgres 지속성 유지

  ## Assumptions

  - 현재 repo의 기존 red 테스트는 이번 작업 범위에 포함해 모두 green으로 만든다. 사용자가 원하는 기준은 “테스트가 다 돌아서 통과해야 성공”이므로 partial gate는 두지 않는다.
  - 운영 nginx는 별도 custom image 없이 config mount 방식으로 간다.
  - Let’s Encrypt는 host 패키지 설치보다 certbot container + webroot 방식을 기본값으로 선택한다.
  - 운영 서버는 main runtime-only 브랜치를 checkout한 상태에서 compose를 실행한다.
