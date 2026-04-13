# DevOps Transition Todo (2026-04-13)

## A. Branch / Release Policy

- [x] `A1` `dev`를 기본 개발 브랜치로 사용한다.
- [x] `A2` `main`에서만 GHCR publish가 실행되도록 workflow를 분리한다.
- [x] `A3` `main` promotion용 runtime worktree 스크립트를 유지한다.
- [x] `A4` `main` promotion allowlist를 prod artifact 기준으로 재정렬한다.
- [ ] `A5` `release/main-promote -> main` 기준으로 prod artifact만 승격되게 만든다.

## B. Compose Split

- [x] `B1` `docker-compose.dev.yml`를 추가한다.
- [x] `B2` `docker-compose.prod.yml`를 추가한다.
- [x] `B3` dev compose는 `build:` 기반 source-build 개발 스택으로 고정한다.
- [x] `B4` prod compose는 `image:` 기반 pull-only 운영 스택으로 고정한다.
- [x] `B5` prod compose에서 `frontend`/`backend`의 외부 `ports`를 제거한다.
- [x] `B6` prod compose에서 `nginx`만 `80/443`을 외부에 노출한다.
- [x] `B7` prod compose에 `postgres-data`, `data-protection-keys`, `media-storage` persistent volume을 유지한다.

## C. Auth / Runtime Policy

- [x] `C1` `main` 기본값에서 `Continue as Local Admin`가 숨겨지도록 한다.
- [x] `C2` `main` 기본값에서 `/api/auth/test-login`이 비활성화되도록 한다.
- [x] `C3` `docker-compose.dev.yml`에서 local admin shortcut / test-login을 명시적으로 활성화한다.
- [x] `C4` `docker-compose.prod.yml`에서 local admin shortcut / test-login을 명시적으로 비활성화한다.
- [x] `C5` `docker-compose.prod.yml`에서 `ASPNETCORE_ENVIRONMENT=Production`을 명시한다.
- [x] `C6` `docker-compose.prod.yml`에서 `Auth__SecureCookies=true`와 `Auth__RequireHttpsMetadata=true`를 명시한다.

## D. Nginx / HTTPS

- [x] `D1` `nginx/local-https.conf`를 로컬 self-signed HTTPS 용도로 유지한다.
- [x] `D2` `nginx/prod-bootstrap.conf`를 추가한다.
- [x] `D3` `nginx/prod.conf`를 추가한다.
- [x] `D4` 운영 nginx에서 `/.well-known/acme-challenge/`를 처리한다.
- [x] `D5` 운영 nginx에서 `80 -> 443` redirect를 적용한다.
- [x] `D6` 운영 nginx에서 `frontend`/`backend` reverse proxy 헤더를 정리한다.

## E. CI / GHCR

- [x] `E1` frontend lint/typecheck/unit test job을 CI에 넣는다.
- [x] `E2` backend test job을 CI에 넣는다.
- [x] `E3` compose smoke를 CI에 넣는다.
- [x] `E4` public Playwright smoke를 CI에 넣는다.
- [x] `E5` GHCR publish를 `main` CI success 이후에만 실행되게 한다.
- [x] `E6` `ci-dev.yml`로 dev CI를 분리한다.
- [x] `E7` `ci-main-runtime.yml`로 main runtime CI를 분리한다.
- [x] `E8` main runtime CI가 `docker-compose.prod.yml` 기반으로 검증되게 한다.
- [x] `E9` GHCR publish가 prod artifact 기준으로만 이미지를 배포하게 한다.

## F. Deployment Artifacts

- [x] `F1` `.env.prod.example`를 추가한다.
- [x] `F2` `DEPLOYMENT.md`를 추가한다.
- [x] `F3` 서버에서 `docker compose pull && docker compose up -d`만으로 운영 배포가 가능하게 한다.
- [x] `F4` Let’s Encrypt 최초 발급 절차를 runbook에 명시한다.
- [x] `F5` 인증서 갱신 및 nginx reload 절차를 runbook에 명시한다.

## G. Verification

- [x] `G1` 현재 compose smoke 스크립트가 `dev`/`main` branch policy를 구분해 검증한다.
- [x] `G2` `docker-compose.dev.yml` 기준 smoke를 통과시킨다.
- [x] `G3` `docker-compose.prod.yml` 기준 smoke를 통과시킨다.
- [x] `G4` prod bootstrap nginx 기준 HTTPS 발급 전 동작을 검증한다.
- [x] `G5` prod nginx 기준 reverse proxy / auth / public page smoke를 검증한다.
- [ ] `G6` CI 전체가 green이어야만 `main` publish가 가능함을 확인한다.
